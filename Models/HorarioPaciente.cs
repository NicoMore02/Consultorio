namespace Models;
public class HorarioPaciente
{
    public int idHorario {get; set;}
    public int idPaciente {get; set;}
    public Paciente paciente {get; set;} = null!;
    public DayOfWeek diaSemana {get; set;}
    public TimeSpan horaInicio {get; set;}
    public TimeSpan horaFin {get; set;}
}