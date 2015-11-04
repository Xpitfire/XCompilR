/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class MethodDefinitionNode : Node {
    public delegate bool Validator(ArgumentList parameter, Node caller = null);

    static public MethodDefinitionNode undefMethod = new MethodDefinitionNode(null) { 
      ParameterValidator = (p, c) => { return false; } 
    };

    public class ArgDef {
      public ArgumentDirection dir;
      public TypeNode type;
    }

    public Environment Environment { get; private set; }
    public IDictionary<VarReferenceExpressionNode, ArgDef> Parameter { get; private set; }
    public Validator ParameterValidator { get; set; }
    public Func<ArgumentList, TypeNode> ReturnTypeResolver;

    public bool IsDefined { get { return Environment != null; } }
    public bool IsStatic;
    public bool IsAbstract;
    public bool IsOverride;
    public bool IsPublic;

    public MethodDefinitionNode(Environment env) {
      this.Environment = env;
      this.Parameter = new Dictionary<VarReferenceExpressionNode, ArgDef>();
      this.ParameterValidator = null;
      this.IsStatic = false;
      this.IsAbstract = false;
      this.IsOverride = false;
      this.IsPublic = true;

      ReturnTypeResolver = (args) => {
        return ReturnType;
      };
    }

    public string FullName {
      get { return Environment.FullName; }
    }

    public string Name {
      get { return Environment.Name; }
      set { Environment.Name = value; }
    }

    public TypeNode ReturnType { get; set; }

    public Environment GetClassEnvironment() {
      if(Environment.IsClassScope())
        return Environment.GetClassEnvironment();

      return null;
    }

    public bool IsClassMethod() {
      return Environment.IsClassScope();
    }

    private StatementNode body;
    public StatementNode Body {
      get { return body; }
      set {
        body = value;

      }
    }

    public override bool Validate(FaultHandler handleFault) {
      if(ReturnType != null && !ReturnType.IsVoid()) {
        if(body is BlockStatementNode) {
          BlockStatementNode b = body as BlockStatementNode;
          if(b.ReturnValueOfType(BaseTypeNode.VoidTypeNode))
            handleFault(ErrorCode.INVOKE_SHOULD_RETURN_VALUE, b, 
              String.Format("function x '{0}' must return a value of type '{1}'", 
                FullName, ReturnType.ToString()));
          else
            if(!b.ReturnValueOfType(ReturnType))
              handleFault(ErrorCode.INVOKE_RETURN_WRONG_TYPE, b, 
                String.Format("function '{0}' must return a value of type '{1}'", 
                  FullName, ReturnType.ToString()));
        }
      }

      foreach(var v in Environment.OfType<VarReferenceExpressionNode>()) {
        if(v.IsStatic && v.Value==null) {
          handleFault(ErrorCode.NO_VALUE_DEFINED, v, 
            "static variable needs a default value");
        }
      }

      if(IsClassMethod()) {
        ClassTypeNode baseClass = GetClassEnvironment().ClassNode.BaseClass;

        if(IsOverride) {
          if(baseClass == null) {
            handleFault(ErrorCode.OVERRIDE_IN_BASECLASS, this, 
              String.Format("modifier override is not allowed in a base class in '{0}' ", FullName));
          } else {
            if(!baseClass.HasMember(Name)) {
              handleFault(ErrorCode.NOTHING_TO_OVERRIDE, this, 
                String.Format("definition for '{0}' doesn't exisit in base class ", FullName));
            } else {
              MethodDefinitionNode baseMethod = baseClass.GetMethod(Name);
              if(!baseMethod.IsPublic) {
                handleFault(ErrorCode.OVERRIDE_PRIVATE, this, 
                  String.Format("cannot override private method '{0}'", FullName));
              } else
                if(!baseMethod.EqualHeader(this)) {
                  handleFault(ErrorCode.OVERRIDE_DIFFERENT_SIGNATURE, this, 
                    String.Format("method '{0}' has a different signature as base definition", FullName));
                }
            }
          }

          if(baseClass != null && body!=null) {
            foreach(var m in body.GetStatementExpressionNodes()
              .OfType<MethodReferenceExpressionNode>()
              .Where(x => x.Method.IsAbstract && x.Method.IsClassMethod())) {
                if(m.Method.GetClassEnvironment() == baseClass.Environment)
                  handleFault(ErrorCode.CALL_ABSTRACT_BASE_MEMBER, m.Method, 
                    "cannot call an abstract base member");
              
            }
          }
        }
      }

      return base.Validate(handleFault);
    }

    public IList<VarReferenceExpressionNode> GetVariables(bool inclusiveHeader = false) {
      var all = Environment.OfType<VarReferenceExpressionNode>().ToList();
      if(inclusiveHeader)
        return all;

      return all.Where(n => !Parameter.ContainsKey(n)).ToList();
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Environment);
      if(ReturnType != null)
        visitor(this, ReturnType);
      if(this.Body != null)
        visitor(this, Body);
    }

    public void AddParameter(ArgumentDirection dir, 
                             string name, 
                             TypeNode type, 
                             ConstExpressionNode value = null) {
      Environment.AddVariable(name, type, false, false, value);
      Parameter.Add(Environment.FindVariable(name), new ArgDef { dir = dir, type = type });
    }

    public void AddVariable(string name, 
                            TypeNode type, 
                            bool isStatic, 
                            ConstExpressionNode value = null) {
      Environment.AddVariable(name, type, isStatic, false, value);
    }

    public void AddVariable(VarReferenceExpressionNode varRef) {
      Environment.AddVariable(varRef);
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      if(IsPublic)
        sb.Append("public ");
      else
        sb.Append("private ");

      if(IsStatic)
        sb.Append("static ");

      if(IsAbstract)
        sb.Append("abstract ");

      if(IsOverride)
        sb.Append("override ");

      sb.Append(FullName);
      sb.Append("( ");
      foreach(var p in Parameter.Values) {
        sb.AppendFormat("{0}:{1} ", p.dir, p.type.GetName());
      }
      sb.Append(")");
      return sb.ToString();
    }

    public bool EqualHeader(MethodDefinitionNode obj) {
      if(obj == this)
        return true;

      MethodDefinitionNode m = obj as MethodDefinitionNode;
      if(!m.Name.Equals(this.Name))
        return false;

      if(m.Parameter.Count != Parameter.Count)
        return false;

      if(!m.ReturnType.Equals(ReturnType))
        return false;

      var pn1 = m.Parameter.Keys.Select(v => v.Name).ToArray();
      var pn2 = Parameter.Keys.Select(v => v.Name).ToArray();
      var pv1 = m.Parameter.Values.ToArray();
      var pv2 = Parameter.Values.ToArray();

      for(int i = 0; i < pn1.Length; i++) {
        if(!pn1[i].Equals(pn2[i]))
          return false;

        if(pv1[i].dir != pv2[i].dir)
          return false;

        if(!pv1[i].type.Equals(pv2[i].type))
          return false;
      }

      return true;
    }

    public bool ValidateParameter(ArgumentList parameter, Node caller = null) {
      if(this.Parameter.Count != parameter.Count)
        throw new SemanticErrorException(ErrorCode.INVOKE_INVALID_PARAMETER_COUNT, caller, 
          String.Format("function or method takes {0} argument, but is called with {1}", 
            this.Parameter.Count, parameter.Count));

      var p2Values = parameter.Values.ToArray();
      
      var p1Types = this.Parameter.Values
                        .Select<MethodDefinitionNode.ArgDef, TypeNode>((v) => { return v.type; })
                        .ToArray();
                        
      var p2Types = p2Values.Select<ExpressionNode, TypeNode>((v) => { return v.GetTypeNode(); })
                            .ToArray();

      var p1Dir = this.Parameter.Values
                      .Select<MethodDefinitionNode.ArgDef, ArgumentDirection>((v) => { return v.dir; })
                      .ToArray();
                      
      var p2Dir = parameter.Directions.ToArray();

      for(int i = 0; i < p1Types.Length; i++) {
        if(!TypeNode.AreAssignable(p1Types[i], p2Types[i]))
          throw new SemanticErrorException(ErrorCode.INVOKE_INVALID_PARAMETER_TYPE, caller, 
            string.Format("parameter {0} takes values of '{1}', but is called with '{2}'", i + 1, p1Types[i], p2Types[i]));

        if(p1Dir[i] != p2Dir[i])
          throw new SemanticErrorException(ErrorCode.INVOKE_INVALID_PARAMETER_DIR, caller, 
            string.Format("parameter {0} takes {1} values, but is called with '{2}'", i + 1, p1Dir[i], p2Dir[i]));

        if(p1Dir[i] != ArgumentDirection.IN) {
          if(p2Values[i].IsConst)
            throw new SemanticErrorException(ErrorCode.ASSIGN_VALUE_TO_CONST, caller, 
              string.Format("parameter {0} must be an assignable variable", i + 1));
        }
      }
      return true;
    }

  }


}
