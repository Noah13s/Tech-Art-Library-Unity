# Float Precision

This demo keeps astronomical positions in double precision while rendering the player near Unity's origin. Planet objects convert their simulation-space offset into a stable local representation, and `EarthClose-up` generates the detailed ground patch used near the surface.

## Structure

- `Scripts/DoubleVector3.cs` contains the double-precision math type.
- `Scripts/FloatPrecisionPlayer.cs` owns the player's simulation position and velocity.
- `Scripts/PerspectiveIllusionObject.cs` maps a planet from simulation space into render space.
- `Scripts/SimulationSunLightController.cs` aligns the scene's directional light with the simulation-space Sun and active planet.
- `Scripts/SphereSurfacePatchGenerator.cs` creates the close-up displaced surface and handles direct ground contact.
- `Scripts/PlanetGravityHandler.cs` applies inverse-square gravity in simulation space.
- `Scripts/AtmosphereHandler.cs` keeps the atmosphere radius aligned with the rendered planet.
- `Planets/` contains project-specific visual assets; `Atmosphere/Runtime/` and `Atmosphere/Profiles/` contain the retained atmosphere implementation and active profile.
- `WebInterface/` contains the optional browser visualization and its Unity bridge.

The active scene is `FloatPrecision.unity`. The old standalone surface experiment and inactive test planets were removed; use the active `Earth` and `EarthClose-up` objects as the reference setup.

The player starts 20,000 km above Earth's sun-facing surface. The initial orbit camera looks toward Earth, and the player uses a lit material with a very low emission floor so direct sunlight remains the dominant contribution.

Within 5 km of the surface, `SphereSurfacePatchGenerator` performs an explicit LOD swap: it hides the coarse Earth renderer and renders only the local curved patch. Perspective-compressed celestial meshes do not cast real-time shadows because their fake render scale produces invalid, camera-sized spherical shadows. The normally lit close-up patch receives only the player's local shadow; the player does not receive shadows to avoid self-shadow precision artifacts.

## Web interface

Node dependencies are generated locally and are intentionally not committed. From `WebInterface/`, run:

```sh
npm install
npm start
```

The scene can also start `server.js` through `NodeServerRunner`. Node.js must be available on the system `PATH`. If the browser visualization is not needed, disable both `NodeServerRunner` and `WebSocketPlayerSender` in the scene.
