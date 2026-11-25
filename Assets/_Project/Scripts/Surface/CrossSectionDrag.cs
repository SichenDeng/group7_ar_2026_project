using System;
using UnityEngine;

namespace ARFunctionLab
{
    /// <summary>
    /// Turns a pointer ray into a position for the cutting plane. The plane slides along one axis
    /// only, so the useful answer is the point on that axis closest to the ray: a view-independent
    /// result that behaves the same for a controller ray, a hand ray and a mouse ray.
    /// Every vector here is in the graph's local space, which keeps the maths free of the scene.
    /// </summary>
    public static class CrossSectionDrag
    {
        /// <summary>Unit direction the plane slides along. Math y is Unity z.</summary>
        public static Vector3 SlideDirection(CrossSectionAxis axis)
        {
            return axis == CrossSectionAxis.X ? Vector3.right : Vector3.forward;
        }

        /// <summary>The component of a local point along the slide axis.</summary>
        public static float Coordinate(Vector3 localPoint, CrossSectionAxis axis)
        {
            return axis == CrossSectionAxis.X ? localPoint.x : localPoint.z;
        }

        /// <summary>
        /// Closest point between the pointer ray and the line the plane slides along, returned as a
        /// coordinate on the slide axis. Fails when the ray runs parallel to that axis, where the
        /// closest point is undefined and the plane should simply stay put.
        /// </summary>
        public static bool TryResolve(
            Vector3 rayOrigin,
            Vector3 rayDirection,
            Vector3 anchor,
            CrossSectionAxis axis,
            out double value)
        {
            value = 0;
            if (rayDirection.sqrMagnitude < 1e-12f) return false;
            Vector3 slide = SlideDirection(axis);
            Vector3 ray = rayDirection.normalized;

            // Standard closest approach of two lines, with both directions already unit length.
            Vector3 between = anchor - rayOrigin;
            float b = Vector3.Dot(slide, ray);
            float denominator = 1f - b * b;
            if (Mathf.Abs(denominator) < 1e-6f) return false;

            float d = Vector3.Dot(slide, between);
            float e = Vector3.Dot(ray, between);
            float t = (b * e - d) / denominator;
            if (float.IsNaN(t) || float.IsInfinity(t)) return false;

            value = Coordinate(anchor, axis) + t;
            return true;
        }

        /// <summary>
        /// Pulls the value onto the nearest grid tick once it is within <paramref name="threshold"/>
        /// of one, so a free drag can still come to rest on a labelled gridline.
        /// </summary>
        public static double Snap(double value, double step, double threshold)
        {
            if (step <= 0 || !FunctionCatalog.IsFinite(step) || !FunctionCatalog.IsFinite(value)) return value;
            double nearest = Math.Round(value / step) * step;
            return Math.Abs(value - nearest) <= step * threshold ? nearest : value;
        }
    }
}
