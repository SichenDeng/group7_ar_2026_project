using System.Collections.Generic;
using UnityEngine;

namespace ARFunctionLab
{
    /// <summary>
    /// Draws the intersection of the surface with a cutting plane: a highlighted curve plus a
    /// translucent quad showing where the cut was taken. Lives in the same local space as the
    /// generated surface mesh, so sampled points need no extra transform.
    /// </summary>
    public class CrossSectionDisplay : MonoBehaviour
    {
        public Material lineMaterial;
        public static readonly Color CurveColor = new Color(1f, 0.62f, 0.17f);
        public static readonly Color BreakColor = new Color(0.93f, 0.26f, 0.26f);
        private static readonly Color PlaneColor = new Color(1f, 0.72f, 0.34f, 0.15f);
        /// <summary>The most recent sample, so the panel can explain an incomplete curve.</summary>
        public CrossSectionCurve LastCurve { get; private set; }
        private Transform graphics;
        private Material curveMaterial;
        private Mesh planeMesh;

        public void Build(
            ParsedFormula formula,
            SurfaceSettings surface,
            IDictionary<string, double> parameters,
            CrossSectionSettings cross,
            Bounds surfaceBounds)
        {
            Clear();
            LastCurve = null;
            if (cross == null || !cross.enabled || surface == null) return;
            CrossSectionCurve curve = CrossSectionSampler.Sample(
                formula, surface, parameters, cross.axis, cross.value, cross.samples);
            LastCurve = curve;
            // Break markers are still worth drawing when no run was long enough to be a line.
            if (curve.Segments.Count == 0 && curve.Breaks.Count == 0) return;

            graphics = new GameObject("Cross section").transform;
            graphics.SetParent(transform, false);
            float span = Mathf.Max(
                (float)(surface.xMax - surface.xMin),
                (float)(surface.yMax - surface.yMin));
            BuildPlane(surface, cross, surfaceBounds, span);
            for (int i = 0; i < curve.Segments.Count; i++) AddCurve(curve.Segments[i], span, i);
            for (int i = 0; i < curve.Breaks.Count; i++) AddBreakMarker(curve.Breaks[i], span, i);
        }

        public void Clear()
        {
            if (graphics != null)
            {
                graphics.gameObject.SetActive(false);
                Destroy(graphics.gameObject);
                graphics = null;
            }
            if (planeMesh != null)
            {
                Destroy(planeMesh);
                planeMesh = null;
            }
        }

        /// <summary>Matches the z extent GraphDisplay uses for the vertical axis.</summary>
        private static void HeightRange(Bounds surfaceBounds, float span, out float zMin, out float zMax)
        {
            zMin = Mathf.Min(0, surfaceBounds.min.y) - span * 0.04f;
            zMax = Mathf.Max(0, surfaceBounds.max.y) + span * 0.08f;
        }

        private void BuildPlane(SurfaceSettings surface, CrossSectionSettings cross, Bounds surfaceBounds, float span)
        {
            float zMin;
            float zMax;
            HeightRange(surfaceBounds, span, out zMin, out zMax);
            float freeMinValue;
            float freeMaxValue;
            if (cross.axis == CrossSectionAxis.X)
            {
                freeMinValue = (float)surface.yMin;
                freeMaxValue = (float)surface.yMax;
            }
            else
            {
                freeMinValue = (float)surface.xMin;
                freeMaxValue = (float)surface.xMax;
            }
            float fixedValue = (float)cross.value;

            Vector3[] corners = new Vector3[4];
            if (cross.axis == CrossSectionAxis.X)
            {
                // Plane x = value: spans math y (Unity z) and height (Unity y).
                corners[0] = new Vector3(fixedValue, zMin, freeMinValue);
                corners[1] = new Vector3(fixedValue, zMax, freeMinValue);
                corners[2] = new Vector3(fixedValue, zMax, freeMaxValue);
                corners[3] = new Vector3(fixedValue, zMin, freeMaxValue);
            }
            else
            {
                // Plane y = value: spans math x (Unity x) and height (Unity y).
                corners[0] = new Vector3(freeMinValue, zMin, fixedValue);
                corners[1] = new Vector3(freeMinValue, zMax, fixedValue);
                corners[2] = new Vector3(freeMaxValue, zMax, fixedValue);
                corners[3] = new Vector3(freeMaxValue, zMin, fixedValue);
            }

            planeMesh = new Mesh();
            planeMesh.name = "Cross section plane";
            planeMesh.vertices = corners;
            planeMesh.colors = new[] { PlaneColor, PlaneColor, PlaneColor, PlaneColor };
            planeMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            planeMesh.RecalculateBounds();

            GameObject item = new GameObject("Plane", typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(graphics, false);
            item.GetComponent<MeshFilter>().sharedMesh = planeMesh;
            // Line.shader is transparent, vertex coloured and double-sided (Cull Off), which is
            // exactly what a translucent quad needs.
            item.GetComponent<MeshRenderer>().sharedMaterial = lineMaterial;
        }

        private void AddCurve(List<Vector3> points, float span, int index)
        {
            GameObject item = new GameObject("Curve " + index);
            item.transform.SetParent(graphics, false);
            LineRenderer line = item.AddComponent<LineRenderer>();
            line.sharedMaterial = CurveMaterial();
            line.useWorldSpace = false;
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
            float width = span * 0.005f;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = CurveColor;
            line.endColor = CurveColor;
            line.numCornerVertices = 2;
        }

        /// <summary>
        /// Marks where the curve meets a gap, so a discontinuity reads as "undefined here" rather
        /// than as a curve that quietly stops.
        /// </summary>
        private void AddBreakMarker(Vector3 position, float span, int index)
        {
            GameObject item = new GameObject("Break " + index);
            item.transform.SetParent(graphics, false);
            LineRenderer line = item.AddComponent<LineRenderer>();
            line.sharedMaterial = CurveMaterial();
            line.useWorldSpace = false;
            line.positionCount = 2;
            float half = span * 0.035f;
            line.SetPosition(0, position + Vector3.down * half);
            line.SetPosition(1, position + Vector3.up * half);
            float width = span * 0.008f;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = BreakColor;
            line.endColor = BreakColor;
        }

        /// <summary>
        /// The curve lies exactly on the surface, so sharing the surface's depth test would make it
        /// z-fight. A copy of the line material that ignores depth keeps it readable from any angle.
        /// </summary>
        private Material CurveMaterial()
        {
            if (curveMaterial != null) return curveMaterial;
            curveMaterial = new Material(lineMaterial);
            curveMaterial.name = "Cross section curve";
            curveMaterial.hideFlags = HideFlags.HideAndDontSave;
            curveMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            return curveMaterial;
        }

        private void OnDestroy()
        {
            Clear();
            if (curveMaterial != null) Destroy(curveMaterial);
        }
    }
}
