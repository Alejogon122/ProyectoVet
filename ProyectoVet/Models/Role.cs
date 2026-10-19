using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Role
{
    public int IdRol { get; set; }

    public string NombreRol { get; set; } = null!;

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<Permiso> IdPermisos { get; set; } = new List<Permiso>();
}
