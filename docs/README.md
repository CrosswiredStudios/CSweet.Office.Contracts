# C-Sweet Office Contracts — Documentation Index

`CSweet.Office.Contracts` (currently `0.7.0`, `net10.0`) owns only cross-process wire concerns shared by C-Sweet Headquarters and C-Sweet Office.

Start here, then go deep:

| Document | Covers |
|---|---|
| [Control plane](control-plane.md) | HTTP enrollment/heartbeat/certificate JSON records + `office_control.proto` gRPC gateway (`OfficeGateway`), tunnel, artifact download |
| [Guest broker](guest-broker.md) | `guest_broker.proto` envelope, `GuestHandshakeClient`/`GuestHandshakeVerifier` crypto, `LengthDelimitedProtobuf` framing |
| [Workloads](workloads.md) | `WorkloadKind`, `WorkloadSpecification` hierarchy, resource limits, image/artifact/lease descriptors |
| [Security & signing](security.md) | `AssignmentEnvelope` canonical digest + signed payload, certificate-recovery proof, trust boundaries |
| [Versioning & release](versioning.md) | SemVer rules, sibling-source behavior, build/test/pack, CI/publish, pin discipline |

## What belongs here vs. not

In scope: cross-process messages, workload specifications, protocol constants (`ProtocolVersions`), framing, signature canonicalization.

Out of scope (lives in the Office repo, never here): privileged provider interfaces, `RuntimeHost` local RPC, platform persistence, scheduling, virtualization implementations.

## Package at a glance

- Package ID: `CSweet.Office.Contracts`, MIT, SourceLink + `snupkg` symbols.
- Target: `net10.0`, `Nullable` + `ImplicitUsings` enabled, `LangVersion latest`.
- Runtime deps: `Google.Protobuf 3.35.1`, `Grpc.Core.Api 2.83.0`.
- Build-time deps: `Grpc.Tools 2.83.0` (`PrivateAssets="All"`), `Microsoft.SourceLink.GitHub`.
- Isolation dep: `CSweet.Isolation.Security 0.1.0` via NuGet, or sibling source at `../CSweet.Isolation` when present (see [Versioning & release](versioning.md)).
- Proto codegen: `ControlPlane/office_control.proto` (`GrpcServices="Both"`), `Guest/guest_broker.proto` (`GrpcServices="None"` — manual framing only).
- Protocol constants (`ProtocolVersions`): `ControlPlaneV1 = "1.0"`, `GuestBrokerV1 = "1.0"`.

## Quick consume

```powershell
dotnet test CSweet.Office.Contracts.slnx -c Release
dotnet pack src/CSweet.Office.Contracts/CSweet.Office.Contracts.csproj -c Release -o artifacts/packages
```

Release validation must also run with package-only restore (`/p:UseLocalIsolation=false`) against the exact package version. See [Versioning & release](versioning.md).
