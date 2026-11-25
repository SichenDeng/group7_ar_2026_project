using System.Collections.Generic;
using UnityEngine;

namespace ARFunctionLab
{
    /// <summary>
    /// Draws the intersection of the surface with a cutting plane: a highlighted curve, a
    /// translucent quad showing where the cut was taken, and a grab handle on the quad's top edge.
    /// Lives in the same local space as the generated surface mesh, so sampled points need no
    /// extra transform.
    /// Objects are created once and reused, because dragging rebuilds the cut every frame and
    /// creating a GameObject per frame would churn the collector.
    /// </summary>
    public class CrossSectionDisplay : MonoBehaviour
    {
        public Material lineMaterial;
        public static readonly Color CurveColor = new Color(1f, 0.62f, 0.17f);
        public static readonly Color BreakColor = new Color(0.93f, 0.26f, 0.26f);
        private static readonly Color PlaneColor = new Color(1f, 0.72f, 0.34f, 0.15f);
        private static readonly Color HandleColor = new Color(1f, 0.62f, 0.17f, 0.95f);

        /// <summary>The most recent sample, so the panel can explain an incomplete curve.</summary>
        public CrossSectionCurve LastCurve { get; private set; }
        /// <summary>Grab targets, exposed so the interactor can recognise its own colliders.</summary>
        public Collider HandleCollider { get; private set; }
        public Collider PlaneCollider { get; private set; }

        /// <summary>Centre of the grab handle, in this component's local space.</summary>
        public Vector3 HandleCentre { get; private set; }

        private Transform root;
        private Mesh planeMesh;
        private Mesh handleMesh;
        private MeshCollider planeMeshCollider;
        private BoxCollider handleBoxCollider;
        private Material curveMaterial;
        private readonly List<LineRenderer> curvePool = new List<LineRenderer>();
        private readonly List<LineRenderer> breakPool = new List<LineRenderer>();

        public void Build(
            ParsedFormula formula,
            SurfaceSettings surface,
            IDictionary<string, double> parameters,
            CrossSectionSettings cross,
            Bounds surfaceBounds)
        {
            LastCurve = null;
            if (cross == null || !cross.enabled || surface == null)
            {
                if (root != null) root.gameObject.SetActive(false);
                return;
            }

            CrossSectionCurve curve = CrossSectionSampler.Sample(
                formula, surface, parameters, cross.axis, cross.value, cross.samples);
            LastCurve = curve;

            EnsureHierarchy();
            // The plane and its handle stay visible even when nothing is drawable, so the user can
            // always drag back out of an undefined position.
            root.gameObject.SetActive(!curve.OutsideDomain);
            if (curve.OutsideDomain) return;

            float span = Mathf.Max(
                (float)(surface.xMax - surface.xMin),
                (float)(surface.yMax - surface.yMin));
            UpdatePlaneAndHandle(surface, cross, surfaceBounds, span);

            for (int i = 0; i < curve.Segments.Count; i++)
            {
                LineRenderer line = Take(curvePool, i, "Curve ");
                List<Vector3> points = curve.Segments[i];
                line.positionCount = points.Count;
                for (int p = 0; p < points.Count; p++) line.SetPosition(p, points[p]);
                float width = span * 0.005f;
                line.startWidth = width;
                line.endWidth = width;
                line.startColor = CurveColor;
                line.endColor = CurveColor;
                line.numCornerVertices = 2;
            }
            Hide(curvePool, curve.Segments.Count);

            for (int i = 0; i < curve.Breaks.Count; i++)
            {
                LineRenderer line = Take(breakPool, i, "Break ");
                float half = span * 0.035f;
                line.positionCount = 2;
                line.SetPosition(0, curve.Breaks[i] + Vector3.down * half);
                line.SetPosition(1, curve.Breaks[i] + Vector3.up * half);
                float width = span * 0.008f;
                line.startWidth = width;
                line.endWidth = width;
                line.startColor = BreakColor;
                line.endColor = BreakColor;
            }
            Hide(breakPool, curve.Breaks.Count);
        }

        public void Clear()
        {
            LastCurve = null;
            if (root != null) root.gameObject.SetActive(false);
        }

        private void EnsureHierarchy()
        {
            if (root != null) return;
            root = new GameObject("Cross section").transform;
            root.SetParent(transform, false);

            planeMesh = NewQuadMesh("Cross section plane", PlaneColor);
            GameObject plane = new GameObject("Plane", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            plane.transform.SetParent(root, false);
            plane.GetComponent<MeshFilter>().sharedMesh = planeMesh;
            plane.GetComponent<MeshRenderer>().sharedMaterial = lineMaterial;
            planeMeshCollider = plane.GetComponent<MeshCollider>();
            PlaneCollider = planeMeshCollider;

            handleMesh = NewQuadMesh("Cross section handle", HandleColor);
            GameObject grip = new GameObject("Handle", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            grip.transform.SetParent(root, false);
            grip.GetComponent<MeshFilter>().sharedMesh = handleMesh;
            grip.GetComponent<MeshRenderer>().sharedMaterial = CurveMaterial();
            handleBoxCollider = grip.GetComponent<BoxCollider>();
            HandleCollider = handleBoxCollider;
        }

        private static Mesh NewQuadMesh(string name, Color color)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new Vector3[4];
            mesh.colors = new[] { color, color, color, color };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }

        /// <summary>Matches the z extent GraphDisplay uses for the vertical axis.</summary>
        private static void HeightRange(Bounds surfaceBounds, float span, out float zMin, out float zMax)
        {
            zMin = Mathf.Min(0, surfaceBounds.min.y) - span * 0.04f;
            zMax = Mathf.Max(0, surfaceBounds.max.y) + span * 0.08f;
        }

        private void UpdatePlaneAndHandle(
            SurfaceSettings surface, CrossSectionSettings cross, Bounds surfaceBounds, float span)
        {
            float zMin;
            float zMax;
            HeightRange(surfaceBounds, span, out zMin, out zMax);
            bool alongX = cross.axis == CrossSectionAxis.X;
            float freeMin = (float)(alongX ? surface.yMin : surface.xMin);
            float freeMax = (float)(alongX ? surface.yMax : surface.xMax);
            float fixedValue = (float)cross.value;
            float middle = (freeMin + freeMax) * 0.5f;

            Vector3[] corners = new Vector3[4];
            if (alongX)
            {
                // Plane x = value: spans math y (Unity z) and height (Unity y).
                corners[0] = new Vector3(fixedValue, zMin, freeMin);
                corners[1] = new Vector3(fixedValue, zMax, freeMin);
                corners[2] = new Vector3(fixedValue, zMax, freeMax);
                corners[3] = new Vector3(fixedValue, zMin, freeMax);
            }
            else
            {
                // Plane y = value: spans math x (Unity x) and height (Unity y).
                corners[0] = new Vector3(freeMin, zMin, fixedValue);
                corners[1] = new Vector3(freeMin, zMax, fixedValue);
                corners[2] = new Vector3(freeMax, zMax, fixedValue);
                corners[3] = new Vector3(freeMax, zMin, fixedValue);
            }
            planeMesh.vertices = corners;
            planeMesh.RecalculateBounds();
            // A MeshCollider only re-reads its mesh when the reference is reassigned.
            planeMeshCollider.sharedMesh = null;
            planeMeshCollider.sharedMesh = planeMesh;

            float wide = span * 0.07f;
            float tall = span * 0.035f;
            float thick = span * 0.025f;
            Vector3 centre = alongX
                ? new Vector3(fixedValue, zMax, middle)
                : new Vector3(middle, zMax, fixedValue);

            Vector3[] tab = new Vector3[4];
            if (alongX)
            {
                tab[0] = centre + new Vector3(0, -tall, -wide);
                tab[1] = centre + new Vector3(0, tall, -wide);
                tab[2] = centre + new Vector3(0, tall, wide);
                tab[3] = centre + new Vector3(0, -tall, wide);
            }
            else
            {
                tab[0] = centre + new Vector3(-wide, -tall, 0);
                tab[1] = centre + new Vector3(-wide, tall, 0);
                tab[2] = centre + new Vector3(wide, tall, 0);
                tab[3] = centre + new Vector3(wide, -tall, 0);
            }
            handleMesh.vertices = tab;
            handleMesh.RecalculateBounds();

            HandleCentre = centre;
            handleBoxCollider.center = centre;
            handleBoxCollider.size = alongX
                ? new Vector3(thick, tall * 2, wide * 2)
                : new Vector3(wide * 2, tall * 2, thick);
        }

        private LineRenderer Take(List<LineRenderer> pool, int index, string name)
        {
            while (pool.Count <= index)
            {
                GameObject item = new GameObject(name + pool.Count);
                item.transform.SetParent(root, false);
                LineRenderer created = item.AddComponent<LineRenderer>();
                created.sharedMaterial = CurveMaterial();
                created.useWorldSpace = false;
                pool.Add(created);
            }
            LineRenderer line = pool[index];
            line.gameObject.SetActive(true);
            return line;
        }

        private static void Hide(List<LineRenderer> pool, int from)
        {
            for (int i = from; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// The curve lies exactly on the surface, so sharing the surface's depth test would make it
        /// z-fight. A copy of the line material that ignores depth keeps it readable from any angle,
        /// and keeps the handle grabbable even when the surface is in front of it.
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
            if (planeMesh != null) Destroy(planeMesh);
            if (handleMesh != null) Destroy(handleMesh);
            if (curveMaterial != null) Destroy(curveMaterial);
        }
    }
}
