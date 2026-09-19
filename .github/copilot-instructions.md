# Muse instructions

This repo is `CSweet.Office.Contracts` — dependency-light wire contracts shared by C-Sweet Headquarters and C-Sweet Office. Keep it limited to cross-repository wire concerns.

Apply `AGENTS.md` plus the `docs/` reference (`README.md` index, `control-plane.md`, `guest-broker.md`, `workloads.md`, `security.md`, `versioning.md`).

Key facts for code completion and review:

- `net10.0`, `Nullable` + `ImplicitUsings`, `LangVersion latest`. Package `CSweet.Office.Contracts 0.7.1`, MIT, SourceLink + `snupkg`.
- `ControlPlane/EnrollmentContracts.cs`: HTTPS JSON `sealed record` claim/heartbeat/certificate/assisted-setup contracts. `OfficeVersion` optional (0.7.0).
- `ControlPlane/office_control.proto`: `OfficeGateway` gRPC (`Connect`, `OpenWorkloadTunnel`, `DownloadArtifact`), package `csweet.office.v1`. Preserve unknown fields.
- `ControlPlane/OfficeCertificateRecovery.cs`: payload is UTF-8 `"csweet-office-certificate-recovery-v1\n{officeId:D}\n{challenge}"`, P-256/SHA-256 IEEE P1363.
- `Guest/guest_broker.proto`: envelope only, no gRPC service. `Guest/GuestHandshake.cs`: HMAC-SHA256 boot proof (token ≥ 16 bytes) + P-256 challenge; single-use verifier. `Guest/LengthDelimitedProtobuf.cs`: BE int32 prefix, 1 MiB default / 16 MiB max.
- `Security/AssignmentEnvelope.cs`: frozen v1 bytes over `CSweet.Isolation.Security`; `Digest("{}") = "sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a"`. Never alter bytes without a major bump.
- `Workloads/WorkloadModels.cs`: `Builder`/`Runtime`/`ToolchainBuild` specs; `WorkloadResourceLimits.Validate()` bounds.
- `ProtocolVersions`: `ControlPlaneV1 = "1.0"`, `GuestBrokerV1 = "1.0"`.
- Sibling `../CSweet.Isolation` auto-used when present; release validation requires `/p:UseLocalIsolation=false`.

Validate: `dotnet test CSweet.Office.Contracts.slnx -c Release`, then pack and inspect the `.nupkg`.
