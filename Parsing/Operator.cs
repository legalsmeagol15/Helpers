using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    internal abstract class Operator : Formula
    {

        protected Operator(DataContext context) : base(context) { }


        protected abstract string Symbol { get; }


        /// <summary>Parses the operator given the context of its node within a token list.  Default parsing is to use combine the 
        /// preceding and following nodes' contents as Inputs and remove those nodes.  If adjacent nodes are missing, and exception is 
        /// thrown.</summary>
        /// <param name="node">The token list node containing the operator.</param>
        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Previous == null) throw new LexingException("Operator type " + Symbol + " requires preceding input.");
            if (node.Next == null) throw new LexingException("Operator type " + Symbol + " requires following input.");
            Inputs = new List<object>() { node.Previous.Contents, node.Next.Contents };

            CombineVariables(node.Previous);
            CombineVariables(node.Next);

            node.Previous.Remove();
            node.Next.Remove();
            
        }


        //Ensures that trees of the same Operator are flattened.
        protected void Associate()
        {


            /*
             * Associativity means:
             * 
             *               &                                           &
             *              / \     => should be reinterpreted as:     / | \
             *             &   1                                      3  2  1
             *            / \
             *           3   2
             */

            int i = 0;
            while (i < Inputs.Count)
            {
                if (Inputs[i] is Formula f && f.GetType() == this.GetType())
                {
                    Inputs.RemoveAt(i);
                    Inputs.InsertRange(i, f.Inputs);
                }
                else i++;
            }
        }


        //internal abstract int ParsingPriority { get; }


        internal static bool TryLex(string token, IList<object> existingInputs, DataContext context, out Operator op)
        {
            switch (token)
            {
                case "+": op = new Plus(context); return true;
                case "-":
                    {   
                        if (existingInputs.Count==0 || existingInputs[existingInputs.Count-1] is Operator)
                            op = new Inverse(context);
                        else
                            op = new Minus(context);
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
                case "!":
                case "~": op = new Inverse(context); return true;

            }
            op = null;
            return false;
        }


        public override string ToString()
        {
            return string.Join(" " + Symbol + " ", Inputs);
        }

        
        

        #region Operator static operations

        /*The purpose of these methods is simply to evaluate, based on the argument types.*/


        /// <summary>Sums the given arguments.</summary>
        public static object Add(params object[] args)
        {
            if (args[0] is decimal m)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] is decimal addend) m += addend;
                    else throw new EvaluationException("Cannot add object of type " + args[i].GetType().Name + " to a number.");
                }
                return m;
            }
            throw new EvaluationException("Cannot add object of type " + args[0].GetType().Name + ".");
        }

        /// <summary>Takes the first argument, and subtracts all following arguments from it.</summary>
        public static object Subtract(params object[] args)
        {
            if (args[0] is decimal m)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] is decimal sub) m -= sub;
                    else throw new EvaluationException("Cannot subtract object of type " + args[i].GetType().Name + " from a number.");
                }
                return m;
            }
            throw new EvaluationException("Cannot subtract from object of type " + args[0].GetType().Name + ".");
        }

        /// <summary>Multiplies the given arguments.</summary>
        public static object Multiply(params object[] args)
        {
            if (args[0] is decimal m)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] is decimal multiplicand) m *= multiplicand;
                    else throw new EvaluationException("Cannot multiply object of type " + args[i].GetType().Name + " with a number.");
                }
                return m;
            }

            else throw new EvaluationException("Cannot multiply object of type " + args[0].GetType().Name + ".");
        }

        /// <summary>Takes the first argument, and divides it by all following arguments in turn.</summary>
        public static object Divide(params object[] args)
        {
            if (args[0] is decimal m)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] is decimal divisor)
                    {
                        if (divisor == 0m) return new EvaluationException("Division by zero.");
                        m /= divisor;
                    }
                    else throw new EvaluationException("Cannot a number with object of type " + args[i].GetType().Name + ".");
                }
                return m;
            }


            else throw new EvaluationException("Cannot multiply object of type " + args[0].GetType().Name + ".");
        }

        /// <summary>Returns the given base, taken to the given power.</summary>
        public static object Power(params object[] args)
        {

            /*
             * NOTE:  exponentiation is not associative.  (2^3)^4  !=  2^(3^4)
             * But, XOR is.  (0111 xor 1011) xor 1110   ==   0111 xor (1011 xor 1110)  ==   0010   ==   0010
             */

            if (args[0] is decimal @base)
            {
                if (args.Length != 2) throw new EvaluationException("Number exponentiation is not associative.  Only two numbers are expected.");
                if (args[1] is decimal exp) return (decimal)Math.Pow((double)@base, (double)exp);
                throw new EvaluationException("A number cannot be exponentiated with an object of type " + args[1].GetType().Name + ".");
            }
            if (args[0] is bool boolA)
            {
                if (args.Length < 2) throw new EvaluationException("For boolean XOR, more than 1 argument is expected.");
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] is bool b) boolA ^= b;
                    else throw new EvaluationException("Bool cannot be XOR'ed with object of type " + args[i].GetType().Name + ".");
                }
            }

            throw new EvaluationException("Exponentation cannot be performed on object of type " + args[0].GetType().Name + ".");
        }

        //public static object Invert(DataContext context, object obj)
        //{            
        //    if (obj is decimal m) return -m;
        //    if (obj is Inverse inv) return inv.Inputs[0];
        //    if (obj is bool b) return !b;
        //    return new Inverse(context, obj);
        //}




        #endregion

    }




    internal sealed class Ampersand : Operator
    {
        public Ampersand(DataContext context) : base(context) { }
        
        protected override object Evaluate(params object[] inputs) => string.Join("", inputs);

        protected override string Symbol => "&";
        internal override int ParsingPriority => PRIORITY_AMPERSAND;
    }



    internal sealed class Colon : Operator
    {
        public Colon(DataContext context) : base(context) { }

        protected override string Symbol => ":";

        internal override int ParsingPriority => PRIORITY_COLON;

        
        protected override object Evaluate(params object[] inputs) => throw new NotImplementedException();

        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            base.Parse(node);

            //Was a ternary being specified?
            if (Inputs[0] is Question q && q.Inputs.Count == 2)
            {
                q.Inputs.Add(Inputs[1]);
                node.Contents = q;
                q.CombineVariables(this);
            }
        }
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

        
        protected override object Evaluate(params object[] inputs) => inputs[inputs.Length-1];

        protected override string Symbol => ".";

        public override string ToString() => string.Join(Symbol, Inputs);

        internal override int ParsingPriority => PRIORITY_DOT;
    }



    internal sealed class Hat : Operator
    {
        public Hat(DataContext context) : base(context) { }

        protected override string Symbol => "^";

        internal override int ParsingPriority => PRIORITY_HAT;

        
        protected override object Evaluate(params object[] inputs)
        {
            if (inputs[inputs.Length - 1] is decimal m)
            {
                for (int i = inputs.Length - 2; i>=0; i--)
                {
                    if (inputs[i] is decimal b) m = (decimal)Math.Pow((double)b, (double)m);
                    else throw new NotImplementedException();
                }
                return m;
            }            
            throw new NotImplementedException();
        }

    }


    internal sealed class Inverse : Operator
    {
        public Inverse(DataContext context) : base(context) { }
        internal Inverse(DataContext context, object item) : base(context)
        {
            Inputs = new List<object>() { item };
            if (item is Formula f) CombineVariables(f);
        }


        internal override int ParsingPriority => PRIORITY_NOT;


        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Next == null) throw new LexingException("Inverse operator must be followed by the argument being inverted.");
            if (Inputs != null) throw new LexingException("Sanity check.");
            Inputs = new List<object>() { node.Next.Remove() };
            if (Inputs[0] is decimal m)
                node.Contents = -m;
            else if (Inputs[0] is bool b)
                node.Contents = !b;
            else
                CombineVariables(Inputs[0]);
        }


        protected override object Evaluate(params object[] inputs)
        {
            if (inputs[0] is decimal m) return -m;
            if (inputs[0] is bool b) return !b;
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
            if (Inputs.Count == 1) return "-" + Inputs[0];
            return base.ToString();
        }

    }



    internal sealed class Minus : Operator
    {
        public Minus(DataContext context) : base(context) { }

        protected override string Symbol => "-";

        internal override int ParsingPriority => PRIORITY_MINUS;

        
        protected override object Evaluate(params object[] inputs)
        {
            if (inputs[0] is decimal a)
            {
                for (int i = 1; i < inputs.Length; i++)
                {
                    if (inputs[i] is decimal b) a -= b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }
        

    }



    internal sealed class Nor : Operator
    {
        public Nor(DataContext context) : base(context) { }
        internal Nor(DataContext context, IEnumerable<object> inputs) : base(context) { }

        internal override int ParsingPriority => PRIORITY_PIPE;

        protected override string Symbol => "nor";

        
        protected override object Evaluate(params object[] inputs)
        {
            if (inputs[0] is bool a)
            {
                for (int  i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i] is bool b) a = !(a | b);
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }
        
    }


    internal sealed class Percent : Operator
    {
        public Percent(DataContext context) : base(context) { }


        protected override string Symbol => "%";

        internal override int ParsingPriority => throw new NotImplementedException();

        
        protected override object Evaluate(object[] inputs)
        {
            if (inputs[0] is decimal a && inputs[1] is decimal b) return (decimal)((int)a % (int)b);
            throw new NotImplementedException();
        }

    }



    internal sealed class Pipe : Operator
    {
        public Pipe(DataContext context) : base(context) { }
        internal Pipe(DataContext context, IEnumerable<object> inputs) : base(context) { }

        internal override int ParsingPriority => PRIORITY_PIPE;

        protected override string Symbol => "|";

        
        protected override object Evaluate(object[] inputs)
        {
            if (inputs[0] is bool a)
            {
                for (int i = 1; i < inputs.Length; i++)
                {
                    if (inputs[i] is bool b) a |= b;
                    else throw new NotImplementedException();
                }
                return a;
            }            
            throw new NotImplementedException();
        }
        
    }

    internal sealed class Plus : Operator
    {

        public Plus(DataContext context) : base(context) { }


        internal override int ParsingPriority => PRIORITY_PLUS;

        protected override string Symbol => "+";
        
        protected override object Evaluate(object[] inputs)
        {
            if (inputs[0] is decimal a)
            {
                for (int i = 1; i < inputs.Length; i++)
                {
                    if (inputs[i] is decimal b) a += b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }
        
    }

    internal sealed class Question : Operator
    {
        public Question(DataContext context) : base(context) { }

        protected override string Symbol => "?";


        internal override int ParsingPriority => PRIORITY_COLON;
        
        protected override object Evaluate(object[] inputs)
        {
            if (inputs[0] is bool b) return b ? inputs[1] : inputs[2];
            throw new NotImplementedException();
        }

        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            base.Parse(node);

            if (Inputs[1] is Colon c && Inputs.Count == 2)
            {
                if (c.Inputs.Count != 2) throw new LexingException("Colon can have only two inputs.");
                Inputs[1] = c.Inputs[0];
                Inputs.Add(c.Inputs[1]);
            }
        }

    }



    internal sealed class Slash : Operator
    {

        public Slash(DataContext context) : base(context) { }

        protected override string Symbol => "/";

        internal override int ParsingPriority => PRIORITY_DIVIDE;
        
        protected override object Evaluate(object[] inputs)
        {
            if (inputs[0] is decimal a)
            {
                for (int i =1; i < inputs.Length; i++)
                {
                    if (inputs[i] is decimal b) a /= b;
                    else throw new NotImplementedException();
                }
                return a;
            }            
            throw new NotImplementedException();
        }
        
    }


    internal sealed class Star : Operator
    {

        public Star(DataContext context) : base(context) { }

        protected override string Symbol => "*";

        internal override int ParsingPriority => PRIORITY_STAR;
        
        protected override object Evaluate(object[] inputs)
        {
            if (inputs[0] is decimal a)
            {
                for (int i = 1; i < inputs.Length; i++)
                {
                    if (inputs[i] is decimal b) a *= b;
                    else throw new NotImplementedException();
                }
                return a;
            }
            throw new NotImplementedException();
        }
        
    }





}
