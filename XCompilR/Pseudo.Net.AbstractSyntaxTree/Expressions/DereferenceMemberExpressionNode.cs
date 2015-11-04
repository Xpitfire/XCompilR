/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class DereferenceMemberExpressionNode : DereferenceExpressionNode {
    public string MemberName { get; private set; }

    public DereferenceMemberExpressionNode(ExpressionNode source, string member)
      : base(source) {
      this.MemberName = member;
    }

    public override bool Validate(FaultHandler handleFault) {
      TypeNode v = Source.GetTypeNode();
      if(v is PointerTypeNode) {
        TypeNode ptrTo = (v as PointerTypeNode).PointsTo;

        if(!ptrTo.HasMember(MemberName))
          handleFault(ErrorCode.MEMBER_DOESNT_EXISIT, this, 
            FaultMessageBuilder.BuildNoMember(MemberName, ptrTo));
        else if(!ptrTo.GetMember(MemberName).IsPublic) {
          handleFault(ErrorCode.MEMBER_IS_PRIVATE, this, 
            FaultMessageBuilder.BuildPrivateMember(MemberName, ptrTo));
        }

      } else
        throw new InvalidOperatorException(this, "-> y", v.Basetype.ToString());

      return base.Validate(handleFault);
    }

    public override TypeNode GetTypeNode() {
      TypeNode v = Source.GetTypeNode();
      if(v is PointerTypeNode) {
        TypeNode ptrTo = (v as PointerTypeNode).PointsTo;

        if(!ptrTo.HasMember(MemberName))
          throw new NotDefinedException(this, MemberName);

        return ptrTo.GetMemberType(MemberName);
      }

      throw new InvalidOperatorException(this, "-> z", v.Basetype.ToString());
    }

    public ExpressionNode GetMember() {
      TypeNode v = Source.GetTypeNode();
      if(v is PointerTypeNode) {
        TypeNode ptrTo = (v as PointerTypeNode).PointsTo;

        if(!ptrTo.HasMember(MemberName))
          throw new NotDefinedException(this, MemberName);

        if(ptrTo is StructTypeNode)
          return ptrTo.GetMember(MemberName);
      }

      throw new InvalidOperatorException(this, "-> x", v.Basetype.ToString());
    }

    public override bool IsConst {
      get {
        return GetMember().IsConst;
      }
    }

    public override ConstExpressionNode GetConstValueExpression() {
      if(IsConst) {
        return GetMember().GetConstValueExpression();
      }
      return base.GetConstValueExpression();
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Source);
    }

    public override string ToString() {
      return String.Format("{0}->{1}", Source.ToString(), MemberName);
    }
  }


}
