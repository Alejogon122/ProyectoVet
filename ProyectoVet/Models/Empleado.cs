using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProyectoVet.Models;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string NumeroCedula { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string CorreoElectronico { get; set; } = null!;

    public string Contrasena { get; set; } = null!;

    public int IdRol { get; set; }

    public bool Activo { get; set; }

    public DateOnly? FechaContratacion { get; set; }

    public virtual ICollection<Cita> CitaCreadaPorEmpleados { get; set; } = new List<Cita>();

    public virtual ICollection<Cita> CitaIdEmpleadoNavigations { get; set; } = new List<Cita>();

    public virtual ICollection<Consulta> Consulta { get; set; } = new List<Consulta>();

    public virtual EmpleadosEspecialidade? EmpleadosEspecialidade { get; set; }

    [ValidateNever]
    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Vacunacione> Vacunaciones { get; set; } = new List<Vacunacione>();
}
