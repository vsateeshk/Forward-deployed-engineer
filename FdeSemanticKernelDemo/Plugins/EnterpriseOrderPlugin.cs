using System.ComponentModel;
using FdeSemanticKernelDemo.Models;
using FdeSemanticKernelDemo.Services;
using Microsoft.SemanticKernel;

namespace FdeSemanticKernelDemo.Plugins;

public class EnterpriseOrderPlugin
{
    private readonly LegacyOrderDatabase _legacyOrderDatabase;
    private readonly string _policyPath;

    public EnterpriseOrderPlugin(LegacyOrderDatabase legacyOrderDatabase)
    {
        _legacyOrderDatabase = legacyOrderDatabase;

        var appDirectory = AppContext.BaseDirectory;
        var directPath = Path.Combine(appDirectory, "Data", "CompanyReturnPolicy.txt");

        if (!File.Exists(directPath))
        {
            directPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "CompanyReturnPolicy.txt");
        }

        _policyPath = directPath;
    }

    [KernelFunction("get_order_details")]
    [Description("Gets the order details for a given order ID from the client's legacy enterprise order system. Use this before recommending any return, refund, or escalation action.")]
    public async Task<string> GetOrderDetailsAsync(string orderId)
    {
        Console.WriteLine($"[Plugin] GetOrderDetailsAsync called for OrderId='{orderId}'");

        var order = _legacyOrderDatabase.GetOrderById(orderId);
        if (order is null)
        {
            return $"No order found for '{orderId}'. Please double-check the order ID before making a decision.";
        }

        var summary = $"OrderId={order.OrderId}; Customer={order.CustomerName}; VIP={order.IsVip}; Item={order.ItemName}; Amount=${order.Amount:F2}; Date={order.OrderDate:yyyy-MM-dd}; Status={order.Status}";

        Console.WriteLine($"[Plugin] Found order: {summary}");
        await Task.CompletedTask;
        return summary;
    }

    [KernelFunction("check_return_policy")]
    [Description("Reads the company's internal return policy document to ground the AI response in official policy rules. This is the RAG-style knowledge check for support decisions.")]
    public async Task<string> CheckReturnPolicyAsync(string query)
    {
        Console.WriteLine("[Plugin] CheckReturnPolicyAsync called. Reading policy document.");

        if (!File.Exists(_policyPath))
        {
            return "Return policy file was not found. Please confirm the file exists in the Data directory.";
        }

        var policyText = await File.ReadAllTextAsync(_policyPath);
        Console.WriteLine($"[Plugin] Policy loaded from {_policyPath}");

        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return policyText;
        }

        // A simple keyword-based grounding step for beginners: keep it readable and easy to understand.
        // In a real client environment this could be replaced with a more advanced search/index layer.
        var policyLines = policyText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                           normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                               .Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (policyLines.Count == 0)
        {
            return policyText;
        }

        return string.Join(Environment.NewLine, policyLines);
    }

    [KernelFunction("create_escalation_ticket")]
    [Description("Creates a structured escalation ticket when approval is required or when policy rules do not allow a direct customer resolution. This is a safety guardrail for human review.")]
    public async Task<string> CreateEscalationTicketAsync(string orderId, string reason, string draftedReply)
    {
        Console.WriteLine($"[Plugin] CreateEscalationTicketAsync called for OrderId='{orderId}'");

        var ticket = _legacyOrderDatabase.CreateEscalationTicket(orderId, reason, draftedReply);

        var summary = $"TicketId={ticket.TicketId}; OrderId={ticket.OrderId}; RequiresManagerApproval={ticket.RequiresManagerApproval}; Summary={ticket.Summary}; ProposedAction={ticket.ProposedAction}";

        Console.WriteLine($"[Plugin] Escalation ticket created: {summary}");
        await Task.CompletedTask;
        return summary;
    }
}
