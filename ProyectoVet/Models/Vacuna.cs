using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Vacuna
{
    public int IdVacuna { get; set; }

    public string NombreVacuna { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? EsquemaDosis { get; set; }

    public bool Activa { get; set; }

    public virtual ICollection<Vacunacione> Vacunaciones { get; set; } = new List<Vacunacione>();
}
