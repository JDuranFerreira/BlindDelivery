using BlindDelivery.Core.Items;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class ItemBudgetTests
    {
        [Test]
        public void Spend_DeductsOnlyWhenAffordable_AndFloorsAtZero()
        {
            var budget = new ItemBudget(12);

            Assert.That(budget.TrySpend(5), Is.True);
            Assert.That(budget.Points, Is.EqualTo(7));
            Assert.That(budget.TrySpend(10), Is.False);
            Assert.That(budget.Points, Is.EqualTo(7));
            Assert.That(budget.TrySpend(7), Is.True);
            Assert.That(budget.Points, Is.Zero);
            Assert.That(budget.TrySpend(1), Is.False);
        }

        [Test]
        public void Reset_RestoresTheFullPool()
        {
            var budget = new ItemBudget(3);
            budget.TrySpend(2);
            budget.Reset(10);

            Assert.That(budget.Points, Is.EqualTo(10));
        }
    }
}
