using System;
using System.Collections.Generic;
using System.Globalization;

namespace ARFunctionLab
{
    public class FormulaParser
    {
        private class Token
        {
            public string Text;
            public int Start;
            public int Length;
            public bool IsNumber;
            public double Number;
        }

        private readonly List<Token> tokens = new List<Token>();
        private readonly List<string> usedParameters = new List<string>();
        private HashSet<string> parameters;
        private int position;
        private int depth;
        private FormulaError failure;

        public static bool IsParameterName(string name)
        {
            return name != null && name.Length == 1 && name[0] >= 'a' && name[0] <= 'w' && name != "e";
        }

        public bool TryParse(string source, IEnumerable<string> parameterNames, out ParsedFormula formula, out FormulaError error)
        {
            formula = null;
            failure = null;
            tokens.Clear();
            usedParameters.Clear();
            position = 0;
            depth = 0;
            parameters = new HashSet<string>();
            if (parameterNames != null)
            {
                foreach (string name in parameterNames)
                {
                    if (!IsParameterName(name))
                    {
                        error = new FormulaError("Invalid parameter name: " + name, 0, 0);
                        return false;
                    }
                    parameters.Add(name);
                }
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                error = new FormulaError("Enter a formula.", 0, 0);
                return false;
            }
            if (source.Length > 2048)
            {
                error = new FormulaError("Formula is too long (maximum 2048 characters).", 0, source.Length);
                return false;
            }
            int offset = 0;
            int equals = source.IndexOf('=');
            if (equals >= 0)
            {
                string prefix = "";
                for (int i = 0; i < equals; i++)
                {
                    if (!char.IsWhiteSpace(source[i])) prefix += source[i];
                }
                if (prefix != "z" && prefix != "f(x,y)")
                {
                    error = new FormulaError("Use z = expression or f(x,y) = expression.", 0, equals);
                    return false;
                }
                offset = equals + 1;
            }
            if (!Tokenize(source, offset)) { error = failure; return false; }
            InsertMultiplication();
            ExpressionNode root = Sum();
            if (failure == null && Current.Text != "") Fail("Unexpected token: " + Current.Text, Current);
            error = failure;
            if (failure != null) return false;
            formula = new ParsedFormula(source, root, usedParameters.ToArray());
            return true;
        }

        private bool Tokenize(string source, int offset)
        {
            int i = offset;
            while (i < source.Length)
            {
                char c = source[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                int start = i;
                if (char.IsDigit(c) || c == '.')
                {
                    while (i < source.Length && char.IsDigit(source[i])) i++;
                    if (i < source.Length && source[i] == '.')
                    {
                        i++;
                        while (i < source.Length && char.IsDigit(source[i])) i++;
                    }
                    if (i < source.Length && (source[i] == 'e' || source[i] == 'E'))
                    {
                        int end = i + 1;
                        if (end < source.Length && (source[end] == '+' || source[end] == '-')) end++;
                        int digits = end;
                        while (end < source.Length && char.IsDigit(source[end])) end++;
                        if (end > digits) i = end;
                    }
                    double number;
                    string text = source.Substring(start, i - start);
                    if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || !FunctionCatalog.IsFinite(number))
                    {
                        failure = new FormulaError("Invalid number.", start, Math.Max(1, i - start));
                        return false;
                    }
                    tokens.Add(new Token { Text = text, Start = start, Length = i - start, IsNumber = true, Number = number });
                }
                else if (char.IsLetter(c))
                {
                    while (i < source.Length && char.IsLetter(source[i])) i++;
                    if (source.Substring(start, i - start) == "log" && i + 1 < source.Length && source[i] == '1' && source[i + 1] == '0') i += 2;
                    string name = source.Substring(start, i - start);
                    if (name == "π") name = "pi";
                    if (FunctionCatalog.ArgumentCount(name) > 0 || name == "pi" || name == "e")
                    {
                        tokens.Add(new Token { Text = name, Start = start, Length = i - start });
                    }
                    else
                    {
                        for (int k = start; k < i; k++)
                        {
                            string variable = source[k].ToString();
                            if (variable != "x" && variable != "y" && variable != "e" && !parameters.Contains(variable))
                            {
                                failure = new FormulaError("Unknown name: " + source.Substring(start, i - start), start, i - start);
                                return false;
                            }
                        }
                        for (int k = start; k < i; k++) tokens.Add(new Token { Text = source[k].ToString(), Start = k, Length = 1 });
                    }
                }
                else
                {
                    string symbol = c.ToString();
                    if (c == '×' || c == '·') symbol = "*";
                    if (c == '÷') symbol = "/";
                    if (c == '−') symbol = "-";
                    if ("+-*/^(),".IndexOf(symbol, StringComparison.Ordinal) < 0)
                    {
                        failure = new FormulaError("Unsupported character: " + c, i, 1);
                        return false;
                    }
                    tokens.Add(new Token { Text = symbol, Start = i, Length = 1 });
                    i++;
                }
            }
            tokens.Add(new Token { Text = "", Start = source.Length, Length = 0 });
            return true;
        }

        private bool IsValue(Token token)
        {
            return token.IsNumber || token.Text == "pi" || token.Text == "e" || token.Text == "x" || token.Text == "y" || parameters.Contains(token.Text);
        }

        private void InsertMultiplication()
        {
            for (int i = 1; i < tokens.Count; i++)
            {
                Token left = tokens[i - 1];
                Token right = tokens[i];
                bool ends = IsValue(left) || left.Text == ")";
                bool starts = IsValue(right) || right.Text == "(" || FunctionCatalog.ArgumentCount(right.Text) > 0;
                if (ends && starts && !(left.IsNumber && right.IsNumber))
                {
                    tokens.Insert(i, new Token { Text = "*", Start = right.Start, Length = 0 });
                    i++;
                }
            }
        }

        private Token Current { get { return tokens[position]; } }

        private void Fail(string message, Token token)
        {
            if (failure == null) failure = new FormulaError(message, token.Start, token.Length);
        }

        private bool Match(string text)
        {
            if (Current.Text != text) return false;
            position++;
            return true;
        }

        private ExpressionNode Binary(string name, ExpressionNode left, ExpressionNode right)
        {
            return new ExpressionNode { Kind = ExpressionKind.Binary, Name = name, Left = left, Right = right };
        }

        private ExpressionNode Sum()
        {
            ExpressionNode node = Product();
            while (failure == null && (Current.Text == "+" || Current.Text == "-"))
            {
                string op = Current.Text;
                position++;
                node = Binary(op, node, Product());
            }
            return node;
        }

        private ExpressionNode Product()
        {
            ExpressionNode node = Unary();
            while (failure == null && (Current.Text == "*" || Current.Text == "/"))
            {
                string op = Current.Text;
                position++;
                node = Binary(op, node, Unary());
            }
            return node;
        }

        private ExpressionNode Unary()
        {
            depth++;
            if (depth > 64)
            {
                Fail("Formula is nested too deeply (maximum 64 levels).", Current);
                depth--;
                return null;
            }
            ExpressionNode node;
            if (Current.Text == "+" || Current.Text == "-")
            {
                string op = Current.Text;
                position++;
                node = new ExpressionNode { Kind = ExpressionKind.Unary, Name = op, Left = Unary() };
            }
            else node = Power();
            depth--;
            return node;
        }

        private ExpressionNode Power()
        {
            ExpressionNode node = Primary();
            if (failure == null && Match("^")) node = Binary("^", node, Unary());
            return node;
        }

        private ExpressionNode Primary()
        {
            if (failure != null) return null;
            Token token = Current;
            if (token.IsNumber)
            {
                position++;
                return new ExpressionNode { Kind = ExpressionKind.Number, Number = token.Number };
            }
            if (Match("("))
            {
                ExpressionNode node = Sum();
                if (!Match(")")) Fail("Expected a closing parenthesis.", Current);
                return node;
            }
            if (token.Text == "pi" || token.Text == "e")
            {
                position++;
                return new ExpressionNode { Kind = ExpressionKind.Number, Number = token.Text == "pi" ? Math.PI : Math.E };
            }
            if (token.Text == "x" || token.Text == "y" || parameters.Contains(token.Text))
            {
                position++;
                if (parameters.Contains(token.Text) && !usedParameters.Contains(token.Text)) usedParameters.Add(token.Text);
                return new ExpressionNode { Kind = ExpressionKind.Variable, Name = token.Text };
            }
            int count = FunctionCatalog.ArgumentCount(token.Text);
            if (count > 0)
            {
                position++;
                if (!Match("(")) { Fail("Expected '(' after " + token.Text + ".", Current); return null; }
                ExpressionNode a = Sum();
                ExpressionNode b = null;
                if (count == 2)
                {
                    if (!Match(",")) { Fail(token.Text + " requires two arguments separated by a comma.", Current); return null; }
                    b = Sum();
                }
                if (!Match(")")) Fail("Expected ')' after " + token.Text + " arguments.", Current);
                return new ExpressionNode { Kind = ExpressionKind.Function, Name = token.Text, Left = a, Right = b };
            }
            Fail("Expected a number, variable or function.", token);
            return null;
        }
    }
}
