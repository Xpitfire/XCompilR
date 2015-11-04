/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;
using System.Collections;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class Environment : Node, IEnumerable<Node> {
    private IDictionary<string, VarReferenceExpressionNode> variables;
    private IDictionary<string, VarReferenceExpressionNode> constvar;
    private IDictionary<string, MethodDefinitionNode> functions;
    private IDictionary<MethodDefinitionNode, MethodReferenceExpressionNode> functionsRef;
    public Environment Prev { get; private set; }
    public bool IsClass { get; private set; }
    public ClassTypeNode ClassNode { get; set; }

    public string Name { get; set; }
    public string FullName {
      get {
        if(Prev != null)
          return Prev.FullName + "." + Name;
        else
          return Name;
      }
    }

    public Environment(Environment prev, bool isClass = false) : this(null, prev, isClass) { }

    public Environment(string name, bool isClass = false) {
      Prev = null;
      this.Name = name;
      variables = new Dictionary<string, VarReferenceExpressionNode>();
      constvar = new Dictionary<string, VarReferenceExpressionNode>();
      this.functions = new Dictionary<string, MethodDefinitionNode>();
      this.functionsRef = new Dictionary<MethodDefinitionNode, MethodReferenceExpressionNode>();
      foreach(var c in constvar) { }

      this.IsClass = isClass;
    }

    public Environment(string name, Environment prev, bool isClass = false)
      : this(name, isClass) {
      this.Prev = prev;
    }

    public void AddVariable(string name, 
                            TypeNode type, 
                            bool isStatic, 
                            bool isPublic, 
                            ExpressionNode value) {
      if(variables.ContainsKey(name) || constvar.ContainsKey(name))
        throw new AlreadyDefinedException(this.ToString(), name);

      VarReferenceExpressionNode v = new VarReferenceExpressionNode(
        name, type, value, this, false, isStatic, isPublic);
      variables.Add(name, v);
    }

    public void AddVariable(VarReferenceExpressionNode varRef) {
      if(variables.ContainsKey(varRef.Name) || constvar.ContainsKey(varRef.Name))
        throw new AlreadyDefinedException(this.ToString(), varRef.FullName);

      variables.Add(varRef.Name, varRef);
    }

    public void RemoveVariables(IEnumerable<string> vars) {
      foreach(var v in vars)
        variables.Remove(v);
    }

    public TypeNode FindVariableType(string name, bool recursive = true) {
      if(variables.ContainsKey(name))
        return variables[name].Type;

      if(constvar.ContainsKey(name))
        return constvar[name].Type;

      if(Prev != null && recursive)
        return Prev.FindVariableType(name, recursive);

      throw new NotDefinedException(name);
    }

    public VarReferenceExpressionNode FindVariable(string name, bool recursive = true) {
      if(variables.ContainsKey(name))
        return variables[name];

      if(constvar.ContainsKey(name))
        return constvar[name];

      if(Prev != null && recursive)
        return Prev.FindVariable(name, recursive);

      throw new NotDefinedException(name);
    }

    public void AddConstVariable(string name, ConstExpressionNode value) {
      if(variables.ContainsKey(name) || constvar.ContainsKey(name))
        throw new AlreadyDefinedException(value, this.ToString(), name);

      constvar.Add(name, new VarReferenceExpressionNode(
                           name, value.GetTypeNode(), value, this, true));
    }

    public ConstExpressionNode GetConstVariable(string name, bool recursive = true) {
      if(constvar.ContainsKey(name))
        return (ConstExpressionNode)constvar[name].Value;

      if(Prev != null && recursive)
        return Prev.GetConstVariable(name, recursive);

      throw new NotDefinedException(name);
    }

    private void OnMethodRemoved(Node n) {
      if(n is MethodDefinitionNode) {
        var m = n as MethodDefinitionNode;
        functionsRef.Remove(m);
        functions.Remove(m.Name);
      }
    }

    public void AddFunction(MethodDefinitionNode m) {
      if(functions.ContainsKey(m.Name)) {
        throw new AlreadyDefinedException(m, 
          String.Format("the function '{0}' is already defined", m.FullName));
      } else {
        functions.Add(m.Name, m);
        m.Removed += OnMethodRemoved;
      }
    }

    public MethodReferenceExpressionNode FindFunction(
                                            string name, 
                                            bool recursive = true, 
                                            bool originalScope = true) {
      if(functions.ContainsKey(name)) {
        if(originalScope) {
          // is not really nessessary, but it limits the number of objects.
          // one reference per methode would work too, but we limit it to 
          // the environment to get a 'better' graph
          if(!functionsRef.ContainsKey(functions[name]))
            functionsRef.Add(functions[name], 
              new MethodReferenceExpressionNode(this, functions[name]));

          return functionsRef[functions[name]];
        }
        return new MethodReferenceExpressionNode(this, functions[name]);
      }

      if(Prev != null && recursive) {
        if(functionsRef.ContainsKey(FindFunctionDefinition(name)))
          return functionsRef[FindFunctionDefinition(name)];

        MethodReferenceExpressionNode mr = Prev.FindFunction(name, recursive, false);
        if(!functionsRef.ContainsKey(mr.Method))
          functionsRef.Add(mr.Method, mr);

        return mr;
      }

      throw new NotDefinedException(name);
    }

    public MethodDefinitionNode FindFunctionDefinition(string name, bool recursive = true) {
      if(functions.ContainsKey(name)) {
        return functions[name];
      }

      if(Prev != null && recursive)
        return Prev.FindFunctionDefinition(name, recursive);

      throw new NotDefinedException(name);
    }

    public bool ExistsFunction(string name, bool recursive = true) {
      if(functions.ContainsKey(name)) {
        return true;
      }

      if(Prev != null && recursive)
        return Prev.ExistsFunction(name, recursive);

      return false;
    }

    public bool IsClassScope() {
      if(IsClass)
        return true;

      if(Prev != null)
        return Prev.IsClassScope();

      return false;
    }

    public Environment GetClassScope(string name) {
      if(IsClass && this.Name.Equals(name))
        return this;

      if(Prev != null)
        return Prev.GetClassScope(name);

      return null;
    }

    public Environment GetClassEnvironment() {
      if(IsClass)
        return this;

      if(Prev != null)
        return Prev.GetClassEnvironment();

      throw new SemanticErrorException(ErrorCode.FATAL_ERROR, 
        String.Format("'{0}' is not a subenvironment of a class", FullName));
    }

    public ExpressionNode Find(string name, bool recursive = true, 
                               bool createIfNotExists = false) {
      try {
        return this.FindVariable(name, recursive);
      } catch(NotDefinedException) {
        try {
          return FindFunction(name, recursive);
        } catch(NotDefinedException ex) {
          if(createIfNotExists)
            return new MethodReferenceExpressionNode(this, name);
          else
            throw ex;
        }
      }
    }

    public override string ToString() {
      return FullName;
    }

    public override bool Validate(FaultHandler handleFault) {
      foreach(var f in functions.Values) {
        if(!(f is PreDefinedMethodNode)) {
          if(f.Body == null && !f.IsAbstract)
            handleFault(ErrorCode.UNDEFINED_METHOD, f, 
              String.Format("no method/function definition for '{0}'", f.FullName));
        }
      }
      return base.Validate(handleFault);
    }

    public override void Visit(Node.Visitor visitor) {
      foreach(var v in variables.Values)
        visitor(this, v);

      foreach(var v in constvar.Values)
        visitor(this, v);

      foreach(var f in functions.Values)
        visitor(this, f);

      if(Prev != null)
        visitor(this, Prev);
    }

    public IEnumerator<Node> GetEnumerator() {
      foreach(Node v in variables.Values)
        yield return v;

      foreach(var v in constvar.Values)
        yield return v;

      foreach(var f in functions.Values)
        yield return f;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }


}
