# AI instructions – abxrlib-for-unity-demo-app

This repo is a **Unity demo application** that shows how to use the ABXRLib SDK for Unity. It does not duplicate team-wide AGENTS.md in sibling repos; it only describes this project and its dependencies.

---

## This repo: Demo app for AbxrLib (Unity)

Example Unity VR/XR project that demonstrates the AbxrLib SDK: configuration, auth, telemetry, UI, and (on Android) optional use of **ArborInsightService**.

### Project setup

- **Structure:** Standard Unity project: `Assets/`, `Packages/` (abxrlib-for-unity added via git URL or local package), `ProjectSettings/`.
- **Prerequisites:** Unity 2022.3 LTS or later; AbxrLib SDK (abxrlib-for-unity) installed as a package.
- **Scenes / usage:** See README and `Assets/Scenes/` (e.g. TrainingDemo.unity). Run in editor or build to Android/WebGL.

### How it uses other projects

- **abxrlib-for-unity:** This demo **depends on** the AbxrLib Unity package. It uses the SDK for analytics, authentication, and (on Android) optional communication with the ArborInsightService. Setup and integration details are in **abxrlib-for-unity** (README and that repo’s `AGENTS.md`).
- **ArborInsightService:** On Android, when the ArborInsightService APK is installed and abxrlib-for-unity is built with the matching client AAR, the demo app (via the SDK) can bind to the service for auth and analytics. This repo does not build or ship the service or AAR; it only consumes them as supplied (e.g. from a distribution channel).

Flow: **abxrlib-for-unity-demo-app** → uses **abxrlib-for-unity** → which can use the **ArborInsightService** (APK + client AAR) on device when available.
