using XCompilR.ParserGen.Library;

namespace XCompile.ParserGen.CocoR
{
    public class BindingParserGen : ABindingParserGen
    {
        public override string ParserName { get; } = "CocoRParserGen";
        public override string AssemblyName { get; } = "XCompilR.ParserGen.CocoR";
        public override IParserGen Parser { get; }

        public BindingParserGen()
        {
            Parser = new CocoParserGen();
        }
    }
}
