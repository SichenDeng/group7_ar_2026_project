using System;
using System.Collections.Generic;

namespace ARFunctionLab
{
    public enum EvaluationError { None, DivisionByZero, Domain, NonFinite, MissingParameter }

    public class FormulaError
    {
        public string Message;
        public int Start;
        public int Length;

        public FormulaError(string message, int start, int length)
        {
            Message = message;
            Start = start;
            Length = length;
        }
    }

    internal enum ExpressionKind { Number, Variable, Unary, Binary, Function }

    internal class ExpressionNode
    {
        public ExpressionKind Kind;
        public string Name;
        public double Number;
        public ExpressionNode Left;
        public ExpressionNode Right;

        public EvaluationError Evaluate(double x, double y, IDictionary<string, double> parameters, out double result)
        {
            result = 0;
            if (Kind == ExpressionKind.Number) { result = Number; return EvaluationError.None; }
            if (Kind == ExpressionKind.Variable)
            {
                if (Name == "x") result = x;
                else if (Name == "y") result = y;
                else if (parameters == null || !parameters.TryGetValue(Name, out result)) return EvaluationError.MissingParameter;
                return FunctionCatalog.IsFinite(result) ? EvaluationError.None : EvaluationError.NonFinite;
            }
            double a;
            double b = 0;
            EvaluationError error = Left.Evaluate(x, y, parameters, out a);
            if (error != EvaluationError.None) return error;
            if (Right != null)
            {
                error = Right.Evaluate(x, y, parameters, out b);
                if (error != EvaluationError.None) return error;
            }
            if (Kind == ExpressionKind.Function) return FunctionCatalog.Evaluate(Name, a, b, out result);
            if (Kind == ExpressionKind.Unary) result = Name == "-" ? -a : a;
            else
            {
                switch (Name)
                {
                    case "+": result = a + b; break;
                    case "-": result = a - b; break;
                    case "*": result = a * b; break;
                    case "/":
                        if (b == 0) return EvaluationError.DivisionByZero;
                        result = a / b; break;
                    case "^":
                        if (a == 0 && b <= 0) return EvaluationError.Domain;
                        if (a < 0 && b != Math.Truncate(b)) return EvaluationError.Domain;
                        result = Math.Pow(a, b); break;
                }
            }
            return FunctionCatalog.IsFinite(result) ? EvaluationError.None : EvaluationError.NonFinite;
        }
    }

    public class ParsedFormula
    {
        private readonly ExpressionNode root;
        public string Source { get; private set; }
        public string[] ParameterNames { get; private set; }

        internal ParsedFormula(string source, ExpressionNode expression, string[] parameters)
        {
            Source = source;
            root = expression;
            ParameterNames = parameters;
        }

        public bool ValidateParameters(IDictionary<string, double> parameters, out string message)
        {
            message = "";
            for (int i = 0; i < ParameterNames.Length; i++)
            {
                double value;
                if (parameters == null || !parameters.TryGetValue(ParameterNames[i], out value))
                {
                    message = "Missing parameter: " + ParameterNames[i];
                    return false;
                }
                if (!FunctionCatalog.IsFinite(value))
                {
                    message = "Parameter must be finite: " + ParameterNames[i];
                    return false;
                }
            }
            return true;
        }

        public bool TryEvaluate(double x, double y, IDictionary<string, double> parameters, out double value, out EvaluationError error)
        {
            error = root.Evaluate(x, y, parameters, out value);
            return error == EvaluationError.None;
        }
    }
}
