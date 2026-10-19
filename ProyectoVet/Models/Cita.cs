using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Cita
{
    public int IdCita { get; set; }

    public int IdMascota { get; set; }

    public int IdEmpleado { get; set; }

    public DateTime FechaHoraInicio { get; set; }

    public DateTime FechaHoraFin { get; set; }

    public string EstadoCita { get; set; } = null!;

    public string? Motivo { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int? CreadaPorEmpleadoId { get; set; }

    public virtual Consulta? Consulta { get; set; }

    public virtual Empleado? CreadaPorEmpleado { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Mascota IdMascotaNavigation { get; set; } = null!;
}
