# Versioning & Release

## Version 0.15.0

Adds the public Office linking request, matching code and encrypted enrollment grant. Existing claim and assisted installation messages remain compatible.

## SemVer

Breaking wire or public API change → major. Additive compatible change → minor. Compatible fix → patch. Every contract change increments `<Version>` in `CSweet.Office.Contracts.csproj` and updates downstream pins (Office + Headquarters must stay synchronized).

Recent history: `0.6.0` certificate-recovery contracts; `0.6.1` neutral isolation encoder + sibling-source auto-detect (bytes unchanged); `0.7.0` optional `OfficeVersion` heartbeat field; `0.7.1` packaging/CI maintenance (no wire change).

## Sibling source vs. package

Version `0.14.1` adds an optional Office compute capacity offer, signed dispatches, result and maintenance deliveries and distinct signed receipts for each. Existing peers can omit these fields. The two receipt domains bind the Office and exact payload digest.

`0.8.0` adds assignment-stop reports and receipts. The original consumer pins were `0.8.0`; local validation uses the packed unpublished package with sibling references disabled. Publication and coordinated deployment are separate release gates.

`Directory.Build.props` defaults `CSweetIsolationRepositoryRoot` to `../CSweet.Isolation` and sets `UseLocalIsolation=true` when that checkout's `CSweet.Isolation.Security.csproj` exists, else `false` (NuGet `CSweet.Isolation.Security 0.1.0`). Explicit `/p:UseLocalIsolation=false` forces package-only restore — required for release validation against the exact published version.

## Build / test / pack

```powershell
dotnet test CSweet.Office.Contracts.slnx -c Release
dotnet pack src/CSweet.Office.Contracts/CSweet.Office.Contracts.csproj -c Release -o artifacts/packages
# release validation (package-only):
dotnet test CSweet.Office.Contracts.slnx -c Release /p:UseLocalIsolation=false
```

Inspect the `.nupkg` version before handoff (per `AGENTS.md`): version, README, symbols (`snupkg` + SourceLink).

## CI / publish

- `ci.yml` (PR + `main`): `setup-dotnet 8.0.x`, `dotnet test -c Release`, `dotnet pack --no-build -o artifacts/packages`, upload `*.nupkg`.
- `publish.yml` (`main` path/tag `v*.*.*` or dispatch, OIDC trusted publishing): verifies tag `vX.Y.Z` matches `<Version>`, restore/build/test/pack `--no-build --no-restore`, `dotnet nuget push --skip-duplicate`, creates GitHub release `vX.Y.Z` if missing.

## Tests as spec

`tests/CSweet.Office.Contracts.Tests`: `ContractTests` (assisted-setup/posture/workload round-trips, payload determinism + field binding, digest vector, gateway round-trip + unknown-field preservation, oversized-frame rejection), `AssignmentEncodingCompatibilityTests` (frozen v1 bytes), `OfficeCertificateRecoveryTests` (domain string + JSON round-trip). Add a pinning test for any new canonical bytes.

`0.10.0` adds `ReconcileAssignmentStop` in HeadquartersControlMessage field 15. This is an additive teardown-only message. The signed assignment bytes remain unchanged.

`0.11.0` adds automatic Office update preparation DTOs in `ControlPlane/OfficeUpdatePreparation.cs`. The corresponding consumer pins were `0.11.0`. A preparation receipt and a fresh installer handoff have distinct purposes; neither changes signed assignment bytes or grants execution authority.

## Runtime public internet (0.10.0)

`RuntimeWorkloadSpecification.NetworkPolicy` optionally carries `RuntimeNetworkPolicy` (`public-internet`, grant revision, blocked CIDRs); omission means restricted execution. `EnvironmentProfile` identifies the requested development profile. Both fields are bound by the original signed specification digest; authorization envelope encoding remains unchanged.

Guest protocol 1.1 adds envelope field 22 (`EgressFrame`) and boot field 16 (`public_internet`). Egress uses flow-scoped open/opened/data/half-close/close/error operations for TCP and connected UDP. Office inventory field 14 (`supports_public_internet`) and the corresponding enrollment DTO property default false. Neither a manifest declaration nor inventory alone grants internet authority. Guest protocol 1.0 and all frozen signing vectors remain supported.

`0.12.0` adds optional assisted-setup installation storage. Both current consumer pins are `0.12.0`.
Headquarters requires Office 0.13.0 or newer for sessions carrying a storage selection, so older
installers cannot silently ignore the chosen drive.

## Streamed proxy bodies and persistent workload disk (0.13.0)

Guest protocol 1.2 (`ProtocolVersions.GuestBrokerV12`) adds envelope field 23 (`ProxyBodyFrame`), `body_streamed` on `ProxyRequest` (field 7) and `ProxyResponse` (field 6), and boot field 17 (`persistent_disk`). A streamed request or response carries an empty inline `body`; its content follows as `ProxyBodyFrame` messages for the same `request_id`: `DATA` frames of at most `ProxyBodyStreaming.MaximumDataBytes` with a zero-based contiguous `sequence` per direction, then `END` whose `sequence` equals the number of `DATA` frames sent. Each direction starts with `ProxyBodyStreaming.InitialCredits`; the receiver returns one `CREDIT`, naming the direction it credits, per consumed `DATA` frame. `ABORT` ends the request with an `error_code`. `ProxyBodyStreaming.Validate` rejects frames that are malformed on their own; sequence and credit accounting are receiver state. Inline bodies are unchanged, so protocol 1.0 and 1.1 peers interoperate as before, and a sender streams only to a peer that negotiated 1.2.

`RuntimeWorkloadSpecification.PersistentDisk` optionally carries `RuntimePersistentDisk(DiskId, Generation, MaximumMegabytes)`; omission means no state outlives the virtual machine. The Office derives the disk location from `DiskId`; no path, device or mount option is carried. A `Generation` above the one the Office holds for the disk requires a blank replacement, and a lower one is stale and refused. The field is bound by the signed specification digest; authorization envelope encoding is unchanged. `OfficeProviderInventory` field 15 (`supports_persistent_disk`) and `RegisterOfficeProviderRequest.SupportsPersistentDisk` default false, and Headquarters sets `PersistentDisk` only for a provider that reports support. All frozen signing vectors remain valid.
