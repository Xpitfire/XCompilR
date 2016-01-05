using System;

namespace XCompilR.Library
{
    [Serializable]
    public abstract class ABindingLanguage
    {
        public abstract string LanguageName { get; }
        public abstract string AssemblyName { get; }
        public abstract AParser Parser { get; }
    }
}
