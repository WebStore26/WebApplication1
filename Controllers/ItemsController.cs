using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebShop.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : Controller
{
    private readonly AppDb _db;

    public ItemsController(AppDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.Shop_Items
            .Where(i => i.IsActive)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Price
            })
            .ToListAsync();

        return Ok(items);
    }
}