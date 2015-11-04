/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class ForStatement : StatementNode {
    public ExpressionNode Start { get; private set; }
    public ExpressionNode End { get; private set; }
    public StatementNode Stat { get; private set; }
    public VarReferenceExpressionNode Var { get; private set; }
    
    public String Varname { get { return Var.Name; } }
    public bool CountDownward { get; private set; }

    public ForStatement(VarReferenceExpressionNode var, 
                        ExpressionNode start, 
                        ExpressionNode end, 
                        StatementNode stat, 
                        bool countDownward) {
      this.Var = var;
      this.Start = start;
      this.End = end;
      this.Stat = stat;
      this.CountDownward = countDownward;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Start);
      visitor(this, End);
      visitor(this, Stat);
      visitor(this, Var);
    }

    public override bool Validate(Node.FaultHandler handleFault) {
      if(!BaseTypeNode.AreAssignable(Var.GetTypeNode(), Start.GetTypeNode()))
        handleFault(ErrorCode.IMPLICIT_CAST_NOT_SUPPORTED, Start, 
          "for start value must be of the same type as counter variable");

      if(!BaseTypeNode.AreAssignable(Var.GetTypeNode(), End.GetTypeNode()))
        handleFault(ErrorCode.IMPLICIT_CAST_NOT_SUPPORTED, End, 
          "for end value must be of the same type as the counter variable");

      return base.Validate(handleFault);
    }

    public override string ToString() {
      return "for";
    }

    public override bool IsInLoopContext() {
      return true;
    }
  }
}
