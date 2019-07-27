using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using System.Runtime.CompilerServices;
using System.Collections;
using DataStructures;

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
        private ISet<Variable> _Sources;
        private WeakReferenceSet<Variable> _Listeners;
        private IEvaluateable _Value = Null.Instance;

        public IEvaluateable Value
        {
            get => _Value;
            private set
            {
                IEvaluateable oldValue = Value;
                _Value = value;
                if (oldValue != Value)
                {
                    // Notify listener variables.
                    NotifyListeners();

                    // Notify other subscribers.
                    ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, Value));
                }
            }
        }
        private IEvaluateable _Contents;
        public IEvaluateable Contents
        {
            get => _Contents;
            set
            {
                if (_Sources == null)
                    _Sources = Helpers.GetTerms(value);
                else
                {
                    ISet<Variable> newTerms = Helpers.GetTerms(value);
                    foreach (Variable source in _Sources)
                        if (!newTerms.Contains(source)) source._Listeners.Remove(this);
                    foreach (Variable source in newTerms)
                        source._Listeners.Add(this);
                    this._Sources = newTerms;
                }
                _Contents = value;
                Value = value.UpdateValue();
            }
        }
        

        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IContext context, string name, IEvaluateable contents = null) {
            this.Context = context;
            this.Name = name;
            this.Contents = contents ?? Dependency.Null.Instance;
        }
        
        private void NotifyListeners()
        {
            Task.Run(() => NotifyListenersAsync());
        }
        private void NotifyListenersAsync()
        {
            
        }

        public event ValueChangedHandler<IEvaluateable> ValueChanged;


        internal string GetExpressionString(IContext perspective)
        {
            throw new NotImplementedException();
        }
        
        public IEnumerable<Variable> GetTerms() => _Sources;

        IEvaluateable IEvaluateable.UpdateValue() => _Value = _Contents.Value;
    }



}
