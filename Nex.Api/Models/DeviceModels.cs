namespace Nex.Api.Models;

public record DeviceInfection(
    string Id,
    string PatientId,
    string PatientName,
    string DeviceType,
    string Organism,
    string Ward,
    DateTime InsertionDate,
    DateTime InfectionDate,
    int DaysToInfection,
    string Status
);

public record DeviceSummary(
    string DeviceType,
    int TotalDevices,
    int Infections,
    double InfectionRate,
    double AvgDaysToInfection
);
