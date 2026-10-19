using System;
using System.Collections.Generic;

namespace ProyectoVet.Models;

public partial class Tratamiento
{
    public int IdTratamiento { get; set; }

    public int IdConsulta { get; set; }

    public int? IdMedicamento { get; set; }

    public string NombreTratamiento { get; set; } = null!;

    public string? Dosis { get; set; }

    public string? Frecuencia { get; set; }

    public string? ViaAdministracion { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string? Instrucciones { get; set; }

    public string EstadoTratamiento { get; set; } = null!;

    public virtual Consulta IdConsultaNavigation { get; set; } = null!;

    public virtual Medicamento? IdMedicamentoNavigation { get; set; }
}
