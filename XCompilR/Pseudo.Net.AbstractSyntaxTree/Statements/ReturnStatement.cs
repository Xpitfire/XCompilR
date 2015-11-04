/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class ReturnStatement : StatementNode {
    public ExpressionNode Expr { get; private set; }

    public ReturnStatement() { Expr = null; }
    
    public ReturnStatement(ExpressionNode expr) {
      this.Expr = expr;
    }

    public override bool ReturnValueOfType(TypeNode type) {
      if(type.Basetype == TypeNode.BaseType.VOID) {
        return Expr == null;
      }

      if(Expr == null)
        return false;

      return TypeNode.AreAssignable(type, Expr.GetTypeNode());
    }

    public override bool Validate(Node.FaultHandler handleFault) {
      if(GetCompleteReferencedFromList().OfType<MethodDefinitionNode>().First().ReturnType.IsVoid()) {
        if(Expr != null)
          handleFault(ErrorCode.INVOKE_SHOULD_RETURN_NO_VALUE, Expr, 
            "this function/method should not return a value");
      } else {
        if(Expr == null)
          handleFault(ErrorCode.INVOKE_SHOULD_RETURN_VALUE, Expr, 
            "this function/method should return a value");
      }
      return base.Validate(handleFault);
    }


    public override void Visit(Node.Visitor visitor) {
      if(Expr != null)
        visitor(this, Expr);
    }

    public override string ToString() {
      return "return";
    }
  }


}
