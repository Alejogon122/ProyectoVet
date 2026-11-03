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

public class ExpedientesClinicoesControllerTests
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

    private static ExpedientesClinicoesController CreateControllerWithTempData(VeterinariaContext context)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return new ExpedientesClinicoesController(context)
        {
            TempData = tempData
        };
    }

    private async Task<(Dueno dueno, Mascota mascotaSinExpediente, Mascota mascotaConExpediente, ExpedientesClinico expedienteExistente)> SeedDataAsync(VeterinariaContext context)
    {
        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Daniela",
            Apellidos = "Castillo",
            NumeroCedula = "401230456",
            Telefono = "88334455",
            CorreoElectronico = "daniela@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);

        var mascota1 = new Mascota
        {
            IdMascota = 1,
            NombreMascota = "Toby",
            ClaseAnimal = "Perro",
            IdDueno = dueno.IdDueno,
            Activa = true
        };
        var mascota2 = new Mascota
        {
            IdMascota = 2,
            NombreMascota = "Misi",
            ClaseAnimal = "Gato",
            IdDueno = dueno.IdDueno,
            Activa = true
        };
        context.Mascotas.AddRange(mascota1, mascota2);

        var expedienteExistente = new ExpedientesClinico
        {
            IdExpediente = 1,
            IdMascota = mascota2.IdMascota,
            FechaApertura = DateTime.Now.AddDays(-30),
            Alergias = "Ninguna",
            Antecedentes = "Vacunas al día",
            ObservacionesGenerales = "Control inicial"
        };
        context.ExpedientesClinicos.Add(expedienteExistente);

        await context.SaveChangesAsync();
        return (dueno, mascota1, mascota2, expedienteExistente);
    }

    [Fact]
    public async Task Create_MascotaSinExpediente_AbreExpedienteExitosamente()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascotaSinExpediente, _, _) = await SeedDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        var nuevoExpediente = new ExpedientesClinico
        {
            IdMascota = mascotaSinExpediente.IdMascota,
            FechaApertura = DateTime.Now,
            Alergias = " Penicilina ",
            Antecedentes = " Fractura previa sanada ",
            ObservacionesGenerales = " Muy dócil "
        };

        var result = await controller.Create(nuevoExpediente);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var expDb = await context.ExpedientesClinicos.FirstOrDefaultAsync(e => e.IdMascota == mascotaSinExpediente.IdMascota);
        Assert.NotNull(expDb);
        Assert.Equal("Penicilina", expDb.Alergias);
        Assert.Equal("Fractura previa sanada", expDb.Antecedentes);
        Assert.Equal("Muy dócil", expDb.ObservacionesGenerales);
    }

    [Fact]
    public async Task Create_MascotaConExpedientePrevio_FallaValidacionPorUnicidad()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, _, mascotaConExpediente, _) = await SeedDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        var expedienteDuplicado = new ExpedientesClinico
        {
            IdMascota = mascotaConExpediente.IdMascota, // Ya tiene expediente #1
            FechaApertura = DateTime.Now,
            Alergias = "Ninguna"
        };

        var result = await controller.Create(expedienteDuplicado);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ExpedientesClinico.IdMascota)));
    }

    [Fact]
    public async Task Create_FechaAperturaFutura_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, mascotaSinExpediente, _, _) = await SeedDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        var nuevoExpediente = new ExpedientesClinico
        {
            IdMascota = mascotaSinExpediente.IdMascota,
            FechaApertura = DateTime.Now.AddDays(5) // Fecha futura
        };

        var result = await controller.Create(nuevoExpediente);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ExpedientesClinico.FechaApertura)));
    }

    [Fact]
    public async Task Edit_ActualizaExpedienteExistente_Exitosamente()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, _, _, expedienteExistente) = await SeedDataAsync(context);

        var controller = CreateControllerWithTempData(context);

        expedienteExistente.Alergias = "Pollo y granos";
        expedienteExistente.ObservacionesGenerales = "Requiere dieta hipoalergénica";

        var result = await controller.Edit(expedienteExistente.IdExpediente, expedienteExistente);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var expDb = await context.ExpedientesClinicos.FindAsync(expedienteExistente.IdExpediente);
        Assert.NotNull(expDb);
        Assert.Equal("Pollo y granos", expDb.Alergias);
    }

    [Fact]
    public async Task Edit_CambiarMascotaAOtraConExpediente_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);
        var (_, _, mascotaConExpediente, _) = await SeedDataAsync(context);

        // Crear otro expediente para la mascota sin expediente
        var otroExpediente = new ExpedientesClinico
        {
            IdExpediente = 2,
            IdMascota = 1,
            FechaApertura = DateTime.Now
        };
        context.ExpedientesClinicos.Add(otroExpediente);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        // Intentar cambiar expediente #2 a mascotaConExpediente (IdMascota = 2 que ya tiene expediente #1)
        otroExpediente.IdMascota = mascotaConExpediente.IdMascota;

        var result = await controller.Edit(otroExpediente.IdExpediente, otroExpediente);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ExpedientesClinico.IdMascota)));
    }
}
