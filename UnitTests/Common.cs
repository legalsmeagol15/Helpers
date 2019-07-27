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

        public static void Permute<T> (IList<T> items, int seed = 0)
        {
            Random rng = new Random(0);
            for(int i = 0; i < items.Count; i++)
            {
                int j = rng.Next(0, items.Count);
                T temp = items[j];
                items[j] = items[i];
                items[i] = temp;
            }
        }
    }
}
