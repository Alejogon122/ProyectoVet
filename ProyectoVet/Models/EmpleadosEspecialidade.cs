using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class EmpleadosEspecialidade
{
    public int IdEmpleado { get; set; }

    public int IdEspecialidad { get; set; }

    public bool EsPrincipal { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Especialidade IdEspecialidadNavigation { get; set; } = null!;
}
