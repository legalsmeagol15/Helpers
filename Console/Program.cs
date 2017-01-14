using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI.TheoremProving;

namespace Exercise
{
    class Program
    {
        static void Main(string[] args)
        {
            Clause[] clauses = new Clause[10];
            Clause c = new Clause("string");
            clauses[0] = c;

            Console.WriteLine(c.Tag == null);
            Console.WriteLine(clauses[1].Tag == null);
            
            Console.WriteLine(c);
            Console.Read();
        }
    }
}
