# Knockout Night (Unity 3D, first-person boxing tournament)

First-person boxing for Android (iPhone later). Version 1 is offline: a run of 5 fights against bots of rising
difficulty, 90 seconds each. Win by knockout or by health when time runs out, then pick 1 of 3 upgrade cards.

## Controls
Left stick = move / circle the opponent (the camera locks on), JAB and CROSS = punches, hold BLOCK = guard.
Editor: WASD, J jab, K cross, hold L to block.

## How the project works
- Everything is code. `Assets/Editor/SceneBuilder.cs` builds the ring, boxers and HUD (menu **Boxing > Rebuild Main Scene**).
- `Assets/Editor/AutoBuildScene.cs` generates the scene automatically on the cloud builder.
- Gameplay: `Assets/Scripts` (Fighter, PlayerBoxer, BotBoxer, BoutManager).
- Build on Unity Build Automation (cloud.unity.com) -> Default Android.

## Next
Online bouts (winner stays, spectators bet and throw items), more cards, more arenas.

Earlier games stay in git history: tags `lastlight-ch1` (The Last Light) and the Neon Arena commits.
