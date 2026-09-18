# Versioning & Release

## SemVer

Breaking wire or public API change → major. Additive compatible change → minor. Compatible fix → patch. Every contract change increments `<Version>` in `CSweet.Office.Contracts.csproj` and updates downstream pins (Office + Headquarters must stay synchronized).

Recent history: `0.6.0` certificate-recovery contracts; `0.6.1` neutral isolation encoder + sibling-source auto-detect (bytes unchanged); `0.7.0` optional `OfficeVersion` heartbeat field.

## Sibling source vs. package

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
