# Contributor instructions (all agents: Codex, Copilot, Claude, Gemini, Cursor)

- Keep this package limited to cross-repository wire concerns.
- Increment its semantic version for every contract change and update downstream pins.
- Build, test, pack, and inspect the `.nupkg` version before handoff.

## Scope

In scope: cross-process messages, workload specs, `ProtocolVersions`, framing, signature canonicalization.
Out of scope: provider interfaces, RuntimeHost RPC, persistence, scheduling, virtualization (Office repo only).

## Layout

- `src/CSweet.Office.Contracts/` — `net10.0`, `Nullable`+`ImplicitUsings`, `LangVersion latest`.
  - `ControlPlane/EnrollmentContracts.cs` — HTTPS JSON claim/heartbeat/certificate/assisted-setup records.
  - `ControlPlane/OfficeCertificateRecovery.cs` — `csweet-office-certificate-recovery-v1` proof payload.
  - `ControlPlane/office_control.proto` — `OfficeGateway` gRPC (`GrpcServices="Both"`, package `csweet.office.v1`).
  - `Guest/guest_broker.proto` — guest envelope (`GrpcServices="None"`, manual framing only).
  - `Guest/GuestHandshake.cs` — `GuestHandshakeClient`/`GuestHandshakeVerifier` (HMAC boot proof + P-256 challenge).
  - `Guest/LengthDelimitedProtobuf.cs` — big-endian int32 framing, 1 MiB default / 16 MiB absolute max.
  - `Security/AssignmentEnvelope.cs` — adapter over `CSweet.Isolation.Security.WorkloadAuthorizationEnvelope`; frozen v1 bytes.
  - `Workloads/WorkloadModels.cs` — `WorkloadKind` + `Builder`/`Runtime`/`ToolchainBuild` specs, `Validate()` bounds.
  - `ProtocolVersions.cs` — `ControlPlaneV1="1.0"`, `GuestBrokerV1="1.0"`.
- `tests/CSweet.Office.Contracts.Tests/` — round-trip, determinism, frozen-bytes, unknown-field, oversize-frame tests.
- `docs/` — human/agent contract reference: `README.md` index, `control-plane.md`, `guest-broker.md`, `workloads.md`, `security.md`, `versioning.md`.

## Rules

- Wire/public-API break → major; additive compatible → minor; fix → patch. Sync Office + Headquarters pins.
- Never change `AssignmentEnvelope` bytes without a major bump + new frozen-vector test.
- Preserve unknown protobuf fields; keep `OfficeVersion` optional/back-compat.
- No secrets in logs/status: receipts, handoff secrets, boot tokens, nonces, `artifact_read_token`.
- Deps: `Google.Protobuf 3.35.1`, `Grpc.Core.Api 2.83.0`; sibling `../CSweet.Isolation` auto-used when present, else `CSweet.Isolation.Security 0.1.0` NuGet.

## Validate

```powershell
dotnet test CSweet.Office.Contracts.slnx -c Release
dotnet pack src/CSweet.Office.Contracts/CSweet.Office.Contracts.csproj -c Release -o artifacts/packages
dotnet test CSweet.Office.Contracts.slnx -c Release /p:UseLocalIsolation=false
```
