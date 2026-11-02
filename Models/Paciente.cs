namespace Models;
public enum ModalidadDePago
{
    PorSession,
    Mensual
}

public class Paciente
{
    public int idPaciente {get; set;}   
    public string? nombre {get; set;}
    public bool plus {get; set;}
    public ModalidadDePago por_o_mensual {get; set;}
    public bool obra_social {get; set;}
    public decimal montoBase {get; set;}
    public decimal montoPlus {get; set;}

    public List<HorarioPaciente> horario {get; set;} = new List<HorarioPaciente>();
    public List<Asistencia> asistencias {get; set;} = new List<Asistencia>();


    public decimal MontoTotal()
    {
        decimal total = montoBase;
        if (plus && obra_social)
        {
            return total += montoPlus;
        } else if (plus && !obra_social)
        {
            return total += montoPlus;
        } else if (!plus && obra_social)
        {
            return total;
        } else
        {
            return total;
        }
    }



}