# Game.Network

Network adapter layer for Phase 7.

- `NetworkSessionManager`: UGS Authentication + Multiplayer Services Session/Relay lifecycle.
- `NetworkPlayerAvatar`: owner proxy sends validated pose to server; NGO NetworkTransform distributes server-authoritative proxy.
- `VivoxVoiceManager`: Vivox 16 group voice, muted-by-default transmission, PTT and mute control.
- `MicrophonePermission`: Android/iOS runtime microphone request.

`Game.Network.asmdef` references `Game.Core`, `Game.Gameplay`, NGO runtime and UGS assemblies. This direction is intentional: Gameplay does not depend on Network, and UI depends only on Core contracts.

Phase 7 does not synchronize inventory, gathering, crafting, survival, world objects, AI or bosses. All clients must use the same deterministic island seed in this first slice; host-selected seed replication is deferred to Phase 19. Network avatar first pose is checked against the server island spawn, so clients with a different island seed will be rejected until seed/session replication is implemented. Those require server-authoritative replication in Phase 19.
