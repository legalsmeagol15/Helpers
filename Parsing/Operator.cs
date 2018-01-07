using Graphs;
using DataStructures;
using Mathematics.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

//using DataStructures.Graphs;
//using Graphs;

namespace Parsing.Operators
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>The </remarks>
    internal abstract class Operator : Formula
    {
        protected Operator(DataContext context, params object[] inputs) : base(context, inputs) { }
        protected Operator(DataContext context, IEnumerable<object> inputs) : base(context, inputs) { }



        protected abstract string Symbol { get; }


        /// <summary>Parses the operator given the context of its node within a token list.  Default parsing behavior is to combine the 
        /// preceding and following nodes' contents as Inputs and remove those nodes.  If adjacent nodes are missing, an exception is 
        /// thrown.</summary>
        /// <param name="node">The token list node containing the operator.</param>
        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Previous == null) throw new LexingException("Operator type " + Symbol + " requires preceding input.");
            if (node.Next == null) throw new LexingException("Operator type " + Symbol + " requires following input.");

            Inputs = new object[2] { node.Previous.Remove(), node.Next.Remove() };
        }





        internal static bool TryLex(string token, DynamicLinkedList<object> existingInputs, DataContext context, out Operator op)
        {
            switch (token)
            {
                case "+": op = new Plus(context); return true;
                case "-":   //Is it a substraction, or an inversion?
                    {
                        if (existingInputs.Count == 0 || existingInputs.Last is Operator) op = new Negation(context);
                        else op = new Minus(context);
                        return true;
                    }
                case "*": op = new Star(context); return true;
                case "/": op = new Slash(context); return true;
                case "^": op = new Hat(context); return true;
                case "&": op = new Ampersand(context); return true;
                case "|": op = new Pipe(context); return true;
                case ".": op = new Dot(context); return true;
                case ":": op = new Colon(context); return true;
                case "%": op = new Percent(context); return true;
                case "!": op = new Negation(context); return true;
                case "~": op = new Negation(context); return true;
                default: { op = null; return false; }
            }
        }


        public override string ToString()
        {
            if (Inputs.Length > 1) return string.Join(" " + Symbol + " ", Inputs);
            if (Inputs.Length == 1) return Inputs[0] + Symbol + "_";
            return "_" + Symbol + "_";
        }

        protected internal override object FromSimplified(DataContext context, IList<object> simplifiedInputs) 
            => throw new InvalidOperationException("Operator " + Symbol + " cannot be simplified.");


        #region Operator simplification





        #endregion


        #region Operator static operations



        #endregion


    }




    internal sealed class Ampersand : Operator
    {
        public Ampersand(DataContext context) : base(context) { }

        protected override object Evaluate(IList<object> evaluatedInputs) => string.Join("", evaluatedInputs);

        protected override string Symbol => "&";
        protected override int ParsingPriority => PRIORITY_AMPERSAND;
        
        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");

    }


    internal sealed class Colon : Operator
    {
        public Colon(DataContext context) : base(context) { }

        protected override string Symbol => ":";

        protected override int ParsingPriority => PRIORITY_COLON;


        protected override object Evaluate(IList<object> evaluatedInputs) => throw new NotImplementedException();

        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            //base.Parse(node);

            ////Was a ternary being specified?
            //if (Inputs[0] is Question q && q.Inputs.Count == 2)
            //{
            //    q.Inputs.Add(Inputs[1]);
            //    node.Contents = q;
            //    q.CombineVariables(this);
            //}
            throw new NotImplementedException();
        }
        

        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Dot : Operator
    {
        public Dot(DataContext context) : base(context) { }

        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            base.Parse(node);
            //Associate();
            if (Inputs.Any(i => !(i is Variable)))
                throw new LexingException("Dot operator must appear only between two Variable objects.");
        }


        protected override object Evaluate(IList<object> evaluatedInputs) => evaluatedInputs[evaluatedInputs.Count - 1];

        protected override string Symbol => ".";


        public override string ToString() => string.Join(Symbol, Inputs);

        protected override int ParsingPriority => PRIORITY_DOT;
        

        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Hat : Operator
    {
        public Hat(DataContext context) : base(context) { }
        internal Hat(DataContext context, params object[] inputs) : base(context, inputs) { }

        protected override string Symbol => "^";

        protected override int ParsingPriority => PRIORITY_HAT;


        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[evaluatedInputs.Count - 1] is decimal m)
            {
                for (int i = evaluatedInputs.Count - 2; i >= 0; i--)
                {
                    if (evaluatedInputs[i] is decimal b) m = (decimal)Math.Pow((double)b, (double)m);
                    else throw new NotImplementedException();
                }
                return m;
            }
            throw new NotImplementedException();
        }
        

        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }


    internal sealed class Negation : Operator
    {
        public Negation(DataContext context) : base(context) { }
        internal Negation(DataContext context, object item) : base(context, new object[1] { item }) { }


        protected override int ParsingPriority => PRIORITY_NOT;


        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Next == null) throw new LexingException("Inverse operator must be followed by the argument being inverted.");

            object item = node.Next.Remove();
            switch (item)
            {
                case decimal m: node.Contents = -m; break;
                case bool b: node.Contents = !b; break;
                default: Inputs = new object[] { item }; break;
            }
        }


        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is decimal m) return -m;
            if (evaluatedInputs[0] is bool b) return !b;
            throw new NotImplementedException();
        }

        protected override string Symbol
        {
            get
            {
                object v = GetValue(Inputs[0]);
                if (v is bool) return "!";
                return "-";
            }
        }

        public override string ToString()
        {
            if (Inputs.Length == 1) return "-" + Inputs[0];
            return base.ToString();
        }


        protected internal override object FromSimplified(DataContext context, IList<object> simplifiedInputs)
        {
            if (simplifiedInputs.Count != 1) throw new InvalidOperationException("Sanity check.");
            return InvertSimplified(context, simplifiedInputs[0]);
        }

        /// <summary>Returns the inverse of the object given.  For use with simplification.  Does not make an promises about whether an 
        /// original reference is returned.</summary>
        internal static object InvertSimplified(DataContext context, object obj)
        {
            switch (obj)
            {
                case Plus plus:
                    return new Plus(context, plus.Inputs.Select(p_input => InvertSimplified(context, p_input)));
                case Minus minus:
                    List<object> newInputs = new List<object>();
                    newInputs.Add(InvertSimplified(context, minus.Inputs[0]));
                    newInputs.AddRange(minus.Inputs.Skip(1));
                    return new Plus(context, newInputs);
                case Negation inv:
                    return inv.Inputs[0];
                case decimal m:
                    return -m;
                case bool b:
                    return !b;
            }
            return new Negation(context, obj);
        }
        

        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Minus : Operator
    {
        public Minus(DataContext context, params object[] inputs) : base(context, inputs) { }

        protected override string Symbol => "-";

        protected override int ParsingPriority => PRIORITY_MINUS;


        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is decimal a)
            {
                for (int i = 1; i < evaluatedInputs.Count; i++)
                {
                    if (evaluatedInputs[i] is decimal b) a -= b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }

        protected internal override object FromSimplified(DataContext context, IList<object> simplifiedInputs)
        {

            //The objective is to get it to a summation of negations.  The first item is treated differently from 
            //the items that follow:  7 - 3 means the 7 is positive, while the 3 is negative, ie, 7 + -3

            List<object> plusInputs = new List<object>();
            AddLinearly(simplifiedInputs[0]);            
            for (int i = 1; i < simplifiedInputs.Count;i++)
                AddLinearly(Negation.InvertSimplified(context, simplifiedInputs[i]));
            return new Plus(context, plusInputs);

            void AddLinearly(object other)
            {
                
                switch (other)
                {
                    case Plus p:
                        plusInputs.AddRange(p.Inputs);
                        break;
                    case Minus m:
                        plusInputs.Add(m.Inputs[0]);
                        plusInputs.AddRange(m.Inputs.Skip(1).Select(s => Negation.InvertSimplified(context, s)));
                        break;
                    default:
                        plusInputs.Add(other);
                        break;
                }
            }
            
        }


        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Nor : Operator
    {
        public Nor(DataContext context) : base(context) { }
        internal Nor(DataContext context, IEnumerable<object> inputs) : base(context) { }

        protected override int ParsingPriority => PRIORITY_PIPE;

        protected override string Symbol => "nor";


        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is bool a)
            {
                for (int i = 0; i < evaluatedInputs.Count; i++)
                {
                    if (evaluatedInputs[i] is bool b) a = !(a | b);
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }

        
        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }


    internal sealed class Percent : Operator
    {
        public Percent(DataContext context) : base(context) { }


        protected override string Symbol => "%";

        protected override int ParsingPriority => throw new NotImplementedException();


        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is decimal a && evaluatedInputs[1] is decimal b) return (decimal)((int)a % (int)b);
            throw new NotImplementedException();
        }
        
        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Pipe : Operator
    {
        public Pipe(DataContext context) : base(context) { }
        internal Pipe(DataContext context, IEnumerable<object> inputs) : base(context) { }

        protected override int ParsingPriority => PRIORITY_PIPE;

        protected override string Symbol => "|";


        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is bool a)
            {
                for (int i = 1; i < evaluatedInputs.Count; i++)
                {
                    if (evaluatedInputs[i] is bool b) a |= b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }

        protected internal override object FromSimplified(DataContext context, IList<object> simplifiedInputs)
        {
            List<object> newInputs = new List<object>();
            foreach(object input in Inputs)
            {
                if (input is Plus p) newInputs.AddRange(p.Inputs);
                else newInputs.Add(input);
            }
            return new Plus(context, newInputs);
        }


        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }

    internal sealed class Plus : Operator
    {

        public Plus(DataContext context) : base(context) { }
        internal Plus(DataContext context, params object[] inputs) : base(context, inputs) { }
        internal Plus(DataContext context, IEnumerable<object> inputs) : base(context, inputs) { }




        protected override int ParsingPriority => PRIORITY_PLUS;

        protected override string Symbol => "+";

        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is decimal a)
            {
                for (int i = 1; i < evaluatedInputs.Count; i++)
                {
                    if (evaluatedInputs[i] is decimal b) a += b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }

        protected override bool IsIdenticalTo(Formula other) => (other is Plus) ? ArePermutations(this.Inputs, other.Inputs) : false;
        
        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }




    internal sealed class Question : Operator
    {
        public Question(DataContext context) : base(context) { }

        protected override string Symbol => "?";


        protected override int ParsingPriority => PRIORITY_COLON;

        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is bool b) return b ? evaluatedInputs[1] : evaluatedInputs[2];
            throw new NotImplementedException();
        }

        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            //base.Parse(node);

            //if (Inputs[1] is Colon c && Inputs.Count == 2)
            //{
            //    if (c.Inputs.Count != 2) throw new LexingException("Colon can have only two inputs.");
            //    Inputs[1] = c.Inputs[0];
            //    Inputs.Add(c.Inputs[1]);
            //}
            throw new NotImplementedException();
        }
        

        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Slash : Operator
    {

        public Slash(DataContext context, params object[] inputs) : base(context, inputs) { }

        protected override string Symbol => "/";

        protected override int ParsingPriority => PRIORITY_DIVIDE;

        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is decimal a)
            {
                for (int i = 1; i < evaluatedInputs.Count; i++)
                {
                    if (evaluatedInputs[i] is decimal b) a /= b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }
        

        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }



    internal sealed class Star : Operator
    {

        public Star(DataContext context) : base(context) { }
        internal Star(DataContext context, params object[] inputs) : base(context, inputs) { }

        protected override string Symbol => "*";

        protected override int ParsingPriority => PRIORITY_STAR;

        protected override object Evaluate(IList<object> evaluatedInputs)
        {
            if (evaluatedInputs[0] is decimal a)
            {
                for (int i = 1; i < evaluatedInputs.Count; i++)
                {
                    if (evaluatedInputs[i] is decimal b) a *= b;

                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            if (Inputs.Length == 2 && Inputs[0] is decimal m)
            {
                if (Inputs[1] is Formula f) return m.ToString() + f.ToString();
                if (Inputs[1] is Variable v) return m.ToString() + v.ToString();
            }

            return base.ToString();
        }


        protected override bool IsIdenticalTo(Formula other) => (other is Star) ? ArePermutations(this.Inputs, other.Inputs) : false;
        
        protected override object GetDerivativeRecursive(Variable v) =>
            throw new InvalidOperationException("No derivation available for operator type '" + Symbol + "'.");
    }





}
