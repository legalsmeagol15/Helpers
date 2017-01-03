using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Satisfiability
{

    public abstract class Constraint<TDomain>
    {
        protected internal object[] Tags;
        
        internal abstract IEnumerable<Variable<TDomain>> Update(Hypothesis<TDomain> hypothesis);

        
    }


    public class UnaryConstraint<TDomain> : Constraint<TDomain>
    {
        private readonly Func<IList<TDomain>, bool> _Updater;

        /// <summary>
        /// Creates a new unary constraint with the given variable tag and updating function.
        /// </summary>
        /// <param name="tag">The tag for variable A affected by this binary constraint.</param>        
        /// <param name="updater">The updating function to use for updating the domain labels.  When this method is executed, the input is 
        /// the variable associated with the given tag.  The method should return a single bool indicating whether the variable's domain 
        /// label was actually changed.</param>
        public UnaryConstraint(object tag, Func<IList<TDomain>, bool> updater)
        {
            Tags = new object[1] { tag };
            _Updater = updater;
        }

        internal override IEnumerable<Variable<TDomain>> Update(Hypothesis<TDomain> hypothesis)
        {
            Variable<TDomain> var = hypothesis.GetVariable(Tags[0]);
            if (_Updater(var.Domains)) return new Variable<TDomain>[1] { var };
            return null;
        }
    }



    public class BinaryConstraint<TDomain> : Constraint<TDomain>
    {
        private readonly Func<IList<TDomain>, IList<TDomain>, bool[]> _Updater;

        /// <summary>
        /// Creates a new binary constraint with the given variable tags and updating function.
        /// </summary>
        /// <param name="tagA">The tag for variable A affected by this binary constraint.</param>
        /// <param name="tagB">The tag for variable B affected by this binary constraint.</param>
        /// <param name="updater">The updating function to use for updating domain labels.  When this method is executed, the inputs 
        /// are the variable associated with tagA, and the variable with tagB.  The method should return a matrix of bools indicating 
        /// which input was changed (for example, a true at position 0 indicating A was changed, and a false at position 1 indicating B was 
        /// not changed).
        /// </param>
        public BinaryConstraint(object tagA, object tagB, Func<IList<TDomain>, IList<TDomain>, bool[]> updater)
        {
            Tags = new object[2] { tagA, tagB };
            _Updater = updater;
        }

        internal override IEnumerable<Variable<TDomain>> Update(Hypothesis<TDomain> hypothesis)
        {
            //Find the variables.
            Variable<TDomain> varA = hypothesis.GetVariable(Tags[0]), varB = hypothesis.GetVariable(Tags[1]);

            //Do the update.
            bool[] updated = _Updater(varA.Domains, varB.Domains);
            if (updated.Length != 2)
                throw new ArgumentException("Invalid result of update function.  Binary constraint updates must produce results of size 2.");

            //Return all the changed results.
            List<Variable<TDomain>> result = new List<Variable<TDomain>>(2);
            if (updated[0]) result.Add(varA);
            if (updated[1]) result.Add(varB);                 
            return result;
        }
    }


    public class TernaryConstraint<TDomain> : Constraint<TDomain>
    {
        private readonly Func<IList<TDomain>, IList<TDomain>, IList<TDomain>, bool[]> _Updater;

        /// <summary>
        /// Creates a new ternary constraint with the given variable tags and updating function.
        /// </summary>
        /// <param name="tagA">The tag for variable A affected by this ternary constraint.</param>
        /// <param name="tagB">The tag for variable B affected by this ternary constraint.</param>
        /// <param name="tagC">The tag for variable C affected by this ternary constraint.</param>
        /// <param name="updater">The updating function to use for updating domain labels.  When this method is executed, the inputs 
        /// are the variable associated with tagA, and the variable with tagB.  The method should return a matrix of bools indicating 
        /// which input was changed (for example, a true at position 0 indicating A was changed, a false at position 1 indicating B was 
        /// not changed, and a false at position 2 indicating B was not changed).
        public TernaryConstraint(object tagA, object tagB, object tagC, Func<IList<TDomain>, IList<TDomain>, IList<TDomain>, bool[]> updater)
        {
            Tags = new object[3] { tagA, tagB, tagC };
            _Updater = updater;
        }

        internal override IEnumerable<Variable<TDomain>> Update(Hypothesis<TDomain> hypothesis)
        {
            //Find the variables.
            Variable<TDomain> varA = hypothesis.GetVariable(Tags[0]), varB = hypothesis.GetVariable(Tags[1]), varC = hypothesis.GetVariable(Tags[2]);

            //Do the update.
            bool[] updated = _Updater(varA.Domains, varB.Domains, varC.Domains);
            if (updated.Length != 3)
                throw new ArgumentException("Invalid result of update function.  Ternary constraint updates must produce results of size 3.");

            //Return all the changed results.
            List<Variable<TDomain>> result = new List<Variable<TDomain>>(3);
            if (updated[0]) result.Add(varA);
            if (updated[1]) result.Add(varB);
            if (updated[2]) result.Add(varC);             
            return result;
        }
    }


    /// <summary>
    /// A 
    /// </summary>
    /// <typeparam name="TDomain"></typeparam>
    public class GlobalConstraint<TDomain> : Constraint<TDomain>
    {
        private readonly Func<IList<TDomain>[], bool[]> _Updater;

        /// <summary>
        /// Creates a new global constraint with the given variable tags and updating function.
        /// </summary>        
        /// <param name="updater">The updating function to use for updating domain labels.  When this method is executed, the inputs 
        /// are the variables associated with the tags in the enumerated order.  The method should return a matrix of bools indicating 
        /// which input was changed (for example, a true at position 0 indicating the first was changed, a false at position 1 was not 
        /// changed, etc.)
        public GlobalConstraint(IEnumerable<object> tags, Func<IList<TDomain>[], bool[]> updater)
        {
            Tags = tags.ToArray();            
            _Updater = updater;
        }

        internal override IEnumerable<Variable<TDomain>> Update(Hypothesis<TDomain> hypothesis)
        {
            //Find the variables and their domains.
            Variable<TDomain>[] vars = new Variable<TDomain>[Tags.Length];
            IList<TDomain>[] domains = new IList<TDomain>[vars.Length];
            for (int i = 0; i < vars.Length; i++)
            {
                Variable<TDomain> v = hypothesis.GetVariable(Tags[i]);
                vars[i] = v;
                domains[i] = v.Domains;                
            }

            //Do the update.
            bool[] updated = _Updater(domains);
            if (updated.Length != vars.Length)
                throw new ArgumentException("Invalid result of update function.  Global constraint updates must produce results of the same size as inputs.");

            //Return all the changed results.
            List<Variable<TDomain>> result = new List<Variable<TDomain>>(vars.Length);
            for (int i = 0; i < vars.Length; i++)
            {
                if (updated[i]) result.Add(vars[i]);
            }            
            return result;
        }
    }
}
