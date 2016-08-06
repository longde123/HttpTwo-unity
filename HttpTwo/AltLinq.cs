using System;
using System.Collections.Generic;

namespace HttpTwo
{
    static class AltLinq
    {
        internal static int Count<T>(IEnumerable<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            return Count(enumerator);
        }

        internal static int Count<T>(IEnumerator<T> enumerator)
        {
            int ret = 0;

            while (enumerator.MoveNext())
            {
                ret++;
            }

            return ret;
        }

        internal static T[] ToArray<T>(IEnumerable<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            int count = Count(enumerator);
            enumerator.Reset();
            enumerator.MoveNext();
            var ret = new T[count];
            for (int i = 0; i < count; i++)
            {
                ret[i] = enumerator.Current;
                enumerator.MoveNext();
            }

            return ret;
        }

        internal static bool SequenceEqual<T>(T[] a, T[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Equals(b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static T[] Take<T>(IEnumerable<T> enumerable, int count)
        {
            var ret = new T[count];
            var enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            for (int i = 0; i < count; i++)
            {
                ret[i] = enumerator.Current;
                enumerator.MoveNext();
            }

            return ret;
        }

        internal static bool Any<T>(IEnumerable<T> enumerable, Predicate<T> pred)
        {
            var enumerator = enumerable.GetEnumerator();
            while(enumerator.MoveNext())
            {
                if (pred(enumerator.Current))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
