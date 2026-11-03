using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProyectoVet.Models;

public partial class ExpedientesClinico
{
    public int IdExpediente { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un paciente (mascota).")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un paciente válido.")]
    public int IdMascota { get; set; }

    [Required(ErrorMessage = "La fecha de apertura es obligatoria.")]
    [DataType(DataType.DateTime)]
    public DateTime FechaApertura { get; set; }

    [StringLength(1000, ErrorMessage = "Las alergias no pueden exceder 1000 caracteres.")]
    public string? Alergias { get; set; }

    [StringLength(2000, ErrorMessage = "Los antecedentes no pueden exceder 2000 caracteres.")]
    public string? Antecedentes { get; set; }

    [StringLength(2000, ErrorMessage = "Las observaciones no pueden exceder 2000 caracteres.")]
    public string? ObservacionesGenerales { get; set; }

    [ValidateNever]
    public virtual ICollection<Consulta> Consulta { get; set; } = new List<Consulta>();

    [ValidateNever]
    public virtual Mascota? IdMascotaNavigation { get; set; }
}
