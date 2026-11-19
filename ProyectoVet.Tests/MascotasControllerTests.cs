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
public class MascotasControllerTests
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

    private static MascotasController CreateControllerWithTempData(VeterinariaContext context)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return new MascotasController(context)
        {
            TempData = tempData
        };
    }

    [Fact]
    public async Task Create_Perro_RegistraCorrectamenteYSeteaOtraClaseAnimalNull()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);

        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Carlos",
            Apellidos = "Soto",
            NumeroCedula = "12345678",
            Telefono = "88888888",
            CorreoElectronico = "carlos@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        var nuevaMascota = new Mascota
        {
            NombreMascota = " Firulais ",
            ClaseAnimal = "Canino", // Debería normalizarse a "Perro"
            OtraClaseAnimal = "algo que debe limpiarse", // Debería quedar en null
            Raza = " Pastor Alemán ",
            Sexo = "Macho",
            IdDueno = dueno.IdDueno,
            Activa = true
        };

        var result = await controller.Create(nuevaMascota);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        var mascotaDb = await context.Mascotas.FirstOrDefaultAsync(m => m.NombreMascota == "Firulais");
        Assert.NotNull(mascotaDb);
        Assert.Equal("Perro", mascotaDb.ClaseAnimal);
        Assert.Null(mascotaDb.OtraClaseAnimal);
        Assert.Equal("Pastor Alemán", mascotaDb.Raza);
        Assert.Null(mascotaDb.NumeroMicrochip);
    }

    [Fact]
    public async Task Create_OtraEspecie_RegistraConEspecieAlternativa()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);

        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Maria",
            Apellidos = "Gomez",
            NumeroCedula = "87654321",
            Telefono = "77777777",
            CorreoElectronico = "maria@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        var nuevaMascota = new Mascota
        {
            NombreMascota = "Tambor",
            ClaseAnimal = "Otra",
            OtraClaseAnimal = " Conejo Enano ",
            IdDueno = dueno.IdDueno,
            Activa = true
        };

        var result = await controller.Create(nuevaMascota);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        var mascotaDb = await context.Mascotas.FirstOrDefaultAsync(m => m.NombreMascota == "Tambor");
        Assert.NotNull(mascotaDb);
        Assert.Equal("Otra", mascotaDb.ClaseAnimal);
        Assert.Equal("Conejo Enano", mascotaDb.OtraClaseAnimal);
    }

    [Fact]
    public async Task Create_OtraEspecieSinEspecificar_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);

        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Ana",
            Apellidos = "Rojas",
            NumeroCedula = "11223344",
            Telefono = "66666666",
            CorreoElectronico = "ana@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        var nuevaMascota = new Mascota
        {
            NombreMascota = "Bicho",
            ClaseAnimal = "Otra",
            OtraClaseAnimal = "   ", // Vacío
            IdDueno = dueno.IdDueno,
            Activa = true
        };

        var result = await controller.Create(nuevaMascota);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Mascota.OtraClaseAnimal)));
    }

    [Fact]
    public async Task Create_MicrochipDuplicado_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);

        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Luis",
            Apellidos = "Mora",
            NumeroCedula = "55443322",
            Telefono = "88990011",
            CorreoElectronico = "luis@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);

        var mascotaExistente = new Mascota
        {
            IdMascota = 10,
            NombreMascota = "Rex",
            ClaseAnimal = "Perro",
            NumeroMicrochip = "CHIP-999",
            IdDueno = dueno.IdDueno,
            Activa = true
        };
        context.Mascotas.Add(mascotaExistente);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        var nuevaMascota = new Mascota
        {
            NombreMascota = "Bobby",
            ClaseAnimal = "Perro",
            NumeroMicrochip = " CHIP-999 ", // Mismo microchip con espacios
            IdDueno = dueno.IdDueno,
            Activa = true
        };

        var result = await controller.Create(nuevaMascota);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Mascota.NumeroMicrochip)));
    }

    [Fact]
    public async Task Create_FechaNacimientoFutura_FallaValidacion()
    {
        var options = CreateInMemoryOptions();
        await using var context = new VeterinariaContext(options);

        var dueno = new Dueno
        {
            IdDueno = 1,
            Nombre = "Elena",
            Apellidos = "Vargas",
            NumeroCedula = "99887766",
            Telefono = "77112233",
            CorreoElectronico = "elena@test.com",
            Contrasena = "123456",
            Activo = true
        };
        context.Duenos.Add(dueno);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        var nuevaMascota = new Mascota
        {
            NombreMascota = "Copito",
            ClaseAnimal = "Gato",
            FechaNacimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), // Fecha futura
            IdDueno = dueno.IdDueno,
            Activa = true
        };

        var result = await controller.Create(nuevaMascota);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Mascota.FechaNacimiento)));
    }
}
