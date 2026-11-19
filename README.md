# ProyectoVet

## Perfiles y vistas

Cada empleado inicia sesión con su correo y contraseña. El sistema abre automáticamente el panel correspondiente a su rol:

| Módulo | Administrador | Veterinario | Secretaria / Recepcionista |
|---|---|---|---|
| Panel de inicio | General | Clínico; agenda asignada al empleado | Recepción; agenda de la clínica |
| Mi perfil | Datos propios | Datos propios | Datos propios |
| Dueños y mascotas | Gestionar | Consultar | Gestionar |
| Citas | Gestionar | Consultar | Gestionar |
| Consultas y expedientes | Gestionar | Crear, consultar y editar | Sin acceso |
| Medicamentos | Gestionar | Consultar | Sin acceso |
| Empleados, especialidades y permisos | Gestionar | Sin acceso | Sin acceso |

La eliminación de consultas y expedientes queda reservada al administrador. El personal veterinario puede consultar el historial clínico compartido de la clínica; únicamente la agenda de su panel se filtra por empleado. Los menús, botones y rutas HTTP usan la misma matriz de acceso, definida en `ProyectoVet/Security/RoleAccess.cs`.

### Crear las cuentas

1. Reiniciar la aplicación para registrar el rol `Secretaria` y cargar la configuración de autenticación.
2. Iniciar sesión como administrador.
3. Abrir **Administración → Empleados y perfiles → Crear nuevo**.
4. Completar los datos reales, establecer la contraseña, elegir **Veterinario** o **Secretaria** y marcar **Activo**.
5. Iniciar sesión con cada cuenta para abrir su panel. Las cuentas existentes con rol **Recepcionista** usan el panel y los permisos de Secretaria.

No se crean cuentas de veterinario o secretaria con contraseñas predeterminadas. Se conservan los usuarios existentes y no se requiere una migración de tablas. Si una cuenta es desactivada o su rol cambia, el acceso se actualiza en la siguiente petición.

La autenticación usa una cookie protegida de ASP.NET Core, con vencimiento deslizante de dos horas. Cerrar sesión requiere POST y token antiforgery. Las sesiones anteriores a este cambio deben iniciar sesión nuevamente. Los permisos del catálogo `Permisos` son descriptivos; la matriz anterior controla el acceso efectivo.

### Verificación

```powershell
dotnet test ProyectoVet.Tests/ProyectoVet.Tests.csproj --configuration Release
```

Las pruebas HTTP arrancan Kestrel con una base aislada en memoria y cuentas sintéticas. Verifican login, paneles, navegación, perfil propio, bloqueo de URL y POST, creación de cuentas por el administrador, cierre de sesión, desactivación y cambios de rol.

La suite contiene 100 casos: 78 unitarios (`Category=Unit`) y 22 de integración HTTP (`Category=Integration`). Las pruebas unitarias usan una base aislada en memoria cuando necesitan persistencia; no requieren SQL Server ni abren un servidor HTTP.

```powershell
# Solo pruebas unitarias
dotnet test ProyectoVet.Tests/ProyectoVet.Tests.csproj --configuration Release --filter "Category=Unit"

# Solo integración HTTP
dotnet test ProyectoVet.Tests/ProyectoVet.Tests.csproj --configuration Release --filter "Category=Integration"
```

La cobertura de los perfiles se distribuye en:

- `AuthControllerTests`: credenciales válidas e inválidas, cuentas inactivas, roles desconocidos, identidad autenticada, cierre de sesión y respuesta 403.
- `HomeRoleTests`: selección del panel, contadores por rol, agenda propia ordenada y limitada, estados vacíos, acceso anónimo y perfil del empleado autenticado.
- `RoleAccessTests`: límites de cada rol, compatibilidad con Recepcionista, rechazo de identidades no autenticadas y autorización sobre la acción resuelta por MVC.
- `RoleAccessHttpTests`: verificación conjunta de cookies, antiforgery, rutas protegidas y vistas Razor.

Las pruebas directas de controladores no ejecutan los filtros de autorización ni antiforgery; esa protección se comprueba con las pruebas HTTP. El proveedor en memoria tampoco comprueba índices, restricciones ni rendimiento de SQL Server.

El informe de `performance/INFORME.md` corresponde a las mediciones anteriores a la protección de rutas. El script de rendimiento anónimo detectará ahora la redirección al login y se detendrá; para nuevas mediciones se necesita una sesión autenticada con los permisos correspondientes.
