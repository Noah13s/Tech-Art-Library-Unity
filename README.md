# Tech Art Library for Unity

A growing Unity package of technical-art experiments, reusable runtime tools, shaders, procedural meshes, physics prototypes, and interactive demo scenes.

The library is exploratory: some folders contain reusable components, while the projects are focused demonstrations intended to be studied, adapted, and extended.

## Requirements

- Unity 2022.1 or newer
- Universal Render Pipeline (URP)
- Input System 1.7
- Shader Graph 14.0
- Mathematics 1.2.6
- TextMesh Pro

The package declares these Unity package dependencies in [`package.json`](package.json). Float Precision and Splitscreen use the Input System package. Some older Robotics and UI examples still use the legacy `UnityEngine.Input` API, so set **Active Input Handling** to **Both** when you want to run every demo without changing their scripts.

## Installation

In Unity, open **Window > Package Manager**, choose **Add package from Git URL**, and enter:

```text
https://github.com/Noah13s/Tech-Art-Library-Unity.git
```

You can also clone the repository and add its folder through **Add package from disk** by selecting `package.json`.

## What is included

| Area | Contents |
| --- | --- |
| [`Base/Projects`](Base/Projects/README.md) | Standalone demonstrations for large-scale rendering, physics, UI, robotics, split screen, and assembly workflows |
| [`Base/Scripts`](Base/Scripts) | General utilities, events, custom Inspector properties, input helpers, transitions, physics helpers, and procedural mesh generators |
| [`Base/Shader`](Base/Shader) | Shader Graph assets for master materials, grids, conveyor belts, and a space skybox |
| [`Base/Materials`](Base/Materials) | Shared materials and material presets |
| [`Base/Meshes`](Base/Meshes) | Reusable mesh assets |
| [`Base/Players`](Base/Players) | Player controllers and related assets |
| [`Base/Prefab`](Base/Prefab) | General-purpose prefabs |
| [`Base/Texture`](Base/Texture) | Shared textures and reference maps |
| [`Samples~`](Samples~) | Optional package samples for AR, VR, physics, serial ports, WebSockets, and general examples |

## Featured projects

| Project | Demonstrates |
| --- | --- |
| [Float Precision](Base/Projects/FloatPrecision/README.md) | Double-precision astronomical coordinates, camera-relative rendering, planetary gravity, local terrain, atmosphere, navigation map, and time warp |
| [Bullet](Base/Projects/Bullet/README.md) | Data-driven ammunition, magazines, penetration, impacts, and fragmentation |
| [Robotics](Base/Projects/Robotics/README.md) | A PD-controlled balancing platform, ball launcher, and directional wind forces |
| [Splitscreen](Base/Projects/Splitscreen/README.md) | Dynamic multi-camera layouts and local gamepad discovery |
| [UI](Base/Projects/UI/README.md) | Navigable menu graphs, carousel and slider controls, and styled menu examples |
| [Assembly Line](Base/Projects/AssemblyLine/README.md) | Trigger-driven object locking, releasing, and prefab assembly |

## Package samples

After installation, open the package in Package Manager and use the **Samples** section to import only what you need:

- **Example 1** — general starter content
- **VR** and **AR** — XR-oriented examples
- **Physics** — physics experiments
- **WebSocket** — browser communication example
- **Serial Port** — hardware communication example

## Notes

- Open a project scene directly from its folder; most demonstrations are independent from one another.
- Treat the assets as prototypes until they have been profiled and adapted to the requirements of a production project.
- The package is currently version `0.0.2`; serialized fields and scene setup may change between revisions.
