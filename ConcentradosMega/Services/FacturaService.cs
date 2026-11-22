using System.Globalization;
using ConcentradosMega.Data;
using ConcentradosMega.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConcentradosMega.Services;

// ECU-02: Factura en PDF
public class FacturaService
{
    public const string Leyenda = "Documento comercial interno. No sustituye la factura electrónica.";
    private static readonly CultureInfo Cr = CultureInfo.GetCultureInfo("es-CR");

    private readonly AppDbContext _db;

    public FacturaService(AppDbContext db) => _db = db;

    /// <summary>
    /// Devuelve la factura de la venta. Si ya existe, la reutiliza (RN-12);
    /// si no, asigna el siguiente consecutivo, genera el PDF y la guarda.
    /// </summary>
    public async Task<Factura> ObtenerOGenerarAsync(int ventaId)
    {
        var existente = await _db.Facturas.FirstOrDefaultAsync(f => f.VentaId == ventaId);
        if (existente != null) return existente;

        var venta = await CargarVentaAsync(ventaId)
            ?? throw new InvalidOperationException("La venta no existe.");

        if (venta.Pedido!.Estado != EstadosPedido.Confirmado)
            throw new InvalidOperationException("Solo se facturan pedidos confirmados.");

        for (var intento = 0; intento < 3; intento++)
        {
            var siguiente = (await _db.Facturas.MaxAsync(f => (int?)f.Consecutivo) ?? 0) + 1;
            var factura = new Factura { VentaId = ventaId, Consecutivo = siguiente, FechaEmision = DateTime.Now };
            factura.ArchivoPdf = GenerarPdf(factura, venta);
            _db.Facturas.Add(factura);
            try
            {
                await _db.SaveChangesAsync();
                return factura;
            }
            catch (DbUpdateException)
            {
                // Otro usuario facturó al mismo tiempo: los índices UNIQUE lo evitaron.
                _db.Entry(factura).State = EntityState.Detached;
                existente = await _db.Facturas.FirstOrDefaultAsync(f => f.VentaId == ventaId);
                if (existente != null) return existente;
            }
        }
        throw new InvalidOperationException("No se pudo asignar el consecutivo. Intente de nuevo.");
    }

    public Task<Venta?> CargarVentaAsync(int ventaId) =>
        _db.Ventas
            .Include(v => v.Pagos)
            .Include(v => v.Pedido!).ThenInclude(p => p.Cliente)
            .Include(v => v.Pedido!).ThenInclude(p => p.Vendedor)
            .Include(v => v.Pedido!).ThenInclude(p => p.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

    public static string Colones(decimal monto) => "₡" + monto.ToString("N2", Cr);

    public static byte[] GenerarPdf(Factura factura, Venta venta)
    {
        var pedido = venta.Pedido!;
        var cliente = pedido.Cliente!;
        var pagado = venta.Pagos.Where(p => p.Estado == EstadosPago.Aprobado).Sum(p => p.Monto);
        var saldo = venta.Total - pagado;
        const string verde = "#1E6B3A";

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Concentrados Mega").FontSize(20).Bold().FontColor(verde);
                    c.Item().Text("Venta y distribución de alimentos para animales");
                    c.Item().Text("Tel. 2222-0000 · ventas@concentradosmega.com");
                });
                row.ConstantItem(170).Border(1).BorderColor(verde).Padding(8).Column(c =>
                {
                    c.Item().AlignCenter().Text("FACTURA").Bold().FontSize(14).FontColor(verde);
                    c.Item().AlignCenter().Text(factura.Numero).Bold();
                    c.Item().AlignCenter().Text(factura.FechaEmision.ToString("dd/MM/yyyy HH:mm"));
                });
            });

            page.Content().PaddingVertical(16).Column(col =>
            {
                col.Spacing(12);

                col.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Cliente").Bold();
                        c.Item().Text(cliente.Nombre);
                        c.Item().Text($"Cédula: {cliente.Cedula}");
                        if (!string.IsNullOrWhiteSpace(cliente.Direccion)) c.Item().Text(cliente.Direccion);
                        if (!string.IsNullOrWhiteSpace(cliente.Telefono)) c.Item().Text($"Tel. {cliente.Telefono}");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Datos de la venta").Bold();
                        c.Item().Text($"Pedido #{pedido.Id} · Venta #{venta.Id}");
                        c.Item().Text($"Fecha del pedido: {pedido.Fecha:dd/MM/yyyy}");
                        c.Item().Text($"Vendedor: {pedido.Vendedor?.NombreCompleto}");
                    });
                });

                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(60);
                        c.RelativeColumn();
                        c.ConstantColumn(55);
                        c.ConstantColumn(90);
                        c.ConstantColumn(95);
                    });

                    t.Header(h =>
                    {
                        foreach (var titulo in new[] { "Código", "Producto", "Cant.", "Precio", "Subtotal" })
                            h.Cell().Background(verde).Padding(5).Text(titulo).FontColor(Colors.White).Bold();
                    });

                    foreach (var d in pedido.Detalles.OrderBy(d => d.Id))
                    {
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.Producto?.Codigo);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.Producto?.Nombre);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(d.Cantidad.ToString());
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Colones(d.PrecioUnitario));
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Colones(d.Subtotal));
                    }
                });

                col.Item().AlignRight().Width(230).Column(c =>
                {
                    c.Item().Row(r => { r.RelativeItem().Text("Total").Bold().FontSize(12); r.RelativeItem().AlignRight().Text(Colones(venta.Total)).Bold().FontSize(12); });
                    c.Item().Row(r => { r.RelativeItem().Text("Pagado"); r.RelativeItem().AlignRight().Text(Colones(pagado)); });
                    c.Item().Row(r => { r.RelativeItem().Text("Saldo al emitir"); r.RelativeItem().AlignRight().Text(Colones(saldo)); });
                });
            });

            page.Footer().Column(c =>
            {
                c.Item().BorderTop(1).BorderColor(verde).PaddingTop(6).AlignCenter()
                    .Text(Leyenda).Italic().FontColor(Colors.Grey.Darken2);
                c.Item().AlignCenter().Text(t =>
                {
                    t.Span("Página ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" de ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        })).GeneratePdf();
    }
}
