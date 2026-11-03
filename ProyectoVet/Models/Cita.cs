using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProyectoVet.Models;

public partial class Cita
{
    public int IdCita { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un paciente (mascota).")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un paciente válido.")]
    public int IdMascota { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un veterinario.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un veterinario válido.")]
    public int IdEmpleado { get; set; }

    [Required(ErrorMessage = "La fecha y hora de inicio es obligatoria.")]
    [DataType(DataType.DateTime)]
    public DateTime FechaHoraInicio { get; set; }

    [Required(ErrorMessage = "La fecha y hora estimada de finalización es obligatoria.")]
    [DataType(DataType.DateTime)]
    public DateTime FechaHoraFin { get; set; }

    [Required(ErrorMessage = "El estado de la cita es obligatorio.")]
    [StringLength(20, ErrorMessage = "El estado no puede exceder 20 caracteres.")]
    public string EstadoCita { get; set; } = "Pendiente";

    [Required(ErrorMessage = "El motivo de la cita es obligatorio.")]
    [StringLength(300, ErrorMessage = "El motivo no puede exceder 300 caracteres.")]
    public string? Motivo { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres.")]
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int? CreadaPorEmpleadoId { get; set; }

    [ValidateNever]
    public virtual Consulta? Consulta { get; set; }

    [ValidateNever]
    public virtual Empleado? CreadaPorEmpleado { get; set; }

    [ValidateNever]
    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    [ValidateNever]
    public virtual Mascota? IdMascotaNavigation { get; set; }
}
