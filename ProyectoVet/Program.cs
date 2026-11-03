using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VeterinariaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionVeterinaria")
    ));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VeterinariaContext>();

    context.Database.EnsureCreated();

    string[] rolesPredeterminados = ["Administrador", "Veterinario", "Recepcionista"];
    foreach (var nombreRol in rolesPredeterminados)
    {
        if (!context.Roles.Any(r => r.NombreRol == nombreRol))
        {
            context.Roles.Add(new ProyectoVet.Models.Role { NombreRol = nombreRol });
        }
    }

    var adminRole = context.Roles.FirstOrDefault(r => r.NombreRol == "Administrador");
    if (adminRole != null)
    {
        var adminEmpleado = context.Empleados.FirstOrDefault(e =>
            e.Nombre.ToLower() == "correa" && e.Apellidos.ToLower() == "alejogon122"
            || e.CorreoElectronico.ToLower() == "alejogon122@vet.com"
            || (e.Nombre + " " + e.Apellidos).ToLower() == "correa alejogon122");

        if (adminEmpleado == null)
        {
            context.Empleados.Add(new ProyectoVet.Models.Empleado
            {
                Nombre = "Correa",
                Apellidos = "Alejogon122",
                NumeroCedula = "0000000000",
                Telefono = "0000000000",
                CorreoElectronico = "alejogon122@vet.com",
                Contrasena = "Admin123!",
                IdRol = adminRole.IdRol,
                Activo = true,
                FechaContratacion = DateOnly.FromDateTime(DateTime.Today)
            });
        }
        else
        {
            adminEmpleado.IdRol = adminRole.IdRol;
            adminEmpleado.Activo = true;
            if (string.IsNullOrWhiteSpace(adminEmpleado.Contrasena))
            {
                adminEmpleado.Contrasena = "Admin123!";
            }
        }
    }

    context.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
