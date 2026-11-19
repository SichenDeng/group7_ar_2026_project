using System;
using System.Collections.Generic;

namespace ARFunctionLab
{
    public class FormulaEditor
    {
        private class Snapshot
        {
            public FormulaDocument Document;
            public int Row;
            public int Index;
        }

        private readonly List<Snapshot> undo = new List<Snapshot>();
        public FormulaDocument Document { get; private set; }
        public int RowId { get; private set; }
        public int Index { get; private set; }
        public int SelectionStart { get; private set; } = -1;
        public int SelectionEnd { get; private set; } = -1;
        public event Action ContentChanged;
        public event Action CursorChanged;

        public FormulaEditor()
        {
            Document = new FormulaDocument();
            RowId = Document.Root.Id;
        }

        public void SetCursor(int rowId, int index)
        {
            MathRow row = Document.FindRow(rowId);
            if (row == null) return;
            RowId = rowId;
            Index = Math.Max(0, Math.Min(index, row.Nodes.Count));
            SelectionStart = -1;
            SelectionEnd = -1;
            NotifyCursor();
        }

        public void Select(int rowId, int start, int end)
        {
            SetCursor(rowId, end);
            MathRow row = Document.FindRow(rowId);
            if (row == null) return;
            SelectionStart = Math.Max(0, Math.Min(start, row.Nodes.Count));
            SelectionEnd = Index;
            NotifyCursor();
        }

        public void SelectPrevious()
        {
            if (Index > 0) Select(RowId, Index - 1, Index);
            else
            {
                MathRow parent;
                int node;
                int slot;
                if (Document.FindParent(RowId, out parent, out node, out slot)) Select(parent.Id, node, node + 1);
            }
        }

        private void Save()
        {
            undo.Add(new Snapshot { Document = Document.Copy(), Row = RowId, Index = Index });
            if (undo.Count > 50) undo.RemoveAt(0);
        }

        private void NotifyCursor() { if (CursorChanged != null) CursorChanged(); }

        private void Changed()
        {
            SelectionStart = -1;
            SelectionEnd = -1;
            if (ContentChanged != null) ContentChanged();
            NotifyCursor();
        }

        private bool RemoveSelection()
        {
            if (SelectionStart < 0 || SelectionStart == SelectionEnd) return false;
            int start = Math.Min(SelectionStart, SelectionEnd);
            int count = Math.Abs(SelectionStart - SelectionEnd);
            Document.FindRow(RowId).Nodes.RemoveRange(start, count);
            Index = start;
            SelectionStart = -1;
            SelectionEnd = -1;
            return true;
        }

        public void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Save();
            RemoveSelection();
            MathRow row = Document.FindRow(RowId);
            for (int i = 0; i < text.Length; i++)
            {
                row.Nodes.Insert(Index++, Document.CreateNode(MathNodeKind.Symbol, text[i].ToString(), 0));
            }
            Changed();
        }

        private bool CanNest()
        {
            int levels = 0;
            int row = RowId;
            MathRow parent;
            int node;
            int slot;
            while (Document.FindParent(row, out parent, out node, out slot))
            {
                levels++;
                row = parent.Id;
            }
            return levels < 12;
        }

        public void InsertTemplate(MathNodeKind kind, string name = "", bool square = false, bool indexedRoot = false)
        {
            if (!CanNest()) return;
            Save();
            int slots = 1;
            if (kind == MathNodeKind.Power || kind == MathNodeKind.Fraction || indexedRoot) slots = 2;
            if (kind == MathNodeKind.Function) slots = FunctionCatalog.ArgumentCount(name);
            if (slots == 0) return;
            MathRow row = Document.FindRow(RowId);
            MathNode node = Document.CreateNode(kind, name, slots);
            bool selected = SelectionStart >= 0 && SelectionStart != SelectionEnd;
            int start = Index;
            if (selected)
            {
                start = Math.Min(SelectionStart, SelectionEnd);
                int count = Math.Abs(SelectionEnd - SelectionStart);
                node.Slots[0].Nodes.AddRange(row.Nodes.GetRange(start, count));
                row.Nodes.RemoveRange(start, count);
            }
            else if ((kind == MathNodeKind.Power || kind == MathNodeKind.Fraction) && Index > 0)
            {
                MathNode previous = row.Nodes[Index - 1];
                bool operand = previous.Kind != MathNodeKind.Symbol || char.IsLetterOrDigit(previous.Text[0]) || previous.Text == ".";
                if (operand)
                {
                    start = Index - 1;
                    if (previous.Kind == MathNodeKind.Symbol && (char.IsDigit(previous.Text[0]) || previous.Text == "."))
                    {
                        while (start > 0)
                        {
                            MathNode prior = row.Nodes[start - 1];
                            if (prior.Kind != MathNodeKind.Symbol || (!char.IsDigit(prior.Text[0]) && prior.Text != ".")) break;
                            start--;
                        }
                    }
                    node.Slots[0].Nodes.AddRange(row.Nodes.GetRange(start, Index - start));
                    row.Nodes.RemoveRange(start, Index - start);
                }
            }
            row.Nodes.Insert(start, node);
            if (square)
            {
                node.Slots[1].Nodes.Add(Document.CreateNode(MathNodeKind.Symbol, "2", 0));
                if (node.Slots[0].Nodes.Count > 0) { RowId = row.Id; Index = start + 1; }
                else { RowId = node.Slots[0].Id; Index = 0; }
            }
            else
            {
                int target = 0;
                if ((kind == MathNodeKind.Power || kind == MathNodeKind.Fraction) && node.Slots[0].Nodes.Count > 0) target = 1;
                RowId = node.Slots[target].Id;
                Index = node.Slots[target].Nodes.Count;
            }
            Changed();
        }

        public void Move(int direction)
        {
            if (SelectionStart >= 0)
            {
                SetCursor(RowId, direction > 0 ? Math.Max(SelectionStart, SelectionEnd) : Math.Min(SelectionStart, SelectionEnd));
                return;
            }
            MathRow row = Document.FindRow(RowId);
            int next = direction > 0 ? Index : Index - 1;
            if (next >= 0 && next < row.Nodes.Count)
            {
                MathNode node = row.Nodes[next];
                if (node.Slots.Count > 0)
                {
                    MathRow target = node.Slots[direction > 0 ? 0 : node.Slots.Count - 1];
                    SetCursor(target.Id, direction > 0 ? 0 : target.Nodes.Count);
                }
                else SetCursor(RowId, Index + direction);
                return;
            }
            ExitSlot(direction);
        }

        public void ExitSlot(int direction = 1)
        {
            MathRow parent;
            int nodeIndex;
            int slotIndex;
            if (!Document.FindParent(RowId, out parent, out nodeIndex, out slotIndex)) return;
            MathNode node = parent.Nodes[nodeIndex];
            int next = slotIndex + direction;
            if (next >= 0 && next < node.Slots.Count)
            {
                MathRow target = node.Slots[next];
                SetCursor(target.Id, direction > 0 ? 0 : target.Nodes.Count);
            }
            else SetCursor(parent.Id, nodeIndex + (direction > 0 ? 1 : 0));
        }

        public void Backspace()
        {
            Save();
            if (RemoveSelection()) { Changed(); return; }
            MathRow row = Document.FindRow(RowId);
            if (Index > 0)
            {
                MathNode node = row.Nodes[Index - 1];
                if (node.Slots.Count == 0) { row.Nodes.RemoveAt(--Index); Changed(); }
                else
                {
                    MathRow slot = node.Slots[node.Slots.Count - 1];
                    SetCursor(slot.Id, slot.Nodes.Count);
                }
                return;
            }
            MathRow parent;
            int nodeIndex;
            int slotIndex;
            if (!Document.FindParent(RowId, out parent, out nodeIndex, out slotIndex)) return;
            MathNode owner = parent.Nodes[nodeIndex];
            bool empty = true;
            for (int i = 0; i < owner.Slots.Count; i++)
            {
                if (owner.Slots[i].Nodes.Count > 0) empty = false;
            }
            if (empty)
            {
                parent.Nodes.RemoveAt(nodeIndex);
                RowId = parent.Id;
                Index = nodeIndex;
                Changed();
            }
            else ExitSlot(-1);
        }

        public void Clear()
        {
            Save();
            Document = new FormulaDocument();
            RowId = Document.Root.Id;
            Index = 0;
            Changed();
        }

        public void Undo()
        {
            if (undo.Count == 0) return;
            Snapshot snapshot = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            Document = snapshot.Document;
            RowId = snapshot.Row;
            Index = snapshot.Index;
            Changed();
        }

        public void LoadPreset(int preset)
        {
            Clear();
            if (preset == 0)
            {
                InsertTemplate(MathNodeKind.Function, "sin"); InsertText("x"); ExitSlot();
                InsertText("+"); InsertTemplate(MathNodeKind.Function, "cos"); InsertText("y"); ExitSlot();
            }
            else if (preset == 1)
            {
                InsertText("x"); InsertTemplate(MathNodeKind.Power, "", true);
                InsertText("-"); InsertText("y"); InsertTemplate(MathNodeKind.Power, "", true);
            }
            else if (preset == 2)
            {
                InsertTemplate(MathNodeKind.Root); InsertText("4-x"); InsertTemplate(MathNodeKind.Power, "", true);
                InsertText("-y"); InsertTemplate(MathNodeKind.Power, "", true); ExitSlot();
            }
            else
            {
                InsertText("1"); InsertTemplate(MathNodeKind.Fraction); InsertText("x"); ExitSlot();
            }
        }
    }
}
