# Projects

This directory contains focused scenes built from the reusable assets in the Tech Art Library. Each project is a prototype and may favor clarity or experimentation over production hardening.

| Project | Main scene | Purpose |
| --- | --- | --- |
| [Assembly Line](AssemblyLine/README.md) | `AssemblyLine/Assembly_Line.unity` | Trigger-driven rigidbody handoffs and runtime prefab assembly |
| [Bullet](Bullet/README.md) | `Bullet/TestRange.unity` | Data-driven ammunition and simplified terminal ballistics |
| [Float Precision](FloatPrecision/README.md) | `FloatPrecision/FloatPrecision.unity` | Large-scale double-precision planetary simulation and rendering |
| [Robotics](Robotics/README.md) | `Robotics/Robotics.unity` | PD balancing, projectile spawning, and wind forces |
| [Splitscreen](Splitscreen/README.md) | `Splitscreen/Splitscreen.unity` | Dynamic camera grids and connected-gamepad UI |
| [UI](UI/README.md) | `UI/Example.unity` | Keyboard and pointer menu navigation with themed UI examples |

The `Scenes` directory contains a general sample scene rather than a separate documented project. The fluid-dynamics scripts at this level are early experiments and do not currently have a dedicated demonstration scene.

## Running a project

1. Open its main scene.
2. Review the scene objects and serialized component references.
3. Enter Play Mode and follow the controls in that project's README.

Float Precision and Splitscreen use the Input System package. Robotics and the custom UI navigation currently use the legacy input API; choose **Both** under **Project Settings > Player > Active Input Handling** when switching between all projects.
