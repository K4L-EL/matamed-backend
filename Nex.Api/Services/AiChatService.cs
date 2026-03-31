using System.ClientModel;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Nex.Api.Models;
using OpenAI.Chat;

namespace Nex.Api.Services;

public interface IAiChatService
{
    Task<string> ChatAsync(string query);
}

public class AiChatService : IAiChatService
{
    private readonly ChatClient? _chatClient;
    private readonly IDashboardService _dashboard;
    private readonly IInfectionService _infections;
    private readonly IPatientService _patients;
    private readonly IOutbreakService _outbreaks;
    private readonly IAlertService _alerts;
    private readonly IResistanceService _resistance;
    private readonly ITransmissionService _transmission;
    private readonly IScreeningService _screening;
    private readonly IDeviceService _devices;
    private readonly ILogger<AiChatService> _logger;

    private const string SystemPrompt = """
        You are MetaMed AI, an intelligent clinical decision-support assistant for hospital 
        infection prevention and control (IPC) teams. You have access to real-time dashboard 
        data including active infections, patient risk scores, outbreak clusters, antimicrobial 
        resistance patterns, screening compliance, device-associated infections, transmission 
        networks, and alerts.

        Your role:
        - Analyse the provided hospital data and answer questions accurately
        - Identify trends, risks, and anomalies
        - Provide actionable clinical recommendations grounded in IPC best practices
        - Draw conclusions from the data and explain your reasoning
        - Suggest preventive measures when appropriate
        - Be concise but thorough; use bullet points and bold for key figures

        Format responses using markdown: **bold** for emphasis, bullet points for lists, 
        and numbered lists for action items. Do not use emojis.
        Always reference specific numbers from the data to support your analysis.
        """;

    public AiChatService(
        IConfiguration configuration,
        IDashboardService dashboard,
        IInfectionService infections,
        IPatientService patients,
        IOutbreakService outbreaks,
        IAlertService alerts,
        IResistanceService resistance,
        ITransmissionService transmission,
        IScreeningService screening,
        IDeviceService devices,
        ILogger<AiChatService> logger)
    {
        _dashboard = dashboard;
        _infections = infections;
        _patients = patients;
        _outbreaks = outbreaks;
        _alerts = alerts;
        _resistance = resistance;
        _transmission = transmission;
        _screening = screening;
        _devices = devices;
        _logger = logger;

        var endpoint = configuration["AZURE_OPENAI_ENDPOINT"];
        var key = configuration["AZURE_OPENAI_KEY"];
        var deployment = configuration["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-4o";

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
        {
            var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
            _chatClient = client.GetChatClient(deployment);
            _logger.LogInformation("Azure OpenAI configured: {Endpoint}, deployment: {Deployment}", endpoint, deployment);
        }
        else
        {
            _logger.LogWarning("Azure OpenAI not configured — AI chat will use fallback responses");
        }
    }

    public async Task<string> ChatAsync(string query)
    {
        var context = await GatherContextAsync();

        if (_chatClient is null)
            return GenerateFallback(query, context);

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($"""
                    ## Current Hospital Data
                    
                    {context}

                    ## User Question
                    
                    {query}
                    """),
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1500,
                Temperature = 0.3f,
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI call failed, using fallback");
            return GenerateFallback(query, context);
        }
    }

    private async Task<string> GatherContextAsync()
    {
        var sb = new StringBuilder();
        var opts = new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        try
        {
            var summary = await _dashboard.GetSummaryAsync();
            sb.AppendLine($"### Dashboard Summary");
            sb.AppendLine($"Active Infections: {summary.ActiveInfections}, Patients at Risk: {summary.PatientsAtRisk}, " +
                          $"Active Outbreaks: {summary.ActiveOutbreaks}, Pending Alerts: {summary.PendingAlerts}, " +
                          $"Avg Risk Score: {summary.RiskScoreAverage:P0}, Infection Rate Change: {summary.InfectionRateChange:+0.0;-0.0}%");
        }
        catch { /* non-critical */ }

        try
        {
            var infections = await _infections.GetAllAsync();
            var active = infections.Where(i => i.Status == "Active").ToList();
            sb.AppendLine($"\n### Active Infections ({active.Count})");
            foreach (var inf in active.Take(15))
                sb.AppendLine($"- {inf.Organism} | {inf.Severity} | {inf.Ward} | HAI: {inf.IsHai} | {inf.Type}");
        }
        catch { /* non-critical */ }

        try
        {
            var patients = await _patients.GetAllAsync();
            var highRisk = patients.Where(p => p.RiskScore > 0.5).OrderByDescending(p => p.RiskScore).Take(10).ToList();
            sb.AppendLine($"\n### High-Risk Patients ({highRisk.Count} of {patients.Count} total)");
            foreach (var p in highRisk)
                sb.AppendLine($"- {p.Name}: {p.RiskScore:P0} risk | {p.Ward} | {p.Status} | Organisms: {string.Join(", ", p.Organisms)} | Infections: {p.ActiveInfections}");
        }
        catch { /* non-critical */ }

        try
        {
            var outbreaks = await _outbreaks.GetAllAsync();
            var active = outbreaks.Where(o => o.Status == "Active").ToList();
            sb.AppendLine($"\n### Active Outbreaks ({active.Count})");
            foreach (var o in active)
                sb.AppendLine($"- {o.Organism} in {o.Location} | {o.Severity} | {o.AffectedPatients} patients | {o.InvestigationStatus}");
        }
        catch { /* non-critical */ }

        try
        {
            var alerts = await _alerts.GetAllAsync();
            var unread = alerts.Where(a => !a.IsRead).ToList();
            var critical = alerts.Where(a => a.Severity == "Critical").ToList();
            sb.AppendLine($"\n### Alerts ({alerts.Count} total, {unread.Count} unread, {critical.Count} critical)");
            foreach (var a in critical.Take(5))
                sb.AppendLine($"- [{a.Severity}] {a.Title}: {a.Description}");
        }
        catch { /* non-critical */ }

        try
        {
            var resistance = await _resistance.GetSummariesAsync();
            sb.AppendLine($"\n### Resistance Patterns ({resistance.Count} organisms)");
            foreach (var r in resistance)
            {
                var rising = r.Patterns.Where(p => p.Trend == "Rising").Select(p => p.Antibiotic).ToList();
                sb.AppendLine($"- {r.Organism}: MDR {r.MdrRate:P0} ({r.TotalIsolates} isolates){(rising.Any() ? $" | Rising: {string.Join(", ", rising)}" : "")}");
            }
        }
        catch { /* non-critical */ }

        try
        {
            var network = await _transmission.GetNetworkAsync();
            sb.AppendLine($"\n### Transmission Network");
            sb.AppendLine($"Organism: {network.Organism} | Cases: {network.TotalCases} | Nodes: {network.Nodes.Count} | Links: {network.Links.Count}");
        }
        catch { /* non-critical */ }

        try
        {
            var screening = await _screening.GetComplianceAsync();
            var avg = screening.Count > 0 ? screening.Average(s => s.ComplianceRate) : 0;
            var overdue = screening.Sum(s => s.Overdue);
            sb.AppendLine($"\n### Screening Compliance");
            sb.AppendLine($"Average: {avg:P0} | Total Overdue: {overdue}");
            foreach (var s in screening.Where(s => s.ComplianceRate < 0.8))
                sb.AppendLine($"- {s.Ward}: {s.ComplianceRate:P0} ({s.Overdue} overdue)");
        }
        catch { /* non-critical */ }

        try
        {
            var devices = await _devices.GetSummariesAsync();
            sb.AppendLine($"\n### Device Infections ({devices.Count} types)");
            foreach (var d in devices)
                sb.AppendLine($"- {d.DeviceType}: {d.Infections}/{d.TotalDevices} ({d.InfectionRate:F1}% rate)");
        }
        catch { /* non-critical */ }

        return sb.ToString();
    }

    private static string GenerateFallback(string query, string context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("**MetaMed AI** (offline mode — Azure OpenAI not configured)\n");
        sb.AppendLine("I have access to the current dashboard data. Here is a summary:\n");

        foreach (var line in context.Split('\n').Where(l => l.StartsWith("###") || l.StartsWith("Active ") || l.StartsWith("Average")))
            sb.AppendLine(line.Replace("###", "**").TrimEnd() + (line.StartsWith("###") ? "**" : ""));

        sb.AppendLine("\n*To enable full AI analysis, configure AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_KEY environment variables.*");
        return sb.ToString();
    }
}
