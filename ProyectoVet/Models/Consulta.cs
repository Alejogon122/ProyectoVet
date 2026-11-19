using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProyectoVet.Models;

public partial class Consulta
{
    public int IdConsulta { get; set; }

    public int IdExpediente { get; set; }

    public int? IdCita { get; set; }

    public int IdVeterinario { get; set; }

    public DateTime FechaConsulta { get; set; }

    public string MotivoConsulta { get; set; } = null!;

    public string? Sintomas { get; set; }

    public string? Diagnostico { get; set; }

    public string? TratamientoIndicado { get; set; }

    public decimal? PesoKg { get; set; }

    public decimal? TemperaturaC { get; set; }

    public string? Observaciones { get; set; }

    // El formulario envía las claves foráneas; EF carga las relaciones por separado.
    [ValidateNever]
    public virtual Cita? IdCitaNavigation { get; set; }

    [ValidateNever]
    public virtual ExpedientesClinico IdExpedienteNavigation { get; set; } = null!;

    [ValidateNever]
    public virtual Empleado IdVeterinarioNavigation { get; set; } = null!;

    [ValidateNever]
    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
