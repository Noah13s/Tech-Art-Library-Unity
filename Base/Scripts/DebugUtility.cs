using UnityEditor;
using UnityEngine;

public static class DebugUtility
{
    /// <summary>
    /// Draws a wireframe box in the scene for debugging purposes.
    /// </summary>
    /// <param name="center">The center position of the box in world space.</param>
    /// <param name="normal">The dimensions of the box (width, height, depth).</param>
    /// <param name="color">The color of the box lines.</param>
    /// <param name="from">The duration in seconds for which the box will be visible in the scene view. Set to 0 for a single frame.</param>
    /// <remarks>
    /// This method uses `Debug.DrawLine` to render the edges of the box.
    /// It only appears in the **Scene View** and does not render in **Game View**.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// DebugDraw.DrawBox(Vector3.zero, new Vector3(2, 2, 2), Color.red, 5f);
    /// </code>
    /// </example>
    public static void DrawFilledWedgeGizmo(Vector3 center, Vector3 normal, Vector3 from, float angle, float height, float innerRadius, float outerRadius, Color color)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Vector3 top = center + normal * height;
        int segments = 32;
        float angleStep = angle / segments;

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);

        // Fill the curved surfaces
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;

            Vector3 currentOuter = center + rotation * (Quaternion.AngleAxis(currentAngle, Vector3.up) * from) * outerRadius;
            Vector3 nextOuter = center + rotation * (Quaternion.AngleAxis(nextAngle, Vector3.up) * from) * outerRadius;
            Vector3 currentInner = center + rotation * (Quaternion.AngleAxis(currentAngle, Vector3.up) * from) * innerRadius;
            Vector3 nextInner = center + rotation * (Quaternion.AngleAxis(nextAngle, Vector3.up) * from) * innerRadius;

            // Bottom surface
            GL.Vertex(currentInner);
            GL.Vertex(currentOuter);
            GL.Vertex(nextOuter);

            GL.Vertex(currentInner);
            GL.Vertex(nextOuter);
            GL.Vertex(nextInner);

            // Top surface
            GL.Vertex(currentInner + normal * height);
            GL.Vertex(nextOuter + normal * height);
            GL.Vertex(currentOuter + normal * height);

            GL.Vertex(currentInner + normal * height);
            GL.Vertex(nextInner + normal * height);
            GL.Vertex(nextOuter + normal * height);

            // Side surfaces
            GL.Vertex(currentInner);
            GL.Vertex(currentInner + normal * height);
            GL.Vertex(currentOuter + normal * height);

            GL.Vertex(currentInner);
            GL.Vertex(currentOuter + normal * height);
            GL.Vertex(currentOuter);
        }

        // Fill the end caps
        Vector3 startOuter = center + rotation * from * outerRadius;
        Vector3 startInner = center + rotation * from * innerRadius;
        Vector3 endOuter = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * outerRadius;
        Vector3 endInner = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * innerRadius;

        // Start cap
        GL.Vertex(startInner);
        GL.Vertex(startOuter);
        GL.Vertex(startOuter + normal * height);

        GL.Vertex(startInner);
        GL.Vertex(startOuter + normal * height);
        GL.Vertex(startInner + normal * height);

        // End cap
        GL.Vertex(endInner);
        GL.Vertex(endOuter + normal * height);
        GL.Vertex(endOuter);

        GL.Vertex(endInner);
        GL.Vertex(endInner + normal * height);
        GL.Vertex(endOuter + normal * height);

        GL.End();
        GL.PopMatrix();

        // Draw wireframe arcs
        Handles.DrawWireArc(center, normal, from, angle, outerRadius);
        Handles.DrawWireArc(center, normal, from, angle, innerRadius);
        Handles.DrawWireArc(top, normal, from, angle, outerRadius);
        Handles.DrawWireArc(top, normal, from, angle, innerRadius);

        // Draw lines for edges
        Vector3[] corners = new Vector3[]
        {
        startOuter, startInner, endOuter, endInner,
        startOuter + normal * height, startInner + normal * height,
        endOuter + normal * height, endInner + normal * height
        };

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[4], corners[5]);
        Gizmos.DrawLine(corners[6], corners[7]);

        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[i + 4]);
    }

    public static void DrawWedgeGizmo(Vector3 center, Vector3 normal, Vector3 from, float angle, float height, float innerRadius, float outerRadius)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Vector3 top = center + normal * height;

        // Draw arcs at bottom and top
        Handles.DrawWireArc(center, normal, from, angle, outerRadius);
        Handles.DrawWireArc(center, normal, from, angle, innerRadius);
        Handles.DrawWireArc(top, normal, from, angle, outerRadius);
        Handles.DrawWireArc(top, normal, from, angle, innerRadius);

        // Calculate corner points
        Vector3 startOuter = center + rotation * from * outerRadius;
        Vector3 endOuter = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * outerRadius;
        Vector3 startInner = center + rotation * from * innerRadius;
        Vector3 endInner = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * innerRadius;

        // Draw vertical lines at edges
        Gizmos.DrawLine(startOuter, startOuter + normal * height);
        Gizmos.DrawLine(endOuter, endOuter + normal * height);
        Gizmos.DrawLine(startInner, startInner + normal * height);
        Gizmos.DrawLine(endInner, endInner + normal * height);

        // Draw lines connecting inner and outer arcs
        Gizmos.DrawLine(startInner, startOuter);
        Gizmos.DrawLine(endInner, endOuter);
        Gizmos.DrawLine(startInner + normal * height, startOuter + normal * height);
        Gizmos.DrawLine(endInner + normal * height, endOuter + normal * height);
    }


    public static void DrawFilledCone(Vector3 position, Vector3 direction, float angle, float height, int segments, Color color)
    {
        direction = direction.normalized;
        Vector3 baseCenter = position + direction * height;
        float radius = Mathf.Tan(angle * Mathf.Deg2Rad) * height;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right == Vector3.zero) right = Vector3.right; // Fix edge case
        Vector3 forward = Vector3.Cross(right, direction).normalized;
        Vector3[] sideVertices = new Vector3[3]; // Triangle for sides
        Vector3[] baseVertices = new Vector3[segments + 1]; // Base circle
        Handles.color = color;
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments * Mathf.PI * 2;
            float t2 = (float)(i + 1) / segments * Mathf.PI * 2;
            Vector3 point1 = baseCenter + (right * Mathf.Cos(t1) + forward * Mathf.Sin(t1)) * radius;
            Vector3 point2 = baseCenter + (right * Mathf.Cos(t2) + forward * Mathf.Sin(t2)) * radius;
            // Draw side triangle (tip, point1, point2)
            sideVertices[0] = position;
            sideVertices[1] = point1;
            sideVertices[2] = point2;
            Handles.DrawAAConvexPolygon(sideVertices);
            // Store base vertices
            baseVertices[i] = point1;
        }
        baseVertices[segments] = baseVertices[0]; // Close the base
        // Draw the base cap
        Handles.DrawAAConvexPolygon(baseVertices);
    }
}
