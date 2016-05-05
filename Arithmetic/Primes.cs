using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Arithmetic
{
    public static class Primes
    {
        private static List<int> _IntPrimes = new List<int>();
        static Primes()
        {
            _IntPrimes.Add(2);
        }
        public static int GetNextPrime(int after)
        {
            if (after < 2) return 2;

            //Try to find the next prime from among the known primes.
            if (after < _IntPrimes.Last())
            {
                for (int i = _IntPrimes.Count - 2; i >= 0; i--)
                    if (_IntPrimes[i] <= after) return _IntPrimes[i + 1];                
            }

            //Failing that, add to the known primes.
            int candidate = _IntPrimes.Last() + 1;
            while (candidate <= int.MaxValue)
            {
                int primeIdx = 0;
                while (primeIdx < _IntPrimes.Count)
                {
                    int prime = _IntPrimes[primeIdx++];          
                    if (candidate % prime == 0)  //This candidate cannot be a prime.  Try the next candidate.
                    {
                        candidate++;
                        primeIdx = 0;
                        continue;
                    }
                }

                //A prime was found to add to the list of known primes.
                _IntPrimes.Add(candidate);
                if (candidate > after) return candidate;    //Shortcut since the next prime was found.
                candidate++;
            }

            throw new OverflowException("The next prime would exceed the maximum value for an int.");
        }

        public static bool IsPrime(int number)
        {
            if (number < 2) return false;

            //If the number would be held in the known primes, return whether it is contained there.
            for (int i = 0; i < _IntPrimes.Count; i++)
            {
                int known = _IntPrimes[i];
                if (known == number) return true;
                if (known > number) return false;                
            }
            
            //Failing that, add to the known primes.
            int candidate = _IntPrimes.Last() + 1;
            while (candidate <= number)
            {
                int primeIdx = 0;
                while (primeIdx < _IntPrimes.Count)
                {
                    int prime = _IntPrimes[primeIdx++];
                    if (candidate % prime == 0)  //This candidate cannot be a prime.  Try the next candidate.
                    {
                        candidate++;
                        primeIdx = 0;
                        continue;
                    }
                }

                //A prime was found to add to the list of known primes.
                _IntPrimes.Add(candidate);
                if (candidate == number) return true;                
                candidate++;
            }

            //In the cases where no new prime can be found which is equal to or smaller than the number, then the number cannot be prime.
            return false;
        }

        /// <summary>
        /// Uses Fermat's little theorem to determine if a number is probably prime.
        /// </summary>
        /// <param name="n">The number whose primality is being tested.</param>
        /// <param name="threshold">The certainty with which the result is required.</param>        
        public static bool IsProbablyPrime(BigInteger n, double threshold)
        {
            throw new NotImplementedException();
        }



    }
}
