using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Vacunacione
{
    public int IdVacunacion { get; set; }

    public int IdMascota { get; set; }

    public int IdVacuna { get; set; }

    public int IdVeterinario { get; set; }

    public DateOnly FechaAplicacion { get; set; }

    public DateOnly? ProximaFecha { get; set; }

    public string? NumeroLote { get; set; }

    public string? Observaciones { get; set; }

    public virtual Mascota IdMascotaNavigation { get; set; } = null!;

    public virtual Vacuna IdVacunaNavigation { get; set; } = null!;

    public virtual Empleado IdVeterinarioNavigation { get; set; } = null!;
}
