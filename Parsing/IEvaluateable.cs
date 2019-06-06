using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    internal enum TypeFlags
    {
        ZeroNullEmpty = 0,
        Number = 1 << 0,
        Positive = 1 << 1,
        Negative = 1 << 2,
        NonInteger = 1 << 3,
        IntegerNatural = Number | ZeroNullEmpty | Positive | Negative,
        NumberAny = Number | ZeroNullEmpty | Positive | Negative | NonInteger,
        String = 1 << 16,
        StringAny = String | ZeroNullEmpty,
        Vector = 1 << 17,
        Boolean = 1 << 18,
        Imaginary = 1 << 19,
        Complex = NumberAny | Imaginary,
        Indexable = 20,
        Range = 21,
        Formula = 1 << 30,
        Error = 1 << 31,
        Any = ~ZeroNullEmpty
    }


    public interface IEvaluateable
    {
        /// <summary>Updates and returns the new value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }

        IEvaluateable UpdateValue();

        
    }

    internal interface ITypeFlag
    {
        TypeFlags Flags { get; }
    }

    public interface IContext
    {
        bool TryGetSubcontext(string token, out IContext ctxt);
        bool TryGetVariable(string token, out IVariable var);
        bool TryGetConstant(string token, out IEvaluateable k);

        IContext Parent { get; }
    }

    public interface IRangeable : IContext
    {
        bool TryGetImmobile(string token, out Reference r);
    }

    internal interface IValidateValue { InputConstraint[] GetConstraints(); }



    public interface IVariable : IEvaluateable
    {
        IContext Context { get; }

        IEvaluateable Contents { get; }

        string Name { get; }
    }

    internal interface IIndexable : IEvaluateable
    {
        IEvaluateable MaxIndex { get; }
        IEvaluateable MinIndex { get; }
        IEvaluateable this[params Number[] indices] { get; }

    }


    public class InputConstraint
    {
        public readonly bool IsVariadic;

        internal readonly TypeFlags[] Allowed;

        internal InputConstraint(bool isVariadic, params TypeFlags[] flags) { this.IsVariadic = isVariadic; this.Allowed = flags; }

        public bool MatchesCount(int count)
            => IsVariadic ? count >= Allowed.Length : count == Allowed.Length;

        /// <summary>Returns the non-matching constraint index.  If all match, returns the count of matching types.</summary>
        internal int MatchesTypes(IEnumerable<TypeFlags> flags)
        {
            int idx = 0;
            foreach (TypeFlags cf in flags)
                if ((cf & ~Allowed[idx++]) != TypeFlags.ZeroNullEmpty)
                    break;
            return idx;
        }

        /// <summary>Returns the non-matching constraint idx.  If all match, returns the count of matching types.</summary>
        public int MatchesTypes(IEnumerable<IEvaluateable> items)
        {
            int idx = 0;
            foreach (IEvaluateable iev in items)
            {
                if (iev is ITypeFlag itf)
                {
                    TypeFlags itemFlags = itf.Flags;
                    if ((itemFlags & Allowed[idx]) != itemFlags)
                        break;
                }
                else
                    break;
                idx++;
            }
            return idx;
        }

    }

}
