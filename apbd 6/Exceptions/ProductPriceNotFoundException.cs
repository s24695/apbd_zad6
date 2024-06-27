namespace apbd_6.Exceptions;

public class ProductPriceNotFoundException : Exception
{
    public ProductPriceNotFoundException() : base("Product not found") { }
}