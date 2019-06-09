using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    internal enum TypeFlags
    {
        Zero = 0,
        Number = 1 << 0,
        Positive = 1 << 1,
        Negative = 1 << 2,
        NonInteger = 1 << 3,
        Integer = 1 << 4,
        IntegerNatural = Number | Zero | Positive | Negative | Integer,
        NumberAny = Number | Zero | Positive | Negative | NonInteger | Integer,
        String = 1 << 16,
        StringAny = String | Zero,
        Vector = 1 << 17,
        Boolean = 1 << 18,
        Imaginary = 1 << 19,
        Complex = NumberAny | Imaginary,
        Indexable = 1 << 20,
        Range = 1 << 21,
        Empty = 1 << 28,
        Null = 1 << 29,
        ZeroNullEmpty = Zero |  Null  | Empty,
        Formula = 1 << 30,
        Error = 1 << 31,
        Any = ~Zero
    }
    

    public interface IEvaluateable
    {
        /// <summary>Updates and returns the new value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }

        IEvaluateable UpdateValue();

        
    }

    internal interface ILiteral<TClr> : IEvaluateable
    {
        TypeFlags Types { get; }

        TClr CLRValue { get; }
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

    internal interface IRangeable : IContext
    {
        bool TryGetImmobile(string token, out Parse.Reference r);
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

        private InputConstraint(bool isVariadic, params TypeFlags[] flags) { this.IsVariadic = isVariadic; this.Allowed = flags; }
        internal static InputConstraint Variadic(params TypeFlags[] flags) => new InputConstraint(true, flags);
        internal static InputConstraint Nonvariadic(params TypeFlags[] flags) => new InputConstraint(false, flags);

        public bool MatchesCount(int count)
            => IsVariadic ? count >= Allowed.Length : count == Allowed.Length;

        /// <summary>Returns the non-matching constraint index.  If all match, returns the count of matching types.</summary>
        internal int MatchesTypes(IEnumerable<TypeFlags> flags)
        {
            int idx = 0;
            foreach (TypeFlags cf in flags)
                if ((cf & ~Allowed[idx++]) != TypeFlags.Zero)
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
