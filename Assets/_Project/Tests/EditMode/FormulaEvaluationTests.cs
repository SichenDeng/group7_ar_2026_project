using System.Collections.Generic;
using NUnit.Framework;

namespace ARFunctionLab.Tests
{
    public class FormulaEvaluationTests
    {
        private const double EvaluationTolerance = 1e-12;

        [TestCase("0", 7.0, -3.0, 0.0)]
        [TestCase("x^2+y^2", 3.0, 4.0, 25.0)]
        [TestCase("x^2-y^2", 3.0, 2.0, 5.0)]
        [TestCase("sin(x)cos(y)", 1.5707963267948966, 0.0, 1.0)]
        public void KnownFormulaMatchesExpectedValue(string source, double x, double y, double expected)
        {
            ParsedFormula formula = Parse(source);

            bool evaluated = formula.TryEvaluate(
                x,
                y,
                new Dictionary<string, double>(),
                out double actual,
                out EvaluationError error);

            Assert.That(evaluated, Is.True, "Evaluation failed with " + error + ".");
            Assert.That(actual, Is.EqualTo(expected).Within(EvaluationTolerance));
        }

        [Test]
        public void DeclaredParameterIsUsedDuringEvaluation()
        {
            FormulaParser parser = new FormulaParser();
            bool parsed = parser.TryParse(
                "a*x+y",
                new[] { "a" },
                out ParsedFormula formula,
                out FormulaError parseError);

            Assert.That(parsed, Is.True, ParseFailureMessage(parseError));

            bool evaluated = formula.TryEvaluate(
                2.0,
                3.0,
                new Dictionary<string, double> { { "a", 4.0 } },
                out double actual,
                out EvaluationError error);

            Assert.That(evaluated, Is.True, "Evaluation failed with " + error + ".");
            Assert.That(actual, Is.EqualTo(11.0).Within(EvaluationTolerance));
        }

        [TestCase("sqrt(-1)", EvaluationError.Domain)]
        [TestCase("1/0", EvaluationError.DivisionByZero)]
        public void InvalidMathReturnsSpecificError(string source, EvaluationError expectedError)
        {
            ParsedFormula formula = Parse(source);

            bool evaluated = formula.TryEvaluate(
                0.0,
                0.0,
                new Dictionary<string, double>(),
                out _,
                out EvaluationError error);

            Assert.That(evaluated, Is.False);
            Assert.That(error, Is.EqualTo(expectedError));
        }

        [TestCase("")]
        [TestCase("unknown(x)")]
        [TestCase("x+")]
        public void InvalidFormulaIsRejected(string source)
        {
            FormulaParser parser = new FormulaParser();

            bool parsed = parser.TryParse(source, null, out _, out FormulaError error);

            Assert.That(parsed, Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Message, Is.Not.Empty);
        }

        private static ParsedFormula Parse(string source)
        {
            FormulaParser parser = new FormulaParser();
            bool parsed = parser.TryParse(source, null, out ParsedFormula formula, out FormulaError error);
            Assert.That(parsed, Is.True, ParseFailureMessage(error));
            return formula;
        }

        private static string ParseFailureMessage(FormulaError error)
        {
            return error == null ? "Formula did not parse." : error.Message;
        }
    }
}
