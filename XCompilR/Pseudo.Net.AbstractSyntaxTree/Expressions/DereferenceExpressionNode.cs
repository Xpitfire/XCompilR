/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class DereferenceExpressionNode : ExpressionNode {
    public ExpressionNode Source { get; private set; }

    public DereferenceExpressionNode(ExpressionNode source) {
      this.Source = source;
    }

    public override TypeNode GetTypeNode() {
      TypeNode v = Source.GetTypeNode();
      if(v is PointerTypeNode)
        return (v as PointerTypeNode).PointsTo;

      throw new SemanticErrorException(ErrorCode.DEREFERENCE_NON_POINTER, this, 
        "Cannot dereferenz non pointer type");
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Source);
    }

    public override string ToString() {
      return String.Format("{0}: {1}->", Source.ToString(), 
        (Source.GetTypeNode() as PointerTypeNode).PointsTo);
    }
  }

}
