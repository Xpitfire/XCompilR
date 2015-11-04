/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class PointerTypeNode : TypeNode {
    public TypeNode PointsTo { get; private set; }

    public PointerTypeNode(TypeRepository repository, TypeNode pointsto)
      : base(repository, BaseType.PTR) {
      this.PointsTo = pointsto;
    }

    public PointerTypeNode(TypeNode pointsto) : this(null, pointsto) { }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, PointsTo);
    }

    public override string ToString() {
      return String.Format("->{0}", PointsTo.GetName());
    }

    public override bool Validate(FaultHandler handleFault) {
      if(PointsTo is UndefinedTypeNode) {
        UndefinedTypeNode p = PointsTo as UndefinedTypeNode;
        if(p.ResolvedTo != null)
          PointsTo = p.ResolvedTo;
      }

      return base.Validate(handleFault);
    }

    public override bool Equals(object obj) {
      if(this == obj)
        return true;
      if(obj is PointerTypeNode) {
        PointerTypeNode p = obj as PointerTypeNode;
        return p.PointsTo.Equals(this.PointsTo);
      }
      return false;
    }

    public override int GetHashCode() {
      return PointsTo.GetHashCode() & BaseType.PTR.GetHashCode();
    }
  }
}
