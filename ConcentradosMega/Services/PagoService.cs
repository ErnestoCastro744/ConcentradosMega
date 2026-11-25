using ConcentradosMega.Data;
using ConcentradosMega.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Services;

public class ResultadoPago
{
    public bool Exito { get; init; }
    public string Mensaje { get; init; } = "";
    public static ResultadoPago Ok(string mensaje) => new() { Exito = true, Mensaje = mensaje };
    public static ResultadoPago Error(string mensaje) => new() { Exito = false, Mensaje = mensaje };
}

public class VentaSaldoVm
{
    public int VentaId { get; set; }
    public int PedidoId { get; set; }
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo => Total - Pagado;
}

public class DetalleVentaVm
{
    public VentaSaldoVm Venta { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();
}

public class PagoPendienteVm
{
    public int PagoId { get; set; }
    public int VentaId { get; set; }
    public string Cliente { get; set; } = "";
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = "";
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; }
}

// Pagos y abonos (ECU-03, RN-10, RN-11)
public class PagoService
{
    public const string Pendiente = "Pendiente";
    public const string Aprobado = "Aprobado";
    public const string Rechazado = "Rechazado";

    public static readonly string[] Metodos = { "Efectivo", "Transferencia", "SINPE" };

    // RN-10: estos medios quedan pendientes hasta que el Administrador los apruebe
    private static readonly string[] RequierenAprobacion = { "Transferencia", "SINPE" };

    private readonly AppDbContext _db;

    public PagoService(AppDbContext db) => _db = db;

    // Saldo = total de la venta - pagos APROBADOS
    private async Task<decimal> PagadoAsync(int ventaId) =>
        await _db.Pagos
            .Where(p => p.VentaId == ventaId && p.Estado == Aprobado)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

    // RN-02: el Vendedor solo ve las ventas de sus pedidos; el Administrador ve todas
    private IQueryable<VentaSaldoVm> ConsultaVentas(string userId, bool esAdmin) =>
        from v in _db.Ventas.AsNoTracking()
        join p in _db.Pedidos on v.PedidoId equals p.Id
        join c in _db.Clientes on p.ClienteId equals c.Id
        where esAdmin || p.VendedorId == userId
        select new VentaSaldoVm
        {
            VentaId = v.Id,
            PedidoId = p.Id,
            Fecha = v.Fecha,
            Cliente = c.Nombre,
            Total = v.Total,
            Pagado = _db.Pagos
                .Where(g => g.VentaId == v.Id && g.Estado == Aprobado)
                .Sum(g => (decimal?)g.Monto) ?? 0m
        };

    public async Task<List<VentaSaldoVm>> ListarVentasAsync(string userId, bool esAdmin) =>
        await ConsultaVentas(userId, esAdmin).OrderByDescending(v => v.VentaId).ToListAsync();

    public async Task<DetalleVentaVm?> ObtenerVentaAsync(int ventaId, string userId, bool esAdmin)
    {
        var venta = await ConsultaVentas(userId, esAdmin).FirstOrDefaultAsync(v => v.VentaId == ventaId);
        if (venta is null) return null;

        var pagos = await _db.Pagos.AsNoTracking()
            .Where(p => p.VentaId == ventaId)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return new DetalleVentaVm { Venta = venta, Pagos = pagos };
    }

    public async Task<List<PagoPendienteVm>> ListarPendientesAsync() =>
        await (from g in _db.Pagos.AsNoTracking()
               where g.Estado == Pendiente
               join v in _db.Ventas on g.VentaId equals v.Id
               join p in _db.Pedidos on v.PedidoId equals p.Id
               join c in _db.Clientes on p.ClienteId equals c.Id
               orderby g.Fecha
               select new PagoPendienteVm
               {
                   PagoId = g.Id,
                   VentaId = v.Id,
                   Cliente = c.Nombre,
                   Monto = g.Monto,
                   Metodo = g.Metodo,
                   Referencia = g.Referencia,
                   Fecha = g.Fecha
               }).ToListAsync();

    public async Task<ResultadoPago> RegistrarAsync(int ventaId, decimal monto, string metodo, string? referencia)
    {
        var venta = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta is null) return ResultadoPago.Error("La venta no existe.");

        // RN-11
        if (monto <= 0) return ResultadoPago.Error("El monto debe ser mayor a cero.");
        if (!Metodos.Contains(metodo)) return ResultadoPago.Error("Método de pago no válido.");

        referencia = referencia?.Trim();
        var requiereAprobacion = RequierenAprobacion.Contains(metodo);
        if (requiereAprobacion && string.IsNullOrEmpty(referencia))
            return ResultadoPago.Error($"El pago por {metodo} requiere número de referencia.");

        var saldo = venta.Total - await PagadoAsync(ventaId);
        if (saldo <= 0) return ResultadoPago.Error("La venta ya está pagada por completo.");
        if (monto > saldo) return ResultadoPago.Error($"El monto supera el saldo pendiente (₡{saldo:N2}).");

        var pago = new Pago
        {
            VentaId = ventaId,
            Monto = monto,
            Metodo = metodo,
            Referencia = string.IsNullOrEmpty(referencia) ? null : referencia,
            Estado = requiereAprobacion ? Pendiente : Aprobado,
            Fecha = DateTime.Now
        };
        _db.Pagos.Add(pago);
        await _db.SaveChangesAsync();

        return ResultadoPago.Ok(requiereAprobacion
            ? "Pago registrado. Queda pendiente de aprobación y aún no reduce el saldo."
            : "Pago registrado y aplicado al saldo.");
    }

    // RN-10: solo el Administrador aprueba o rechaza (se controla en el controlador)
    public async Task<ResultadoPago> ResolverAsync(int pagoId, bool aprobar)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.Id == pagoId);
        if (pago is null) return ResultadoPago.Error("El pago no existe.");
        if (pago.Estado != Pendiente) return ResultadoPago.Error("El pago ya fue resuelto.");

        if (aprobar)
        {
            var venta = await _db.Ventas.AsNoTracking().FirstAsync(v => v.Id == pago.VentaId);
            var saldo = venta.Total - await PagadoAsync(pago.VentaId);
            if (pago.Monto > saldo)
                return ResultadoPago.Error($"No se puede aprobar: el monto supera el saldo actual (₡{saldo:N2}).");
        }

        pago.Estado = aprobar ? Aprobado : Rechazado;
        await _db.SaveChangesAsync();
        return ResultadoPago.Ok(aprobar ? "Pago aprobado y aplicado al saldo." : "Pago rechazado.");
    }
}
