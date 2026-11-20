using System;
using NUnit.Framework;

namespace ARFunctionLab.Tests
{
    public class FunctionCatalogTests
    {
        private const double EvaluationTolerance = 1e-12;

        [TestCase("sin", Math.PI / 2, 0.0, 1.0)]
        [TestCase("cos", Math.PI, 0.0, -1.0)]
        [TestCase("tan", Math.PI / 4, 0.0, 1.0)]
        [TestCase("asin", 1.0, 0.0, Math.PI / 2)]
        [TestCase("acos", 1.0, 0.0, 0.0)]
        [TestCase("atan", 1.0, 0.0, Math.PI / 4)]
        [TestCase("sqrt", 9.0, 0.0, 3.0)]
        [TestCase("abs", -3.0, 0.0, 3.0)]
        [TestCase("ln", Math.E, 0.0, 1.0)]
        [TestCase("log10", 100.0, 0.0, 2.0)]
        [TestCase("exp", 1.0, 0.0, Math.E)]
        [TestCase("log", 2.0, 8.0, 3.0)]
        [TestCase("root", 3.0, -8.0, -2.0)]
        public void SupportedFunctionMatchesKnownValue(
            string name,
            double firstArgument,
            double secondArgument,
            double expected)
        {
            EvaluationError error = FunctionCatalog.Evaluate(
                name,
                firstArgument,
                secondArgument,
                out double actual);

            Assert.That(error, Is.EqualTo(EvaluationError.None));
            Assert.That(actual, Is.EqualTo(expected).Within(EvaluationTolerance));
        }

        [TestCase("asin", 2.0, 0.0)]
        [TestCase("acos", -2.0, 0.0)]
        [TestCase("sqrt", -1.0, 0.0)]
        [TestCase("ln", 0.0, 0.0)]
        [TestCase("log10", 0.0, 0.0)]
        [TestCase("log", 1.0, 8.0)]
        [TestCase("log", -2.0, 8.0)]
        [TestCase("log", 2.0, -8.0)]
        [TestCase("root", 0.0, 8.0)]
        [TestCase("root", 2.0, -8.0)]
        [TestCase("root", 2.5, 8.0)]
        [TestCase("notAFunction", 1.0, 1.0)]
        public void InvalidFunctionInputReturnsDomainError(string name, double firstArgument, double secondArgument)
        {
            EvaluationError error = FunctionCatalog.Evaluate(name, firstArgument, secondArgument, out _);

            Assert.That(error, Is.EqualTo(EvaluationError.Domain));
        }

        [Test]
        public void OverflowReturnsNonFiniteError()
        {
            EvaluationError error = FunctionCatalog.Evaluate("exp", 1000.0, 0.0, out _);

            Assert.That(error, Is.EqualTo(EvaluationError.NonFinite));
        }

        [Test]
        public void CatalogNamesHaveMatchingArgumentCounts()
        {
            foreach (string name in FunctionCatalog.Names)
            {
                int expected = name == "log" || name == "root" ? 2 : 1;
                Assert.That(FunctionCatalog.ArgumentCount(name), Is.EqualTo(expected), name);
            }
        }
    }
}
