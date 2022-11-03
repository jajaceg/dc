using dc4asp;
using System.Collections.Immutable;

namespace TestProject
{
    public readonly record struct Graph(int[] vertices, (int, int)[] edges);

    public class Tests
    {
        [Test]
        public void TestStableModelChecking()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1, -6));    // a :- not f.
            model.rules.Add(ImmutableList.Create(2, -3));    // b :- not c.
            model.rules.Add(ImmutableList.Create(3, 1));     // c :- a.
            model.rules.Add(ImmutableList.Create(4, 1, -2)); // d :- a, not b.
            model.rules.Add(ImmutableList.Create(4, -4));    // d :- not d.
            model.rules.Add(ImmutableList.Create(5, 4, -6)); // e :- d, not f.
            model.rules.Add(ImmutableList.Create(6, 2));     // f :- b.
            bool result = model.IsStable(ImmutableHashSet.Create(1, 3, 4, 5));  // a, c, d, e
            Assert.IsTrue(result);
            result = model.IsStable(ImmutableHashSet.Create(1, 3, 4, 5, 6));  // a, c, d, e, f
            Assert.IsFalse(result);
        }

        [Test]
        public void TestAnswerSets1()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1, -6));    // a :- not f.
            model.rules.Add(ImmutableList.Create(2, -3));    // b :- not c.
            model.rules.Add(ImmutableList.Create(3, 1));     // c :- a.
            model.rules.Add(ImmutableList.Create(4, 1, -2)); // d :- a, not b.
            model.rules.Add(ImmutableList.Create(4, -4));    // d :- not d.
            model.rules.Add(ImmutableList.Create(5, 4, -6)); // e :- d, not f.
            model.rules.Add(ImmutableList.Create(6, 2));     // f :- b.
            var result = model.AnswerSets(6).First();
            Assert.That(result, Is.EquivalentTo(ImmutableHashSet.Create(1, 3, 4, 5)));
        }

        [Test]
        public void TestAnswerSets2()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1));    
            model.rules.Add(ImmutableList.Create(2, 1));    
            model.rules.Add(ImmutableList.Create(3, 1, 2));     
            model.rules.Add(ImmutableList.Create(4, 5));     
            var result = model.AnswerSets(5).First();
            Assert.That(result, Is.EquivalentTo(ImmutableHashSet.Create(1, 2, 3)));
        }

        [Test]
        public void TestAnswerSets3()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(0, -1, -2, -3));
            var result = model.AnswerSets(3).FirstOrDefault();
            Assert.IsNull(result);
        }

        [Test]
        public void TestAnswerSets4()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1, 2));
            var result = model.AnswerSets(2).FirstOrDefault();
            Assert.That(result, Is.EquivalentTo(ImmutableHashSet.Create<int>()));
        }

        [Test]
        public void TestAnswerSets5()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1, -2));    // a :- not b.
            model.rules.Add(ImmutableList.Create(2, -1));    // b :- not a.
            model.rules.Add(ImmutableList.Create(3, -4));    // c :- not d.
            model.rules.Add(ImmutableList.Create(4, -3));    // d :- not c.
            var result = model.AnswerSets(4);
            Assert.That(result.Count, Is.EqualTo(4));
        }

        [Test]
        public void TestAnswerSets6()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1, -2));    // a :- not b.
            model.rules.Add(ImmutableList.Create(2, -1));    // b :- not a.
            model.rules.Add(ImmutableList.Create(3, 1));     // p :- a.
            var result = model.AnswerSets(3);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ElementAt(0), Is.EquivalentTo(ImmutableHashSet.Create(2)));
            Assert.That(result.ElementAt(1), Is.EquivalentTo(ImmutableHashSet.Create(1, 3)));
        }

        [Test]
        public void TestAnswerSets7()
        {
            Model model = new();
            model.rules.Add(ImmutableList.Create(1, -2));    // a :- not b.
            model.rules.Add(ImmutableList.Create(2, -1));    // b :- not a.
            model.rules.Add(ImmutableList.Create(3, 1));     // p :- a.
            model.rules.Add(ImmutableList.Create(0, -2));    // :- not b.
            var result = model.AnswerSets(3);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ElementAt(0), Is.EquivalentTo(ImmutableHashSet.Create(2)));
        }
    }
}