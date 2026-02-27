using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

/// <summary>
/// Centralized mock data used by all stub services.
/// Single source of truth for consistent cross-service references.
/// </summary>
public static class StubData
{
    private static readonly DateTime Now = DateTime.UtcNow;

    public static readonly List<Patient> Patients =
    [
        new("P001", "James Wilson", 67, "Male", "ICU-A", "A-12", Now.AddDays(-8), "Critical", 0.87, 2, ["MRSA", "C. difficile"]),
        new("P002", "Sarah Chen", 54, "Female", "Ward 3B", "3B-04", Now.AddDays(-3), "Stable", 0.42, 1, ["E. coli"]),
        new("P003", "Ahmed Hassan", 71, "Male", "ICU-B", "B-07", Now.AddDays(-12), "Critical", 0.91, 1, ["Klebsiella pneumoniae"]),
        new("P004", "Maria Garcia", 45, "Female", "Ward 2A", "2A-11", Now.AddDays(-5), "Stable", 0.23, 0, []),
        new("P005", "David Thompson", 82, "Male", "Ward 4C", "4C-01", Now.AddDays(-15), "Monitoring", 0.65, 1, ["VRE"]),
        new("P006", "Lisa Patel", 39, "Female", "Surgical", "S-03", Now.AddDays(-2), "Stable", 0.31, 0, []),
        new("P007", "Robert Kim", 58, "Male", "ICU-A", "A-05", Now.AddDays(-6), "Critical", 0.78, 1, ["Pseudomonas aeruginosa"]),
        new("P008", "Emma Brown", 73, "Female", "Ward 3B", "3B-09", Now.AddDays(-9), "Monitoring", 0.56, 1, ["MRSA"]),
        new("P009", "Thomas Wright", 61, "Male", "Ward 2A", "2A-03", Now.AddDays(-4), "Stable", 0.19, 0, []),
        new("P010", "Fatima Al-Rashid", 48, "Female", "Surgical", "S-08", Now.AddDays(-1), "Stable", 0.35, 0, []),
        new("P011", "Michael O'Brien", 76, "Male", "ICU-B", "B-02", Now.AddDays(-11), "Critical", 0.83, 2, ["MRSA", "Candida auris"]),
        new("P012", "Yuki Tanaka", 63, "Female", "Ward 4C", "4C-06", Now.AddDays(-7), "Monitoring", 0.48, 1, ["C. difficile"])
    ];

    public static readonly List<Infection> Infections =
    [
        new("INF001", "P001", "James Wilson", "MRSA", "Bloodstream", "Central line", "ICU-A", "Active", Now.AddDays(-6), null, "High", true),
        new("INF002", "P001", "James Wilson", "C. difficile", "Gastrointestinal", "Endogenous", "ICU-A", "Active", Now.AddDays(-4), null, "Medium", true),
        new("INF003", "P002", "Sarah Chen", "E. coli", "Urinary tract", "Catheter", "Ward 3B", "Active", Now.AddDays(-2), null, "Medium", true),
        new("INF004", "P003", "Ahmed Hassan", "Klebsiella pneumoniae", "Respiratory", "Ventilator", "ICU-B", "Active", Now.AddDays(-10), null, "Critical", true),
        new("INF005", "P005", "David Thompson", "VRE", "Wound", "Surgical site", "Ward 4C", "Active", Now.AddDays(-7), null, "Medium", true),
        new("INF006", "P007", "Robert Kim", "Pseudomonas aeruginosa", "Respiratory", "Ventilator", "ICU-A", "Active", Now.AddDays(-5), null, "High", true),
        new("INF007", "P008", "Emma Brown", "MRSA", "Skin/Soft tissue", "Wound", "Ward 3B", "Monitoring", Now.AddDays(-8), null, "Low", false),
        new("INF008", "P011", "Michael O'Brien", "MRSA", "Bloodstream", "Central line", "ICU-B", "Active", Now.AddDays(-9), null, "Critical", true),
        new("INF009", "P011", "Michael O'Brien", "Candida auris", "Bloodstream", "Unknown", "ICU-B", "Active", Now.AddDays(-3), null, "Critical", true),
        new("INF010", "P012", "Yuki Tanaka", "C. difficile", "Gastrointestinal", "Antibiotic-associated", "Ward 4C", "Monitoring", Now.AddDays(-5), null, "Medium", false)
    ];

    public static readonly List<Outbreak> Outbreaks =
    [
        new("OB001", "MRSA", "ICU Complex", Now.AddDays(-9), null, "Active", 3, "High", "In Progress"),
        new("OB002", "C. difficile", "Ward 4C", Now.AddDays(-5), null, "Active", 2, "Medium", "Preliminary"),
        new("OB003", "Candida auris", "ICU-B", Now.AddDays(-3), null, "Suspected", 1, "Critical", "Initiated"),
        new("OB004", "VRE", "Surgical Ward", Now.AddDays(-20), Now.AddDays(-8), "Resolved", 4, "Medium", "Closed")
    ];

    public static readonly List<Alert> Alerts =
    [
        new("ALT001", "New MRSA Case Detected", "MRSA bloodstream infection identified in ICU-B patient M. O'Brien", "Critical", "Infection", Now.AddHours(-2), false, "INF008", "Infection"),
        new("ALT002", "Candida auris Alert", "First C. auris case detected - enhanced screening recommended", "Critical", "Outbreak", Now.AddHours(-6), false, "OB003", "Outbreak"),
        new("ALT003", "C. difficile Cluster", "Two linked C. difficile cases in Ward 4C within 72 hours", "High", "Outbreak", Now.AddDays(-1), false, "OB002", "Outbreak"),
        new("ALT004", "High Risk Patient", "Patient A. Hassan risk score elevated to 0.91", "High", "Risk", Now.AddDays(-1), true, "P003", "Patient"),
        new("ALT005", "Screening Overdue", "3 patients overdue for MRSA screening in ICU-A", "Medium", "Compliance", Now.AddDays(-2), true, null, null),
        new("ALT006", "Hand Hygiene Alert", "Ward 3B compliance dropped below 80% threshold", "Medium", "Compliance", Now.AddDays(-3), true, null, null)
    ];

    public static readonly List<LocationRisk> LocationRisks =
    [
        new("LOC001", "ICU-A", "Intensive Care", 0.82, 3, 16, 0.88),
        new("LOC002", "ICU-B", "Intensive Care", 0.89, 3, 12, 0.92),
        new("LOC003", "Ward 2A", "General", 0.21, 0, 24, 0.67),
        new("LOC004", "Ward 3B", "General", 0.45, 2, 20, 0.75),
        new("LOC005", "Ward 4C", "General", 0.53, 2, 22, 0.73),
        new("LOC006", "Surgical", "Surgical", 0.34, 0, 18, 0.61),
        new("LOC007", "Emergency", "Emergency", 0.41, 0, 30, 0.83),
        new("LOC008", "Neonatal", "Intensive Care", 0.28, 0, 10, 0.50)
    ];

    public static List<TrendPoint> GenerateInfectionTrends(int days)
    {
        var rng = new Random(42);
        return Enumerable.Range(0, days).SelectMany(d =>
        {
            var date = Now.AddDays(-days + d).Date;
            return new[]
            {
                new TrendPoint(date, 2 + rng.Next(0, 4), "HAI"),
                new TrendPoint(date, 1 + rng.Next(0, 3), "Community"),
            };
        }).ToList();
    }

    public static List<ForecastTrend> GenerateForecastTrends(int days)
    {
        var rng = new Random(42);
        return Enumerable.Range(0, days).Select(d =>
        {
            var date = Now.AddDays(d).Date;
            var predicted = 3.0 + Math.Sin(d * 0.5) * 1.5 + rng.NextDouble();
            return new ForecastTrend(
                date,
                Math.Round(predicted, 1),
                Math.Round(predicted - 1.5, 1),
                Math.Round(predicted + 1.5, 1),
                d < 3 ? Math.Round(predicted + rng.NextDouble() - 0.5, 1) : 0
            );
        }).ToList();
    }
}
