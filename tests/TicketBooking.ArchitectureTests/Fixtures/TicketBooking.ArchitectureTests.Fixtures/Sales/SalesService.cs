using TicketBooking.Customers.Core;

namespace TicketBooking.Sales.Core;

public class SalesService
{
    public CustomerRepository Customers { get; } = new();
}
