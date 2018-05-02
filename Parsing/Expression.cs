using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Helpers.Parsing
{
    public interface IExpression
    {
        IExpression Evaluate();        
    }

    

    public static class Expression
    {
        public static IExpression FromString(string str, Variable.DataContext context = null, Function.Factory factory = null)
        {
            if (str == null) return new Error("Expression string cannot be null.");
            string[] rawTokens = _Regex.Split(str);
            Debug.Assert(rawTokens.Length > 0);
            Stack<Nesting> nestStack = new Stack<Nesting>();
            Nesting focus = Nesting.Parenthetical(), root = focus;
            nestStack.Push(focus);
            for (int tokenIdx = 0; tokenIdx < rawTokens.Length; tokenIdx++)
            {
                //Step #2a - skip whitespace.
                string rawToken = rawTokens[tokenIdx].Trim();
                if (rawToken == "" || string.IsNullOrWhiteSpace(rawToken)) continue;

                //Step #2b - handle particular tokens.
                switch (rawToken)
                {
                    // Starting a new nesting clause?
                    case "(":
                    case "[":
                    case "{":
                        nestStack.Push(Nesting.FromSymbol(rawToken));
                        // An implicit scalar?
                        if (focus.Inputs.Count > 0 && focus.Inputs.Last() is Number)
                            focus.Inputs.Add(new Helpers.Parsing.Functions.Multiplication());
                        focus.Inputs.Add(nestStack.Peek());
                        continue;
                    // Concluding the current nesting clause?
                    case ")":
                    case "]":
                    case "}":
                        if (nestStack.Pop().Closer == rawToken[0]) return new Error("Nesting error.");
                        if (nestStack.Count == 0) return new Error("Too many closing brackets.");
                        focus = nestStack.Peek();
                        continue;
                    // A divider between inputs?
                    case ",": continue;
                    // A divider between sub-vectors?
                    case ";":
                        if (nestStack.Count == 0 || nestStack.Peek().Opener != '[')
                            return new Error("Vector subdivision outside of declared vector.");
                        int lastIdx = focus.Inputs.FindLastIndex((item) => item is Nesting n && n.Opener == '[');
                        Nesting subVector = Nesting.Bracketed(focus.Inputs.Skip(lastIdx + 1));
                        focus.Inputs.RemoveRange(0, lastIdx + 1);
                        focus.Inputs.Add(subVector);
                        continue;
                    // A string?
                    case var s when s.StartsWith("\"") && s.EndsWith("\"") && s.Count((c) => c == '\"') == 2:
                        focus.Inputs.Add(new Helpers.Parsing.String(s.Substring(1, s.Length - 2)));
                        continue;

                    //A negation?                        
                    case "-":
                    case "!":
                    case "~":
                        Function op;
                        if (focus.Inputs.Count == 0 || focus.Inputs.Last() is Helpers.Parsing.Functions.Operator) op = new Helpers.Parsing.Functions.Negation();
                        else op = new Helpers.Parsing.Functions.Subtraction();
                        focus.Inputs.Add(op);
                        continue;

                    //Some other operation?
                    case "+": { focus.Inputs.Add(new Helpers.Parsing.Functions.Addition()); continue; }
                    case "*": { focus.Inputs.Add(new Helpers.Parsing.Functions.Multiplication()); continue; }
                    case "/": { focus.Inputs.Add(new Helpers.Parsing.Functions.Division()); continue; }
                    case "^": { focus.Inputs.Add(new Helpers.Parsing.Functions.Exponentiation()); continue; }
                    case "|": { focus.Inputs.Add(new Helpers.Parsing.Functions.Or()); continue; }
                    case "&": { focus.Inputs.Add(new Helpers.Parsing.Functions.And()); continue; }
                    case ".": { focus.Inputs.Add(new Helpers.Parsing.Functions.Relation()); continue; }
                    case ":": { focus.Inputs.Add(new Helpers.Parsing.Functions.Range()); continue; }

                }

                //Step #2c - try to interpret as literals.
                if (decimal.TryParse(rawToken, out decimal m)) { focus.Inputs.Add(new Helpers.Parsing.Number(m)); continue; }
                if (bool.TryParse(rawToken, out bool b)) { focus.Inputs.Add(new Helpers.Parsing.Boolean(b)); continue; }

                //Step #2d - try to interpret as a named function.
                if (factory != null && factory.TryMakeFunction(rawToken, out Function f)) { focus.Inputs.Add(f); continue; }

                //Step #2e - try to interpret as a variable, or create the variable.
                if (context != null)
                {
                    if (context.TryGetVariable(rawToken, out Variable v1)) { focus.Inputs.Add(v1); continue; }
                    if (context.TryCreateVariable(rawToken, out Variable v2) { focus.Inputs.Add(v2); continue; }
                }
                
            }
        }


        private static Regex _Regex = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);
        private static string regExPattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | ({6}) | ({7})",
               StringPattern,                          //0
               LeftNestPattern,                        //1
               RightNestPattern,                       //2
               OperatorPattern,                        //3
               WordPattern,                            //4
               NumberPattern,                          //5
               SpacePattern,                           //6
               VariablePattern);                       //7
        private const string StringPattern = "\\\"[^\\\"]+\\\"";
        private const string LeftNestPattern = @"[([{]";
        private const string RightNestPattern = @"[)\]}]";
        private const string OperatorPattern = @"[+-/*&|^~!.]"; //@"\+\-*/&\|^~!\.;";
        private const string WordPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        private const string NumberPattern = @"(-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?"; //Includes support for scientific notation!
        private const string SpacePattern = @"?=\s+";
        private const string VariablePattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
    }
}
