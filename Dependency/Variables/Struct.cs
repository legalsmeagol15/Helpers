using DataStructures;
using Helpers;
using Mathematics.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    internal enum VariableMode
    {
        NONE, UPDATING_CONTENTS, UPDATING_VALUE, UPDATING_CHILD_VALUE
    }

    [Serializable]
    public abstract class VariableStruct<T> : IAsyncUpdater, ISyncUpdater, IUpdatedVariable, INotifyUpdates<IEvaluateable>, IContext, IVariable<T>
        where T : struct
    {
        
        internal VariableMode Mode { get; private set; } = VariableMode.NONE;
        private readonly object _Lock = new object();
        public bool IsValid { get; private set; } = true;

        protected readonly IConverter<T> Converter;
        protected VariableStruct(IConverter<T> converter)
        {
            this.Converter = converter;
        }
        protected void RegisterChildVariable(Variable v)
        {
            v.Parent = this;
        }
        private T _Native;
        public T Native
        {
            get => _Native;
            set => this.Contents = Converter.ConvertUp(value);
        }

        private IEvaluateable _Contents = Dependency.Null.Instance;
        public IEvaluateable Contents
        {
            get => _Contents;
            set
            {
                VariableMode priorMode = VariableMode.NONE;
                try
                {
                    Monitor.Enter(_Lock);
                    if ((priorMode = Mode) != VariableMode.NONE)
                        throw new InvalidOperationException("Cannot change contents while in state " + Mode.ToString());
                    Mode = VariableMode.UPDATING_CONTENTS;
                    if (Converter.TryConvertDown(value, out T newCLRValue))
                    {
                        ApplyContents(newCLRValue);
                        Update.ForVariable(this, value).Execute();
                    }
                    else
                        InvalidateContents(new ConversionError(value, typeof(T)));
                }
                catch (Exception e)
                {
                    InvalidateContents(new InvalidValueError("Unknown error applying new contents to sub-properties(" + e.ToString() + ")"));
                }
                finally { Mode = priorMode; Monitor.Exit(_Lock); }                
            }
        }
        private bool CommitContents(IEvaluateable newContents)
        {
            if (_Contents.Equals(newContents)) return false;
            _Contents = newContents;
            return true;
        }
        bool IUpdatedVariable.CommitContents(IEvaluateable newContents) => CommitContents(newContents);

        /// <summary>The given CLR value must be dissected and applied to the appropriate sub-
        /// properties' contents.</summary>        
        /// <returns>Returns true if successful, false if not.</returns>
        protected abstract bool ApplyContents(T newCLRValue);
        /// <summary>The given value must be applied to all sub-properties in the event of a 
        /// conversion failure.</summary>
        protected abstract void InvalidateContents(Error e);
        
        protected abstract IEvaluateable ComposeValue();

        private IEvaluateable _Value = Dependency.Null.Instance;
        public IEvaluateable Value
        {
            get => _Value;
            private set
            {
                IEvaluateable oldValue = _Value;
                _Value = value;
                Updated?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, value));
            }
        }
        public event ValueChangedHandler<IEvaluateable> Updated;

        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)        
        {
            VariableMode priorMode = VariableMode.NONE;
            try
            {
                Monitor.Enter(_Lock);
                priorMode = Mode;

                // The update comes from the contents itself?
                if (updatedChild.Equals(_Contents)) 
                    return CommitValue(_Contents.Value) ? Update.UniversalSet : null;

                // The update comes from a child after changing the Contents?
                else if (Mode == VariableMode.UPDATING_CONTENTS)
                    return null;

                // The update comes from a child?  Force Contents to align.
                Mode = VariableMode.UPDATING_CHILD_VALUE;
                IEvaluateable syncedContents = ComposeValue();
                CommitContents(syncedContents);
                return CommitValue(syncedContents) ? Update.UniversalSet : null;
            }
            // Let exceptions through.
            finally
            {
                Mode = priorMode; Monitor.Exit(_Lock);
            }
        }
        
        private bool CommitValue(IEvaluateable newValue)
        {
            try
            {
                Monitor.Enter(_Lock);
                bool changed = false;
                if (Converter.TryConvertDown(newValue, out T clr) && !clr.Equals(_Native))
                {
                    _Native = clr;
                    changed = true;
                    IsValid = true;
                }
                else
                    IsValid = false;
                if (!Value.Equals(newValue)) { Value = newValue; changed = true; }
                return changed;
            }
            finally { Monitor.Exit(_Lock); }

        }
        bool IUpdatedVariable.CommitValue(IEvaluateable newValue) => CommitValue(newValue);



        internal ISyncUpdater Parent { get; set; }
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = value; } }


        #region VariableStruct IContext members

        bool IContext.TryGetProperty(string path, out IEvaluateable property)
            => TryGetProperty(path.ToLower(), out property);
        protected abstract bool TryGetProperty(string path, out IEvaluateable property);
        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = default; return false; }

        #endregion


        #region VariableStruct IAsyncUpdate members (listeners)

        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();
        bool IAsyncUpdater.RemoveListener(ISyncUpdater listener) => Listeners.Remove(listener);

        bool IAsyncUpdater.AddListener(ISyncUpdater listener) => Listeners.Add(listener);

        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;

        #endregion

    }

    public sealed class VariableVectorN : VariableStruct<VectorN>
    {
        public readonly Variable X, Y;
        public VariableVectorN(VectorN initial = default) : this(initial.X, initial.Y) { }
        public VariableVectorN(IEvaluateable x, IEvaluateable y) : base(Values.Converter<VectorN>.Default)
        {
            X = new Variable(x) { Parent = this };
            Y = new Variable(y) { Parent = this };
        }
        protected override bool ApplyContents(VectorN newCLRValue)
        {
            
            X.Contents = newCLRValue.X;
            Y.Contents = newCLRValue.Y;
            return true;
        }

        protected override IEvaluateable ComposeValue() => new Dependency.Vector(X.Value, Y.Value);

        protected override void InvalidateContents(Error e)
        {
            X.Contents = e;
            Y.Contents = e;
        }

        protected override bool TryGetProperty(string path, out IEvaluateable property)
        {
            if (path.Equals("x") || path.Equals("X")) { property = X; return true; }
            if (path.Equals("y") || path.Equals("Y")) { property = Y; return true; }
            property = default;
            return false;
        }
    }

    /// <summary>
    /// Uses reflection to manage a CLR native struct.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Struct<T> : VariableStruct<T> where T : struct
    {
        private readonly Dictionary<PropertyInfo, Variable> _Variables;
        private readonly Dictionary<Variable, dynamic> _Converters;
        public Struct(IConverter<T> converter = null, T initial = default) : base(converter ?? Values.Converter<T>.Default)
        {
            _Variables = new Dictionary<PropertyInfo, Variable>();
            _Converters = new Dictionary<Variable, dynamic>();
            foreach (PropertyInfo pinfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!pinfo.CanWrite || !pinfo.CanRead) continue;
                if (!pinfo.PropertyType.IsValueType) continue;
                dynamic clrValue = pinfo.GetValue(initial);
                dynamic sub_converter = Dependency.Values.Converter.GetDefaultFor(clrValue);
                Variable v = new Variable(sub_converter.ConvertUp(clrValue)) { Parent = this };
                _Converters[v] = sub_converter;
                _Variables[pinfo] = v;
            }
            Native = initial;
        }
        protected override bool ApplyContents(T newCLRValue)
        {
            foreach (var kvp in _Variables)
            {
                PropertyInfo pinfo = kvp.Key;
                Variable v = kvp.Value;
                dynamic converter = _Converters[v];
                dynamic newSubCLRValue = pinfo.GetValue(newCLRValue);
                v.Contents = converter.ConvertUp(newSubCLRValue);
            }
            return _Variables.Any();
        }

        protected override IEvaluateable ComposeValue()
        {
            // Build a native struct.  Any sub-value that can't be reduced from a sub-property's 
            // current value should default to the current Native's sub-value.
            object boxedNative = new T();
            foreach (var kvp in _Variables)
            {
                PropertyInfo pinfo = kvp.Key;
                Variable v = kvp.Value;
                dynamic converter = _Converters[v];

                object nativeSubVal = converter.CanConvertDown(v.Value) ? converter.ConvertDown(v.Value) : pinfo.GetValue(Native);
               
                pinfo.SetValue(boxedNative, nativeSubVal);
            }

            // Now convert the struct to a dependency value.
            return Converter.ConvertUp((T)boxedNative);
        }

        protected override void InvalidateContents(Error e)
        {
            foreach (Variable v in _Variables.Values) v.Contents = e;
        }

        public IEvaluateable this[string name]
        {
            get
            {
                if (!TryGetProperty(name.ToLower(), out IEvaluateable result))
                    throw new ArgumentException("No binding property named " + name);
                return result;
            }
        }
        protected override bool TryGetProperty(string path, out IEvaluateable property)
        {
            path = path.ToLower();
            foreach (var kvp in _Variables)
            {
                string name = kvp.Key.Name.ToLower();
                if (name.Equals(path)) { property = kvp.Value; return true; }
            }
            property = default;
            return false;
        }
    }
}
