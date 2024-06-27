using System.Data;
using System.Data.SqlClient;
using apbd_6.Exceptions;
using apbd_6.Models;

namespace apbd_6.Services;

public class WarehousesService : IWarehousesService
{
    private readonly IConfiguration _configuration;

    public WarehousesService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProduct(ProductWarehouse productWarehouse)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        using var command = new SqlCommand();
        command.Connection = connection;

        var idOrder = await GetOrderId(command, productWarehouse);
        var price = await GetProductPrice(command, productWarehouse.IdProduct);
        await ValidateWarehouseExistence(command, productWarehouse.IdWarehouse);

        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        command.Transaction = transaction;

        try
        {
            await UpdateOrderFulfilledAt(command, productWarehouse.CreatedAt, idOrder);
            await InsertProductWarehouse(command, productWarehouse, price, idOrder);
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw new Exception();
        }

        var idProductWarehouse = await GetProductWarehouseId(command);
        await connection.CloseAsync();

        return idProductWarehouse;
    }

    private async Task<int> GetOrderId(SqlCommand command, ProductWarehouse productWarehouse)
    {
        command.CommandText = "SELECT TOP 1 [Order].IdOrder FROM [Order] " +
                              "LEFT JOIN Product_Warehouse ON [Order].IdOrder = Product_Warehouse.IdOrder " +
                              "WHERE [Order].IdProduct = @IdProduct " +
                              "AND [Order].Amount = @Amount " +
                              "AND Product.Warehouse.IdProductWarehouse IS NULL " +
                              "AND [Order.CreatedAt] < @CreatedAt";

        command.Parameters.AddWithValue("@IdProduct", productWarehouse.IdProduct);
        command.Parameters.AddWithValue("@Amount", productWarehouse.Amount);
        command.Parameters.AddWithValue("@CreatedAt", productWarehouse.CreatedAt);

        var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows)
            throw new OrderNotFoundException();

        await reader.ReadAsync();
        int idOrder = (int)reader["IdOrder"];
        await reader.CloseAsync();
        command.Parameters.Clear();

        return idOrder;
    }

    private async Task<double> GetProductPrice(SqlCommand command, int idProduct)
    {
        command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", idProduct);

        var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) throw new ProductPriceNotFoundException();

        await reader.ReadAsync();
        double price = (double)reader["Price"];
        await reader.CloseAsync();
        command.Parameters.Clear();

        return price;
    }

    private async Task ValidateWarehouseExistence(SqlCommand command, int idWarehouse)
    {
        command.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);

        var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) throw new WarehouseNotFoundException();

        await reader.CloseAsync();
        command.Parameters.Clear();
    }

    private async Task UpdateOrderFulfilledAt(SqlCommand command, DateTime createdAt, int idOrder)
    {
        command.CommandText = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@CreatedAt", createdAt);
        command.Parameters.AddWithValue("@IdOrder", idOrder);

        int rowsUpdated = await command.ExecuteNonQueryAsync();
        if (rowsUpdated < 1) throw new UpdateFailedException();

        command.Parameters.Clear();
    }

    private async Task InsertProductWarehouse(SqlCommand command, ProductWarehouse productWarehouse, double price,
        int idOrder)
    {
        command.CommandText = "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, Amount, Price, CreatedAt) " +
                              $"VALUES(@IdWarehouse, @IdProduct, @Amount, @Amount * {price}, @CreatedAt)";
        command.Parameters.AddWithValue("@IdWarehouse", productWarehouse.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", productWarehouse.IdProduct);
        command.Parameters.AddWithValue("@Amount", productWarehouse.Amount);
        command.Parameters.AddWithValue("@CreatedAt", productWarehouse.CreatedAt);

        int rowsInserted = await command.ExecuteNonQueryAsync();
        if (rowsInserted < 1) throw new InsertProductFailedException();

        command.Parameters.Clear();
    }

    private async Task<int> GetProductWarehouseId(SqlCommand command)
    {
        command.CommandText = "SELECT TOP 1 IdProductWarehouse FROM Product_Warehouse ORDER BY IdProductWarehouse DESC";
        var reader = await command.ExecuteReaderAsync();

        await reader.ReadAsync();
        int idProductWarehouse = (int)reader["IdProductWarehouse"];
        await reader.CloseAsync();

        return idProductWarehouse;
    }

    public async Task<int> AddProductWithProcedure(ProductWarehouse productWarehouse)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        using var command = new SqlCommand("proc", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@IdProduct", productWarehouse.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", productWarehouse.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", productWarehouse.Amount);
        command.Parameters.AddWithValue("@CreatedAt", productWarehouse.CreatedAt);

        try
        {
            var newProductId = await command.ExecuteScalarAsync();
            return Convert.ToInt32(newProductId);
        }
        catch (Exception e)
        {
            throw new InsertProductFailedException();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
    
}