using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Mathematics
{
    public static class Primes
    {
        private static readonly List<int> _IntPrimes = new List<int>() { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };
        
        /// <summary>Returns the list of all prime factors (other than 1) which are factors for all given ints.</summary>
        public static IList<int> GetFactors(int a, int b, params int[] ints)
        {
            //TODO:  validate GetFactors

            HashSet<int> @checked = new HashSet<int>();
            List<int> result = new List<int>();
            int end = Math.Min(Math.Min(a, b), ints.Min());
            int prime = 2;
            while (prime * prime <= end)
            {
                if (IsFactor(prime)) AddFactor(prime);
                prime = GetNextPrime(prime);
            }

            return result;

            void AddFactor(int factor)
            {
                if (!@checked.Add(factor)) return;
                result.Add(prime);
                for (int i = 0; i < result.Count; i++)
                {
                    int combined = result[i] * factor;
                    if (combined > end) return;
                    if (IsFactor(combined)) AddFactor(combined);
                }
            }

            bool IsFactor(int num) => (a % num == 0 && b % num == 0 && ints.All(i => i % num == 0));
        }

        /// <summary>
        /// Complexity:  this method is guaranteed to return in no more than 5d iterations, where d is the number of base-10 digits in the 
        /// largest provided int.
        /// </summary>
        public static int GetGreatestCommonDenominator(int a, int b)
        {
            int mod = -1;
            while (mod != 0)
            {
                if (a == 1 || b==1) return 1;                
                else if (a > b) { mod = a % b; a = mod; }
                else { mod = b % a; b = mod; }
            }
            return a;
        }

        public static int GetNextPrime(int after)
        {
            // Try to find the next prime from among the known primes.  In C#'s List.BinarySearch, if the item is in the list, the index 
            // of the prime will be the return value.  Otherwise, the bitwise complement (~) of the index of the next item is returned.
            // If there is no "next" item, the bitwise complement of the _IntPrimes.Count is returned.
            int idx = _IntPrimes.BinarySearch(after);
            if (idx >= 0 && idx <= _IntPrimes.Count - 2) return _IntPrimes[idx + 1];
            else if (idx < 0 && ~idx < _IntPrimes.Count) return _IntPrimes[~idx];
            
            // Otherwise, all known primes <= after.  At least one new prime will need to be added.
            int candidate = _IntPrimes.Last() + 2;
            while (candidate <= int.MaxValue)
            {
                //TODO:  Use the IsProbablyPrime approach to weed out definite non-primes.
                if (_IntPrimes.Any(p => p % candidate == 0)) { candidate += 2; continue; }
                
                //A prime was found to add to the list of known primes.
                _IntPrimes.Add(candidate);
                if (candidate > after) return candidate;    //Shortcut since the next prime was found.
                candidate+=2;
            }

            throw new OverflowException("The next prime would exceed the maximum value for an int.");
        }


        public static bool IsPrime(int number)
        {
            if (number < 2) return false;

            //If the number would be held in the known primes, return whether it is contained there.
            int idx = _IntPrimes.BinarySearch(number);
            if (idx >= 0) return true;
            else if (~idx < _IntPrimes.Count) return false;            
            
            //Failing that, add to the known primes.
            int candidate = _IntPrimes.Last() + 2;
            while (candidate <= number)
            {
                //TODO:  Use the IsProbablyPrime approach to weed out definite non-primes.
                if (_IntPrimes.Any(p => number % p == 0)) return false;                

                //A prime was found to add to the list of known primes.
                _IntPrimes.Add(candidate);
                if (candidate == number) return true;                
                candidate += 2;  // Adding to is an optimization since even numbers are never prime other than 2.
            }

            // All numbers equal to and below this number have been checked for primeness.  This number cannot be prime.
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
