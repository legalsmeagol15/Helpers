using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dependency;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class Test_Dependency
    {
        [TestMethod]
        public void TestMethod1()
        {
        }


        
        public class DemoDrawing : IContext
        {
            public IContext Parent { get => null; }

            private IDictionary<string, IContext> layers = new Dictionary<string, IContext>();
            private IDictionary<string, DemoColor> palette = new Dictionary<string, DemoColor>();
            
            bool IContext.Get(string name, out object subcontext)
            {
                name = name.ToLower();
                if (layers.TryGetValue(name, out subcontext)) return true;
                if (!name.StartsWith("layer")) { subcontext = null; return false; }
                layers.Add(name, subcontext = new DemoLayer());
                return true;
            }

            bool IContext.Get(string name, out Variable variable)
            {
                throw new NotImplementedException();
            }
            
            
        }

        public class DemoLayer : IContext
        {
            public object Parent { get; private set; }

            Variable _Name = null;
            private Dictionary<string, DemoLine> lines = new Dictionary<string, DemoLine>();

            public DemoLayer(object parent) { this.Parent = parent; }

            bool IContext.Get(string name, out Variable variable)
            {
                if (name.ToLower() != "name") { variable = null;return false; }
                if (_Name == null)
                {
                    _Name = new Variable("\"namedLayer\"");
                }
            }

            bool IContext.Get(string name, out object subcontext)
            {
                
            }

            
            
        }

        public class DemoLine
        {
            [DependencyProperty]
            public double X0 = 1;

            [DependencyProperty]
            public double Y0 = 1;

            [DependencyProperty]
            public double X1 { get; set; } = 2;

            [DependencyProperty]
            public double Y1 { get; set; } = 2;

            [DependencyProperty]
            public DemoColor Color = new DemoColor(1.0, 0.8, 0.6, 0.4);

        }

        public struct DemoColor
        {
            [DependencyProperty]
            public double A { get; set; }

            [DependencyProperty]
            public double R;

            [DependencyProperty]
            public double G;

            [DependencyProperty]
            public double B;

            public DemoColor(double a, double r, double g, double b) { this.A = a; this.R = r; this.G = g; this.B = b; }
        }
    }
}
