using Parsing.Dependency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dependency
{
    
    [Serializable]
    public class Clause : IEvaluateable
    {
        internal string Opener;
        internal string Closer;
        public ISet<Variable> Terms { get; internal set; }

        public IEvaluateable[] Inputs { get; protected set; }

        internal Clause(string opener, string closer, params IEvaluateable[] inputs) { this.Opener = opener; this.Closer = closer; this.Inputs = inputs ?? new IEvaluateable[0]; }


        
        public static Clause Parenthetical(params IEvaluateable[] expressions) => new Clause("(", ")", expressions);
        public static Clause Bracketed(params IEvaluateable[] expressions) => new Clause("[", "]", expressions);
        public static Clause Braced(params IEvaluateable[] expressions) => new Clause("{", "}", expressions);
        public static Clause Naked(params IEvaluateable[] expressions) => new Clause("", "", expressions);
        

        public bool IsParenthetical => Opener == "(" && Closer == ")";
        public bool IsBracketed => Opener == "[" && Closer == "]";
        public bool IsBraced => Opener == "{" && Closer == "}";
        public virtual bool IsNaked => Opener == "" && Closer == "";

        /// <summary>Call to evaluate this function or clause.</summary>
        public virtual IEvaluateable Evaluate() => (Inputs.Length == 1) ? Inputs[0].Evaluate() : this;

        protected IEvaluateable[] GetEvaluatedInputs() => Inputs.Select(i => i.Evaluate() ?? Variable.Null).ToArray();

        

        
        public override string ToString() => (Opener != "" ? Opener + " " : "") + string.Join(", ", (object[])Inputs) + (Closer != "" ? " " + Closer : "");


        public override bool Equals(object obj)
        {
            Clause other = obj as Clause;
            if (other == null) return false;
            if (!this.Opener.Equals(other.Opener)) return false;
            if (!this.Closer.Equals(other.Closer)) return false;
            if (this.Inputs.Length != other.Inputs.Length) return false;
            for (int i = 0; i < Inputs.Length; i++) if (!Inputs[i].Equals(other.Inputs[i])) return false;
            return true;
        }

        [NonSerialized]
        private int _CachedHashCode = -1;
        public override int GetHashCode()
        {
            if (_CachedHashCode != -1) return _CachedHashCode;
            int h = 0;
            foreach (IEvaluateable input in Inputs) h = unchecked(h + input.GetHashCode());
            if (_CachedHashCode == -1) _CachedHashCode++;
            return _CachedHashCode = h;
        }
        
    }
}
