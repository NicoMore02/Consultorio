using System.ComponentModel.DataAnnotations;
using Models;

namespace ViewModels;

public class PacienteViewModel : IValidatableObject
{
    public int IdPaciente { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Plus")]
    public bool Plus { get; set; }

    [Display(Name = "Modalidad de pago")]
    public ModalidadDePago Modalidad { get; set; }

    [Display(Name = "Obra social")]
    public bool ObraSocial { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto no puede ser negativo")]
    [Display(Name = "Monto base")]
    public decimal MontoBase { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto no puede ser negativo")]
    [Display(Name = "Monto plus")]
    public decimal MontoPlus { get; set; }

    public List<HorarioViewModel> Horarios { get; set; } = new();

    // Validaciones que cruzan campos
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Plus && MontoPlus <= 0)
            yield return new ValidationResult(
                "Si el paciente tiene plus, el monto plus debe ser mayor a 0",
                new[] { nameof(MontoPlus) });

        if (!Enum.IsDefined(Modalidad))
            yield return new ValidationResult(
                "Seleccioná una modalidad de pago válida",
                new[] { nameof(Modalidad) });

        if (Horarios.Count == 0)
            yield return new ValidationResult(
                "Agregá al menos un día y horario",
                new[] { nameof(Horarios) });

        for (int i = 0; i < Horarios.Count; i++)
        {
            if (!Enum.IsDefined(Horarios[i].DiaSemana))
                yield return new ValidationResult(
                    "Seleccioná un día válido",
                    new[] { $"{nameof(Horarios)}[{i}].{nameof(HorarioViewModel.DiaSemana)}" });

            if (Horarios[i].HoraInicio < TimeSpan.Zero ||
                Horarios[i].HoraInicio >= TimeSpan.FromDays(1) ||
                Horarios[i].HoraFin <= TimeSpan.Zero ||
                Horarios[i].HoraFin >= TimeSpan.FromDays(1))
                yield return new ValidationResult(
                    "Las horas deben estar dentro del mismo día",
                    new[] { nameof(Horarios) });

            if (Horarios[i].HoraFin <= Horarios[i].HoraInicio)
                yield return new ValidationResult(
                    "La hora de fin debe ser posterior a la de inicio",
                    new[] { $"{nameof(Horarios)}[{i}].{nameof(HorarioViewModel.HoraFin)}" });

            for (int j = i + 1; j < Horarios.Count; j++)
            {
                if (Horarios[i].DiaSemana == Horarios[j].DiaSemana &&
                    Horarios[i].HoraInicio < Horarios[j].HoraFin &&
                    Horarios[j].HoraInicio < Horarios[i].HoraFin)
                {
                    yield return new ValidationResult(
                        "Los horarios del paciente no pueden superponerse entre sí",
                        new[] { nameof(Horarios) });
                    break;
                }
            }
        }
    }

    // Modelo -> ViewModel (para Editar / Detalle)
    public static PacienteViewModel FromModel(Paciente p) => new()
    {
        IdPaciente = p.idPaciente,
        Nombre = p.nombre ?? "",
        Plus = p.plus,
        Modalidad = p.por_o_mensual,
        ObraSocial = p.obra_social,
        MontoBase = p.montoBase,
        MontoPlus = p.montoPlus,
        Horarios = p.horario.Select(h => new HorarioViewModel
        {
            IdHorario = h.idHorario,
            DiaSemana = h.diaSemana,
            HoraInicio = h.horaInicio,
            HoraFin = h.horaFin
        }).ToList()
    };

    // ViewModel -> Modelo (para Crear / Actualizar)
    public Paciente ToModel()
    {
        var paciente = new Paciente
        {
            idPaciente = IdPaciente,
            nombre = Nombre,
            plus = Plus,
            por_o_mensual = Modalidad,
            obra_social = ObraSocial,
            montoBase = MontoBase,
            montoPlus = MontoPlus
        };

        paciente.horario = Horarios.Select(h => new HorarioPaciente
        {
            idHorario = h.IdHorario,
            idPaciente = IdPaciente,
            paciente = paciente,
            diaSemana = h.DiaSemana,
            horaInicio = h.HoraInicio,
            horaFin = h.HoraFin
        }).ToList();

        return paciente;
    }
}

public class HorarioViewModel
{
    public int IdHorario { get; set; }

    [Display(Name = "Día")]
    public DayOfWeek DiaSemana { get; set; }

    [Display(Name = "Hora de inicio")]
    [DataType(DataType.Time)]
    public TimeSpan HoraInicio { get; set; }

    [Display(Name = "Hora de fin")]
    [DataType(DataType.Time)]
    public TimeSpan HoraFin { get; set; }
}
