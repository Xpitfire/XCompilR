/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class RangeExpressionNode : ValueCollectionNode {
    public ExpressionNode From { get; private set; }
    public ExpressionNode To { get; private set; }
    
    public RangeExpressionNode(ExpressionNode from, ExpressionNode to) {
      this.From = from;
      this.To = to;
    }

    public override ConstExpressionNode[] GetValues() {
      ConstExpressionNode from = From.GetConstValueExpression();
      ConstExpressionNode to = To.GetConstValueExpression();

      List<ConstExpressionNode> values = new List<ConstExpressionNode>();
      values.Add(from);
      while (!from.Equals(to)) {
        from = from.Increment();
        values.Add(from);
      }

      return values.ToArray();
    }

    public override bool IsConst {
      get {
        return From.IsConst && To.IsConst;
      }
    }

    public override bool Validate(FaultHandler handleFault) {
      if (!IsConst)
        handleFault(ErrorCode.RANGE_NOT_CONST, this, 
          "Range must be const!");

      if (From.GetTypeNode().Basetype != To.GetTypeNode().Basetype)
        handleFault(ErrorCode.TYPES_NOT_EQUAL, this, 
          "Range from/to must have the same type");

      if (!From.GetTypeNode().HasIncrement())
        handleFault(ErrorCode.INVALID_TYPE, this, 
          String.Format("Can not create range of type '{0}'", From.GetTypeNode()));

      ConstExpressionNode from = From.GetConstValueExpression();
      ConstExpressionNode to = To.GetConstValueExpression();
      if (from.ReduceGreater(to).Value)
        handleFault(ErrorCode.RANGE_BAD_LIMIT, this, 
          "upper limit is smaller than the lower");

      return base.Validate(handleFault);
    }

    public override void Visit(Visitor visitor) {
      visitor(this, From);
      visitor(this, To);
    }

    public override TypeNode GetTypeNode() {
      return From.GetTypeNode();
    }

    public override string ToString() {
      return String.Format("{0}..{1}", From, To);
    }
  }
}
