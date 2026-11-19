using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ARFunctionLab
{
    public static class UIFactory
    {
        public static readonly Color Ink = new Color(0.08f, 0.16f, 0.25f);
        public static readonly Color Accent = new Color(0.07f, 0.40f, 0.71f);
        public static readonly Color Muted = new Color(0.35f, 0.43f, 0.52f);
        public static readonly Color Paper = new Color(0.941f, 0.965f, 0.988f);
        public static readonly Color ActionKey = new Color(0.86f, 0.91f, 0.95f);
        public static readonly Color SelectedTab = new Color(0.74f, 0.87f, 0.97f);
        public static readonly Color MenuBackground = new Color(0.87f, 0.92f, 0.97f);
        public static TMP_FontAsset Font;
        private static Sprite rounded;

        // A sliced sprite keeps corners round without stretching them on wide keys.
        private static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 32;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(Mathf.Abs(x - 15.5f) - 6, 0);
                        float dy = Mathf.Max(Mathf.Abs(y - 15.5f) - 6, 0);
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(10 - Mathf.Sqrt(dx * dx + dy * dy))));
                    }
                texture.Apply();
                rounded = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 12);
                rounded.hideFlags = HideFlags.HideAndDontSave;
                return rounded;
            }
        }

        public static void Round(Image image)
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
        }

        public static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static Image Panel(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, float x, float y, float width, float height, string text, float size, Color color)
        {
            TextMeshProUGUI label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font != null ? Font : TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.text = text;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(string name, Transform parent, float x, float y, float width, float height, string text, UnityAction action, bool primary = false)
        {
            Image image = Panel(name, parent, x, y, width, height, primary ? Accent : Color.white);
            Round(image);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Navigation navigation = new Navigation();
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.87f, 0.94f, 1);
            colors.pressedColor = new Color(0.67f, 0.82f, 0.95f);
            button.colors = colors;
            TextMeshProUGUI label = Label("Label", image.transform, 3, 0, width - 6, height, text, 23, primary ? Color.white : Ink);
            label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(action);
            return button;
        }

    }
}
