using System;
using UnityEngine;

/// <summary>
/// A double-precision vector used for simulation-space positions and velocity.
/// </summary>
[Serializable]
public struct DoubleVector3
{
    public double x;
    public double y;
    public double z;

    public DoubleVector3(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static DoubleVector3 Zero => new(0, 0, 0);

    public static DoubleVector3 operator +(DoubleVector3 a, DoubleVector3 b) =>
        new(a.x + b.x, a.y + b.y, a.z + b.z);

    public static DoubleVector3 operator -(DoubleVector3 a, DoubleVector3 b) =>
        new(a.x - b.x, a.y - b.y, a.z - b.z);

    public static DoubleVector3 operator *(DoubleVector3 vector, double scalar) =>
        new(vector.x * scalar, vector.y * scalar, vector.z * scalar);

    public double Dot(DoubleVector3 other) =>
        x * other.x + y * other.y + z * other.z;

    public double Magnitude() => Math.Sqrt(Dot(this));

    public DoubleVector3 Normalized()
    {
        double magnitude = Magnitude();
        return magnitude > 0 ? this * (1.0 / magnitude) : Zero;
    }

    public DoubleVector3 Negate() => new(-x, -y, -z);

    public DoubleVector3 Cross(DoubleVector3 other) =>
        new(
            y * other.z - z * other.y,
            z * other.x - x * other.z,
            x * other.y - y * other.x
        );

    public static DoubleVector3 Lerp(DoubleVector3 a, DoubleVector3 b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return new DoubleVector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    public static explicit operator Vector3(DoubleVector3 value) =>
        new((float)value.x, (float)value.y, (float)value.z);
}
