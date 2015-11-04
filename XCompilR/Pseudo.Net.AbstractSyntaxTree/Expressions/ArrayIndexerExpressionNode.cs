/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class ArrayIndexerExpressionNode : ExpressionNode {
    public ExpressionNode Index { get; private set; }
    public ExpressionNode Array { get; private set; }

    public ArrayIndexerExpressionNode(ExpressionNode array, ExpressionNode index) {
      this.Index = index;
      this.Array = array;
    }

    public override bool Validate(FaultHandler handleFault) {
      TypeNode t = this.Array.GetTypeNode();
      if(!t.IsArray() && !t.IsString())
        handleFault(ErrorCode.INDEXER_NOT_VALID_ON_TYPE, this, 
          String.Format("{0} isn't an array or string", Array));

      if(Index.GetTypeNode().Basetype != TypeNode.BaseType.INT) {
        handleFault(ErrorCode.ARRAY_INDEXER_NOT_INT, Index, 
          "array indexer must be an integer");
      } else if(t.IsArray() && Index.IsConst) {
        var arrayType = t as ArrayTypeNode;
        var idx = (int)Index.GetConstValueExpression().GetValue();
        if(!arrayType.IsInRange(idx))
          handleFault(ErrorCode.ARRAY_INDEXER_OUT_OF_RANGE, Index, 
            "array index is ot of range");
      }

      return base.Validate(handleFault);
    }

    public int GetDimensions() {
      ArrayIndexerExpressionNode i = this;
      int result = 1;
      while(i.Array is ArrayIndexerExpressionNode) {
        i = i.Array as ArrayIndexerExpressionNode;
        result++;
      }
      return result;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Index);
      visitor(this, Array);
    }

    public override TypeNode GetTypeNode() {
      return Array.GetTypeNode().GetArrayType();
    }

    public override string ToString() {
      return String.Format("{0}[{1}]", Array, Index);
    }
  }
}
