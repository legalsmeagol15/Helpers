using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>
    /// This object can be used to enable object reference serialization without 
    /// </summary>
    /// <remarks>
    /// <para/>Use this object as the context for serialization, like:
    /// <code>
    /// MyObject obj = new MyObject();
    /// SerializationGraph graph = new SerializationGraph();
    /// StreamingContext streamingContext = new StreamingContext(graph);
    /// BinaryFormatter formatter = new BinaryFormatter(null, streamingContext);
    /// MemoryStream ms = new MemoryStream(new byte[2048]);
    /// formatter.Serialize(ms, obj);
    /// </code>
    /// Then, you can use the <seealso cref="ISerializable"/> to call the "SerializeRefs" methods 
    /// <code>
    /// </code>
    /// To deserialize, you simply do the opposite of the serialization:
    /// <code>
    /// </code>
    /// </remarks>
    public sealed class SerializationGraph
    {
        private const string GRAPH_ID = "SerializationGraphId";
        private const string GRAPH_MEMBERS = "SerializationGraphMembers";
        private readonly object _Lock = new object();
        private readonly Dictionary<object, int> _SerializingIndices = new Dictionary<object, int>();
        private readonly List<object> _DeserializingList = new List<object>();

        public static int SerializeRefs<T, U>(StreamingContext context,
                                              SerializationInfo info,
                                              T obj,
                                              string name,
                                              params U[] refs)
                                                        where T : class, ISerializable
                                                        where U : class, ISerializable
            => SerializeRefs(context, info, obj, name, (IEnumerable<U>)refs);

        public static int SerializeRefs<T, U>(StreamingContext context,
                                              SerializationInfo info,
                                              T obj,
                                              string name,
                                              IEnumerable<U> refs)
                                                    where T : class, ISerializable
                                                    where U : class, ISerializable
        {
            SerializationGraph graph = context.Context as SerializationGraph;
            if (graph == null)
                throw new ArgumentException("The StreamingContext must have a " + typeof(SerializationGraph).Name + " as its Context.");
            return graph.SerializeRefs(info, obj, name, refs);
        }

        public int SerializeRefs<T, U>(SerializationInfo info,
                                       T obj,
                                       string name,
                                       params U[] refs)
                                            where T : class, ISerializable
                                            where U : class, ISerializable
            => SerializeRefs(info, obj, name, (IEnumerable<U>)refs);



        public int SerializeRefs<T, U>(SerializationInfo info,
                                      T obj,
                                      string name,
                                      IEnumerable<U> refs)
                                            where T : class, ISerializable
                                            where U : class, ISerializable
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (name == null || string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (name == GRAPH_ID || name == GRAPH_MEMBERS)
                throw new ArgumentException("Invalid property name " + name + ".");
            if (refs == null)
                throw new ArgumentNullException(nameof(refs));
            if (!refs.Any()) return 0;

            int count = 0;

            List<int> ref_indices = new List<int>();

            // Lock so we don't have the same object serialized at 2+ places.
            lock (_Lock)
            {
                // We presume that the serializable members of obj are serialized because it 
                // implements ISerializable, but the object should be indexed in case something 
                // else refers to it.
                if (!_SerializingIndices.TryGetValue(obj, out int idx_obj))
                {
                    idx_obj = _SerializingIndices.Count;
                    _SerializingIndices[obj] = idx_obj;
                }
                info.AddValue(GRAPH_ID, idx_obj);

                // Some items must be serialized at this location.  Which ones?
                HashSet<object> toSerialize = new HashSet<object>();
                foreach (object r in refs)
                {
                    if (!_SerializingIndices.TryGetValue(r, out int i))
                    {
                        toSerialize.Add(r);
                        i = _SerializingIndices.Count;
                        _SerializingIndices[obj] = i;
                    }
                    ref_indices.Add(i);
                }
                info.AddValue(GRAPH_MEMBERS, toSerialize.ToArray());
                count += toSerialize.Count;

            }

            // Add every reference in-order.
            info.AddValue(name, ref_indices.ToArray());

            // Just for kicks, return the count of items newly serialized on this pass.
            return count;
        }

        /// <summary>
        /// When called from the deserialization constructor of an <seealso cref="ISerializable"/> object, 
        /// this method will deserialized object references.
        /// </summary>
        /// <typeparam name="T">The type of object being deserialized.  This must be a reference-
        /// type object, and must implement <seealso cref="ISerializable"/> so it can also call 
        /// this method in its deserialization constructor.</typeparam>
        /// <typeparam name="U">The type of object expected to be linked.  This must be a 
        /// reference-type object, and must implement <seealso cref="ISerializable"/> so it can 
        /// also call this method in its deserialization constructor.</typeparam>
        /// <param name="context">The streaming context for this method.  This must be passed with 
        /// the <seealso cref="StreamingContext.Context"/> set to a 
        /// <see cref="SerializationGraph"/>.</param>
        /// <param name="info">The source of info to be deserialized through 
        /// <seealso cref="SerializationInfo.GetValue(string, Type)"/> calls.</param>
        /// <param name="obj">The object being deserialized.  This method should be called from 
        /// its deserialization constructor.</param>
        /// <param name="name">The name of the property being linked.</param>
        /// <param name="linker">The method used to link the object.  For example, if the 
        /// <paramref name="obj"/> has a "Parent" property, then passing the function:
        /// <para/>(target) => Parent = target
        /// <para/>will cause the parent object to be set.  Likewise, if the 
        /// <paramref name="obj"/> has a "Children" property, then passing the function:
        /// <para/>(target) => Children.Add(target)
        /// <para/>will populate the children as this method iterates through each linked object.
        /// <returns>Returns the count of items actually deserialized by this method to be 
        /// returned.</returns>
        public static int DeserializeRefs<T, U>(StreamingContext context, SerializationInfo info, T obj, string name, Action<U> linker) where T : class, ISerializable where U : class,  ISerializable
        {
            SerializationGraph graph = context.Context as SerializationGraph;
            if (graph == null)
                throw new ArgumentException("The StreamingContext must have a " + typeof(SerializationGraph).Name + " as its Context.");
            return graph.DeserializeRefs(info, obj, name, linker);
        }

        /// <summary>
        /// When called from the deserialization constructor of an <seealso cref="ISerializable"/> object, 
        /// this method will deserialized object references.
        /// </summary>
        /// <typeparam name="T">The type of object being deserialized.  This must be a reference-
        /// type object, and must implement <seealso cref="ISerializable"/> so it can also call 
        /// this method in its deserialization constructor.</typeparam>
        /// <typeparam name="U">The type of object expected to be linked.  This must be a 
        /// reference-type object, and must implement <seealso cref="ISerializable"/> so it can 
        /// also call this method in its deserialization constructor.</typeparam>
        /// <param name="info">The source of info to be deserialized through 
        /// <seealso cref="SerializationInfo.GetValue(string, Type)"/> calls.</param>
        /// <param name="obj">The object being deserialized.  This method should be called from 
        /// its deserialization constructor.</param>
        /// <param name="name">The name of the property being linked.</param>
        /// <param name="linker">The method used to link the object.  For example, if the 
        /// <paramref name="obj"/> has a "Parent" property, then passing the function:
        /// <para/>(target) => Parent = target
        /// <para/>will cause the parent object to be set.  Likewise, if the 
        /// <paramref name="obj"/> has a "Children" property, then passing the function:
        /// <para/>(target) => Children.Add(target)
        /// <para/>will populate the children as this method iterates through each linked object.
        /// <returns>Returns the count of items actually deserialized by this method to be 
        /// returned.</returns>
        public int DeserializeRefs<T, U>(SerializationInfo info, T obj, string name, Action<U> linker) where T : class, ISerializable where U : class, ISerializable
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (name == null || string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (name == GRAPH_ID || name == GRAPH_MEMBERS)
                throw new ArgumentException("Invalid property name " + name + ".");
            if (linker == null)
                throw new ArgumentNullException(nameof(linker));

            // Get the index for this object.
            int idx_obj = info.GetInt32(GRAPH_ID);

            // If an item with this index already exists, then we've encountered a cycle in the 
            // the graph.  Return rather than deserializing the object a second time.
            if (idx_obj < _DeserializingList.Count && _DeserializingList[idx_obj] != null)
                return 0;

            // Make sure the list is big enough for this index, then set the list at this index to 
            // this object.
            while (idx_obj >= _DeserializingList.Count)
                _DeserializingList.Add(null);
            _DeserializingList[idx_obj] = this;

            // Deserialize all the referenced objects.  We don't need to store references to them 
            // really because they'll exist in the graph's list when deserialization is complete.
            object[] deserialized;
            lock (_Lock)
            {
                deserialized = (object[])info.GetValue(GRAPH_MEMBERS, typeof(object[]));
            }

            // Now, each referenced object should exist in the list.
            int[] references = (int[])info.GetValue(name, typeof(int[]));

            // Do the linking.
            foreach (int r in references)
                linker((U)_DeserializingList[r]);

            // As a matter of interest, return the count of deserialized objects.  Don't  know if 
            // this will ever be useful.
            return deserialized.Length;
        }


    }
}
