using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class Test_Primes
    {
        [TestMethod]
        public void TestPrimes_IsPrime()
        {
            Assert.IsFalse(Arithmetic.Primes.IsPrime(-1));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(0));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(1));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(2));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(3));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(4));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(5));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(6));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(7));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(8));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(9));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(10));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(11));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(12));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(13));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(14));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(15));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(16));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(17));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(18));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(19));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(20));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(19));
            Assert.IsFalse(Arithmetic.Primes.IsPrime(18));
            Assert.IsTrue(Arithmetic.Primes.IsPrime(17));
        }

        [TestMethod]
        public void TestPrimes_GetNextPrime()
        {
            Assert.AreEqual(2, Arithmetic.Primes.GetNextPrime(-1));
            Assert.AreEqual(2, Arithmetic.Primes.GetNextPrime(0));
            Assert.AreEqual(2, Arithmetic.Primes.GetNextPrime(1));
            Assert.AreEqual(3, Arithmetic.Primes.GetNextPrime(2));
            Assert.AreEqual(5, Arithmetic.Primes.GetNextPrime(3));
            Assert.AreEqual(5, Arithmetic.Primes.GetNextPrime(4));
            Assert.AreEqual(7, Arithmetic.Primes.GetNextPrime(5));
            Assert.AreEqual(7, Arithmetic.Primes.GetNextPrime(6));
            Assert.AreEqual(11, Arithmetic.Primes.GetNextPrime(7));
            Assert.AreEqual(11, Arithmetic.Primes.GetNextPrime(8));
            Assert.AreEqual(11, Arithmetic.Primes.GetNextPrime(9));
            Assert.AreEqual(11, Arithmetic.Primes.GetNextPrime(10));
            Assert.AreEqual(13, Arithmetic.Primes.GetNextPrime(11));
            Assert.AreEqual(13, Arithmetic.Primes.GetNextPrime(12));
            Assert.AreEqual(17, Arithmetic.Primes.GetNextPrime(13));
            Assert.AreEqual(17, Arithmetic.Primes.GetNextPrime(14));
            Assert.AreEqual(17, Arithmetic.Primes.GetNextPrime(15));
            Assert.AreEqual(17, Arithmetic.Primes.GetNextPrime(16));
            Assert.AreEqual(19, Arithmetic.Primes.GetNextPrime(17));
            Assert.AreEqual(19, Arithmetic.Primes.GetNextPrime(18));
            Assert.AreEqual(23, Arithmetic.Primes.GetNextPrime(19));
            Assert.AreEqual(23, Arithmetic.Primes.GetNextPrime(20));
            Assert.AreEqual(997, Arithmetic.Primes.GetNextPrime(996));
            Assert.AreEqual(23, Arithmetic.Primes.GetNextPrime(20));
            Assert.AreEqual(23, Arithmetic.Primes.GetNextPrime(19));
            Assert.AreEqual(19, Arithmetic.Primes.GetNextPrime(18));
            Assert.AreEqual(19, Arithmetic.Primes.GetNextPrime(17));

            try
            {
                //Arithmetic.Primes.GetNextPrime(int.MaxValue); //Takes too long, duh!
                //Assert.Fail("An exception should have been thrown.");
            }
            catch (OverflowException)
            {
                //Correct exception thrown.
            }
            catch(Exception)
            {
                Assert.Fail("Wrong exception type thrown.");
            }
        }
    }
}
