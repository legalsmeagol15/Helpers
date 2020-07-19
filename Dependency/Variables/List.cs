using DataStructures;
using Dependency.Functions;
using Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    //public sealed class List<T> : IVariable<IList<T>>, ISyncUpdater, IAsyncUpdater, INotifyUpdates<IEvaluateable>, IIndexedDynamic, IUpdatedVariable
    //{
    //    private IEvaluateable _Contents;
    //    private Dictionary<int, WeakReference< Variable<T>>> _Members;
    //    private readonly IConverter<T> _MemberConverter;

    //    public List(IConverter<T> converter = null, params IEvaluateable[] contents)
    //    {
    //        this._MemberConverter = converter ?? Dependency.Values.Converter<T>.Default;
    //        this.Contents = new Vector(contents);
    //    }

    //    public IEvaluateable Contents
    //    {
    //        get => _Contents;
    //        set
    //        {
    //            Update.ForVariable(this, value).Execute();
    //            if (value is Vector new_vec)
    //            {
    //                foreach (var kvp in _Members)
    //                {
    //                    if (!kvp.Value.TryGetTarget(out Variable<T> memberVar))
    //                        continue;
    //                    int idx = kvp.Key;
    //                    if (idx >= 0 && idx < new_vec.Count)
    //                        memberVar.Contents = new_vec[idx];
    //                    else if (!(memberVar.Contents is IndexingError))
    //                        memberVar.Contents = new IndexingError(this, null, "Invalid index " + idx.ToString());
    //                }
    //            }
    //        }
    //    }
        
    //    public IEvaluateable Value => _Contents.Value;

    //    private IList<T> _Native = null;
    //    public IList<T> Native
    //    {
            
    //    }
    //    IList<T> IVariable<IList<T>>.Native { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    bool IIndexable.ControlsReindex => true;

    //    void IIndexedDynamic.Reindex(IEnumerable<IEvaluateable> keys)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    void IIndexedDynamic.Reindex(int start, int end)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
