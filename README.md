# Neon Arena (Unity 3D, first-person team deathmatch)

Mobile first-person arena shooter for Android (iPhone later). Version 1 is offline: you and 2 bot allies
(blue) against 3 bot enemies (red). First team to 20 kills or the most kills after 3 minutes wins.

## Controls
Left stick = move, drag the right side = aim, FIRE (hold), JUMP, RELOAD.
Editor: WASD, right mouse = look, F = fire, Space = jump, R = reload.

## How the project works
- Everything is code. `Assets/Editor/SceneBuilder.cs` builds the arena, units and HUD (menu **Neon > Rebuild Main Scene**).
- `Assets/Editor/AutoBuildScene.cs` generates the scene automatically on the cloud builder.
- Gameplay: `Assets/Scripts` (Combatant, FpsPlayer, WeaponController, BotAI, MatchManager, HUD helpers).
- Build on Unity Build Automation (cloud.unity.com) -> Default Android.

## Next
Online play (Netcode + Relay), more maps and weapons, minimap, player progression.

The old story game "The Last Light" (Chapter 1) is kept in git history (tag `lastlight-ch1`).
