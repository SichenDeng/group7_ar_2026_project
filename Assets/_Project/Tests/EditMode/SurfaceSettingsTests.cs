using NUnit.Framework;

namespace ARFunctionLab.Tests
{
    public class SurfaceSettingsTests
    {
        [Test]
        public void DefaultSettingsAreValid()
        {
            SurfaceSettings settings = new SurfaceSettings();

            bool valid = settings.Validate(out string message);

            Assert.That(valid, Is.True, message);
        }

        [TestCase(1)]
        [TestCase(257)]
        public void ResolutionOutsideSupportedRangeIsRejected(int cells)
        {
            SurfaceSettings settings = new SurfaceSettings { cells = cells };

            bool valid = settings.Validate(out string message);

            Assert.That(valid, Is.False);
            Assert.That(message, Does.Contain("Resolution"));
        }

        [Test]
        public void ReversedDomainIsRejected()
        {
            SurfaceSettings settings = new SurfaceSettings { xMin = 1, xMax = -1 };

            bool valid = settings.Validate(out string message);

            Assert.That(valid, Is.False);
            Assert.That(message, Does.Contain("minimum"));
        }

        [TestCase(0.0)]
        [TestCase(1000001.0)]
        public void InvalidHeightLimitIsRejected(double maximumAbsoluteHeight)
        {
            SurfaceSettings settings = new SurfaceSettings
            {
                maximumAbsoluteHeight = maximumAbsoluteHeight
            };

            bool valid = settings.Validate(out string message);

            Assert.That(valid, Is.False);
            Assert.That(message, Does.Contain("Height limit"));
        }

        [Test]
        public void CoordinateNamesAreRejectedAsParameters()
        {
            Assert.That(FormulaParser.IsParameterName("x"), Is.False,
                "x is a graph coordinate and cannot also be a parameter.");
            Assert.That(FormulaParser.IsParameterName("y"), Is.False,
                "y is a graph coordinate and cannot also be a parameter.");
        }

        [TestCase("a")]
        [TestCase("m")]
        [TestCase("w")]
        public void SupportedParameterNameIsAccepted(string name)
        {
            Assert.That(FormulaParser.IsParameterName(name), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("e")]
        [TestCase("z")]
        [TestCase("A")]
        [TestCase("aa")]
        public void UnsupportedParameterNameIsRejected(string name)
        {
            Assert.That(FormulaParser.IsParameterName(name), Is.False);
        }

        [Test]
        public void DuplicateParametersAreRejected()
        {
            SurfaceSettings settings = new SurfaceSettings();
            settings.parameters.Add(new FormulaParameter { name = "a", value = 1 });
            settings.parameters.Add(new FormulaParameter { name = "a", value = 2 });

            bool valid = settings.TryGetParameters(out _, out string message);

            Assert.That(valid, Is.False);
            Assert.That(message, Does.Contain("unique"));
        }
    }
}
