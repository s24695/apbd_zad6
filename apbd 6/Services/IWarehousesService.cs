using apbd_6.Models;

namespace apbd_6.Services;

public interface IWarehousesService
{
    public Task<int> AddProduct(ProductWarehouse productWarehouse);
}