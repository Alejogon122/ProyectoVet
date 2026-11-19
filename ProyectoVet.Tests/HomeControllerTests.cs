using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProyectoVet.Controllers;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Models.ViewModels;
using Xunit;
using ProyectoVet.Security;
using ProyectoVet.Tests.Support;

namespace ProyectoVet.Tests;

[Trait("Category", "Unit")]
public class HomeControllerTests
{
    [Fact]
    public async Task Index_RetornaDashboardConDatosCorrectos()
    {
        var options = new DbContextOptionsBuilder<VeterinariaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new VeterinariaContext(options);

        var duenoActivo1 = new Dueno
        {
            Nombre = "Ana",
            Apellidos = "García",
            NumeroCedula = "1111111",
            Telefono = "5551234567",
            CorreoElectronico = "ana@correo.com",
            Contrasena = "pass123",
            FechaRegistro = DateTime.Now.AddDays(-10),
            Activo = true
        };

        var duenoActivo2 = new Dueno
        {
            Nombre = "Luis",
            Apellidos = "Pérez",
            NumeroCedula = "2222222",
            Telefono = "5557654321",
            CorreoElectronico = "luis@correo.com",
            Contrasena = "pass123",
            FechaRegistro = DateTime.Now.AddDays(-20),
            Activo = true
        };

        var duenoInactivo = new Dueno
        {
            Nombre = "Pedro",
            Apellidos = "Soto",
            NumeroCedula = "3333333",
            Telefono = "5551111111",
            CorreoElectronico = "pedro@correo.com",
            Contrasena = "pass123",
            FechaRegistro = DateTime.Now.AddDays(-30),
            Activo = false
        };

        context.Duenos.AddRange(duenoActivo1, duenoActivo2, duenoInactivo);
        await context.SaveChangesAsync();

        var role = new Role { NombreRol = "Veterinario" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var empleado = new Empleado
        {
            Nombre = "Dr. José",
            Apellidos = "Ramírez",
            NumeroCedula = "987654321",
            Telefono = "5550001234",
            CorreoElectronico = "empleado@vet.com",
            Contrasena = "pass123",
            IdRol = role.IdRol,
            Activo = true,
            FechaContratacion = DateOnly.FromDateTime(DateTime.Today.AddYears(-2))
        };

        context.Empleados.Add(empleado);
        await context.SaveChangesAsync();

        var mascotaActiva1 = new Mascota
        {
            NombreMascota = "Max",
            ClaseAnimal = "Perro",
            IdDueno = duenoActivo1.IdDueno,
            Activa = true,
            IdDuenoNavigation = duenoActivo1
        };

        var mascotaActiva2 = new Mascota
        {
            NombreMascota = "Milo",
            ClaseAnimal = "Gato",
            IdDueno = duenoActivo2.IdDueno,
            Activa = true,
            IdDuenoNavigation = duenoActivo2
        };

        var mascotaInactiva = new Mascota
        {
            NombreMascota = "Coco",
            ClaseAnimal = "Conejo",
            IdDueno = duenoInactivo.IdDueno,
            Activa = false,
            IdDuenoNavigation = duenoInactivo
        };

        context.Mascotas.AddRange(mascotaActiva1, mascotaActiva2, mascotaInactiva);
        await context.SaveChangesAsync();

        var hoy = DateTime.Today;
        var mañana = hoy.AddDays(1);
        var haceDosDias = hoy.AddDays(-2);

        context.Citas.AddRange(
            new Cita
            {
                IdMascota = mascotaActiva1.IdMascota,
                IdEmpleado = empleado.IdEmpleado,
                FechaHoraInicio = hoy.AddHours(9),
                FechaHoraFin = hoy.AddHours(10),
                EstadoCita = "Pendiente",
                Motivo = "Control general",
                FechaCreacion = DateTime.Now,
                IdMascotaNavigation = mascotaActiva1,
                IdEmpleadoNavigation = empleado
            },
            new Cita
            {
                IdMascota = mascotaActiva2.IdMascota,
                IdEmpleado = empleado.IdEmpleado,
                FechaHoraInicio = mañana.AddHours(11),
                FechaHoraFin = mañana.AddHours(12),
                EstadoCita = "Pendiente",
                Motivo = "Vacunación",
                FechaCreacion = DateTime.Now,
                IdMascotaNavigation = mascotaActiva2,
                IdEmpleadoNavigation = empleado
            },
            new Cita
            {
                IdMascota = mascotaActiva1.IdMascota,
                IdEmpleado = empleado.IdEmpleado,
                FechaHoraInicio = haceDosDias,
                FechaHoraFin = haceDosDias.AddHours(1),
                EstadoCita = "Cancelada",
                Motivo = "Cancelada por cliente",
                FechaCreacion = DateTime.Now,
                IdMascotaNavigation = mascotaActiva1,
                IdEmpleadoNavigation = empleado
            }
        );

        var expediente = new ExpedientesClinico
        {
            IdMascota = mascotaActiva1.IdMascota,
            FechaApertura = DateTime.Today.AddDays(-30),
            IdMascotaNavigation = mascotaActiva1
        };

        context.ExpedientesClinicos.Add(expediente);
        await context.SaveChangesAsync();

        context.Consultas.Add(new Consulta
        {
            IdExpediente = expediente.IdExpediente,
            IdVeterinario = empleado.IdEmpleado,
            FechaConsulta = DateTime.Today,
            MotivoConsulta = "Chequeo anual",
            Diagnostico = "Todo en orden",
            IdExpedienteNavigation = expediente,
            IdVeterinarioNavigation = empleado
        });

        await context.SaveChangesAsync();

        using var homeTest = new ControllerTestScope<HomeController>(
            new HomeController(NullLogger<HomeController>.Instance, context), RoleAccess.Administrador);
        var controller = homeTest.Controller;
        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

        Assert.Equal(2, model.TotalMascotas);
        Assert.Equal(2, model.TotalDuenos);
        Assert.Equal(1, model.TotalCitasHoy);
        Assert.Equal(1, model.TotalConsultas);
        Assert.Equal(2, model.ProximasCitas.Count);
        Assert.All(model.ProximasCitas, cita => Assert.NotEqual("Cancelada", cita.EstadoCita));
    }

    [Fact]
    public async Task TodosLosControladores_DevuelvenVistasYModelosCorrectos()
    {
        await using var context = CreateSeededContext();

        var dueno = context.Duenos.First();
        var empleado = context.Empleados.First();
        var mascota = context.Mascotas.First();
        var expediente = context.ExpedientesClinicos.First();
        var cita = context.Citas.First();
        var consulta = context.Consultas.First();
        var especialidad = context.Especialidades.First();
        var medicamento = context.Medicamentos.First();
        var permiso = context.Permisos.First();

        using var homeTest = new ControllerTestScope<HomeController>(
            new HomeController(NullLogger<HomeController>.Instance, context), RoleAccess.Administrador);
        var homeController = homeTest.Controller;
        Assert.IsType<ViewResult>(await homeController.Index());
        Assert.IsType<ViewResult>(homeController.Privacy());

        await AssertControllerViewAsync(new DuenoController(context), dueno.IdDueno, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new MascotasController(context), mascota.IdMascota, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new EmpleadoesController(context), empleado.IdEmpleado, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new EspecialidadesController(context), especialidad.IdEspecialidad, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new MedicamentosController(context), medicamento.IdMedicamento, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new PermisoesController(context), permiso.IdPermiso, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new ExpedientesClinicoesController(context), expediente.IdExpediente, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new CitasController(context), cita.IdCita, "Index", "Details", "Create", "Edit", "Delete");
        await AssertControllerViewAsync(new ConsultasController(context), consulta.IdConsulta, "Index", "Details", "Create", "Edit", "Delete");
    }

    [Fact]
    public async Task Crud_Post_ConValidacionesYPersistencia_EnTodosLosControladores()
    {
        await using var context = CreateSeededContext();

        await AssertDuenoCrudAsync(context);
        await AssertMascotaCrudAsync(context);
        await AssertEmpleadoCrudAsync(context);
        await AssertEspecialidadCrudAsync(context);
        await AssertMedicamentoCrudAsync(context);
        await AssertPermisoCrudAsync(context);
        await AssertExpedienteCrudAsync(context);
        await AssertCitaCrudAsync(context);
        await AssertConsultaCrudAsync(context);
    }

    private static async Task AssertControllerViewAsync(object controller, int id, params string[] actionNames)
    {
        var type = controller.GetType();

        foreach (var actionName in actionNames)
        {
            object result = actionName switch
            {
                "Index" => type.Name switch
                {
                    nameof(DuenoController) => await ((DuenoController)controller).Index(),
                    nameof(MascotasController) => await ((MascotasController)controller).Index(),
                    nameof(EmpleadoesController) => await ((EmpleadoesController)controller).Index(),
                    nameof(EspecialidadesController) => await ((EspecialidadesController)controller).Index(),
                    nameof(MedicamentosController) => await ((MedicamentosController)controller).Index(),
                    nameof(PermisoesController) => await ((PermisoesController)controller).Index(),
                    nameof(ExpedientesClinicoesController) => await ((ExpedientesClinicoesController)controller).Index(),
                    nameof(CitasController) => await ((CitasController)controller).Index(),
                    nameof(ConsultasController) => await ((ConsultasController)controller).Index(),
                    _ => throw new InvalidOperationException($"Controller no soportado: {type.Name}")
                },
                "Details" => type.Name switch
                {
                    nameof(DuenoController) => await ((DuenoController)controller).Details(id),
                    nameof(MascotasController) => await ((MascotasController)controller).Details(id),
                    nameof(EmpleadoesController) => await ((EmpleadoesController)controller).Details(id),
                    nameof(EspecialidadesController) => await ((EspecialidadesController)controller).Details(id),
                    nameof(MedicamentosController) => await ((MedicamentosController)controller).Details(id),
                    nameof(PermisoesController) => await ((PermisoesController)controller).Details(id),
                    nameof(ExpedientesClinicoesController) => await ((ExpedientesClinicoesController)controller).Details(id),
                    nameof(CitasController) => await ((CitasController)controller).Details(id),
                    nameof(ConsultasController) => await ((ConsultasController)controller).Details(id),
                    _ => throw new InvalidOperationException($"Controller no soportado: {type.Name}")
                },
                "Create" => type.Name switch
                {
                    nameof(DuenoController) => ((DuenoController)controller).Create(),
                    nameof(MascotasController) => ((MascotasController)controller).Create(),
                    nameof(EmpleadoesController) => ((EmpleadoesController)controller).Create(),
                    nameof(EspecialidadesController) => ((EspecialidadesController)controller).Create(),
                    nameof(MedicamentosController) => ((MedicamentosController)controller).Create(),
                    nameof(PermisoesController) => ((PermisoesController)controller).Create(),
                    nameof(ExpedientesClinicoesController) => ((ExpedientesClinicoesController)controller).Create(),
                    nameof(CitasController) => ((CitasController)controller).Create(),
                    nameof(ConsultasController) => ((ConsultasController)controller).Create(),
                    _ => throw new InvalidOperationException($"Controller no soportado: {type.Name}")
                },
                "Edit" => type.Name switch
                {
                    nameof(DuenoController) => await ((DuenoController)controller).Edit(id),
                    nameof(MascotasController) => await ((MascotasController)controller).Edit(id),
                    nameof(EmpleadoesController) => await ((EmpleadoesController)controller).Edit(id),
                    nameof(EspecialidadesController) => await ((EspecialidadesController)controller).Edit(id),
                    nameof(MedicamentosController) => await ((MedicamentosController)controller).Edit(id),
                    nameof(PermisoesController) => await ((PermisoesController)controller).Edit(id),
                    nameof(ExpedientesClinicoesController) => await ((ExpedientesClinicoesController)controller).Edit(id),
                    nameof(CitasController) => await ((CitasController)controller).Edit(id),
                    nameof(ConsultasController) => await ((ConsultasController)controller).Edit(id),
                    _ => throw new InvalidOperationException($"Controller no soportado: {type.Name}")
                },
                "Delete" => type.Name switch
                {
                    nameof(DuenoController) => await ((DuenoController)controller).Delete(id),
                    nameof(MascotasController) => await ((MascotasController)controller).Delete(id),
                    nameof(EmpleadoesController) => await ((EmpleadoesController)controller).Delete(id),
                    nameof(EspecialidadesController) => await ((EspecialidadesController)controller).Delete(id),
                    nameof(MedicamentosController) => await ((MedicamentosController)controller).Delete(id),
                    nameof(PermisoesController) => await ((PermisoesController)controller).Delete(id),
                    nameof(ExpedientesClinicoesController) => await ((ExpedientesClinicoesController)controller).Delete(id),
                    nameof(CitasController) => await ((CitasController)controller).Delete(id),
                    nameof(ConsultasController) => await ((ConsultasController)controller).Delete(id),
                    _ => throw new InvalidOperationException($"Controller no soportado: {type.Name}")
                },
                _ => throw new InvalidOperationException($"Acción no soportada: {actionName}")
            };

            if (result is Task<IActionResult> taskResult)
            {
                result = await taskResult;
            }

            var viewResult = Assert.IsType<ViewResult>(result);
            if (actionName is "Details" or "Edit" or "Delete")
            {
                Assert.NotNull(viewResult.Model);
            }
        }
    }

    private static void ConfigureTempData(Controller controller)
    {
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static async Task AssertDuenoCrudAsync(VeterinariaContext context)
    {
        var controller = new DuenoController(context);
        ConfigureTempData(controller);

        var validModel = new Dueno
        {
            Nombre = "Carlos",
            Apellidos = "López",
            NumeroCedula = "4444444",
            Telefono = "5554444444",
            CorreoElectronico = "carlos@correo.com",
            Contrasena = "pass456",
            Activo = true
        };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Duenos.CountAsync());

        controller.ModelState.AddModelError("Nombre", "Nombre requerido");
        var invalidCreate = new Dueno { Nombre = "", Apellidos = "Pérez", NumeroCedula = "5555555", Telefono = "5555555555", CorreoElectronico = "otro@correo.com", Contrasena = "pass789", Activo = true };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Duenos.CountAsync());

        controller.ModelState.Clear();
        var nuevoDueno = await context.Duenos.OrderByDescending(d => d.IdDueno).FirstAsync();
        nuevoDueno.Nombre = "Carlos Actualizado";
        var editResult = await controller.Edit(nuevoDueno.IdDueno, nuevoDueno);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Carlos Actualizado", (await context.Duenos.FindAsync(nuevoDueno.IdDueno))!.Nombre);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("Nombre", "Nombre requerido");
        var invalidEdit = new Dueno { IdDueno = nuevoDueno.IdDueno, Nombre = "", Apellidos = "Pérez", NumeroCedula = "5555555", Telefono = "5555555555", CorreoElectronico = "otro@correo.com", Contrasena = "pass789", Activo = true };
        var invalidEditResult = await controller.Edit(nuevoDueno.IdDueno, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevoDueno.IdDueno);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Duenos.CountAsync());
    }

    private static async Task AssertMascotaCrudAsync(VeterinariaContext context)
    {
        var controller = new MascotasController(context);
        ConfigureTempData(controller);
        var dueno = await context.Duenos.FirstAsync();

        var validModel = new Mascota
        {
            NombreMascota = "Luna",
            ClaseAnimal = "Perro",
            IdDueno = dueno.IdDueno,
            Activa = true
        };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Mascotas.CountAsync());

        controller.ModelState.AddModelError("NombreMascota", "Nombre requerido");
        var invalidCreate = new Mascota { NombreMascota = "", ClaseAnimal = "Gato", IdDueno = dueno.IdDueno, Activa = true };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Mascotas.CountAsync());

        controller.ModelState.Clear();
        var nuevaMascota = await context.Mascotas.OrderByDescending(m => m.IdMascota).FirstAsync();
        nuevaMascota.NombreMascota = "Luna Actualizada";
        var editResult = await controller.Edit(nuevaMascota.IdMascota, nuevaMascota);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Luna Actualizada", (await context.Mascotas.FindAsync(nuevaMascota.IdMascota))!.NombreMascota);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("NombreMascota", "Nombre requerido");
        var invalidEdit = new Mascota { IdMascota = nuevaMascota.IdMascota, NombreMascota = "", ClaseAnimal = "Gato", IdDueno = dueno.IdDueno, Activa = true };
        var invalidEditResult = await controller.Edit(nuevaMascota.IdMascota, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevaMascota.IdMascota);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Mascotas.CountAsync());
    }

    private static async Task AssertEmpleadoCrudAsync(VeterinariaContext context)
    {
        var controller = new EmpleadoesController(context);
        ConfigureTempData(controller);
        var rol = await context.Roles.FirstAsync();

        var validModel = new Empleado
        {
            Nombre = "María",
            Apellidos = "Vega",
            NumeroCedula = "111111111",
            Telefono = "5551111111",
            CorreoElectronico = "maria@vet.com",
            Contrasena = "pass123",
            IdRol = rol.IdRol,
            Activo = true,
            FechaContratacion = DateOnly.FromDateTime(DateTime.Today)
        };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Empleados.CountAsync());

        controller.ModelState.AddModelError("Nombre", "Nombre requerido");
        var invalidCreate = new Empleado { Nombre = "", Apellidos = "Pérez", NumeroCedula = "222222222", Telefono = "5552222222", CorreoElectronico = "falso@vet.com", Contrasena = "pass456", IdRol = rol.IdRol, Activo = true, FechaContratacion = DateOnly.FromDateTime(DateTime.Today) };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Empleados.CountAsync());

        controller.ModelState.Clear();
        var nuevoEmpleado = await context.Empleados.OrderByDescending(e => e.IdEmpleado).FirstAsync();
        nuevoEmpleado.Nombre = "María Actualizada";
        var editResult = await controller.Edit(nuevoEmpleado.IdEmpleado, nuevoEmpleado);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("María Actualizada", (await context.Empleados.FindAsync(nuevoEmpleado.IdEmpleado))!.Nombre);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("Nombre", "Nombre requerido");
        var invalidEdit = new Empleado { IdEmpleado = nuevoEmpleado.IdEmpleado, Nombre = "", Apellidos = "Pérez", NumeroCedula = "222222222", Telefono = "5552222222", CorreoElectronico = "falso@vet.com", Contrasena = "pass456", IdRol = rol.IdRol, Activo = true, FechaContratacion = DateOnly.FromDateTime(DateTime.Today) };
        var invalidEditResult = await controller.Edit(nuevoEmpleado.IdEmpleado, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevoEmpleado.IdEmpleado);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Empleados.CountAsync());
    }

    private static async Task AssertEspecialidadCrudAsync(VeterinariaContext context)
    {
        var controller = new EspecialidadesController(context);
        ConfigureTempData(controller);
        var validModel = new Especialidade { NombreEspecialidad = "Dermatología", Descripcion = "Especialidad cutánea" };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Especialidades.CountAsync());

        controller.ModelState.AddModelError("NombreEspecialidad", "Nombre requerido");
        var invalidCreate = new Especialidade { NombreEspecialidad = "", Descripcion = "Otro" };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Especialidades.CountAsync());

        controller.ModelState.Clear();
        var nuevaEspecialidad = await context.Especialidades.OrderByDescending(e => e.IdEspecialidad).FirstAsync();
        nuevaEspecialidad.NombreEspecialidad = "Dermatología avanzada";
        var editResult = await controller.Edit(nuevaEspecialidad.IdEspecialidad, nuevaEspecialidad);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Dermatología avanzada", (await context.Especialidades.FindAsync(nuevaEspecialidad.IdEspecialidad))!.NombreEspecialidad);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("NombreEspecialidad", "Nombre requerido");
        var invalidEdit = new Especialidade { IdEspecialidad = nuevaEspecialidad.IdEspecialidad, NombreEspecialidad = "", Descripcion = "Otro" };
        var invalidEditResult = await controller.Edit(nuevaEspecialidad.IdEspecialidad, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevaEspecialidad.IdEspecialidad);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Especialidades.CountAsync());
    }

    private static async Task AssertMedicamentoCrudAsync(VeterinariaContext context)
    {
        var controller = new MedicamentosController(context);
        ConfigureTempData(controller);
        var validModel = new Medicamento { NombreMedicamento = "Ibuprofeno", PrincipioActivo = "Ibuprofeno", Presentacion = "Tabletas", Concentracion = "400mg", Activo = true, Observaciones = "Uso general" };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Medicamentos.CountAsync());

        controller.ModelState.AddModelError("NombreMedicamento", "Nombre requerido");
        var invalidCreate = new Medicamento { NombreMedicamento = "", PrincipioActivo = "X", Presentacion = "Tabletas", Concentracion = "100mg", Activo = true };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Medicamentos.CountAsync());

        controller.ModelState.Clear();
        var nuevoMedicamento = await context.Medicamentos.OrderByDescending(m => m.IdMedicamento).FirstAsync();
        nuevoMedicamento.NombreMedicamento = "Ibuprofeno 600";
        var editResult = await controller.Edit(nuevoMedicamento.IdMedicamento, nuevoMedicamento);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Ibuprofeno 600", (await context.Medicamentos.FindAsync(nuevoMedicamento.IdMedicamento))!.NombreMedicamento);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("NombreMedicamento", "Nombre requerido");
        var invalidEdit = new Medicamento { IdMedicamento = nuevoMedicamento.IdMedicamento, NombreMedicamento = "", PrincipioActivo = "X", Presentacion = "Tabletas", Concentracion = "100mg", Activo = true };
        var invalidEditResult = await controller.Edit(nuevoMedicamento.IdMedicamento, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevoMedicamento.IdMedicamento);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Medicamentos.CountAsync());
    }

    private static async Task AssertPermisoCrudAsync(VeterinariaContext context)
    {
        var controller = new PermisoesController(context);
        ConfigureTempData(controller);
        var validModel = new Permiso { NombrePermiso = "Editar citas", Descripcion = "Permiso para editar citas" };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Permisos.CountAsync());

        controller.ModelState.AddModelError("NombrePermiso", "Nombre requerido");
        var invalidCreate = new Permiso { NombrePermiso = "", Descripcion = "Otro" };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Permisos.CountAsync());

        controller.ModelState.Clear();
        var nuevoPermiso = await context.Permisos.OrderByDescending(p => p.IdPermiso).FirstAsync();
        nuevoPermiso.NombrePermiso = "Editar citas v2";
        var editResult = await controller.Edit(nuevoPermiso.IdPermiso, nuevoPermiso);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Editar citas v2", (await context.Permisos.FindAsync(nuevoPermiso.IdPermiso))!.NombrePermiso);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("NombrePermiso", "Nombre requerido");
        var invalidEdit = new Permiso { IdPermiso = nuevoPermiso.IdPermiso, NombrePermiso = "", Descripcion = "Otro" };
        var invalidEditResult = await controller.Edit(nuevoPermiso.IdPermiso, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevoPermiso.IdPermiso);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Permisos.CountAsync());
    }

    private static async Task AssertExpedienteCrudAsync(VeterinariaContext context)
    {
        var controller = new ExpedientesClinicoesController(context);
        ConfigureTempData(controller);
        var mascota = await context.Mascotas.FirstAsync();

        var otraMascota = new Mascota
        {
            NombreMascota = "Nina",
            ClaseAnimal = "Gato",
            IdDueno = mascota.IdDueno,
            Activa = true
        };
        context.Mascotas.Add(otraMascota);
        await context.SaveChangesAsync();

        var validModel = new ExpedientesClinico { IdMascota = otraMascota.IdMascota, FechaApertura = DateTime.Today, Alergias = "Ninguna", Antecedentes = "Sin antecedentes", ObservacionesGenerales = "Todo bien" };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.ExpedientesClinicos.CountAsync());

        controller.ModelState.AddModelError("IdMascota", "Mascota requerida");
        var invalidCreate = new ExpedientesClinico { IdMascota = 0, FechaApertura = DateTime.Today, Alergias = "Ninguna" };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.ExpedientesClinicos.CountAsync());

        controller.ModelState.Clear();
        var nuevoExpediente = await context.ExpedientesClinicos.OrderByDescending(e => e.IdExpediente).FirstAsync();
        nuevoExpediente.ObservacionesGenerales = "Se actualizó";
        var editResult = await controller.Edit(nuevoExpediente.IdExpediente, nuevoExpediente);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Se actualizó", (await context.ExpedientesClinicos.FindAsync(nuevoExpediente.IdExpediente))!.ObservacionesGenerales);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("IdMascota", "Mascota requerida");
        var invalidEdit = new ExpedientesClinico { IdExpediente = nuevoExpediente.IdExpediente, IdMascota = 0, FechaApertura = DateTime.Today, ObservacionesGenerales = "Se actualizó" };
        var invalidEditResult = await controller.Edit(nuevoExpediente.IdExpediente, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevoExpediente.IdExpediente);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.ExpedientesClinicos.CountAsync());
    }

    private static async Task AssertCitaCrudAsync(VeterinariaContext context)
    {
        var controller = new CitasController(context);
        ConfigureTempData(controller);
        var mascota = await context.Mascotas.FirstAsync();
        var empleado = await context.Empleados.FirstAsync();

        var validModel = new Cita
        {
            IdMascota = mascota.IdMascota,
            IdEmpleado = empleado.IdEmpleado,
            FechaHoraInicio = DateTime.Today.AddDays(2).AddHours(9),
            FechaHoraFin = DateTime.Today.AddDays(2).AddHours(10),
            EstadoCita = "Pendiente",
            Motivo = "Chequeo adicional",
            Observaciones = "Necesario",
            FechaCreacion = DateTime.Now
        };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Citas.CountAsync());

        controller.ModelState.AddModelError("Motivo", "Motivo requerido");
        var invalidCreate = new Cita { IdMascota = mascota.IdMascota, IdEmpleado = empleado.IdEmpleado, FechaHoraInicio = DateTime.Today.AddDays(1).AddHours(9), FechaHoraFin = DateTime.Today.AddDays(1).AddHours(10), EstadoCita = "Pendiente", Motivo = "", Observaciones = "Error", FechaCreacion = DateTime.Now };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Citas.CountAsync());

        controller.ModelState.Clear();
        var nuevaCita = await context.Citas.OrderByDescending(c => c.IdCita).FirstAsync();
        nuevaCita.Motivo = "Chequeo actualizado";
        var editResult = await controller.Edit(nuevaCita.IdCita, nuevaCita);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Chequeo actualizado", (await context.Citas.FindAsync(nuevaCita.IdCita))!.Motivo);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("Motivo", "Motivo requerido");
        var invalidEdit = new Cita { IdCita = nuevaCita.IdCita, IdMascota = mascota.IdMascota, IdEmpleado = empleado.IdEmpleado, FechaHoraInicio = DateTime.Today.AddDays(3).AddHours(9), FechaHoraFin = DateTime.Today.AddDays(3).AddHours(10), EstadoCita = "Pendiente", Motivo = "", Observaciones = "Error", FechaCreacion = DateTime.Now };
        var invalidEditResult = await controller.Edit(nuevaCita.IdCita, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevaCita.IdCita);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Citas.CountAsync());
    }

    private static async Task AssertConsultaCrudAsync(VeterinariaContext context)
    {
        var controller = new ConsultasController(context);
        ConfigureTempData(controller);
        var expediente = await context.ExpedientesClinicos.FirstAsync();
        var cita = await context.Citas.FirstAsync();
        var empleado = await context.Empleados.FirstAsync();

        var otraCita = new Cita
        {
            IdMascota = cita.IdMascota,
            IdEmpleado = empleado.IdEmpleado,
            FechaHoraInicio = DateTime.Today.AddDays(4).AddHours(9),
            FechaHoraFin = DateTime.Today.AddDays(4).AddHours(10),
            EstadoCita = "Pendiente",
            Motivo = "Consulta nueva",
            FechaCreacion = DateTime.Now
        };
        context.Citas.Add(otraCita);
        await context.SaveChangesAsync();

        var validModel = new Consulta
        {
            IdExpediente = expediente.IdExpediente,
            IdCita = otraCita.IdCita,
            IdVeterinario = empleado.IdEmpleado,
            FechaConsulta = DateTime.Today,
            MotivoConsulta = "Control de seguimiento",
            Sintomas = "Leve dolor",
            Diagnostico = "Todo bien",
            TratamientoIndicado = "Reposo"
        };

        var createResult = await controller.Create(validModel);
        Assert.IsType<RedirectToActionResult>(createResult);
        Assert.Equal(2, await context.Consultas.CountAsync());

        controller.ModelState.AddModelError("MotivoConsulta", "Motivo requerido");
        var invalidCreate = new Consulta { IdExpediente = expediente.IdExpediente, IdCita = cita.IdCita, IdVeterinario = empleado.IdEmpleado, FechaConsulta = DateTime.Today, MotivoConsulta = "", Sintomas = "Dolor", Diagnostico = "Todo bien" };
        var invalidCreateResult = await controller.Create(invalidCreate);
        Assert.IsType<ViewResult>(invalidCreateResult);
        Assert.Equal(2, await context.Consultas.CountAsync());

        controller.ModelState.Clear();
        var nuevaConsulta = await context.Consultas.OrderByDescending(c => c.IdConsulta).FirstAsync();
        nuevaConsulta.MotivoConsulta = "Control actualizado";
        var editResult = await controller.Edit(nuevaConsulta.IdConsulta, nuevaConsulta);
        Assert.IsType<RedirectToActionResult>(editResult);
        Assert.Equal("Control actualizado", (await context.Consultas.FindAsync(nuevaConsulta.IdConsulta))!.MotivoConsulta);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("MotivoConsulta", "Motivo requerido");
        var invalidEdit = new Consulta { IdConsulta = nuevaConsulta.IdConsulta, IdExpediente = expediente.IdExpediente, IdCita = cita.IdCita, IdVeterinario = empleado.IdEmpleado, FechaConsulta = DateTime.Today, MotivoConsulta = "", Sintomas = "Dolor", Diagnostico = "Todo bien" };
        var invalidEditResult = await controller.Edit(nuevaConsulta.IdConsulta, invalidEdit);
        Assert.IsType<ViewResult>(invalidEditResult);

        var deleteResult = await controller.DeleteConfirmed(nuevaConsulta.IdConsulta);
        Assert.IsType<RedirectToActionResult>(deleteResult);
        Assert.Equal(1, await context.Consultas.CountAsync());
    }

    private static VeterinariaContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<VeterinariaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new VeterinariaContext(options);

        var role = new Role { NombreRol = "Veterinario" };
        context.Roles.Add(role);

        var dueno = new Dueno
        {
            Nombre = "Ana",
            Apellidos = "García",
            NumeroCedula = "1111111",
            Telefono = "5551234567",
            CorreoElectronico = "ana@correo.com",
            Contrasena = "pass123",
            FechaRegistro = DateTime.Now,
            Activo = true
        };
        context.Duenos.Add(dueno);

        var empleado = new Empleado
        {
            Nombre = "Dr. José",
            Apellidos = "Ramírez",
            NumeroCedula = "987654321",
            Telefono = "5550001234",
            CorreoElectronico = "empleado@vet.com",
            Contrasena = "pass123",
            IdRol = role.IdRol,
            Activo = true,
            FechaContratacion = DateOnly.FromDateTime(DateTime.Today)
        };
        context.Empleados.Add(empleado);

        var mascota = new Mascota
        {
            NombreMascota = "Max",
            ClaseAnimal = "Perro",
            IdDueno = dueno.IdDueno,
            Activa = true,
            IdDuenoNavigation = dueno
        };
        context.Mascotas.Add(mascota);

        var expediente = new ExpedientesClinico
        {
            IdMascota = mascota.IdMascota,
            FechaApertura = DateTime.Today,
            IdMascotaNavigation = mascota
        };
        context.ExpedientesClinicos.Add(expediente);

        var cita = new Cita
        {
            IdMascota = mascota.IdMascota,
            IdEmpleado = empleado.IdEmpleado,
            FechaHoraInicio = DateTime.Today.AddHours(9),
            FechaHoraFin = DateTime.Today.AddHours(10),
            EstadoCita = "Pendiente",
            Motivo = "Control general",
            FechaCreacion = DateTime.Now,
            IdMascotaNavigation = mascota,
            IdEmpleadoNavigation = empleado
        };
        context.Citas.Add(cita);

        var consulta = new Consulta
        {
            IdExpediente = expediente.IdExpediente,
            IdCita = cita.IdCita,
            IdVeterinario = empleado.IdEmpleado,
            FechaConsulta = DateTime.Today,
            MotivoConsulta = "Chequeo anual",
            Diagnostico = "Todo en orden",
            IdExpedienteNavigation = expediente,
            IdCitaNavigation = cita,
            IdVeterinarioNavigation = empleado
        };
        context.Consultas.Add(consulta);

        var especialidad = new Especialidade
        {
            NombreEspecialidad = "Cirugía",
            Descripcion = "Especialidad quirúrgica"
        };
        context.Especialidades.Add(especialidad);

        var medicamento = new Medicamento
        {
            NombreMedicamento = "Amoxicilina",
            PrincipioActivo = "Amoxicilina",
            Presentacion = "Capsulas",
            Concentracion = "500mg",
            Activo = true,
            Observaciones = "Uso veterinario"
        };
        context.Medicamentos.Add(medicamento);

        var permiso = new Permiso
        {
            NombrePermiso = "Ver dashboard",
            Descripcion = "Permiso para consultar dashboard"
        };
        context.Permisos.Add(permiso);

        context.SaveChanges();
        return context;
    }
}
