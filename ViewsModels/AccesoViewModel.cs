using System.ComponentModel.DataAnnotations;

namespace ViewModels;

public class AccesoViewModel
{
    [Required(ErrorMessage = "Ingresá la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
