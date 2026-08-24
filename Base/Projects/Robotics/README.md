# Robotics

A compact physics scene combining feedback control and environmental forces. A platform uses a proportional-derivative controller to keep a rigidbody ball near its center, while a launcher and directional wind volume provide disturbances.

## Open the demo

Open [`Robotics.unity`](Robotics.unity), enter Play Mode, aim with the mouse, and press **Space** to create or replace the ball.

This scene currently uses the legacy `UnityEngine.Input` API. Set **Project Settings > Player > Active Input Handling** to **Both** or **Input Manager (Old)** before running it.

## Components

- [`BallBalance`](BallBalance.cs) reads ball position and velocity, then tilts the platform with configurable proportional (`positionGain`) and derivative (`velocityGain`) gains.
- [`BallShooter`](BallShooter.cs) casts through the mouse position and launches the configured ball prefab.
- [`Wind`](Wind.cs) applies force to non-kinematic rigidbodies inside a configurable range and angle; its gizmos show the affected cone.

## Tuning

Start with small gain changes. Raising `positionGain` makes the platform correct position error more aggressively; raising `velocityGain` adds damping. Excessive proportional gain with too little damping will cause oscillation. The platform's maximum turning angle provides a final stability limit.
