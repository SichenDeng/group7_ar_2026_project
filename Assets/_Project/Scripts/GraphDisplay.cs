using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace ARFunctionLab
{
    public class GraphDisplay : MonoBehaviour
    {
        public Material lineMaterial;
        public Bounds LocalBounds { get; private set; }
        private Transform graphics;
        private float span;
        private static readonly Color XColor = new Color(0.95f, 0.4f, 0.4f);
        private static readonly Color YColor = new Color(0.25f, 0.8f, 0.5f);
        private static readonly Color ZColor = new Color(0.4f, 0.65f, 1);
        private static readonly Color TickColor = new Color(0.64f, 0.7f, 0.8f);

        public static double TickStep(double length)
        {
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(length / 8)));
            double normalized = length / 8 / magnitude;
            return (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10) * magnitude;
        }

        public void Build(SurfaceSettings settings, Bounds surface)
        {
            if (graphics != null)
            {
                graphics.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(graphics.gameObject);
                else DestroyImmediate(graphics.gameObject);
            }
            graphics = new GameObject("Coordinate grid").transform;
            graphics.SetParent(transform, false);
            float xMin = (float)settings.xMin, xMax = (float)settings.xMax;
            float yMin = (float)settings.yMin, yMax = (float)settings.yMax;
            span = Mathf.Max(xMax - xMin, yMax - yMin);
            float zMin = Mathf.Min(0, surface.min.y) - span * 0.04f;
            float zMax = Mathf.Max(0, surface.max.y) + span * 0.08f;
            Color grid = new Color(0.35f, 0.45f, 0.6f, 0.35f);
            ForTicks(settings.xMin, settings.xMax, value =>
            {
                float x = (float)value;
                AddLine("Grid x", new Vector3(x, 0, yMin), new Vector3(x, 0, yMax), grid, span * 0.001f);
                if (IsLabelTick(value, xMax - xMin))
                    AddLabel(Number(value), new Vector3(x, 0, yMin - span * 0.055f), TickColor, false);
            });
            ForTicks(settings.yMin, settings.yMax, value =>
            {
                float y = (float)value;
                AddLine("Grid y", new Vector3(xMin, 0, y), new Vector3(xMax, 0, y), grid, span * 0.001f);
                if (IsLabelTick(value, yMax - yMin) && Math.Abs(value - yMin) > span * 1e-6)
                    AddLabel(Number(value), new Vector3(xMin - span * 0.055f, 0, y), TickColor, false);
            });
            // Draw true coordinate axes only when they intersect the plotting rectangle.
            if (yMin <= 0 && yMax >= 0)
                AddLine("X axis", new Vector3(xMin, 0, 0), new Vector3(xMax, 0, 0), XColor, span * 0.0025f);
            if (xMin <= 0 && xMax >= 0)
                AddLine("Y axis", new Vector3(0, 0, yMin), new Vector3(0, 0, yMax), YColor, span * 0.0025f);
            AddLabel("x", new Vector3(xMax + span * 0.05f, 0, Mathf.Clamp(0, yMin, yMax)), XColor, true);
            AddLabel("y", new Vector3(Mathf.Clamp(0, xMin, xMax), 0, yMax + span * 0.05f), YColor, true);
            if (xMin <= 0 && xMax >= 0 && yMin <= 0 && yMax >= 0)
            {
                AddLine("Z axis", new Vector3(0, zMin, 0), new Vector3(0, zMax, 0), ZColor, span * 0.0025f);
                AddLabel("z", new Vector3(0, zMax + span * 0.04f, 0), ZColor, true);
                ForTicks(zMin, zMax, value =>
                {
                    if (Math.Abs(value) < 1e-12) return;
                    AddLine("Z tick", new Vector3(-span * 0.009f, (float)value, 0), new Vector3(span * 0.009f, (float)value, 0), ZColor, span * 0.001f);
                    AddLabel(Number(value), new Vector3(span * 0.035f, (float)value, 0), TickColor, false);
                });
            }
            Bounds bounds = new Bounds();
            bounds.SetMinMax(new Vector3(xMin, zMin, yMin), new Vector3(xMax, zMax, yMax));
            bounds.Expand(span * 0.22f);
            LocalBounds = bounds;
        }

        private static string Number(double value)
        {
            if (Math.Abs(value) < 1e-12) value = 0;
            return value.ToString("G5", CultureInfo.InvariantCulture);
        }

        private bool IsLabelTick(double value, double axisLength)
        {
            double step = TickStep(Math.Max(axisLength, span * 0.5));
            return Math.Abs(value / step - Math.Round(value / step)) < 1e-5;
        }

        private static void ForTicks(double min, double max, Action<double> draw)
        {
            double step = TickStep(max - min);
            double start = Math.Ceiling(min / step) * step;
            for (int i = 0; i < 32; i++)
            {
                double value = start + i * step;
                if (value > max + step * 1e-6) break;
                draw(value);
            }
        }

        private void AddLine(string label, Vector3 a, Vector3 b, Color color, float width)
        {
            GameObject item = new GameObject(label);
            item.transform.SetParent(graphics, false);
            LineRenderer line = item.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
        }

        private void AddLabel(string text, Vector3 position, Color color, bool axis)
        {
            GameObject item = new GameObject("Coordinate " + text);
            item.transform.SetParent(graphics, false);
            item.transform.localPosition = position;
            TextMeshPro label = item.AddComponent<TextMeshPro>();
            label.font = UIFactory.Font;
            label.fontSize = span * (axis ? 0.48f : 0.28f);
            label.text = text;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta = new Vector2(span * 0.15f, span * 0.08f);
            item.AddComponent<FaceCamera>();
        }
    }
}
