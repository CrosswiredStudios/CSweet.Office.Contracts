# Guest Broker

Guest-side channel between the Office host broker and the workload VM guest. Protobuf schema only — no gRPC service (`GrpcServices="None"`); transport is `LengthDelimitedProtobuf` framing plus the `GuestHandshakeClient`/`GuestHandshakeVerifier` authentication dance.

Source files: `Guest/guest_broker.proto`, `Guest/GuestHandshake.cs`, `Guest/LengthDelimitedProtobuf.cs`, `ProtocolVersions.cs`.

## Protocol version

`ProtocolVersions.GuestBrokerV1 = "1.0"`, `GuestBrokerV11 = "1.1"` (public egress) and `GuestBrokerV12 = "1.2"` (streamed proxy bodies, persistent disk). Carried in `GuestEnvelope.protocol_version`, `GuestBootConfiguration.protocol_version`, and `ExpectedGuestIdentity.ProtocolVersion`.

## Envelope (`guest_broker.proto`, `csweet.office.guest.v1`)

`GuestEnvelope`: `protocol_version=1`, `message_id=2`, oneof:

| Field | Message | Direction | Purpose |
|---|---|---|---|
| 9 | `GuestBootConfiguration` | host → guest (pre-handshake) | Boot lease: `workload_id`, `channel_id`, `protocol_version`, `guest_image_digest`, `artifact_digest`, `boot_token`, `lease_expires_at_unix_seconds`, `artifact_root`, `workload_kind` (int, see [Workloads](workloads.md)), `installation_id`, `business_id`, `tick_id`, `local_broker_socket_path`, `workload_token_path`, `maximum_frame_bytes`. |
| 10 | `GuestHello` | guest → host | `workload_id`, `channel_id`, `guest_image_digest`, `artifact_digest`, `ephemeral_public_key` (DER SPKI P-256), `boot_token_proof` (32-byte HMAC). |
| 11 | `HostChallenge` | host → guest | `nonce` (32 bytes), `expires_at_unix_seconds`. |
| 12 | `GuestProof` | guest → host | `signature` (ECDSA P-256/SHA-256 over challenge payload). |
| 13 | `GuestLease` | host → guest | `accepted`, `reason_code`, `expires_at_unix_seconds`, `maximum_frame_bytes`. |
| 14 | `GuestHealth` | guest → host | `state`, `uptime_seconds`. |
| 15/16 | `ProxyRequest`/`ProxyResponse` | bidi | `request_id`-correlated; request has `purpose`, `method`, `path`, `headers{}`, `body`; response has `status_code`, `headers{}`, `body`, `error_code`. Protocol 1.2 adds `body_streamed` to both. |
| 17 | `StreamChunk` | bidi | `stream_id`, `sequence`, `content`, `completed`, `digest`. |
| 18 | `StartCommand` | host → guest | `workload_kind`, `entrypoint[]`, `build_profile_id`, `maximum_log_bytes`, `environment{}`, `maximum_output_bytes`. |
| 19 | `ShutdownCommand` | host → guest | `reason_code`, `grace_period_seconds`. |
| 20 | `GuestExit` | guest → host | `exit_code`, `reason_code`, `detail`. |
| 21 | `GuestBootFailure` | guest → host | `reason_code`, `detail`. |
| 22 | `EgressFrame` | bidi | Protocol 1.1 public egress flow frames. |
| 23 | `ProxyBodyFrame` | bidi | Protocol 1.2 streamed proxy body: `request_id`, `direction`, `operation`, `sequence`, `data`, `error_code`. |

## Handshake (`GuestHandshake.cs`)

`ExpectedGuestIdentity(WorkloadId, ChannelId, GuestImageDigest, ArtifactDigest?, BootToken, ExpiresAt, ProtocolVersion)` — both sides must agree on it out-of-band (from `BrokerChannelLease` + `GuestBootConfiguration`).

Flow:

1. Guest builds `GuestHello` via `GuestHandshakeClient.CreateHello()`: fresh P-256 ephemeral key, `BootTokenProof = HMAC-SHA256(bootToken, helloPayload)`. `helloPayload` = NUL-joined `"csweet-guest-hello-v1"`, protocol version, workload/channel GUIDs (`D`), image digest, artifact digest (or `""`), lease expiry unix seconds, raw SPKI bytes. Boot token must be ≥ 16 UTF-8 bytes.
2. Host runs `GuestHandshakeVerifier.VerifyHelloAndCreateChallenge(hello)`: rejects expired lease, mismatched IDs/digests, key length outside 64–1024 bytes, proof length ≠ 32, bad HMAC (fixed-time compare), unparsable key or trailing key bytes. Returns 32-byte nonce challenge expiring at `min(lease, now+1min)`.
3. Guest answers with `GuestHandshakeClient.Answer(challenge)`: validates nonce is 32 bytes and expiry is in `(now, lease]`; signs `challengePayload` = NUL-joined `"csweet-guest-challenge-v1"`, workload/channel GUIDs, expiry (invariant culture), then raw nonce.
4. Host runs `VerifyProof(proof, maximumFrameBytes)`: single-use (second call throws), returns `GuestLease`. Reject codes: `challenge-expired`, `invalid-frame-limit` (must be 4096–16 MiB), `invalid-guest-proof`. Accept sets `ExpiresAt = lease expiry`, `MaximumFrameBytes` as negotiated.

`GuestHandshakeClient` is `IDisposable` (owns the ephemeral key). `GuestHandshakeVerifier` disposes the imported guest key after `VerifyProof`.

## Framing (`LengthDelimitedProtobuf.cs`)

Big-endian `int32` length prefix + protobuf payload over any `Stream`.

- `DefaultMaximumFrameBytes = 1 MiB`, `AbsoluteMaximumFrameBytes = 16 MiB`.
- `WriteAsync<T>(stream, message, maximumFrameBytes, ct)`: rejects messages larger than the limit before writing.
- `ReadAsync<T>(stream, parser, maximumFrameBytes, ct)`: returns `default` on clean EOF (zero prefix bytes); throws `EndOfStreamException` on truncated prefix/payload; throws `InvalidDataException` on negative/oversize length — checked before allocating (regression-tested).
- `maximumFrameBytes` must be within `1..AbsoluteMaximumFrameBytes`; negotiated value comes from the handshake `GuestLease`.

## Runtime public internet (0.10.0)

`RuntimeWorkloadSpecification.NetworkPolicy` optionally carries `RuntimeNetworkPolicy` (`public-internet`, grant revision, blocked CIDRs); omission means restricted execution. `EnvironmentProfile` identifies the requested development profile. Both fields are bound by the original signed specification digest; authorization envelope encoding remains unchanged.

Guest protocol 1.1 adds envelope field 22 (`EgressFrame`) and boot field 16 (`public_internet`). Egress uses flow-scoped open/opened/data/half-close/close/error operations for TCP and connected UDP. Office inventory field 14 (`supports_public_internet`) and the corresponding enrollment DTO property default false. Neither a manifest declaration nor inventory alone grants internet authority. Guest protocol 1.0 and all frozen signing vectors remain supported.

## Streamed proxy bodies and persistent workload disk (0.13.0)

Guest protocol 1.2 (`ProtocolVersions.GuestBrokerV12`) adds envelope field 23 (`ProxyBodyFrame`), `body_streamed` on `ProxyRequest` (field 7) and `ProxyResponse` (field 6), and boot field 17 (`persistent_disk`). A streamed request or response carries an empty inline `body`; its content follows as `ProxyBodyFrame` messages for the same `request_id`: `DATA` frames of at most `ProxyBodyStreaming.MaximumDataBytes` with a zero-based contiguous `sequence` per direction, then `END` whose `sequence` equals the number of `DATA` frames sent. Each direction starts with `ProxyBodyStreaming.InitialCredits`; the receiver returns one `CREDIT`, naming the direction it credits, per consumed `DATA` frame. `ABORT` ends the request with an `error_code`. `ProxyBodyStreaming.Validate` rejects frames that are malformed on their own; sequence and credit accounting are receiver state. Inline bodies are unchanged, so protocol 1.0 and 1.1 peers interoperate as before, and a sender streams only to a peer that negotiated 1.2.

`RuntimeWorkloadSpecification.PersistentDisk` optionally carries `RuntimePersistentDisk(DiskId, Generation, MaximumMegabytes)`; omission means no state outlives the virtual machine. The Office derives the disk location from `DiskId`; no path, device or mount option is carried. A `Generation` above the one the Office holds for the disk requires a blank replacement, and a lower one is stale and refused. The field is bound by the signed specification digest; authorization envelope encoding is unchanged. `OfficeProviderInventory` field 15 (`supports_persistent_disk`) and `RegisterOfficeProviderRequest.SupportsPersistentDisk` default false, and Headquarters sets `PersistentDisk` only for a provider that reports support. All frozen signing vectors remain valid.
