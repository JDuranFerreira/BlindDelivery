using BlindDelivery.Gameplay.Levels;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class PlatformBuilderTests
    {
        [Test]
        public void CalculatePositions_LeftToRight_IncludesBothEnds()
        {
            float[] positions = PlatformBuilder.CalculatePositions(0f, 3f, 1f);
            Assert.That(positions, Is.EqualTo(new[] { 0f, 1f, 2f, 3f }));
        }

        [Test]
        public void CalculatePositions_ReversedMarkers_StillRunsLeftToRight()
        {
            float[] positions = PlatformBuilder.CalculatePositions(0f, -3f, 1f);
            Assert.That(positions, Is.EqualTo(new[] { -3f, -2f, -1f, 0f }));
        }

        [Test]
        public void CalculatePositions_SnapsMarkersToGrid()
        {
            float[] positions = PlatformBuilder.CalculatePositions(0.1f, 2.9f, 1f);
            Assert.That(positions, Is.EqualTo(new[] { 0f, 1f, 2f, 3f }));
        }

        [Test]
        public void CalculatePositions_SameMarker_GivesOneCell()
        {
            float[] positions = PlatformBuilder.CalculatePositions(2f, 2f, 1f);
            Assert.That(positions, Is.EqualTo(new[] { 2f }));
        }
    }
}
