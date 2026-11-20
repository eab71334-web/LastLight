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

1. Add repository secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`
   (see https://game.ci/docs/github/activation). `activation.yml` produces the `.alf` request file.
2. Actions > **Build Game** > Run workflow.
3. Artifacts: `LastLight-Android-APK` (installable, debug-signed) and `LastLight-iOS-IPA` (unsigned,
   needs sideloading/re-signing).

The Unity version lives in `ProjectSettings/ProjectVersion.txt` (the workflow reads it with `unityVersion: auto`).

## Next steps

- Replace the primitive shapes with real models and animations (Assets/Models), keep the scene builder as the layout.
- Chapters 2+, more NPCs, choices, music and sound effects.
- Arabic text needs a font and a text-shaping package (the built-in UI text does not join Arabic letters).
