namespace apbd_6.Exceptions;

public class WarehouseNotFoundException : Exception
{
    public WarehouseNotFoundException() : base("Warehouse not found")
    {
    }
}