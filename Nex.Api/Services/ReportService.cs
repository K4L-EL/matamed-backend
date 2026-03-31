using System.ClientModel;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Nex.Api.Models;
using OpenAI.Chat;

namespace Nex.Api.Services;

public interface IReportService
{
    Task<ReportResponse> GenerateReportAsync(ReportRequest request);
}

public class ReportService : IReportService
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
    private readonly ILogger<ReportService> _logger;

    private const string ReportSystemPrompt = """
        You are MetaMed AI, generating a professional infection prevention and control (IPC) 
        surveillance report for a hospital, inspired by the ESPAUR reporting standard from UKHSA.

        For each section you are given structured hospital data. Write a professional narrative 
        paragraph (3-6 sentences) that:
        - States the key findings with specific numbers
        - Identifies trends, risks, and anomalies
        - Compares current figures to expected benchmarks where possible
        - Uses clinical IPC terminology appropriately
        - Is suitable for a senior clinical audience

        Return ONLY the narrative text, no markdown headers. Write in third person, formal tone.
        Reference specific data points. Do not use emojis. Be precise and evidence-based.
        """;

    public ReportService(
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
        ILogger<ReportService> logger)
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
        }
    }

    public async Task<ReportResponse> GenerateReportAsync(ReportRequest request)
    {
        var summary = await _dashboard.GetSummaryAsync();
        var infections = await _infections.GetAllAsync();
        var patients = await _patients.GetAllAsync();
        var outbreaks = await _outbreaks.GetAllAsync();
        var alerts = await _alerts.GetAllAsync();
        var resistanceSummaries = await _resistance.GetSummariesAsync();
        var network = await _transmission.GetNetworkAsync();
        var screening = await _screening.GetComplianceAsync();
        var devices = await _devices.GetSummariesAsync();
        var deviceInfections = await _devices.GetInfectionsAsync();

        var period = request.DateRange ?? $"March 2026";

        var execSummary = BuildExecutiveSummary(summary, infections, patients, outbreaks, alerts);
        var infectionBurden = BuildInfectionBurden(infections);
        var resistanceSection = BuildResistancePatterns(resistanceSummaries);
        var patientRisk = BuildPatientRisk(patients);
        var outbreakSection = BuildOutbreakAnalysis(outbreaks);
        var screeningSection = BuildScreeningCompliance(screening);
        var deviceSection = BuildDeviceInfections(devices, deviceInfections);
        var transmissionSection = BuildTransmissionAnalysis(network);

        var narrativeTasks = new Dictionary<string, Task<string>>
        {
            ["executive"] = GenerateNarrative("Executive Summary", FormatExecutiveSummaryContext(summary, infections, patients, outbreaks)),
            ["infections"] = GenerateNarrative("Burden of Healthcare-Associated Infections", FormatInfectionContext(infections)),
            ["resistance"] = GenerateNarrative("Antimicrobial Resistance Patterns", FormatResistanceContext(resistanceSummaries)),
            ["patients"] = GenerateNarrative("Patient Risk Analysis", FormatPatientContext(patients)),
            ["outbreaks"] = GenerateNarrative("Outbreak Analysis", FormatOutbreakContext(outbreaks)),
            ["screening"] = GenerateNarrative("Screening Compliance", FormatScreeningContext(screening)),
            ["devices"] = GenerateNarrative("Device-Associated Infections", FormatDeviceContext(devices, deviceInfections)),
            ["transmission"] = GenerateNarrative("Transmission Network Analysis", FormatTransmissionContext(network)),
            ["recommendations"] = GenerateRecommendations(summary, infections, outbreaks, resistanceSummaries, screening),
        };

        await Task.WhenAll(narrativeTasks.Values);

        execSummary = execSummary with { Narrative = await narrativeTasks["executive"] };
        infectionBurden = infectionBurden with { Narrative = await narrativeTasks["infections"] };
        resistanceSection = resistanceSection with { Narrative = await narrativeTasks["resistance"] };
        patientRisk = patientRisk with { Narrative = await narrativeTasks["patients"] };
        outbreakSection = outbreakSection with { Narrative = await narrativeTasks["outbreaks"] };
        screeningSection = screeningSection with { Narrative = await narrativeTasks["screening"] };
        deviceSection = deviceSection with { Narrative = await narrativeTasks["devices"] };
        transmissionSection = transmissionSection with { Narrative = await narrativeTasks["transmission"] };

        var recommendations = new ReportSection(
            "Recommendations & Action Plan",
            await narrativeTasks["recommendations"],
            new List<ReportMetric>(),
            new List<ReportChartData>(),
            new List<ReportTable>()
        );

        return new ReportResponse(
            "MetaMed IPC Surveillance Report",
            DateTime.UtcNow.ToString("o"),
            period,
            execSummary,
            infectionBurden,
            resistanceSection,
            patientRisk,
            outbreakSection,
            screeningSection,
            deviceSection,
            transmissionSection,
            recommendations
        );
    }

    private ReportExecutiveSummary BuildExecutiveSummary(
        DashboardSummary summary, List<Infection> infections, List<Patient> patients,
        List<Outbreak> outbreaks, List<Alert> alerts)
    {
        var activeInf = infections.Count(i => i.Status == "Active");
        var haiCount = infections.Count(i => i.IsHai);
        var criticalAlerts = alerts.Count(a => a.Severity == "Critical");

        return new ReportExecutiveSummary(
            "",
            new List<ReportMetric>
            {
                new("Active Infections", activeInf.ToString(), $"{summary.InfectionRateChange:+0.0;-0.0}%"),
                new("Patients at Risk", summary.PatientsAtRisk.ToString()),
                new("HAI Rate", $"{(infections.Count > 0 ? (double)haiCount / infections.Count * 100 : 0):F1}%"),
                new("Active Outbreaks", summary.ActiveOutbreaks.ToString(), Severity: outbreaks.Any(o => o.Severity == "Critical") ? "critical" : "warning"),
                new("Avg Risk Score", $"{summary.RiskScoreAverage:P0}"),
                new("Critical Alerts", criticalAlerts.ToString(), Severity: criticalAlerts > 0 ? "critical" : "ok"),
                new("Total Patients", patients.Count.ToString()),
                new("Pending Alerts", summary.PendingAlerts.ToString()),
            },
            new List<ReportChartData>
            {
                new("pie", "Infections by Severity",
                    new List<string> { "Critical", "High", "Medium", "Low" },
                    new List<ReportDataSeries>
                    {
                        new("Count", new List<double>
                        {
                            infections.Count(i => i.Severity == "Critical"),
                            infections.Count(i => i.Severity == "High"),
                            infections.Count(i => i.Severity == "Medium"),
                            infections.Count(i => i.Severity == "Low"),
                        })
                    }),
            }
        );
    }

    private ReportSection BuildInfectionBurden(List<Infection> infections)
    {
        var active = infections.Where(i => i.Status == "Active").ToList();
        var byOrganism = active.GroupBy(i => i.Organism).OrderByDescending(g => g.Count()).ToList();
        var byWard = active.GroupBy(i => i.Ward).OrderByDescending(g => g.Count()).ToList();
        var byType = active.GroupBy(i => i.Type).OrderByDescending(g => g.Count()).ToList();

        return new ReportSection(
            "Burden of Healthcare-Associated Infections",
            "",
            new List<ReportMetric>
            {
                new("Total Active", active.Count.ToString()),
                new("HAI Count", active.Count(i => i.IsHai).ToString()),
                new("Most Common Organism", byOrganism.FirstOrDefault()?.Key ?? "N/A"),
                new("Highest Burden Ward", byWard.FirstOrDefault()?.Key ?? "N/A"),
            },
            new List<ReportChartData>
            {
                new("bar", "Active Infections by Organism",
                    byOrganism.Select(g => g.Key).ToList(),
                    new List<ReportDataSeries> { new("Infections", byOrganism.Select(g => (double)g.Count()).ToList()) }),
                new("bar", "Active Infections by Ward",
                    byWard.Select(g => g.Key).ToList(),
                    new List<ReportDataSeries> { new("Infections", byWard.Select(g => (double)g.Count()).ToList()) }),
                new("pie", "Infections by Type",
                    byType.Select(g => g.Key).ToList(),
                    new List<ReportDataSeries> { new("Count", byType.Select(g => (double)g.Count()).ToList()) }),
            },
            new List<ReportTable>
            {
                new("Active Infection Details",
                    new List<string> { "Patient", "Organism", "Type", "Ward", "Severity", "HAI", "Detected" },
                    active.Select(i => new List<string>
                    {
                        i.PatientName, i.Organism, i.Type, i.Ward, i.Severity,
                        i.IsHai ? "Yes" : "No", i.DetectedAt.ToString("dd MMM yyyy")
                    }).ToList()),
            }
        );
    }

    private ReportSection BuildResistancePatterns(List<ResistanceSummary> summaries)
    {
        var allPatterns = summaries.SelectMany(s => s.Patterns).ToList();

        return new ReportSection(
            "Antimicrobial Resistance Patterns",
            "",
            new List<ReportMetric>
            {
                new("Organisms Tracked", summaries.Count.ToString()),
                new("Avg MDR Rate", summaries.Count > 0 ? $"{summaries.Average(s => s.MdrRate):P0}" : "N/A"),
                new("Rising Resistance", allPatterns.Count(p => p.Trend == "Rising").ToString(), Severity: "warning"),
                new("Total Isolates", summaries.Sum(s => s.TotalIsolates).ToString()),
            },
            new List<ReportChartData>
            {
                new("bar", "MDR Rate by Organism",
                    summaries.Select(s => s.Organism).ToList(),
                    new List<ReportDataSeries> { new("MDR Rate (%)", summaries.Select(s => s.MdrRate * 100).ToList()) }),
                new("bar", "Resistance Rate by Antibiotic (All Organisms)",
                    allPatterns.GroupBy(p => p.Antibiotic).Select(g => g.Key).Take(10).ToList(),
                    new List<ReportDataSeries>
                    {
                        new("Resistance (%)", allPatterns.GroupBy(p => p.Antibiotic)
                            .Select(g => g.Average(p => p.ResistanceRate) * 100).Take(10).ToList())
                    }),
            },
            new List<ReportTable>
            {
                new("Resistance Summary by Organism",
                    new List<string> { "Organism", "Total Isolates", "MDR Rate", "Key Patterns" },
                    summaries.Select(s => new List<string>
                    {
                        s.Organism, s.TotalIsolates.ToString(), $"{s.MdrRate:P0}",
                        string.Join(", ", s.Patterns.Where(p => p.Trend == "Rising").Select(p => $"{p.Antibiotic} ({p.ResistanceRate:P0})").Take(3))
                    }).ToList()),
                new("Detailed Resistance Patterns",
                    new List<string> { "Organism", "Antibiotic", "Resistance Rate", "Samples", "Trend" },
                    allPatterns.OrderByDescending(p => p.ResistanceRate).Select(p => new List<string>
                    {
                        p.Organism, p.Antibiotic, $"{p.ResistanceRate:P0}", p.SampleCount.ToString(), p.Trend
                    }).ToList()),
            }
        );
    }

    private ReportSection BuildPatientRisk(List<Patient> patients)
    {
        var riskBands = new[] { ("Critical", 0.8, 1.0), ("High", 0.6, 0.8), ("Medium", 0.4, 0.6), ("Low", 0.0, 0.4) };
        var distribution = riskBands.Select(b =>
            (b.Item1, patients.Count(p => p.RiskScore >= b.Item2 && p.RiskScore < (b.Item1 == "Critical" ? 1.01 : b.Item3)))).ToList();

        var byWard = patients.GroupBy(p => p.Ward).Select(g => (g.Key, g.Average(p => p.RiskScore))).OrderByDescending(x => x.Item2).ToList();

        return new ReportSection(
            "Patient Risk Analysis",
            "",
            new List<ReportMetric>
            {
                new("Total Patients", patients.Count.ToString()),
                new("Critical Risk", distribution.First(d => d.Item1 == "Critical").Item2.ToString(), Severity: "critical"),
                new("High Risk", distribution.First(d => d.Item1 == "High").Item2.ToString(), Severity: "warning"),
                new("Avg Risk Score", $"{patients.Average(p => p.RiskScore):P0}"),
            },
            new List<ReportChartData>
            {
                new("pie", "Risk Distribution",
                    distribution.Select(d => d.Item1).ToList(),
                    new List<ReportDataSeries> { new("Patients", distribution.Select(d => (double)d.Item2).ToList()) }),
                new("bar", "Average Risk Score by Ward",
                    byWard.Select(w => w.Key).ToList(),
                    new List<ReportDataSeries> { new("Risk Score", byWard.Select(w => Math.Round(w.Item2 * 100, 1)).ToList()) }),
            },
            new List<ReportTable>
            {
                new("High-Risk Patients",
                    new List<string> { "Patient", "Ward", "Bed", "Risk Score", "Status", "Active Infections", "Organisms" },
                    patients.Where(p => p.RiskScore >= 0.6).OrderByDescending(p => p.RiskScore).Select(p => new List<string>
                    {
                        p.Name, p.Ward, p.BedNumber, $"{p.RiskScore:P0}", p.Status,
                        p.ActiveInfections.ToString(), string.Join(", ", p.Organisms)
                    }).ToList()),
            }
        );
    }

    private ReportSection BuildOutbreakAnalysis(List<Outbreak> outbreaks)
    {
        var active = outbreaks.Where(o => o.Status == "Active").ToList();

        return new ReportSection(
            "Outbreak Analysis",
            "",
            new List<ReportMetric>
            {
                new("Active Outbreaks", active.Count.ToString(), Severity: active.Any(o => o.Severity == "Critical") ? "critical" : "warning"),
                new("Total Affected", active.Sum(o => o.AffectedPatients).ToString()),
                new("Under Investigation", active.Count(o => o.InvestigationStatus == "In Progress").ToString()),
            },
            new List<ReportChartData>
            {
                new("bar", "Outbreak Size by Cluster",
                    active.Select(o => $"{o.Organism} ({o.Location})").ToList(),
                    new List<ReportDataSeries> { new("Affected Patients", active.Select(o => (double)o.AffectedPatients).ToList()) }),
            },
            new List<ReportTable>
            {
                new("Active Outbreak Details",
                    new List<string> { "Organism", "Location", "Severity", "Patients", "Status", "Investigation", "Detected" },
                    active.Select(o => new List<string>
                    {
                        o.Organism, o.Location, o.Severity, o.AffectedPatients.ToString(),
                        o.Status, o.InvestigationStatus, o.DetectedAt.ToString("dd MMM yyyy")
                    }).ToList()),
            }
        );
    }

    private ReportSection BuildScreeningCompliance(List<ScreeningCompliance> screening)
    {
        var avgRate = screening.Count > 0 ? screening.Average(s => s.ComplianceRate) : 0;
        var totalOverdue = screening.Sum(s => s.Overdue);

        return new ReportSection(
            "Screening Compliance",
            "",
            new List<ReportMetric>
            {
                new("Average Compliance", $"{avgRate:P0}", Severity: avgRate < 0.8 ? "warning" : "ok"),
                new("Total Overdue", totalOverdue.ToString(), Severity: totalOverdue > 10 ? "warning" : "ok"),
                new("Wards Below 80%", screening.Count(s => s.ComplianceRate < 0.8).ToString()),
            },
            new List<ReportChartData>
            {
                new("bar", "Screening Compliance by Ward",
                    screening.Select(s => s.Ward).ToList(),
                    new List<ReportDataSeries>
                    {
                        new("Compliance (%)", screening.Select(s => Math.Round(s.ComplianceRate * 100, 1)).ToList()),
                    }),
            },
            new List<ReportTable>
            {
                new("Ward Screening Summary",
                    new List<string> { "Ward", "Required", "Completed", "Overdue", "Compliance Rate" },
                    screening.OrderBy(s => s.ComplianceRate).Select(s => new List<string>
                    {
                        s.Ward, s.TotalRequired.ToString(), s.Completed.ToString(),
                        s.Overdue.ToString(), $"{s.ComplianceRate:P0}"
                    }).ToList()),
            }
        );
    }

    private ReportSection BuildDeviceInfections(List<DeviceSummary> summaries, List<DeviceInfection> infections)
    {
        return new ReportSection(
            "Device-Associated Infections",
            "",
            new List<ReportMetric>
            {
                new("Device Types", summaries.Count.ToString()),
                new("Total Infections", summaries.Sum(s => s.Infections).ToString()),
                new("Highest Rate", summaries.Count > 0 ? $"{summaries.Max(s => s.InfectionRate):F1}%" : "N/A"),
                new("Avg Days to Infection", summaries.Count > 0 ? $"{summaries.Average(s => s.AvgDaysToInfection):F1}" : "N/A"),
            },
            new List<ReportChartData>
            {
                new("bar", "Infection Rate by Device Type",
                    summaries.Select(s => s.DeviceType).ToList(),
                    new List<ReportDataSeries> { new("Rate (%)", summaries.Select(s => (double)s.InfectionRate).ToList()) }),
                new("bar", "Total Devices vs Infections",
                    summaries.Select(s => s.DeviceType).ToList(),
                    new List<ReportDataSeries>
                    {
                        new("Total Devices", summaries.Select(s => (double)s.TotalDevices).ToList(), "#94a3b8"),
                        new("Infections", summaries.Select(s => (double)s.Infections).ToList(), "#ef4444"),
                    }),
            },
            new List<ReportTable>
            {
                new("Device Infection Summary",
                    new List<string> { "Device Type", "Total Devices", "Infections", "Infection Rate", "Avg Days" },
                    summaries.Select(s => new List<string>
                    {
                        s.DeviceType, s.TotalDevices.ToString(), s.Infections.ToString(),
                        $"{s.InfectionRate:F1}%", $"{s.AvgDaysToInfection:F1}"
                    }).ToList()),
            }
        );
    }

    private ReportSection BuildTransmissionAnalysis(TransmissionNetwork network)
    {
        var nodesByType = network.Nodes.GroupBy(n => n.NodeType).ToList();
        var linksByType = network.Links.GroupBy(l => l.LinkType).ToList();

        return new ReportSection(
            "Transmission Network Analysis",
            "",
            new List<ReportMetric>
            {
                new("Primary Organism", network.Organism),
                new("Total Cases", network.TotalCases.ToString()),
                new("Network Nodes", network.Nodes.Count.ToString()),
                new("Transmission Links", network.Links.Count.ToString()),
                new("Avg Confidence", network.Links.Count > 0 ? $"{network.Links.Average(l => l.Confidence):P0}" : "N/A"),
            },
            new List<ReportChartData>
            {
                new("pie", "Nodes by Type",
                    nodesByType.Select(g => g.Key).ToList(),
                    new List<ReportDataSeries> { new("Count", nodesByType.Select(g => (double)g.Count()).ToList()) }),
                new("bar", "Links by Type",
                    linksByType.Select(g => g.Key).ToList(),
                    new List<ReportDataSeries> { new("Count", linksByType.Select(g => (double)g.Count()).ToList()) }),
            },
            new List<ReportTable>
            {
                new("Transmission Links",
                    new List<string> { "Source", "Target", "Type", "Confidence", "Evidence" },
                    network.Links.OrderByDescending(l => l.Confidence).Select(l =>
                    {
                        var src = network.Nodes.FirstOrDefault(n => n.Id == l.SourceId);
                        var tgt = network.Nodes.FirstOrDefault(n => n.Id == l.TargetId);
                        return new List<string>
                        {
                            src?.PatientName ?? l.SourceId, tgt?.PatientName ?? l.TargetId,
                            l.LinkType, $"{l.Confidence:P0}", l.Evidence
                        };
                    }).ToList()),
            }
        );
    }

    private async Task<string> GenerateNarrative(string sectionTitle, string dataContext)
    {
        if (_chatClient is null)
            return $"This section presents the {sectionTitle.ToLower()} data from the current reporting period. AI-generated narrative requires Azure OpenAI configuration.";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(ReportSystemPrompt),
                new UserChatMessage($"Write a professional narrative for the \"{sectionTitle}\" section of an IPC surveillance report.\n\nData:\n{dataContext}"),
            };

            var opts = new ChatCompletionOptions { MaxOutputTokenCount = 500, Temperature = 0.3f };
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, opts);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate narrative for {Section}", sectionTitle);
            return $"This section presents the {sectionTitle.ToLower()} data from the current reporting period.";
        }
    }

    private async Task<string> GenerateRecommendations(
        DashboardSummary summary, List<Infection> infections, List<Outbreak> outbreaks,
        List<ResistanceSummary> resistance, List<ScreeningCompliance> screening)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Active infections: {infections.Count(i => i.Status == "Active")}");
        sb.AppendLine($"Active outbreaks: {outbreaks.Count(o => o.Status == "Active")}");
        sb.AppendLine($"Avg screening compliance: {(screening.Count > 0 ? screening.Average(s => s.ComplianceRate) : 0):P0}");
        sb.AppendLine($"Organisms with rising resistance: {string.Join(", ", resistance.SelectMany(r => r.Patterns.Where(p => p.Trend == "Rising").Select(p => $"{r.Organism}/{p.Antibiotic}")))}");
        sb.AppendLine($"Critical infections: {infections.Count(i => i.Severity == "Critical")} | High-risk wards: {string.Join(", ", infections.Where(i => i.Status == "Active").GroupBy(i => i.Ward).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key))}");

        if (_chatClient is null)
            return "Configure Azure OpenAI for AI-generated recommendations based on current data.";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("""
                    You are MetaMed AI generating an evidence-based recommendations section for an IPC 
                    surveillance report. Generate 6-8 specific, actionable recommendations organised by 
                    priority (Immediate, Short-term, Medium-term). Each recommendation should reference 
                    specific data points. Use numbered format. Be specific about which wards, organisms, 
                    or interventions. Include IPC best-practice references where relevant.
                    """),
                new UserChatMessage($"Generate prioritised recommendations based on this data:\n{sb}"),
            };

            var opts = new ChatCompletionOptions { MaxOutputTokenCount = 800, Temperature = 0.3f };
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, opts);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations");
            return "Configure Azure OpenAI for AI-generated recommendations.";
        }
    }

    private static string FormatExecutiveSummaryContext(DashboardSummary s, List<Infection> inf, List<Patient> pat, List<Outbreak> outbreaks) =>
        $"Active infections: {inf.Count(i => i.Status == "Active")}, HAI: {inf.Count(i => i.IsHai)}, " +
        $"Patients: {pat.Count}, At risk: {s.PatientsAtRisk}, Outbreaks: {s.ActiveOutbreaks}, " +
        $"Infection rate change: {s.InfectionRateChange}%, Risk score avg: {s.RiskScoreAverage:P0}, " +
        $"Organisms: {string.Join(", ", inf.Where(i => i.Status == "Active").Select(i => i.Organism).Distinct())}";

    private static string FormatInfectionContext(List<Infection> infections)
    {
        var active = infections.Where(i => i.Status == "Active").ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"Total active: {active.Count}, HAI: {active.Count(i => i.IsHai)}");
        foreach (var g in active.GroupBy(i => i.Organism))
            sb.AppendLine($"- {g.Key}: {g.Count()} cases, wards: {string.Join("/", g.Select(i => i.Ward).Distinct())}, severity: {string.Join("/", g.Select(i => i.Severity).Distinct())}");
        foreach (var w in active.GroupBy(i => i.Ward))
            sb.AppendLine($"- {w.Key}: {w.Count()} infections");
        return sb.ToString();
    }

    private static string FormatResistanceContext(List<ResistanceSummary> summaries)
    {
        var sb = new StringBuilder();
        foreach (var s in summaries)
        {
            sb.AppendLine($"{s.Organism}: {s.TotalIsolates} isolates, MDR rate {s.MdrRate:P0}");
            foreach (var p in s.Patterns)
                sb.AppendLine($"  - {p.Antibiotic}: {p.ResistanceRate:P0} ({p.SampleCount} samples, {p.Trend})");
        }
        return sb.ToString();
    }

    private static string FormatPatientContext(List<Patient> patients)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Total patients: {patients.Count}");
        sb.AppendLine($"Critical: {patients.Count(p => p.Status == "Critical")}, Monitoring: {patients.Count(p => p.Status == "Monitoring")}, Stable: {patients.Count(p => p.Status == "Stable")}");
        sb.AppendLine($"High risk (>60%): {patients.Count(p => p.RiskScore >= 0.6)}");
        foreach (var p in patients.Where(p => p.RiskScore >= 0.6).OrderByDescending(p => p.RiskScore))
            sb.AppendLine($"- {p.Name}: {p.RiskScore:P0} risk, {p.Ward}, {p.ActiveInfections} infections, organisms: {string.Join(", ", p.Organisms)}");
        return sb.ToString();
    }

    private static string FormatOutbreakContext(List<Outbreak> outbreaks)
    {
        var sb = new StringBuilder();
        foreach (var o in outbreaks.Where(o => o.Status == "Active"))
            sb.AppendLine($"- {o.Organism} in {o.Location}: {o.AffectedPatients} patients, {o.Severity}, investigation: {o.InvestigationStatus}");
        return sb.ToString();
    }

    private static string FormatScreeningContext(List<ScreeningCompliance> screening)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Average compliance: {(screening.Count > 0 ? screening.Average(s => s.ComplianceRate) : 0):P0}");
        foreach (var s in screening)
            sb.AppendLine($"- {s.Ward}: {s.ComplianceRate:P0} ({s.Completed}/{s.TotalRequired}, {s.Overdue} overdue)");
        return sb.ToString();
    }

    private static string FormatDeviceContext(List<DeviceSummary> summaries, List<DeviceInfection> infections)
    {
        var sb = new StringBuilder();
        foreach (var s in summaries)
            sb.AppendLine($"- {s.DeviceType}: {s.Infections}/{s.TotalDevices} ({s.InfectionRate:F1}%), avg days to infection: {s.AvgDaysToInfection:F1}");
        return sb.ToString();
    }

    private static string FormatTransmissionContext(TransmissionNetwork network)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Organism: {network.Organism}, Cases: {network.TotalCases}, Nodes: {network.Nodes.Count}, Links: {network.Links.Count}");
        foreach (var l in network.Links)
        {
            var src = network.Nodes.FirstOrDefault(n => n.Id == l.SourceId);
            var tgt = network.Nodes.FirstOrDefault(n => n.Id == l.TargetId);
            sb.AppendLine($"- {src?.PatientName ?? l.SourceId} -> {tgt?.PatientName ?? l.TargetId}: {l.LinkType}, confidence: {l.Confidence:P0}");
        }
        return sb.ToString();
    }
}
