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
    public class Struct<T> : Variable, IContext where T : struct
    {
        private readonly IContextualizer<T> _Contextualizer;
        private T _CLRValue;
        private IEvaluateable _CachedValue;


        /// <summary>
        /// Whether or not the dependency value converts to a value CLR value.
        /// </summary>
        public bool IsValid { get; private set; }

        public Struct(T initial = default, IContextualizer<T> contextualizer = null)
        {
            this._Contextualizer = contextualizer ?? Contextualizer<T>.Default;
            Set(initial);
        }

        public void Set(T clrValue)
        {
            this.Contents = _Contextualizer.ConvertUp(clrValue);
        }

        public T Get()
        {
            return _CLRValue;
        }

        bool _AllowUpdates = true;
        internal override bool CommitContents(IEvaluateable newContents)
        {
            bool contentsChanged = false;
            _AllowUpdates = false;
            contentsChanged |= _Contextualizer.TryConvertDown(newContents, out T newCLRValue) 
                                && _Contextualizer.ApplyContents(newCLRValue);
            contentsChanged |= base.CommitContents(newContents);
            _AllowUpdates = true;
            return contentsChanged;
        }

        internal override bool OnContentsUpdated(Update caller, ISyncUpdater updatedChild)
        {
            if (!_AllowUpdates) return false;
            IEvaluateable newValue = _Contextualizer.ComposeValue();
            return CommitValue(newValue);
        }


        bool IContext.TryGetSubcontext(string path, out IContext ctxt)
            => _Contextualizer.TryGetSubcontext(path, out ctxt);

        bool IContext.TryGetProperty(string path, out IEvaluateable property)
            => _Contextualizer.TryGetProperty(path, out property);

        public event ValueChangedHandler<T> Changed;


    }

}
