using ConcentradosMega.Data;
using ConcentradosMega.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Services;

// ECU-04: Rutas de entrega
public class RutaService
{
    private readonly AppDbContext _db;

    public RutaService(AppDbContext db) => _db = db;

    /// <summary>Pedidos confirmados que todavía no están en ninguna ruta.</summary>
    public Task<List<Pedido>> PedidosDisponiblesAsync() =>
        _db.Pedidos
            .Include(p => p.Cliente)
            .Where(p => p.Estado == EstadosPedido.Confirmado && p.RutaPedido == null)
            .OrderBy(p => p.Fecha)
            .ToListAsync();

    /// <summary>
    /// Asigna pedidos a la ruta. Devuelve los mensajes de los pedidos rechazados:
    /// no confirmados o ya asignados a otra ruta (RN-13).
    /// </summary>
    public async Task<List<string>> AsignarPedidosAsync(int rutaId, IEnumerable<int> pedidoIds)
    {
        var errores = new List<string>();
        var ruta = await _db.Rutas.Include(r => r.Pedidos).FirstOrDefaultAsync(r => r.Id == rutaId)
            ?? throw new InvalidOperationException("La ruta no existe.");

        if (ruta.Estado == EstadosRuta.Finalizada)
        {
            errores.Add("La ruta ya está finalizada; no se le pueden agregar pedidos.");
            return errores;
        }

        var orden = ruta.Pedidos.Count == 0 ? 0 : ruta.Pedidos.Max(p => p.Orden);
        foreach (var id in pedidoIds.Distinct())
        {
            var pedido = await _db.Pedidos.Include(p => p.RutaPedido).FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) { errores.Add($"El pedido #{id} no existe."); continue; }
            if (pedido.Estado != EstadosPedido.Confirmado) { errores.Add($"El pedido #{id} no está confirmado."); continue; }
            if (pedido.RutaPedido != null) { errores.Add($"El pedido #{id} ya está asignado a la ruta #{pedido.RutaPedido.RutaId}."); continue; }

            _db.RutaPedidos.Add(new RutaPedido { RutaId = rutaId, PedidoId = id, Orden = ++orden });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // El índice UNIQUE de PedidoId detuvo una asignación simultánea (RN-13)
                _db.ChangeTracker.Clear();
                errores.Add($"El pedido #{id} ya está asignado a otra ruta.");
            }
        }
        return errores;
    }

    /// <summary>Quita un pedido de la ruta si aún no se ha entregado.</summary>
    public async Task<string?> QuitarPedidoAsync(int rutaPedidoId)
    {
        var rp = await _db.RutaPedidos.FindAsync(rutaPedidoId);
        if (rp == null) return "La entrega no existe.";
        if (rp.EstadoEntrega != EstadosEntrega.Pendiente) return "No se puede quitar un pedido que ya tiene resultado de entrega.";
        _db.RutaPedidos.Remove(rp);
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>
    /// Cambia el estado de una entrega. El repartidor solo puede tocar sus propias
    /// entregas (RN-02); el administrador puede tocar cualquiera.
    /// </summary>
    public async Task<string?> CambiarEstadoEntregaAsync(int rutaPedidoId, string estado, string? observacion,
        string usuarioId, bool esAdmin)
    {
        if (!EstadosEntrega.Todos.Contains(estado)) return "Estado de entrega no válido.";

        var rp = await _db.RutaPedidos.Include(x => x.Ruta).FirstOrDefaultAsync(x => x.Id == rutaPedidoId);
        if (rp == null) return "La entrega no existe.";
        if (!esAdmin && rp.Ruta!.RepartidorId != usuarioId) return "No tiene permiso sobre esta entrega.";
        if (estado == EstadosEntrega.NoEntregado && string.IsNullOrWhiteSpace(observacion))
            return "Indique el motivo por el que no se entregó.";

        rp.EstadoEntrega = estado;
        rp.Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        rp.FechaEntrega = estado == EstadosEntrega.Pendiente ? null : DateTime.Now;

        var ruta = await _db.Rutas.Include(r => r.Pedidos).FirstAsync(r => r.Id == rp.RutaId);
        ruta.Estado = ruta.Pedidos.All(p => p.EstadoEntrega != EstadosEntrega.Pendiente)
            ? EstadosRuta.Finalizada
            : ruta.Pedidos.Any(p => p.EstadoEntrega != EstadosEntrega.Pendiente)
                ? EstadosRuta.EnCurso
                : EstadosRuta.Planificada;

        await _db.SaveChangesAsync();
        return null;
    }
}
