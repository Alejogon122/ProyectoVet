using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class VwAgendaCita
{
    public int IdCita { get; set; }

    public DateTime FechaHoraInicio { get; set; }

    public DateTime FechaHoraFin { get; set; }

    public string EstadoCita { get; set; } = null!;

    public string? Motivo { get; set; }

    public int IdMascota { get; set; }

    public string NombreMascota { get; set; } = null!;

    public int IdDueno { get; set; }

    public string NombreDueno { get; set; } = null!;

    public int IdEmpleado { get; set; }

    public string NombreVeterinario { get; set; } = null!;
}
