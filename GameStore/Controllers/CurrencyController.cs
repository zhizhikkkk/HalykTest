using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using GameStore.Data;
using GameStore.Models;
using System.Xml.Linq;
using System.Xml;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class CurrencyController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CurrencyController(ApplicationDbContext context)
    {
        _context = context;
    }


   [HttpGet("save/{date}")]
public async Task<IActionResult> SaveCurrencyData(string date)
{
    if (!DateTime.TryParse(date, out var parsedDate))
    {
        return BadRequest(new { message = "Invalid date format. Please use YYYY-MM-DD." });
    }

    var formattedDate = parsedDate.ToString("dd.MM.yyyy");
    var url = $"https://nationalbank.kz/rss/get_rates.cfm?fdate={formattedDate}";
    using var client = new HttpClient();
    var response = await client.GetStringAsync(url);

    Console.WriteLine(response); 

    try
    {
        var xdoc = XDocument.Parse(response);
        var rates = xdoc.Descendants("item")
            .Select(x => new Currency
            {
                TITLE = x.Element("fullname").Value,
                CODE = x.Element("title").Value,
                VALUE = decimal.Parse(x.Element("description").Value, CultureInfo.InvariantCulture), 
                A_DATE = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc) 
            }).ToList();

        foreach (var rate in rates)
        {
            var existingRate = await _context.Currencies
                .FirstOrDefaultAsync(r => r.A_DATE == rate.A_DATE && r.CODE == rate.CODE);

            if (existingRate != null)
            {
                existingRate.TITLE = rate.TITLE;
                existingRate.VALUE = rate.VALUE;
                _context.Currencies.Update(existingRate);
            }
            else
            {
                _context.Currencies.Add(rate);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { count = rates.Count });
    }
    catch (XmlException ex)
    {
        return BadRequest(new { message = "Invalid XML format", details = ex.Message });
    }
    catch (FormatException ex)
    {
        return BadRequest(new { message = "Invalid number format", details = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred", details = ex.ToString() }); 
    }
}





    [HttpGet("{date}/{code?}")]
    public async Task<IActionResult> GetCurrencyData(string date, string code = null)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { message = "Invalid date format. Please use YYYY-MM-DD." });
        }

        parsedDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

        var query = _context.Currencies.Where(c => c.A_DATE == parsedDate);

        if (!string.IsNullOrEmpty(code))
        {
            query = query.Where(c => c.CODE == code);
        }

        var result = await query.ToListAsync();

        if (!result.Any())
        {
            return NotFound(new { message = "No data found" });
        }

        return Ok(result);
    }

}