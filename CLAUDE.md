# NexoStock

## Mision y alcance
Backend ASP.NET Core Web API para un sistema de inventarios. La fase inicial cubre autenticacion, usuarios y roles. Productos, bodegas, inventario y movimientos se incorporaran por iteraciones bajo las reglas de este documento.

El frontend React + TypeScript es un cliente separado: consume contratos HTTP publicos y no accede directamente a EF Core, Identity ni a la base de datos.

## Arquitectura objetivo

Aplicar Clean Architecture con separacion por capas y dependencias dirigidas hacia el dominio:

```text
src/
	NexoStock.Domain/          # Entidades, value objects, enums, reglas y eventos de dominio
	NexoStock.Application/     # Casos de uso, puertos, DTOs, validaciones y autorizacion de aplicacion
	NexoStock.Infrastructure/  # EF Core, Identity, repositorios, migraciones, JWT y servicios externos
	NexoStock.Api/             # Controllers, middleware, composicion y configuracion HTTP
tests/
	NexoStock.Domain.Tests/
	NexoStock.Application.Tests/
	NexoStock.Api.Tests/
```

La solucion ya aplica esta separacion para autenticacion, usuarios, roles y articulos. Toda nueva funcionalidad debe respetar los limites anteriores; las responsabilidades heredadas se extraeran de forma incremental, sin una reescritura masiva.

### Regla de dependencias

- `Domain` no depende de ASP.NET Core, EF Core, Identity, JWT, SQL Server ni infraestructura.
- `Application` depende solo de `Domain` y abstracciones propias.
- `Infrastructure` implementa las interfaces de `Application` y puede depender de `Domain`.
- `Api` depende de `Application` e `Infrastructure` solamente para registrar implementaciones en el composition root.
- Ningun controller debe contener reglas de negocio, consultas EF Core ni acceso directo a `DbContext`.
- Las dependencias deben apuntar hacia adentro; las referencias circulares estan prohibidas.

## Responsabilidades por capa

### Domain

- Modelar invariantes del negocio en entidades, value objects y servicios de dominio.
- Exponer comportamiento, no setters publicos indiscriminados.
- No usar DTOs, `IQueryable`, modelos de EF Core ni tipos HTTP.

### Application

- Organizar el sistema por casos de uso, preferentemente `Feature/UseCase`.
- Definir comandos, queries, handlers, interfaces de persistencia y resultados de aplicacion.
- Validar entrada y reglas de aplicacion antes de invocar infraestructura.
- Mantener los casos de uso independientes de HTTP; los controllers solo adaptan requests y responses.

### Infrastructure

- Usar Entity Framework Core como ORM oficial y SQL Server como proveedor actual.
- Mantener `ApplicationDbContext`, configuraciones Fluent API, repositorios y migraciones aqui cuando se extraiga la capa.
- No filtrar entidades EF Core hacia API o frontend.
- Preferir consultas proyectadas a DTOs, `AsNoTracking()` en lecturas y `CancellationToken` en operaciones I/O.
- Centralizar transacciones y concurrencia en una unidad de trabajo solo cuando el caso de uso lo necesite; evitar abstracciones genericas sin comportamiento real.

### Api

- Mantener controllers delgados, con rutas, binding, autorizacion y mapeo de errores.
- Usar DTOs de entrada y salida; nunca exponer entidades de dominio, `ApplicationUser` ni `PasswordHash`.
- Definir contratos HTTP consistentes: `200/201` para exito, `400` para validacion, `401` para autenticacion, `403` para permisos, `404` para ausencia y `409` para conflictos.
- Usar middleware o `ProblemDetails` para errores; no devolver stack traces ni detalles internos.
- Mantener Swagger/OpenAPI actualizado con cada cambio de contrato.

## Autorizacion por roles

Las autorizaciones deben expresarse mediante politicas nombradas, no con cadenas de roles dispersas en controllers.

| Politica | Roles permitidos | Uso actual |
|---|---|---|
| `Profile.Read` | `Administrador`, `Bodeguero` | Consultar el perfil autenticado |
| `Users.Read` | `Administrador` | Listar usuarios |
| `Users.Create` | `Administrador` | Crear usuarios y asignar roles |
| `Articles.Read` | Segun matriz | Consultar articulos |
| `Articles.Create` | Segun matriz | Crear articulos con codigo unico |

- Todo endpoint protegido debe declarar `[Authorize(Policy = ...)]` o una politica equivalente.
- La interfaz puede ocultar opciones no autorizadas, pero la API siempre debe hacer cumplir el permiso.
- Un usuario autenticado sin el rol requerido recibe `403 Forbidden`; sin token recibe `401 Unauthorized`.
- Los nuevos modulos deben agregar sus politicas a `ApplicationPolicies` y documentar la matriz antes de implementar endpoints.
- La matriz se persiste en `RolePermissions`; no agregar permisos solo en la interfaz.
- La gestion administrativa usa `GET /api/permissions` y `PUT /api/permissions/{role}`.
- Todo cambio de permisos requiere migracion EF Core y pruebas de autorizacion.

## Persistencia y ORM

- EF Core es el ORM permitido; no mezclar acceso SQL directo salvo una justificacion documentada y una prueba de integracion.
- Configurar entidades con Fluent API y mantener nombres de tablas, indices y restricciones explicitos.
- Las migraciones son codigo revisable: una migracion por cambio coherente, nombre descriptivo y ejecucion explicita.
- No ejecutar `Database.Migrate()` automaticamente en produccion.
- Las consultas deben paginar colecciones, evitar N+1 y no materializar datos antes de filtrar.
- Los cambios de esquema requieren actualizar modelo, migracion y pruebas de persistencia.

## Seguridad

- Nunca guardar contrasenas en texto plano ni registrar tokens, secretos o `PasswordHash`.
- No incluir secretos, cadenas de conexion reales ni claves JWT en el repositorio.
- Usar User Secrets en desarrollo y variables de entorno o un gestor de secretos en despliegue.
- Mantener validaciones de JWT, HTTPS, expiracion y autorizacion activas.
- Aplicar el principio de minimo privilegio y autorizar por politica o rol en el caso de uso correspondiente.
- Validar y limitar entradas; no construir SQL, rutas de archivo o HTML con datos no confiables.

## Estandar SDD (Specification-Driven Development)

Todo cambio funcional debe seguir este ciclo:

1. **Especificacion:** describir objetivo, alcance, actores, reglas, contrato, criterios de aceptacion y casos fuera de alcance.
2. **Diseno:** identificar capa y caso de uso afectado, modelo de datos, dependencias, riesgos y estrategia de migracion.
3. **Pruebas primero o junto al cambio:** convertir cada criterio de aceptacion en pruebas unitarias, de integracion o de contrato segun corresponda.
4. **Implementacion:** hacer el cambio minimo respetando las dependencias y convenciones de este documento.
5. **Verificacion:** ejecutar restore, build, tests y comprobaciones manuales de API cuando aplique.
6. **Evidencia:** actualizar documentacion, OpenAPI, migraciones y notas de decisiones si el cambio altera contratos o arquitectura.

Una tarea no se considera terminada si no tiene criterios de aceptacion verificables y pruebas proporcionales al riesgo. Si una solicitud contradice estos lineamientos, documentar la excepcion y su razon antes de implementarla.

## Pruebas

- `Domain`: pruebas unitarias rapidas para invariantes y reglas.
- `Application`: pruebas unitarias de casos de uso con puertos simulados.
- `Infrastructure`: pruebas de integracion contra una base de datos controlada para EF Core, migraciones y consultas relevantes.
- `Api`: pruebas de contrato/integracion para autenticacion, autorizacion, codigos HTTP y serializacion.
- Mantener la piramide de pruebas: muchas unitarias, integracion focalizada y pocas pruebas end-to-end.
- Toda correccion de bug debe incluir una prueba que reproduzca el fallo cuando sea viable.

## Convenciones

- C# nullable habilitado, async/await y `CancellationToken` en operaciones asincronas.
- Nombres de tablas, propiedades y codigo de dominio en ingles; documentacion y especificaciones en espanol.
- Preferir tipos fuertes, records para DTOs inmutables y resultados explicitos frente a excepciones para flujo esperado.
- Evitar clases estaticas globales, service locators, repositorios genericos vacios y logica duplicada.
- Mantener cambios pequenos, revisables y acompanados de pruebas.

## Comandos de verificacion

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/NexoStock.Api --launch-profile http
dotnet ef migrations add <NombreDescriptivo> --project src/NexoStock.Infrastructure --startup-project src/NexoStock.Api
dotnet ef database update --project src/NexoStock.Infrastructure --startup-project src/NexoStock.Api
```

Para el cliente:

```powershell
cd frontend
npm.cmd install
npm.cmd run build
npm.cmd run dev
```
