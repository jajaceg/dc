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
            model.rules.Add(ImmutableList.Create<int>(2, -7));    // a :- not f.
            model.rules.Add(ImmutableList.Create<int>(3, -4));    // b :- not c.
            model.rules.Add(ImmutableList.Create<int>(4, 2));     // c :- a.
            model.rules.Add(ImmutableList.Create<int>(5, 2, -3)); // d :- a, not b.
            model.rules.Add(ImmutableList.Create<int>(5, -5));    // d :- not d.
            model.rules.Add(ImmutableList.Create<int>(6, 5, -7)); // e :- d, not f.
            model.rules.Add(ImmutableList.Create<int>(7, 3));     // f :- b.
            bool result = model.isStable(ImmutableHashSet.Create(2, 4, 5, 6));  // a, c, d, e
            Assert.IsTrue(result);
            result = model.isStable(ImmutableHashSet.Create(2, 4, 5, 6, 7));  // a, c, d, e, f
            Assert.IsFalse(result);
        }
    }
}