using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public static class Common
    {
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

        public static void AssertThrows(Action action) => AssertThrows<Exception>(action);
    }
}
