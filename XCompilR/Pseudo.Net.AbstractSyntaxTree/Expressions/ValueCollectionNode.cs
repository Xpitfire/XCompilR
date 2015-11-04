/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class ValueCollectionNode : ExpressionNode {
    private List<ExpressionNode> values;

    public ValueCollectionNode() {
      values = new List<ExpressionNode>();
    }

    public void AddValues(ExpressionNode e) {
      values.Add(e);
    }

    public virtual ConstExpressionNode[] GetValues() {
      List<ConstExpressionNode> v = new List<ConstExpressionNode>();
      foreach (var r in values) {
        if (r is ValueCollectionNode)
          v.AddRange((r as ValueCollectionNode).GetValues());
        else if (r is ConstExpressionNode)
          v.Add(r as ConstExpressionNode);
        else
          throw new InvalidOperationException("ValueCollection items must be const!");
      }

      return v.ToArray();
    }

    public override bool Validate(Node.FaultHandler handleFault) {
      if (values.Any()) {
        TypeNode t = values[0].GetTypeNode();
        if (!t.HasIncrement())
          handleFault(ErrorCode.RANGE_NOT_SUPPORTED_FOR_TYPE, this, 
            String.Format("Can not create range of type '{0}'", t));

        foreach (var r in values) {
          if (!r.IsConst)
            handleFault(ErrorCode.VALUE_NOT_CONST, r, 
              "values of value collection must be const");

          if (t.Basetype != r.GetTypeNode().Basetype)
            handleFault(ErrorCode.TYPES_NOT_EQUAL, this, 
              "values in value collection must have the same type");
        }
      }
      return base.Validate(handleFault);
    }

    public override void Visit(Node.Visitor visitor) {
      foreach (var v in values)
        visitor(this, v);
    }

    public override TypeNode GetTypeNode() {
      if (values.Any())
        return values[0].GetTypeNode();

      return BaseTypeNode.VoidTypeNode;
    }

    public override bool IsConst {
      get {
        return true; // checked in Validate
      }
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      bool flag = true;
      foreach (var v in values) {
        if (flag)
          flag = false;
        else
          sb.Append(", ");
        sb.Append(v);
      }
      return sb.ToString();
    }
  }
}
