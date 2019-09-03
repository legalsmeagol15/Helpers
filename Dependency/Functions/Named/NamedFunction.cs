using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{

    public abstract class NamedFunction : Function, IExpression, INamed
    {
        private string _CachedNamed;
        public virtual string Name { get => _CachedNamed; }

        IEvaluateable IExpression.Contents => new Vector(this.Inputs.ToArray());

        protected NamedFunction() { _CachedNamed = this.GetType().Name.ToUpper(); }
        
    }

}
