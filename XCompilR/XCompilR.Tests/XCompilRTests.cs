using Microsoft.VisualStudio.TestTools.UnitTesting;
using XCompile.ParserGen.CocoR;
using XCompilR.Core;
using XCompilR.Library;
using XCompilR.ParserGen.Library;

namespace XCompilR.Tests
{
    [TestClass]
    public class XCompilRTests
    {
        
        [XCompile("XCompilR.JavaScript", "test.js", TargetMainClass = "Test", TargetNamespace = "Demo")]
        private class DemoBinding : XCompileObject { }

        [TestMethod]
        public void TestDynamicObject()
        {
            dynamic d = new DemoBinding();
            Assert.IsTrue(int.Parse(d.Ident) == 5);
        }
        
        [TestMethod]
        public void TestParserGenJavaScript()
        {
            IParserGen parserGen = new CocoParserGen();
            AParser parser = parserGen.CreateParser("JavaScript.ATG");
            Assert.IsNotNull(parser);
        }
        
    }
}
