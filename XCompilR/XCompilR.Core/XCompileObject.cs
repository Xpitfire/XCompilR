using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompilR.Core
{
    [Serializable]
    public abstract class XCompileObject : DynamicObject
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _properties.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _properties[binder.Name] = value;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(Dictionary<string, object>))
            {
                result = _properties;
                return true;
            }
            result = null;
            return base.TryConvert(binder, out result);
        }

        protected XCompileObject()
        {
            var attributeArray = (XCompileAttribute[])GetType().GetCustomAttributes(typeof(XCompileAttribute), false);

            if (attributeArray.Length != 1)
            {
                throw new XCompileException("Invalid attribute notation on target class!");
            }

            var attribute = attributeArray[0];
            attribute.BindMembers(this);
        }
    }
}
