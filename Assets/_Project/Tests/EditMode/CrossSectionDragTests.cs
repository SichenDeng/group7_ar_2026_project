using NUnit.Framework;
using UnityEngine;

namespace ARFunctionLab.Tests
{
    public class CrossSectionDragTests
    {
        private const double Tolerance = 1e-4;

        [Test]
        public void SlideDirectionFollowsTheFixedAxis()
        {
            Assert.That(CrossSectionDrag.SlideDirection(CrossSectionAxis.X), Is.EqualTo(Vector3.right));
            Assert.That(CrossSectionDrag.SlideDirection(CrossSectionAxis.Y), Is.EqualTo(Vector3.forward),
                "Math y is Unity z.");
        }

        [Test]
        public void RayStraightDownResolvesToItsOwnCoordinate()
        {
            bool resolved = CrossSectionDrag.TryResolve(
                new Vector3(3, 5, 0), Vector3.down, Vector3.zero, CrossSectionAxis.X, out double value);

            Assert.That(resolved, Is.True);
            Assert.That(value, Is.EqualTo(3.0).Within(Tolerance));
        }

        [Test]
        public void AxisYResolvesAlongLocalZ()
        {
            bool resolved = CrossSectionDrag.TryResolve(
                new Vector3(0, 5, -2), Vector3.down, Vector3.zero, CrossSectionAxis.Y, out double value);

            Assert.That(resolved, Is.True);
            Assert.That(value, Is.EqualTo(-2.0).Within(Tolerance));
        }

        [Test]
        public void SlantedRayResolvesToWhereItCrossesTheAxis()
        {
            // The ray passes exactly through (2, 0, 0), which lies on the slide line.
            Vector3 origin = new Vector3(0, 4, -4);
            Vector3 direction = new Vector3(2, 0, 0) - origin;

            bool resolved = CrossSectionDrag.TryResolve(
                origin, direction, Vector3.zero, CrossSectionAxis.X, out double value);

            Assert.That(resolved, Is.True);
            Assert.That(value, Is.EqualTo(2.0).Within(Tolerance));
        }

        [Test]
        public void AnchorOffsetShiftsTheResult()
        {
            bool resolved = CrossSectionDrag.TryResolve(
                new Vector3(4, 9, 0), Vector3.down, new Vector3(1, 2, 0), CrossSectionAxis.X, out double value);

            Assert.That(resolved, Is.True);
            Assert.That(value, Is.EqualTo(4.0).Within(Tolerance),
                "The answer is a coordinate on the axis, not a distance from the anchor.");
        }

        [Test]
        public void RayParallelToTheAxisIsRejected()
        {
            bool resolved = CrossSectionDrag.TryResolve(
                new Vector3(0, 1, 0), Vector3.right, Vector3.zero, CrossSectionAxis.X, out double value);

            Assert.That(resolved, Is.False, "A parallel ray has no single closest point.");
        }

        [Test]
        public void DegenerateRayDirectionIsRejected()
        {
            bool resolved = CrossSectionDrag.TryResolve(
                Vector3.zero, Vector3.zero, Vector3.zero, CrossSectionAxis.X, out double value);

            Assert.That(resolved, Is.False);
        }

        [TestCase(2.03, 2.0)]
        [TestCase(1.9, 2.0)]
        [TestCase(-0.02, 0.0)]
        public void SnapPullsNearbyValuesOntoATick(double value, double expected)
        {
            Assert.That(CrossSectionDrag.Snap(value, 1, 0.15), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(2.5)]
        [TestCase(1.4)]
        public void SnapLeavesValuesBetweenTicksAlone(double value)
        {
            Assert.That(CrossSectionDrag.Snap(value, 1, 0.15), Is.EqualTo(value).Within(Tolerance));
        }

        [Test]
        public void SnapRespectsTheStepSize()
        {
            Assert.That(CrossSectionDrag.Snap(4.9, 5, 0.15), Is.EqualTo(5.0).Within(Tolerance));
            Assert.That(CrossSectionDrag.Snap(4.9, 1, 0.15), Is.EqualTo(5.0).Within(Tolerance));
            Assert.That(CrossSectionDrag.Snap(3.0, 5, 0.15), Is.EqualTo(3.0).Within(Tolerance),
                "3 is too far from either 0 or 5 to snap.");
        }

        [Test]
        public void SnapIgnoresAnUnusableStep()
        {
            Assert.That(CrossSectionDrag.Snap(1.23, 0, 0.15), Is.EqualTo(1.23).Within(Tolerance));
            Assert.That(CrossSectionDrag.Snap(1.23, double.NaN, 0.15), Is.EqualTo(1.23).Within(Tolerance));
        }
    }
}
