using DataStructures;
using Mathematics.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Parsing.Operators
{
    internal abstract class Operator : Formula
    {

        //protected Operator(DataContext context) : base(context) { }
        protected Operator(DataContext context, params object[] inputs) : base(context) { Inputs = inputs.ToList(); }


        protected abstract string Symbol { get; }


        /// <summary>Parses the operator given the context of its node within a token list.  Default parsing is to use combine the 
        /// preceding and following nodes' contents as Inputs and remove those nodes.  If adjacent nodes are missing, and exception is 
        /// thrown.</summary>
        /// <param name="node">The token list node containing the operator.</param>
        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Previous == null) throw new LexingException("Operator type " + Symbol + " requires preceding input.");
            if (node.Next == null) throw new LexingException("Operator type " + Symbol + " requires following input.");
            Inputs.Add(node.Previous.Contents);
            Inputs.Add(node.Next.Contents);            

            CombineVariables(node.Previous.Contents);
            CombineVariables(node.Next.Contents);

            node.Previous.Remove();
            node.Next.Remove();
            
        }

        


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
            if (Inputs.Count > 1) return string.Join(" " + Symbol + " ", Inputs);
            if (Inputs.Count == 1) return Inputs[0] + Symbol + "_";
            return Symbol;            
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
        protected internal override int ParsingPriority => PRIORITY_AMPERSAND;
        
    }


    internal sealed class Colon : Operator
    {
        public Colon(DataContext context) : base(context) { }

        protected override string Symbol => ":";

        protected internal override int ParsingPriority => PRIORITY_COLON;

        
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

        protected internal override int ParsingPriority => PRIORITY_DOT;

        
    }



    internal sealed class Hat : Operator
    {
        public Hat(DataContext context) : base(context) { }

        protected override string Symbol => "^";

        protected internal override int ParsingPriority => PRIORITY_HAT;

        
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


        protected internal override int ParsingPriority => PRIORITY_NOT;


        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Next == null) throw new LexingException("Inverse operator must be followed by the argument being inverted.");            
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
        public Minus(DataContext context, params object[] inputs) : base(context, inputs) { }

        protected override string Symbol => "-";

        protected internal override int ParsingPriority => PRIORITY_MINUS;

        
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

        protected internal override int ParsingPriority => PRIORITY_PIPE;

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

        protected internal override int ParsingPriority => throw new NotImplementedException();

        
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

        protected internal override int ParsingPriority => PRIORITY_PIPE;

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


        protected internal override int ParsingPriority => PRIORITY_PLUS;

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


        protected internal override int ParsingPriority => PRIORITY_COLON;
        
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



    internal sealed class Slash : Operator, IDifferentiable<object, object>
    {

        public Slash(DataContext context, params object[] inputs) : base(context, inputs) { }

        protected override string Symbol => "/";

        protected internal override int ParsingPriority => PRIORITY_DIVIDE;
        
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
        
        object IDifferentiable<object, object>.Evaluate(object atValue) => Value;

        object IDifferentiable<object, object>.GetDerivative(IEnumerable<IVariable<object>> differentiatingVariables)
        {

            //object f, f_prime, g, g_prime;

            //if (Inputs[1] is IDifferentiable<object, object> gid)
            //{
            //    if (gid is Formula g_f) g = g_f.Copy();

            //    g = ((Formula)gid).Copy();
            //    g_prime = gid.GetDerivative(differentiatingVariables);
            //}
            //if (Inputs[0] is IDifferentiable<object, object> f)
            //{
            //    if (Inputs[1] is IDifferentiable<object, object> g)
            //    {
            //        g_copy = ((Formula)g).Copy();                    
            //        f_prime_g = new Operators.Star(Context, f.GetDerivative(differentiatingVariables), g_copy);
            //        f_g_prime = new Operators.Star(Context, ((Formula)f).Copy(), g.GetDerivative(differentiatingVariables));
            //        object numer = new Operators.Minus(Context, f_prime_g, f_g_prime);
            //    }
            //}
            throw new NotImplementedException();
        }

        IDifferentiable<object, object> IDifferentiable<object, object>.GetIntegral(object constant, IEnumerable<IVariable<object>> integratingVariables)
        {
            throw new NotImplementedException();
        }
    }


    internal sealed class Star : Operator
    {

        public Star(DataContext context) : base(context) { }
        internal Star(DataContext context, params object[] inputs) :base(context) { Inputs = inputs.ToList(); }

        protected override string Symbol => "*";

        protected internal override int ParsingPriority => PRIORITY_STAR;
        
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

        public override string ToString()
        {
            if (Inputs.Count == 2 && Inputs[0] is decimal m)
            {
                if (Inputs[1] is Formula f) return m.ToString() + f.ToString();
                if (Inputs[1] is Variable v) return m.ToString() + v.ToString();
            }
                
            return base.ToString();
        }

    }





}
