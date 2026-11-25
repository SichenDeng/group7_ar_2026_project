using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARFunctionLab
{
    public class FormulaInputUI : MonoBehaviour
    {
        public SurfacePlotter plotter;
        public CrossSectionDisplay crossSection;
        public Transform graphRoot;
        public FormulaEditor Editor { get; private set; }
        public MathKeyboard Keyboard { get; private set; }
        private TextMeshProUGUI status;
        private TextMeshProUGUI rangeLabel;
        private TextMeshProUGUI resolutionLabel;
        private Button cutToggle;
        private Button cutValueButton;
        private TextMeshProUGUI cutAxisLabel;
        private TextMeshProUGUI cutLabel;
        private bool cutEditing;
        private string cutDraft = "";
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
            Image cutPanel = UIFactory.Panel("Cross section controls", transform, 28, 370, 648, 52, Color.white);
            UIFactory.Round(cutPanel);
            cutToggle = UIFactory.Button("Toggle cut", cutPanel.transform, 0, 0, 150, 52, "Cut", ToggleCut);
            Button axisButton = UIFactory.Button("Cut axis", cutPanel.transform, 158, 0, 90, 52, "", SwapCutAxis);
            cutAxisLabel = axisButton.GetComponentInChildren<TextMeshProUGUI>();
            Button back = UIFactory.Button("Cut back", cutPanel.transform, 256, 0, 96, 52, "", delegate { StepCut(-1); });
            MathKeyboard.DrawIcon(back, "left");
            cutValueButton = UIFactory.Button("Cut value", cutPanel.transform, 360, 0, 184, 52, "", BeginOrCommitCutEdit);
            cutLabel = cutValueButton.GetComponentInChildren<TextMeshProUGUI>();
            cutLabel.fontSize = 22;
            Button forward = UIFactory.Button("Cut forward", cutPanel.transform, 552, 0, 96, 52, "", delegate { StepCut(1); });
            MathKeyboard.DrawIcon(forward, "right");

            RectTransform keyboardRect = UIFactory.Rect("Keyboard", transform, 28, 430, 648, 318);
            Keyboard = keyboardRect.gameObject.AddComponent<MathKeyboard>();
            Keyboard.Initialize(Editor, Generate);
            Keyboard.Intercept = HandleCutKey;
            BuildMenu();
            Editor.ContentChanged += OnDraftChanged;
            // Dragging the plane changes the value without going through this panel.
            plotter.CrossSectionChanged += OnCrossSectionChanged;
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

        private void ToggleCut()
        {
            menu.SetActive(false);
            cutEditing = false;
            plotter.ToggleCrossSection();
            RefreshCutRow();
            RefreshStatus();
        }

        private void SwapCutAxis()
        {
            menu.SetActive(false);
            cutEditing = false;
            plotter.SetCrossSectionAxis(
                plotter.crossSection.axis == CrossSectionAxis.X ? CrossSectionAxis.Y : CrossSectionAxis.X);
            RefreshCutRow();
            RefreshStatus();
        }

        /// <summary>Stepping the plane also switches the cut on, so one press is enough to see it.</summary>
        private void StepCut(int direction)
        {
            menu.SetActive(false);
            cutEditing = false;
            if (!plotter.crossSection.enabled) plotter.ToggleCrossSection();
            plotter.StepCrossSection(direction);
            RefreshCutRow();
            RefreshStatus();
        }

        /// <summary>Tapping the value borrows the number keys so any position can be typed.</summary>
        private void BeginOrCommitCutEdit()
        {
            menu.SetActive(false);
            if (cutEditing) { CommitCutEdit(); return; }
            if (!plotter.crossSection.enabled) plotter.ToggleCrossSection();
            cutEditing = true;
            cutDraft = "";
            Keyboard.ShowTab(0);
            status.color = UIFactory.Muted;
            status.text = "Type a position for the cut, then press the enter key.";
            RefreshCutRow();
        }

        private void CommitCutEdit()
        {
            cutEditing = false;
            double value;
            if (double.TryParse(cutDraft, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && FunctionCatalog.IsFinite(value))
            {
                plotter.SetCrossSectionValue(value);
                double min;
                double max;
                CrossSectionSettings.GetFixedRange(plotter.settings, plotter.crossSection.axis, out min, out max);
                if (value < min || value > max)
                {
                    RefreshCutRow();
                    ShowError("That position is outside the domain, so the cut moved to the edge.");
                    return;
                }
            }
            RefreshCutRow();
            RefreshStatus();
        }

        /// <summary>While a cut position is being typed, the number keys feed it, not the formula.</summary>
        private bool HandleCutKey(string command)
        {
            if (!cutEditing) return false;
            if (command == "plot") { CommitCutEdit(); return true; }
            if (command == "delete")
            {
                if (cutDraft.Length > 0) cutDraft = cutDraft.Substring(0, cutDraft.Length - 1);
                RefreshCutRow();
                return true;
            }
            if (command == "-")
            {
                cutDraft = cutDraft.StartsWith("-") ? cutDraft.Substring(1) : "-" + cutDraft;
                RefreshCutRow();
                return true;
            }
            if (command.Length == 1 && (char.IsDigit(command[0]) || command == "."))
            {
                if (cutDraft.Length < 12) cutDraft += command;
                RefreshCutRow();
                return true;
            }
            // Any other key means the user moved on; let the formula editor have it.
            cutEditing = false;
            RefreshCutRow();
            return false;
        }

        private void RefreshCutRow()
        {
            CrossSectionSettings cut = plotter.crossSection;
            cutToggle.GetComponent<Image>().color = cut.enabled ? UIFactory.SelectedTab : Color.white;
            cutAxisLabel.text = cut.axis == CrossSectionAxis.X ? "<i>x</i>" : "<i>y</i>";
            cutValueButton.GetComponent<Image>().color = cutEditing ? UIFactory.SelectedTab : Color.white;
            string prefix = cut.axis == CrossSectionAxis.X ? "x = " : "y = ";
            if (cutEditing)
            {
                cutLabel.text = prefix + cutDraft + "|";
                cutLabel.color = UIFactory.Accent;
                return;
            }
            cutLabel.text = cut.enabled ? prefix + Number(cut.value) : "off";
            cutLabel.color = cut.enabled ? UIFactory.Ink : UIFactory.Muted;
        }

        /// <summary>
        /// Rebuilds the whole status line from the current surface and cut. It always overwrites,
        /// so a problem that has since been resolved cannot linger from an earlier refresh.
        /// </summary>
        private void RefreshStatus()
        {
            if (cutEditing) return;
            status.color = UIFactory.Muted;
            status.text = draftChanged ? "Edited" : "";

            SurfaceResult result = plotter.LastResult;
            if (result != null && result.ExcludedSamples > 0)
                status.text = "Points outside the height limit were omitted.";
            else if (result != null && result.UndefinedSamples > 0)
                status.text = "Undefined points were omitted.";

            CrossSectionCurve curve = crossSection != null ? crossSection.LastCurve : null;
            if (curve != null && curve.IsEmpty && (curve.HasGaps || curve.OutsideDomain))
            {
                // Nothing is drawable here. Say so quietly: an empty cut is a fact about the
                // function, not a mistake the user made.
                status.text = curve.OutsideDomain
                    ? "No cut here: the plane is outside the plotted domain."
                    : "No cut here: the surface is undefined along this line.";
            }
            else if (curve != null && curve.HasGaps)
            {
                status.color = CrossSectionDisplay.BreakColor;
                status.text = "The cut breaks where the surface is undefined, marked in red.";
            }

            if (draftChanged && status.text != "Edited") status.text = "Edited. " + status.text;
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
            RefreshCutRow();
            resolutionLabel.text = "Mesh: " + plotter.settings.cells + " × " + plotter.settings.cells;
            RefreshStatus();
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

        private void OnCrossSectionChanged()
        {
            RefreshCutRow();
            RefreshStatus();
        }

        private void OnDestroy()
        {
            if (Editor != null) Editor.ContentChanged -= OnDraftChanged;
            if (plotter != null) plotter.CrossSectionChanged -= OnCrossSectionChanged;
        }
    }
}
