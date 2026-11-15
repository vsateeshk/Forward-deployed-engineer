using FdeSemanticKernelDemo.Models;

namespace FdeSemanticKernelDemo.Services;

public class LegacyOrderDatabase
{
    private readonly List<Order> _orders;
    private readonly List<SupportTicket> _tickets;

    public LegacyOrderDatabase()
    {
        _orders = new List<Order>
        {
            new(
                "ORD-1001",
                "Alicia Gomez",
                true,
                "Wireless Noise-Canceling Headphones",
                299.99m,
                new DateTime(2026, 10, 12),
                "Delivered"),
            new(
                "ORD-1002",
                "Marcus Lee",
                false,
                "Office Chair",
                179.00m,
                new DateTime(2026, 09, 15),
                "Delivered"),
            new(
                "ORD-1003",
                "Priya Shah",
                true,
                "Smartphone Case",
                49.99m,
                new DateTime(2026, 11, 01),
                "Delivered"),
            new(
                "ORD-1004",
                "David Chen",
                false,
                "4K Monitor",
                899.00m,
                new DateTime(2026, 10, 28),
                "Delivered")
        };

        _tickets = new List<SupportTicket>();
    }

    public Order? GetOrderById(string orderId)
    {
        return _orders.FirstOrDefault(o => string.Equals(o.OrderId, orderId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<Order> GetAllOrders()
    {
        return _orders;
    }

    public SupportTicket CreateEscalationTicket(string orderId, string reason, string draftedReply)
    {
        var ticketId = $"TKT-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var ticket = new SupportTicket(
            ticketId,
            orderId,
            reason,
            draftedReply,
            true);

        _tickets.Add(ticket);
        return ticket;
    }

    public IReadOnlyList<SupportTicket> GetEscalationTickets()
    {
        return _tickets;
    }
}
