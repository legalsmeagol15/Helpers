using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{


    /// <summary>An exception signaling a poorly-formed string the cannot be interpreted as a Formula.</summary>
    public class LexingException : Exception
    {

        /// <summary>The list of string tokens where whose interpretation caused the exception.</summary>
        public string[] Tokens { get; internal set; }

        /// <summary>The index of the token which caused the exception.</summary>
        public int TokenIndex { get; internal set; }

        /// <summary>Creates a new FormatException.</summary>
        /// <param name="message">The message to accompany the exception.</param>
        /// <param name="tokens">The set of string tokens that caused the exception.</param>
        /// <param name="tokenIndex">The index of the token where the exception occurred.</param>
        public LexingException(string message, IEnumerable<string> tokens = null, int tokenIndex = -1) : base(message, null)
        {
            Tokens = (tokens!=null) ? tokens.ToArray() : null;            
            TokenIndex = tokenIndex;
        }

        /// <summary>Creates a new FormatException.</summary>
        /// <param name="innerException">The inner exception thrown.</param>
        /// <param name="message">The message to accompany the exception.</param>
        /// <param name="tokens">The set of string tokens that caused the exception.</param>
        /// <param name="tokenIndex">The index of the token where the exception occurred.</param>
        public LexingException(string message, Exception innerException, IEnumerable<string> tokens = null, int tokenIndex = -1) 
            : base (message, innerException)
        {
            Tokens = tokens.ToArray();
            TokenIndex = tokenIndex;
        }
    }






    /// <summary>An exception signaling a problem with bracket or parenthetical nesting in a string to be interpreted as a 
    /// Formula.</summary>
    public class NestingFormatException : LexingException
    {
        /// <summary>The string symbol used to create the nesting structure.</summary>
        public readonly string Opener;

        /// <summary>The index (into the associated tokens) of the opening symbol for the errant nesting structure.</summary>
        public readonly int OpenerIndex;

        /// <summary>Creates a new NestingFormatException.</summary>
        /// <param name="message">The message to accompany the exception.</param>        
        /// <param name="opener">The symbol used to create the nesting bracket.</param>
        /// <param name="openerIndex">The index of the opening symbol.</param>
        public NestingFormatException(string message, string opener, int openerIndex) : base(message)
        {
            Opener = opener;
            OpenerIndex = openerIndex;
        }
    }





    /// <summary>An exception thrown when the evaluation of a Formula goes awry.</summary>
    [Serializable]
    public class EvaluationException : Exception
    {
        /// <summary>Creates a new EvaluationException.</summary>
        public EvaluationException() { }

        /// <summary>Creates a new EvaluationException.</summary>
        public EvaluationException(string message) : base(message) { }

        /// <summary>Creates a new EvaluationException.</summary>
        public EvaluationException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Creates a new EvaluationException.</summary>
        protected EvaluationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }


    ///// <summary>
    ///// Represents a fatal error in parsing or evaluation.
    ///// </summary>
    //public abstract class Error
    //{
    //    /// <summary>The explanation of this error.</summary>
    //    public readonly string Message;

    //    /// <summary>Creates a new error.</summary>        
    //    protected Error(string message) { Message = message; }
    //}

    ///// <summary>An error generated when evaluation goes awry.</summary>
    //public class EvaluationError : Error
    //{
    //    /// <summary>Creates a new EvaluationError, with the given message.</summary>        
    //    public EvaluationError(string message) : base(message) { }
    //}


}
