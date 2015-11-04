/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
 
  public class ClassTypeNode : StructTypeNode {
    public bool IsAbstract;
    public ClassTypeNode BaseClass;

    public ClassTypeNode(Environment env, 
                         string name, 
                         ClassTypeNode baseClass, 
                         bool isAbstract)
      : base(env, name, BaseType.CLASS) {
      this.IsAbstract = isAbstract;
      this.BaseClass = baseClass;
      this.Environment.ClassNode = this;
    }

    public override void Visit(Node.Visitor visitor) {
      base.Visit(visitor);
      if(BaseClass != null)
        visitor(this, BaseClass);
    }

    public MethodDefinitionNode GetMethod(string name) {
      return Environment.FindFunctionDefinition(name, false);
    }

    public MethodDefinitionNode GetContructor() {
      return Environment.FindFunctionDefinition(Name, false);
    }

    public override bool Validate(FaultHandler handleFault) {
      try {
        GetContructor();
      } catch(NotDefinedException) {
        handleFault(ErrorCode.NOT_CONSTRUCTOR_DEFINED, this, 
          String.Format("no constructor for class '{0}' defined", FullName));
      }

      if(BaseClass != null && BaseClass.IsAbstract) {
        foreach(var m in BaseClass.Environment.OfType<MethodDefinitionNode>().Where(meth => meth.IsAbstract)) {
          try {
            MethodDefinitionNode myImpl = Environment.FindFunctionDefinition(m.Name, false);
            if(!myImpl.IsOverride)
              handleFault(ErrorCode.OVERRIDE_KEYWORD_NEEDED, this, 
                String.Format("use 'override' keyword to override abtract method '{0}'", FullName));
            else if(!myImpl.EqualHeader(m))
              handleFault(ErrorCode.OVERRIDE_DIFFERENT_SIGNATURE, this, 
                String.Format("method '{0}' has a different signature as abstract base definition", FullName));
          } catch(NotDefinedException) {
            handleFault(ErrorCode.NO_IMPL_FOR_ABSTRACT_METHOD, this, 
              String.Format("method '{0}' has a different signature as base definition", FullName));
          }
        }
      }

      if(!IsAbstract) {
        foreach(var m in Environment.OfType<MethodDefinitionNode>().Where(meth => meth.IsAbstract)) {
          handleFault(ErrorCode.ABSTRACT_IN_NON_ABSTACT_CLASS, this, 
            String.Format("the method '{0}' can not be abstract, because its class isn't", FullName));
        }
      }

      return base.Validate(handleFault);
    }

    public override bool HasMember(string name) {
      if(base.HasMember(name))
        return true;
      else if(BaseClass != null)
        return BaseClass.HasMember(name);
      else
        return false;
    }

    public override ExpressionNode GetMember(string name) {
      if(base.HasMember(name))
        return base.GetMember(name);
      else if(BaseClass != null)
        return BaseClass.GetMember(name);

      throw new NotDefinedException(this, name);
    }

    public override TypeNode GetMemberType(string name) {
      return GetMember(name).GetTypeNode();
    }

    public override string ToString() {
      return Environment.FullName;
    }

    internal bool IsA(ClassTypeNode baseClass) {
      if(baseClass == this)
        return true;

      if(this.BaseClass != null) {
        return this.BaseClass.IsA(baseClass);
      }

      return false;
    }
  }
}
