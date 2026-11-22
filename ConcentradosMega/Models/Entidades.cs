using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ConcentradosMega.Models;

// Roles del sistema (RN-01)
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";
    public const string Repartidor = "Repartidor";

    public static readonly string[] Todos = { Administrador, Vendedor, Repartidor };
}

public static class EstadosPedido
{
    public const string Pendiente = "Pendiente";
    public const string Confirmado = "Confirmado";
    public const string Anulado = "Anulado";
}

public static class EstadosPago
{
    public const string Pendiente = "Pendiente";
    public const string Aprobado = "Aprobado";
    public const string Rechazado = "Rechazado";
}

public static class MetodosPago
{
    public const string Efectivo = "Efectivo";
    public const string Sinpe = "SINPE";
}

public static class EstadosRuta
{
    public const string Planificada = "Planificada";
    public const string EnCurso = "En curso";
    public const string Finalizada = "Finalizada";
}

public static class EstadosEntrega
{
    public const string Pendiente = "Pendiente";
    public const string Entregado = "Entregado";
    public const string NoEntregado = "No entregado";

    public static readonly string[] Todos = { Pendiente, Entregado, NoEntregado };
}

public static class TiposMovimiento
{
    public const string Entrada = "Entrada";
    public const string Salida = "Salida";
}

// Tabla 1-2: AspNetUsers / AspNetRoles (Identity)
public class Usuario : IdentityUser
{
    [Required, StringLength(100)]
    public string NombreCompleto { get; set; } = "";

    public bool Activo { get; set; } = true;
}

// Tabla 3
public class Cliente
{
    public int Id { get; set; }

    [Required, StringLength(20), Display(Name = "Cédula")]
    public string Cedula { get; set; } = "";

    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";

    [StringLength(20), Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(100), EmailAddress]
    public string? Correo { get; set; }

    [StringLength(250), Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    // RN-03: los clientes se desactivan, no se borran
    public bool Estado { get; set; } = true;

    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}

// Tabla 4
public class Producto
{
    public int Id { get; set; }

    [Required, StringLength(20), Display(Name = "Código")]
    public string Codigo { get; set; } = "";

    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";

    [StringLength(250), Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Column(TypeName = "decimal(12,2)"), Range(0.01, 99999999)]
    public decimal Precio { get; set; }

    [Display(Name = "Stock mínimo")]
    public int StockMinimo { get; set; }

    // RN-04: solo productos activos en catálogo y pedidos
    public bool Estado { get; set; } = true;

    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}

// Tabla 5
public class Pedido
{
    public int Id { get; set; }

    // RN-15: GUID generado en el navegador para pedidos sin conexión (único)
    public Guid Guid { get; set; } = Guid.NewGuid();

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Required]
    public string VendedorId { get; set; } = "";
    public Usuario? Vendedor { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required, StringLength(20)]
    public string Estado { get; set; } = EstadosPedido.Pendiente;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Total { get; set; }

    // RN-08
    [StringLength(250), Display(Name = "Motivo de anulación")]
    public string? MotivoAnulacion { get; set; }

    // RN-14: 1 si vino de la sincronización sin conexión
    public bool OrigenLocal { get; set; }

    public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    public Venta? Venta { get; set; }
    public RutaPedido? RutaPedido { get; set; }
}

// Tabla 6
public class DetallePedido
{
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }

    // RN-07: precio congelado al momento del pedido
    [Column(TypeName = "decimal(12,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }
}

// Tabla 7
public class Venta
{
    public int Id { get; set; }

    // RN-09: una sola venta por pedido (UNIQUE)
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Total { get; set; }

    public Factura? Factura { get; set; }
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

// Tabla 8
public class MovimientoInventario
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Required, StringLength(10)]
    public string Tipo { get; set; } = TiposMovimiento.Entrada;

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    // RN-06: una sola salida por pedido y producto
    public int? PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    [StringLength(250)]
    public string? Observacion { get; set; }
}

// Tabla 9
public class Factura
{
    public int Id { get; set; }

    // RN-12: una sola factura por venta (UNIQUE); si existe se reutiliza
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    // Consecutivo único, p. ej. 1, 2, 3... (se muestra como FAC-000001)
    public int Consecutivo { get; set; }

    [Display(Name = "Fecha de emisión")]
    public DateTime FechaEmision { get; set; } = DateTime.Now;

    public byte[]? ArchivoPdf { get; set; }

    [NotMapped]
    public string Numero => $"FAC-{Consecutivo:D6}";
}

// Tabla 10
public class Pago
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    // RN-11: monto positivo y no mayor al saldo
    [Column(TypeName = "decimal(12,2)")]
    public decimal Monto { get; set; }

    [Required, StringLength(20), Display(Name = "Método")]
    public string Metodo { get; set; } = MetodosPago.Efectivo;

    // RN-10: SINPE queda Pendiente y no baja el saldo hasta aprobarse
    [Required, StringLength(20)]
    public string Estado { get; set; } = EstadosPago.Aprobado;

    [StringLength(50)]
    public string? Referencia { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
}

// Tabla 11
public class Ruta
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required, Display(Name = "Repartidor")]
    public string RepartidorId { get; set; } = "";
    public Usuario? Repartidor { get; set; }

    [Required, StringLength(20)]
    public string Estado { get; set; } = EstadosRuta.Planificada;

    public ICollection<RutaPedido> Pedidos { get; set; } = new List<RutaPedido>();
}

// Tabla 12
public class RutaPedido
{
    public int Id { get; set; }

    public int RutaId { get; set; }
    public Ruta? Ruta { get; set; }

    // RN-13: un pedido solo puede estar en una ruta (UNIQUE)
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int Orden { get; set; }

    [Required, StringLength(20), Display(Name = "Estado de entrega")]
    public string EstadoEntrega { get; set; } = EstadosEntrega.Pendiente;

    public DateTime? FechaEntrega { get; set; }

    [StringLength(250)]
    public string? Observacion { get; set; }
}
