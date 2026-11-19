using System;
using System.Collections.Generic;

namespace ARFunctionLab
{
    [Serializable]
    public class FormulaParameter
    {
        public string name = "a";
        public double value = 1;
    }

    [Serializable]
    public class SurfaceSettings
    {
        public double xMin = -10;
        public double xMax = 10;
        public double yMin = -10;
        public double yMax = 10;
        public int cells = 128;
        public double maximumAbsoluteHeight = 100;
        public List<FormulaParameter> parameters = new List<FormulaParameter>();

        public SurfaceSettings Copy()
        {
            SurfaceSettings copy = (SurfaceSettings)MemberwiseClone();
            copy.parameters = new List<FormulaParameter>();
            foreach (FormulaParameter parameter in parameters)
                copy.parameters.Add(parameter == null ? null : new FormulaParameter { name = parameter.name, value = parameter.value });
            return copy;
        }

        public bool TryGetParameters(out Dictionary<string, double> values, out string message)
        {
            values = new Dictionary<string, double>();
            message = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                FormulaParameter parameter = parameters[i];
                if (parameter == null || !FormulaParser.IsParameterName(parameter.name))
                {
                    message = "Parameters must use a single lowercase letter, excluding e, x, y and z.";
                    return false;
                }
                if (!FunctionCatalog.IsFinite(parameter.value) || values.ContainsKey(parameter.name))
                {
                    message = "Parameter values must be finite and names must be unique.";
                    return false;
                }
                values.Add(parameter.name, parameter.value);
            }
            return true;
        }

        public bool Validate(out string message)
        {
            message = "";
            if (!FunctionCatalog.IsFinite(xMin) || !FunctionCatalog.IsFinite(xMax) || !FunctionCatalog.IsFinite(yMin) || !FunctionCatalog.IsFinite(yMax) || xMin >= xMax || yMin >= yMax)
                message = "Each domain minimum must be smaller than its maximum.";
            else if (Math.Abs(xMin) > 10000 || Math.Abs(xMax) > 10000 || Math.Abs(yMin) > 10000 || Math.Abs(yMax) > 10000)
                message = "Domain limits must be within -10000 and 10000.";
            else if ((float)xMin >= (float)xMax || (float)yMin >= (float)yMax)
                message = "This range is too narrow to display at these coordinates.";
            else if (cells < 2 || cells > 256) message = "Resolution must be between 2 and 256 cells.";
            else if (!FunctionCatalog.IsFinite(maximumAbsoluteHeight) || maximumAbsoluteHeight <= 0 || maximumAbsoluteHeight > 1000000)
                message = "Height limit must be greater than zero and at most 1000000.";
            return message == "";
        }
    }
}
