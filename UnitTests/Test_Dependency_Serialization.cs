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
        public void Test_Simple()
        {
            _RoundTrip(new Number(10));
            _RoundTrip(new Dependency.String("someString"));
            _RoundTrip(new Variable<string>("AnotherString"));
            var p = Parse.FromString("(6*ABS(-3))", null, null);
            _RoundTrip(new Variable(p));

            void _RoundTrip(IEvaluateable before)
            {
                BinaryFormatter bf = new BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All));
                MemoryStream ms = new MemoryStream(new byte[2048]);
                bf.Serialize(ms, before);
                ms.Seek(0, SeekOrigin.Begin);
                IEvaluateable after = (IEvaluateable)bf.Deserialize(ms);

                try
                {
                    Assert.AreEqual(before.Value, after.Value);
                }
                catch (AssertFailedException)
                {
                    if (after.Value is Error e_after && before.Value is Error e_before)
                    {
                        // Errors are unique because they maintain a reference to their 
                        // complainants, so of course they will not be unique.
                        // TODO:  compare Error serialization equality.
                        Assert.AreEqual(e_before.GetType(), e_after.GetType());
                        Assert.AreEqual(e_before.Message, e_after.Message);
                        
                    }
                    else throw;
                }
                
            }
        }

        [TestMethod]
        public void Test_References()
        {
            // Set up an object to serialize
            HostContext oldContext = new HostContext();
            oldContext.SomeHosts.Add(new VariableHost());
            oldContext.SomeHosts.Add(new VariableHost());
            oldContext.SomeHosts[0].SomeProp.Contents = new Number(1);
            var expression = Parse.FromString("var0.Property * 10", null, oldContext);
            oldContext.SomeHosts[1].SomeProp.Contents = expression;

            // Show that the original state is as expected
            Assert.AreEqual(oldContext.SomeHosts[0].SomeProp.Value, 1);
            Assert.AreEqual(oldContext.SomeHosts[1].SomeProp.Value, 10);
            Assert.IsTrue(oldContext.SomeHosts[1].SomeProp.DependsOn(oldContext.SomeHosts[0].SomeProp));

            BinaryFormatter bf = new BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All));
            MemoryStream ms = new MemoryStream(new byte[2048]);
            bf.Serialize(ms, oldContext);
            ms.Seek(0, SeekOrigin.Begin);
            HostContext newContext = (HostContext)bf.Deserialize(ms);


        }

        [Serializable]
        private class HostContext : IContext
        {
            public readonly List<VariableHost> SomeHosts = new List<VariableHost>();
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
                    ctxt = SomeHosts[idx];
                    return true;
                }
                ctxt = default;
                return false;
            }
        }
        [Serializable]
        private class VariableHost : IContext
        {
            public readonly Variable SomeProp = new Variable();

            bool IContext.TryGetProperty(string path, out IEvaluateable property)
            {
                if (path.ToLower() == "property") { property = SomeProp; return true; }
                property = default;
                return false;
                
            }

            bool IContext.TryGetSubcontext(string path, out IContext ctxt)
            {
                ctxt = default;
                return false;
            }
        }
    }
}
