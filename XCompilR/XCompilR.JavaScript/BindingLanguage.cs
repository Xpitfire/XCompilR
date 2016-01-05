using XCompilR.Library;

namespace XCompilR.JavaScript
{
    public class BindingLanguage : ABindingLanguage
    {
        public override string LanguageName { get; } = "JavaScript";
        public override string AssemblyName { get; } = "XCompilR.JavaScript";
        public override AParser Parser { get; }

        public BindingLanguage()
        {
            var p = new Parser();
            p.InitParser(new Scanner());
            Parser = p;
        }
    }
}
