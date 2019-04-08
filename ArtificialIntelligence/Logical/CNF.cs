using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtificialIntelligence.Logical
{
    public class CNF<TLiteral>
    {
        public class DisjointClause
        {
            internal readonly Literal<TLiteral>[] Literals;

        }
        internal class Literal<TLiteral>
        {
            public readonly bool Negative;
            public readonly TLiteral Tag;
            public Literal(TLiteral tag, bool negative = false)
            {
                Tag = tag;
                Negative = negative;
            }

            
            public static implicit operator Literal<TLiteral>(TLiteral l)
            {
                return new Literal<TLiteral>(l, false);
            }

            public static Literal<TLiteral> operator -(Literal<TLiteral> l)
            {
                return new Literal<TLiteral>(l.Tag, !l.Negative);
            }

            public override string ToString()
            {
                if (Negative) return "-" + Tag.ToString();
                return Tag.ToString();
            }
        }
    }
}
