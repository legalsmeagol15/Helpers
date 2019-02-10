using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{

    public sealed class TupleDictionary<TKey, TValue> where TKey : UnorderedTuple
    {

    }

    public abstract class UnorderedTuple
    {
        public override abstract int GetHashCode();
        public override abstract bool Equals(object other);
        public abstract override string ToString();
    }

    public sealed class UnorderedTuple<Ta, Tb> : UnorderedTuple
    {
        public readonly Ta A;
        public readonly Tb B;
        public UnorderedTuple(Ta a, Tb b) { this.A = a;this.B = b; }
        public override string ToString() => "{" + A.ToString() + "," + B.ToString() + "}";
        public override bool Equals(object other)
        {            
            UnorderedTuple<Ta, Tb> o1 = other as UnorderedTuple<Ta, Tb>;
            if (o1 != null)
            {
                if (o1.A.Equals(A) && o1.B.Equals(B)) return true;
                if (o1.B.Equals(A) && o1.A.Equals(B)) return true;
                return false;
            }

            UnorderedTuple<Tb, Ta> o2 = other as UnorderedTuple<Tb, Ta>;
            if (o2 != null)
            {
                if (o2.A.Equals(A) && o2.B.Equals(B)) return true;
                if (o2.B.Equals(A) && o2.A.Equals(B)) return true;
                return false;
            }

            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
