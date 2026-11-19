using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProyectoVet.Controllers;
using ProyectoVet.Data;
using ProyectoVet.Models;
using Xunit;

namespace ProyectoVet.Tests;

[Trait("Category", "Unit")]
public class ConsultasControllerTests
{
    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static VeterinariaContext CreateContext() => new(
        new DbContextOptionsBuilder<VeterinariaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ConsultasController CreateController(VeterinariaContext context, IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        return new ConsultasController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext, RouteData = new RouteData() },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider()),
            ObjectValidator = services.GetRequiredService<IObjectModelValidator>()
        };
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllersWithViews();
        return services.BuildServiceProvider();
    }

    private static Consulta FormularioValido() => new()
    {
        IdExpediente = 1,
        IdVeterinario = 2,
        FechaConsulta = new DateTime(2026, 11, 18, 10, 30, 0),
        MotivoConsulta = "Control general",
        PesoKg = 14.50m,
        TemperaturaC = 38.5m
    };

    [Theory]
    [InlineData(null)]
    [InlineData(3)]
    public async Task Create_FormularioSinObjetosDeNavegacion_GuardaConsulta(int? idCita)
    {
        await using var context = CreateContext();
        using var services = CreateServices();
        var controller = CreateController(context, services);
        var consulta = FormularioValido();
        consulta.IdCita = idCita;

        // Ejecutar la validación MVC real: llamar solo al controlador oculta el fallo.
        Assert.True(controller.TryValidateModel(consulta));
        var result = await controller.Create(consulta);

        Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
        context.ChangeTracker.Clear();
        var guardada = await context.Consultas.SingleAsync();
        Assert.Equal(idCita, guardada.IdCita);
        Assert.Equal(14.50m, guardada.PesoKg);
        Assert.Equal(38.5m, guardada.TemperaturaC);
        Assert.Equal("Control general", guardada.MotivoConsulta);
    }

    [Fact]
    public async Task Edit_FormularioSinObjetosDeNavegacion_GuardaCambios()
    {
        await using var context = CreateContext();
        var original = FormularioValido();
        context.Add(original);
        await context.SaveChangesAsync();
        var id = original.IdConsulta;
        context.ChangeTracker.Clear();
        using var services = CreateServices();
        var controller = CreateController(context, services);
        var consulta = FormularioValido();
        consulta.IdConsulta = id;
        consulta.Diagnostico = "Evolución favorable";

        Assert.True(controller.TryValidateModel(consulta));
        var result = await controller.Edit(id, consulta);

        Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
        context.ChangeTracker.Clear();
        Assert.Equal("Evolución favorable", (await context.Consultas.SingleAsync()).Diagnostico);
    }

    [Fact]
    public async Task Create_SinMotivo_NoGuardaYConservaErrorDeValidacion()
    {
        await using var context = CreateContext();
        using var services = CreateServices();
        var controller = CreateController(context, services);
        var consulta = FormularioValido();
        consulta.MotivoConsulta = null!;

        Assert.False(controller.TryValidateModel(consulta));
        var result = await controller.Create(consulta);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Consulta.MotivoConsulta)));
        Assert.Empty(await context.Consultas.ToListAsync());
    }
}
