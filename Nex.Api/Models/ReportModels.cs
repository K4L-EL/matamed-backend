namespace Nex.Api.Models;

public record ReportRequest(string ReportType = "full", string? DateRange = null);

public record ReportResponse(
    string Title,
    string GeneratedAt,
    string Period,
    ReportExecutiveSummary ExecutiveSummary,
    ReportSection InfectionBurden,
    ReportSection ResistancePatterns,
    ReportSection PatientRisk,
    ReportSection OutbreakAnalysis,
    ReportSection ScreeningCompliance,
    ReportSection DeviceInfections,
    ReportSection TransmissionAnalysis,
    ReportSection Recommendations
);

public record ReportExecutiveSummary(
    string Narrative,
    List<ReportMetric> KeyMetrics,
    List<ReportChartData> Charts
);

public record ReportSection(
    string Title,
    string Narrative,
    List<ReportMetric> KeyMetrics,
    List<ReportChartData> Charts,
    List<ReportTable> Tables
);

public record ReportMetric(
    string Label,
    string Value,
    string? Change = null,
    string? Severity = null
);

public record ReportChartData(
    string ChartType,
    string Title,
    List<string> Labels,
    List<ReportDataSeries> Series
);

public record ReportDataSeries(
    string Name,
    List<double> Values,
    string? Color = null
);

public record ReportTable(
    string Title,
    List<string> Headers,
    List<List<string>> Rows
);
