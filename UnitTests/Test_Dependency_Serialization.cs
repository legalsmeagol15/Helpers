using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Dependency;
using Dependency.Variables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class Test_Dependency_Serialization
    {
        [TestMethod]
        public void TestReferences()
        {
            // Set up an object to serialize
            HostContext oldContext = new HostContext();
            oldContext.Hosts.Add(new VariableHost());
            oldContext.Hosts.Add(new VariableHost());
            var expression = Parse.FromString("var0 * 10", null, oldContext);
            oldContext.Hosts[0].Property.Contents = new Number(1);
            oldContext.Hosts[1].Property.Contents = expression;

            // Show that the original state is as expected
            Assert.AreEqual(oldContext.Hosts[0].Property.Value, 1);
            Assert.AreEqual(oldContext.Hosts[1].Property.Value, 10);
            Assert.IsTrue(oldContext.Hosts[1].Property.DependsOn(oldContext.Hosts[0].Property));

            BinaryFormatter bf = new BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All));
            MemoryStream ms = new MemoryStream(new byte[2048]);
            bf.Serialize(ms, oldContext);
            ms.Seek(0, SeekOrigin.Begin);
            HostContext newContext = (HostContext)bf.Deserialize(ms);


        }

        [Serializable]
        private class HostContext : IContext
        {
            public readonly List<VariableHost> Hosts = new List<VariableHost>();
            public readonly Variable<string> Name = new Variable<string>("Some name");

            bool IContext.TryGetProperty(string path, out IEvaluateable property)
            {
                if (path.ToLower() == "name") { property = Name; return true; }
                property = default;
                return false;
            }

            bool IContext.TryGetSubcontext(string path, out IContext ctxt)
            {
                if (path.ToLower().StartsWith("var"))
                {
                    int idx = int.Parse(path.Substring("var".Length));
                    ctxt = Hosts[idx];
                    return true;
                }
                ctxt = default;
                return false;
            }
        }
        [Serializable]
        private class VariableHost : IContext
        {
            public readonly Variable Property = new Variable();

            bool IContext.TryGetProperty(string path, out IEvaluateable property)
            {
                if (path.ToLower() == "property") { property = Property; return true; }
                property = default;
                return false;
                
            }

            bool IContext.TryGetSubcontext(string path, out IContext ctxt)
            {
                throw new NotImplementedException();
            }
        }
    }
}
