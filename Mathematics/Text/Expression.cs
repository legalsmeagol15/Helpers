using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Arithmetic.Text
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence rules.  The allowed symbols are non-negative numbers written using double-precision floating-point syntax; variables 
    /// that consist of a letter or underscore followed by zero or more letters, underscores, or digits; parentheses; and the six operator symbols +, -, *, /, %, and ^.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; and "x 23" consists of a 
    /// variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The normalizer is used to convert variables into a canonical form (like into all caps), and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement that it consist of a letter or underscore followed by zero or more letters, underscores, or digits.)  Their 
    /// use is described in detail in the constructor and method comments.
    /// </summary>
    /// <author>Wesley Oates</author>
    /// <date> Sep 24, 2015.</date>
    /// <remarks>Note that this class, and associate classes, were developed during CS3500 Fall '15.  Some code or commenting may come from the assignment skeletons.</remarks>
    public class Expression : IEvaluateable
    {


        /// <summary>
        /// The list of typed token objects maintained in this Formula.
        /// </summary>
        private readonly IList<IList<IToken>> Arguments;

        /// <summary>
        /// The scale multiplier of this Formula.  For sub-formulas (ie, those nested in another formula 
        /// through parentheses), this value is used to set negation.  Note that is not PRESENTLY useful 
        /// for setting implicit multiplicative scale (ie, 2(5+7) as opposed to the currently required 
        /// 2*(5+7) ), but it could be so changed in the future. 
        /// </summary>
        private readonly double Scalar;


        /// <summary>
        /// The function which will operate on this formula's arguments.
        /// </summary>
        private readonly NamedFunction Function;

     

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Expression(String expression, IDictionary<string, NamedFunction> allowedFunctions = null)
            : this(null, GetTokens(expression), 1.0, (s => s), (s => true), allowedFunctions)
        {
        }

        
        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Expression(String expression, Func<string, string> normalizer, Func<string, bool> isValid,
                        IDictionary<string, NamedFunction> allowedFunctions = null) :
            this(null, GetTokens(expression), 1.0, normalizer, isValid, allowedFunctions)
        {
        }


        /// <summary>
        /// Creates a new Formula with a scalar as appropriate.  Note that currently, the scalar is only 
        /// used to establish negation, and should not be used as an implicit multiplier.
        /// </summary>
        /// <param name="tokens">The tokens to convert into appropriate types.</param>
        /// <param name="scalar">The scalar is only used to establish negation, and should not be used as an 
        /// implicit multiplier.</param>
        /// <param name="normalizer">The variable name normalization function.</param>
        /// <param name="isValid">The variable name validation function.</param>
        protected Expression(NamedFunction expression, IEnumerable<string> tokens, double scalar,
                            Func<string, string> normalizer, Func<string, bool> isValid,
                            IDictionary<string, NamedFunction> allowedFunctions)
        {
            this.Scalar = scalar;
            this.Function = expression;
            Arguments = ConvertToArguments(tokens, normalizer, isValid, allowedFunctions);
            if (this.Function == null && Arguments.Count > 1)
                throw new FormulaFormatException("Multiple arguments are allowed only for named functions.");
            else if (this.Function != null)
            {
                if (Arguments.Count < this.Function.MinimumArgs)
                    throw new FormulaFormatException("Function " + this.Function.Name + " requires at least " + this.Function.MinimumArgs + " argument(s).");
                if (Arguments.Count > this.Function.MinimumArgs)
                    throw new FormulaFormatException("Function " + this.Function.Name + " allows no more than " + this.Function.MaximumArgs + " argument(s).");
            }
        }

        /// <summary>
        /// Creates a Expression for an argument-less named function.
        /// </summary>
        /// <param name="function"></param>
        protected Expression(NamedFunction function, double scalar)
        {
            this.Function = function;
            this.Scalar = scalar;
            Arguments = new List<IList<IToken>>();
        }


        /// <summary>
        /// Stores typed tokens based on the given string token representations.  This is the method 
        /// that will do the magic as far as converting strings into typed tokens.
        /// </summary>
        /// <param name="stringTokens">The enumerated string tokens to interpret and convert into type-
        /// safe values.</param>
        /// <param name="normalizer">The method that returns the normalized version of a variable's name.</param>
        /// <param name="isValid">The method that determines whether a given variable's name (already normalized) is valid.</param>
        /// <param name="allowedFunctions">The named functions that the formula is allowed to build.</param>
        protected internal static IList<IList<IToken>> ConvertToArguments(IEnumerable<string> stringTokens, Func<string, string> normalizer, Func<string, bool> isValid, 
                                                                          IDictionary<string, NamedFunction> allowedFunctions)
        {
            if (stringTokens == null)
                throw new FormulaFormatException("stringTokens cannot be null.");

            //Setup
            IList<IList<IToken>> allArgs = new List<IList<IToken>>();
            List<IToken> buildingArg = new List<IToken>();
            double sign = 1.0;  //For tracking negation
            int paren = 0;      //For tracking parenthetical level.
            List<string> nestingTokens = new List<string>(); //For tracking parentheses-nested tokens.


            NamedFunction buildingFunction = null;


            //The token-by-token driver is a foreach loop to take advantage of the just-in-time token 
            //production in the GetTokens() static method.  This driver will handle nesting 
            //formulae first (ie, parentheticals, named functions) before moving on.
            foreach (string stringToken in stringTokens)
            {
                //Step #0-throw away whitespace
                if (string.IsNullOrWhiteSpace(stringToken)) continue;

                //Step #1
                //Handle nesting parentheses.  This works by using a parentheses depth counter 'paren'.
                //If paren>0, then we are in the middle of a nested parentheses expression.  All tokens 
                //in between the first and last parentheses (including sub-parentheses) are included in 
                //a nested tokens list, and when the concluding parenthesis is encountered, a new 
                //sub-formula is made of those tokens and added to this Expression's token list.
                if (paren > 0)
                {

                    if (stringToken == "(" || stringToken == "[") //Count up the parentheses depth.
                    {
                        paren++;
                        nestingTokens.Add(stringToken);
                    }


                    else if (stringToken == ")" || stringToken == "]") //Count down the parentheses depth
                    {
                        if (--paren == 0)   //If ==0, it's the concluding parenthesis token.  Create the 
                                            //new expression.
                        {
                            //Formula nestedExp = new Formula(nestingTokens, sign, normalizer, isValid);
                            Expression nestedExp = new Expression(buildingFunction, nestingTokens, sign, normalizer, isValid, allowedFunctions);
                            buildingFunction = null;
                            buildingArg.Add(nestedExp);
                            nestingTokens.Clear();
                            sign = 1.0;
                        }
                        else nestingTokens.Add(stringToken);     //If !=0, it's a sub-parentheses.  Just add 
                                                                 //and continue.
                    }
                    else nestingTokens.Add(stringToken);
                    continue;
                }
                if (stringToken == "(" || stringToken == "[")
                {
                    if (buildingArg.Count > 0 && buildingArg.Last() is IEvaluateable)
                        throw new FormulaFormatException("Nesting parenthesis may not occur next to another constant, variable, or formula (except named functions that require 1 or more arguments).");
                    paren++;
                    continue;
                }
                if (stringToken == ")" || stringToken == "]")
                {
                    //This is outside a nesting parentheses, so a closing parenthesis is illegal.
                    throw new FormulaFormatException("Invalid closing parentheses '" + stringToken + "'.  Omit.");
                }


                //At this point, there should be no parentheses bracketing where the foreach loop
                //is operating.  Nested expressions will have been added as sub-formulae.                
                if (paren > 0) throw new FormulaFormatException("Parentheses error - incomplete parenthetical expression.");

                //At this point, there should be no buildingFunction stored, because it will have 
                //been taken care of in a sub-formula with nesting parentheses above.                
                if (buildingFunction != null) throw new FormulaFormatException("Functions must be defined in nested parentheses.");

                //Step #2 - handle named function building
                if (allowedFunctions != null && allowedFunctions.ContainsKey(stringToken))
                {
                    if (buildingArg.Count > 0 && buildingArg.Last() is IEvaluateable)
                        throw new FormulaFormatException("Named function cannot follow " + buildingArg.Last() + ".");
                    NamedFunction newFunc = allowedFunctions[stringToken];

                    //If the new Func takes no args anyway, no need to build parenthetical formulae.
                    if (newFunc.MaximumArgs < 1)
                    {
                        buildingArg.Add(new Expression(newFunc, sign));
                        sign = 1.0;
                    }
                    else buildingFunction = newFunc;

                    continue;
                }


                //Step #3 - handle argument distinguishing ','
                if (stringToken == ",")
                {
                    //This is like ending a complete formula.  Incomplete parentheticals or 
                    //incomplete building functions have been handled, so only need to look out for 
                    //sign problems.
                    if (sign != 1.0) throw new FormulaFormatException("Negation error - value-type argument must follow '-'.");
                    allArgs.Add(buildingArg);
                    buildingArg = new List<IToken>();
                    continue;
                }


                //Step #3 - handle variable storage.
                string normed = normalizer(stringToken);
                if (IsValidStandard(normed))
                {
                    if (!isValid(normed))
                        throw new FormulaFormatException("Invalid variable name per custom variable validator.");
                    if (buildingArg.Count > 0 && buildingArg.Last() is IEvaluateable)
                        throw new FormulaFormatException("Variable may not come immediately after " + buildingArg.Last().GetType().Name);

                    buildingArg.Add(new Variable(normed, sign));
                    sign = 1.0;
                    continue;
                }



                //Step #4 - handle negation.
                //This works by setting a negation sign to 1 or -1, as appropriate.  Negation is a unary 
                //operator, so it will only activate if there are no prior tokens, or the prior token is an 
                //operation.
                if (stringToken == "-")
                {
                    //If the last token was a value token, the '-' must be asking for a subtraction.
                    if (buildingArg.Count > 0 && buildingArg.Last() is IEvaluateable)
                        buildingArg.Add(new Operation('-'));

                    else
                    {
                        //If sign has not been negated, negate it.
                        if (sign == 1.0) sign = -1.0;

                        //Otherwise, too many negations - throw exception
                        else throw new FormulaFormatException("Negation error.");
                    }
                    continue;
                }



                //Step #5 - handle regular arithmetic operators besides negation.
                if (stringToken == "+" || stringToken == "*" || stringToken == "/" || stringToken == "^" || stringToken == "%" || stringToken=="-")
                {
                    //Check rule 7 - parenthesis following.
                    if (buildingArg.Count == 0 || buildingArg.Last() is Operation)
                        throw new FormulaFormatException("Operator '" + stringToken
                            + "' must follow a value-type expression.");

                    buildingArg.Add(new Operation(stringToken[0]));
                    continue;
                }


                //Step #6 - handle constant values.  These should simply be stored as Constant objects.
                double num = 0;
                if (double.TryParse(stringToken, out num))
                {
                    if (buildingArg.Count > 0 && buildingArg.Last() is IEvaluateable)
                        throw new FormulaFormatException("Error on " + stringToken + ".  Numbers may not come after other variables, numbers, or formulas.");
                    buildingArg.Add(new Constant(num * sign));
                    sign = 1.0;
                    continue;
                }


                //Step last - uh-oh.  If the foreach loop has gotten to this point, it means it does not recognize the token.                
                throw new FormulaFormatException("Unrecognized variable name or token " + stringToken + ".");

            }

            //At this point, a set of semi-validated tokens should have been created, but paren
            //might be out of balance.
            if (paren > 0) throw new FormulaFormatException("Extra opening parenthesis '('.");

            //If it's still trying to build a named function, that's a problem.
            if (buildingFunction != null) throw new FormulaFormatException("Named functions must be "
                + " followed by complete parenthetical expressions of arguments.");

            //If there's no tokens, that's a problem.
            if (allArgs.Count == 0 && buildingArg.Count == 0) throw new FormulaFormatException("No valid tokens provided.");

            //If the building arg has no tokens, that's an argument division problem.
            if (buildingArg.Count == 0) throw new FormulaFormatException("No valid tokens for argument.");

            //If the last token is an operator, that's a problem too.
            if (buildingArg.Last() is Operation) throw new FormulaFormatException("Cannot end a formula with an operator.");

            //Parentheses level ok, return the result.
            allArgs.Add(buildingArg);
            return allArgs;
        }



        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup = null)
        {
            try
            {
                if (Function == null)
                {
                    //Report the evaluation of the first (and only) stored argument.
                    object value = Evaluate(this.Arguments[0], lookup, this.Scalar);
                    return value;
                }
                else
                {
                    //Report the evaluated Arguments to the named function.
                    List<object> args = this.Arguments.Select(arg => Evaluate(arg, lookup, 1.0)).ToList();
                    object value = Function.Evaluate(args);
                    if (value is double) value = (double)value * this.Scalar;
                    return value;
                }

            }
            catch (Exception ex)
            {
                return new FormulaError(ex.Message);
            }

        }

        /// <summary>
        /// This will return the double evlauation of the formula.  Note that if the result is 
        /// not a double but a FormulaError, this method will throw an exception.
        /// </summary>

        double IEvaluateable.Evaluate(Func<string, double> lookupFunction)
        {
            return (double)Evaluate(lookupFunction);
        }

        /// <summary>
        /// Evaluates the given tokens and returns a value, or if the tokens cannot be evaluated, returns 
        /// a FormulaError object.
        /// </summary>        
        /// <param name="tokens">The set of tokens to evaluate.</param>
        /// <param name="lookup">The lookup function for finding the value of a variable given a variable name.
        /// </param>
        /// <param name="scalar">The scale to multiply the result.  The purpose of this argument is to allow 
        /// formula negation, though in the future it may be set up to allow implicit multiplication.</param>
        /// <remarks>This method is set up as a separate static method to allow easy white box testing.  It 
        /// follows the rules of the non-static Evaluate(Func) method described in the assignment.
        /// </remarks>
        protected static object Evaluate(IEnumerable<IToken> tokens, Func<string, double> lookup,
                                            double scalar = 1.0)
        {
            //Set up a list of tokens to manipulate without changing the original.
            List<object> tempTokens = new List<object>(tokens);

            //Go through token-by-token and evaluate according to the correct order of operations.  The 
            //first pass will replace sub-formulas and variables with their respective values.  From 
            //there, the order of operations is maintained by taking subsequent passes through the list 
            //until there is a single constant value remaining, with each pass focusing on an operation 
            //in ascending order.
            for (int i = 0; i < tempTokens.Count; i += 2)
            {
                //1st, replace sub-formulas with their values.
                if (tempTokens[i] is Expression)
                {
                    Expression f = (Expression)tempTokens[i];
                    tempTokens[i] = f.Evaluate(lookup);
                    if (tempTokens[i] is FormulaError) return tempTokens[i];
                }


                //TODO:  Arithmetic.Text.Expression.Evaluate - really only need to check every other token to see if it's a variable or formula

                //2nd, replace variables with their values.
                else if (tempTokens[i] is Variable)
                {
                    try
                    {
                        tempTokens[i] = ((Variable)tempTokens[i]).Evaluate(lookup);
                    }
                    catch
                    {
                        //The native exception is caught to ensure the correct type of exception is thrown.
                        throw new ArgumentException("Cannot lookup value of Variable "
                                                    + tempTokens[i].ToString());
                    }

                }

                else if (tempTokens[i] is Constant)
                    tempTokens[i] = ((Constant)tempTokens[i]).Value;


            }

            //Now that sub-values have been updated, perform the operations.  The tokens should presently 
            //be free of both variables and sub-formulas, and be in a format of operations and constants 
            //like:  Constant-Operation-Constant-Operation- ... - Constant.
            //TODO:  Someday, the Operation objects themselves will implement the actual calculation in a 
            //priorityQueue that follows order of operations.


            //Pass #1 - exponent (power ^)
            for (int i = 1; i < tempTokens.Count; i += 2)
            {
                Operation oper = (Operation)(tempTokens[i]);
                if (oper.Operator == '^')
                {
                    tempTokens[i - 1] = Math.Pow((double)tempTokens[i - 1],
                        (double)tempTokens[i + 1]);
                    tempTokens.RemoveAt(i);
                    tempTokens.RemoveAt(i);
                    i -= 2;
                    continue;
                }
            }




            //Pass #2 - division
            for (int i = 1; i < tempTokens.Count; i += 2)
            {
                Operation oper = (Operation)(tempTokens[i]);
                if (oper.Operator == '/')
                {
                    double divisor = (double)tempTokens[i + 1];
                    if (divisor == 0) return new FormulaError("Division by 0.");
                    tempTokens[i - 1] = (double)tempTokens[i - 1] / divisor;
                    tempTokens.RemoveAt(i);
                    tempTokens.RemoveAt(i);
                    i -= 2;
                    continue;
                }
            }

            //pass #2a - modulus
            for (int i = 1; i < tempTokens.Count; i += 2)
            {
                Operation oper = (Operation)(tempTokens[i]);
                if (oper.Operator == '%')
                {
                    double divisor = (double)tempTokens[i + 1];
                    if (divisor == 0) return new FormulaError("Division by 0.");
                    tempTokens[i - 1] = (double)tempTokens[i - 1] % divisor;
                    tempTokens.RemoveAt(i);
                    tempTokens.RemoveAt(i);
                    i -= 2;
                    continue;
                }
            }

            //Pass #3 - multiplication
            for (int i = 1; i < tempTokens.Count; i += 2)
            {
                Operation oper = (Operation)(tempTokens[i]);
                if (oper.Operator == '*')
                {
                    tempTokens[i - 1] = (double)tempTokens[i - 1] * (double)tempTokens[i + 1];
                    tempTokens.RemoveAt(i);
                    tempTokens.RemoveAt(i);
                    i -= 2;
                    continue;
                }
            }

            //Pass #4 - subtraction
            for (int i = 1; i < tempTokens.Count; i += 2)
            {
                Operation oper = (Operation)(tempTokens[i]);
                if (oper.Operator == '-')
                {
                    tempTokens[i - 1] = (double)tempTokens[i - 1] - (double)tempTokens[i + 1];
                    tempTokens.RemoveAt(i);
                    tempTokens.RemoveAt(i);
                    i -= 2;
                    continue;
                }
            }

            //Pass #5 - addition
            for (int i = 1; i < tempTokens.Count; i += 2)
            {
                Operation oper = (Operation)(tempTokens[i]);
                if (oper.Operator == '+')
                {
                    tempTokens[i - 1] = (double)tempTokens[i - 1] + (double)tempTokens[i + 1];
                    tempTokens.RemoveAt(i);
                    tempTokens.RemoveAt(i);
                    i -= 2;
                    continue;
                }
            }

            
            //The only thing left should be a final Constant containing the value.
            if (tempTokens[0] is double) return (double)tempTokens[0] * scalar;

            //Otherwise, this is a problem - throw a FormulaError, per the instructions in the header.
            return new FormulaError("Unevaluated tokens.  Tokens did not evaluate to 1 constant double.");
        }





        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            List<Variable> result = new List<Variable>();
            GetVariables(result);
            return result.Distinct().Select(v => v.Name);
        }
        /// <summary>
        /// Recursively gets the Variables of this Formula and any sub-Formulas.
        /// </summary>
        /// <param name="list">The list being compiled.  Note that if the same Variable appears in multiple 
        /// locations, the list will contain several references to that Variable.</param>
        private void GetVariables(List<Variable> list)
        {
            foreach (IList<IToken> argument in Arguments)
            {
                list.AddRange(argument.OfType<Variable>());
                foreach (Expression f in argument.OfType<Expression>()) f.GetVariables(list);
            }


        }



        /// <summary>
        /// Determines whether the given variable name begins with a letter or underscore, and there
        /// after contains any mix of letters, digits, and/or underscores.
        /// </summary>
        protected static bool IsValidStandard(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;

            int i = 1;
            while (i < name.Length && (char.IsLetterOrDigit(name[i]) || name[i] == '_')) i++;
            return i == name.Length;

        }


        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {

            string str = "";
            if (Function == null)
            {
                if (Scalar == -1.0) str = " -(";
                else if (Scalar == 1.0) str = "";

                //For future enhancements:
                //else if (Scalar < 0.0) str = " " + Scalar + "(";
                //else str = Scalar + "(";
            }
            else
            {
                if (Scalar == -1.0) str = " -" + Function.Name + "(";
                else if (Scalar == 1.0) str = Function.Name + "(";

                //For future enhancements:
                //else if (Scalar < 0.0) str = " " + Scalar + "(";
                //else str = Scalar + "" + Function.Name + "(";
            }


            for (int i = 0; i < Arguments.Count; i++)
            {
                foreach (IToken token in Arguments[i])
                {

                    if (token is Expression)
                    {

                        Expression f = (Expression)token;
                        if (f.Scalar == 1.0 && f.Function == null) str += "(" + token.ToString() + ")";
                        else str += token.ToString();
                    }
                    else str += token.ToString();
                }
                if (i < Arguments.Count - 1) str += ", ";
            }

            if (Scalar != 1.0 || Function != null) str += ")";

            return str;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens, which are compared as doubles, and variable tokens,
        /// whose normalized forms are compared as strings.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            Expression other = obj as Expression;
            if (other == null) return false;
            if (Function == null ^ other.Function == null) return false;


            //If they have an unequal count of tokens, cannot be equal.
            if (Arguments.Count != other.Arguments.Count) return false;

            //If not the same function, they are not equal
            if (Function != null && !Function.Equals(other.Function)) return false;


            //If not the same scalar, they are not equal.
            if (Scalar != other.Scalar) return false;

            //If they are unequal token-by-token, return false.
            for (int i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i].Count != other.Arguments[i].Count) return false;
                for (int j = 0; j < Arguments[i].Count; j++)
                {
                    if (!Arguments[i][j].Equals(other.Arguments[i][j])) return false;
                }
            }


            //Falsification conditions have been ruled out, so they must be equal.
            return true;
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Expression f1, Expression f2)
        {
            //TODO:  test for infinite recursion.
            if (Object.ReferenceEquals(f1, null) && Object.ReferenceEquals(f2, null)) return true;
            if (Object.ReferenceEquals(f1, null)) return false;
            if (Object.ReferenceEquals(f2, null)) return false;
            return f1.Equals(f2);
        }


        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Expression f1, Expression f2)
        {
            return !(f1 == f2);
        }


        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            long result = 1 + Arguments.Count;
            foreach (Object token in Arguments)
            {
                result += token.GetHashCode();
            }
            return Math.Abs((int)result);
        }



        #region "Formula token splitting members"
        // Patterns for individual tokens
        private const String lpPattern = @"\(";
        private const String rpPattern = @"\)";
        private const String opPattern = @"[\+\-*/^,]";
        private const String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        private const String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
        private const String spacePattern = @"\s+";

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        protected static IEnumerable<string> GetTokens(String formula)
        {


            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }

        }

        #endregion

    }


    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }
}

