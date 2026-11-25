using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARFunctionLab.Tests
{
    public class CrossSectionTests
    {
        private const float MeshTolerance = 1e-5f;

        [Test]
        public void FlatSurfaceProducesOneContinuousSegment()
        {
            CrossSectionCurve curve = Sample("0", Settings(), CrossSectionAxis.X, 0, 16);

            Assert.That(curve.Segments.Count, Is.EqualTo(1));
            Assert.That(curve.Segments[0].Count, Is.EqualTo(17), "A sample count of n yields n + 1 points.");
            Assert.That(curve.HasGaps, Is.False);
            Assert.That(curve.Breaks, Is.Empty, "A continuous curve has nothing to mark.");
            foreach (Vector3 point in curve.Segments[0])
            {
                Vector3 math = GraphCoordinates.ToMath(point);
                Assert.That(math.x, Is.EqualTo(0f).Within(MeshTolerance), "x is held fixed by the plane.");
                Assert.That(math.z, Is.EqualTo(0f).Within(MeshTolerance));
            }
        }

        [Test]
        public void ParaboloidCrossSectionMatchesIndependentCalculation()
        {
            CrossSectionCurve curve = Sample("x^2+y^2", Settings(), CrossSectionAxis.X, 1, 8);

            Assert.That(curve.Segments.Count, Is.EqualTo(1));
            foreach (Vector3 point in curve.Segments[0])
            {
                Vector3 math = GraphCoordinates.ToMath(point);
                Assert.That(math.x, Is.EqualTo(1f).Within(MeshTolerance));
                Assert.That(math.z, Is.EqualTo(1f + math.y * math.y).Within(MeshTolerance));
            }
        }

        [Test]
        public void AxisYHoldsTheYCoordinateFixed()
        {
            CrossSectionCurve curve = Sample("x^2+y^2", Settings(), CrossSectionAxis.Y, 1, 8);

            Assert.That(curve.Segments.Count, Is.EqualTo(1));
            foreach (Vector3 point in curve.Segments[0])
            {
                Vector3 math = GraphCoordinates.ToMath(point);
                Assert.That(math.y, Is.EqualTo(1f).Within(MeshTolerance));
                Assert.That(math.z, Is.EqualTo(math.x * math.x + 1f).Within(MeshTolerance));
            }
        }

        [Test]
        public void EachAxisSweepsItsOwnCoordinate()
        {
            // Deliberately asymmetric so swapping the two ranges would be visible.
            SurfaceSettings settings = Settings();
            settings.xMin = -1;
            settings.xMax = 1;
            settings.yMin = -5;
            settings.yMax = 5;

            List<Vector3> alongY = Sample("0", settings, CrossSectionAxis.X, 0, 4).Segments[0];
            List<Vector3> alongX = Sample("0", settings, CrossSectionAxis.Y, 0, 4).Segments[0];

            Assert.That(GraphCoordinates.ToMath(alongY[0]).y, Is.EqualTo(-5f).Within(MeshTolerance));
            Assert.That(GraphCoordinates.ToMath(alongY[4]).y, Is.EqualTo(5f).Within(MeshTolerance));
            Assert.That(GraphCoordinates.ToMath(alongX[0]).x, Is.EqualTo(-1f).Within(MeshTolerance));
            Assert.That(GraphCoordinates.ToMath(alongX[4]).x, Is.EqualTo(1f).Within(MeshTolerance));
        }

        [Test]
        public void PlaneOutsideTheDomainIsReportedRatherThanSilent()
        {
            CrossSectionCurve above = Sample("0", Settings(), CrossSectionAxis.X, 5, 16);
            CrossSectionCurve below = Sample("0", Settings(), CrossSectionAxis.X, -5, 16);

            Assert.That(above.Segments, Is.Empty);
            Assert.That(above.OutsideDomain, Is.True);
            Assert.That(below.OutsideDomain, Is.True);
        }

        [Test]
        public void UndefinedSampleSplitsTheCurveAndIsMarked()
        {
            SurfaceSettings settings = Settings();
            settings.xMin = -1;
            settings.xMax = 1;
            settings.yMin = -1;
            settings.yMax = 1;

            // Sampling hits y = 0 exactly, where 1/y is a division by zero.
            CrossSectionCurve curve = Sample("1/y", settings, CrossSectionAxis.X, 0, 4);

            Assert.That(curve.Segments.Count, Is.EqualTo(2), "The curve breaks either side of the discontinuity.");
            Assert.That(curve.Segments[0].Count, Is.EqualTo(2));
            Assert.That(curve.Segments[1].Count, Is.EqualTo(2));
            Assert.That(curve.UndefinedSamples, Is.EqualTo(1));
            Assert.That(curve.ExcludedSamples, Is.Zero);
            Assert.That(curve.Breaks.Count, Is.EqualTo(2), "Both sides of the gap are marked.");
        }

        [Test]
        public void SamplesBeyondTheHeightLimitAreDroppedAndMarked()
        {
            SurfaceSettings settings = Settings();
            settings.maximumAbsoluteHeight = 2;

            // z = y^2 over [-2, 2]; the four outermost samples exceed the limit.
            CrossSectionCurve curve = Sample("x^2+y^2", settings, CrossSectionAxis.X, 0, 8);

            Assert.That(curve.Segments.Count, Is.EqualTo(1));
            Assert.That(curve.Segments[0].Count, Is.EqualTo(5));
            Assert.That(curve.ExcludedSamples, Is.EqualTo(4));
            Assert.That(curve.UndefinedSamples, Is.Zero);
            Assert.That(curve.Breaks.Count, Is.EqualTo(2), "Each clipped end is marked.");
            foreach (Vector3 point in curve.Segments[0])
            {
                Assert.That(Mathf.Abs(GraphCoordinates.ToMath(point).z), Is.LessThanOrEqualTo(2f + MeshTolerance));
            }
        }

        [Test]
        public void IsolatedPointDrawsNoLineButStillReportsTheBreak()
        {
            SurfaceSettings settings = Settings();
            settings.xMin = -1;
            settings.xMax = 1;
            settings.yMin = -1;
            settings.yMax = 1;

            // Sampling y = -1, 0, 1 leaves one defined point either side of the discontinuity,
            // and a run of one point cannot be drawn as a line.
            CrossSectionCurve curve = Sample("1/y", settings, CrossSectionAxis.X, 0, 2);

            Assert.That(curve.Segments, Is.Empty);
            Assert.That(curve.HasGaps, Is.True);
            Assert.That(curve.Breaks.Count, Is.EqualTo(2),
                "Nothing is drawable, so the markers are the only feedback the user gets.");
        }

        [Test]
        public void ClampKeepsThePlaneInsideTheDomain()
        {
            SurfaceSettings settings = Settings();
            CrossSectionSettings cut = new CrossSectionSettings { axis = CrossSectionAxis.X, value = 9 };

            cut.ClampToDomain(settings);
            Assert.That(cut.value, Is.EqualTo(2.0));

            cut.value = -9;
            cut.ClampToDomain(settings);
            Assert.That(cut.value, Is.EqualTo(-2.0));
        }

        [Test]
        public void ClampUsesTheFixedAxisDomain()
        {
            SurfaceSettings settings = Settings();
            settings.xMin = -1;
            settings.xMax = 1;
            settings.yMin = -5;
            settings.yMax = 5;

            CrossSectionSettings cut = new CrossSectionSettings { axis = CrossSectionAxis.Y, value = 4 };
            cut.ClampToDomain(settings);
            Assert.That(cut.value, Is.EqualTo(4.0), "y = 4 is inside the y domain.");

            cut.axis = CrossSectionAxis.X;
            cut.ClampToDomain(settings);
            Assert.That(cut.value, Is.EqualTo(1.0), "The same value must clamp to the narrower x domain.");
        }

        [Test]
        public void MissingFormulaCutsNothing()
        {
            CrossSectionCurve curve = CrossSectionSampler.Sample(
                null, Settings(), new Dictionary<string, double>(), CrossSectionAxis.X, 0, 8);

            Assert.That(curve.Segments, Is.Empty);
            Assert.That(curve.OutsideDomain, Is.False);
        }

        private static SurfaceSettings Settings()
        {
            return new SurfaceSettings
            {
                xMin = -2,
                xMax = 2,
                yMin = -2,
                yMax = 2,
                cells = 8,
                maximumAbsoluteHeight = 100
            };
        }

        private static CrossSectionCurve Sample(
            string source, SurfaceSettings settings, CrossSectionAxis axis, double value, int samples)
        {
            FormulaParser parser = new FormulaParser();
            bool parsed = parser.TryParse(source, null, out ParsedFormula formula, out FormulaError error);
            Assert.That(parsed, Is.True, error == null ? "Formula did not parse." : error.Message);

            return CrossSectionSampler.Sample(
                formula, settings, new Dictionary<string, double>(), axis, value, samples);
        }
    }
}
