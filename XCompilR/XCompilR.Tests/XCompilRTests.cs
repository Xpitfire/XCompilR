using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XCompile.ParserGen.CocoR;
using XCompilR.Core;

namespace XCompilR.Tests
{
    [TestClass]
    public class XCompilRTests
    {
        [XCompile("XCompilR.JavaScript", "test.js")]
        private class Demo : XCompileObject { }

        [TestMethod]
        public void TestDynamicObject()
        {
            dynamic d = new Demo();
            Assert.IsTrue(int.Parse(d.Ident) == 5);
        }

        [TestMethod]
        public void TestParserGen()
        {
            IParserGen parserGen = new CocoParserGen();
            AParser aParser = parserGen.CreateParser("JavaScript.atg");
            Assert.IsNotNull(aParser);
        }
    }
}
