using ConcentradosMega.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Data;

// Datos de prueba: 3 usuarios, 5 clientes, 12 productos, 2 pedidos, 1 ruta.
// Se ejecuta al iniciar la app solo si la base está vacía.
public static class SeedData
{
    // Contraseña de prueba de los 3 usuarios (solo desarrollo)
    public const string ClavePrueba = "Mega123!";

    public static async Task InicializarAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var rol in Roles.Todos)
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));

        var admin = await CrearUsuario(userManager, "admin@mega.com", "Luis Administrador", Roles.Administrador);
        var vendedor = await CrearUsuario(userManager, "vendedor@mega.com", "Valeria Vendedora", Roles.Vendedor);
        var repartidor = await CrearUsuario(userManager, "repartidor@mega.com", "Roberto Repartidor", Roles.Repartidor);

        if (await db.Clientes.AnyAsync()) return;

        var clientes = new[]
        {
            new Cliente { Cedula = "101110111", Nombre = "Finca La Esperanza", Telefono = "8888-1111", Direccion = "San Carlos, Alajuela" },
            new Cliente { Cedula = "202220222", Nombre = "Lechería El Roble", Telefono = "8888-2222", Direccion = "Zarcero, Alajuela" },
            new Cliente { Cedula = "303330333", Nombre = "Granja Los Pinos", Telefono = "8888-3333", Direccion = "Turrialba, Cartago" },
            new Cliente { Cedula = "404440444", Nombre = "Agroservicios Mora", Telefono = "8888-4444", Direccion = "Grecia, Alajuela" },
            new Cliente { Cedula = "505550555", Nombre = "Avícola San José", Telefono = "8888-5555", Direccion = "Desamparados, San José" },
        };
        db.Clientes.AddRange(clientes);

        var productos = new[]
        {
            new Producto { Codigo = "P001", Nombre = "Concentrado lechero 46 kg", Precio = 14500, StockMinimo = 20 },
            new Producto { Codigo = "P002", Nombre = "Concentrado engorde res 46 kg", Precio = 13800, StockMinimo = 20 },
            new Producto { Codigo = "P003", Nombre = "Alimento pollo inicio 46 kg", Precio = 16200, StockMinimo = 15 },
            new Producto { Codigo = "P004", Nombre = "Alimento pollo engorde 46 kg", Precio = 15900, StockMinimo = 15 },
            new Producto { Codigo = "P005", Nombre = "Alimento gallina ponedora 46 kg", Precio = 15400, StockMinimo = 15 },
            new Producto { Codigo = "P006", Nombre = "Alimento cerdo desarrollo 46 kg", Precio = 14900, StockMinimo = 10 },
            new Producto { Codigo = "P007", Nombre = "Alimento equino 40 kg", Precio = 17500, StockMinimo = 10 },
            new Producto { Codigo = "P008", Nombre = "Alimento perro adulto 20 kg", Precio = 21000, StockMinimo = 8 },
            new Producto { Codigo = "P009", Nombre = "Sal mineral 25 kg", Precio = 9800, StockMinimo = 10 },
            new Producto { Codigo = "P010", Nombre = "Melaza 20 L", Precio = 7600, StockMinimo = 10 },
            new Producto { Codigo = "P011", Nombre = "Alimento conejo 25 kg", Precio = 11200, StockMinimo = 5 },
            new Producto { Codigo = "P012", Nombre = "Alimento tilapia 40 kg", Precio = 18900, StockMinimo = 5, Estado = false },
        };
        db.Productos.AddRange(productos);
        await db.SaveChangesAsync();

        // Existencias iniciales
        foreach (var p in productos)
            db.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = p.Id, Tipo = TiposMovimiento.Entrada, Cantidad = 100,
                Observacion = "Inventario inicial"
            });

        // Pedido 1: confirmado, con venta y salida de inventario
        var pedido1 = new Pedido { ClienteId = clientes[0].Id, VendedorId = vendedor.Id, Estado = EstadosPedido.Confirmado };
        pedido1.Detalles.Add(Linea(productos[0], 10));
        pedido1.Detalles.Add(Linea(productos[8], 4));
        pedido1.Total = pedido1.Detalles.Sum(d => d.Subtotal);

        // Pedido 2: pendiente
        var pedido2 = new Pedido { ClienteId = clientes[2].Id, VendedorId = vendedor.Id, Estado = EstadosPedido.Pendiente };
        pedido2.Detalles.Add(Linea(productos[2], 6));
        pedido2.Detalles.Add(Linea(productos[4], 3));
        pedido2.Total = pedido2.Detalles.Sum(d => d.Subtotal);

        db.Pedidos.AddRange(pedido1, pedido2);
        await db.SaveChangesAsync();

        db.Ventas.Add(new Venta { PedidoId = pedido1.Id, Total = pedido1.Total });
        foreach (var d in pedido1.Detalles)
            db.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = d.ProductoId, Tipo = TiposMovimiento.Salida, Cantidad = d.Cantidad,
                PedidoId = pedido1.Id, Observacion = $"Salida por pedido #{pedido1.Id}"
            });

        // 1 ruta con el pedido confirmado
        var ruta = new Ruta { Nombre = "Ruta Norte", RepartidorId = repartidor.Id };
        ruta.Pedidos.Add(new RutaPedido { PedidoId = pedido1.Id, Orden = 1 });
        db.Rutas.Add(ruta);

        await db.SaveChangesAsync();
    }

    private static DetallePedido Linea(Producto p, int cantidad) => new()
    {
        ProductoId = p.Id, Cantidad = cantidad, PrecioUnitario = p.Precio, Subtotal = p.Precio * cantidad
    };

    private static async Task<Usuario> CrearUsuario(UserManager<Usuario> um, string correo, string nombre, string rol)
    {
        var u = await um.FindByEmailAsync(correo);
        if (u != null) return u;

        u = new Usuario { UserName = correo, Email = correo, EmailConfirmed = true, NombreCompleto = nombre };
        var r = await um.CreateAsync(u, ClavePrueba);
        if (!r.Succeeded)
            throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
        await um.AddToRoleAsync(u, rol);
        return u;
    }
}
