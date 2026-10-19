using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Dueno
{
    public int IdDueno { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string NumeroCedula { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string? TelefonoOpcional { get; set; }

    public string CorreoElectronico { get; set; } = null!;

    public string Contrasena { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Mascota> Mascota { get; set; } = new List<Mascota>();
}
