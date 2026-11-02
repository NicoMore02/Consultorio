namespace Models;
public class Asistencia
{
    public int idAsistencia {get; set;}
    public int idPaciente {get; set;}
    public DateTime fecha {get; set;}
    public Paciente paciente {get; set;}
    public bool presente {get; set;}

    public decimal MontoAsistencia()
    {
        decimal total = 0;
        if (presente)
        {
            return total = paciente.MontoTotal();
        }
        return total;
    }
}