namespace ViewModels;

public class ResumenMensualViewModel
{
    public decimal TotalAcumulado { get; set; }
    public List<ResumenMesViewModel> Meses { get; set; } = new();
}

public class ResumenMesViewModel
{
    public DateTime Mes { get; set; }
    public List<FilaCobroVM> Cobros { get; set; } = new();
    public decimal Total { get; set; }
}
