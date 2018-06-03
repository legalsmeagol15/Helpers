using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    [Serializable]
    /// <summary>
    /// A Context which allows for concurrent access to member variables and sub-contexts, as well as dynamic adding and removing of 
    /// member sub-contexts.
    /// <para/>It is expected that multiple threads will be accessing the context.  Changing or accessing the variable contents is 
    /// protected by mutexes.
    /// <para/>Instances of this class are serializable.
    /// </summary>
    public partial class DataContext : Context
    {

        // TODO:  Function Factory should be a member of a DataContext.
        
       
        public Variable this[string name] { get { lock (this) { return Variables[name]; } } }

        public DataContext() : base("Root") { }


        
        public override bool TryDelete(Variable v) { lock (this) return base.TryDelete(v); }

        
        public override bool TryGet(string name, out Variable v) { lock (this) return base.TryGet(name, out v); }

      
       
        public override bool TryAdd(string name, out Variable v) { lock (this) return base.TryAdd(name, out v); }


        
        public override bool TryGet(string name, out Context subContext) { lock (this) return base.TryGet(name, out subContext); }


    }


}
