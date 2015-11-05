/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System.Collections.Generic;
using Pseudo.Net.AbstractSyntaxTree;
using System;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net {
  public class SyntaxTreeBuilder {
    public enum SearchScope { GLOBAL, THIS_CLASS/*, BASE_CLASS*/ };
    
    private AbstractSyntaxTree.Environment curEnv { get; set; }    
    public ProgramRootNode ProgramRoot;
    
    public SyntaxTreeBuilder() {
      this.ProgramRoot = new ProgramRootNode(new AbstractSyntaxTree.Environment("Global"));
      this.curEnv = this.ProgramRoot.Environment;
      this.AddFunction(MethodeBuilder.Read(this.curEnv));
      this.AddFunction(MethodeBuilder.Write(this.curEnv));
      this.AddFunction(MethodeBuilder.WriteLn(this.curEnv));
      this.AddFunction(MethodeBuilder.Real(this.curEnv));
      this.AddFunction(MethodeBuilder.Int(this.curEnv));
      this.AddFunction(MethodeBuilder.Chr(this.curEnv));
      this.AddFunction(MethodeBuilder.New(this.curEnv));
      this.AddFunction(MethodeBuilder.Dispose(this.curEnv));
      this.AddFunction(MethodeBuilder.Length(this.curEnv));
      this.AddFunction(MethodeBuilder.Low(this.curEnv));
      this.AddFunction(MethodeBuilder.High(this.curEnv));
    }

    
    public string Name { 
      get { return ProgramRoot.Name; } 
      set { ProgramRoot.Name = value; } 
    }
    
    public StatementNode MainBody { 
      get { return ProgramRoot.EntryPoint.Body; } 
      set { ProgramRoot.EntryPoint.Body = value; } 
    }
    
    private TypeRepository types { 
      get { return ProgramRoot.types; } 
    }
    
    #region Scope

    private Stack<AbstractSyntaxTree.Environment> scopeStack = new Stack<AbstractSyntaxTree.Environment>();

    public AbstractSyntaxTree.Environment EnterScope(bool isClassScope = false) {
      scopeStack.Push(this.curEnv);
      this.curEnv = new AbstractSyntaxTree.Environment(this.curEnv, isClassScope);
      return this.curEnv;
    }

    public AbstractSyntaxTree.Environment EnterScope(AbstractSyntaxTree.Environment env) {
      scopeStack.Push(this.curEnv);
      this.curEnv = env;
      return this.curEnv;
    }

    public AbstractSyntaxTree.Environment EnterEntryPointScope() {
      this.ProgramRoot.EntryPoint = new MethodDefinitionNode(EnterScope());
      this.ProgramRoot.EntryPoint.IsPublic = true;
      this.ProgramRoot.EntryPoint.IsStatic = true;
      this.ProgramRoot.EntryPoint.Name = "Main";
      this.ProgramRoot.EntryPoint.ReturnType = BaseTypeNode.VoidTypeNode;
      return this.curEnv;
    } 

    public AbstractSyntaxTree.Environment LeaveScope() {
      if(scopeStack.Count == 0)
        throw new Exception("can't leave global scope");

      this.curEnv = scopeStack.Pop();
      return this.curEnv;
    }
    #endregion

    #region Types

    public void AddType(string name, TypeNode typenode) {
      types.AddType(name, typenode);
      if(typenode is ClassTypeNode) {
        this.AddFunction(MethodeBuilder.ClassCast(
          this.curEnv, typenode as ClassTypeNode));
      }
    }

    public bool ExistsType(string typename) {
      return types.Exists(typename);
    }

    public void AddAliasType(string typename, string aliasto) {
      types.AddAliasType(typename, aliasto);
    }

    public TypeNode FindOrCreateType(string typename) {
      return types.FindOrCreate(typename);
    }

    public TypeNode FindType(string typename) {
      return types.Find(typename);
    }
    #endregion

    #region Variables

    public TypeNode FindVariableType(string name) {
      return this.curEnv.FindVariableType(name);
    }    

    public VarReferenceExpressionNode FindVariable(string name, 
                                        SearchScope Scope = SearchScope.GLOBAL) {
      switch(Scope) {
        default:
        case SearchScope.GLOBAL:
          return this.curEnv.FindVariable(name);

        case SearchScope.THIS_CLASS:
          if(!InClassScope())
            throw new SemanticErrorException(ErrorCode.THIS_KEYWORD_NOT_ALLOWED, 
              "'this' is only allowed in a class");

          return this.curEnv.GetClassEnvironment().FindVariable(name, false);
                    
      }
    }

    public void AddConstVariable(string name, ConstExpressionNode value) {
      this.curEnv.AddConstVariable(name, value);
    }

    public void AddConstVariable(string name, int value) {
      this.curEnv.AddConstVariable(name, new IntegerNode(value));
    }

    public void AddConstVariable(string name, double value) {
      this.curEnv.AddConstVariable(name, new RealNode(value));
    }

    public void AddConstVariable(string name, char value) {
      this.curEnv.AddConstVariable(name, new CharNode(value));
    }

    public ConstExpressionNode GetConstVariable(string name) {
      return this.curEnv.GetConstVariable(name);
    }

    public void AddVariable(string name, 
                            TypeNode type, 
                            bool isStatic, 
                            bool isPublic, 
                            ExpressionNode value = null) {
      this.curEnv.AddVariable(name, type, isStatic, isPublic, value);
    }
    #endregion

    #region Methods

    public MethodReferenceExpressionNode FindFunction(ExpressionNode e) {
      if(e is MethodReferenceExpressionNode)
        return (MethodReferenceExpressionNode)e;

      if(e is DereferenceMemberExpressionNode) {
        DereferenceMemberExpressionNode dr = e as DereferenceMemberExpressionNode;
        ExpressionNode member = dr.GetMember();
        if(member is MethodReferenceExpressionNode)
          return member as MethodReferenceExpressionNode;
      }

      throw new SemanticErrorException(ErrorCode.FATAL_ERROR, e, 
        String.Format("'{0}' is not a function or methode", e.ToString()));
    }

    public MethodReferenceExpressionNode FindFunction(string name) {
      return this.curEnv.FindFunction(name);
    }

    public MethodDefinitionNode FindFunctionDefinition(string name) {
      return this.curEnv.FindFunctionDefinition(name, false);
    }

    public void AddFunction(MethodDefinitionNode m) {
      this.curEnv.AddFunction(m);
    }
    #endregion

    public ExpressionNode Find(string name, SearchScope Scope = SearchScope.GLOBAL) {
      switch(Scope) {
        default:
        case SearchScope.GLOBAL:
          return this.curEnv.Find(name, createIfNotExists: true);

        case SearchScope.THIS_CLASS:
          if(!InClassScope())
            throw new SemanticErrorException(ErrorCode.THIS_KEYWORD_NOT_ALLOWED, 
              "'this' is only allowed in a class");

          return this.curEnv.GetClassEnvironment().Find(name, false);
      }

    }

    public bool Exists(string name) {
      try {
        return this.curEnv.Find(name, createIfNotExists: false) != null;
      } catch(NotDefinedException) {
        return false;
      }
    }

    public bool InClassScope() {
      return this.curEnv.IsClassScope();
    }

    public AbstractSyntaxTree.Environment GetSubClass(string classname) {
      return this.curEnv.GetClassEnvironment().Prev.GetClassScope(classname);
    }

  }
}
