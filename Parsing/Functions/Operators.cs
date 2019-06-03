using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using System.Diagnostics;

namespace Dependency
{
    public abstract class Operator : Function
    {
        
        protected Operator() { }

        public static bool TryCreate(string token, out Operator oper)
        {
            switch (token)
            {
                case "-": oper = new Subtraction(); return true; // Might still end up a negation
                case "!":
                case "~": oper = new Negation(); return true;
                case "+": oper = new Addition(); return true;
                case "*": oper = new Multiplication(); return true;
                case "/": oper = new Division(); return true;
                case "^": oper = new Exponentiation(); return true;
                case "&": oper = new And(); return true;
                case "|": oper = new Or(); return true;
                case ":": oper = new Range(); return true;
                default:oper = null; return false;
            }
        }

        internal abstract bool Parse(DataStructures.DynamicLinkedList<IEvaluateable>.Node node);

        protected bool ParseMany<T>(DataStructures.DynamicLinkedList<IEvaluateable>.Node node) where T : IEvaluateable
        {
            if (node.Previous == null || node.Next == null) return false;
            IEvaluateable prev = node.Previous.Remove(), next = node.Next.Remove();
            LinkedList<IEvaluateable> list = new LinkedList<IEvaluateable>();
            list.AddLast(prev);
            list.AddLast(next);
            while (node.Previous != null && node.Previous is T)
            {
                node.Previous.Remove();
                if (node.Previous == null) return false;
                list.AddFirst(node.Previous.Remove());
            }
            while (node.Next != null && node.Next is T)
            {
                node.Next.Remove();
                if (node.Next == null) return false;
                list.AddLast(node.Next.Remove());
            }            
            this.Inputs = list.ToArray();
            return true;
        }
        protected bool ParseBinary(DataStructures.DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Previous == null || node.Next == null) return false;
            this.Inputs = new IEvaluateable[] { node.Previous.Remove(), node.Next.Remove() };
            return true;
        }
        
    }

    
    public sealed class Addition : Operator
    {

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs)
        {
            if (!inputs.Any()) return new InputCountError(this, inputs, 2);
            Number sum = 0;
            int idx = 0;
            foreach (IEvaluateable ie in inputs)
            {
                if (!_IsAddable(ie)) return new TypeMismatchError(this, inputs, idx, typeof(Number));
                sum += (Number)ie;
                idx++;
            }
            return sum;
            bool _IsAddable(IEvaluateable ie) => ie is Number;
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<Addition>(node);

        public override string ToString() => string.Join(" + ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class And : Operator
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] inputs)
        {
            if (!inputs.Any()) return new InputCountError(this, inputs, 2);
            Dependency.Boolean b = true;
            int idx = 0;
            foreach (IEvaluateable ie in inputs)
            {
                if (!_CanAnd(ie)) return new TypeMismatchError(this, inputs, idx, typeof(Number));
                b &= (Dependency.Boolean)ie;
                idx++;
            }
            return b;
            bool _CanAnd(IEvaluateable ie) => ie is Dependency.Boolean;
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<And>(node);

        public override string ToString() => string.Join(" & ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Division : Operator
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] rawInputs)
        {
            IEvaluateable[] inputs = rawInputs.ToArray();
            if (inputs.Length != 2) return new InputCountError(this, inputs, 2);
            if (!_IsDivisible(inputs[0])) return new TypeMismatchError(this, inputs, 0, typeof(Number));
            if (!_IsDivisible(inputs[1])) return new TypeMismatchError(this, inputs, 1, typeof(Number));
            Number a = (Number)inputs[0];
            Number b = (Number)inputs[1];
            return a / b;
            bool _IsDivisible(IEvaluateable ie) => ie is Number;
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseBinary(node);

        public override string ToString() => string.Join(" / ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Exponentiation : Operator
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] rawInputs)
        {
            IEvaluateable[] inputs = rawInputs.ToArray();
            if (inputs.Length != 2) return new InputCountError(this, inputs, 2);
            if (!_CanExponentiate(inputs[0])) return new TypeMismatchError(this, inputs, 0, typeof(Number));
            if (!_CanExponentiate(inputs[1])) return new TypeMismatchError(this, inputs, 1, typeof(Number));
            Number a = (Number)inputs[0];
            Number b = (Number)inputs[1];
            return a ^ b;
            bool _CanExponentiate(IEvaluateable ie) => ie is Number;
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseBinary(node);

        public override string ToString() => string.Join(" ^ ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Indexing : Operator, IExpression
    {
        //This is unique: an operator that is also a left-to-right expression.

        internal IEvaluateable Base => Inputs[0];

        /// <summary>The vector that this bracket object contains.</summary>
        internal IEvaluateable Ordinal { get => Inputs[1]; set { Inputs[1] = value; } }

        protected override IEvaluateable Evaluate(int constraintIndex, IEvaluateable[] inputs)
        {
            if (inputs.Length < 2) return new InputCountError(this, inputs, null);
            IEvaluateable b = inputs[0];
            IIndexable bi = b as IIndexable;
            if (bi == null)
                return new IndexingError(this, inputs, "An object of type " + b.GetType().Name + " is not indexable.");
            Number[] ordinals;
            switch (constraintIndex)
            {
                case 0:
                    ordinals = new Number[inputs.Length - 1];
                    for (int i = 1; i < inputs.Length; i++) ordinals[i - 1] = (Number)inputs[i];
                case 1:
                    if (!(inputs[1] is Vector))
                        return new IndexingError(this, inputs, "Index must be a set of numbers or a vector.");
                    Vector v = (Vector)inputs[1];
                    if (!v.TryOrdinalize(out ordinals))
                        return new IndexingError(this, inputs, "A vector index must evaluate to a set of numbers.");
            }
            IEvaluateable o = Ordinal.Value;
            
            switch (o)
            {
                case Number n: return bi[n];
                case Vector v:
                    if (v.TryOrdinalize(out Number[] ns)) return bi[ns];
                    return new IndexingError(this, new IEvaluateable[] { b, o }, "Vector ordinal cannot be converted to number indices.");
                default:
                    return new IndexingError(this, new IEvaluateable[] { b, o }, "An object of type " + o.GetType().Name + " cannot index.");
            }
        }
        

        public void SetContents(IEvaluateable @base, IEvaluateable contents) { Inputs = new IEvaluateable[] { @base, Ordinal }; }
        void IExpression.SetContents(params IEvaluateable[] contents)
        {
            Debug.Assert(contents.Length == 2);
            SetContents(contents[0], contents[1]);
        }

        public override string ToString() => Base.ToString() + "{" + Ordinal.ToString() + "}";
    }


    public sealed class Multiplication : Operator
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] inputs)
        {
            if (!inputs.Any()) return new InputCountError(this, inputs, 2);
            Number product = 0;
            int idx = 0;
            foreach (IEvaluateable ie in inputs)
            {
                if (!_CanMultply(ie)) return new TypeMismatchError(this, inputs, idx, typeof(Number));
                product *= (Number)ie;
                idx++;
            }
            if (idx == 1) return inputs.First();
            return product;
            bool _CanMultply(IEvaluateable ie) => ie is Number;
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<Multiplication>(node);

        public override string ToString() => string.Join(" * ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Negation : Operator
    {   
        protected override IEvaluateable Evaluate(IEvaluateable[] inputs)
        {
            if (!inputs.Any()) return new InputCountError(this, inputs, 1);
            IEvaluateable ie = inputs.First();
            if (ie != inputs.Last()) return new InputCountError(this, inputs, 1);
            if (ie is Number n) return -n;
            if (ie is Dependency.Boolean b) return !b;
            return new TypeMismatchError(this, inputs, 0, typeof(Number), typeof(Dependency.Boolean));            
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Next == null) return false;
            Inputs = new IEvaluateable[] { node.Next.Remove() };
            return true;
        }

        public override string ToString() => "-" + Inputs[0].ToString();
    }

    public sealed class Or : Operator
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] inputs)
        {
            if (inputs.Length == 0) return new InputCountError(this, inputs, 2);
            Dependency.Boolean b = false;
            int idx = 0;
            foreach (IEvaluateable ie in inputs)
            {
                if (!_CanOr(ie)) return new TypeMismatchError(this, inputs, idx, typeof(Number));
                b |= (Dependency.Boolean)ie;
                idx++;
            }
            return b;
            bool _CanOr(IEvaluateable ie) => ie is Dependency.Boolean;
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<Or>(node);

        public override string ToString() => string.Join(" | ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Range : Operator, IIndexable
    {
        public readonly IEvaluateable Start;
        public readonly IEvaluateable End;

        IEvaluateable IIndexable.this[params Number[] indices] => throw new NotImplementedException();

        IEvaluateable IIndexable.MaxIndex => throw new NotImplementedException();

        IEvaluateable IIndexable.MinIndex => throw new NotImplementedException();

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs)
        {
            throw new NotImplementedException();
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Subtraction : Operator
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] rawInputs)
        {
            IEvaluateable[] inputs = rawInputs.ToArray();
            if (inputs.Length != 2) return new InputCountError(this, inputs, 2);
            if (!_CanSubtract(inputs[0])) return new TypeMismatchError(this, inputs, 0, typeof(Number));
            if (!_CanSubtract(inputs[1])) return new TypeMismatchError(this, inputs, 1, typeof(Number));
            Number a = (Number)inputs[0];
            Number b = (Number)inputs[1];
            return a - b;
            bool _CanSubtract(IEvaluateable ie) => ie is Number;
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Previous == null || node.Previous.Contents is Operator)
            {
                Negation n = new Negation();
                node.Contents = n;
                return n.Parse(node);               
            }
            return ParseBinary(node);
        }

        public override string ToString() => string.Join(" - ", (IEnumerable<IEvaluateable>)Inputs);
    }

    
}
