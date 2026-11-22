using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ConcentradosMega.Data;
using ConcentradosMega.Models;
using ConcentradosMega.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Controllers;

public class RutaFormViewModel
{
    [Required(ErrorMessage = "Ingrese un nombre"), StringLength(100)]
    public string Nombre { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Seleccione un repartidor"), Display(Name = "Repartidor")]
    public string RepartidorId { get; set; } = "";

    public List<int> PedidoIds { get; set; } = new();
}

// ECU-04: Rutas (Zahid)
[Authorize]
public class RutasController : Controller
{
    private readonly AppDbContext _db;
    private readonly RutaService _rutas;
    private readonly UserManager<Usuario> _users;

    public RutasController(AppDbContext db, RutaService rutas, UserManager<Usuario> users)
    {
        _db = db;
        _rutas = rutas;
        _users = users;
    }

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Index(DateTime? fecha)
    {
        var rutas = _db.Rutas.Include(r => r.Repartidor).Include(r => r.Pedidos).AsQueryable();
        if (fecha.HasValue) rutas = rutas.Where(r => r.Fecha == fecha.Value.Date);
        ViewData["Fecha"] = fecha?.ToString("yyyy-MM-dd");
        return View(await rutas.OrderByDescending(r => r.Fecha).ThenBy(r => r.Nombre).ToListAsync());
    }

    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Crear()
    {
        await CargarListas();
        return View(new RutaFormViewModel());
    }

    [Authorize(Roles = Roles.Administrador), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RutaFormViewModel vm)
    {
        var repartidores = await _users.GetUsersInRoleAsync(Roles.Repartidor);
        if (!repartidores.Any(r => r.Id == vm.RepartidorId))
            ModelState.AddModelError(nameof(vm.RepartidorId), "Seleccione un repartidor válido.");

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(vm);
        }

        var ruta = new Ruta { Nombre = vm.Nombre.Trim(), Fecha = vm.Fecha.Date, RepartidorId = vm.RepartidorId };
        _db.Rutas.Add(ruta);
        await _db.SaveChangesAsync();

        var errores = await _rutas.AsignarPedidosAsync(ruta.Id, vm.PedidoIds);
        TempData["Ok"] = "Ruta creada.";
        if (errores.Count > 0) TempData["Error"] = string.Join(" ", errores);
        return RedirectToAction(nameof(Detalle), new { id = ruta.Id });
    }

    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Detalle(int id)
    {
        var ruta = await _db.Rutas
            .Include(r => r.Repartidor)
            .Include(r => r.Pedidos).ThenInclude(rp => rp.Pedido!).ThenInclude(p => p.Cliente)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (ruta == null) return NotFound();

        ViewBag.Disponibles = await _rutas.PedidosDisponiblesAsync();
        return View(ruta);
    }

    [Authorize(Roles = Roles.Administrador), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(int id, List<int> pedidoIds)
    {
        if (pedidoIds.Count == 0)
            TempData["Error"] = "Seleccione al menos un pedido.";
        else
        {
            var errores = await _rutas.AsignarPedidosAsync(id, pedidoIds);
            if (errores.Count > 0) TempData["Error"] = string.Join(" ", errores);
            else TempData["Ok"] = "Pedidos asignados.";
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [Authorize(Roles = Roles.Administrador), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int id, int rutaPedidoId)
    {
        var error = await _rutas.QuitarPedidoAsync(rutaPedidoId);
        if (error != null) TempData["Error"] = error; else TempData["Ok"] = "Pedido quitado de la ruta.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // Vista del repartidor: solo sus entregas (RN-02)
    [Authorize(Roles = Roles.Repartidor + "," + Roles.Administrador)]
    public async Task<IActionResult> MisEntregas(bool todas = false)
    {
        var q = _db.RutaPedidos
            .Include(rp => rp.Ruta)
            .Include(rp => rp.Pedido!).ThenInclude(p => p.Cliente)
            .Include(rp => rp.Pedido!).ThenInclude(p => p.Detalles).ThenInclude(d => d.Producto)
            .Where(rp => rp.Ruta!.RepartidorId == UsuarioId);

        if (!todas) q = q.Where(rp => rp.EstadoEntrega == EstadosEntrega.Pendiente);

        ViewData["Todas"] = todas;
        return View(await q.OrderBy(rp => rp.Ruta!.Fecha).ThenBy(rp => rp.Orden).ToListAsync());
    }

    [Authorize(Roles = Roles.Repartidor + "," + Roles.Administrador), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int rutaPedidoId, string estado, string? observacion, string? volver)
    {
        var error = await _rutas.CambiarEstadoEntregaAsync(rutaPedidoId, estado, observacion, UsuarioId,
            User.IsInRole(Roles.Administrador));
        if (error != null) TempData["Error"] = error; else TempData["Ok"] = $"Entrega marcada como «{estado}».";
        return Url.IsLocalUrl(volver) ? Redirect(volver!) : RedirectToAction(nameof(MisEntregas));
    }

    private async Task CargarListas()
    {
        var repartidores = await _users.GetUsersInRoleAsync(Roles.Repartidor);
        ViewBag.Repartidores = new SelectList(repartidores.Where(r => r.Activo), "Id", "NombreCompleto");
        ViewBag.Disponibles = await _rutas.PedidosDisponiblesAsync();
    }
}
