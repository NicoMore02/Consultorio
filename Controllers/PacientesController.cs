using interfaces;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Globalization;
using ViewModels;

namespace Controllers;

public class PacientesController : Controller
{
    private readonly IPacienteRepository _repo;

    public PacientesController(IPacienteRepository repo)
    {
        _repo = repo;
    }

    // Página principal: lista de alumnos + acumulado de cobros del mes
    public IActionResult Index(DateTime? fecha)
    {
        var dia = (fecha ?? DateTime.Today).Date;

        var pacientes = _repo.ListarPacientes();
        var asistencias = _repo.ObtenerAsistenciaDelDia(dia); // idPaciente -> presente
        var asistenciasDelMes = _repo.ContarAsistenciasPresentesDelMes(dia);

        var vm = new AsistenciaDiaViewModel { Fecha = dia };
        var cultura = CultureInfo.GetCultureInfo("es-AR");

        foreach (var p in pacientes)
        {
            bool? presente = asistencias.TryGetValue(p.idPaciente, out var v) ? v : null;

            vm.Pacientes.Add(new FilaPacienteVM
            {
                IdPaciente = p.idPaciente,
                Nombre = p.nombre ?? "",
                Horario = p.horario.Count == 0
                    ? "Sin horario asignado"
                    : string.Join(" · ", p.horario
                        .OrderBy(h => h.diaSemana == DayOfWeek.Sunday ? 7 : (int)h.diaSemana)
                        .Select(h => $"{cultura.DateTimeFormat.GetDayName(h.diaSemana)} {h.horaInicio:hh\\:mm}-{h.horaFin:hh\\:mm}")),
                Presente = presente
            });

        }

        vm.Cobros.AddRange(CalcularCobros(pacientes, asistenciasDelMes));
        vm.Total = vm.Cobros.Sum(c => c.Monto);
        return View(vm);
    }

    [HttpGet]
    public IActionResult Resumen()
    {
        var mesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var pacientes = _repo.ListarPacientes();
        var asistenciasPorMes = _repo.ContarAsistenciasPresentesPorMes(mesActual);
        var vm = new ResumenMensualViewModel();

        foreach (var (mes, asistencias) in asistenciasPorMes.OrderByDescending(x => x.Key))
        {
            var cobros = CalcularCobros(pacientes, asistencias);
            var total = cobros.Sum(c => c.Monto);

            vm.Meses.Add(new ResumenMesViewModel
            {
                Mes = mes,
                Cobros = cobros,
                Total = total
            });
        }

        vm.TotalAcumulado = vm.Meses.Sum(m => m.Total);
        return View(vm);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ConfigurarFormulario("Crear", "Agregar paciente", "Guardar paciente");
        return View(new PacienteViewModel
        {
            Horarios =
            [
                new HorarioViewModel
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(10, 0, 0)
                }
            ]
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(PacienteViewModel vm)
    {
        if (ModelState.IsValid)
            ValidarConflictosHorarios(vm);

        if (!ModelState.IsValid)
        {
            ConfigurarFormulario("Crear", "Agregar paciente", "Guardar paciente");
            return View(vm);
        }

        _repo.CrearPaciente(vm.ToModel());
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Actualizar(int? id)
    {
        if (!id.HasValue)
            return View("SeleccionarActualizacion", _repo.ListarPacientes());

        var paciente = _repo.GetPaciente(id.Value);
        if (paciente == null)
            return NotFound();

        ConfigurarFormulario("Actualizar", "Actualizar paciente", "Guardar cambios");
        return View("Crear", PacienteViewModel.FromModel(paciente));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Actualizar(int id, PacienteViewModel vm)
    {
        if (id <= 0)
            return BadRequest();

        if (_repo.GetPaciente(id) == null)
            return NotFound();

        vm.IdPaciente = id;
        if (ModelState.IsValid)
            ValidarConflictosHorarios(vm, id);

        if (!ModelState.IsValid)
        {
            ConfigurarFormulario("Actualizar", "Actualizar paciente", "Guardar cambios");
            return View("Crear", vm);
        }

        _repo.ActualizarPaciente(vm.ToModel());
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Eliminar()
    {
        return View(_repo.ListarPacientes());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        if (id <= 0)
            return BadRequest();

        if (_repo.GetPaciente(id) == null)
            return NotFound();

        _repo.EliminarPaciente(id);
        TempData["Mensaje"] = "El paciente se eliminó correctamente.";
        return RedirectToAction(nameof(Eliminar));
    }

    // Botones Sí (1) / No (0)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegistrarAsistencia(int idPaciente, int presente, DateTime fecha)
    {
        _repo.RegistrarAsistencia(idPaciente, fecha, presente == 1);
        return RedirectToAction(nameof(Index), new { fecha = fecha.ToString("yyyy-MM-dd") });
    }

    private void ValidarConflictosHorarios(PacienteViewModel vm, int? excluirPacienteId = null)
    {
        var horariosExistentes = _repo.ListarPacientes()
            .Where(p => p.idPaciente != excluirPacienteId)
            .SelectMany(p => p.horario.Select(h => new
            {
                Nombre = p.nombre ?? "Otro paciente",
                Horario = h
            }))
            .ToList();

        foreach (var horario in vm.Horarios)
        {
            var conflicto = horariosExistentes.FirstOrDefault(e =>
                e.Horario.diaSemana == horario.DiaSemana &&
                horario.HoraInicio < e.Horario.horaFin &&
                e.Horario.horaInicio < horario.HoraFin);

            if (conflicto == null)
                continue;

            var cultura = CultureInfo.GetCultureInfo("es-AR");
            var dia = cultura.DateTimeFormat.GetDayName(horario.DiaSemana);
            ModelState.AddModelError(
                nameof(vm.Horarios),
                $"El horario del {dia} de {horario.HoraInicio:hh\\:mm} a {horario.HoraFin:hh\\:mm} se superpone con el de {conflicto.Nombre} ({conflicto.Horario.horaInicio:hh\\:mm}-{conflicto.Horario.horaFin:hh\\:mm}).");
        }
    }

    private void ConfigurarFormulario(string action, string title, string submitText)
    {
        ViewData["FormAction"] = action;
        ViewData["Title"] = title;
        ViewData["SubmitText"] = submitText;
    }

    private static List<FilaCobroVM> CalcularCobros(
        IEnumerable<Paciente> pacientes,
        IReadOnlyDictionary<int, int> asistenciasDelMes)
    {
        var cobros = new List<FilaCobroVM>();

        foreach (var paciente in pacientes)
        {
            asistenciasDelMes.TryGetValue(paciente.idPaciente, out var cantidadSesiones);
            var sesionesPlus = paciente.plus ? cantidadSesiones : 0;

            if (paciente.por_o_mensual == ModalidadDePago.PorSession && cantidadSesiones > 0)
            {
                cobros.Add(new FilaCobroVM
                {
                    Nombre = paciente.nombre ?? "",
                    Concepto = $"{cantidadSesiones} {(cantidadSesiones == 1 ? "sesión" : "sesiones")}",
                    Monto = paciente.MontoTotal() * cantidadSesiones
                });
            }

            if (paciente.por_o_mensual == ModalidadDePago.Mensual)
            {
                cobros.Add(new FilaCobroVM
                {
                    Nombre = paciente.nombre ?? "",
                    Concepto = sesionesPlus == 0
                        ? "Mensual"
                        : $"Mensual + {sesionesPlus} {(sesionesPlus == 1 ? "sesión" : "sesiones")} de plus",
                    Monto = paciente.montoBase + paciente.montoPlus * sesionesPlus
                });
            }
        }

        return cobros;
    }
}
