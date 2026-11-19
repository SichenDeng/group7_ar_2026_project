using System.Collections.Generic;
using System.Text;

namespace ARFunctionLab
{
    public enum MathNodeKind { Symbol, Power, Fraction, Root, Function, Group, Absolute }

    public class MathRow
    {
        public int Id;
        public List<MathNode> Nodes = new List<MathNode>();

        public MathRow Copy()
        {
            MathRow copy = new MathRow { Id = Id };
            for (int i = 0; i < Nodes.Count; i++) copy.Nodes.Add(Nodes[i].Copy());
            return copy;
        }
    }

    public class MathNode
    {
        public int Id;
        public MathNodeKind Kind;
        public string Text;
        public List<MathRow> Slots = new List<MathRow>();

        public MathNode Copy()
        {
            MathNode copy = new MathNode { Id = Id, Kind = Kind, Text = Text };
            for (int i = 0; i < Slots.Count; i++) copy.Slots.Add(Slots[i].Copy());
            return copy;
        }
    }

    public class FormulaDocument
    {
        public MathRow Root;
        private int nextId = 1;

        public FormulaDocument() { Root = CreateRow(); }

        public MathRow CreateRow() { return new MathRow { Id = nextId++ }; }

        public MathNode CreateNode(MathNodeKind kind, string text, int slots)
        {
            MathNode node = new MathNode { Id = nextId++, Kind = kind, Text = text };
            for (int i = 0; i < slots; i++) node.Slots.Add(CreateRow());
            return node;
        }

        public FormulaDocument Copy()
        {
            FormulaDocument copy = new FormulaDocument();
            copy.Root = Root.Copy();
            copy.nextId = nextId;
            return copy;
        }

        public MathRow FindRow(int id) { return FindRow(Root, id); }

        private MathRow FindRow(MathRow row, int id)
        {
            if (row.Id == id) return row;
            for (int i = 0; i < row.Nodes.Count; i++)
            {
                for (int j = 0; j < row.Nodes[i].Slots.Count; j++)
                {
                    MathRow found = FindRow(row.Nodes[i].Slots[j], id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public bool FindParent(int rowId, out MathRow parent, out int nodeIndex, out int slotIndex)
        {
            return FindParent(Root, rowId, out parent, out nodeIndex, out slotIndex);
        }

        private bool FindParent(MathRow row, int id, out MathRow parent, out int nodeIndex, out int slotIndex)
        {
            for (int i = 0; i < row.Nodes.Count; i++)
            {
                MathNode node = row.Nodes[i];
                for (int j = 0; j < node.Slots.Count; j++)
                {
                    if (node.Slots[j].Id == id)
                    {
                        parent = row; nodeIndex = i; slotIndex = j;
                        return true;
                    }
                    if (FindParent(node.Slots[j], id, out parent, out nodeIndex, out slotIndex)) return true;
                }
            }
            parent = null; nodeIndex = -1; slotIndex = -1;
            return false;
        }

        public int FindEmptyRow() { return FindEmptyRow(Root); }

        private int FindEmptyRow(MathRow row)
        {
            if (row.Nodes.Count == 0) return row.Id;
            for (int i = 0; i < row.Nodes.Count; i++)
            {
                for (int j = 0; j < row.Nodes[i].Slots.Count; j++)
                {
                    int empty = FindEmptyRow(row.Nodes[i].Slots[j]);
                    if (empty != 0) return empty;
                }
            }
            return 0;
        }

        public bool TryExport(out string source, out int emptyRowId)
        {
            emptyRowId = FindEmptyRow();
            source = "";
            if (emptyRowId != 0) return false;
            source = Export(Root);
            return true;
        }

        private string Export(MathRow row)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < row.Nodes.Count; i++)
            {
                MathNode node = row.Nodes[i];
                if (node.Kind == MathNodeKind.Symbol)
                {
                    if (node.Text == "π") text.Append("(pi)");
                    else text.Append(node.Text);
                }
                else if (node.Kind == MathNodeKind.Power) text.Append("((" + Export(node.Slots[0]) + ")^(" + Export(node.Slots[1]) + "))");
                else if (node.Kind == MathNodeKind.Fraction) text.Append("((" + Export(node.Slots[0]) + ")/(" + Export(node.Slots[1]) + "))");
                else if (node.Kind == MathNodeKind.Group) text.Append("(" + Export(node.Slots[0]) + ")");
                else if (node.Kind == MathNodeKind.Absolute) text.Append("(abs(" + Export(node.Slots[0]) + "))");
                else if (node.Kind == MathNodeKind.Root)
                {
                    if (node.Slots.Count == 1) text.Append("(sqrt(" + Export(node.Slots[0]) + "))");
                    else text.Append("(root(" + Export(node.Slots[0]) + "," + Export(node.Slots[1]) + "))");
                }
                else
                {
                    text.Append("(" + node.Text + "(");
                    for (int j = 0; j < node.Slots.Count; j++)
                    {
                        if (j > 0) text.Append(",");
                        text.Append(Export(node.Slots[j]));
                    }
                    text.Append("))");
                }
            }
            return text.ToString();
        }
    }
}
