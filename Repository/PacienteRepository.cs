using interfaces;
using Microsoft.Data.Sqlite;
using Models;
using System.Globalization;
namespace Repository;

public class PacienteRepository : IPacienteRepository
{
    private readonly string connectionString;

    public PacienteRepository(string _connectionString)
    {
        connectionString = _connectionString;
    }

    public void CrearPaciente(Paciente paciente)
    {
        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();

        using var transaccion = conexion.BeginTransaction();
        try
        {
            const string sql = @"
                INSERT INTO Paciente
                    (nombre, plus, por_o_mensual, obra_social, montoBase, montoPlus)
                VALUES
                    (@nombre, @plus, @por_o_mensual, @obra_social, @montoBase, @montoPlus)";
            using (var comando = new SqliteCommand(sql, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@nombre", paciente.nombre);
                comando.Parameters.AddWithValue("@plus", paciente.plus);
                comando.Parameters.AddWithValue("@por_o_mensual", paciente.por_o_mensual.ToString());
                comando.Parameters.AddWithValue("@obra_social", paciente.obra_social);
                comando.Parameters.AddWithValue("@montoBase", paciente.montoBase);
                comando.Parameters.AddWithValue("@montoPlus", paciente.montoPlus);
                comando.ExecuteNonQuery();
            }

            using (var comandoId = new SqliteCommand("SELECT last_insert_rowid()", conexion, transaccion))
                paciente.idPaciente = Convert.ToInt32(comandoId.ExecuteScalar());

            const string sqlHorario = @"
                INSERT INTO HorarioPaciente (""id-Paciente"", dia_semana, hora_inicio, hora_fin)
                VALUES (@idPaciente, @diaSemana, @horaInicio, @horaFin)";
            foreach (var horario in paciente.horario)
            {
                using var comandoHorario = new SqliteCommand(sqlHorario, conexion, transaccion);
                comandoHorario.Parameters.AddWithValue("@idPaciente", paciente.idPaciente);
                comandoHorario.Parameters.AddWithValue("@diaSemana", horario.diaSemana.ToString());
                comandoHorario.Parameters.AddWithValue("@horaInicio", horario.horaInicio.ToString(@"hh\:mm"));
                comandoHorario.Parameters.AddWithValue("@horaFin", horario.horaFin.ToString(@"hh\:mm"));
                comandoHorario.ExecuteNonQuery();
            }

            transaccion.Commit();
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public List<Paciente> ListarPacientes()
    {
        var Pacientes = new List<Paciente>();
        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();

        string sqlPa = "SELECT * FROM Paciente";
        using var comandoPa = new SqliteCommand(sqlPa, conexion);
        using (var lectorPa = comandoPa.ExecuteReader())
        {
            while (lectorPa.Read())
            {
                var paci = new Paciente
                {
                    idPaciente = Convert.ToInt32(lectorPa["id-Paciente"]),
                    nombre = lectorPa["nombre"].ToString(),
                    plus = LeerBooleano(lectorPa["plus"]),
                    por_o_mensual = (ModalidadDePago)Enum.Parse(typeof(ModalidadDePago), lectorPa["por_o_mensual"].ToString()!, true),
                    obra_social = LeerBooleano(lectorPa["obra_social"]),
                    montoBase = LeerDecimal(lectorPa["montoBase"]),
                    montoPlus = LeerDecimal(lectorPa["montoPlus"]),
                    horario = new List<HorarioPaciente>() // Inicializamos la lista vacía
                };
                Pacientes.Add(paci);
            }
        }

    // Completado del bucle para cargar los horarios de cada paciente
        foreach (var paciente in Pacientes)
        {
            string sqlHor = "SELECT * FROM HorarioPaciente WHERE \"id-Paciente\" = @idPaciente";
            using var comandoHor = new SqliteCommand(sqlHor, conexion);
            comandoHor.Parameters.AddWithValue("@idPaciente", paciente.idPaciente);

            using (var lectorHor = comandoHor.ExecuteReader())
            {
                while (lectorHor.Read())
                {
                    var hor = new HorarioPaciente
                    {
                        idHorario = Convert.ToInt32(lectorHor["id_horario"]),
                        idPaciente = paciente.idPaciente,
                        paciente = paciente, // Vinculamos la referencia al objeto paciente en memoria
                        diaSemana = LeerDiaSemana(lectorHor["dia_semana"].ToString()!),
                        horaInicio = TimeSpan.Parse(lectorHor["hora_inicio"].ToString()!),
                        horaFin = TimeSpan.Parse(lectorHor["hora_fin"].ToString()!)
                    };

                    paciente.horario.Add(hor);
                }
            }
        }

        return Pacientes;
    }

    public Paciente? GetPaciente(int id)
    {
        Paciente? paciente = null;

        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();

    // 1. Consultar los datos fijos del paciente por su ID
        string sqlPa = "SELECT * FROM Paciente WHERE \"id-Paciente\" = @id";
        using (var comandoPa = new SqliteCommand(sqlPa, conexion))
        {
            comandoPa.Parameters.AddWithValue("@id", id);
            using var lectorPa = comandoPa.ExecuteReader();

            if (lectorPa.Read())
            {
                paciente = new Paciente
                {
                    idPaciente = Convert.ToInt32(lectorPa["id-Paciente"]),
                    nombre = lectorPa["nombre"].ToString()!,
                    plus = LeerBooleano(lectorPa["plus"]),
                    por_o_mensual = (ModalidadDePago)Enum.Parse(typeof(ModalidadDePago), lectorPa["por_o_mensual"].ToString()!, true),
                    obra_social = LeerBooleano(lectorPa["obra_social"]),
                    montoBase = LeerDecimal(lectorPa["montoBase"]),
                    montoPlus = LeerDecimal(lectorPa["montoPlus"]),
                    horario = new List<HorarioPaciente>()
                };
            }
        }

    // Si no se encontró el paciente en la base de datos, retornamos null inmediatamente
        if (paciente == null)
            return null;

    // 2. Cargar los horarios asignados al paciente
        string sqlHor = "SELECT * FROM HorarioPaciente WHERE \"id-Paciente\" = @idPaciente";
        using (var comandoHor = new SqliteCommand(sqlHor, conexion))
        {
            comandoHor.Parameters.AddWithValue("@idPaciente", paciente.idPaciente);
            using var lectorHor = comandoHor.ExecuteReader();

            while (lectorHor.Read())
            {
                var hor = new HorarioPaciente
                {
                    idHorario = Convert.ToInt32(lectorHor["id_horario"]),
                    idPaciente = paciente.idPaciente,
                    paciente = paciente, // Vinculamos la referencia en memoria
                    diaSemana = LeerDiaSemana(lectorHor["dia_semana"].ToString()!),
                    horaInicio = TimeSpan.Parse(lectorHor["hora_inicio"].ToString()!),
                    horaFin = TimeSpan.Parse(lectorHor["hora_fin"].ToString()!)
                };

            paciente.horario.Add(hor);
            }
        }

        return paciente;
    }
    public void EliminarPaciente(int id)
    {
        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();
        AsegurarTablaAsistenciaDiaria(conexion);

    // Iniciamos una transacción para asegurar que se borren todas las dependencias
        using var transaccion = conexion.BeginTransaction();

        try
        {
        // 1. Eliminar los registros de Asistencia asociados al paciente
            string sqlAsis = "DELETE FROM Asistencia WHERE \"id-Paciente\" = @id";
            using (var cmdAsis = new SqliteCommand(sqlAsis, conexion, transaccion))
            {
                cmdAsis.Parameters.AddWithValue("@id", id);
                cmdAsis.ExecuteNonQuery();
            }

            using (var cmdAsisDiaria = new SqliteCommand(
                "DELETE FROM AsistenciaDiaria WHERE idPaciente = @id", conexion, transaccion))
            {
                cmdAsisDiaria.Parameters.AddWithValue("@id", id);
                cmdAsisDiaria.ExecuteNonQuery();
            }

        // 2. Eliminar los Horarios asignados al paciente
            string sqlHor = "DELETE FROM HorarioPaciente WHERE \"id-Paciente\" = @id";
            using (var cmdHor = new SqliteCommand(sqlHor, conexion, transaccion))
            {
                cmdHor.Parameters.AddWithValue("@id", id);
                cmdHor.ExecuteNonQuery();
            }

        // 3. Eliminar al Paciente
            string sqlPa = "DELETE FROM Paciente WHERE \"id-Paciente\" = @id";
            using (var cmdPa = new SqliteCommand(sqlPa, conexion, transaccion))
            {
                cmdPa.Parameters.AddWithValue("@id", id);
                int filasAfectadas = cmdPa.ExecuteNonQuery();

                if (filasAfectadas == 0)
                {
                    throw new Exception($"No se encontró ningún paciente con el ID {id}.");
                }
            }

        // Confirmamos la eliminación total en la base de datos
            transaccion.Commit();
        }
        catch (Exception)
        {
        // Si falla algún paso, revertimos los cambios para no dejar datos huérfanos
            transaccion.Rollback();
            throw;
        }
    }

    public void ActualizarPaciente(Paciente paciente)
    {
        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();

        using var transaccion = conexion.BeginTransaction();

        try
        {
        // 1. Actualizar los datos principales del Paciente
            string sqlPa = @"
            UPDATE Paciente 
            SET nombre = @nombre, 
                plus = @plus, 
                por_o_mensual = @por_o_mensual, 
                obra_social = @obra_social,
                montoBase = @montoBase,
                montoPlus = @montoPlus
            WHERE ""id-Paciente"" = @id";

            using (var cmdPa = new SqliteCommand(sqlPa, conexion, transaccion))
            {
                cmdPa.Parameters.AddWithValue("@id", paciente.idPaciente);
                cmdPa.Parameters.AddWithValue("@nombre", paciente.nombre);
                cmdPa.Parameters.AddWithValue("@plus", paciente.plus);
                cmdPa.Parameters.AddWithValue("@por_o_mensual", paciente.por_o_mensual.ToString());
                cmdPa.Parameters.AddWithValue("@obra_social", paciente.obra_social);
                cmdPa.Parameters.AddWithValue("@montoBase", paciente.montoBase);
                cmdPa.Parameters.AddWithValue("@montoPlus", paciente.montoPlus);

                int filasAfectadas = cmdPa.ExecuteNonQuery();
                if (filasAfectadas == 0)
                {
                    throw new Exception($"No se encontró ningún paciente con el ID {paciente.idPaciente}.");
                }
            }

        // 2. Reemplazar sus horarios (Estrategia: Borrar previos e Insertar los nuevos)
            string sqlDelHor = "DELETE FROM HorarioPaciente WHERE \"id-Paciente\" = @idPaciente";
            using (var cmdDel = new SqliteCommand(sqlDelHor, conexion, transaccion))
            {
                cmdDel.Parameters.AddWithValue("@idPaciente", paciente.idPaciente);
                cmdDel.ExecuteNonQuery();
            }

            if (paciente.horario != null && paciente.horario.Count > 0)
            {
                string sqlHor = @"
                INSERT INTO HorarioPaciente (""id-Paciente"", dia_semana, hora_inicio, hora_fin) 
                VALUES (@idPaciente, @diaSemana, @horaInicio, @horaFin)";

                foreach (var horario in paciente.horario)
                {
                    using var cmdHor = new SqliteCommand(sqlHor, conexion, transaccion);
                    cmdHor.Parameters.AddWithValue("@idPaciente", paciente.idPaciente);
                    cmdHor.Parameters.AddWithValue("@diaSemana", horario.diaSemana.ToString());
                    cmdHor.Parameters.AddWithValue("@horaInicio", horario.horaInicio.ToString(@"hh\:mm"));
                    cmdHor.Parameters.AddWithValue("@horaFin", horario.horaFin.ToString(@"hh\:mm"));

                    cmdHor.ExecuteNonQuery();
                }
            }

            transaccion.Commit();
        }
        catch (Exception)
        {
            transaccion.Rollback();
            throw;
        }
    }

    public void RegistrarAsistencia(int idPaciente, DateTime fecha, bool presente)
    {
        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();
        AsegurarTablaAsistenciaDiaria(conexion);

        string fechaTxt = fecha.ToString("yyyy-MM-dd");
        int valor = presente ? 1 : 0;

        const string sql = @"
            INSERT INTO AsistenciaDiaria (idPaciente, fecha, presente)
            VALUES (@idPaciente, @fecha, @presente)
            ON CONFLICT (idPaciente, fecha)
            DO UPDATE SET presente = excluded.presente";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@idPaciente", idPaciente);
        comando.Parameters.AddWithValue("@fecha", fechaTxt);
        comando.Parameters.AddWithValue("@presente", valor);
        comando.ExecuteNonQuery();
    }

    public Dictionary<int, bool> ObtenerAsistenciaDelDia(DateTime fecha)
    {
        var resultado = new Dictionary<int, bool>();

        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();
        AsegurarTablaAsistenciaDiaria(conexion);

        const string sql = "SELECT idPaciente, presente FROM AsistenciaDiaria WHERE fecha = @fecha";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@fecha", fecha.ToString("yyyy-MM-dd"));

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            resultado[Convert.ToInt32(lector["idPaciente"])] = LeerBooleano(lector["presente"]);
        }

        return resultado;
    }

    public Dictionary<int, int> ContarAsistenciasPresentesDelMes(DateTime fecha)
    {
        var resultado = new Dictionary<int, int>();
        var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
        var inicioMesSiguiente = inicioMes.AddMonths(1);

        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();
        AsegurarTablaAsistenciaDiaria(conexion);

        const string sql = @"
            SELECT idPaciente, presente
            FROM AsistenciaDiaria
            WHERE fecha >= @inicioMes AND fecha < @inicioMesSiguiente";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@inicioMes", inicioMes.ToString("yyyy-MM-dd"));
        comando.Parameters.AddWithValue("@inicioMesSiguiente", inicioMesSiguiente.ToString("yyyy-MM-dd"));

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            if (!LeerBooleano(lector["presente"]))
                continue;

            var idPaciente = Convert.ToInt32(lector["idPaciente"]);
            resultado[idPaciente] = resultado.GetValueOrDefault(idPaciente) + 1;
        }

        return resultado;
    }

    public Dictionary<DateTime, Dictionary<int, int>> ContarAsistenciasPresentesPorMes(DateTime fechaLimite)
    {
        var resultado = new Dictionary<DateTime, Dictionary<int, int>>();
        var inicioMesLimite = new DateTime(fechaLimite.Year, fechaLimite.Month, 1);

        using var conexion = new SqliteConnection(connectionString);
        conexion.Open();
        AsegurarTablaAsistenciaDiaria(conexion);

        const string sql = @"
            SELECT substr(fecha, 1, 7) AS mes, idPaciente,
                   SUM(CASE
                       WHEN CAST(presente AS INTEGER) = 1 OR lower(presente) IN ('true', 'si', 'sí') THEN 1
                       ELSE 0
                   END) AS cantidadPresentes
            FROM AsistenciaDiaria
            WHERE fecha < @fechaLimite
            GROUP BY mes, idPaciente
            ORDER BY mes DESC";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@fechaLimite", inicioMesLimite.ToString("yyyy-MM-dd"));

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            var mesTexto = lector["mes"].ToString()!;
            var mes = DateTime.ParseExact(mesTexto, "yyyy-MM", CultureInfo.InvariantCulture);
            var idPaciente = Convert.ToInt32(lector["idPaciente"]);

            if (!resultado.TryGetValue(mes, out var asistenciasPorPaciente))
            {
                asistenciasPorPaciente = new Dictionary<int, int>();
                resultado.Add(mes, asistenciasPorPaciente);
            }

            asistenciasPorPaciente[idPaciente] = Convert.ToInt32(lector["cantidadPresentes"]);
        }

        return resultado;
    }

    private static void AsegurarTablaAsistenciaDiaria(SqliteConnection conexion)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS AsistenciaDiaria (
                idPaciente INTEGER NOT NULL,
                fecha TEXT NOT NULL,
                presente INTEGER NOT NULL,
                PRIMARY KEY (idPaciente, fecha)
            );

            INSERT OR IGNORE INTO AsistenciaDiaria (idPaciente, fecha, presente)
            SELECT ""id-Paciente"", fecha, presente
            FROM Asistencia;";
        using var comando = new SqliteCommand(sql, conexion);
        comando.ExecuteNonQuery();
    }

    private static bool LeerBooleano(object valor)
    {
        var texto = valor.ToString()?.Trim();
        if (bool.TryParse(texto, out var resultado))
            return resultado;

        return texto?.ToLowerInvariant() switch
        {
            "1" or "si" or "sí" => true,
            "0" or "no" => false,
            _ => throw new FormatException($"No se reconoce el valor booleano '{texto}'.")
        };
    }

    private static decimal LeerDecimal(object valor) =>
        valor == DBNull.Value ? 0m : Convert.ToDecimal(valor);

    private static DayOfWeek LeerDiaSemana(string valor)
    {
        if (Enum.TryParse<DayOfWeek>(valor, true, out var dia))
            return dia;

        return valor.Trim().ToLowerInvariant() switch
        {
            "domingo" => DayOfWeek.Sunday,
            "lunes" => DayOfWeek.Monday,
            "martes" => DayOfWeek.Tuesday,
            "miércoles" or "miercoles" => DayOfWeek.Wednesday,
            "jueves" => DayOfWeek.Thursday,
            "viernes" => DayOfWeek.Friday,
            "sábado" or "sabado" => DayOfWeek.Saturday,
            _ => throw new FormatException($"No se reconoce el día de la semana '{valor}'.")
        };
    }
}