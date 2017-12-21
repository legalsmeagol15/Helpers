using System;
using System.Text.RegularExpressions;

namespace Parsing
{

    /// <summary>
    /// A DataContext object manages the collection of parseable functions and variables.
    /// </summary>
    public class DataContext
    {
        


        #region DataContext constructors

        private DataContext(Func<string, bool> variableNameValidator, Func<string, string> variableNameNormalizer, 
                            Func<string, bool> functionNameValidator, Func<string, string> functionNameNormalizer)
        {
            _VariableManager = new Variable.Manager(variableNameValidator, variableNameNormalizer);
            _FunctionFactory = NamedFunction.Factory.FromStandard();

            string regExPattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | ({6}) | ({7})",
                StringPattern,                          //0
                LeftNestPattern,                        //1
                RightNestPattern,                       //2
                OperatorPattern,                        //3
                WordPattern,                            //4
                NumberPattern,                          //5
                SpacePattern,                           //6
                _VariableManager.VariablePattern);      //7
            FormulaPattern = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);
        }

        private DataContext(Variable.Manager varManager, NamedFunction.Factory funcFactory)
        {
            _VariableManager = varManager;
            _FunctionFactory = funcFactory;

            string regExPattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | ({6}) | ({7})",
                StringPattern,                          //0
                LeftNestPattern,                        //1
                RightNestPattern,                       //2
                OperatorPattern,                        //3
                WordPattern,                            //4
                NumberPattern,                          //5
                SpacePattern,                           //6
                _VariableManager.VariablePattern);      //7
            FormulaPattern = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);
        }


        /// <summary>
        /// Creates a basic-type DataContext, which uses the standard name normalization and validation functions, and includes no unit 
        /// conversion support.
        /// </summary>        
        public static DataContext FromBasic()
        {
            return new DataContext(null, null, null, null);
        }


        #endregion




        #region DataContext Variable management

        /// <summary>The object which maintains a table of existing Variables, and manages their updates through multi-threading.</summary>
        private Variable.Manager _VariableManager;

        /// <summary>Returns whether the given Variable name is a valid Variable, according to the rules of the context.</summary>
        public bool IsVariableNameValid(string name) => _VariableManager.IsValid(name);

        /// <summary>Tries to retrieve the Variable in this Manager which is associated with the given (normalized) name.</summary>
        /// <param name="name">The name of the Variable to retrieve.  The name will be normalized according to the context's normalization 
        /// rules.</param>
        /// <param name="v">Out.  If the Variable can be a retrieve, a reference to it will be contained in this argument.  If not, this 
        /// argument will return null.</param>
        /// <returns>Returns true if the Variable could be retrieved; otherwise, returns false.</returns>
        public bool TryGetVariable(string name, out Variable v) => _VariableManager.TryGet(name, out v);

        /// <summary>Adds a Variable with the given (normalized) name to this Manager.  If the Variable already exists, returns 
        /// null.</summary>
        /// <param name="name">The name of the Variable to add.  The name will be normalized according to the context's normalization 
        /// rules.</param>
        /// <returns>Returns a reference to the Variable added.</returns>
        public Variable AddVariable(string name) => _VariableManager.Add(name);

        /// <summary>
        /// Removes the given Variable from this manager, if there are no context references to it.
        /// </summary>
        /// <returns>Returns true if the Variable is removed.  If the Variable has active references remaining, or if the Variable does 
        /// not exist in this Manager to begin with, returns false.</returns>
        public bool RemoveVariable(Variable v) => _VariableManager.Remove(v);

        #endregion



        #region DataContext NamedFunction management


        /// <summary>The object which creates new NamedFunctions.</summary>
        private NamedFunction.Factory _FunctionFactory;


        /// <summary>Tries to create a new instance of a NamedFunction that has the given (normalized) name.</summary>
        /// <param name="name">The name of the function to create.  The name will be normalized according to the context's normalization 
        /// rules.</param>
        /// <param name="function">Out.  The newly-created NamedFunction instance, if one could be created from the given name.  If it 
        /// could not, returns null.</param>
        /// <returns>Returns true if a new NamedFunction could be created; otherwise, returns false.</returns>
        internal bool TryMakeFunction(string name, out NamedFunction function)
        {
            function = _FunctionFactory.Make(name);
            return function != null;
        }

        /// <summary>Performs any necessary special handling of the given token.</summary>
        /// <param name="token">The string token to interpret.</param>
        /// <param name="specialToken">Out.  Returns a reference to the special token resulting from the given string.  If the string 
        /// could not be interpreted, returns null.</param>
        /// <returns>Returns true if the string could be interpreted; otherwise, returns false.</returns>
        public virtual bool InterpretToken(string token, out object specialToken)
        {
            specialToken = null;
            return false;
        }

        #endregion







        #region DataContext formula Regular Expression members

        private const string StringPattern = "\\\"[^\\\"]+\\\"";
        private const string LeftNestPattern = @"[([{]";
        private const string RightNestPattern = @"[)\]}]";
        private const string OperatorPattern = @"[+-/*&|^~!.]"; //@"\+\-*/&\|^~!\.;";
        private const string WordPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        private const string NumberPattern = @"(-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?"; //Includes support for scientific notation!
        private const string SpacePattern = @"?=\s+";


        private static string StandardRegExPattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | ({6}) | ({7})",
                                                                   StringPattern,                          //0
                                                                   LeftNestPattern,                        //1
                                                                   RightNestPattern,                       //2
                                                                   OperatorPattern,                        //3
                                                                   WordPattern,                            //4
                                                                   NumberPattern,                          //5
                                                                   SpacePattern,                           //6
                                                                   Variable.Manager.StandardVariablePattern);      //7
        public static Regex StandardFormulaPattern = new Regex(StandardRegExPattern, RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// The cached regular expression structure used to lex strings into formulae.
        /// </summary>
        public Regex FormulaPattern { get; private set; }

        #endregion
    }


    internal interface ICacheValue
    {
        object Value { get; }
        object Update();
    }
}
