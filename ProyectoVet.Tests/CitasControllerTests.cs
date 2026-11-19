using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Controllers;
using ProyectoVet.Data;
using ProyectoVet.Models;
using Xunit;

namespace ProyectoVet.Tests;

[Trait("Category", "Unit")]
public class CitasControllerTests
{
    private DbContextOptions<VeterinariaContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<VeterinariaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static CitasController CreateControllerWithTempData(VeterinariaContext context)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return new CitasController(context)
        {
            TempData = tempData
        };
    }

    private async Task<(Dueno dueno, Mascota mascota, Empleado vet)> SeedBaseDataAsync(VeterinariaContext context)
    {
        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Laura",
            Apellidos = "Jiménez",
            NumeroCedula = "301110222",
            Telefono = "88881234",
            CorreoElectronico = "laura@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);

        var mascota = new Mascota
        {
            IdMascota = 1,
            NombreMascota = "Max",
            ClaseAnimal = "Perro",
            IdDueno = dueno.IdDueno,
            Activa = true
        };
        context.Mascotas.Add(mascota);

        var vet = new Empleado
        {
            IdEmpleado = 3,
            Nombre = "María",
            Apellidos = "Vargas",
            NumeroCedula = "109870654",
            Telefono = "87654321",
            CorreoElectronico = "maria@vet.com",
            Contrasena = "vet123",
            IdRol = 3,
            Activo = true
        };
        context.Empleados.Add(vet);

        await context.SaveChangesAsync();
        return (dueno, mascota, vet);
    }

    private DateTime GetNextWeekday(DayOfWeek targetDay)
    {
        var date = DateTime.Today.AddDays(1);
        while (date.DayOfWeek != targetDay)
        {
            date = date.AddDays(1);
        }
        return date;
    }

    [Fact]
    public async Task Create_CitaValida_AgendaExitosamente()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascota, vet) = await SeedBaseDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        var fechaLunes = GetNextWeekday(DayOfWeek.Monday).AddHours(10); // Lunes a las 10:00 AM

        var nuevaCita = new Cita
        {
            IdMascota = mascota.IdMascota,
            IdEmpleado = vet.IdEmpleado,
            FechaHoraInicio = fechaLunes,
            FechaHoraFin = fechaLunes.AddMinutes(30),
            EstadoCita = "Pendiente",
            Motivo = "Vacunación anual",
            Observaciones = "Traer carnet"
        };

        var result = await controller.Create(nuevaCita);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var citaDb = await context.Citas.FirstOrDefaultAsync();
        Assert.NotNull(citaDb);
        Assert.Equal("Vacunación anual", citaDb.Motivo);
        Assert.Equal("Pendiente", citaDb.EstadoCita);
        Assert.Equal(mascota.IdMascota, citaDb.IdMascota);
        Assert.Equal(vet.IdEmpleado, citaDb.IdEmpleado);
    }

    [Fact]
    public async Task Create_CitaEnDomingo_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascota, vet) = await SeedBaseDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        var fechaDomingo = GetNextWeekday(DayOfWeek.Sunday).AddHours(11);

        var nuevaCita = new Cita
        {
            IdMascota = mascota.IdMascota,
            IdEmpleado = vet.IdEmpleado,
            FechaHoraInicio = fechaDomingo,
            FechaHoraFin = fechaDomingo.AddMinutes(30),
            EstadoCita = "Pendiente",
            Motivo = "Revisión general"
        };

        var result = await controller.Create(nuevaCita);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Cita.FechaHoraInicio)));
    }

    [Fact]
    public async Task Create_HorarioFueraDeAtencion_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascota, vet) = await SeedBaseDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        // Intento a las 7:00 AM (la apertura es 08:00 AM)
        var fechaLunes = GetNextWeekday(DayOfWeek.Monday).AddHours(7);

        var nuevaCita = new Cita
        {
            IdMascota = mascota.IdMascota,
            IdEmpleado = vet.IdEmpleado,
            FechaHoraInicio = fechaLunes,
            FechaHoraFin = fechaLunes.AddMinutes(30),
            EstadoCita = "Pendiente",
            Motivo = "Madrugada"
        };

        var result = await controller.Create(nuevaCita);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Cita.FechaHoraInicio)));
    }

    [Fact]
    public async Task Create_FechaFinAnteriorAInicio_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascota, vet) = await SeedBaseDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        var fechaLunes = GetNextWeekday(DayOfWeek.Monday).AddHours(10);

        var nuevaCita = new Cita
        {
            IdMascota = mascota.IdMascota,
            IdEmpleado = vet.IdEmpleado,
            FechaHoraInicio = fechaLunes,
            FechaHoraFin = fechaLunes.AddMinutes(-15), // Fin antes de inicio
            EstadoCita = "Pendiente",
            Motivo = "Inconsistente"
        };

        var result = await controller.Create(nuevaCita);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Cita.FechaHoraFin)));
    }

    [Fact]
    public async Task Create_SolapamientoHorarioVeterinario_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascota, vet) = await SeedBaseDataAsync(context);

        var fechaLunes = GetNextWeekday(DayOfWeek.Monday).AddHours(10);

        // Cita ya existente de 10:00 a 10:30
        var citaExistente = new Cita
        {
            IdCita = 10,
            IdMascota = mascota.IdMascota,
            IdEmpleado = vet.IdEmpleado,
            FechaHoraInicio = fechaLunes,
            FechaHoraFin = fechaLunes.AddMinutes(30),
            EstadoCita = "Confirmada",
            Motivo = "Existente",
            FechaCreacion = DateTime.Now
        };
        context.Citas.Add(citaExistente);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        // Otra mascota con el mismo veterinario de 10:15 a 10:45 (se solapa)
        var mascota2 = new Mascota
        {
            IdMascota = 2,
            NombreMascota = "Rocky",
            ClaseAnimal = "Perro",
            IdDueno = 1,
            Activa = true
        };
        context.Mascotas.Add(mascota2);
        await context.SaveChangesAsync();

        var nuevaCitaSolapada = new Cita
        {
            IdMascota = mascota2.IdMascota,
            IdEmpleado = vet.IdEmpleado,
            FechaHoraInicio = fechaLunes.AddMinutes(15),
            FechaHoraFin = fechaLunes.AddMinutes(45),
            EstadoCita = "Pendiente",
            Motivo = "Solapada"
        };

        var result = await controller.Create(nuevaCitaSolapada);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Cita.IdEmpleado)));
    }
}
