using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataStructures;
using System.Reflection;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class Test_IntervalSet
    {
        [TestMethod]
        public void TestIntervalSet_Adding()
        {
            IntegerSet intSet = new IntegerSet();

            MethodInfo[] methods = typeof(IntervalSet<int>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).ToArray();
            MethodInfo mInfo = methods.First(m=> m.Name=="GetIndex" && m.GetParameters().Length==1);            
            
            Assert.IsNotNull(mInfo);
            Func<int, int> getIndex = (i) => (int)mInfo.Invoke(intSet, new object[] { i });
            Assert.AreEqual(-1, getIndex(0));
            Assert.AreEqual(-1, getIndex(1000000));

            for (int setIdx = 0; setIdx < 100; setIdx+=20)
            {
                for (int i = setIdx + 0; i < setIdx + 20; i++) Assert.IsFalse(intSet.Includes(i));
                Assert.IsTrue(intSet.Include(setIdx));
                Assert.IsFalse(intSet.Include(setIdx));                
                for (int i = setIdx + 1; i < setIdx + 10; i++) Assert.IsFalse(intSet.Includes(i));
                for (int i = setIdx + 1; i < setIdx + 10; i++) Assert.IsTrue(intSet.Include(i));
                for (int i = setIdx + 1; i < setIdx + 10; i++) Assert.IsFalse(intSet.Include(i));
                for (int i = setIdx + 0; i < setIdx + 10; i++) Assert.IsTrue(intSet.Includes(i));
                for (int i = setIdx + 10; i < setIdx + 20; i++) Assert.IsFalse(intSet.Includes(i));
            }

            Assert.AreEqual(-1, getIndex(-1));
            Assert.AreEqual(4, getIndex(1000000));
            for (int i = 0; i < 100; i++)
            {
                int expected = i / 20;
                int actual = getIndex(i);
                Assert.AreEqual(expected, actual);
            }

            Assert.IsFalse(intSet.Includes(-1));
            Assert.IsFalse(intSet.Includes(1000000));
            
        }

        public void TestIntervalSet_Removing()
        {
            
        }
    }
}
