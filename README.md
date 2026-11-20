# The Last Light (Unity 3D story game)

Chapter 1 of a story-driven 3D adventure for Android and iPhone.
You wash ashore on the island of Mira. Its lighthouse has gone dark. Find the lens,
the flame crystal and the mirror, then relight the Mira Light.

## How the project works

- Everything is code. `Assets/Editor/SceneBuilder.cs` builds the island, village, lighthouse,
  characters and UI. Menu: **Story > Rebuild Main Scene** (in Unity), or automatically in CI.
- `Assets/Editor/BuildScript.cs` is the CI entry point (Android APK, iOS project).
- Gameplay scripts are in `Assets/Scripts`.
- Controls: left stick to walk, drag the right side to look, button to talk/take.
  In the editor: WASD, right mouse button to look, E to interact, Space to continue dialogue.

## Building on GitHub

1. Get a Unity Personal license file (`.ulf`) on a computer with Unity Hub:
   Hub > Preferences > Licenses > Add > Personal. The file is then at
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
2. Add repository secrets: `UNITY_LICENSE` (full contents of the `.ulf`), `UNITY_EMAIL`, `UNITY_PASSWORD`.
   See https://game.ci/docs/github/activation (the old `.alf` request action is no longer supported).
3. Actions > **Build Game** > Run workflow.
4. Artifacts: `LastLight-Android-APK` (installable, debug-signed) and `LastLight-iOS-IPA` (unsigned,
   needs sideloading/re-signing).

The Unity version lives in `ProjectSettings/ProjectVersion.txt` (the workflow reads it with `unityVersion: auto`).

## Next steps

- Replace the primitive shapes with real models and animations (Assets/Models), keep the scene builder as the layout.
- Chapters 2+, more NPCs, choices, music and sound effects.
- Arabic text needs a font and a text-shaping package (the built-in UI text does not join Arabic letters).
