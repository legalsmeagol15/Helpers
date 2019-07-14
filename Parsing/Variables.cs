using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using System.Runtime.CompilerServices;

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

        
        private ISet<Variable> _Terms;

        private IEvaluateable _Value;
        public IEvaluateable Value
        {
            get => _Value;
            private set
            {
                IEvaluateable oldValue = Value;
                Value = value;
                if (oldValue != Value)
                {
                    NotifyListeners();
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
                if (_Terms == null)
                    _Terms = Helpers.GetTerms(value);
                else
                {
                    ISet<Variable> newTerms = Helpers.GetTerms(value);
                    foreach (Variable oldTerm in _Terms.Except(newTerms))
                        RemoveListener(this);
                    foreach (Variable newTerm in newTerms.Except(_Terms))
                        AddListener(this);
                    this._Terms = newTerms;
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

        public IEnumerable<Variable> GetTerms()
        {
            throw new NotImplementedException();
        }

        private readonly ConditionalWeakTable<Variable, object> Listeners = new ConditionalWeakTable<Variable, object>();
       
        private bool RemoveListener(Variable listener)
        {
            
        }
        private bool AddListener(Variable listener)
        {

        }
    }



}
