using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Configuration;

namespace UnitTests
{
    [TestClass]
    public class Test_Configuration
    {
        [TestMethod]
        public void InitializeConfigurationAttribute()
        {

            ConfigurationAttribute a = new ConfigurationAttribute("SomeName", default, Flags.None, ">=1.2.3.4");
 
            
        }
    }
}
