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

        public void BindMembers(dynamic bindingObj)
        {
            Type type = bindingObj.GetType();

            if (!type.IsSubclassOf(typeof(XCompileObject)))
            {
                throw new XCompileException("Invalid base class inheritance! Class does not derive from CrossCompileObject!");
            }

            var parser = Language.Parser;
            parser.BindingObject = bindingObj;
            parser.Parse(Source);

        }
    }
}
