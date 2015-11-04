/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class UnaryExpressionNode : ExpressionNode {
    public enum Operator { UNDEF, PLUS, MINUS, NOT };
    public Operator Op { get; private set; }
    public ExpressionNode Expr { get; private set; }
    public UnaryExpressionNode(Operator op, ExpressionNode expr) {
      this.Op = op;
      this.Expr = expr;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Expr);
    }

    public override TypeNode GetTypeNode() {
      switch(Op) {
        case Operator.MINUS:
        case Operator.PLUS:
          if(Expr.GetTypeNode().IsNumeric())
            return Expr.GetTypeNode();
          break;

        case Operator.NOT:
          if(Expr.GetTypeNode().IsBool())
            return Expr.GetTypeNode();
          break;
      }

      throw new InvalidOperatorException(this, Op.ToString(), 
        Expr.GetTypeNode().ToString());
    }

    public override string ToString() {
      return String.Format("({0} {1})", Op.ToString(), Expr.ToString());
    }

    public override bool Validate(FaultHandler handleFault) {
      try {
        GetTypeNode();
      } catch(SemanticErrorException ex) {
        handleFault(ex.Code, this, ex.Message);
      }
      return base.Validate(handleFault);
    }

    public override bool IsConst {
      get {
        return Expr.IsConst;
      }
    }

    public override ConstExpressionNode GetConstValueExpression() {
      if(!IsConst)
        throw new InvalidOperationException("Binary Expression isn't const");

      ConstExpressionNode e = Expr.GetConstValueExpression();

      switch(Op) {
        case Operator.MINUS:
          return e.ReduceMinus();
        case Operator.PLUS:
          return e.ReducePlus();
        case Operator.NOT:
          return e.ReduceNot();
      }

      throw new NotImplementedException();
    }
  }

}
