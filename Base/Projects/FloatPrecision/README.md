# Float Precision

Float Precision is a large-scale planetary simulation demo. It keeps astronomical positions and velocity in double precision while rendering the player near Unity's origin, allowing travel from interplanetary distances down to a detailed local surface without storing huge values in scene transforms.

The main scene is [`FloatPrecision.unity`](FloatPrecision.unity).

## Highlights

- Double-precision simulation coordinates with camera-relative rendering
- Perspective-compressed planets that keep a useful apparent size at extreme distances
- A smooth transition to true local scale near a planet
- A generated, displaced surface patch for close flight and ground contact
- Inverse-square gravity from multiple celestial bodies
- Camera-relative atmospheric scattering and simulation-space sunlight
- Perspective-aware volumetric clouds for Earth
- A navigable 3D map with textured planets and the player's real mesh
- Gravity-aware trajectory prediction with impact and prediction-limit markers
- Time controls available in both the flight view and navigation map
- Optional WebSocket browser visualization

## Getting started

1. Open [`FloatPrecision.unity`](FloatPrecision.unity).
2. Enter Play Mode. The player begins 20,000 km above Earth's sun-facing surface.
3. Use the speed presets in the flight HUD, then approach Earth.
4. Press **Tab** to inspect the system in the navigation map.
5. Enable `Velocity Active` on the player to simulate inertial motion and gravity rather than direct positional movement.

The scene uses the Input System package. It does not require the legacy input API.

## Flight controls

| Input | Action |
| --- | --- |
| `W` / `S` | Pitch |
| `A` / `D` | Yaw |
| `Q` / `E` | Roll |
| `Shift` | Move forward, or add forward velocity in velocity mode |
| `Ctrl` | Move backward, or add reverse velocity in velocity mode |
| Right mouse drag | Orbit/look around the player |
| `Tab` | Open or close the navigation map |

The **Atmosphere Speed**, **Space**, and **Spacex2** buttons change the movement or acceleration increment without modifying the double-precision position directly.

## Time controls

The flight view and navigation map both provide slower, pause/resume, faster, and `1x` controls. Available running scales are `0.1x`, `0.25x`, `0.5x`, `1x`, `2x`, `4x`, `8x`, `16x`, `32x`, and `64x`.

| Input | Action |
| --- | --- |
| `[` | Use the next slower time scale |
| `]` | Use the next faster time scale |
| `\` | Pause or resume |
| `Backspace` | Reset to `1x` |

Time controls remain responsive while paused because the interface and map navigation use unscaled time. Higher time scales also enlarge the fixed simulation step to keep the real-time physics workload bounded; very fast warp is therefore intended for travel, not precise low-altitude maneuvering.

## Navigation map

The map presents simulation-space positions rather than the perspective-compressed render transforms used by the flight camera. Planet and player marker sizes are adjusted separately from distance scale so close objects remain legible without cluttering a system overview.

| Input | Action |
| --- | --- |
| Right mouse drag | Orbit around the focus point |
| Middle mouse drag or `Shift` + right mouse drag | Pan |
| Mouse wheel | Zoom |
| `W`, `A`, `S`, `D` | Pan with the keyboard |
| Click a marker or use the object list | Select an object |
| `F` | Focus the current selection |
| `Home` | Restore the system overview |
| `Tab` | Return to flight |

Map orientation, focus, and zoom persist when it is closed and reopened. While velocity mode is active, the cyan trajectory uses the same celestial masses as the live simulation and integrates future gravity with adaptive substeps. It stops when the player intersects a body or when the finite prediction horizon is reached; the endpoint label identifies which occurred. The line is rendered two-sided so it remains visible from either side of its orbital plane.

## How the scale illusion works

[`FloatPrecisionPlayer`](Scripts/FloatPrecisionPlayer.cs) owns the authoritative double-precision position and velocity. [`PerspectiveIllusionObject`](Scripts/PerspectiveIllusionObject.cs) subtracts the player's simulation position from each body's simulation position and converts the result into a stable local representation.

Earth transitions from its compressed far representation to true local scale between roughly 80 km and 5 km altitude. Position, scale, atmosphere, and close-up surface use the same eased render state so the handoff does not visibly resize the planet.

Within the close-up range, [`SphereSurfacePatchGenerator`](Scripts/SphereSurfacePatchGenerator.cs) displays a curved displaced patch and provides local ground contact. The coarse planet renderer is hidden there to avoid overlapping surfaces, z-fighting, and unstable large-scale shadows.

Atmospheric scattering uses a separate camera-relative proxy capped near the far planet's render scale. Scene depth and camera positions are converted into that proxy together, keeping the atmosphere aligned while avoiding million-unit shader calculations.

## Volumetric clouds

Earth uses an adapted version of [UnityVolumetricCloudsURP](https://github.com/jiaozi158/UnityVolumetricCloudsURP). The renderer, shader, noise textures, and original MIT notice are stored in `VolumetricClouds` so the project has no external runtime download.

[`PlanetVolumetricCloudsController`](Scripts/PlanetVolumetricCloudsController.cs) creates the global cloud volume and exposes the initial Earth tuning on the Earth object. Altitude and wind remain physical metre-based values. At runtime, the controller supplies the cloud renderer with Earth's current perspective-proxy center, radius, and metres-to-render-units scale. Cloud shell thickness, noise frequency, wind displacement, ray steps, and optical density are converted together so the layer remains consistent across distance bands.

The renderer combines detailed local volumetric noise with a unique procedural weather field generated once for the entire planet. Domain-warped multi-scale noise, latitude flow, and procedural vortices create fronts and spiral systems without repeating the small 3D detail texture across the globe. `Planet-Wide Weather` controls determine coverage, contrast, seed, map resolution, and close-range influence. Between 75 km and 750 km altitude, small volumetric erosion features smoothly give way to the planetary field; above 750 km no tiled shape or erosion source remains in the render.

The initial profile uses a 1.5–7 km layer, broad low-frequency formations, 35 km/h wind, 48 primary steps, and four light steps. Close flight uses 65%-resolution bilateral rendering; the renderer smoothly increases to full resolution from orbit to avoid aliasing the compressed cloud shell. Cloud-cookie ground shadows are disabled for this first pass because the upstream renderer replaces the main directional-light cookie; they should be enabled only after integrating them with the existing local shadow solution.

## Gravity and lighting

Each [`PlanetGravityHandler`](Scripts/PlanetGravityHandler.cs) contributes inverse-square acceleration from its body's configured mass while velocity mode is enabled. The player's velocity is stored in metres per second and integrated over time.

[`SimulationSunLightController`](Scripts/SimulationSunLightController.cs) turns the simulation-space direction to the Sun into a stable directional light. Planet-scale render proxies do not cast normal real-time shadows because their artificial scale would create camera-sized spherical shadows. The local ground patch receives the player's nearby shadow instead, and the player avoids unstable self-shadow reception at astronomical precision.

## Project structure

| Path | Responsibility |
| --- | --- |
| [`Scripts/DoubleVector3.cs`](Scripts/DoubleVector3.cs) | Double-precision vector math |
| [`Scripts/FloatPrecisionPlayer.cs`](Scripts/FloatPrecisionPlayer.cs) | Player position, orientation, thrust, and velocity |
| [`Scripts/PerspectiveIllusionObject.cs`](Scripts/PerspectiveIllusionObject.cs) | Simulation-to-render-space mapping |
| [`Scripts/SphereSurfacePatchGenerator.cs`](Scripts/SphereSurfacePatchGenerator.cs) | Close-up displaced terrain and ground contact |
| [`Scripts/PlanetGravityHandler.cs`](Scripts/PlanetGravityHandler.cs) | Celestial mass and gravitational acceleration |
| [`Scripts/AtmosphereHandler.cs`](Scripts/AtmosphereHandler.cs) | Atmosphere alignment and scale synchronization |
| [`Scripts/PlanetVolumetricCloudsController.cs`](Scripts/PlanetVolumetricCloudsController.cs) | Earth cloud volume and perspective-scale synchronization |
| [`Scripts/SimulationSunLightController.cs`](Scripts/SimulationSunLightController.cs) | Stable Sun-relative lighting |
| [`Scripts/SimulationMapController.cs`](Scripts/SimulationMapController.cs) | Runtime map, trajectory predictor, object details, and time controls |
| `Atmosphere/Runtime` | URP render feature, render pass, shaders, and depth support |
| `Atmosphere/Profiles` | Atmosphere profile assets |
| `VolumetricClouds` | Adapted URP cloud renderer, shaders, noise assets, and license |
| `Planets` | Planet materials, textures, and visual assets |
| `WebInterface` | Optional Node.js and WebSocket visualization |

## Web interface

Node dependencies are generated locally and are intentionally not committed. From `WebInterface`, run:

```sh
npm install
npm start
```

The scene can also start `server.js` through `NodeServerRunner`. Node.js must be available on the system `PATH`. If the browser view is not needed, disable both `NodeServerRunner` and `WebSocketPlayerSender` in the scene.

## Design constraints

- The simulation values are astronomical, but Unity rendering and physics remain local and single precision.
- Planet size, atmosphere, terrain, and shadows are intentionally faked at different distance bands.
- The navigation trajectory is a prediction over a finite horizon, not a permanent orbit trail.
- High time warp trades simulation precision for faster travel.
- The close-up terrain is a local patch, not a globally streamed planetary terrain system.
