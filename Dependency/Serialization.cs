using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    internal interface ISerializedVariable : ISerializable
    {
        void GetObjectData(SerializationInfo info, ISerializationManager manager);
    }
    public interface ISerializationManager
    {
        void Serialize(object toBeSerialized, SerializationInfo info, StreamingContext context);
    }
    /// <summary>
    /// When the dependency graph is serialized, all variables must be in a "settled" state with 
    /// no extant <seealso cref="Update"/>s being processed and their 
    /// <seealso cref="Variable.Value"/>s fully determined. This object manages that process.
    /// </summary>
    public sealed class Serialization : ISerializationManager
    {
        private const int UPDATE_SETTLE_TIME_MS = 1000;
        public readonly int AllowedDelay;
        public Serialization(int allowedDelay = -1)
        {
            this.AllowedDelay = allowedDelay;
        }

        private void Update_Settled(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public static void GetObjectData(object toBeSerialized, SerializationInfo info, StreamingContext context)
        {
            if (!(context.Context is ISerializationManager ism) )
                throw new ArgumentException("Context must implement " + nameof(ISerializationManager));
            ism.Serialize(toBeSerialized, info, context);
        }
        private static readonly object _SerializationObject = new object();
        internal void Serialize(ISerializedVariable variable, SerializationInfo info, StreamingContext context)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable), "Was a non-" + nameof(ISerializedVariable) + " passed to " + nameof(GetObjectData) + "?");

            // Only one thread may serialize at a time.
            lock (_SerializationObject)
            {
                // Serialization must wait until all values settle
                if (!Update.Settle(UPDATE_SETTLE_TIME_MS, true) && !Update.IsPaused)
                    throw new InvalidOperationException("Failed to timely settle updates.");
                variable.GetObjectData(info, this);
            }
            
        }

        void ISerializationManager.Serialize(object toBeSerialized, SerializationInfo info, StreamingContext context)
            => Serialize(toBeSerialized as ISerializedVariable, info, context);
    }
}
