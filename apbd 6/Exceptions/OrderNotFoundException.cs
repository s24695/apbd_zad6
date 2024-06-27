namespace apbd_6.Exceptions;

public class OrderNotFoundException : Exception
{
    public OrderNotFoundException() : base("Order not found") { }
}