using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public static class Common
    {
        [DebuggerStepThrough]
        public static void AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail();
            }
            catch (T)
            {
                // Do nothing.
            }
            catch (AssertFailedException)
            {
                throw new AssertFailedException("Expected " + typeof(T).Name + " was not thrown.");
            }
            catch (Exception e)
            {
                throw new AssertFailedException("Wrong exception type: " + e.GetType().Name + "\n" + e.Message);
            }
        }

        [DebuggerStepThrough]
        public static void AssertThrows(Action action) => AssertThrows<Exception>(action);

        [DebuggerStepThrough]
        public static void AssertNoThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                throw new AssertFailedException("An exception of type " + e.GetType().Name + " was thrown.", e);
            }
        }

        public static void AssertNoThrow<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                throw new AssertFailedException("Prohibited exception of type " + typeof(T).Name + " was thrown.");
            }
            catch (Exception)
            {
                // Do nothing.
            }
        }


        public static void Permute<T>(IList<T> items, int seed = 0)
        {
            Random rng = new Random(0);
            for (int i = 0; i < items.Count; i++)
            {
                int j = rng.Next(0, items.Count);
                T temp = items[j];
                items[j] = items[i];
                items[i] = temp;
            }
        }

        public static long Time<TInput>(Action<TInput> action, TInput input0)
        {
            DateTime start = DateTime.Now;
            action(input0);
            DateTime end = DateTime.Now;
            return (end - start).Milliseconds;
        }
    }
}
