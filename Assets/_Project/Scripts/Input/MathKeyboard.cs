using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARFunctionLab
{
    public class MathKeyboard : MonoBehaviour
    {
        private FormulaEditor editor;
        private Action generate;
        private RectTransform keys;
        private readonly Button[] tabs = new Button[3];
        /// <summary>
        /// Optional hook that can consume a key before the formula editor sees it, so another
        /// field on the panel can borrow the keyboard. Return true when the key was handled.
        /// </summary>
        public Func<string, bool> Intercept;

        public void Initialize(FormulaEditor model, Action submit)
        {
            editor = model;
            generate = submit;
            string[] names = { "123", "f(x)", "ABC" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                tabs[i] = UIFactory.Button(names[i], transform, i * 104, 0, 92, 44, names[i], delegate { ShowTab(index); });
                tabs[i].GetComponentInChildren<TextMeshProUGUI>().fontSize = 26;
            }
            keys = UIFactory.Rect("Keys", transform, 0, 62, 648, 256);
            ShowTab(0);
        }

        private void Key(string label, string command, float column, int row, int columns, float span = 1)
        {
            float unit = (648 + 7) / columns;
            Button button = UIFactory.Button("Key " + command, keys, column * unit, row * 64, unit * span - 7, 57, label, delegate { Press(command); }, command == "plot");
            button.GetComponentInChildren<TextMeshProUGUI>().fontSize = columns == 10 ? 26 : 27;
            if (command == "left" || command == "right" || command == "delete" || command == "plot")
            {
                if (command != "plot") button.GetComponent<Image>().color = UIFactory.ActionKey;
                DrawIcon(button, command);
            }
        }

        public static void DrawIcon(Button button, string command)
        {
            button.GetComponentInChildren<TextMeshProUGUI>().text = "";
            RectTransform rect = button.GetComponent<RectTransform>();
            float cx = rect.sizeDelta.x / 2, cy = rect.sizeDelta.y / 2;
            Color color = command == "plot" ? Color.white : UIFactory.Ink;
            Action<float, float, float, float> line = (x1, y1, x2, y2) =>
            {
                Vector2 delta = new Vector2(x2 - x1, y2 - y1);
                Image image = UIFactory.Panel("Icon", rect, cx + x1, cy + y1, delta.magnitude, 2.5f, color);
                image.raycastTarget = false;
                image.rectTransform.pivot = new Vector2(0, 0.5f);
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            };
            if (command == "delete")
            {
                line(-16, 0, -7, -10); line(-7, -10, 15, -10); line(15, -10, 15, 10);
                line(15, 10, -7, 10); line(-7, 10, -16, 0);
                line(-3, -5, 7, 5); line(-3, 5, 7, -5);
            }
            else if (command == "plot")
            {
                line(12, -10, 12, 5); line(12, 5, -12, 5);
                line(-12, 5, -5, -2); line(-12, 5, -5, 12);
            }
            else
            {
                float direction = command == "left" ? -1 : 1;
                line(-4 * direction, -9, 5 * direction, 0);
                line(5 * direction, 0, -4 * direction, 9);
            }
        }

        public void ShowTab(int index)
        {
            for (int i = keys.childCount - 1; i >= 0; i--)
            {
                keys.GetChild(i).gameObject.SetActive(false);
                Destroy(keys.GetChild(i).gameObject);
            }
            for (int i = 0; i < tabs.Length; i++)
                tabs[i].GetComponent<Image>().color = i == index ? UIFactory.SelectedTab : UIFactory.Paper;
            if (index == 0)
            {
                string[] labels = { "<i>x</i>", "<i>y</i>", "π", "e", "7", "8", "9", "×", "□²", "□<sup>□</sup>", "√□", "|□|", "4", "5", "6", "−", "a/b", "(", ")", ",", "1", "2", "3", "+" };
                string[] commands = { "x", "y", "π", "e", "7", "8", "9", "*", "square", "power", "sqrt", "abs", "4", "5", "6", "-", "fraction", "group", "exit", ",", "1", "2", "3", "+" };
                for (int i = 0; i < labels.Length; i++) Key(labels[i], commands[i], i % 8, i / 8, 8);
                string[] footer = { "0", ".", "/", "left", "right", "delete", "plot" };
                Key("0", "0", 0, 3, 8, 2);
                for (int i = 1; i < footer.Length; i++) Key(footer[i] == "/" ? "÷" : footer[i], footer[i], i + 1, 3, 8);
            }
            else if (index == 1)
            {
                string[] labels = { "sin", "cos", "tan", "√□", "sin<sup>−1</sup>", "cos<sup>−1</sup>", "tan<sup>−1</sup>", "|□|", "ln", "log<sub>10</sub>", "log<sub>□</sub>", "<sup>□</sup>√□" };
                string[] commands = { "sin", "cos", "tan", "sqrt", "asin", "acos", "atan", "abs", "ln", "log10", "log", "root" };
                for (int i = 0; i < labels.Length; i++) Key(labels[i], commands[i], i % 4, i / 4, 4);
                Key("e<sup>□</sup>", "exp", 0, 3, 8, 2);
                Key("10<sup>□</sup>", "tenpower", 2, 3, 8, 2);
                Key("", "left", 4, 3, 8); Key("", "right", 5, 3, 8);
                Key("", "delete", 6, 3, 8); Key("", "plot", 7, 3, 8);
            }
            else
            {
                string[] rows = { "qwertyuiop", "asdfghjkl", "zxcvbnm" };
                for (int r = 0; r < rows.Length; r++)
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        string letter = rows[r][c].ToString();
                        Key(letter, letter, c + (10 - rows[r].Length) / 2f, r, 10);
                    }
                Key("(", "group", 0, 3, 10); Key(")", "exit", 1, 3, 10); Key(",", ",", 2, 3, 10);
                Key("", " ", 3, 3, 10, 3);
                Key("", "left", 6, 3, 10); Key("", "right", 7, 3, 10);
                Key("", "delete", 8, 3, 10); Key("", "plot", 9, 3, 10);
            }
        }

        public void Press(string command)
        {
            if (Intercept != null && Intercept(command)) return;
            switch (command)
            {
                case "left": editor.Move(-1); return;
                case "right": editor.Move(1); return;
                case "exit": editor.ExitSlot(); return;
                case "delete": editor.Backspace(); return;
                case "undo": editor.Undo(); return;
                case "clear": editor.Clear(); return;
                case "plot": generate(); return;
                case "square": editor.InsertTemplate(MathNodeKind.Power, "", true); return;
                case "power": editor.InsertTemplate(MathNodeKind.Power); return;
                case "fraction": editor.InsertTemplate(MathNodeKind.Fraction); return;
                case "sqrt": editor.InsertTemplate(MathNodeKind.Root); return;
                case "root": editor.InsertTemplate(MathNodeKind.Root, "", false, true); return;
                case "abs": editor.InsertTemplate(MathNodeKind.Absolute); return;
                case "group": editor.InsertTemplate(MathNodeKind.Group); return;
                case "exp": editor.InsertText("e"); editor.InsertTemplate(MathNodeKind.Power); return;
                case "tenpower": editor.InsertText("10"); editor.InsertTemplate(MathNodeKind.Power); return;
            }
            if (FunctionCatalog.ArgumentCount(command) > 0) editor.InsertTemplate(MathNodeKind.Function, command);
            else editor.InsertText(command);
        }
    }
}
