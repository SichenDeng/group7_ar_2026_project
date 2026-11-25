using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARFunctionLab
{
    /// <summary>Which coordinate the cutting plane holds fixed.</summary>
    public enum CrossSectionAxis
    {
        /// <summary>The plane is x = value, so the curve runs along y.</summary>
        X,
        /// <summary>The plane is y = value, so the curve runs along x.</summary>
        Y
    }

    [Serializable]
    public class CrossSectionSettings
    {
        public bool enabled;
        public CrossSectionAxis axis = CrossSectionAxis.X;
        public double value;
        public int samples = 256;

        public CrossSectionSettings Copy()
        {
            return (CrossSectionSettings)MemberwiseClone();
        }

        /// <summary>Domain of the coordinate the plane holds fixed.</summary>
        public static void GetFixedRange(SurfaceSettings surface, CrossSectionAxis axis, out double min, out double max)
        {
            if (axis == CrossSectionAxis.X) { min = surface.xMin; max = surface.xMax; }
            else { min = surface.yMin; max = surface.yMax; }
        }

        /// <summary>Domain of the coordinate the curve sweeps across.</summary>
        public static void GetFreeRange(SurfaceSettings surface, CrossSectionAxis axis, out double min, out double max)
        {
            if (axis == CrossSectionAxis.X) { min = surface.yMin; max = surface.yMax; }
            else { min = surface.xMin; max = surface.xMax; }
        }

        /// <summary>Keeps the plane inside the plotted domain after the domain changes.</summary>
        public void ClampToDomain(SurfaceSettings surface)
        {
            double min;
            double max;
            GetFixedRange(surface, axis, out min, out max);
            if (!FunctionCatalog.IsFinite(value)) value = 0;
            value = Math.Max(min, Math.Min(max, value));
        }
    }

    /// <summary>
    /// The sampled cut, together with enough detail to tell the user why a curve is incomplete
    /// instead of silently drawing nothing.
    /// </summary>
    public class CrossSectionCurve
    {
        /// <summary>One polyline per run of consecutive defined samples.</summary>
        public readonly List<List<Vector3>> Segments = new List<List<Vector3>>();
        /// <summary>Points where a drawable run meets a gap, for marking the discontinuity.</summary>
        public readonly List<Vector3> Breaks = new List<Vector3>();
        /// <summary>Samples the formula could not evaluate, such as a division by zero.</summary>
        public int UndefinedSamples;
        /// <summary>Samples that evaluated past the height limit.</summary>
        public int ExcludedSamples;
        /// <summary>True when the plane lies outside the plotted domain, so nothing was sampled.</summary>
        public bool OutsideDomain;

        public bool HasGaps { get { return UndefinedSamples > 0 || ExcludedSamples > 0; } }
        public bool IsEmpty { get { return Segments.Count == 0; } }
    }

    public static class CrossSectionSampler
    {
        public const int MinimumSamples = 2;
        public const int MaximumSamples = 2048;

        /// <summary>
        /// Samples z along the cutting plane. Undefined samples and samples beyond the height limit
        /// break the curve, matching how <see cref="SurfaceGenerator"/> drops those cells from the
        /// mesh, and each boundary between a drawable run and a gap is recorded in
        /// <see cref="CrossSectionCurve.Breaks"/>. Points are in Unity space via
        /// <see cref="GraphCoordinates.ToUnity"/>.
        /// </summary>
        public static CrossSectionCurve Sample(
            ParsedFormula formula,
            SurfaceSettings surface,
            IDictionary<string, double> parameters,
            CrossSectionAxis axis,
            double value,
            int samples)
        {
            CrossSectionCurve curve = new CrossSectionCurve();
            if (formula == null || surface == null) return curve;
            string ignored;
            if (!surface.Validate(out ignored)) return curve;
            if (!FunctionCatalog.IsFinite(value)) return curve;

            double fixedMin;
            double fixedMax;
            CrossSectionSettings.GetFixedRange(surface, axis, out fixedMin, out fixedMax);
            if (value < fixedMin || value > fixedMax)
            {
                curve.OutsideDomain = true;
                return curve;
            }

            double freeMin;
            double freeMax;
            CrossSectionSettings.GetFreeRange(surface, axis, out freeMin, out freeMax);

            int count = Mathf.Clamp(samples, MinimumSamples, MaximumSamples);
            List<Vector3> current = null;
            bool hasPrevious = false;
            bool previousValid = false;
            Vector3 previousPoint = Vector3.zero;

            for (int i = 0; i <= count; i++)
            {
                double t = freeMin + (freeMax - freeMin) * i / count;
                double x = axis == CrossSectionAxis.X ? value : t;
                double y = axis == CrossSectionAxis.X ? t : value;
                double z;
                EvaluationError error;
                bool valid = true;
                if (!formula.TryEvaluate(x, y, parameters, out z, out error))
                {
                    curve.UndefinedSamples++;
                    valid = false;
                }
                else if (Math.Abs(z) > surface.maximumAbsoluteHeight)
                {
                    curve.ExcludedSamples++;
                    valid = false;
                }

                Vector3 point = valid ? GraphCoordinates.ToUnity(x, y, z) : Vector3.zero;
                if (hasPrevious)
                {
                    if (valid && !previousValid) curve.Breaks.Add(point);
                    else if (!valid && previousValid) curve.Breaks.Add(previousPoint);
                }
                hasPrevious = true;
                previousValid = valid;

                if (!valid)
                {
                    current = null;
                    continue;
                }
                previousPoint = point;
                if (current == null)
                {
                    current = new List<Vector3>();
                    curve.Segments.Add(current);
                }
                current.Add(point);
            }

            // A lone point cannot be drawn as a line, but its break marker is still reported.
            for (int i = curve.Segments.Count - 1; i >= 0; i--)
            {
                if (curve.Segments[i].Count < 2) curve.Segments.RemoveAt(i);
            }
            return curve;
        }
    }
}
