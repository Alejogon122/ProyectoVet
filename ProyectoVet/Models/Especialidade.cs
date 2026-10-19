using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Especialidade
{
    public int IdEspecialidad { get; set; }

    public string NombreEspecialidad { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<EmpleadosEspecialidade> EmpleadosEspecialidades { get; set; } = new List<EmpleadosEspecialidade>();
}
