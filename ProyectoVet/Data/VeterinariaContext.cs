using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Models;

namespace ProyectoVet.Data;

public partial class VeterinariaContext : DbContext
{
    public VeterinariaContext(DbContextOptions<VeterinariaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<Consulta> Consultas { get; set; }

    public virtual DbSet<Dueno> Duenos { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<EmpleadosEspecialidade> EmpleadosEspecialidades { get; set; }

    public virtual DbSet<Especialidade> Especialidades { get; set; }

    public virtual DbSet<ExpedientesClinico> ExpedientesClinicos { get; set; }

    public virtual DbSet<Mascota> Mascotas { get; set; }

    public virtual DbSet<Medicamento> Medicamentos { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tratamiento> Tratamientos { get; set; }

    public virtual DbSet<Vacuna> Vacunas { get; set; }

    public virtual DbSet<Vacunacione> Vacunaciones { get; set; }

    public virtual DbSet<VwAgendaCita> VwAgendaCitas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.IdCita);

            entity.HasIndex(e => new { e.FechaHoraInicio, e.EstadoCita }, "IX_Citas_FechaEstado");

            entity.HasIndex(e => e.IdEmpleado, "IX_Citas_IdEmpleado");

            entity.HasIndex(e => e.IdMascota, "IX_Citas_IdMascota");

            entity.HasIndex(e => new { e.IdEmpleado, e.FechaHoraInicio }, "UX_Citas_Empleado_Inicio_Activa")
                .IsUnique()
                .HasFilter("([EstadoCita]<>'Cancelada')");

            entity.HasIndex(e => new { e.IdMascota, e.FechaHoraInicio }, "UX_Citas_Mascota_Inicio_Activa")
                .IsUnique()
                .HasFilter("([EstadoCita]<>'Cancelada')");

            entity.Property(e => e.EstadoCita)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaHoraFin).HasPrecision(0);
            entity.Property(e => e.FechaHoraInicio).HasPrecision(0);
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.CreadaPorEmpleado).WithMany(p => p.CitaCreadaPorEmpleados)
                .HasForeignKey(d => d.CreadaPorEmpleadoId)
                .HasConstraintName("FK_Citas_CreadaPor");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.CitaIdEmpleadoNavigations)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Veterinario");

            entity.HasOne(d => d.IdMascotaNavigation).WithMany(p => p.Cita)
                .HasForeignKey(d => d.IdMascota)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Mascotas");
        });

        modelBuilder.Entity<Consulta>(entity =>
        {
            entity.HasKey(e => e.IdConsulta);

            entity.HasIndex(e => new { e.IdExpediente, e.FechaConsulta }, "IX_Consultas_Expediente_Fecha").IsDescending(false, true);

            entity.HasIndex(e => e.IdCita, "UX_Consultas_Cita")
                .IsUnique()
                .HasFilter("([IdCita] IS NOT NULL)");

            entity.Property(e => e.Diagnostico)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.FechaConsulta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MotivoConsulta)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.PesoKg).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Sintomas)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.TemperaturaC).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.TratamientoIndicado)
                .HasMaxLength(2000)
                .IsUnicode(false);

            entity.HasOne(d => d.IdCitaNavigation).WithOne(p => p.Consulta)
                .HasForeignKey<Consulta>(d => d.IdCita)
                .HasConstraintName("FK_Consultas_Citas");

            entity.HasOne(d => d.IdExpedienteNavigation).WithMany(p => p.Consulta)
                .HasForeignKey(d => d.IdExpediente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Consultas_Expedientes");

            entity.HasOne(d => d.IdVeterinarioNavigation).WithMany(p => p.Consulta)
                .HasForeignKey(d => d.IdVeterinario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Consultas_Veterinarios");
        });

        modelBuilder.Entity<Dueno>(entity =>
        {
            entity.HasKey(e => e.IdDueno);

            entity.HasIndex(e => e.NumeroCedula, "UQ_Duenos_Cedula").IsUnique();

            entity.HasIndex(e => e.CorreoElectronico, "UQ_Duenos_Correo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellidos)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Contrasena)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(254)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumeroCedula)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TelefonoOpcional)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado);

            entity.HasIndex(e => e.IdRol, "IX_Empleados_IdRol");

            entity.HasIndex(e => e.NumeroCedula, "UQ_Empleados_Cedula").IsUnique();

            entity.HasIndex(e => e.CorreoElectronico, "UQ_Empleados_Correo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellidos)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Contrasena)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(254)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumeroCedula)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleados_Roles");
        });

        modelBuilder.Entity<EmpleadosEspecialidade>(entity =>
        {
            entity.HasKey(e => new { e.IdEmpleado, e.IdEspecialidad });

            entity.HasIndex(e => e.IdEmpleado, "UX_Empleado_Especialidad_Principal")
                .IsUnique()
                .HasFilter("([EsPrincipal]=(1))");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithOne(p => p.EmpleadosEspecialidade)
                .HasForeignKey<EmpleadosEspecialidade>(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmpleadoEsp_Empleados");

            entity.HasOne(d => d.IdEspecialidadNavigation).WithMany(p => p.EmpleadosEspecialidades)
                .HasForeignKey(d => d.IdEspecialidad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmpleadoEsp_Especialidades");
        });

        modelBuilder.Entity<Especialidade>(entity =>
        {
            entity.HasKey(e => e.IdEspecialidad);

            entity.HasIndex(e => e.NombreEspecialidad, "UQ_Especialidades_Nombre").IsUnique();

            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NombreEspecialidad)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ExpedientesClinico>(entity =>
        {
            entity.HasKey(e => e.IdExpediente);

            entity.HasIndex(e => e.IdMascota, "UQ_Expedientes_Mascota").IsUnique();

            entity.Property(e => e.Alergias)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Antecedentes)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.FechaApertura)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ObservacionesGenerales)
                .HasMaxLength(2000)
                .IsUnicode(false);

            entity.HasOne(d => d.IdMascotaNavigation).WithOne(p => p.ExpedientesClinico)
                .HasForeignKey<ExpedientesClinico>(d => d.IdMascota)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expedientes_Mascotas");
        });

        modelBuilder.Entity<Mascota>(entity =>
        {
            entity.HasKey(e => e.IdMascota);

            entity.HasIndex(e => e.IdDueno, "IX_Mascotas_IdDueno");

            entity.HasIndex(e => e.NumeroMicrochip, "UQ_Mascotas_Microchip")
                .IsUnique()
                .HasFilter("([NumeroMicrochip] IS NOT NULL)");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.ClaseAnimal)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreMascota)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumeroMicrochip)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.OtraClaseAnimal)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Raza)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Sexo)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.IdDuenoNavigation).WithMany(p => p.Mascota)
                .HasForeignKey(d => d.IdDueno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mascotas_Duenos");
        });

        modelBuilder.Entity<Medicamento>(entity =>
        {
            entity.HasKey(e => e.IdMedicamento);

            entity.HasIndex(e => new { e.NombreMedicamento, e.Presentacion, e.Concentracion }, "UQ_Medicamentos_NombrePresentacion").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Concentracion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NombreMedicamento)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Presentacion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrincipioActivo)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.IdPermiso);

            entity.HasIndex(e => e.NombrePermiso, "UQ_Permisos_Nombre").IsUnique();

            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NombrePermiso)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.HasIndex(e => e.NombreRol, "UQ_Roles_NombreRol").IsUnique();

            entity.Property(e => e.NombreRol)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasMany(d => d.IdPermisos).WithMany(p => p.IdRols)
                .UsingEntity<Dictionary<string, object>>(
                    "RolesPermiso",
                    r => r.HasOne<Permiso>().WithMany()
                        .HasForeignKey("IdPermiso")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolesPermisos_Permisos"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("IdRol")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolesPermisos_Roles"),
                    j =>
                    {
                        j.HasKey("IdRol", "IdPermiso");
                        j.ToTable("RolesPermisos");
                    });
        });

        modelBuilder.Entity<Tratamiento>(entity =>
        {
            entity.HasKey(e => e.IdTratamiento);

            entity.HasIndex(e => e.IdConsulta, "IX_Tratamientos_Consulta");

            entity.Property(e => e.Dosis)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EstadoTratamiento)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Activo");
            entity.Property(e => e.Frecuencia)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Instrucciones)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.NombreTratamiento)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ViaAdministracion)
                .HasMaxLength(80)
                .IsUnicode(false);

            entity.HasOne(d => d.IdConsultaNavigation).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.IdConsulta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tratamientos_Consultas");

            entity.HasOne(d => d.IdMedicamentoNavigation).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.IdMedicamento)
                .HasConstraintName("FK_Tratamientos_Medicamentos");
        });

        modelBuilder.Entity<Vacuna>(entity =>
        {
            entity.HasKey(e => e.IdVacuna);

            entity.HasIndex(e => e.NombreVacuna, "UQ_Vacunas_Nombre").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.EsquemaDosis)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.NombreVacuna)
                .HasMaxLength(120)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Vacunacione>(entity =>
        {
            entity.HasKey(e => e.IdVacunacion);

            entity.HasIndex(e => new { e.IdMascota, e.FechaAplicacion }, "IX_Vacunaciones_Mascota_Fecha").IsDescending(false, true);

            entity.Property(e => e.NumeroLote)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.IdMascotaNavigation).WithMany(p => p.Vacunaciones)
                .HasForeignKey(d => d.IdMascota)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vacunaciones_Mascotas");

            entity.HasOne(d => d.IdVacunaNavigation).WithMany(p => p.Vacunaciones)
                .HasForeignKey(d => d.IdVacuna)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vacunaciones_Vacunas");

            entity.HasOne(d => d.IdVeterinarioNavigation).WithMany(p => p.Vacunaciones)
                .HasForeignKey(d => d.IdVeterinario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vacunaciones_Veterinario");
        });

        modelBuilder.Entity<VwAgendaCita>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AgendaCitas");

            entity.Property(e => e.EstadoCita)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaHoraFin).HasPrecision(0);
            entity.Property(e => e.FechaHoraInicio).HasPrecision(0);
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.NombreDueno)
                .HasMaxLength(131)
                .IsUnicode(false);
            entity.Property(e => e.NombreMascota)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreVeterinario)
                .HasMaxLength(131)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
