using System.Security.Claims;
using ConcentradosMega.Data;
using ConcentradosMega.Models;
using ConcentradosMega.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Controllers;

// ECU-02: Factura PDF (Zahid)
[Authorize(Roles = Roles.Administrador + "," + Roles.Vendedor)]
public class FacturasController : Controller
{
    private readonly AppDbContext _db;
    private readonly FacturaService _facturas;

    public FacturasController(AppDbContext db, FacturaService facturas)
    {
        _db = db;
        _facturas = facturas;
    }

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool EsAdmin => User.IsInRole(Roles.Administrador);

    // Lista de ventas con botón para generar o ver la factura
    public async Task<IActionResult> Index(string? buscar)
    {
        var ventas = _db.Ventas
            .Include(v => v.Factura)
            .Include(v => v.Pedido!).ThenInclude(p => p.Cliente)
            .AsQueryable();

        if (!EsAdmin) ventas = ventas.Where(v => v.Pedido!.VendedorId == UsuarioId); // RN-02

        if (!string.IsNullOrWhiteSpace(buscar))
            ventas = ventas.Where(v => v.Pedido!.Cliente!.Nombre.Contains(buscar) || v.Pedido.Cliente.Cedula.Contains(buscar));

        ViewData["Buscar"] = buscar;
        return View(await ventas.OrderByDescending(v => v.Fecha).ToListAsync());
    }

    // Genera la factura (o reutiliza la existente, RN-12) y la descarga
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(int ventaId)
    {
        if (!await PuedeVerVenta(ventaId)) return Forbid();
        try
        {
            var factura = await _facturas.ObtenerOGenerarAsync(ventaId);
            TempData["Ok"] = $"Factura {factura.Numero} lista.";
            return RedirectToAction(nameof(Ver), new { id = factura.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // Muestra el PDF guardado en el navegador
    public async Task<IActionResult> Ver(int id, bool descargar = false)
    {
        var factura = await _db.Facturas.Include(f => f.Venta!).ThenInclude(v => v.Pedido)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (factura == null) return NotFound();
        if (!EsAdmin && factura.Venta!.Pedido!.VendedorId != UsuarioId) return Forbid();

        if (factura.ArchivoPdf == null)
        {
            var venta = await _facturas.CargarVentaAsync(factura.VentaId);
            factura.ArchivoPdf = FacturaService.GenerarPdf(factura, venta!);
            await _db.SaveChangesAsync();
        }

        var nombre = $"{factura.Numero}.pdf";
        return descargar
            ? File(factura.ArchivoPdf, "application/pdf", nombre)
            : File(factura.ArchivoPdf, "application/pdf");
    }

    private Task<bool> PuedeVerVenta(int ventaId) =>
        EsAdmin
            ? Task.FromResult(true)
            : _db.Ventas.AnyAsync(v => v.Id == ventaId && v.Pedido!.VendedorId == UsuarioId);
}
