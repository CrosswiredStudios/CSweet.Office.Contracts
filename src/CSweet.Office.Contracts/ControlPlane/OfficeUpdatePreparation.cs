namespace CSweet.Office.Contracts.ControlPlane;

/// <summary>Exchanges the short-lived, machine-bound update handoff for a preparation receipt.</summary>
public sealed record BeginOfficeUpdatePreparationRequest(
    string HandoffSecret, string MachineName, string OperatingSystem, string Architecture);

/// <summary>A receipt grants only preparation/status access for this update, never workload authority.</summary>
public sealed record OfficeUpdatePreparationRequest(
    Guid SessionId, string Receipt, string MachineName, string OperatingSystem, string Architecture,
    bool PackagePrepared = false, string? ErrorCode = null);

public sealed record OfficeUpdatePreparationResponse(
    bool Succeeded, string? ErrorCode, string State, string? TargetVersion = null,
    string? Receipt = null, string? LaunchUri = null, string? ControlPlaneUrl = null);
