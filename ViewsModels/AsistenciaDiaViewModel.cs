namespace ViewModels;

public class AsistenciaDiaViewModel
{
    public DateTime Fecha { get; set; }
    public List<FilaPacienteVM> Pacientes { get; set; } = new();
    public List<FilaCobroVM> Cobros { get; set; } = new();
    public decimal Total { get; set; }
}

public class FilaPacienteVM
{
    public int IdPaciente { get; set; }
    public string Nombre { get; set; } = "";
    public string Horario { get; set; } = ""; // <- Nueva propiedad añadida
    public bool? Presente { get; set; } // null = todavía sin marcar
}

public class FilaCobroVM
{
    public string Nombre { get; set; } = "";
    public string Concepto { get; set; } = "";
    public decimal Monto { get; set; }
}
