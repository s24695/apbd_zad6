using apbd_6.Models;

namespace apbd_6.Services;

public interface IWarehousesService
{
    Task<int> AddProduct(ProductWarehouse productWarehouse);
    Task<int> AddProductWithProcedure(ProductWarehouse productWarehouse);
}