using apbd_6.Models;
using apbd_6.Services;
using Microsoft.AspNetCore.Mvc;


namespace apbd_6.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehousesController : ControllerBase
{
    private readonly IWarehousesService _warehousesService;

    public WarehousesController(IWarehousesService warehousesService)
    {
        _warehousesService = warehousesService;
    }

    [HttpPost]
    public async Task<ActionResult> AddProduct(ProductWarehouse product)
    {
        int result = await _warehousesService.AddProduct(product);
        return Ok();
    }
}