# Security & Signing

How assignments are authenticated and how certificate recovery proves key possession. This package provides deterministic encoders and domain-separated payloads — never trust decisions.

## Assignment envelope (`Security/AssignmentEnvelope.cs`)

Thin adapter over `CSweet.Isolation.Security.WorkloadAuthorizationEnvelope` (NuGet `CSweet.Isolation.Security 0.1.0`, or sibling `../CSweet.Isolation` source). Byte layout is frozen; `AssignmentEncodingCompatibilityTests` pins the exact base64 for a known vector.

- `Digest(specificationJson)` → `"sha256:<hex>"` canonical SHA-256 of the exact `specification_json` bytes. Known vector: `Digest("{}") = "sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a"`.
- `Payload(officeId, assignmentId, workloadId, fencingEpoch, providerId, specificationSha256, issuedAt, leaseExpiresAt)` → `Encode("CSweet.Office.WorkloadAuthorization", authorizationVersion=1, ...)` bytes that HQ signs into `WorkloadAssignment.signature` under `signature_key_id`.
- `CurrentAuthorizationVersion = 1`. Bump only on a breaking payload change (requires major package version + downstream pin sync).
- Payload binds office, assignment, workload, fencing epoch, provider, spec digest, issue/lease timestamps. Deterministic: same inputs → same bytes. Any bound field change → different bytes → signature fails.

Verify path (Office side): recompute `Digest(specification_json)`, compare to `specification_sha256`; recompute `Payload(...)`, verify `signature` against the trusted key from `GatewayHello` / `HeadquartersAssignmentTrustResponse` / `ClaimOfficeResponse`. Reject on mismatch, stale `fencing_epoch`, or expired lease. Key rotation: `signature_key_id` selects the key; never accept a key learned from the assignment itself.

## Certificate recovery proof (`ControlPlane/OfficeCertificateRecovery.cs`)

`OfficeCertificateRecoveryProof.Payload(officeId, challenge)` = UTF-8 `"csweet-office-certificate-recovery-v1\n{officeId:D}\n{challenge}"`. Signed with the Office's original P-256 private key (ECDSA/SHA-256, IEEE P1363), sent as `OfficeCertificateRecoveryRequest(Challenge, SignatureBase64)`. Stable domain + office binding (tested). HQ must enforce one-use challenges, expiry, and approval checks.

## Guest handshake crypto

Covered in [Guest broker](guest-broker.md): HMAC-SHA256 boot proof (token ≥ 16 bytes, fixed-time compare) + ECDSA P-256/SHA-256 challenge signature over a `csweet-guest-challenge-v1` domain. Lease expiry bounds both steps.

## Trust boundaries

- HQ trusts: enrollment token / handoff secret (one-use), CSR identity, recovery signature + challenge freshness, heartbeat posture as advisory.
- Office trusts: assignment signature under a pinned key id, fencing epoch ordering, artifact digest + `artifact_read_token` scoping.
- Guest trusts: boot token + lease; host trusts: boot proof + challenge signature.
- Never log or return secrets: enrollment receipts, handoff secrets, boot tokens, nonces, private keys, `artifact_read_token`. Keep `sanitized_failure`/`log_excerpt` secret-free.
