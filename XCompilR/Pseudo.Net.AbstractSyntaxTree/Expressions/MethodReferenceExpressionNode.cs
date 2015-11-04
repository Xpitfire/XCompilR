/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class MethodReferenceExpressionNode : ExpressionNode {
    public string Name { get; private set; }
    private Environment env;
    private MethodDefinitionNode method;

    public override bool IsPublic {
      get {
        return Method.IsPublic;
      }
    }

    public MethodDefinitionNode Method {
      get {
        if(method == MethodDefinitionNode.undefMethod) {
          try {
            method = env.FindFunctionDefinition(Name);
          } catch(NotDefinedException) {
            // at least we tried. scan later for undefined methods
          }
        }
        return method;
      }
      private set { method = value; }
    }

    public MethodReferenceExpressionNode(Environment env, MethodDefinitionNode method) {
      this.env = env;
      this.Method = method;
      this.Name = method.Name;
    }

    public MethodReferenceExpressionNode(Environment env, string name) {
      this.env = env;
      this.Name = name;
      this.Method = MethodDefinitionNode.undefMethod;
    }

    public override TypeNode GetTypeNode() {
      return Method.ReturnType;
    }

    public override void Visit(Node.Visitor visitor) {
      if(Method.IsDefined)
        visitor(this, Method);
    }

    public override string ToString() {
      if(!Method.IsDefined) {
        return Name + " (not resolved)";
      }

      return Method.FullName;
    }
  }
}
