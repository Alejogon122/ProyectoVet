using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProyectoVet.Models;

public partial class Mascota
{
    public int IdMascota { get; set; }

    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres.")]
    public string NombreMascota { get; set; } = null!;

    [Required(ErrorMessage = "Debe seleccionar la especie o clase de la mascota.")]
    [StringLength(20, ErrorMessage = "La especie no puede exceder 20 caracteres.")]
    public string ClaseAnimal { get; set; } = null!;

    [StringLength(50, ErrorMessage = "La especie alternativa no puede exceder 50 caracteres.")]
    public string? OtraClaseAnimal { get; set; }

    [StringLength(80, ErrorMessage = "La raza no puede exceder 80 caracteres.")]
    public string? Raza { get; set; }

    [StringLength(15, ErrorMessage = "El sexo no puede exceder 15 caracteres.")]
    public string? Sexo { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(50, ErrorMessage = "El color no puede exceder 50 caracteres.")]
    public string? Color { get; set; }

    [StringLength(50, ErrorMessage = "El número de microchip no puede exceder 50 caracteres.")]
    public string? NumeroMicrochip { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un dueño responsable.")]
    public int IdDueno { get; set; }

    public bool Activa { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres.")]
    public string? Observaciones { get; set; }

    [ValidateNever]
    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    [ValidateNever]
    public virtual ExpedientesClinico? ExpedientesClinico { get; set; }

    [ValidateNever]
    public virtual Dueno? IdDuenoNavigation { get; set; }

    [ValidateNever]
    public virtual ICollection<Vacunacione> Vacunaciones { get; set; } = new List<Vacunacione>();
}
