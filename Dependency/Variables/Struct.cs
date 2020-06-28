using Helpers;
using Mathematics.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    [Serializable]
    public abstract class VariableStruct<T> : Variable, IContext where T : struct
    {
        protected readonly IConverter<T> Converter;
        private readonly Dictionary<string, Variable> _Children = new Dictionary<string, Variable>();
        /// <summary>Whether or not the dependency value converts to a value CLR value.</summary>
        public bool IsValid { get; private set; }
        protected VariableStruct(IConverter<T> converter)
        {
            this.Converter = converter;
        }
        protected void RegisterChildVariable(string name, Variable v)
        {
            name = name.ToLower();
            _Children.Add(name, v);
            v.Parent = this;
        }
        private T _Native;
        public T Native
        {
            get => _Native;
            set => this.Contents = Converter.ConvertUp(value);
        }

        private bool _PassChildrensValueChangeUp = true;
        internal override bool CommitContents(IEvaluateable newContents)
        {
            bool changed = UpdateChildren(newContents);
            changed |= base.CommitContents(newContents);
            return changed;
        }

        private bool UpdateChildren(IEvaluateable newContents)
        {
            try
            {
                _PassChildrensValueChangeUp = false;
                return (Converter.TryConvertDown(newContents, out T newCLRValue)
                               && ApplyContents(newCLRValue));
            }
            finally { _PassChildrensValueChangeUp = true; }
        }
        protected abstract bool ApplyContents(T newCLRValue);

        internal override bool OnChildUpdated(Update caller, ISyncUpdater updatedChild)
        {
            IEvaluateable evaluated = ComposeValue();
            bool changed = false;
            if (updatedChild.Equals(this.Contents))
            {
                // The update came from the contents of this variable. Overwrite the contents of 
                // the children.
                changed |= UpdateChildren(evaluated);
            }
            {
                // The update came from the child of this variable.  Overwrite the contents of 
                // this host struct with a new converted value (but only if we're not in the 
                // middle of an overwriting child update).
                if (!_PassChildrensValueChangeUp) return false;
                changed |= base.CommitContents(evaluated);
            }
            if (!Converter.TryConvertDown(evaluated, out T newCLR))
                throw new InvalidCastException("Failed to convert " + evaluated.ToString() + " to native.");
            else
                _Native = newCLR;
            changed |= CommitValue(evaluated);
            return changed;
        }

        protected abstract IEvaluateable ComposeValue();
        internal override IEvaluateable Evaluate() => ComposeValue();

        bool IContext.TryGetProperty(string path, out IEvaluateable property)
            => TryGetProperty(path, out property);
        protected abstract bool TryGetProperty(string path, out IEvaluateable property);
        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = default; return false; }
    }

    public sealed class Struct<T> : VariableStruct<T> where T : struct
    {
        private readonly Dictionary<PropertyInfo, Variable> _Variables;
        private readonly Dictionary<Variable, dynamic> _Converters;
        public Struct(IConverter<T> converter, T initial = default) : base(converter)
        {
            _Variables = new Dictionary<PropertyInfo, Variable>();
            _Converters = new Dictionary<Variable, dynamic>();
            foreach (PropertyInfo pinfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public){
                if (!pinfo.CanWrite || !pinfo.CanRead) continue;
                if (!pinfo.PropertyType.IsValueType) continue;
                dynamic clrValue = pinfo.GetValue(initial);
                dynamic sub_converter = Dependency.Values.Converter.GetDefaultFor(clrValue);
                Variable v = new Variable(sub_converter.ConvertUp(clrValue));
                _Converters[v] = sub_converter;
                _Variables[pinfo] = v;
            }
            Native = initial;
        }
        protected override bool ApplyContents(T newCLRValue)
        {
            bool applied = false;
            foreach (var kvp in _Variables)
            {
                PropertyInfo pinfo = kvp.Key;
                Variable v = kvp.Value;
                dynamic converter = _Converters[v];
                dynamic newSubCLRValue = pinfo.GetValue(newCLRValue);
                v.Contents = converter.ConvertUp(newSubCLRValue);
                applied = true;
            }
            return applied;
        }

        protected override IEvaluateable ComposeValue()
        {
            // Build a native struct.  Any sub-value that can't be reduced from a sub-property's 
            // current value should default to the current Native's sub-value.
            T asNative = new T();
            foreach (var kvp in _Variables)
            {
                PropertyInfo pinfo = kvp.Key;
                Variable v = kvp.Value;
                dynamic converter = _Converters[v];

                if (!converter.TryConvertDown(v.Value, out dynamic nativeSubVal))
                    nativeSubVal = pinfo.GetValue(Native);
                pinfo.SetValue(asNative, nativeSubVal);
            }

            // Now convert the struct to a dependency value.
            return Converter.ConvertUp(asNative);
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
