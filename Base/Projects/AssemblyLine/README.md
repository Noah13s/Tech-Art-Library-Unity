# Assembly Line

A small physics prototype for passing an object through assembly stations. Trigger events temporarily lock the object's rigidbody, move it to a station target, attach a prefab, and release it after a delay.

## Open the demo

Open [`Assembly_Line.unity`](Assembly_Line.unity) and enter Play Mode. The scene contains two assembly triggers, their lock targets, a ground plane, and sample rigidbody objects.

## Main component

[`AssemblyLineObject`](../../Scripts/AssemblyLine/AssemblyLineObject.cs) stores assembly metadata and exposes the operations used by the scene's trigger events:

- `LockObject` makes the rigidbody kinematic and moves it to a target.
- `AddChildPrefab` instantiates and parents an assembly part.
- `ReleaseObject` returns the rigidbody to physics after a configurable delay.
- `RemoveChildObject` removes an attached part by name.

The scene uses shared event-trigger components to call these methods. When building another station, assign the target transform and callbacks in the trigger's Inspector rather than hard-coding a station sequence.

## Limitations

This is a workflow prototype. It does not yet include conveyor scheduling, inventory validation, persistence, or robust handling for objects entering multiple stations simultaneously.
