# Concentrados Mega

Sistema web ASP.NET Core MVC (.NET 10) para pedidos, ventas, facturación, pagos, inventario y rutas de entrega.

## Requisitos

1. Git
2. .NET 10 SDK (`dotnet --version` debe mostrar `10.0.x`)
3. Visual Studio 2026 o VS Code con C# Dev Kit
4. SQL Server Express (instancia `.\SQLEXPRESS`) y SSMS

## Cómo correrlo

```powershell
git clone https://github.com/ErnestoCastro744/ConcentradosMega.git
cd ConcentradosMega
dotnet restore
dotnet run --project ConcentradosMega
```

Al iniciar, la aplicación crea la base `ConcentradosMega`, aplica las migraciones y carga los datos de prueba.
Si su instancia de SQL Server tiene otro nombre, cambie `ConnectionStrings:DefaultConnection` en
`ConcentradosMega/appsettings.json` (no suba ese cambio).

Para crear la base a mano en SSMS, ejecute `Database/ConcentradosMega.sql`.

### Usuarios de prueba

| Correo | Rol |
|---|---|
| admin@mega.com | Administrador |
| vendedor@mega.com | Vendedor |
| repartidor@mega.com | Repartidor |

La contraseña de prueba está en `ConcentradosMega/Data/SeedData.cs` (`ClavePrueba`).

## Pruebas

```powershell
dotnet test
```

## Estructura

| Carpeta | Contenido |
|---|---|
| `Models/Entidades.cs` | Las entidades (12 tablas contando usuarios y roles de Identity) |
| `Data/AppDbContext.cs` | DbContext, llaves foráneas, UNIQUE y CHECK |
| `Data/SeedData.cs` | Datos de prueba |
| `Data/Migrations/` | Migraciones de EF Core |
| `Services/` | Lógica de negocio (una clase por módulo) |
| `Controllers/`, `Views/` | Pantallas MVC |
| `ConcentradosMega.Tests/` | Pruebas xUnit |

## Reglas para trabajar

- Cada quien trabaja en su rama: `nombre/modulo` (ej. `ian/clientes`) y abre un pull request hacia `main`.
- No subir cambios a `appsettings.json` con datos de su máquina.
- Nombres en español, clases en PascalCase, una regla de negocio = un comentario `// RN-xx`.
- Para una tabla nueva: entidad en `Models`, `DbSet` en `AppDbContext`, y `dotnet ef migrations add NombreCambio -o Data/Migrations`.

## Módulos

| Módulo | Responsable | Estado |
|---|---|---|
| Base, login, roles, layout | Luis | Hecho (adelantado por Zahid, revisar) |
| Base de datos, entidades, datos de prueba | Ernesto | Hecho (adelantado por Zahid, revisar) |
| Factura PDF (ECU-02) y Rutas (ECU-04) | Zahid | Hecho |
| Clientes y Productos | Ian | Pendiente |
| Pedidos y Ventas (ECU-01) | Dilan | Pendiente |
| Inventario y pedido sin conexión (ECU-05) | Brandon | Pendiente |
| Pagos (ECU-03) | Ernesto | Pendiente |
