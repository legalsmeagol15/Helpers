using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Dependency
{
    [Serializable]
    public class Context
    {
        public readonly Context Parent;
        public readonly Dependency.Manager Manager;
        public readonly object Object;
        public readonly Function.Factory Functions;
        internal readonly ClassProfile ClassProfile;
        public readonly Dictionary<string, Context> StrongSubcontexts = new Dictionary<string, Context>();
        public readonly Dictionary<string, Variable> StrongVariables = new Dictionary<string, Variable>();
        public readonly Dictionary<string, WeakReference<Context>> WeakSubcontexts = new Dictionary<string, WeakReference<Context>>();
        public readonly Dictionary<string, WeakReference<Variable>> WeakVariables = new Dictionary<string, WeakReference<Variable>>();

        public static Context FromRoot()
        {
            return new Context(null, null, null, null);
        }
        public static Context FromRoot(Dependency.Manager manager, object root, Function.Factory functions = null)
        {

            if (manager.Roots.TryGetValue(root, out Context c)) return c;
            if (!manager.ClassProfiles.TryGetValue(root.GetType(), out ClassProfile cp))
                cp = ClassProfile.FromType(root.GetType());
            return new Context(null, cp, root, functions);            
        }
        
        
        internal Context(Context parent, ClassProfile classProfile, object obj, Function.Factory functions = null)
        {
            this.Parent = parent;
            this.ClassProfile = classProfile ?? throw new ArgumentNullException("Given " + typeof(ClassProfile).Name + " cannot be null.");
            this.Object = obj;
            if (parent == null)
            {                
                this.Functions = functions ?? Function.Factory.StandardFactory;
            }
            else
            {
                this.Functions = parent.Functions;
            }            
        }


        internal bool TryCreateFunction(string name, out Function f) => Functions.TryCreateFunction(name, out f);

        internal bool TryGetSubcontext(string name, out Context subcontext)
        {
            if (StrongSubcontexts.TryGetValue(name, out subcontext)) return true;
            else if (WeakSubcontexts.TryGetValue(name, out WeakReference<Context> wr) && wr.TryGetTarget(out subcontext)) return true;
            subcontext = null;
            return false;
        }

        internal bool TryGetVariable(string name, out Variable variable)
        {
            if (StrongVariables.TryGetValue(name, out variable)) return true;
            else if (WeakVariables.TryGetValue(name, out WeakReference<Variable> wr) && wr.TryGetTarget(out variable)) return true;
            variable = null;
            return false;
        }

        internal bool TryAddSubcontext(string name, out Context subcontext)
        {
            if (StrongSubcontexts.TryGetValue(name, out subcontext)) return false;
            if (WeakSubcontexts.TryGetValue(name, out WeakReference<Context> wr) && wr.TryGetTarget(out subcontext)) return false;

            if (ClassProfile != null)
            {
                if (!ClassProfile.ContextProfiles.TryGetValue(name, out ContextProfile ctxtProfile)) { subcontext = null; return false; }
                subcontext = ctxtProfile.Create(this);
                if (ctxtProfile.IsWeak)
                {
                    if (wr != null) wr.SetTarget(subcontext);
                    else WeakSubcontexts[name] = new WeakReference<Context>(subcontext);
                }
                else
                    StrongSubcontexts[name] = subcontext;
                return true;
            }
            
        }

        internal bool TryAddVariable(string name, out Variable variable)
        {
            if (StrongVariables.TryGetValue(name, out variable)) return false;
            if (WeakVariables.TryGetValue(name, out WeakReference<Variable> wr) && wr.TryGetTarget(out variable)) return false;

            // TODO:  what if the ClassProfile is null because it wasn't associated with any object?
            if (!ClassProfile.PropertyProfiles.TryGetValue(name, out PropertyProfile propProfile)) { variable = null; return false; }
            variable = propProfile.Create(this);
            if (propProfile.IsWeak)
            {
                if (wr != null) wr.SetTarget(variable);
                else WeakVariables[name] = new WeakReference<Variable>(variable);
            }
            else StrongVariables[name] = variable;
            return true;
        }

        public bool IsAncestorOf(Context other)
        {
            while (other != null)
            {
                if (ReferenceEquals(this, other)) return true;
                other = other.Parent;
            }
            return false;                
        }

        public Variable Declare(string path, string contents = "")
        {
            IEvaluateable iev = Expression.FromStringInternal(path, this, this.Functions);
            Reference r = iev as Reference;
            return r.Variable;            
        }

        

        public Variable GetVariable(string name)
        {
            throw new NotImplementedException();
        }

        public Context GetSubcontext(string name)
        {
            throw new NotImplementedException();
        }
    }
}
