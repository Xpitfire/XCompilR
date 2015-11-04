/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
 
  public class PreDefinedMethodNode : MethodDefinitionNode {
    public PreDefinedMethodNode(Environment env) : base(env) { }
  }

  public static class MethodeBuilder {
    static public MethodDefinitionNode Read(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Read";
      m.ReturnType = BaseTypeNode.VoidTypeNode;
      m.ParameterValidator = (p, c) => {
        if(p.Count == 0)
          return true;

        if(p.Count == 1 && p.First().Key.GetTypeNode() is BaseTypeNode)
          return true;

        return false;
      };
      //  m.AddParameter(ArgumentDirection.OUT, "readval", BaseTypeNode.VoidTypeNode);
      return m;
    }

    static public MethodDefinitionNode Write(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Write";
      m.ReturnType = BaseTypeNode.VoidTypeNode;
      m.ParameterValidator = (p, c) => { return true; };
      return m;
    }

    static public MethodDefinitionNode WriteLn(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "WriteLn";
      m.ReturnType = BaseTypeNode.VoidTypeNode;
      m.ParameterValidator = (p, c) => { return true; };
      return m;
    }

    static public MethodDefinitionNode Real(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Real";
      m.ReturnType = BaseTypeNode.RealTypeNode;
      m.AddParameter(ArgumentDirection.IN, "val", BaseTypeNode.IntegerTypeNode);
      return m;
    }

    static public MethodDefinitionNode Int(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Int";
      m.ReturnType = BaseTypeNode.IntegerTypeNode;
      m.ParameterValidator = (p, c) => {
        if(p.Count == 0)
          return false;

        if(p.Count == 1) {
          var t = p.First().Key.GetTypeNode();
          if(t.Basetype == TypeNode.BaseType.INT
            || t.Basetype == TypeNode.BaseType.REAL
            || t.Basetype == TypeNode.BaseType.CHAR
            || t.Basetype == TypeNode.BaseType.BOOL
            )
            return true;
        }

        return false;
      };

      //m.AddParameter(ArgumentDirection.IN, "val", BaseTypeNode.CharTypeNode);
      return m;
    }

    static public MethodDefinitionNode Chr(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Chr";
      m.ReturnType = BaseTypeNode.CharTypeNode;
      m.AddParameter(ArgumentDirection.IN, "val", BaseTypeNode.IntegerTypeNode);
      return m;
    }

    static public MethodDefinitionNode Length(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Length";
      m.ReturnType = BaseTypeNode.IntegerTypeNode;
      m.AddParameter(ArgumentDirection.IN, "val", BaseTypeNode.StringTypeNode);
      return m;
    }

    static public MethodDefinitionNode Low(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Low";
      m.ReturnType = BaseTypeNode.IntegerTypeNode;
      m.ParameterValidator = (parameter, caller) => {
        if(parameter.Count != 1)
          return false;

        var p = parameter.First();
        if(p.Value != ArgumentDirection.IN)
          return false;

        TypeNode t = p.Key.GetTypeNode();
        return (t.IsNumeric() || t.IsArray());
      };

      return m;
    }

    static public MethodDefinitionNode High(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "High";
      m.ReturnType = BaseTypeNode.IntegerTypeNode;
      m.ParameterValidator = (parameter, caller) => {
        if(parameter.Count != 1)
          return false;

        var p = parameter.First();
        if(p.Value != ArgumentDirection.IN)
          return false;

        TypeNode t = p.Key.GetTypeNode();
        return (t.IsNumeric() || t.IsArray());
      };

      return m;
    }

    static public MethodDefinitionNode New(Environment env) {
      PreDefinedMethodNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "New";

      m.ReturnTypeResolver = (args) => {
        return new PointerTypeNode((args.First().Key as TypeReferenceExpressionNode).Type);
      };

      m.ReturnType = new PointerTypeNode(BaseTypeNode.VoidTypeNode);
      m.ParameterValidator = (p, c) => {
        var values = p.Values.ToArray();

        if(p.Directions.Any((d) => { return d != ArgumentDirection.IN; }))
          return false;

        if(values.Length == 0 || !(values[0] is TypeReferenceExpressionNode))
          return false;

        TypeReferenceExpressionNode typeref = values[0] as TypeReferenceExpressionNode;

        if(typeref.Type is ClassTypeNode) {
          ClassTypeNode ct = typeref.Type as ClassTypeNode;
          if(ct.IsAbstract)
            throw new SemanticErrorException(ErrorCode.NEW_ABSTRACT, m, "New is not allowed for abstract classes");

          MethodDefinitionNode ctor = ct.GetContructor();
          ArgumentList args = new ArgumentList(p);
          args.Remove(0); // remove first parameter, because this is the classtype
          return ctor.ValidateParameter(args, c);
        } else {
          return values.Length == 1;
        }
      };
      m.AddParameter(ArgumentDirection.IN, "type", BaseTypeNode.TypeTypeNode);
      return m;
    }

    static public MethodDefinitionNode Dispose(Environment env) {
      MethodDefinitionNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = "Dispose";
      m.ReturnType = BaseTypeNode.VoidTypeNode;
      m.AddParameter(ArgumentDirection.IN, "ptr", new PointerTypeNode(BaseTypeNode.VoidTypeNode));
      return m;
    }

    static public MethodDefinitionNode ClassCast(Environment env, ClassTypeNode classtype) {
      PreDefinedMethodNode m = new PreDefinedMethodNode(new Environment(env));
      m.Name = classtype.Name;

      m.ReturnType = new PointerTypeNode(classtype);
      m.ParameterValidator = (p, c) => {
        if(p.Count != 1)
          return false;

        TypeNode ptr = p.First().Key.GetTypeNode();
        if(ptr.IsPtr()) {
          TypeNode t = (ptr as PointerTypeNode).PointsTo;

          return t.IsClass();

        }
        return false;
      };
      m.AddParameter(ArgumentDirection.IN, "ptr", new PointerTypeNode(BaseTypeNode.VoidTypeNode));
      return m;
    }
  }



}
