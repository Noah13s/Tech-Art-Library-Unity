using System;
using System.Collections.Generic;
using UnityEngine;

public static class DubinsPath
{
    public enum PathType { LSL, RSR, LSR, RSL, RLR, LRL }

    public struct DubinsParameters
    {
        public float t;
        public float p;
        public float q;
    }

    public struct DubinsResult
    {
        public PathType pathType;
        public DubinsParameters parameters;
        public float totalLength;
    }

    const float TWO_PI = 2 * Mathf.PI;

    // Wrap angle to [0, 2π)
    static float mod2pi(float angle)
    {
        angle = angle % TWO_PI;
        if (angle < 0)
            angle += TWO_PI;
        return angle;
    }

    // Calculate the best Dubins path (in a canonical frame where start=(0,0,0))
    // The goal is given by (x, y, phi), where x,y are in world units and phi in radians.
    public static DubinsResult CalculateDubinsPath(float x, float y, float phi, float radius)
    {
        float D = Mathf.Sqrt(x * x + y * y) / radius;
        float theta = mod2pi(Mathf.Atan2(y, x));
        float alpha = mod2pi(-theta);
        float beta = mod2pi(phi - theta);

        List<DubinsResult> paths = new List<DubinsResult>();
        float t, p, q;

        if (LSL(alpha, beta, D, out t, out p, out q))
        {
            DubinsResult res = new DubinsResult();
            res.pathType = PathType.LSL;
            res.parameters = new DubinsParameters { t = t, p = p, q = q };
            res.totalLength = (t + p + q) * radius;
            paths.Add(res);
        }
        if (RSR(alpha, beta, D, out t, out p, out q))
        {
            DubinsResult res = new DubinsResult();
            res.pathType = PathType.RSR;
            res.parameters = new DubinsParameters { t = t, p = p, q = q };
            res.totalLength = (t + p + q) * radius;
            paths.Add(res);
        }
        if (LSR(alpha, beta, D, out t, out p, out q))
        {
            DubinsResult res = new DubinsResult();
            res.pathType = PathType.LSR;
            res.parameters = new DubinsParameters { t = t, p = p, q = q };
            res.totalLength = (t + p + q) * radius;
            paths.Add(res);
        }
        if (RSL(alpha, beta, D, out t, out p, out q))
        {
            DubinsResult res = new DubinsResult();
            res.pathType = PathType.RSL;
            res.parameters = new DubinsParameters { t = t, p = p, q = q };
            res.totalLength = (t + p + q) * radius;
            paths.Add(res);
        }
        if (RLR(alpha, beta, D, out t, out p, out q))
        {
            DubinsResult res = new DubinsResult();
            res.pathType = PathType.RLR;
            res.parameters = new DubinsParameters { t = t, p = p, q = q };
            res.totalLength = (t + p + q) * radius;
            paths.Add(res);
        }
        if (LRL(alpha, beta, D, out t, out p, out q))
        {
            DubinsResult res = new DubinsResult();
            res.pathType = PathType.LRL;
            res.parameters = new DubinsParameters { t = t, p = p, q = q };
            res.totalLength = (t + p + q) * radius;
            paths.Add(res);
        }
        if (paths.Count == 0)
            throw new Exception("No valid Dubins path found.");

        DubinsResult best = paths[0];
        foreach (var res in paths)
        {
            if (res.totalLength < best.totalLength)
                best = res;
        }
        return best;
    }

    // -- Dubins formulas (see [1], [2]) --

    static bool LSL(float alpha, float beta, float D, out float t, out float p, out float q)
    {
        float tmp = D + Mathf.Sin(alpha) - Mathf.Sin(beta);
        float squared = 2 + (D * D) - 2 * Mathf.Cos(alpha - beta) + 2 * D * (Mathf.Sin(alpha) - Mathf.Sin(beta));
        if (squared < 0)
        {
            t = p = q = 0;
            return false;
        }
        p = Mathf.Sqrt(squared);
        t = mod2pi(-alpha + Mathf.Atan2((Mathf.Cos(beta) - Mathf.Cos(alpha)), tmp));
        q = mod2pi(beta - Mathf.Atan2((Mathf.Cos(beta) - Mathf.Cos(alpha)), tmp));
        return true;
    }

    static bool RSR(float alpha, float beta, float D, out float t, out float p, out float q)
    {
        float tmp = D - Mathf.Sin(alpha) + Mathf.Sin(beta);
        float squared = 2 + (D * D) - 2 * Mathf.Cos(alpha - beta) + 2 * D * (Mathf.Sin(beta) - Mathf.Sin(alpha));
        if (squared < 0)
        {
            t = p = q = 0;
            return false;
        }
        p = Mathf.Sqrt(squared);
        t = mod2pi(alpha - Mathf.Atan2((Mathf.Cos(alpha) - Mathf.Cos(beta)), tmp));
        q = mod2pi(-beta + Mathf.Atan2((Mathf.Cos(alpha) - Mathf.Cos(beta)), tmp));
        return true;
    }

    static bool LSR(float alpha, float beta, float D, out float t, out float p, out float q)
    {
        float squared = -2 + (D * D) + 2 * Mathf.Cos(alpha - beta) + 2 * D * (Mathf.Sin(alpha) + Mathf.Sin(beta));
        if (squared < 0)
        {
            t = p = q = 0;
            return false;
        }
        p = Mathf.Sqrt(squared);
        float tmp = Mathf.Atan2(-Mathf.Cos(alpha) - Mathf.Cos(beta), D + Mathf.Sin(alpha) + Mathf.Sin(beta));
        t = mod2pi(-alpha + tmp);
        q = mod2pi(-mod2pi(beta) + tmp);
        return true;
    }

    static bool RSL(float alpha, float beta, float D, out float t, out float p, out float q)
    {
        float squared = -2 + (D * D) + 2 * Mathf.Cos(alpha - beta) - 2 * D * (Mathf.Sin(alpha) + Mathf.Sin(beta));
        if (squared < 0)
        {
            t = p = q = 0;
            return false;
        }
        p = Mathf.Sqrt(squared);
        float tmp = Mathf.Atan2(Mathf.Cos(alpha) + Mathf.Cos(beta), D - Mathf.Sin(alpha) - Mathf.Sin(beta));
        t = mod2pi(alpha - tmp);
        q = mod2pi(beta - tmp);
        return true;
    }

    static bool RLR(float alpha, float beta, float D, out float t, out float p, out float q)
    {
        float tmp = (6 - D * D + 2 * Mathf.Cos(alpha - beta) + 2 * D * (Mathf.Sin(alpha) - Mathf.Sin(beta))) / 8f;
        if (Mathf.Abs(tmp) > 1)
        {
            t = p = q = 0;
            return false;
        }
        p = mod2pi(2 * Mathf.Acos(tmp));
        t = mod2pi(alpha - Mathf.Atan2(Mathf.Cos(alpha) - Mathf.Cos(beta), D - Mathf.Sin(alpha) + Mathf.Sin(beta)) + p / 2f);
        q = mod2pi(alpha - beta - t + mod2pi(p));
        return true;
    }

    static bool LRL(float alpha, float beta, float D, out float t, out float p, out float q)
    {
        float tmp = (6 - D * D + 2 * Mathf.Cos(alpha - beta) - 2 * D * (Mathf.Sin(alpha) - Mathf.Sin(beta))) / 8f;
        if (Mathf.Abs(tmp) > 1)
        {
            t = p = q = 0;
            return false;
        }
        p = mod2pi(2 * Mathf.Acos(tmp));
        t = mod2pi(-alpha - Mathf.Atan2(Mathf.Cos(alpha) - Mathf.Cos(beta), D + Mathf.Sin(alpha) - Mathf.Sin(beta)) + p / 2f);
        q = mod2pi(mod2pi(beta) - alpha - t + mod2pi(p));
        return true;
    }

    // ======================================================
    // The following functions sample the computed Dubins path.
    // The algorithm works in a canonical frame (start=(0,0,0)) and
    // then transforms points back into world coordinates.
    // ------------------------------------------------------

    // Call this to get a list of waypoints (on the XZ plane) between start and goal.
    // start and end: positions (as Vector2) in world units.
    // startAngle & endAngle: orientations in radians.
    // radius: minimum turning radius.
    // stepSize: approximate distance between waypoints.
    public static List<Vector3> GetPathPoints(Vector2 start, float startAngle, Vector2 end, float endAngle, float radius, float stepSize)
    {
        // Transform to canonical frame (start at (0,0,0) with angle 0)
        float dx = end.x - start.x;
        float dy = end.y - start.y;
        float cosTheta = Mathf.Cos(startAngle);
        float sinTheta = Mathf.Sin(startAngle);
        float x = cosTheta * dx + sinTheta * dy;
        float y = -sinTheta * dx + cosTheta * dy;
        float phi = mod2pi(endAngle - startAngle);

        // Compute best Dubins path in canonical frame
        DubinsResult dubins = CalculateDubinsPath(x, y, phi, radius);

        // Sample the canonical path (each segment is sampled separately)
        List<Vector2> pathCanonical = new List<Vector2>();
        Pose2D config = new Pose2D(new Vector2(0, 0), 0);

        // First segment
        char seg1 = GetMotion(dubins.pathType, 0);
        pathCanonical.AddRange(SampleSegment(config, seg1, dubins.parameters.t, radius, stepSize));
        config = SegmentEndpoint(config, seg1, dubins.parameters.t, radius);

        // Second segment (always straight)
        char seg2 = GetMotion(dubins.pathType, 1);
        pathCanonical.AddRange(SampleSegment(config, seg2, dubins.parameters.p, radius, stepSize));
        config = SegmentEndpoint(config, seg2, dubins.parameters.p, radius);

        // Third segment
        char seg3 = GetMotion(dubins.pathType, 2);
        pathCanonical.AddRange(SampleSegment(config, seg3, dubins.parameters.q, radius, stepSize));
        // (Endpoint need not be used here)

        // Transform canonical points back to world frame
        List<Vector3> pathWorld = new List<Vector3>();
        foreach (Vector2 pt in pathCanonical)
        {
            float wx = cosTheta * pt.x - sinTheta * pt.y + start.x;
            float wy = sinTheta * pt.x + cosTheta * pt.y + start.y;
            pathWorld.Add(new Vector3(wx, 0, wy));
        }
        return pathWorld;
    }

    // A simple 2D pose structure
    public struct Pose2D
    {
        public Vector2 position;
        public float angle; // in radians
        public Pose2D(Vector2 pos, float ang) { position = pos; angle = ang; }
    }

    // Return the motion type for the given segment of the path.
    // It returns 'L', 'R', or 'S' for left arc, right arc, or straight.
    static char GetMotion(PathType type, int segment)
    {
        switch (type)
        {
            case PathType.LSL:
                return (segment == 1) ? 'S' : 'L';
            case PathType.RSR:
                return (segment == 1) ? 'S' : 'R';
            case PathType.LSR:
                return (segment == 0) ? 'L' : (segment == 1 ? 'S' : 'R');
            case PathType.RSL:
                return (segment == 0) ? 'R' : (segment == 1 ? 'S' : 'L');
            case PathType.RLR:
                return (segment == 1) ? 'L' : 'R';
            case PathType.LRL:
                return (segment == 1) ? 'R' : 'L';
            default:
                return 'S';
        }
    }

    // Sample points along a segment given starting configuration, motion type,
    // segment parameter (t, p, or q) and step size.
    static List<Vector2> SampleSegment(Pose2D start, char motion, float segParam, float radius, float stepSize)
    {
        List<Vector2> points = new List<Vector2>();
        // For a straight segment the actual length = segParam * radius;
        // for an arc, segParam is an angle in radians.
        float segLength = (motion == 'S') ? segParam * radius : segParam;
        int numSamples = Mathf.Max(2, Mathf.CeilToInt(segLength / stepSize));
        for (int i = 0; i <= numSamples; i++)
        {
            float fraction = (float)i / numSamples;
            float param = segParam * fraction;
            Pose2D pose = IntegrateSegment(start, motion, param, radius);
            points.Add(pose.position);
        }
        return points;
    }

    // Compute the endpoint of a segment.
    static Pose2D SegmentEndpoint(Pose2D start, char motion, float segParam, float radius)
    {
        return IntegrateSegment(start, motion, segParam, radius);
    }

    // Integrate the motion for a given segment.
    // For 'L' or 'R', param is an angle (radians); for 'S', param is distance/ radius.
    static Pose2D IntegrateSegment(Pose2D start, char motion, float param, float radius)
    {
        Pose2D result = start;
        if (motion == 'S')
        {
            float distance = param * radius;
            result.position += new Vector2(Mathf.Cos(start.angle), Mathf.Sin(start.angle)) * distance;
        }
        else if (motion == 'L')
        {
            float dtheta = param;
            Vector2 center = start.position + new Vector2(-Mathf.Sin(start.angle), Mathf.Cos(start.angle)) * radius;
            float startAngle = start.angle - Mathf.PI / 2;
            float newAngle = startAngle + dtheta;
            result.position = center + new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * radius;
            result.angle = mod2pi(start.angle + dtheta);
        }
        else if (motion == 'R')
        {
            float dtheta = param;
            Vector2 center = start.position + new Vector2(Mathf.Sin(start.angle), -Mathf.Cos(start.angle)) * radius;
            float startAngle = start.angle + Mathf.PI / 2;
            float newAngle = startAngle - dtheta;
            result.position = center + new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * radius;
            result.angle = mod2pi(start.angle - dtheta);
        }
        return result;
    }
}
