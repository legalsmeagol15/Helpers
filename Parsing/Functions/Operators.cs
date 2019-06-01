using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;

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


    [InputTypeConstraint(0, true, TypesAllowed.Vector, TypesAllowed.IntegerNumber | TypesAllowed.PositiveNumber)]
    [InputTypeConstraint(1, false, TypesAllowed.Vector, TypesAllowed.Vector)]
    public sealed class Indexing : Operator, IIndexable
    {
        IEvaluateable IIndexable.this[params Number[] indices] => throw new NotImplementedException();

        IEvaluateable IIndexable.MaxIndex => throw new NotImplementedException();

        IEvaluateable IIndexable.MinIndex => throw new NotImplementedException();

        protected override IEvaluateable Evaluate(int constraintIndex, IEvaluateable[] inputs)
        {
            throw new NotImplementedException();
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (!ParseBinary(node)) return false;
            Clause curly = this.Inputs[1] as Clause;
            if (curly == null) throw new Exception("Should be impossible.");
            this.Inputs[1] = curly.Contents;
            return true;
        }

        public override string ToString() => Inputs[0].ToString() + "{ " + Inputs[1].ToString() + " }";
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
