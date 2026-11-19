using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProyectoVet.Controllers;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Security;
using Xunit;

namespace ProyectoVet.Tests;

// HTTP real: cookies, antiforgery, autorización, vistas Razor y persistencia aislada.
[Trait("Category", "Integration")]
public class RoleAccessHttpTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private string _url = "";

    public async Task InitializeAsync()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "ProyectoVet", "wwwroot"))) root = root.Parent;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HomeController).Assembly.FullName,
            EnvironmentName = "Development",
            ContentRootPath = root == null ? Directory.GetCurrentDirectory() : Path.Combine(root.FullName, "ProyectoVet")
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0));
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<VeterinariaContext>(o => o.UseInMemoryDatabase(database));
        builder.Services.AddRoleSecurity().AddApplicationPart(typeof(HomeController).Assembly);
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession();
        _app = builder.Build();
        _app.UseStaticFiles();
        _app.UseRouting();
        _app.UseSession();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllerRoute("default", "{controller=Auth}/{action=Login}/{id?}");
        await SeedAsync();
        await _app.StartAsync();
        _url = _app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private HttpClient CreateClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = new Uri(_url)
    };

    private async Task SeedAsync()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VeterinariaContext>();
        for (var i = 0; i < RoleAccess.Roles.Length; i++)
        {
            var role = new Role { IdRol = i + 1, NombreRol = RoleAccess.Roles[i] };
            db.Roles.Add(role);
            db.Empleados.Add(new Empleado
            {
                IdEmpleado = i + 1, IdRol = role.IdRol, Nombre = role.NombreRol, Apellidos = "Prueba",
                NumeroCedula = $"TEST{i}", Telefono = "88888888", CorreoElectronico = $"rol{i + 1}@test.local",
                Contrasena = "ClaveDePrueba123!", Activo = true
            });
        }
        var dueno = new Dueno
        {
            IdDueno = 1, Nombre = "Dueño", Apellidos = "Prueba", NumeroCedula = "TESTD",
            Telefono = "88888888", CorreoElectronico = "dueno@test.local", Contrasena = "Prueba", Activo = true
        };
        db.Duenos.Add(dueno);
        for (var i = 1; i <= 2; i++)
        {
            db.Mascotas.Add(new Mascota
            {
                IdMascota = i, NombreMascota = i == 1 ? "MiPaciente" : "OtroPaciente",
                ClaseAnimal = "Perro", IdDueno = 1, Activa = true
            });
            db.ExpedientesClinicos.Add(new ExpedientesClinico { IdExpediente = i, IdMascota = i, FechaApertura = DateTime.Today });
            db.Citas.Add(new Cita
            {
                IdCita = i, IdMascota = i, IdEmpleado = i == 1 ? 2 : 1,
                FechaHoraInicio = DateTime.Today.AddDays(1).AddHours(9),
                FechaHoraFin = DateTime.Today.AddDays(1).AddHours(10), EstadoCita = "Pendiente", Motivo = "Control"
            });
        }
        await db.SaveChangesAsync();
    }

    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Debe existir un token antiforgery en el formulario.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private async Task LoginAsync(HttpClient client, int employee)
    {
        var html = await client.GetStringAsync("/Auth/Login");
        var result = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["correoElectronico"] = $"rol{employee}@test.local", ["contrasena"] = "ClaveDePrueba123!",
            ["__RequestVerificationToken"] = Token(html)
        }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Contains("/Home", result.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Anonimo_NoPuedeVerNiModificarConsultas()
    {
        using var client = CreateClient();
        foreach (var path in new[] { "/Consultas/Index", "/Consultas/Create", "/Empleadoes/Index", "/Home/Index" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Auth/Login", response.Headers.Location!.OriginalString);
        }
        var post = await client.PostAsync("/Consultas/Create", new FormUrlEncodedContent([]));
        Assert.Contains("/Auth/Login", post.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData(1, "Panel de Control", true, true)]
    [InlineData(2, "Mi próxima agenda", true, false)]
    [InlineData(3, "Atención en recepción", false, false)]
    [InlineData(4, "Atención en recepción", false, false)]
    public async Task Login_MuestraPanelYMenuSegunRol(int employee, string title, bool clinical, bool admin)
    {
        using var client = CreateClient();
        await LoginAsync(client, employee);
        var home = WebUtility.HtmlDecode(await client.GetStringAsync("/Home/Index"));
        Assert.Contains(title, home);
        Assert.Equal(clinical, home.Contains("/Consultas"));
        Assert.Equal(admin, home.Contains("/Empleadoes"));
        if (employee == 2)
        {
            Assert.Contains("MiPaciente", home);
            Assert.DoesNotContain("OtroPaciente", home);
            Assert.DoesNotContain("/Citas/Create", home);
        }
        var perfil = WebUtility.HtmlDecode(await client.GetStringAsync("/Home/Perfil"));
        Assert.Contains($"rol{employee}@test.local", perfil);
        Assert.DoesNotContain("ClaveDePrueba", perfil);
    }

    [Theory]
    [InlineData(2, "/Empleadoes/Create")]
    [InlineData(2, "/Permisoes/Index")]
    [InlineData(2, "/Citas/Create")]
    [InlineData(2, "/Medicamentos/Edit/1")]
    [InlineData(2, "/Consultas/Delete/1")]
    [InlineData(3, "/Consultas/Index")]
    [InlineData(3, "/ExpedientesClinicoes/Details/1")]
    [InlineData(3, "/Empleadoes/Index")]
    [InlineData(4, "/Consultas/Create")]
    public async Task Rol_NoPuedeAbrirRutasRestringidas(int employee, string path)
    {
        using var client = CreateClient();
        await LoginAsync(client, employee);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/AccessDenied", response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(response.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task Secretaria_PostDirecto_NoPuedeCrearConsulta()
    {
        using var client = CreateClient();
        await LoginAsync(client, 3);
        var response = await client.PostAsync("/Consultas/Create", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/AccessDenied", response.Headers.Location!.OriginalString);
        using var scope = _app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<VeterinariaContext>().Consultas.ToListAsync());
    }

    [Fact]
    public async Task Veterinario_SoloVeAccionesPermitidasEnVistasCompartidas()
    {
        using var client = CreateClient();
        await LoginAsync(client, 2);
        foreach (var module in new[] { "Citas", "Dueno", "Mascotas", "Medicamentos" })
        {
            var html = await client.GetStringAsync($"/{module}/Index");
            foreach (var action in new[] { "Create", "Edit", "Delete" })
                Assert.DoesNotContain($"/{module}/{action}", html);
        }
        Assert.DoesNotContain("/Mascotas/Edit", await client.GetStringAsync("/Mascotas/Details/1"));
        var clinical = await client.GetStringAsync("/ExpedientesClinicoes/Index");
        Assert.Contains("/ExpedientesClinicoes/Create", clinical);
        Assert.DoesNotContain("/ExpedientesClinicoes/Delete", clinical);
    }

    [Fact]
    public async Task CuentaDesactivada_NoConservaAccesoConCookieExistente()
    {
        using var client = CreateClient();
        await LoginAsync(client, 2);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaContext>();
            (await db.Empleados.FindAsync(2))!.Activo = false;
            await db.SaveChangesAsync();
        }
        var response = await client.GetAsync("/Consultas/Index");
        Assert.Contains("/Auth/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task CambioDeRol_ActualizaPermisosEnSiguientePeticion()
    {
        using var client = CreateClient();
        await LoginAsync(client, 2);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaContext>();
            (await db.Empleados.FindAsync(2))!.IdRol = 3;
            await db.SaveChangesAsync();
        }
        var response = await client.GetAsync("/Consultas/Index");
        Assert.Contains("/Auth/AccessDenied", response.Headers.Location!.OriginalString);
        Assert.Contains("Atención en recepción", WebUtility.HtmlDecode(await client.GetStringAsync("/Home/Index")));
    }

    [Fact]
    public async Task Logout_CierraSesionYExigeAntiforgery()
    {
        using var client = CreateClient();
        await LoginAsync(client, 2);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync("/Auth/Logout", new FormUrlEncodedContent([]))).StatusCode);
        var home = await client.GetStringAsync("/Home/Index");
        var response = await client.PostAsync("/Auth/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(home)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", (await client.GetAsync("/Home/Index")).Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Administrador_PuedeCrearCuentaConRol(int role)
    {
        using var client = CreateClient();
        await LoginAsync(client, 1);
        var html = await client.GetStringAsync("/Empleadoes/Create");
        var response = await client.PostAsync("/Empleadoes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html), ["Nombre"] = "Nueva", ["Apellidos"] = "Cuenta",
            ["NumeroCedula"] = "999999999", ["Telefono"] = "88888888", ["CorreoElectronico"] = "nueva@test.local",
            ["Contrasena"] = "Prueba123!", ["IdRol"] = role.ToString(), ["Activo"] = "true"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = _app.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<VeterinariaContext>().Empleados.SingleAsync(e => e.CorreoElectronico == "nueva@test.local");
        Assert.Equal(role, account.IdRol);
        Assert.True(account.Activo);
    }

    [Fact]
    public async Task Administrador_PuedeCambiarRolSinCambiarContrasena()
    {
        using var client = CreateClient();
        await LoginAsync(client, 1);
        var html = await client.GetStringAsync("/Empleadoes/Edit/2");
        Assert.DoesNotContain("ClaveDePrueba123!", html);
        var response = await client.PostAsync("/Empleadoes/Edit/2", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html), ["IdEmpleado"] = "2",
            ["Nombre"] = "Veterinario", ["Apellidos"] = "Prueba", ["NumeroCedula"] = "TEST1",
            ["Telefono"] = "88888888", ["CorreoElectronico"] = "rol2@test.local",
            ["Contrasena"] = "", ["IdRol"] = "3", ["Activo"] = "true"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = _app.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<VeterinariaContext>().Empleados.FindAsync(2);
        Assert.Equal(3, account!.IdRol);
        Assert.Equal("ClaveDePrueba123!", account.Contrasena);
    }
}
