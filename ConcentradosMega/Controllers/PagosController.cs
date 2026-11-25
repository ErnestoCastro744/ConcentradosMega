using System.Security.Claims;
using ConcentradosMega.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConcentradosMega.Controllers;

[Authorize(Roles = "Administrador,Vendedor")]
public class PagosController : Controller
{
    private readonly PagoService _pagos;

    public PagosController(PagoService pagos) => _pagos = pagos;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool EsAdmin => User.IsInRole("Administrador");

    public async Task<IActionResult> Index() =>
        View(await _pagos.ListarVentasAsync(UserId, EsAdmin));

    public async Task<IActionResult> Venta(int id)
    {
        var vm = await _pagos.ObtenerVentaAsync(id, UserId, EsAdmin);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(int ventaId, decimal monto, string metodo, string? referencia)
    {
        // RN-02: solo se registra sobre ventas a las que el usuario tiene acceso
        if (await _pagos.ObtenerVentaAsync(ventaId, UserId, EsAdmin) is null) return NotFound();

        var r = await _pagos.RegistrarAsync(ventaId, monto, metodo, referencia);
        TempData[r.Exito ? "PagoOk" : "PagoError"] = r.Mensaje;
        return RedirectToAction(nameof(Venta), new { id = ventaId });
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Pendientes() =>
        View(await _pagos.ListarPendientesAsync());

    [Authorize(Roles = "Administrador")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolver(int id, bool aprobar)
    {
        var r = await _pagos.ResolverAsync(id, aprobar);
        TempData[r.Exito ? "PagoOk" : "PagoError"] = r.Mensaje;
        return RedirectToAction(nameof(Pendientes));
    }
}
