using ConcentradosMega.Data;
using ConcentradosMega.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcentradosMega.Controllers;

// Gestión de clientes. RN-03: los clientes se desactivan, nunca se borran.
[Authorize(Roles = "Administrador,Vendedor")]
public class ClientesController : Controller
{
    private readonly AppDbContext _db;

    public ClientesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, bool inactivos = false)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();

        if (!inactivos) query = query.Where(c => c.Estado);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(c => c.Nombre.Contains(q) || c.Cedula.Contains(q));
        }

        ViewBag.Q = q;
        ViewBag.Inactivos = inactivos;
        return View(await query.OrderBy(c => c.Nombre).ToListAsync());
    }

    public IActionResult Create() => View(new Cliente());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Cedula,Nombre,Telefono,Correo,Direccion")] Cliente cliente)
    {
        cliente.Cedula = (cliente.Cedula ?? "").Trim();

        if (await _db.Clientes.AnyAsync(c => c.Cedula == cliente.Cedula))
            ModelState.AddModelError(nameof(Cliente.Cedula), "Ya existe un cliente con esa cédula.");

        if (!ModelState.IsValid) return View(cliente);

        cliente.Estado = true;
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Cliente \"{cliente.Nombre}\" creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Cedula,Nombre,Telefono,Correo,Direccion")] Cliente datos)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return NotFound();

        datos.Cedula = (datos.Cedula ?? "").Trim();

        if (await _db.Clientes.AnyAsync(c => c.Cedula == datos.Cedula && c.Id != id))
            ModelState.AddModelError(nameof(Cliente.Cedula), "Ya existe otro cliente con esa cédula.");

        if (!ModelState.IsValid)
        {
            datos.Id = id;
            return View(datos);
        }

        cliente.Cedula = datos.Cedula;
        cliente.Nombre = datos.Nombre;
        cliente.Telefono = datos.Telefono;
        cliente.Correo = datos.Correo;
        cliente.Direccion = datos.Direccion;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Cliente \"{cliente.Nombre}\" actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // RN-03: desactivar o reactivar, sin borrar (se conserva el historial de pedidos)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return NotFound();

        cliente.Estado = !cliente.Estado;
        await _db.SaveChangesAsync();

        TempData["Ok"] = cliente.Estado
            ? $"Cliente \"{cliente.Nombre}\" reactivado."
            : $"Cliente \"{cliente.Nombre}\" desactivado.";
        return RedirectToAction(nameof(Index), new { inactivos = !cliente.Estado });
    }
}
