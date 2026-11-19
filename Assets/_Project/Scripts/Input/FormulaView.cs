using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARFunctionLab
{
    public class FormulaView : MonoBehaviour, IPointerClickHandler
    {
        private class LayoutBox
        {
            public MathRow Row;
            public MathNode Node;
            public float Width;
            public float Above;
            public float Below;
            public float Size;
            public List<LayoutBox> Children = new List<LayoutBox>();
        }

        private class RowHit
        {
            public int Id;
            public int Depth;
            public Rect Bounds;
            public float Baseline;
            public List<float> Stops = new List<float>();
        }

        private FormulaEditor editor;
        private RectTransform content;
        private RectTransform caret;
        private RectTransform selection;
        private TextMeshProUGUI measure;
        private readonly List<RowHit> rows = new List<RowHit>();
        private float offset;
        private float contentWidth;

        public void Initialize(FormulaEditor model)
        {
            editor = model;
            Image background = gameObject.GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.color = Color.white;
            UIFactory.Round(background);
            gameObject.AddComponent<RectMask2D>();
            content = UIFactory.Rect("Math content", transform, 0, 0, 600, 180);
            content.anchorMin = new Vector2(0, 0.5f);
            content.anchorMax = new Vector2(0, 0.5f);
            content.pivot = new Vector2(0, 0.5f);
            content.anchoredPosition = Vector2.zero;
            measure = UIFactory.Label("Measure", transform, 0, 0, 1, 1, "", 36, Color.clear);
            measure.gameObject.SetActive(false);
            editor.ContentChanged += Rebuild;
            editor.CursorChanged += RefreshCursor;
            Rebuild();
        }

        private float TextWidth(string text, float size)
        {
            measure.fontSize = size;
            return measure.GetPreferredValues(text).x + 2;
        }

        private LayoutBox MeasureRow(MathRow row, float size)
        {
            LayoutBox box = new LayoutBox { Row = row, Size = size, Above = size * 0.8f, Below = size * 0.23f };
            for (int i = 0; i < row.Nodes.Count; i++)
            {
                LayoutBox child = MeasureNode(row.Nodes[i], size);
                box.Children.Add(child);
                box.Width += child.Width + 2;
                box.Above = Mathf.Max(box.Above, child.Above);
                box.Below = Mathf.Max(box.Below, child.Below);
            }
            if (row.Nodes.Count == 0) box.Width = size * 0.7f;
            return box;
        }

        private LayoutBox MeasureNode(MathNode node, float size)
        {
            LayoutBox box = new LayoutBox { Node = node, Size = size, Above = size * 0.8f, Below = size * 0.23f };
            if (node.Kind == MathNodeKind.Symbol)
            {
                box.Width = TextWidth(DisplaySymbol(node.Text), size);
                return box;
            }
            for (int i = 0; i < node.Slots.Count; i++)
            {
                float childSize = size;
                if (node.Kind == MathNodeKind.Power && i == 1) childSize *= 0.7f;
                if (node.Kind == MathNodeKind.Fraction) childSize *= 0.85f;
                if (node.Kind == MathNodeKind.Root && node.Slots.Count == 2 && i == 0) childSize *= 0.6f;
                box.Children.Add(MeasureRow(node.Slots[i], childSize));
            }
            LayoutBox a = box.Children[0];
            if (node.Kind == MathNodeKind.Power)
            {
                LayoutBox b = box.Children[1];
                box.Width = a.Width + b.Width + 3;
                box.Above = Mathf.Max(a.Above, size * 0.55f + b.Above);
                box.Below = a.Below;
            }
            else if (node.Kind == MathNodeKind.Fraction)
            {
                LayoutBox b = box.Children[1];
                box.Width = Mathf.Max(a.Width, b.Width) + 14;
                box.Above = size * 0.23f + 7 + a.Above + a.Below;
                box.Below = b.Above + b.Below + 7 - size * 0.23f;
            }
            else if (node.Kind == MathNodeKind.Root)
            {
                LayoutBox inner = box.Children[box.Children.Count - 1];
                box.Width = inner.Width + size * 0.65f + 10;
                box.Above = inner.Above + 7;
                box.Below = inner.Below;
                if (box.Children.Count == 2) { box.Width += a.Width; box.Above = Mathf.Max(box.Above, size * 0.5f + a.Above); }
            }
            else
            {
                box.Width = size * 0.55f;
                if (node.Kind == MathNodeKind.Function) box.Width += TextWidth(DisplayFunction(node.Text), size);
                for (int i = 0; i < box.Children.Count; i++)
                {
                    LayoutBox child = box.Children[i];
                    box.Width += child.Width;
                    if (i > 0) box.Width += TextWidth(",", size);
                    box.Above = Mathf.Max(box.Above, child.Above);
                    box.Below = Mathf.Max(box.Below, child.Below);
                }
            }
            return box;
        }

        private string DisplaySymbol(string text)
        {
            if (text == "*") return "×";
            if (text == "-") return "−";
            return text;
        }

        private string DisplayFunction(string text)
        {
            if (text == "asin") return "sin<sup>-1</sup>";
            if (text == "acos") return "cos<sup>-1</sup>";
            if (text == "atan") return "tan<sup>-1</sup>";
            return text;
        }

        private RectTransform DrawRect(string name, float x, float y, float width, float height, Color color)
        {
            Image image = UIFactory.Panel(name, content, 0, 0, width, height, color);
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private void Line(float x1, float y1, float x2, float y2)
        {
            Vector2 delta = new Vector2(x2 - x1, y2 - y1);
            RectTransform line = DrawRect("Math line", x1, y1, delta.magnitude, 2, UIFactory.Ink);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void Glyph(string text, float x, float baseline, float width, float size)
        {
            TextMeshProUGUI label = UIFactory.Label("Math " + text, content, 0, 0, width + 4, size * 2, text, size, UIFactory.Ink);
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.BaselineLeft;
            label.rectTransform.anchorMin = new Vector2(0, 0.5f);
            label.rectTransform.anchorMax = new Vector2(0, 0.5f);
            label.rectTransform.pivot = new Vector2(0, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(x, baseline);
        }

        private void ArrangeRow(LayoutBox box, float x, float y, int depth)
        {
            RowHit hit = new RowHit { Id = box.Row.Id, Depth = depth, Bounds = new Rect(x - 3, y - box.Below - 5, box.Width + 6, box.Above + box.Below + 10), Baseline = y };
            rows.Add(hit);
            hit.Stops.Add(x);
            if (box.Children.Count == 0)
            {
                DrawRect("Empty slot", x, y - box.Below, box.Width, box.Above + box.Below, new Color(0.78f, 0.88f, 0.97f, 0.7f));
            }
            for (int i = 0; i < box.Children.Count; i++)
            {
                LayoutBox child = box.Children[i];
                ArrangeNode(child, x, y, depth + 1);
                x += child.Width + 2;
                hit.Stops.Add(x);
            }
        }

        private void ArrangeNode(LayoutBox box, float x, float y, int depth)
        {
            MathNode node = box.Node;
            float size = box.Size;
            if (node.Kind == MathNodeKind.Symbol) { Glyph(DisplaySymbol(node.Text), x, y, box.Width, size); return; }
            LayoutBox a = box.Children[0];
            if (node.Kind == MathNodeKind.Power)
            {
                ArrangeRow(a, x, y, depth);
                ArrangeRow(box.Children[1], x + a.Width + 3, y + size * 0.55f, depth);
            }
            else if (node.Kind == MathNodeKind.Fraction)
            {
                LayoutBox b = box.Children[1];
                float bar = y + size * 0.23f;
                ArrangeRow(a, x + (box.Width - a.Width) / 2, bar + 7 + a.Below, depth);
                ArrangeRow(b, x + (box.Width - b.Width) / 2, bar - 7 - b.Above, depth);
                Line(x + 2, bar, x + box.Width - 2, bar);
            }
            else if (node.Kind == MathNodeKind.Root)
            {
                float start = x;
                if (box.Children.Count == 2) { ArrangeRow(a, x, y + size * 0.5f, depth); start += a.Width; }
                LayoutBox inner = box.Children[box.Children.Count - 1];
                float innerX = start + size * 0.65f;
                ArrangeRow(inner, innerX, y, depth);
                Line(start, y + size * 0.2f, start + size * 0.18f, y + size * 0.28f);
                Line(start + size * 0.18f, y + size * 0.28f, start + size * 0.35f, y - inner.Below);
                Line(start + size * 0.35f, y - inner.Below, innerX - 2, y + inner.Above + 5);
                Line(innerX - 2, y + inner.Above + 5, innerX + inner.Width + 4, y + inner.Above + 5);
            }
            else
            {
                if (node.Kind == MathNodeKind.Function)
                {
                    string name = DisplayFunction(node.Text);
                    float width = TextWidth(name, size);
                    Glyph(name, x, y, width, size);
                    x += width;
                }
                bool absolute = node.Kind == MathNodeKind.Absolute;
                if (absolute) Line(x + 3, y - box.Below, x + 3, y + box.Above);
                else Glyph("(", x, y, size * 0.3f, size);
                x += size * 0.28f;
                for (int i = 0; i < box.Children.Count; i++)
                {
                    if (i > 0) { Glyph(",", x, y, size * 0.3f, size); x += TextWidth(",", size); }
                    ArrangeRow(box.Children[i], x, y, depth);
                    x += box.Children[i].Width;
                }
                if (absolute) Line(x + 3, y - box.Below, x + 3, y + box.Above);
                else Glyph(")", x, y, size * 0.3f, size);
            }
        }

        private void Rebuild()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                content.GetChild(i).gameObject.SetActive(false);
                Destroy(content.GetChild(i).gameObject);
            }
            rows.Clear();
            LayoutBox layout = MeasureRow(editor.Document.Root, 38);
            float available = ((RectTransform)transform).rect.height - 30;
            float scale = Mathf.Min(1, available / (layout.Above + layout.Below));
            content.localScale = new Vector3(scale, scale, 1);
            contentWidth = layout.Width + 40;
            content.sizeDelta = new Vector2(contentWidth, 180);
            ArrangeRow(layout, 18, (layout.Below - layout.Above) / 2, 0);
            selection = DrawRect("Selection", 0, 0, 1, 1, new Color(UIFactory.Accent.r, UIFactory.Accent.g, UIFactory.Accent.b, 0.22f));
            caret = DrawRect("Caret", 0, 0, 2, 40, UIFactory.Accent);
            RefreshCursor();
        }

        private void RefreshCursor()
        {
            if (caret == null) return;
            for (int i = 0; i < rows.Count; i++)
            {
                RowHit row = rows[i];
                if (row.Id != editor.RowId) continue;
                float x = row.Stops[Mathf.Clamp(editor.Index, 0, row.Stops.Count - 1)];
                caret.anchoredPosition = new Vector2(x, row.Bounds.yMin + 3);
                caret.sizeDelta = new Vector2(2, row.Bounds.height - 6);
                bool selected = editor.SelectionStart >= 0 && editor.SelectionStart != editor.SelectionEnd;
                selection.gameObject.SetActive(selected);
                if (selected)
                {
                    float a = row.Stops[editor.SelectionStart];
                    float b = row.Stops[editor.SelectionEnd];
                    selection.anchoredPosition = new Vector2(Mathf.Min(a, b), row.Bounds.yMin);
                    selection.sizeDelta = new Vector2(Mathf.Abs(b - a), row.Bounds.height);
                }
                float scale = content.localScale.x;
                float viewWidth = ((RectTransform)transform).rect.width;
                if (x * scale + offset > viewWidth - 24) offset = viewWidth - 24 - x * scale;
                if (x * scale + offset < 18) offset = 18 - x * scale;
                offset = Mathf.Clamp(offset, Mathf.Min(0, viewWidth - contentWidth * scale), 0);
                content.anchoredPosition = new Vector2(offset, 0);
                return;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out point)) return;
            RowHit best = null;
            for (int i = 0; i < rows.Count; i++)
            {
                RowHit row = rows[i];
                if (row.Bounds.Contains(point) && (best == null || row.Depth > best.Depth)) best = row;
            }
            if (best == null) best = rows[0];
            int index = 0;
            float distance = float.MaxValue;
            for (int i = 0; i < best.Stops.Count; i++)
            {
                float next = Mathf.Abs(point.x - best.Stops[i]);
                if (next < distance) { distance = next; index = i; }
            }
            editor.SetCursor(best.Id, index);
        }

        private void OnDestroy()
        {
            if (editor == null) return;
            editor.ContentChanged -= Rebuild;
            editor.CursorChanged -= RefreshCursor;
        }
    }
}
