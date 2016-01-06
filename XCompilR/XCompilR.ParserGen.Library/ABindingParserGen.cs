using System;

namespace XCompilR.ParserGen.Library
{
    [Serializable]
    public abstract class ABindingParserGen
    {
        public abstract string ParserName { get; }
        public abstract string AssemblyName { get; }
        public abstract IParserGen Parser { get; }
    }
}
