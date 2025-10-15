using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using Car4You.DAL;

public class SitemapController : Controller
{
    private readonly CarDbContext _context;

    public SitemapController(CarDbContext context)
    {
        _context = context;
    }
    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        // ✅ Strony statyczne
        var urls = new List<XElement>
            {
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/"),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "1.0")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/CarList"),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Contact"),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.6")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/About"),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.5")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Privacy"),
                    new XElement(ns + "changefreq", "yearly"),
                    new XElement(ns + "priority", "0.3")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Rental"),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.6")
                )
            };

        // 🔹 Dynamiczne linki do szczegółów aut
        var cars = await _context.Cars
            .Where(c => c.IsHidden == false)
            .Select(c => new { c.Id, c.PublishDate })
            .ToListAsync();

        foreach (var car in cars)
        {
            urls.Add(
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Cars/Details/{car.Id}"),
                    new XElement(ns + "lastmod", car.PublishDate.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8")
                )
            );
        }

        var sitemap = new XDocument(new XElement(ns + "urlset", urls));
        return Content(sitemap.ToString(), "application/xml");
    }
}


