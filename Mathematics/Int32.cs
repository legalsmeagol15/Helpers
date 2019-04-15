﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public static class Int32
    {
        /// <summary>
        /// Calculates the square root of the given int (System.Int32).
        /// </summary>
        /// <remarks>From http://www.codecodex.com/wiki/Calculate_an_integer_square_root#C.23 .</remarks>
        public static int Sqrt(int num)
        {
            if (0 == num) { return 0; }  // Avoid zero divide  
            int n = (num / 2) + 1;       // Initial estimate, never low  
            int n1 = (n + (num / n)) / 2;
            while (n1 < n)
            {
                n = n1;
                n1 = (n + (num / n)) / 2;
            } 
            return n;
        } 


        /// <summary>
        /// Returns the log base 2 of the given number.
        /// </summary> 
        public static int Log_2(int number)
        {
            if (number < 1) throw new ArithmeticException("There is no integer log_2 of an int whose value is less than 1.");
            int highest1 = 0;
            for (int i = 1; i<32; i++)
            {
                number <<= 1;
                if ((number & 1) == 1) highest1 = i;
            }
            return highest1;
        }
        public static int Log(int number, double newBase)
        {
            return (int)Math.Log(number, newBase);
        }

        public static int Exp_2(int number)
        {            
            return 1 << number;
        }
        public static bool IsEven(int number)
        {
            return (number & 1) == 0;
        }
        public static bool IsOdd(int number) => (number & 1) == 0;

        public static int Mod(int number, int modulus)
        {
            return ((number % modulus) + modulus) % modulus;
        }

        /// <summary>Returns the next power of 2 above the given number.</summary>
        public static int RoundUpPow2(int x)
        {
            if (x <= 0) return 0;
            for (int i = 1; i < 32; i++)
            {
                if ((x >> i) == 0) return 1 << i;                
            }
            return 0;
        }

        public static int Pow(int number, int power)
        {
            if (power < 0) throw new ArgumentException("Power must be 0 or greater.");
            int result = 1;
            for (int i = 0; i < power; i++) result *= number;
            return result;
        }
    }
}