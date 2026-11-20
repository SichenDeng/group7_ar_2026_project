using NUnit.Framework;
using UnityEngine;

namespace ARFunctionLab.Tests
{
    public class GraphCoordinatesTests
    {
        [Test]
        public void MathPointRoundTripsThroughUnityCoordinates()
        {
            Vector3 unityPoint = GraphCoordinates.ToUnity(1.25, -2.5, 7.75);
            Vector3 mathPoint = GraphCoordinates.ToMath(unityPoint);

            Assert.That(mathPoint.x, Is.EqualTo(1.25f));
            Assert.That(mathPoint.y, Is.EqualTo(-2.5f));
            Assert.That(mathPoint.z, Is.EqualTo(7.75f));
        }

        [Test]
        public void FormulaHeightMapsToUnityVerticalAxis()
        {
            Vector3 unityPoint = GraphCoordinates.ToUnity(2.0, 3.0, 5.0);

            Assert.That(unityPoint, Is.EqualTo(new Vector3(2f, 5f, 3f)));
        }
    }
}
