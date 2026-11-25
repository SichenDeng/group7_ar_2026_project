using System.Collections.Generic;
using UnityEngine;

namespace ARFunctionLab
{
    public class GraphViewport : MonoBehaviour
    {
        private Camera view;
        private SurfacePlotter plotter;
        private GraphDisplay display;
        private CrossSectionDisplay crossSection;
        private Transform content;
        private bool quest;
        private int screenWidth, screenHeight;
        public RectTransform Sidebar;
        public RectTransform Controls;

        public void Initialize(Camera camera, SurfacePlotter target, GraphDisplay grid, CrossSectionDisplay cut, Transform graphContent, bool isQuest)
        {
            view = camera;
            plotter = target;
            display = grid;
            crossSection = cut;
            content = graphContent;
            quest = isQuest;
            plotter.SurfaceChanged += Refresh;
            plotter.CrossSectionChanged += RefreshCrossSection;
        }

        /// <summary>Rebuilds only the cutting plane, leaving the mesh and camera fit untouched.</summary>
        private void RefreshCrossSection()
        {
            if (crossSection == null || plotter.LastResult == null) return;
            Dictionary<string, double> parameters;
            string message;
            if (!plotter.settings.TryGetParameters(out parameters, out message)) return;
            crossSection.Build(
                plotter.CurrentFormula,
                plotter.settings,
                parameters,
                plotter.crossSection,
                plotter.LastResult.Mesh.bounds);
        }

        private void Refresh()
        {
            display.Build(plotter.settings, plotter.LastResult.Mesh.bounds);
            RefreshCrossSection();
            content.localPosition = -display.LocalBounds.center;
            if (quest)
            {
                Vector3 size = display.LocalBounds.size;
                transform.localScale = Vector3.one * (0.9f / Mathf.Max(size.x, size.y, size.z));
            }
            else Fit();
        }

        private void LateUpdate()
        {
            if (quest || Sidebar == null || (screenWidth == Screen.width && screenHeight == Screen.height)) return;
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            Sidebar.GetWorldCorners(corners);
            float left = Mathf.Clamp((corners[2].x + 16) / Screen.width, 0.3f, 0.7f);
            view.rect = new Rect(left, 0.07f, 1 - left, 0.9f);
            if (Controls != null)
            {
                Controls.anchorMin = Controls.anchorMax = new Vector2((1 + left) / 2, 0);
                Controls.anchoredPosition = new Vector2(0, 18);
            }
            Fit();
        }

        public static float FitDistance(Bounds bounds, Matrix4x4 localToWorld, Quaternion cameraRotation, Vector3 center, float verticalFov, float aspect)
        {
            Quaternion inverse = Quaternion.Inverse(cameraRotation);
            float vertical = Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f);
            float horizontal = vertical * Mathf.Max(aspect, 0.01f);
            float distance = 0.01f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = inverse * (localToWorld.MultiplyPoint3x4(corner) - center);
                distance = Mathf.Max(distance, Mathf.Abs(point.x) / horizontal - point.z, Mathf.Abs(point.y) / vertical - point.z);
            }
            return distance * 1.06f;
        }

        public void Fit()
        {
            if (quest || plotter.LastResult == null) return;
            Vector3 center = content.TransformPoint(display.LocalBounds.center);
            float distance = FitDistance(display.LocalBounds, content.localToWorldMatrix, view.transform.rotation, center, view.fieldOfView, view.aspect);
            view.transform.position = center - view.transform.forward * distance;
            view.nearClipPlane = Mathf.Max(0.0001f, distance * 0.001f);
            view.farClipPlane = Mathf.Max(10, distance * 4);
        }

        public void Rotate(float degrees)
        {
            transform.Rotate(Vector3.up, degrees, Space.World);
            Fit();
        }

        public void ResetView()
        {
            transform.rotation = Quaternion.identity;
            Fit();
        }

        private void OnDestroy()
        {
            if (plotter == null) return;
            plotter.SurfaceChanged -= Refresh;
            plotter.CrossSectionChanged -= RefreshCrossSection;
        }
    }
}
