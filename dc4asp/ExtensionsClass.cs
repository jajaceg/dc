﻿using System.Text;

namespace dc4asp
{
    static class ExtensionsClass
    {
        private static Random rng = new Random();

        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string AsText<T>(this IEnumerable<T> set)
        {
            StringBuilder sb = new();
            foreach (T el in set)
            {
                sb.Append($"{el} ");
            }
            return sb.ToString();
        }
    }
}
