# Pruebas de rendimiento de ProyectoVet

Fecha local: 18 de noviembre de 2026 (Costa Rica).

La aplicación respondió sin errores en 45 000 peticiones HTTP de lectura contra SQL Server LocalDB. Estos resultados son una referencia con pocos datos; no acreditan capacidad de producción ni el rendimiento del guardado.

## Método

- Compilación Release, .NET 8, Kestrel en http://127.0.0.1:5187 y LocalDB en el mismo equipo Windows.
- Base existente: 5 consultas, 10 expedientes, 13 citas, 10 mascotas y 7 empleados. No se insertaron consultas ni citas para generar carga.
- Cinco rutas, concurrencias de 1, 5 y 10 clientes; 1 000 solicitudes por lote y tres repeticiones por combinación: 45 000 solicitudes medidas.
- Cinco solicitudes de calentamiento por ruta y comprobaciones previas fuera de las estadísticas. Aplicación y SQL ya estaban calientes; first_observed no representa un arranque en frío.
- Clientes sin sesión autenticada, conexiones HTTP reutilizadas, sin pausas entre solicitudes. No se siguieron redirecciones: se exigió HTTP 200, para no medir accidentalmente el login.
- Se midió desde antes del envío hasta recibir todo el HTML: incluye servidor, SQL y transferencia local. No incluye CSS, JavaScript, imágenes, CDN ni renderizado del navegador.
- Se conservaron las muestras individuales en results.csv; no se guardaron respuestas HTML ni datos clínicos.
- La ejecución final se realizó después de terminar las pruebas funcionales, para evitar superponer esa carga. Sigue siendo un equipo compartido con otros procesos.
- Suma de duración de los lotes: 35.44 segundos. Son ráfagas cortas; no es una prueba sostenida ni de estrés prolongado.

## Resultados

Promedio y p95 calculados sobre las 3 000 muestras de cada combinación, sin promediar percentiles. Todos los lotes terminaron sin errores HTTP ni de conexión.

| Ruta | Clientes simultáneos | Promedio (ms) | p95 (ms) | Máximo (ms) |
|---|---:|---:|---:|---:|
| `/Consultas/Index` | 1 | 1.39 | 2.15 | 17.92 |
| `/Consultas/Index` | 5 | 2.13 | 2.83 | 4.81 |
| `/Consultas/Index` | 10 | 3.55 | 4.90 | 21.14 |
| `/Consultas/Create` | 1 | 1.98 | 2.58 | 23.76 |
| `/Consultas/Create` | 5 | 3.44 | 4.48 | 16.68 |
| `/Consultas/Create` | 10 | 5.39 | 7.49 | 14.54 |
| `/Mascotas/Index` | 1 | 1.12 | 1.46 | 50.48 |
| `/Mascotas/Index` | 5 | 1.74 | 2.23 | 24.32 |
| `/Mascotas/Index` | 10 | 3.23 | 4.57 | 6.86 |
| `/Citas/Index` | 1 | 1.96 | 3.33 | 70.52 |
| `/Citas/Index` | 5 | 2.50 | 3.19 | 24.47 |
| `/Citas/Index` | 10 | 4.22 | 5.91 | 21.29 |
| `/Consultas/Details/6` | 1 | 1.03 | 1.38 | 23.88 |
| `/Consultas/Details/6` | 5 | 1.59 | 2.06 | 25.61 |
| `/Consultas/Details/6` | 10 | 2.92 | 4.32 | 28.59 |

p95 significa que el 95 % de las solicitudes tardó ese tiempo o menos. Es más útil que el promedio para detectar esperas de los usuarios. El máximo es un caso aislado; sin trazas y métricas de recursos no se puede atribuir su causa.

Con diez clientes, abrir el formulario de consulta fue la operación con mayor promedio entre las rutas medidas. Esto coincide con un formulario que ejecuta tres consultas para llenar listas, pero no demuestra por sí solo qué operación interna consume ese tiempo. No se alcanzó ni se determinó el punto de saturación.

## Mejoras sugeridas, en orden de prioridad

1. **Paginar los listados y filtrar en SQL.** `ConsultasController.Index` materializa todos los registros con `ToListAsync`. Usar páginas de 20-50 registros, filtros por fecha/paciente y orden estable por `FechaConsulta` e `IdConsulta`. Evita que cada visita transfiera y renderice todo el historial; beneficia también a Citas y Mascotas. Para avanzar por historiales grandes, evaluar paginación por clave.
2. **Reducir los datos del listado.** `ConsultarConsultasConRelaciones` carga entidades completas de consulta, cita, expediente, mascota, dueño y empleado. Proyectar con `Select` a un modelo con solo las columnas visibles; evitar traer el texto clínico completo y datos del empleado que no se muestran. Medir `AsNoTracking` o `AsNoTrackingWithIdentityResolution` en lecturas; su ventaja depende de las relaciones repetidas. Mantener separado el flujo de edición.
3. **Limitar los selectores del formulario.** `CargarConsultasDropDowns` trae todos los expedientes, citas no canceladas y empleados activos, mediante tres `ToList` síncronos. Cambiar a búsquedas por paciente/fecha, excluir citas ya utilizadas cuando corresponda y cargar opciones bajo demanda. Usar `ToListAsync` para liberar el hilo durante las esperas de SQL; eso favorece concurrencia, sin garantizar que una consulta individual tarde menos. Ejecutar secuencialmente sobre el mismo DbContext; no usar Task.WhenAll con ese contexto.
4. **Validar índices con planes reales.** El modelo define `(IdExpediente, FechaConsulta)`, útil para el historial de un expediente. Ese índice no garantiza resolver el orden global por fecha. Con una base de prueba grande, revisar el plan y evaluar `(FechaConsulta DESC, IdConsulta DESC)` para el listado paginado. No crear índices sin medir: ocupan espacio y añaden trabajo a las escrituras. Verificar también los índices existentes en SQL; la definición del modelo no prueba su presencia física.

Estas son propuestas basadas en el código y la documentación, no mejoras de velocidad ya demostradas. No se modificaron controladores, índices ni lógica de la aplicación durante esta tarea.

## Qué falta para evaluar capacidad real

- Repetir en una base de prueba aislada con 1 000, 10 000 y 100 000 consultas sintéticas y relaciones representativas.
- Probar sesiones autenticadas y un flujo mixto: listar, buscar, abrir expediente, guardar y editar. El GET del formulario no prueba el POST de guardado; falta medir validación, inserción, restricciones y bloqueos concurrentes.
- Mantener cada escalón de carga varios minutos desde otra máquina y observar CPU, memoria, GC, conexiones SQL, esperas, errores y p95/p99. Comparar antes/después en iguales condiciones.
- Acordar objetivos propios: por ejemplo, p95 de lectura menor de 300 ms y de guardado menor de 500 ms, con menos de 1 % de errores a la carga esperada. Son objetivos propuestos, no estándares ni resultados de esta ejecución.
- Medir por separado la experiencia del navegador y las dependencias externas. No extrapolar las peticiones por segundo locales a usuarios reales.

## Repetir la prueba

Requisitos: Python 3, .NET 8 y acceso a la instancia LocalDB configurada. Desde la raíz del repositorio, iniciar el servidor en una terminal:

```powershell
dotnet run --project ProyectoVet/ProyectoVet.csproj --configuration Release --no-launch-profile --urls http://127.0.0.1:5187
```

El inicio ejecuta la inicialización habitual de Program.cs. En otra terminal, sin compilaciones ni pruebas simultáneas:

```powershell
python performance/http_benchmark.py --requests 1000 --repeats 3
```

Los archivos results.json y results.csv se sobrescriben. Guardar una copia para comparar cambios. Detener el servidor con Ctrl+C al terminar. El script solo acepta HTTP local y envía GET; los valores de concurrencia están definidos en el script.

Las 22 pruebas funcionales existentes también pasaron en Release; comprueban comportamiento, no capacidad de carga.

## Referencias

- [Microsoft: consultas eficientes, proyección, límites e índices](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [Microsoft: seguimiento y consultas sin seguimiento](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [Microsoft: pruebas de carga y estrés en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/load-tests?view=aspnetcore-9.0)
