# CSweet.Office.Contracts

Dependency-light wire contracts shared by C-Sweet Headquarters and C-Sweet Office.

The package owns only cross-process protocol concerns: headquarters enrollment and control-plane gRPC messages, guest broker framing and authentication, portable workload specifications, and canonical assignment signatures. Privileged provider interfaces and RuntimeHost local RPC remain private to the Office repository.

## Packages

`CSweet.Office.Contracts` targets .NET 8 and is released with SourceLink and symbol packages. Releases follow semantic versioning. Breaking wire or public API changes require a major version; additive compatible changes require a minor version; compatible fixes require a patch version.

## Local development

```powershell
dotnet test CSweet.Office.Contracts.slnx -c Release
dotnet pack src/CSweet.Office.Contracts/CSweet.Office.Contracts.csproj -c Release -o artifacts/packages
```

Consumers automatically detect a sibling checkout. This package also detects a sibling CSweet.Isolation checkout; explicit false UseLocal properties retain package-only checks. Release validation must also run with that property disabled against the exact package version.

## Version 0.6.0

Adds Office certificate recovery challenge/response contracts and a domain-separated ECDSA P-256/SHA-256 proof payload. The additive wire protocol lets an already-approved Office recover an expired certificate using its original private key. Headquarters owns one-use challenge enforcement and authorization; the wire helpers grant no trust by themselves.

## Version 0.6.1

Compatible maintenance: reuse the neutral isolation signature encoder with the original Office byte format, detect sibling isolation source automatically, and preserve the 0.6.0 certificate-recovery contracts. Office and Headquarters pins must remain synchronized.

## Version 0.7.0

Adds the optional OfficeVersion heartbeat field to HTTP and gRPC control messages. Headquarters can refresh the reported runtime version after an in-place update. Older senders remain compatible.
