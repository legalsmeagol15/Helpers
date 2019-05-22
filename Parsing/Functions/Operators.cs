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
        protected internal enum Priorities
        {
            REFERENCE = 50,
            OR = 60,
            AND = 70,
            EXPONENTIATION = 100,
            NEGATION = 200,
            MULTIPLICATION = 300,
            DIVISION = 400,
            ADDITION = 500,
            SUBTRACTION = 600
        }
        protected Operator() { }        

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
            this.Contents = list.ToArray();
            return true;
        }
        protected bool ParseBinary(DataStructures.DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Previous == null || node.Next == null) return false;
            this.Contents = new IEvaluateable[] { node.Previous.Remove(), node.Next.Remove() };
            return true;
        }
        
    }

    public sealed class Addition : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> inputs)
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
    }

    public sealed class And : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> inputs)
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
    }

    public sealed class Division : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> rawInputs)
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
    }

    public sealed class Exponentiation : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> rawInputs)
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
    }

    public sealed class Multiplication : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> inputs)
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
    }

    public sealed class Negation : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> inputs)
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
            Contents = new IEvaluateable[] { node.Next.Remove() };
            return true;
        }
    }

    public sealed class Or : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> inputs)
        {
            if (!inputs.Any()) return new InputCountError(this, inputs, 2);
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
    }


    public sealed class Reference : Operator
    {
        public Reference(Transaction tHandle)
        {
            if (!tHandle.IsOpen) throw new SyntaxException();
        }
    }

    public sealed class Subtraction : Operator
    {
        protected override IEvaluateable Evaluate(IEnumerable<IEvaluateable> rawInputs)
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
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseBinary(node);
    }

    
}
