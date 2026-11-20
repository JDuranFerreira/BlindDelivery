using BlindDelivery.Core.Save;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public sealed class LevelResultsDataTests
    {
        [Test]
        public void Record_KeepsBestTimeAndNeverLosesStars()
        {
            var data = new LevelResultsData();
            Assert.IsTrue(data.Record("Level01", 6f, 2));
            Assert.IsFalse(data.Record("Level01", 8f, 1), "slower run with fewer stars is no improvement");
            Assert.AreEqual(6f, data.BestTime("Level01"), 0.0001f);

            Assert.IsTrue(data.Record("Level01", 4f, 2), "faster time is an improvement");
            Assert.AreEqual(4f, data.BestTime("Level01"), 0.0001f);
            Assert.AreEqual(1, data.levels.Count);

            Assert.IsTrue(data.Record("Level02", 3f, 3));
            Assert.AreEqual(2, data.levels.Count);
        }

        [Test]
        public void BestTime_UnknownLevel_IsMaxValue()
        {
            var data = new LevelResultsData();
            Assert.AreEqual(float.MaxValue, data.BestTime("missing"));
        }
    }
}
