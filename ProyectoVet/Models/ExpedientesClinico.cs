using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class ExpedientesClinico
{
    public int IdExpediente { get; set; }

    public int IdMascota { get; set; }

    public DateTime FechaApertura { get; set; }

    public string? Alergias { get; set; }

    public string? Antecedentes { get; set; }

    public string? ObservacionesGenerales { get; set; }

    public virtual ICollection<Consulta> Consulta { get; set; } = new List<Consulta>();

    public virtual Mascota IdMascotaNavigation { get; set; } = null!;
}
