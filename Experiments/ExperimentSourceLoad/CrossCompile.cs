using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VeApps.Experiments;
using PostSharp.Patterns.Diagnostics;
using PostSharp.Extensibility;
using PostSharp.Constraints;

namespace ExperimentSourceLoad
{
    [Serializable]
    public enum BindingLanguage
    {
        JavaScript
    }

    public class CrossCompileException : Exception
    {
        public CrossCompileException() { }

        public CrossCompileException(string message) : base(message) { }

        public CrossCompileException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    [ContractClass(typeof (CrossCompileObject))]
    public class CrossCompileAttribute : Attribute
    {
        public BindingLanguage Language { get; set; }

        public string Source { get; set; }

        public CrossCompileAttribute(BindingLanguage bindingLanguage, string sourceFile)
        {
            Language = bindingLanguage;
            Source = sourceFile;
        }

        public void BindMembers(dynamic obj)
        {
            Type type = obj.GetType();

            if (!type.IsSubclassOf(typeof(CrossCompileObject)))
            {
                throw new CrossCompileException("Invalid base class inheritance! Class does not derive from CrossCompileObject!");
            }

            // TODO: change to attribute language reference and create a parser at runtime 
            Parser p = new Parser(new Scanner(Source));

            p.BindingObject(obj);
            p.Parse();
        }
    }

    [Serializable]
    public class CrossCompileObject : DynamicObject
    {
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return properties.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            properties[binder.Name] = value;
            return true;
        }

        public CrossCompileObject()
        {
            CrossCompileAttribute[] attributeArray =
                       (CrossCompileAttribute[])GetType().GetCustomAttributes(typeof(CrossCompileAttribute), false);

            if (attributeArray.Length != 1)
            {
                throw new CrossCompileException("Invalid attribute notation on target class!");
            }

            CrossCompileAttribute attribute = attributeArray[0];
            attribute.BindMembers(this);
        }
    }

    public class CrossCompileHandler
    {
        public static IEnumerable<Type> GetTypesWithCrossCompileAttribute(Assembly assembly)
        {
            return assembly.GetTypes().Where(type => 
                type.GetCustomAttributes(typeof(CrossCompileAttribute), true).Length > 0);
        }
        
    }

}
