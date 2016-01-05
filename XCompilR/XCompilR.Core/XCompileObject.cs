using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using XCompilR.Library;

namespace XCompilR.Core
{
    [Serializable]
    public abstract class XCompileObject : DynamicObject
    {
        private readonly Dictionary<string, object> _members = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _members.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _members[binder.Name] = value;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(Dictionary<string, object>))
            {
                result = _members;
                return true;
            }
            result = null;
            return base.TryConvert(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Type dictType = typeof(Dictionary<string, object>);
            try
            {
                result = dictType.InvokeMember(
                             binder.Name,
                             BindingFlags.InvokeMethod,
                             null, _members, args);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
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
