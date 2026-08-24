# UI

A collection of menu-navigation experiments and visual references inspired by game interfaces. It includes a graph-based selection system, reusable carousel and slider elements, pointer feedback, and themed GTA V and Minecraft scenes.

## Scenes

| Scene | Focus |
| --- | --- |
| [`Example.unity`](Example.unity) | General navigation and reusable controls |
| [`GTAV/GTAV.unity`](GTAV/GTAV.unity) | Category highlighting and menu-panel switching |
| [`Minecraft/MinecraftUI.unity`](Minecraft/MinecraftUI.unity) | A Minecraft-inspired visual layout |

## Controls

In the general UI example:

- **Arrow keys** move through connected menu nodes.
- **Enter** activates the selected element.
- **Escape** moves back through the navigation tree.
- Pointer hover and press events can also select and interact with `UIControlElement` components.

The custom navigation currently uses the legacy `UnityEngine.Input` API. Set **Active Input Handling** to **Both** or **Input Manager (Old)** to run it.

## Architecture

- [`NodeTreeSystem`](Scripts/Core/NodeTreeSystem.cs) stores UI elements at logical grid positions and provides Editor tooling for building the graph.
- [`UIControlSystem`](Scripts/Core/UIControlSystem.cs) owns selection, directional navigation, activation, and back navigation.
- [`UIControlElement`](Scripts/Core/UIControlElement.cs) is the selectable base component with pointer support.
- [`UILeftRightElement`](Scripts/UILeftRightElement.cs), [`UISliderElement`](Scripts/UISliderElement.cs), and [`Carrousel`](Scripts/Carrousel.cs) implement common adjustable controls.
- The `GTAV/Scripts` folder contains lightweight highlight and menu-display helpers used by that themed scene.

These examples are most useful as references for interaction patterns and visual prototyping. Adapt navigation rules, accessibility states, and input actions before production use.
