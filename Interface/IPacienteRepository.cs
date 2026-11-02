using Models;
namespace interfaces;

public interface IPacienteRepository
{
    void CrearPaciente(Paciente paciente);

    List<Paciente> ListarPacientes();

    Paciente? GetPaciente(int id);

    void EliminarPaciente(int id);

    void ActualizarPaciente(Paciente paciente);
    public void RegistrarAsistencia(int idPaciente, DateTime fecha, bool presente);
    Dictionary<int, bool> ObtenerAsistenciaDelDia(DateTime fecha);
    Dictionary<int, int> ContarAsistenciasPresentesDelMes(DateTime fecha);
    Dictionary<DateTime, Dictionary<int, int>> ContarAsistenciasPresentesPorMes(DateTime fechaLimite);
}