using System;
using System.Collections.Generic;
using ProyectoVet.Models;

namespace ProyectoVet.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalMascotas { get; set; }
        public int TotalDuenos { get; set; }
        public int TotalCitasHoy { get; set; }
        public int TotalConsultas { get; set; }
        public List<Cita> ProximasCitas { get; set; } = new List<Cita>();
    }
}
