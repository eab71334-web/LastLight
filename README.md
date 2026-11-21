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

## Realistic boxer (opponent)

The opponent is a skinned human (MakeHuman CC0 base mesh, 53 bones) with brand-new boxing animations:
idle, shuffle, jab, cross, block, block_hit, hit, hit2, hit_body, stagger, ko, win.
No importer package is needed: `Assets/Resources/Boxer/boxer.bytes` is read by `Assets/Scripts/BoxerRig.cs`,
which builds the SkinnedMeshRenderer and plays the clips at runtime. If the file fails to load, the old primitive body is shown.

Regenerate with `python3 tools/boxer/export.py Assets/Resources/Boxer/boxer.bytes`
(needs numpy and the Sabak-Engine file `assets/human.bin`; change `SRC` in `tools/boxer/build_char.py`).
Edit poses/clips in `tools/boxer/poses.py` and `tools/boxer/anims.py`; `tools/boxer/render.py` draws contact sheets to check them.
