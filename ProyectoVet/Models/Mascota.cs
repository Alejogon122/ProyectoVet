using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Mascota
{
    public int IdMascota { get; set; }

    public string NombreMascota { get; set; } = null!;

    public string ClaseAnimal { get; set; } = null!;

    public string? OtraClaseAnimal { get; set; }

    public string? Raza { get; set; }

    public string? Sexo { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string? Color { get; set; }

    public string? NumeroMicrochip { get; set; }

    public int IdDueno { get; set; }

    public bool Activa { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ExpedientesClinico? ExpedientesClinico { get; set; }

    public virtual Dueno IdDuenoNavigation { get; set; } = null!;

    public virtual ICollection<Vacunacione> Vacunaciones { get; set; } = new List<Vacunacione>();
}
