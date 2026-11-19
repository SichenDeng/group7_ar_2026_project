using System;

namespace ARFunctionLab
{
    public static class FunctionCatalog
    {
        public static readonly string[] Names = { "sin", "cos", "tan", "asin", "acos", "atan", "sqrt", "abs", "ln", "log10", "exp", "log", "root" };

        public static int ArgumentCount(string name)
        {
            if (name == "log" || name == "root") return 2;
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] == name) return 1;
            }
            return 0;
        }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static EvaluationError Evaluate(string name, double a, double b, out double value)
        {
            value = 0;
            switch (name)
            {
                case "sin": value = Math.Sin(a); break;
                case "cos": value = Math.Cos(a); break;
                case "tan": value = Math.Tan(a); break;
                case "asin":
                    if (a < -1 || a > 1) return EvaluationError.Domain;
                    value = Math.Asin(a); break;
                case "acos":
                    if (a < -1 || a > 1) return EvaluationError.Domain;
                    value = Math.Acos(a); break;
                case "atan": value = Math.Atan(a); break;
                case "sqrt":
                    if (a < 0) return EvaluationError.Domain;
                    value = Math.Sqrt(a); break;
                case "abs": value = Math.Abs(a); break;
                case "ln":
                    if (a <= 0) return EvaluationError.Domain;
                    value = Math.Log(a); break;
                case "log10":
                    if (a <= 0) return EvaluationError.Domain;
                    value = Math.Log10(a); break;
                case "exp": value = Math.Exp(a); break;
                case "log":
                    if (a <= 0 || a == 1 || b <= 0) return EvaluationError.Domain;
                    value = Math.Log(b) / Math.Log(a); break;
                case "root":
                    if (a < 1 || a != Math.Truncate(a)) return EvaluationError.Domain;
                    if (b < 0 && a % 2 != 1) return EvaluationError.Domain;
                    value = b < 0 ? -Math.Pow(-b, 1 / a) : Math.Pow(b, 1 / a); break;
                default: return EvaluationError.Domain;
            }
            return IsFinite(value) ? EvaluationError.None : EvaluationError.NonFinite;
        }
    }
}
