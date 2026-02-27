namespace Nex.Api.Models;

public record CreatePatientRequest(
    string Name,
    int Age,
    string Gender,
    string Ward,
    string BedNumber,
    string Status = "Stable"
);

public record CreateInfectionRequest(
    string PatientId,
    string Organism,
    string Type,
    string Location,
    string Ward,
    string Severity,
    bool IsHai = true
);

public record CreateAlertRequest(
    string Title,
    string Description,
    string Severity,
    string Category
);

public record CreateOutbreakRequest(
    string Organism,
    string Location,
    string Severity,
    int AffectedPatients = 1
);

public record CreateScreeningRecordRequest(
    string PatientId,
    string PatientName,
    string Ward,
    string ScreeningType,
    DateTime DueDate
);

public record CreateDeviceInfectionRequest(
    string PatientId,
    string PatientName,
    string DeviceType,
    string Organism,
    string Ward,
    DateTime InsertionDate,
    DateTime InfectionDate
);
