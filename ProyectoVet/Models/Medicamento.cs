using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Medicamento
{
    public int IdMedicamento { get; set; }

    public string NombreMedicamento { get; set; } = null!;

    public string? PrincipioActivo { get; set; }

    public string? Presentacion { get; set; }

    public string? Concentracion { get; set; }

    public bool Activo { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
