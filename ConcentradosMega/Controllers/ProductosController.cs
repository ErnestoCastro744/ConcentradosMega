using ConcentradosMega.Data;
using ConcentradosMega.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Controllers;

// Catálogo de productos. Todos los usuarios con acceso lo consultan;
// solo el Administrador crea, edita y desactiva (RN-03, RN-04).
[Authorize(Roles = "Administrador,Vendedor")]
public class ProductosController : Controller
{
    private readonly AppDbContext _db;

    public ProductosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string orden = "nombre", bool inactivos = false)
    {
        var esAdmin = User.IsInRole("Administrador");
        var query = _db.Productos.AsNoTracking().AsQueryable();

        // RN-04: el catálogo solo muestra productos activos; los inactivos los ve solo el Administrador
        if (!(esAdmin && inactivos)) query = query.Where(p => p.Estado);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p => p.Nombre.Contains(q) || p.Codigo.Contains(q));
        }

        query = orden switch
        {
            "precio" => query.OrderBy(p => p.Precio),
            "precio_desc" => query.OrderByDescending(p => p.Precio),
            "codigo" => query.OrderBy(p => p.Codigo),
            _ => query.OrderBy(p => p.Nombre)
        };

        ViewBag.Q = q;
        ViewBag.Orden = orden;
        ViewBag.Inactivos = esAdmin && inactivos;
        ViewBag.EsAdmin = esAdmin;
        return View(await query.ToListAsync());
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Create() => View(new Producto());

    [Authorize(Roles = "Administrador")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Codigo,Nombre,Descripcion,Precio,StockMinimo")] Producto producto)
    {
        producto.Codigo = (producto.Codigo ?? "").Trim().ToUpperInvariant();
        await ValidarAsync(producto, null);
        if (!ModelState.IsValid) return View(producto);

        producto.Estado = true;
        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Producto \"{producto.Nombre}\" creado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return producto is null ? NotFound() : View(producto);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, [Bind("Codigo,Nombre,Descripcion,Precio,StockMinimo")] Producto datos)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto is null) return NotFound();

        datos.Codigo = (datos.Codigo ?? "").Trim().ToUpperInvariant();
        await ValidarAsync(datos, id);

        if (!ModelState.IsValid)
        {
            datos.Id = id;
            return View(datos);
        }

        producto.Codigo = datos.Codigo;
        producto.Nombre = datos.Nombre;
        producto.Descripcion = datos.Descripcion;
        producto.Precio = datos.Precio;          // no afecta pedidos ya registrados (RN-07)
        producto.StockMinimo = datos.StockMinimo;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Producto \"{producto.Nombre}\" actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // RN-03: desactivar o reactivar, sin borrar (se conserva el historial)
    [Authorize(Roles = "Administrador")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto is null) return NotFound();

        producto.Estado = !producto.Estado;
        await _db.SaveChangesAsync();

        TempData["Ok"] = producto.Estado
            ? $"Producto \"{producto.Nombre}\" reactivado."
            : $"Producto \"{producto.Nombre}\" desactivado: ya no aparece en el catálogo.";
        return RedirectToAction(nameof(Index), new { inactivos = !producto.Estado });
    }

    private async Task ValidarAsync(Producto p, int? idActual)
    {
        if (p.Precio <= 0)
            ModelState.AddModelError(nameof(Producto.Precio), "El precio debe ser mayor a cero.");
        if (p.StockMinimo < 0)
            ModelState.AddModelError(nameof(Producto.StockMinimo), "El stock mínimo no puede ser negativo.");

        if (!string.IsNullOrEmpty(p.Codigo) &&
            await _db.Productos.AnyAsync(x => x.Codigo == p.Codigo && x.Id != idActual))
            ModelState.AddModelError(nameof(Producto.Codigo), "Ya existe un producto con ese código.");
    }
}
