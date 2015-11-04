/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class MemberSelectorExpressionNode : ExpressionNode {
    public string Member { get; private set; }
    public ExpressionNode MemberOf { get; private set; }

    public MemberSelectorExpressionNode(ExpressionNode memberof, string membername) {
      this.Member = membername;
      this.MemberOf = memberof;
    }

    public override bool Validate(FaultHandler handleFault) {
      TypeNode type = MemberOf.GetTypeNode();
      if(!type.HasMember(Member))
        handleFault(ErrorCode.MEMBER_DOESNT_EXISIT, this, 
          "member '" + Member + "' doesn't exist");
      else if(type.IsClass()) {
        if(!type.GetMember(Member).IsPublic) {
          handleFault(ErrorCode.MEMBER_IS_PRIVATE, this, 
            FaultMessageBuilder.BuildPrivateMember(Member, type));
        }
      }
      return base.Validate(handleFault);
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, MemberOf);
    }

    public VarReferenceExpressionNode GetVarReference() {
      return MemberOf.GetTypeNode().GetMember(Member) as VarReferenceExpressionNode;
    }

    public override string ToString() {
      return String.Format("{0}.{1}", MemberOf.ToString(), Member);
    }

    public override TypeNode GetTypeNode() {
      if(MemberOf.GetTypeNode().HasMember(Member))
        return MemberOf.GetTypeNode().GetMemberType(Member);

      throw new SemanticErrorException(ErrorCode.MEMBER_DOESNT_EXISIT, this, 
        "compound doesn't have a member '" + Member + "'");
    }
  }
}
