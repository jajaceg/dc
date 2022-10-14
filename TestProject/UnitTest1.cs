using dc4asp;
using System.Collections.Immutable;

namespace TestProject
{
    public class Tests
    {
        [Test]
        public void TestStableModelChecking()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create<int>(1, -6));    // a :- not f.
            model.rules.Add(ImmutableList.Create<int>(2, -3));    // b :- not c.
            model.rules.Add(ImmutableList.Create<int>(3, 1));     // c :- a.
            model.rules.Add(ImmutableList.Create<int>(4, 1, -2)); // d :- a, not b.
            model.rules.Add(ImmutableList.Create<int>(4, -4));    // d :- not d.
            model.rules.Add(ImmutableList.Create<int>(5, 4, -6)); // e :- d, not f.
            model.rules.Add(ImmutableList.Create<int>(6, 2));     // f :- b.
            bool result = model.isStable(ImmutableHashSet.Create(1, 3, 4, 5));  // a, c, d, e
            Assert.IsTrue(result);
            result = model.isStable(ImmutableHashSet.Create(1, 3, 4, 5, 6));  // a, c, d, e, f
            Assert.IsFalse(result);
        }

        [Test]
        public void TestAnswerSets()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create<int>(1, -6));    // a :- not f.
            model.rules.Add(ImmutableList.Create<int>(2, -3));    // b :- not c.
            model.rules.Add(ImmutableList.Create<int>(3, 1));     // c :- a.
            model.rules.Add(ImmutableList.Create<int>(4, 1, -2)); // d :- a, not b.
            model.rules.Add(ImmutableList.Create<int>(4, -4));    // d :- not d.
            model.rules.Add(ImmutableList.Create<int>(5, 4, -6)); // e :- d, not f.
            model.rules.Add(ImmutableList.Create<int>(6, 2));     // f :- b.
            var result = model.AnswerSets().First();
            Assert.That(result, Is.EquivalentTo(ImmutableHashSet.Create(1, 3, 4, 5)));
        }
    }
}