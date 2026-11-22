using ConcentradosMega.Data;
using ConcentradosMega.Models;
using ConcentradosMega.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace ConcentradosMega.Tests;

// Pruebas del módulo de Zahid: Factura PDF (ECU-02) y Rutas (ECU-04).
// Usan SQLite en memoria para que los índices UNIQUE se apliquen de verdad.
public class FacturaYRutaTests : IDisposable
{
    private readonly SqliteConnection _con;
    private readonly AppDbContext _db;
    private readonly Usuario _vendedor, _repartidor, _otroRepartidor;
    private readonly Cliente _cliente;
    private readonly Producto _producto;

    public FacturaYRutaTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _con = new SqliteConnection("DataSource=:memory:");
        _con.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_con).Options);
        _db.Database.EnsureCreated();

        _vendedor = new Usuario { UserName = "v", NombreCompleto = "Vendedor" };
        _repartidor = new Usuario { UserName = "r", NombreCompleto = "Repartidor" };
        _otroRepartidor = new Usuario { UserName = "r2", NombreCompleto = "Otro" };
        _cliente = new Cliente { Cedula = "1", Nombre = "Finca", Direccion = "Alajuela" };
        _producto = new Producto { Codigo = "P1", Nombre = "Concentrado", Precio = 1000 };
        _db.AddRange(_vendedor, _repartidor, _otroRepartidor, _cliente, _producto);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _con.Dispose();
    }

    private Pedido CrearPedido(string estado, bool conVenta)
    {
        var p = new Pedido { ClienteId = _cliente.Id, VendedorId = _vendedor.Id, Estado = estado, Total = 3000 };
        p.Detalles.Add(new DetallePedido { ProductoId = _producto.Id, Cantidad = 3, PrecioUnitario = 1000, Subtotal = 3000 });
        _db.Pedidos.Add(p);
        _db.SaveChanges();
        if (conVenta)
        {
            _db.Ventas.Add(new Venta { PedidoId = p.Id, Total = p.Total });
            _db.SaveChanges();
        }
        return p;
    }

    private int VentaDe(Pedido p) => _db.Ventas.Single(v => v.PedidoId == p.Id).Id;

    [Fact]
    public async Task Factura_genera_pdf_y_consecutivo()
    {
        var venta = VentaDe(CrearPedido(EstadosPedido.Confirmado, true));
        var f = await new FacturaService(_db).ObtenerOGenerarAsync(venta);

        Assert.Equal(1, f.Consecutivo);
        Assert.Equal("FAC-000001", f.Numero);
        Assert.NotNull(f.ArchivoPdf);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(f.ArchivoPdf!, 0, 4));
    }

    [Fact]
    public async Task Factura_se_reutiliza_si_ya_existe_RN12()
    {
        var venta = VentaDe(CrearPedido(EstadosPedido.Confirmado, true));
        var svc = new FacturaService(_db);
        var f1 = await svc.ObtenerOGenerarAsync(venta);
        var f2 = await svc.ObtenerOGenerarAsync(venta);

        Assert.Equal(f1.Id, f2.Id);
        Assert.Equal(1, await _db.Facturas.CountAsync());
    }

    [Fact]
    public async Task Consecutivos_avanzan_entre_ventas()
    {
        var svc = new FacturaService(_db);
        var a = await svc.ObtenerOGenerarAsync(VentaDe(CrearPedido(EstadosPedido.Confirmado, true)));
        var b = await svc.ObtenerOGenerarAsync(VentaDe(CrearPedido(EstadosPedido.Confirmado, true)));
        Assert.Equal(a.Consecutivo + 1, b.Consecutivo);
    }

    [Fact]
    public async Task Base_de_datos_rechaza_dos_facturas_para_la_misma_venta()
    {
        var venta = VentaDe(CrearPedido(EstadosPedido.Confirmado, true));
        _db.Facturas.Add(new Factura { VentaId = venta, Consecutivo = 1 });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Simula a otro usuario insertando una segunda factura para la misma venta
        _db.Facturas.Add(new Factura { VentaId = venta, Consecutivo = 2 });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    private Ruta CrearRuta(Usuario repartidor)
    {
        var r = new Ruta { Nombre = "Norte", RepartidorId = repartidor.Id };
        _db.Rutas.Add(r);
        _db.SaveChanges();
        return r;
    }

    [Fact]
    public async Task Asigna_pedido_confirmado()
    {
        var p = CrearPedido(EstadosPedido.Confirmado, true);
        var ruta = CrearRuta(_repartidor);
        var errores = await new RutaService(_db).AsignarPedidosAsync(ruta.Id, new[] { p.Id });

        Assert.Empty(errores);
        Assert.Single(_db.RutaPedidos.Where(x => x.RutaId == ruta.Id));
    }

    [Fact]
    public async Task Rechaza_pedido_no_confirmado()
    {
        var p = CrearPedido(EstadosPedido.Pendiente, false);
        var ruta = CrearRuta(_repartidor);
        var errores = await new RutaService(_db).AsignarPedidosAsync(ruta.Id, new[] { p.Id });

        Assert.Single(errores);
        Assert.Empty(_db.RutaPedidos);
    }

    [Fact]
    public async Task Rechaza_pedido_ya_asignado_a_otra_ruta_RN13()
    {
        var p = CrearPedido(EstadosPedido.Confirmado, true);
        var svc = new RutaService(_db);
        var r1 = CrearRuta(_repartidor);
        var r2 = CrearRuta(_otroRepartidor);

        Assert.Empty(await svc.AsignarPedidosAsync(r1.Id, new[] { p.Id }));
        var errores = await svc.AsignarPedidosAsync(r2.Id, new[] { p.Id });

        Assert.Single(errores);
        Assert.Contains("ya está asignado", errores[0]);
        Assert.Equal(1, await _db.RutaPedidos.CountAsync());
    }

    [Fact]
    public async Task Repartidor_no_puede_cambiar_entregas_ajenas_RN02()
    {
        var p = CrearPedido(EstadosPedido.Confirmado, true);
        var svc = new RutaService(_db);
        var ruta = CrearRuta(_repartidor);
        await svc.AsignarPedidosAsync(ruta.Id, new[] { p.Id });
        var rp = _db.RutaPedidos.Single();

        var error = await svc.CambiarEstadoEntregaAsync(rp.Id, EstadosEntrega.Entregado, null, _otroRepartidor.Id, false);
        Assert.NotNull(error);

        error = await svc.CambiarEstadoEntregaAsync(rp.Id, EstadosEntrega.Entregado, null, _repartidor.Id, false);
        Assert.Null(error);
        Assert.Equal(EstadosRuta.Finalizada, (await _db.Rutas.FindAsync(ruta.Id))!.Estado);
    }

    [Fact]
    public async Task No_entregado_exige_motivo()
    {
        var p = CrearPedido(EstadosPedido.Confirmado, true);
        var svc = new RutaService(_db);
        var ruta = CrearRuta(_repartidor);
        await svc.AsignarPedidosAsync(ruta.Id, new[] { p.Id });
        var rp = _db.RutaPedidos.Single();

        Assert.NotNull(await svc.CambiarEstadoEntregaAsync(rp.Id, EstadosEntrega.NoEntregado, " ", _repartidor.Id, false));
        Assert.Null(await svc.CambiarEstadoEntregaAsync(rp.Id, EstadosEntrega.NoEntregado, "Cliente ausente", _repartidor.Id, false));
    }
}
