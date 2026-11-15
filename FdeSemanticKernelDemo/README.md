# FDE Semantic Kernel Demo

This project demonstrates a real-world Forward-Deployed Engineer (FDE) scenario: integrating an AI agent into a legacy enterprise order system using Microsoft Semantic Kernel.

The fictional client is a retail company that already has a .NET order management system, customer support workflows, and business policy documents. The AI agent helps support teams triage incoming customer emails by:

- checking the order details in the legacy system,
- reading the return policy from an internal document,
- deciding whether the request is eligible,
- and escalating to a human if policy or approval rules require it.

This is very close to real FDE work: connect AI to the systems customers already use, while respecting business rules, data ownership, and operational safety.

## What this demo shows

- **Legacy system integration**: the app reads real order data from a simulated repository.
- **RAG pattern**: the AI checks the company return policy document before making decisions.
- **Tool calling / plugins**: Semantic Kernel automatically invokes C# functions when they are relevant.
- **Safety guardrails**: high-value or policy-sensitive cases are escalated instead of being auto-approved.
- **Console traceability**: logs show which plugin methods were called and why.

## Project structure

- `Program.cs` - console entry point and AI orchestration
- `Services/LegacyOrderDatabase.cs` - simulated legacy customer order data
- `Plugins/EnterpriseOrderPlugin.cs` - plugin methods used by Semantic Kernel
- `Models/OrderModels.cs` - order and ticket data models
- `Data/CompanyReturnPolicy.txt` - company return policy used as knowledge grounding
- `appsettings.json` - Azure OpenAI / OpenAI configuration placeholders

## How this maps to Forward-Deployed Engineering

In many enterprise client engagements, the hard problem is not building AI itself. The hard problem is:

- connecting AI to old but critical systems,
- grounding it in internal policy documents,
- enforcing approval paths,
- and making sure the solution is useful, safe, and production-friendly.

This demo is intentionally simple, but it mirrors real FDE conversations with clients:

- "We have a working order system, but it is old and not AI-ready."
- "We do not want the AI making decisions outside policy."
- "We need a secure path to look at order data and summarize the next action."
- "We need a human approval escalation for high-value exceptions."

That is exactly the kind of problem FDEs help solve.

## Prerequisites

- .NET 8 SDK
- An Azure OpenAI deployment or OpenAI API key

## Step-by-step: add your key and run

1. Open `appsettings.json`.
2. Replace the placeholder values with your Azure OpenAI or OpenAI key.
3. If using Azure OpenAI, fill in:
   - `AzureOpenAI:Endpoint`
   - `AzureOpenAI:ApiKey`
   - `AzureOpenAI:DeploymentName`
4. If using OpenAI directly, fill in:
   - `OpenAI:ApiKey`
   - `OpenAI:ModelId`
5. Save the file.
6. Run the following commands from the project folder:

```bash
dotnet restore
dotnet build
dotnet run
```

## Use the demo

You will see a small interactive menu with sample customer emails. You can:

- choose one of the prepared scenarios,
- or type your own email message.

The agent will evaluate the request by checking:

- the order metadata,
- the return policy,
- and whether a manager must be involved.

## How to trace the "thought process"

This project is designed to make the Semantic Kernel function calls visible in the console.

Look for these patterns in the output:

- plugin method logs such as `GetOrderDetailsAsync called...`
- policy lookups from the return policy file
- escalation logs when the agent sees a policy exception or high-value issue
- Semantic Kernel runtime logs showing function selection and execution

The important learning point is that the model is not just generating free text. It is using structured tools to gather evidence before answering.

## Why the plugin attributes matter

The methods inside `EnterpriseOrderPlugin` use:

- `[KernelFunction]` to tell Semantic Kernel that this method is callable,
- `[Description]` to help the model understand when to call it.

This is a crucial FDE concept: good plugin metadata makes the agent more reliable and easier to reason about.

## Why FunctionChoiceBehavior.Auto() matters

This demo uses:

```csharp
FunctionChoiceBehavior.Auto()
```

This lets the LLM decide when to call plugin functions automatically. That is a realistic pattern for enterprise AI agents: the model asks for tools when it needs facts, not just for every prompt.

## Recommended next ideas

- Add a database-backed repository instead of hardcoded sample data
- Add an email intake service that reads actual messages from a queue
- Add policy versioning and audit logging
- Extend the plugin with a more advanced approval workflow
- Add unit tests around your plugin logic and policy decisions

## Useful learning links

- Microsoft Learn: Semantic Kernel
- Microsoft Learn: Azure OpenAI
- Microsoft Learn: .NET and ASP.NET Core
- Microsoft Learn: Azure Functions and app integration patterns

---

This demo is intentionally beginner-friendly. The goal is to help .NET developers understand how AI agents are connected to business rules and enterprise systems in a safe, practical way.
