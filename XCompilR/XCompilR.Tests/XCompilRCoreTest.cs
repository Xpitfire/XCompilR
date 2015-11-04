using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XCompilR.Core;

namespace XCompilR.Tests
{
    [TestClass]
    public class XCompilRCoreTest
    {
        [XCompile("XCompilR.JavaScript", "test.js")]
        private class Demo : XCompileObject { }

        [TestMethod]
        public void TestDynamicObject()
        {
            dynamic d = new Demo();
            Assert.IsTrue(int.Parse(d.Ident) == 5);
        }
    }
}
