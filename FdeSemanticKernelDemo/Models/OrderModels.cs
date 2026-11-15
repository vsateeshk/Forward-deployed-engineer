namespace FdeSemanticKernelDemo.Models;

public record Order(
    string OrderId,
    string CustomerName,
    bool IsVip,
    string ItemName,
    decimal Amount,
    DateTime OrderDate,
    string Status);

public record SupportTicket(
    string TicketId,
    string OrderId,
    string Summary,
    string ProposedAction,
    bool RequiresManagerApproval);
