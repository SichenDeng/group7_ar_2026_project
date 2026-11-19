using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARFunctionLab
{
    public class FormulaInputUI : MonoBehaviour
    {
        public SurfacePlotter plotter;
        public Transform graphRoot;
        public FormulaEditor Editor { get; private set; }
        public MathKeyboard Keyboard { get; private set; }
        private TextMeshProUGUI status;
        private TextMeshProUGUI rangeLabel;
        private TextMeshProUGUI resolutionLabel;
        private GameObject menu;
        private bool built;
        private bool draftChanged;

        public void Build()
        {
            if (built) return;
            built = true;
            Editor = new FormulaEditor();
            Image background = UIFactory.Panel("Panel", transform, 0, 0, 704, 844, UIFactory.Paper);
            UIFactory.Round(background);
            UIFactory.Label("App name", transform, 28, 24, 550, 32, "AR Function Lab", 22, UIFactory.Muted);
            UIFactory.Button("More", transform, 624, 18, 52, 44, "···", ToggleMenu);
            UIFactory.Label("Prefix", transform, 28, 88, 55, 148, "z =", 32, UIFactory.Ink);
            RectTransform formulaRect = UIFactory.Rect("Formula", transform, 87, 86, 589, 150);
            formulaRect.gameObject.AddComponent<FormulaView>().Initialize(Editor);
            status = UIFactory.Label("Status", transform, 28, 252, 648, 48, "", 18, UIFactory.Muted);
            status.textWrappingMode = TextWrappingModes.Normal;
            status.alignment = TextAlignmentOptions.TopLeft;
            Image rangePanel = UIFactory.Panel("Range controls", transform, 28, 310, 648, 52, Color.white);
            UIFactory.Round(rangePanel);
            rangeLabel = UIFactory.Label("Range value", rangePanel.transform, 12, 0, 376, 52, "", 24, UIFactory.Ink);
            rangeLabel.alignment = TextAlignmentOptions.Center;
            Button expand = UIFactory.Button("Expand range", rangePanel.transform, 400, 0, 116, 52, "× 2", delegate { ChangeRange(true); });
            Button shrink = UIFactory.Button("Shrink range", rangePanel.transform, 528, 0, 120, 52, "÷ 2", delegate { ChangeRange(false); });
            expand.GetComponent<Image>().color = UIFactory.SelectedTab;
            shrink.GetComponent<Image>().color = UIFactory.SelectedTab;
            RectTransform keyboardRect = UIFactory.Rect("Keyboard", transform, 28, 430, 648, 318);
            Keyboard = keyboardRect.gameObject.AddComponent<MathKeyboard>();
            Keyboard.Initialize(Editor, Generate);
            BuildMenu();
            Editor.ContentChanged += OnDraftChanged;
            Editor.LoadPreset(0);
            Generate();
        }

        private void BuildMenu()
        {
            Image panel = UIFactory.Panel("More menu", transform, 22, 72, 660, 228, UIFactory.MenuBackground);
            UIFactory.Round(panel);
            menu = panel.gameObject;
            string[] actions = { "Undo", "Clear", "Select term", "Next slot" };
            UnityEngine.Events.UnityAction[] commands = {
                delegate { Editor.Undo(); }, delegate { Editor.Clear(); },
                delegate { Editor.SelectPrevious(); }, delegate { Editor.ExitSlot(); }
            };
            for (int i = 0; i < actions.Length; i++)
            {
                int index = i;
                UIFactory.Button(actions[i], menu.transform, 12 + i * 162, 12, 150, 46, actions[i], delegate { commands[index](); menu.SetActive(false); });
            }
            string[] presets = { "Waves", "Saddle", "Dome", "1 / x" };
            for (int i = 0; i < presets.Length; i++)
            {
                int preset = i;
                UIFactory.Button("Preset " + presets[i], menu.transform, 12 + i * 162, 72, 150, 46, presets[i], delegate { Editor.LoadPreset(preset); Generate(); menu.SetActive(false); });
            }
            resolutionLabel = UIFactory.Label("Resolution", menu.transform, 16, 142, 250, 50, "", 20, UIFactory.Muted);
            int[] levels = { 64, 128, 256 };
            for (int i = 0; i < levels.Length; i++)
            {
                int cells = levels[i];
                UIFactory.Button("Resolution " + cells, menu.transform, 294 + i * 118, 142, 108, 46, cells.ToString(), delegate { SetResolution(cells); menu.SetActive(false); });
            }
            menu.SetActive(false);
        }

        private void ToggleMenu()
        {
            menu.SetActive(!menu.activeSelf);
        }

        public void ChangeRange(bool expand)
        {
            menu.SetActive(false);
            string message;
            bool success = expand ? plotter.TryExpandDomain(out message) : plotter.TryShrinkDomain(out message);
            if (!success) { ShowError(message); return; }
            RefreshPlotState();
        }

        private void OnDraftChanged()
        {
            draftChanged = true;
            status.text = "Edited";
            status.color = UIFactory.Muted;
        }

        private static string Number(double value) { return value.ToString("G6", CultureInfo.InvariantCulture); }

        private void RefreshRangeLabel()
        {
            SurfaceSettings s = plotter.settings;
            rangeLabel.text = s.xMin == s.yMin && s.xMax == s.yMax
                ? "[" + Number(s.xMin) + ", " + Number(s.xMax) + "]"
                : "x: [" + Number(s.xMin) + ", " + Number(s.xMax) + "]   y: [" + Number(s.yMin) + ", " + Number(s.yMax) + "]";
        }

        private void RefreshPlotState()
        {
            RefreshRangeLabel();
            resolutionLabel.text = "Mesh: " + plotter.settings.cells + " × " + plotter.settings.cells;
            status.color = UIFactory.Muted;
            status.text = draftChanged ? "Edited" : "";
            SurfaceResult result = plotter.LastResult;
            if (result != null && result.ExcludedSamples > 0)
                status.text = "Points outside the height limit were omitted.";
            else if (result != null && result.UndefinedSamples > 0)
                status.text = "Undefined points were omitted.";
            if (draftChanged && status.text != "Edited") status.text = "Edited. " + status.text;
        }

        public void Generate()
        {
            string source;
            int empty;
            if (!Editor.Document.TryExport(out source, out empty))
            {
                Editor.SetCursor(empty, 0);
                ShowError("Complete the highlighted slot.");
                return;
            }
            string message;
            if (!plotter.TryPlot(source, out message)) { ShowError(message); return; }
            draftChanged = false;
            RefreshPlotState();
        }

        private void ShowError(string message)
        {
            status.text = message;
            status.color = new Color(0.7f, 0.18f, 0.22f);
        }

        public void SetResolution(int cells)
        {
            string message;
            if (!plotter.TrySetResolution(cells, out message)) { ShowError(message); return; }
            RefreshPlotState();
        }

        private void OnDestroy()
        {
            if (Editor != null) Editor.ContentChanged -= OnDraftChanged;
        }
    }
}
