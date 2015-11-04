/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public abstract class TypeNode : Node {
    public enum BaseType { INT, REAL, STRING, CHAR, BOOL, PTR, ARRAY, COMPOUND, VOID, UNDEF, NULL, CLASS, TYPE };

    public BaseType Basetype { get; protected set; }
    protected TypeRepository repo;

    public TypeNode(TypeRepository repository, BaseType basetype) {
      this.Basetype = basetype;
      this.repo = repository;
    }

    public TypeNode(BaseType basetype)
      : this(null, basetype) {

    }

    public void SetRepository(TypeRepository repository) {
      this.repo = repository;
    }

    //public abstract BaseType GetBaseType();
    public bool IsPtr() { return this.Basetype == BaseType.PTR; }
    public bool IsNumeric() { return this.Basetype == BaseType.REAL || this.Basetype == BaseType.INT; }
    public bool IsBool() { return this.Basetype == BaseType.BOOL; }
    public bool IsArray() { return this.Basetype == BaseType.ARRAY; }
    public bool IsCompound() { return this.Basetype == BaseType.COMPOUND; }
    public bool IsVoid() { return this.Basetype == BaseType.VOID; }
    public bool IsString() { return this.Basetype == BaseType.STRING; }
    public bool IsClass() { return this.Basetype == BaseType.CLASS; }
    public bool IsType() { return this.Basetype == BaseType.TYPE; }
    public bool HasIncrement() { return this.Basetype == BaseType.INT || this.Basetype == BaseType.CHAR; }
    public bool IsPrimitiv() {
      switch(Basetype) {
        case BaseType.BOOL:
        case BaseType.CHAR:
        case BaseType.INT:
        case BaseType.REAL:
          return true;
      }

      return false;
    }

    public virtual bool HasMember(string name) { return this.Basetype == BaseType.COMPOUND; }
    public virtual ExpressionNode GetMember(string name) {
      throw new SemanticErrorException(ErrorCode.TYPE_HAS_NO_MEMBERS, this, 
        "type '" + Basetype.ToString() + "' doesn't contain members");
    }
    public virtual TypeNode GetMemberType(string name) {
      throw new SemanticErrorException(ErrorCode.TYPE_HAS_NO_MEMBERS, this, 
        "type '" + Basetype.ToString() + "' doesn't contain members");
    }
    public virtual TypeNode GetArrayType() {
      throw new SemanticErrorException(ErrorCode.TYPE_IS_NO_ARRAY, this, 
        "type '" + Basetype.ToString() + "' isn't an array");
    }

    static public bool AreNumeric(TypeNode n1, TypeNode n2) {
      return n1.IsNumeric() && n2.IsNumeric();
    }

    static public bool AreBool(TypeNode n1, TypeNode n2) {
      return n1.IsBool() && n2.IsBool();
    }

    static public bool AreAssignable(TypeNode dest, TypeNode src) {
      if(dest.Basetype == BaseType.VOID)
        return true;

      if(dest.Basetype == src.Basetype) {
        if(dest.IsPtr()) {
          TypeNode d = (dest as PointerTypeNode).PointsTo;
          TypeNode s = (src as PointerTypeNode).PointsTo;
          if(s.IsVoid())
            return true;

          return AreAssignable(d, s);
        } else if(dest.IsClass()) {
          // src is dest;   Derived is Base
          ClassTypeNode s = src as ClassTypeNode;
          return s.IsA(dest as ClassTypeNode);
        } else if(dest.IsCompound()) {
          StructTypeNode s = src as StructTypeNode;
          return s.Equals(dest);
        }

        return true;
      }

      if(AreNumeric(dest, src)) {
        if(dest.Basetype == BaseType.REAL)
          return true;
      }
      return false;
    }

    private string GetTypeName(bool createNameIfNotInRepo) {
      if(createNameIfNotInRepo)
        return String.Format("{0}{1}", Basetype, GetHashCode());
      else
        return ToString();
    }

    public virtual string GetName(bool createNameIfNotInRepo = false) {
      try {
        if(repo != null)
          return repo.GetName(this);
        else
          return GetTypeName(createNameIfNotInRepo);
      } catch(NotDefinedException) {
        return GetTypeName(createNameIfNotInRepo);
      }
    }

    public virtual bool HasName() {
      try {
        if(repo != null)
          return repo.GetName(this).Length > 0;
        else
          return false;
      } catch(NotDefinedException) {
        return false;
      }
    }
  }

}
