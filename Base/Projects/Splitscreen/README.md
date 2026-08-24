# Splitscreen

A local-multiplayer layout prototype that arranges multiple cameras into a normalized screen grid and visualizes connected Input System gamepads.

## Open the demo

Open [`Splitscreen.unity`](Splitscreen.unity) and enter Play Mode. The scene contains six sample cameras, a player-count slider, a screen grid, and a device list.

## Main components

- [`SplitScreenGrid`](../../Scripts/SplitScreenGrid.cs) calculates camera viewports from the assigned camera array and supports replacing that array at runtime.
- [`DynamicSplitscreenGridLayoutGroup`](../../Scripts/UI/DynamicSplitScreenGridLayoutGroup.cs) lays out one to four UI views, including the asymmetric three-player arrangement.
- [`LocalMultiplayer`](../../Scripts/LocalMultiplayer.cs) reads `Gamepad.all` from the Input System and creates a UI entry for each connected pad.
- [`Instantiator`](../../Scripts/Instantiator.cs) is wired to the scene slider to rebuild the requested number of sample views.

The scene requires the Input System package. Connect controllers before entering Play Mode, or call `DetectGamepads` again after devices change.

## Scope

This demo handles presentation and discovery. It does not pair input devices to spawned players or manage player joining, leaving, and per-player action maps.
