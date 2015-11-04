/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class UndefinedTypeNode : TypeNode {
    public string Typename { get; private set; }

    private TypeNode resolvedTo;

    public TypeNode ResolvedTo {
      get {
        if(resolvedTo == null && repo.Exists(Typename)) {
          resolvedTo = repo.Find(Typename);
        }
        return resolvedTo;
      }
      set {
        resolvedTo = value;
        if(value != null)
          Basetype = value.Basetype;
      }
    }

    public UndefinedTypeNode(TypeRepository repository, string typename)
      : base(repository, BaseType.UNDEF) {
      this.Typename = typename;
      this.ResolvedTo = null;
      this.repo = repository;
    }

    public override void Visit(Node.Visitor visitor) {
      if(ResolvedTo != null) {
        ResolvedTo.Visit(visitor);
      }
    }

    public override bool HasMember(string name) {
      if(ResolvedTo != null)
        return ResolvedTo.HasMember(name);

      return base.HasMember(name);
    }

    public override TypeNode GetMemberType(string name) {
      if(ResolvedTo != null)
        return ResolvedTo.GetMemberType(name);

      return base.GetMemberType(name);
    }

    public override ExpressionNode GetMember(string name) {
      if(ResolvedTo != null)
        return ResolvedTo.GetMember(name);

      return base.GetMember(name);
    }

    public override string ToString() {
      if(ResolvedTo != null)
        return ResolvedTo.ToString();

      return String.Format("UNDEF Type '{0}'", Typename);
    }
  }


}
