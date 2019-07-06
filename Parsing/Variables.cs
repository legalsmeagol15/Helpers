using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Helpers;

namespace Dependency
{
    public enum Mobility
    {
        // TODO:  more info will probably be needed
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }

    
    
    public sealed class Variable : IEvaluateable
    {
        public readonly IContext Context;

        public readonly string Name;

        public readonly List<WeakReference<Variable>> Listeners = new List<WeakReference<Variable>>();

        public IEvaluateable Value { get; private set; }
        private IEvaluateable _Contents;
        public IEvaluateable Contents { get => _Contents; set => SetContents(value); }

        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IContext context, string name, IEvaluateable contents = null) {
            this.Context = context;
            this.Name = name;
            this.Contents = contents ?? Dependency.Null.Instance;
        }

        internal void SetContents(IEvaluateable newContents)
        {
            _Contents = newContents;
            IEvaluateable oldValue = Value;
            Value = newContents.UpdateValue();
            if (oldValue != Value)
            {
                NotifyListeners();
                ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, Value));
            }
        }

        void NotifyListeners()
        {
            Task.Run(() => NotifyListenersAsync());
        }
        private void NotifyListenersAsync()
        {
            // TODO:  notify the dependency listeners.
        }

        public event ValueChangedHandler<IEvaluateable> ValueChanged;


        internal string GetExpressionString(IContext perspective)
        {
            throw new NotImplementedException();
        }

        public IEvaluateable UpdateValue() => Value = _Contents.UpdateValue();
    }



}
