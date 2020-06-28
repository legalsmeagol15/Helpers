using Helpers;
using Mathematics.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{

    public sealed class Contextualizer<T> : IContextualizer<T>
    {

        private static readonly IContextualizer<T> _Default;
        static Contextualizer()
        {
            if (typeof(VectorN).Equals(typeof(T)))
                _Default = (IContextualizer<T>)(IContextualizer<VectorN>)(new ContextualizerVectorN());
            else
                _Default = (IContextualizer<T>)(new Contextualizer<T>());

        }
        public static IContextualizer<T> Default => _Default;

        bool IContextualizer<T>.ApplyContents(T newCLR)
        {
            throw new NotImplementedException();
        }

        IEvaluateable IContextualizer<T>.ComposeValue()
        {
            throw new NotImplementedException();
        }

        IEvaluateable IContextualizer<T>.ConvertUp(T obj)
        {
            throw new NotImplementedException();
        }

        bool IContextualizer<T>.TryConvertDown(IEvaluateable ie, out T target)
        {
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


    internal sealed class ContextualizerVectorN : IContextualizer<VectorN>
    {
        private readonly Variable<Number> _XVar, _YVar;
        public ContextualizerVectorN(Number x = default, Number y = default)
        {
            this._XVar = new Variable<Number>(x);
            this._YVar = new Variable<Number>(y);
            _XVar.Updated += X_or_Y_Updated;
            _YVar.Updated += X_or_Y_Updated;
        }

        private void X_or_Y_Updated(object sender, ValueChangedArgs<IEvaluateable> e)
        {
            throw new NotImplementedException();
        }

        bool IContextualizer<VectorN>.ApplyContents(VectorN newCLR)
        {
            if (_XVar.Contents.Equals(newCLR.X) && _YVar.Contents.Equals(newCLR.Y))
                return false;
            _XVar.Contents = newCLR.X;
            _YVar.Contents = newCLR.Y;
            return true;
        }

        IEvaluateable IContextualizer<VectorN>.ComposeValue()
            => new Dependency.Vector((IEvaluateable)_XVar.Get(), (IEvaluateable)_YVar.Get());

        IEvaluateable IContextualizer<VectorN>.ConvertUp(VectorN obj)
            => new Dependency.Vector((IEvaluateable)obj.X, (IEvaluateable)obj.Y);

        bool IContextualizer<VectorN>.TryConvertDown(IEvaluateable ie, out VectorN target)
        {
            if (ie is Dependency.Vector v && v.Size == 2 && v[0].Value is Number x && v[1].Value is Number y)
            {
                target = new Mathematics.Geometry.VectorN(x, y);
                return true;
            }
            target = default;
            return false;
        }

        bool IContext.TryGetProperty(string path, out IEvaluateable property)
        {
            path = path.ToLower();
            if (path == "x" ) { property = _XVar; return true; }
            if (path == "y") { property = _YVar; return true; }
            property = default;
            return false;
        }

        bool IContext.TryGetSubcontext(string path, out IContext ctxt)
        {
            ctxt = default;
            return false;
        }
    }
}
