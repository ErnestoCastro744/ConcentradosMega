using System.Diagnostics;
using System.Security.Claims;
using ConcentradosMega.Data;
using ConcentradosMega.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Controllers;

public class InicioViewModel
{
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    public int PedidosPendientes { get; set; }
    public int VentasSinFactura { get; set; }
    public int PedidosSinRuta { get; set; }
    public int EntregasPendientes { get; set; }
}

// Pantalla de inicio por rol
[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var usuario = await _db.Users.FindAsync(id);
        var vm = new InicioViewModel
        {
            Nombre = usuario?.NombreCompleto ?? User.Identity!.Name!,
            Rol = Roles.Todos.FirstOrDefault(User.IsInRole) ?? ""
        };

        if (User.IsInRole(Roles.Repartidor))
        {
            vm.EntregasPendientes = await _db.RutaPedidos
                .CountAsync(rp => rp.Ruta!.RepartidorId == id && rp.EstadoEntrega == EstadosEntrega.Pendiente);
        }
        else
        {
            // RN-02: el vendedor solo cuenta sus propios registros
            var pedidos = _db.Pedidos.AsQueryable();
            if (User.IsInRole(Roles.Vendedor)) pedidos = pedidos.Where(p => p.VendedorId == id);

            vm.PedidosPendientes = await pedidos.CountAsync(p => p.Estado == EstadosPedido.Pendiente);
            vm.VentasSinFactura = await pedidos.CountAsync(p => p.Venta != null && p.Venta.Factura == null);
            vm.PedidosSinRuta = await pedidos.CountAsync(p => p.Estado == EstadosPedido.Confirmado && p.RutaPedido == null);
        }
        return View(vm);
    }

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
