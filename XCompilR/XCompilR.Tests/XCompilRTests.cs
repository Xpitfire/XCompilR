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
        
        [XCompile("XCompilR.JavaScript", "test.js", "Test", "Demo")]
        private class DemoJavaScript : XCompileObject { }

        [TestMethod]
        public void TestDynamicObject()
        {
            dynamic d = new DemoJavaScript();
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
