using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{

    /// <summary>
    /// A function which takes a base indexable object (like a <seealso cref="Vector"/>, or a 
    /// <seealso cref="Reference"/> which points to a property <seealso cref="Variables.Variable"/>), and an ordinal 
    /// value 'n', and returns the 'nth' item associated with that base object.
    /// </summary>
    [NonVariadic(0, TypeFlags.Any, TypeFlags.Any)]
    internal sealed class Indexing : Function
    {
        private IIndexable _Base;
        
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            IIndexable b = evaluatedInputs[0] as IIndexable;
            if (b == null)
                return new IndexingError(this, evaluatedInputs[0], evaluatedInputs[1],
                                         "Value of type " + evaluatedInputs[0].GetType().Name + " is not indexable.");
            Variables.Update.StructureLock.EnterReadLock();

            try
            {
                return b[evaluatedInputs[1]];
            }
            catch
            {
                return new IndexingError(this, evaluatedInputs[0], evaluatedInputs[1],
                                         "Index " +evaluatedInputs[1].ToString()+" is not valid on object of type  " + evaluatedInputs[0].GetType().Name + ".");
            }finally { Variables.Update.StructureLock.ExitReadLock(); }

        }

        public override string ToString() => Inputs[0].ToString() + "[" + Inputs[1].ToString() + "]";
    }
}
