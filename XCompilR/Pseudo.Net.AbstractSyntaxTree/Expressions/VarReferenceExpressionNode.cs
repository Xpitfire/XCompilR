/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class VarReferenceExpressionNode : ExpressionNode {
    public string Name { get; private set; }
    public TypeNode Type { get; private set; }
    public ExpressionNode Value { get; set; }
    private bool isConst;
    private bool isPublic;
    public override bool IsConst { get { return isConst; } }
    public bool IsStatic { get; set; }
    public override bool IsPublic { get { return isPublic; } }
    public Environment Environment { get; private set; }

    public VarReferenceExpressionNode(string name, 
                                      TypeNode type, 
                                      ExpressionNode value, 
                                      Environment env, 
                                      bool isConst = false, 
                                      bool isStatic = false, 
                                      bool isPublic = true) {
      this.Name = name;
      this.Type = type;
      this.Value = value;
      this.isConst = isConst;
      this.IsStatic = isStatic;
      this.isPublic = isPublic;
      this.Environment = env;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Type);
      if(Value != null)
        visitor(this, Value);
    }

    public override TypeNode GetTypeNode() {
      return Type;
    }

    public override string ToString() {
      return String.Format("var({0})", Name);
    }

    public string FullName { 
      get { 
        return this.Environment.FullName + "." + Name; 
      } 
    }

    public override bool Validate(FaultHandler handleFault) {
      if(GetReferenceFromList().Length == 1 && !IsConst) {
        if(!Environment.GetReferenceFromList()
          .OfType<StructTypeNode>().Any() &&
          !Environment.GetReferenceFromList()
          .OfType<PreDefinedMethodNode>().Any())
          handleFault(ErrorCode.VAR_NEVER_REFERENCED, this, 
            String.Format("'{0}' is never referenced", FullName), false);
      }

      if(Value != null) {
        if(!Value.IsConst)
          handleFault(ErrorCode.VALUE_NOT_CONST, Value, 
            "initializer must be constant");
      }
    
      return base.Validate(handleFault);
    }

    public override ConstExpressionNode GetConstValueExpression() {
      if(IsConst) {
        return Value.GetConstValueExpression();
      }

      return base.GetConstValueExpression();
    }

  }

}
