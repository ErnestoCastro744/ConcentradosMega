using ConcentradosMega.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Data;

public class AppDbContext : IdentityDbContext<Usuario>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Ruta> Rutas => Set<Ruta>();
    public DbSet<RutaPedido> RutaPedidos => Set<RutaPedido>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Cliente>().HasIndex(c => c.Cedula).IsUnique();
        b.Entity<Producto>().HasIndex(p => p.Codigo).IsUnique();

        b.Entity<Pedido>(e =>
        {
            e.HasIndex(p => p.Guid).IsUnique();                       // RN-15
            e.HasOne(p => p.Cliente).WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Vendedor).WithMany()
                .HasForeignKey(p => p.VendedorId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Pedido_Estado",
                "[Estado] IN ('Pendiente','Confirmado','Anulado')"));
        });

        b.Entity<DetallePedido>(e =>
        {
            e.HasOne(d => d.Pedido).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Producto).WithMany()
                .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Detalle_Cantidad", "[Cantidad] > 0"));
        });

        b.Entity<Venta>(e =>
        {
            e.HasIndex(v => v.PedidoId).IsUnique();                   // RN-09
            e.HasOne(v => v.Pedido).WithOne(p => p.Venta)
                .HasForeignKey<Venta>(v => v.PedidoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MovimientoInventario>(e =>
        {
            e.HasOne(m => m.Producto).WithMany(p => p.Movimientos)
                .HasForeignKey(m => m.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Pedido).WithMany()
                .HasForeignKey(m => m.PedidoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => new { m.PedidoId, m.ProductoId, m.Tipo }).IsUnique()
                .HasFilter("[PedidoId] IS NOT NULL");                 // RN-06
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Mov_Tipo", "[Tipo] IN ('Entrada','Salida')");
                t.HasCheckConstraint("CK_Mov_Cantidad", "[Cantidad] > 0");
            });
        });

        b.Entity<Factura>(e =>
        {
            e.HasIndex(f => f.VentaId).IsUnique();                    // RN-12
            e.HasIndex(f => f.Consecutivo).IsUnique();
            e.HasOne(f => f.Venta).WithOne(v => v.Factura)
                .HasForeignKey<Factura>(f => f.VentaId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Pago>(e =>
        {
            e.HasOne(p => p.Venta).WithMany(v => v.Pagos)
                .HasForeignKey(p => p.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Pago_Monto", "[Monto] > 0");  // RN-11
                t.HasCheckConstraint("CK_Pago_Estado", "[Estado] IN ('Pendiente','Aprobado','Rechazado')");
            });
        });

        b.Entity<Ruta>(e =>
        {
            e.HasOne(r => r.Repartidor).WithMany()
                .HasForeignKey(r => r.RepartidorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RutaPedido>(e =>
        {
            e.HasIndex(rp => rp.PedidoId).IsUnique();                 // RN-13
            e.HasOne(rp => rp.Ruta).WithMany(r => r.Pedidos)
                .HasForeignKey(rp => rp.RutaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rp => rp.Pedido).WithOne(p => p.RutaPedido)
                .HasForeignKey<RutaPedido>(rp => rp.PedidoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
