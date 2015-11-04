/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class BaseTypeNode : TypeNode {
    static public readonly BaseTypeNode IntegerTypeNode = new BaseTypeNode(BaseType.INT);
    static public readonly BaseTypeNode RealTypeNode = new BaseTypeNode(BaseType.REAL);
    static public readonly BaseTypeNode StringTypeNode = new BaseTypeNode(BaseType.STRING);
    static public readonly BaseTypeNode CharTypeNode = new BaseTypeNode(BaseType.CHAR);
    static public readonly BaseTypeNode BoolTypeNode = new BaseTypeNode(BaseType.BOOL);
    //   static public readonly BaseTypeNode PtrTypeNode = new BaseTypeNode(BaseType.PTR);
    static public readonly BaseTypeNode VoidTypeNode = new BaseTypeNode(BaseType.VOID);
    static public readonly BaseTypeNode TypeTypeNode = new BaseTypeNode(BaseType.TYPE);


    private BaseTypeNode(BaseType basetype)
      : base(basetype) {

    }

    public override void Visit(Node.Visitor visitor) {
    }

    public override TypeNode GetArrayType() {
      if(this.Basetype == BaseType.STRING)
        return BaseTypeNode.CharTypeNode;

      return base.GetArrayType();
    }

    public override string ToString() {
      return Basetype.ToString();
    }

    static public TypeNode GetMax(TypeNode n1, TypeNode n2) {
      if(AreNumeric(n1, n2)) {
        if(n1.Basetype == BaseType.REAL || n2.Basetype == BaseType.REAL)
          return RealTypeNode;

        return IntegerTypeNode;
      }

      if(n1.Basetype != n2.Basetype)
        throw new SemanticErrorException(ErrorCode.IMPLICIT_CAST_NOT_SUPPORTED, 
          String.Format("Types '{0}' and '{1}' are not compatible", n1.Basetype, n2.Basetype));

      return n1;
    }

  }

}
