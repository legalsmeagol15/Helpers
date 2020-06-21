using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    [Serializable]
    public class Struct<T> : Variable, IContext where T : struct
    {
        protected readonly IConverter<T> _Converter;
        private readonly IContextualizer<T> _Contextualizer;
        private T _CLRValue;
        public bool IsValid { get; private set; }

        public Struct(IContextualizer<T> contextualizer = null, IConverter<T> converter = null, T initial = default)
        {
            if (converter == null)
            {
                converter = Dependency.Values.Converter<T>.Default;
                if (converter is null || converter is Dependency.Values.Converter<T>)
                    throw new ArgumentException("Specialized converted required for " + nameof(Struct<T>) + ".");
            }
            this._Converter = converter;
            IEvaluateable iev = converter.ConvertFrom(initial);
            this.Contents = iev;
            this._Contextualizer = contextualizer ?? new AutoContextualizer(this);
        }

        internal override bool CommitValue(IEvaluateable newValue)
        {
            if (_Converter.TryConvertTo(newValue, out T newCLR))
            {
                if (!newCLR.Equals(_CLRValue))
                    _Contextualizer.Apply(this, newCLR);
                IsValid = true;
            }
            else
            {
                IsValid = false;
            }
            return base.CommitValue(newValue);
        }


        public void Set(T clrValue)
        {
            this.Contents = _Converter.ConvertFrom(clrValue);
        }

        public T Get()
        {
            return _CLRValue;
        }

        bool IContext.TryGetSubcontext(string path, out IContext ctxt)
            => _Contextualizer.TryGetSubcontext(path, out ctxt);

        bool IContext.TryGetProperty(string path, out IEvaluateable property)
            => _Contextualizer.TryGetProperty(path, out property);

        private class AutoContextualizer : IContextualizer<T>
        {
            private readonly Struct<T> _Host;
            private Dictionary<PropertyInfo, Variable> _Subproperties;
            private Dictionary<Variable, dynamic> _Converters;

            public AutoContextualizer(Struct<T> host)
            {
                this._Host = host;
                this._Subproperties = new Dictionary<PropertyInfo, Variable>();
                foreach (PropertyInfo pinfo in typeof(T).GetProperties())
                {
                    string name = pinfo.Name.ToLower();
                    if (!pinfo.PropertyType.IsValueType)
                        throw new ArgumentException("On type " + typeof(T).Name + ", property '" + name + "' must be value type.");
                    if (_Subproperties.ContainsKey(pinfo))
                        throw new ArgumentException("Duplicate sub-property '" + name + "' on type " + typeof(T).Name);
                    dynamic sub_value = pinfo.GetValue(_Host._CLRValue);
                    dynamic sub_converter = Values.Converter.GetDefaultFor(sub_value, true);
                    if (sub_converter == null) 
                        throw new ArgumentException("Sub-property '" + name + "' of type " + pinfo.PropertyType.Name + " on type " + typeof(T).Name + " has no defined default converter.");
                    Variable v = new Variable(sub_converter.ConvertFrom(sub_value))
                    {
                        Parent = host
                    };
                    _Subproperties[pinfo] = v;
                }
            }

            void IContextualizer<T>.Apply(Variable v, T newCLR)
            {
                foreach (var kvp in _Subproperties)
                {
                    PropertyInfo pinfo = kvp.Key;
                    Variable sub_v = kvp.Value;
                    dynamic converter = Values.Converter.GetDefaultFor(sub_v);

                    object newClrValue = pinfo.GetValue(newCLR);
                    IEvaluateable existingContents = sub_v.Contents;
                    IEvaluateable newContents = converter.ConvertFrom(newClrValue);
                    if (newContents.Equals(existingContents)) 
                        continue;

                    sub_v.Contents = newContents;
                }
                throw new NotImplementedException();
            }

            bool IContext.TryGetProperty(string path, out IEvaluateable property)
            {
                throw new NotImplementedException();
            }

            bool IContext.TryGetSubcontext(string path, out IContext ctxt)
            {
                throw new NotImplementedException();
            }
        }
    }
}
