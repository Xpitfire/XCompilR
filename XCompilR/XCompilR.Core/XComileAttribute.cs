using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XCompilR.Core
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    [ContractClass(typeof(XCompileObject))]
    public class XCompileAttribute : Attribute
    {
        private ABindingLanguage Language { get; }

        public string Source { get; set; }

        public XCompileAttribute(string bindingLanguageAssembly, string sourceFile)
        {
            Assembly assembly = Assembly.Load(bindingLanguageAssembly);
            Type type = assembly.GetType(bindingLanguageAssembly + ".BindingLanguage");
            Language = (ABindingLanguage)Activator.CreateInstance(type);

            Source = sourceFile;
        }

        public void BindMembers(dynamic obj)
        {
            Type type = obj.GetType();

            if (!type.IsSubclassOf(typeof(XCompileObject)))
            {
                throw new XCompileException("Invalid base class inheritance! Class does not derive from CrossCompileObject!");
            }
            
            Assembly assembly = Assembly.Load(Language.AssemblyName);

            type = assembly.GetType(Language.ScannerTypeName);
            IScanner scanner = (IScanner)Activator.CreateInstance(type, Source);
            type = assembly.GetType(Language.ParserTypeName);
            IParser parser = (IParser)Activator.CreateInstance(type, scanner);

            parser.BindingObject(obj);
            parser.Parse();
        }
    }
}
