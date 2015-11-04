/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class AssignStatementNode : StatementNode {
    public ExpressionNode RValue { get; private set; }
    public ExpressionNode LValue { get; private set; }

    public AssignStatementNode(ExpressionNode lvalue, ExpressionNode rvalue) {
      this.RValue = rvalue;
      this.LValue = lvalue;
    }

    public override bool Validate(FaultHandler handleFault) {
      if(!TypeNode.AreAssignable(LValue.GetTypeNode(), RValue.GetTypeNode()))
        handleFault(ErrorCode.IMPLICIT_CAST_NOT_SUPPORTED, this, 
          String.Format("'{0}' is not compatible with '{1}'", 
            LValue.GetTypeNode(), RValue.GetTypeNode()));

      if(LValue.IsConst)
        handleFault(ErrorCode.ASSIGN_VALUE_TO_CONST, LValue, 
          String.Format("cannot assign value to const expression '{0}'", LValue));

      return base.Validate(handleFault);
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, LValue);
      visitor(this, RValue);
    }

    public override string ToString() {
      return String.Format("{0} := {1}", LValue.ToString(), RValue.ToString());
    }
  }
}
