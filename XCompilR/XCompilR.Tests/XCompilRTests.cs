using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XCompile.ParserGen.CocoR;
using XCompilR.Core;

namespace XCompilR.Tests
{
    [TestClass]
    public class XCompilRTests
    {
        
        [XCompile("XCompilR.JavaScript", "test.js", "Test")]
        private class DemoJavaScript : XCompileObject { }

        [TestMethod]
        public void TestDynamicObject()
        {
            dynamic d = new DemoJavaScript();
            Assert.IsTrue(int.Parse(d.Ident) == 5);
        }

        [XCompile("XCompilR.PseudoNet", "hello.psc", "Hello")]
        private class DemoPseudoNet : XCompileObject { }

        [TestMethod]
        public void TestDynamicObjectPseudo()
        {
            dynamic d = new DemoPseudoNet();
            Assert.IsNotNull(d);
        }

        [TestMethod]
        public void TestParserGenJavaScript()
        {
            IParserGen parserGen = new CocoParserGen();
            AParser parser = parserGen.CreateParser("JavaScript.ATG");
            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void TestParserGenPseudoNet()
        {
            IParserGen parserGen = new CocoParserGen();
            AParser parser = parserGen.CreateParser("PSEUDO.atg");
            Assert.IsNotNull(parser);
        }
        
    }
}
