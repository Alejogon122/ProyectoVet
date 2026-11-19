using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProyectoVet.Controllers;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Models.ViewModels;
using ProyectoVet.Security;
using ProyectoVet.Tests.Support;
using Xunit;

namespace ProyectoVet.Tests;

[Trait("Category", "Unit")]
public class HomeRoleTests
{
    private static async Task<VeterinariaContext> CreateContextAsync()
    {
        var db = new VeterinariaContext(new DbContextOptionsBuilder<VeterinariaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var role = new Role { NombreRol = RoleAccess.Veterinario };
        db.Empleados.AddRange(Enumerable.Range(1, 2).Select(id => new Empleado
        {
            IdEmpleado = id, Nombre = $"Empleado {id}", Apellidos = "Prueba", NumeroCedula = $"TEST{id}",
            Telefono = "88888888", CorreoElectronico = $"empleado{id}@test.local", Contrasena = "Prueba123!",
            Activo = true, IdRolNavigation = role
        }));
        var owner = new Dueno
        {
            Nombre = "Dueño", Apellidos = "Prueba", NumeroCedula = "TESTD", Telefono = "88888888",
            CorreoElectronico = "dueno@test.local", Contrasena = "Prueba", Activo = true
        };
        var pet = new Mascota { IdMascota = 1, NombreMascota = "Luna", ClaseAnimal = "Perro", Activa = true, IdDuenoNavigation = owner };
        db.Mascotas.Add(pet);
        var expediente = new ExpedientesClinico { IdExpediente = 1, IdMascotaNavigation = pet, FechaApertura = DateTime.Today };
        db.Consultas.Add(new Consulta
        {
            IdExpedienteNavigation = expediente, IdVeterinario = 1,
            FechaConsulta = DateTime.Today, MotivoConsulta = "Control"
        });
        // Desorden deliberado: el panel debe ordenar y limitar, sin citas pasadas ni canceladas.
        foreach (var day in Enumerable.Range(0, 10).Reverse())
            db.Citas.Add(Appointment(1, DateTime.Today.AddDays(day).AddHours(10)));
        db.Citas.Add(Appointment(2, DateTime.Today.AddHours(11)));
        db.Citas.Add(Appointment(1, DateTime.Today.AddDays(-1).AddHours(10)));
        var cancelled = Appointment(1, DateTime.Today.AddHours(9));
        cancelled.EstadoCita = "Cancelada";
        db.Citas.Add(cancelled);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return db;
    }

    private static Cita Appointment(int vet, DateTime start) => new()
    {
        IdEmpleado = vet, IdMascota = 1, Motivo = "Control", EstadoCita = "Pendiente",
        FechaHoraInicio = start, FechaHoraFin = start.AddMinutes(30)
    };

    [Theory]
    [InlineData("Administrador", null, 2, 1)]
    [InlineData("Veterinario", "Veterinario", 1, 1)]
    [InlineData("Secretaria", "Secretaria", 2, 0)]
    [InlineData("Recepcionista", "Secretaria", 2, 0)]
    public async Task Index_SeleccionaVistaYContadoresDelRol(string role, string? view, int appointments, int consultations)
    {
        await using var db = await CreateContextAsync();
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db), role);

        var result = Assert.IsType<ViewResult>(await test.Controller.Index());

        Assert.Equal(view, result.ViewName);
        var model = Assert.IsType<DashboardViewModel>(result.Model);
        Assert.Equal(appointments, model.TotalCitasHoy);
        Assert.Equal(consultations, model.TotalConsultas);
        Assert.Equal(1, model.TotalMascotas);
        Assert.Equal(1, model.TotalDuenos);
    }

    [Fact]
    public async Task Index_Veterinario_FiltraOrdenaYLimitaSuAgenda()
    {
        await using var db = await CreateContextAsync();
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db), RoleAccess.Veterinario);

        var result = Assert.IsType<ViewResult>(await test.Controller.Index());
        var agenda = Assert.IsType<DashboardViewModel>(result.Model).ProximasCitas;

        Assert.Equal(7, agenda.Count);
        Assert.All(agenda, c =>
        {
            Assert.Equal(1, c.IdEmpleado);
            Assert.NotEqual("Cancelada", c.EstadoCita);
            Assert.True(c.FechaHoraInicio >= DateTime.Today);
        });
        Assert.Equal(Enumerable.Range(0, 7).Select(day => DateTime.Today.AddDays(day).AddHours(10)),
            agenda.Select(c => c.FechaHoraInicio));
    }

    [Fact]
    public async Task Index_Anonimo_RedirigeAlLogin()
    {
        await using var db = await CreateContextAsync();
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db));
        var result = Assert.IsType<RedirectToActionResult>(await test.Controller.Index());
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Auth", result.ControllerName);
    }

    [Fact]
    public async Task Index_SinCitasAsignadas_DevuelveAgendaVacia()
    {
        await using var db = await CreateContextAsync();
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db), RoleAccess.Veterinario, employeeId: 99);
        var result = Assert.IsType<ViewResult>(await test.Controller.Index());
        var model = Assert.IsType<DashboardViewModel>(result.Model);
        Assert.Empty(model.ProximasCitas);
        Assert.Equal(0, model.TotalCitasHoy);
    }

    [Fact]
    public async Task Perfil_DevuelveLosDatosDelUsuarioAutenticado()
    {
        await using var db = await CreateContextAsync();
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db), RoleAccess.Veterinario, employeeId: 2);
        var result = Assert.IsType<ViewResult>(await test.Controller.Perfil());
        var model = Assert.IsType<PerfilViewModel>(result.Model);
        Assert.Equal("Empleado 2 Prueba", model.Nombre);
        Assert.Equal("empleado2@test.local", model.Correo);
        Assert.Equal(RoleAccess.Veterinario, model.Rol);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Perfil_EmpleadoInexistenteOInactivo_NoDevuelveDatos(bool inactive)
    {
        await using var db = await CreateContextAsync();
        if (inactive)
        {
            (await db.Empleados.FindAsync(2))!.Activo = false;
            await db.SaveChangesAsync();
        }
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db), RoleAccess.Veterinario, inactive ? 2 : 99);
        Assert.IsType<NotFoundResult>(await test.Controller.Perfil());
    }

    [Theory]
    [InlineData("")]
    [InlineData("identificador-invalido")]
    public async Task Perfil_SinIdentificadorValido_SolicitaAutenticacion(string id)
    {
        await using var db = await CreateContextAsync();
        using var test = new ControllerTestScope<HomeController>(new HomeController(NullLogger<HomeController>.Instance, db));
        test.Controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id)
        ], "Prueba"));
        Assert.IsType<ChallengeResult>(await test.Controller.Perfil());
    }
}
