using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARFunctionLab.Tests
{
    public class SurfaceGeneratorTests
    {
        private const float MeshTolerance = 1e-5f;

        [Test]
        public void ZeroSurfaceCoversRequestedDomain()
        {
            SurfaceSettings settings = ValidSettings(cells: 2);
            settings.xMin = -1;
            settings.xMax = 1;
            settings.yMin = -2;
            settings.yMax = 2;

            SurfaceResult result = Generate("0", settings);
            try
            {
                Assert.That(result.TotalSamples, Is.EqualTo(9));
                Assert.That(result.UndefinedSamples, Is.Zero);
                Assert.That(result.ExcludedSamples, Is.Zero);
                Assert.That(result.Triangles, Is.EqualTo(8));
                Assert.That(result.Mesh.vertexCount, Is.EqualTo(9));

                HashSet<int> xCoordinates = new HashSet<int>();
                HashSet<int> yCoordinates = new HashSet<int>();
                foreach (Vector3 vertex in result.Mesh.vertices)
                {
                    Vector3 point = GraphCoordinates.ToMath(vertex);
                    Assert.That(point.z, Is.EqualTo(0f).Within(MeshTolerance));
                    xCoordinates.Add(Mathf.RoundToInt(point.x * 1000));
                    yCoordinates.Add(Mathf.RoundToInt(point.y * 1000));
                }

                CollectionAssert.AreEquivalent(new[] { -1000, 0, 1000 }, xCoordinates);
                CollectionAssert.AreEquivalent(new[] { -2000, 0, 2000 }, yCoordinates);
            }
            finally
            {
                Object.DestroyImmediate(result.Mesh);
            }
        }

        [Test]
        public void ParaboloidVerticesMatchIndependentCalculation()
        {
            SurfaceSettings settings = ValidSettings(cells: 4);
            settings.xMin = -1;
            settings.xMax = 1;
            settings.yMin = -1;
            settings.yMax = 1;

            SurfaceResult result = Generate("x^2+y^2", settings);
            try
            {
                Assert.That(result.TotalSamples, Is.EqualTo(25));
                Assert.That(result.Triangles, Is.EqualTo(32));
                Assert.That(result.Mesh.vertexCount, Is.EqualTo(25));

                foreach (Vector3 vertex in result.Mesh.vertices)
                {
                    Vector3 point = GraphCoordinates.ToMath(vertex);
                    float expected = point.x * point.x + point.y * point.y;
                    Assert.That(point.z, Is.EqualTo(expected).Within(MeshTolerance));
                }
            }
            finally
            {
                Object.DestroyImmediate(result.Mesh);
            }
        }

        private static SurfaceSettings ValidSettings(int cells)
        {
            return new SurfaceSettings
            {
                xMin = -1,
                xMax = 1,
                yMin = -1,
                yMax = 1,
                cells = cells,
                maximumAbsoluteHeight = 100
            };
        }

        private static SurfaceResult Generate(string source, SurfaceSettings settings)
        {
            FormulaParser parser = new FormulaParser();
            bool parsed = parser.TryParse(source, null, out ParsedFormula formula, out FormulaError parseError);
            Assert.That(parsed, Is.True, parseError == null ? "Formula did not parse." : parseError.Message);

            SurfaceGenerator generator = new SurfaceGenerator();
            bool generated = generator.TryGenerate(
                formula,
                settings,
                new Dictionary<string, double>(),
                out SurfaceResult result,
                out string message);

            Assert.That(generated, Is.True, message);
            return result;
        }
    }
}
