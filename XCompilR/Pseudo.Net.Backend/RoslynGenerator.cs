/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Backend;
using Pseudo.Net.Common;
using Pseudo.Net.Exceptions;
using PseudoToDotNetWrapper;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Pseudo.Net.Backend.Roslyn {
  public class RoslynGenerator : BaseGenerator {
    private AbstractSyntaxTree.Environment curClassEnv = null;

    private void ReportError(ErrorCode code, Node source, string msg, bool stop = false) {
      int Line = 0;
      int Col = 0;

      if(source != null) {
        Line = source.Line;
        Col = source.Col;
      }

      ErrorMessage m = new ErrorMessage(code, Line, Col, "(Roslyn) " + msg);

      if(errorReporter != null)
        errorReporter(m);

      if(stop)
        throw new NotSupportedException(m.ToString());
    }

    public RoslynGenerator(ProgramRootNode root, ReportErrorHandler errorHandler)
      : base(root, errorHandler) { }

    public override void Generate(Stream stream, Target target) {
      var namespaceroot = Syntax.NamespaceDeclaration(Syntax.IdentifierName(root.Name));

      var classTree = Syntax.ClassDeclaration(root.Name)
        .AddMembers(GenerateTypeDefs(root.types.Distinct().ToArray()))
        .AddMembers(GenerateEnvironment(root.Environment))
        .AddMembers(GenerateMethod(root.EntryPoint))
        .AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));

      namespaceroot = namespaceroot.AddMembers(classTree);

      var treeroot = Syntax.CompilationUnit()
        .AddMembers(namespaceroot)
        //   .AddUsings(Syntax.UsingDirective(Syntax.IdentifierName("System")))
        .AddUsings(Syntax.UsingDirective(Syntax.IdentifierName("PseudoToDotNetWrapper")))
        .AddUsings(Syntax.UsingDirective(Syntax.ParseName("PseudoToDotNetWrapper.Types")))
        //        .AddUsings(Syntax.UsingDirective(Syntax.ParseName("PseudoToDotNetWrapper.Types"))
        //          .WithAlias(Syntax.NameEquals(Syntax.IdentifierName("Pseudo"))))
;

    //  Console.WriteLine(treeroot.NormalizeWhitespace("  ", true).ToFullString());
      switch(target) {
        case Target.CS:
          StreamWriter sw = new StreamWriter(stream);
          treeroot.NormalizeWhitespace("  ", true).WriteTo(sw);
          sw.Close();
          break;

        case Target.DGML:
          //XmlWriter xw = XmlWriter.Create(stream);
          //treeroot.ToDgml(LanguageNames.CSharp,
          //  new SyntaxDgmlOptions { ShowTrivia = false, ShowSpan = false }).WriteTo(xw);
          //xw.Close();
          throw new NotSupportedException("DGML is not avaialbe!");

        case Target.EXE:
          var compilation = Compilation.Create(
            root.Environment.Name + ".exe",
            options: new CompilationOptions(OutputKind.ConsoleApplication),
            syntaxTrees: new[] { SyntaxTree.Create(treeroot) },
            references: new[] { 
            new MetadataFileReference(typeof(object).Assembly.Location),
            new MetadataFileReference(typeof(BaseUtils).Assembly.Location, embedInteropTypes:true)
          });

          EmitResult compileResult = compilation.Emit(stream);

          if(!compileResult.Success) {
            if(errorReporter != null) {
              foreach(var e in compileResult.Diagnostics) {
                errorReporter(new ErrorMessage(ErrorCode.CSHARP_COMPILER_ERROR, 0, 0,
                  e.Info.GetMessage(), e.Info.WarningLevel > 0));
              }
            }
          }
          break;
      }
    }
    #region Declaration

    private MemberDeclarationSyntax[] GenerateEnvironment(AbstractSyntaxTree.Environment env, bool isPublic = false) {
      List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();

      foreach(VarReferenceExpressionNode v in env
       .OfType<VarReferenceExpressionNode>()
       .Where(w => !w.IsConst)) {
        members.Add(GenerateVarDefinition(v, isPublic));
      }

      foreach(MethodDefinitionNode m in env
        .OfType<MethodDefinitionNode>()
        .Where(n => !(n is PreDefinedMethodNode))) {
        members.Add(GenerateMethod(m));

        foreach(var v in m.GetVariables(false)
          .Where(p => p.IsStatic)
          .OfType<VarReferenceExpressionNode>()) {
          members.Add(GenerateVarDefinition(v));
        }
      }

      return members.ToArray();
    }

    private MemberDeclarationSyntax[] GenerateTypeDefs(TypeNode[] types) {
      List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();
      foreach(var t in types) {
        if(t is ClassTypeNode)
          members.Add(GenerateClassTypeDef(t as ClassTypeNode));
        else if(t is StructTypeNode)
          members.Add(GenerateStructTypeDef(t as StructTypeNode));
      }

      return members.ToArray();
    }

    private TypeDeclarationSyntax GenerateClassTypeDef(ClassTypeNode t) {
      curClassEnv = t.Environment;

      ClassDeclarationSyntax ret = Syntax.ClassDeclaration(t.GetName(true))
        .AddMembers(GenerateEnvironment(t.Environment, false))
        .AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));

      if(t.IsAbstract)
        ret = ret.AddModifiers(Syntax.Token(SyntaxKind.AbstractKeyword));

      if(t.BaseClass != null)
        ret = ret.AddBaseListTypes(GetTypeSyntax(t.BaseClass));

      curClassEnv = null;
      return ret;
    }

    private TypeDeclarationSyntax GenerateStructTypeDef(StructTypeNode t) {
      curClassEnv = t.Environment;
      ClassDeclarationSyntax ret = Syntax.ClassDeclaration(t.GetName(true))
        .AddBaseListTypes(Syntax.IdentifierName("Compound"))
        .AddMembers(GenerateEnvironment(t.Environment, true))
        .AddMembers(GenerateTypeDefs(t.Environment
          .OfType<VarReferenceExpressionNode>()
          .Select<VarReferenceExpressionNode, TypeNode>(v => v.GetTypeNode())
          .ToArray()))
        .AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));

      curClassEnv = null;
      return ret;
    }

    private ExpressionSyntax GetArrayInitializer(ArrayTypeNode t) {
      ArrayTypeNode basetype = t;
      StringBuilder sb = new StringBuilder();

      while(basetype.Typeof is ArrayTypeNode) {
        basetype = basetype.Typeof as ArrayTypeNode;
      }
      
      SeparatedSyntaxList<ArgumentSyntax> arguments = Syntax.SeparatedList<ArgumentSyntax>(
        Syntax.Argument(RoslynUtils.GetLiteralExpression(t.Size)));
      basetype = t;
      while(basetype.Typeof is ArrayTypeNode) {
        basetype = basetype.Typeof as ArrayTypeNode;
        arguments = arguments.Add(Syntax.Argument(
          RoslynUtils.GetLiteralExpression(basetype.Size)));
      }

      return Syntax.ObjectCreationExpression(GetTypeSyntax(t))
        .WithArgumentList(Syntax.ArgumentList(arguments));

      throw new NotImplementedException();
    }

    private ExpressionSyntax CloneValueTypes(ExpressionNode e) {
      TypeNode t = e.GetTypeNode();
      if(e is VarReferenceExpressionNode
      || e is MemberSelectorExpressionNode) {
        return Syntax.CastExpression(GetTypeSyntax(e.GetTypeNode()),
                Syntax.InvocationExpression(
                  Syntax.MemberAccessExpression(
                    SyntaxKind.MemberAccessExpression,
                    GenerateExpression(e),
                    Syntax.IdentifierName("Clone")
              )));
      }

      return GenerateExpression(e);
    }

    private ExpressionSyntax AddValueAccess(ExpressionNode e, bool assign) {
      if(!e.IsConst) {
        if(e is VarReferenceExpressionNode
          || e is DereferenceExpressionNode
          || e is DereferenceMemberExpressionNode
          || e is ArrayIndexerExpressionNode
          || e is MemberSelectorExpressionNode
          ) {
          return
            Syntax.MemberAccessExpression(
              SyntaxKind.MemberAccessExpression,
              GenerateExpression(e),
              Syntax.IdentifierName("Value")
            );
        }
      }

      return GenerateExpression(e);
    }

    private ExpressionSyntax GetTypeInitialValue(TypeNode t) {
      if(t is ArrayTypeNode) {
        return GetArrayInitializer(t as ArrayTypeNode);
      } else if(t is PointerTypeNode || t is StructTypeNode) {
        return Syntax.ObjectCreationExpression(GetTypeSyntax(t))
          .WithArgumentList(Syntax.ArgumentList());
      } else if(t is BaseTypeNode) {
        switch(t.GetName()) {
          case "INT":
            return RoslynUtils.GetLiteralExpression(0);
          case "STRING":
            return RoslynUtils.GetLiteralExpression("");
          case "REAL":
            return RoslynUtils.GetLiteralExpression(0.0);
          case "CHAR":
            return RoslynUtils.GetLiteralExpression(' ');
          case "BOOL":
            return RoslynUtils.GetLiteralExpression(false);
          default:
            throw new NotImplementedException();
        }
      }
      throw new NotImplementedException();
    }

    private string GetVarRefName(ExpressionNode e) {
      if(e is VarReferenceExpressionNode) {
        var v = e as VarReferenceExpressionNode;
        if(v.IsStatic)
          return "µ" + v.Environment.Name + "_" + v.Name + v.GetHashCode();
        else if(v.Name == "Value") // reserved attribute
          return "µValue";
        else
          return v.Name;

      } else if(e is MethodReferenceExpressionNode)
        return (e as MethodReferenceExpressionNode).Name;
      throw new NotImplementedException("No Name mangling for " + e.GetType().Name);
    }

    private MemberDeclarationSyntax GenerateVarDefinition(VarReferenceExpressionNode v, bool isPublic = false) {
      SyntaxTokenList modifier = Syntax.TokenList(
        Syntax.Token(SyntaxKind.ReadOnlyKeyword));

      if(v.IsPublic || isPublic)
        modifier = modifier.Add(Syntax.Token(SyntaxKind.PublicKeyword));
      else
        modifier = modifier.Add(Syntax.Token(SyntaxKind.PrivateKeyword));

      if(v.IsConst) {
        if(curClassEnv != null && curClassEnv.IsClassScope())
          modifier = modifier.Add(Syntax.Token(SyntaxKind.ReadOnlyKeyword));
        else
          modifier = modifier.Add(Syntax.Token(SyntaxKind.ConstKeyword));
      } else if(v.IsStatic)
        modifier = modifier.Add(Syntax.Token(SyntaxKind.StaticKeyword));

      ExpressionSyntax initValue;
      if(v.IsConst || v.Value != null) {
        initValue = GenerateExpression(v.Value);
      } else
        initValue = GetTypeInitialValue(v.Type);

      return Syntax.FieldDeclaration(
        Syntax.VariableDeclaration(GetTypeSyntax(v.Type))
          .AddVariables(Syntax.VariableDeclarator(GetVarRefName(v))
            .WithInitializer(Syntax.EqualsValueClause(initValue))
        )).WithModifiers(modifier);
    }

    private ParameterListSyntax GetParameterList(MethodDefinitionNode m) {
      ParameterListSyntax list = Syntax.ParameterList();
      foreach(var arg in m.Parameter) {
        ParameterSyntax p = Syntax.Parameter(
            Syntax.Identifier(GetVarRefName(arg.Key)))
          .WithType(GetTypeSyntax(arg.Value.type));

        //   if (arg.Value.dir == ArgumentDirection.IO)
        //     p = p.WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.RefKeyword)));
        //   else 
        //   if (arg.Value.dir == ArgumentDirection.OUT)
        //     p = p.WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.OutKeyword)));

        list = list.AddParameters(p);
      }

      return list;
    }

    private StatementSyntax[] GetLocalVarDecl(MethodDefinitionNode m) {
      List<StatementSyntax> decl = new List<StatementSyntax>();

      var variables = m.GetVariables(false).Where(p => !p.IsStatic);
      foreach(var v in variables) {
        ExpressionSyntax initVal;
        if(v.IsConst || v.Value != null)
          initVal = GenerateExpression(v.Value);
        else
          initVal = GetTypeInitialValue(v.Type);

        decl.Add(Syntax.LocalDeclarationStatement(
          Syntax.VariableDeclaration(GetTypeSyntax(v.Type))
          .AddVariables(Syntax.VariableDeclarator(GetVarRefName(v))
            .WithInitializer(Syntax.EqualsValueClause(initVal))
          ))
          //     .AddModifiers(Syntax.Token(SyntaxKind.ReadOnlyKeyword))
          );
      }
      return decl.ToArray();
    }

    private BaseMethodDeclarationSyntax GenerateMethod(MethodDefinitionNode m) {
      bool isConstructor = false;
      bool allowOverride = false;

      if(m.IsClassMethod()) {
        ClassTypeNode c = m.GetClassEnvironment().ClassNode;
        isConstructor = c.GetContructor() == m;
        allowOverride = !c.IsAbstract && c.BaseClass != null;
      }

      SyntaxTokenList modifier = Syntax.TokenList();
      if(m.IsPublic)
        modifier = modifier.Add(Syntax.Token(SyntaxKind.PublicKeyword));
      else
        modifier = modifier.Add(Syntax.Token(SyntaxKind.PrivateKeyword));

      if(m.IsStatic)
        modifier = modifier.Add(Syntax.Token(SyntaxKind.StaticKeyword));

      if(m.IsAbstract)
        modifier = modifier.Add(Syntax.Token(SyntaxKind.AbstractKeyword));

      if(allowOverride) {
        if(m.IsOverride)
          modifier = modifier.Add(Syntax.Token(SyntaxKind.OverrideKeyword));
      } else {
        if(m.IsClassMethod() && !isConstructor && m.IsPublic && !m.IsAbstract && !m.IsStatic)
          modifier = modifier.Add(Syntax.Token(SyntaxKind.VirtualKeyword));
      }

      BaseMethodDeclarationSyntax method;

      if(isConstructor) {
        ConstructorDeclarationSyntax decl = Syntax.ConstructorDeclaration(m.Name)
          .WithParameterList(GetParameterList(m))
          .WithModifiers(modifier);

        ClassTypeNode c = m.GetClassEnvironment().ClassNode;
        if(c.BaseClass != null) {
          if(c.BaseClass.GetContructor().Parameter.Any()) {
            if(m.Body != null && m.Body is BlockStatementNode) {
              BlockStatementNode b = m.Body as BlockStatementNode;
              var callBaseClass = b.GetStatementes()
                .OfType<InvokeStatementNode>()
                .Where(s => s.Function.GetMethodDefinitionNode().Name.Equals(c.BaseClass.Name))
                .ToArray();

              if(callBaseClass.Length == 1) {
                var args = callBaseClass[0].Function.Arguments;
                callBaseClass[0].IsConstructor = true;

                SeparatedSyntaxList<ArgumentSyntax> argList = Syntax.SeparatedList<ArgumentSyntax>();
                foreach(var a in args.Values) {
                  argList = argList.Add(Syntax.Argument(GenerateExpression(a)));
                }

                decl = decl.WithInitializer(Syntax.ConstructorInitializer(
                  SyntaxKind.BaseConstructorInitializer,
                  Syntax.ArgumentList(argList)
                  ));


              } else {
                ReportError(ErrorCode.BASE_CONTRUCTOR_MUST_BE_CALLED, b,
                  String.Format("baseclass contructor is called {0} times", callBaseClass.Length));
              }
            } else {
              ReportError(ErrorCode.NO_BODY_DEFINED, m, "Constructor needs a block");
            }
          }
        }


        method = decl.WithBody(Syntax.Block(GetLocalVarDecl(m))
          .AddStatements(GenerateStatementBlock(m.Body).Statements.ToArray()));
      } else {
        MethodDeclarationSyntax decl = Syntax.MethodDeclaration(GetTypeSyntax(m.ReturnType), m.Name)
          .WithParameterList(GetParameterList(m)).WithModifiers(modifier);

        if(m.Body != null) {
          decl = decl.WithBody(Syntax.Block(GetLocalVarDecl(m))
            .AddStatements(GenerateStatementBlock(m.Body).Statements.ToArray()));
        }

        method = decl;
      }

      return method;

    }
    #endregion

    #region Statements
    private BlockSyntax GenerateBlock(BlockStatementNode b) {
      BlockSyntax block = Syntax.Block();
      foreach(var s in b.GetStatementes()) {
        if(!s.IsContructorCall())
          block = block.AddStatements(GenerateStatement(s));
      }

      return block;
    }

    private BlockSyntax GenerateStatementBlock(StatementNode s) {
      if(s is BlockStatementNode)
        return GenerateBlock(s as BlockStatementNode);

      return Syntax.Block(GenerateStatement(s));
    }

    private StatementSyntax GenerateStatement(StatementNode s) {
      if(s is BlockStatementNode)
        return GenerateBlock(s as BlockStatementNode);
      else if(s is InvokeStatementNode)
        return GenerateCallStatement(s as InvokeStatementNode);
      else if(s is ReturnStatement)
        return GenerateReturnStatement(s as ReturnStatement);
      else if(s is AssignStatementNode)
        return GenerateAssignStatement(s as AssignStatementNode);
      else if(s is IfStatement)
        return GenerateIfStatement(s as IfStatement);
      else if(s is WhileStatement)
        return GenerateWhileStatement(s as WhileStatement);
      else if(s is RepeatStatement)
        return GenerateRepeatStatement(s as RepeatStatement);
      else if(s is ForStatement)
        return GenerateForStatement(s as ForStatement);
      else if(s is HaltStatement)
        return GenerateHaltStatement(s as HaltStatement);
      else if(s is BreakStatement)
        return GenerateBreakStatement(s as BreakStatement);
      else if(s is CaseStatement)
        return GenerateCaseStatement(s as CaseStatement);

      throw new NotImplementedException(s.GetType().Name);
    }

    private StatementSyntax GenerateCaseStatement(CaseStatement s) {
      SyntaxList<SwitchSectionSyntax> labels = Syntax.List<SwitchSectionSyntax>();
      foreach(var c in s.Cases) {
        if(c.Label is ValueCollectionNode) {
          SyntaxList<SwitchLabelSyntax> range = Syntax.List<SwitchLabelSyntax>();

          foreach(var la in (c.Label as ValueCollectionNode).GetValues()) {
            range = range.Add(Syntax.SwitchLabel(SyntaxKind.CaseSwitchLabel,
              GenerateExpression(la.GetConstValueExpression())));
          }

          labels = labels.Add(Syntax.SwitchSection(range,
            GenerateStatementBlock(c.Statment)
              .AddStatements(Syntax.BreakStatement())
          ));
        } else {
          labels = labels.Add(Syntax.SwitchSection(
              Syntax.SwitchLabel(SyntaxKind.CaseSwitchLabel,
            GenerateExpression(c.Label.GetConstValueExpression())),
            GenerateStatementBlock(c.Statment)
              .AddStatements(Syntax.BreakStatement())
          ));
        }
      }

      if(s.DefaultStatement != null) {
        labels = labels.Add(Syntax.SwitchSection(
            Syntax.SwitchLabel(SyntaxKind.DefaultSwitchLabel),
              GenerateStatementBlock(s.DefaultStatement)
                .AddStatements(Syntax.BreakStatement())
          ));
      }

      return Syntax.SwitchStatement(GenerateExpression(s.Expr), labels);
    }

    private StatementSyntax GenerateBreakStatement(BreakStatement breakStatement) {
      return Syntax.BreakStatement();
    }

    private StatementSyntax GenerateHaltStatement(HaltStatement haltStatement) {
      return Syntax.ParseStatement("System.Environment.Exit(0);");
    }

    private StatementSyntax GenerateForStatement(ForStatement s) {
      SyntaxKind compareOp, incOp;

      if(s.CountDownward) {
        compareOp = SyntaxKind.GreaterThanOrEqualExpression;
        incOp = SyntaxKind.PostDecrementExpression;
      } else {
        compareOp = SyntaxKind.LessThanOrEqualExpression;
        incOp = SyntaxKind.PostIncrementExpression;
      }

      return Syntax.ForStatement(GenerateStatement(s.Stat))
          .AddInitializers(Syntax.BinaryExpression(
                            SyntaxKind.AssignExpression,
                            AddValueAccess(s.Var, true),
                            GenerateExpression(s.Start)))
          .WithCondition(Syntax.BinaryExpression(compareOp,
            Syntax.IdentifierName(GetVarRefName(s.Var)),
            GenerateExpression(s.End)))
          .WithIncrementors(
            Syntax.SeparatedList<ExpressionSyntax>(
              Syntax.PostfixUnaryExpression(incOp, 
                Syntax.IdentifierName(GetVarRefName(s.Var)))
            ));
    }

    private StatementSyntax GenerateRepeatStatement(RepeatStatement s) {
      return Syntax.DoStatement(GenerateStatement(s.Stat),
        Syntax.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
          GenerateParenthesizedExpression(s.Expr)));
    }

    private StatementSyntax GenerateWhileStatement(WhileStatement s) {
      return Syntax.WhileStatement(GenerateExpression(s.Expr), GenerateStatement(s.Stat));
    }

    private StatementSyntax GenerateIfStatement(IfStatement s) {
      if(s.ElseStat == null)
        return Syntax.IfStatement(GenerateExpression(s.Expr),
          GenerateStatement(s.IfStat));

      return Syntax.IfStatement(GenerateExpression(s.Expr),
        GenerateStatement(s.IfStat),
        Syntax.ElseClause(GenerateStatement(s.ElseStat)));
    }

    private StatementSyntax GenerateAssignStatement(AssignStatementNode s) {
      return Syntax.ExpressionStatement(
        Syntax.BinaryExpression(
          SyntaxKind.AssignExpression,
          AddValueAccess(s.LValue, true),
          AddValueAccess(s.RValue, false)
          )
        );
    }

    private ReturnStatementSyntax GenerateReturnStatement(ReturnStatement returnStatement) {
      if(returnStatement.Expr != null)
        return Syntax.ReturnStatement(GenerateExpression(returnStatement.Expr));

      return Syntax.ReturnStatement();
    }

    private ExpressionStatementSyntax GenerateCallStatement(InvokeStatementNode s) {
      return Syntax.ExpressionStatement(GenerateExpression(s.Function));
    }

    #endregion

    #region Expressions
    private ExpressionSyntax GenerateExpression(ExpressionNode e) {
      if(e.IsConst)
        return GenerateConstExpression(e.GetConstValueExpression());

      else if(e is InvokeExpressionNode)
        return GenerateCallExpression(e as InvokeExpressionNode);
      else if(e is VarReferenceExpressionNode)
        return GenerateVarReferenceExpression(e as VarReferenceExpressionNode);
      else if(e is BinaryExpressionNode)
        return GenerateBinaryExpression(e as BinaryExpressionNode);
      else if(e is UnaryExpressionNode)
        return GenerateUnaryExpression(e as UnaryExpressionNode);
      else if(e is ArrayIndexerExpressionNode)
        return GenerateArrayIndexerExpression(e as ArrayIndexerExpressionNode);
      else if(e is MemberSelectorExpressionNode)
        return GenerateMemberSelectorExpression(e as MemberSelectorExpressionNode);
      else if(e is DereferenceMemberExpressionNode)
        return GenerateDereferenceMemberExpression(e as DereferenceMemberExpressionNode);
      else if(e is DereferenceExpressionNode)
        return GenerateDereferenceExpression(e as DereferenceExpressionNode);
      else if(e is AddressOfExpressionNode)
        return GenerateAddressOfExpression(e as AddressOfExpressionNode);
      else if(e is TypeReferenceExpressionNode)
        return GenerateTypeReferenceExpression(e as TypeReferenceExpressionNode);

      throw new NotImplementedException(e.GetType().Name);
    }

    private ExpressionSyntax GenerateTypeReferenceExpression(TypeReferenceExpressionNode e) {
      return Syntax.IdentifierName(e.Type.GetName());
    }

    private ExpressionSyntax GenerateParenthesizedExpression(ExpressionNode e) {
      ExpressionSyntax expr = GenerateExpression(e);
      if(expr is BinaryExpressionSyntax)
        expr = Syntax.ParenthesizedExpression(expr);

      return expr;
    }

    private ExpressionSyntax GenerateAddressOfExpression(AddressOfExpressionNode e) {
      return GenerateExpression(e.AddressOf);
    }

    private ExpressionSyntax GenerateDereferenceExpression(DereferenceExpressionNode e) {
      if(e.Source.GetTypeNode() is PointerTypeNode)
        return AddValueAccess(e.Source, true);
      else
        ReportError(ErrorCode.CODE_GEN_ERROR, e, 
          "Dereference only possible for pointer types", true);

      return null;
    }

    private ExpressionSyntax SelectValue(ExpressionSyntax e) {
      return Syntax.MemberAccessExpression(
              SyntaxKind.MemberAccessExpression,
              e,
              Syntax.IdentifierName("Value")
            );
    }

    private ExpressionSyntax GenerateDereferenceMemberExpression(DereferenceMemberExpressionNode e) {
      return Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression,
        SelectValue(GenerateExpression(e.Source)),
        Syntax.IdentifierName(GetVarRefName(e.GetMember())));
    }

    private ExpressionSyntax GenerateMemberSelectorExpression(MemberSelectorExpressionNode e) {
      return Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression,
        GenerateExpression(e.MemberOf),
        Syntax.IdentifierName(GetVarRefName(e.GetVarReference())));
    }

    private ExpressionSyntax GetIndexExpression(ExpressionNode index, int offset) {
      if(index is IntegerNode) {
        int val = (index as IntegerNode).Value;
        return RoslynUtils.GetLiteralExpression(val - offset);
      } else {
        return
          Syntax.BinaryExpression(SyntaxKind.SubtractExpression,
            GenerateExpression(index),
            RoslynUtils.GetLiteralExpression(offset));
      }
    }

    private ElementAccessExpressionSyntax GenerateArrayIndexerExpression(ArrayIndexerExpressionNode e) {
      SeparatedSyntaxList<ArgumentSyntax> args = Syntax.SeparatedList<ArgumentSyntax>();

      Stack<ArrayIndexerExpressionNode> indexertree = new Stack<ArrayIndexerExpressionNode>();

      indexertree.Push(e);
      while(e.Array is ArrayIndexerExpressionNode) {
        e = e.Array as ArrayIndexerExpressionNode;
        indexertree.Push(e);
      }

      ExpressionSyntax array = GenerateExpression(e.Array);

      while(indexertree.Count > 0) {
        e = indexertree.Pop();

        if(e.Array.GetTypeNode() is ArrayTypeNode) {
          ArrayTypeNode a = e.Array.GetTypeNode() as ArrayTypeNode;
          if(a.Offset > 0) {
            args = args.Add(Syntax.Argument(GetIndexExpression(e.Index, a.Offset)));
          } else
            args = args.Add(Syntax.Argument(GenerateExpression(e.Index)));
        } else if(e.Array.GetTypeNode().IsString()) {
          args = args.Add(Syntax.Argument(GetIndexExpression(e.Index, 1)));
        } else
          args = args.Add(Syntax.Argument(GenerateExpression(e.Index)));
      }

      if(e.Array.GetTypeNode().GetArrayType() is ArrayTypeNode)
        ReportError(ErrorCode.CODE_GEN_ERROR, e, "Wrong number of indices inside []");

      return Syntax.ElementAccessExpression(array, Syntax.BracketedArgumentList(args));
    }

    private ExpressionSyntax GenerateUnaryExpression(UnaryExpressionNode e) {
      Dictionary<UnaryExpressionNode.Operator, SyntaxKind> op = 
        new Dictionary<UnaryExpressionNode.Operator, SyntaxKind>() { 
        {UnaryExpressionNode.Operator.MINUS, SyntaxKind.NegateExpression},
        {UnaryExpressionNode.Operator.NOT, SyntaxKind.LogicalNotExpression},
        {UnaryExpressionNode.Operator.PLUS, SyntaxKind.PlusExpression}
      };

      return Syntax.PrefixUnaryExpression(op[e.Op], GenerateParenthesizedExpression(e.Expr));
    }

    private ExpressionSyntax GenerateBinaryExpression(BinaryExpressionNode e) {
      Dictionary<BinaryExpressionNode.Operator, SyntaxKind> op = 
        new Dictionary<BinaryExpressionNode.Operator, SyntaxKind>() { 
        {BinaryExpressionNode.Operator.AND,   SyntaxKind.LogicalAndExpression},
        {BinaryExpressionNode.Operator.DIV,   SyntaxKind.DivideExpression},
        {BinaryExpressionNode.Operator.EQ,    SyntaxKind.EqualsExpression},
        {BinaryExpressionNode.Operator.GE,    SyntaxKind.GreaterThanOrEqualExpression},
        {BinaryExpressionNode.Operator.GT,    SyntaxKind.GreaterThanExpression},
        {BinaryExpressionNode.Operator.LE,    SyntaxKind.LessThanOrEqualExpression},
        {BinaryExpressionNode.Operator.LT,    SyntaxKind.LessThanExpression},
        {BinaryExpressionNode.Operator.MINUS, SyntaxKind.SubtractExpression},
        {BinaryExpressionNode.Operator.MOD,   SyntaxKind.ModuloExpression},
        {BinaryExpressionNode.Operator.MULT,  SyntaxKind.MultiplyExpression},
        {BinaryExpressionNode.Operator.NE,    SyntaxKind.NotEqualsExpression},
        {BinaryExpressionNode.Operator.OR,    SyntaxKind.LogicalOrExpression},
        {BinaryExpressionNode.Operator.PLUS,  SyntaxKind.AddExpression},
        {BinaryExpressionNode.Operator.ISA,   SyntaxKind.IsExpression}
      };

      if(e.Op != BinaryExpressionNode.Operator.ISA)
        return Syntax.BinaryExpression(op[e.Op],
          AddValueAccess(e.LeftNode, true),
          AddValueAccess(e.RightNode, true));
      else
        return Syntax.BinaryExpression(op[e.Op],
          GenerateParenthesizedExpression(e.LeftNode),
          GenerateParenthesizedExpression(e.RightNode));
    }

    private ExpressionSyntax GenerateVarReferenceExpression(VarReferenceExpressionNode e) {
      if(e.Environment.IsClassScope() && e.Environment.IsClass) {
        if(e.Environment.GetClassEnvironment() == curClassEnv)
          return Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression,
            Syntax.ThisExpression(), Syntax.IdentifierName(GetVarRefName(e)));
        else
          return Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression,
            Syntax.BaseExpression(), Syntax.IdentifierName(GetVarRefName(e)));
      }

      return Syntax.IdentifierName(GetVarRefName(e));
    }

    private ExpressionSyntax GenerateConstExpression(ConstExpressionNode e) {
      if(e is StringNode)
        return RoslynUtils.GetLiteralExpression((e as StringNode).Value);

      if(e is CharNode)
        return RoslynUtils.GetLiteralExpression((e as CharNode).Value);

      if(e is IntegerNode)
        return RoslynUtils.GetLiteralExpression((e as IntegerNode).Value);

      if(e is RealNode)
        return RoslynUtils.GetLiteralExpression((e as RealNode).Value);

      if(e is BoolNode)
        return RoslynUtils.GetLiteralExpression((e as BoolNode).Value);

      if(e is NullNode)
        return Syntax.LiteralExpression(SyntaxKind.NullLiteralExpression);

      throw new Exception("undefined const expression node");
    }

    private ArgumentListSyntax GenerateCallArguments(ArgumentList args) {
      ArgumentListSyntax result = Syntax.ArgumentList();
      foreach(var arg in args) {
        ArgumentSyntax a = null;
        if(arg.Value == ArgumentDirection.IN)
          a = Syntax.Argument(CloneValueTypes(arg.Key));
        else
          a = Syntax.Argument(GenerateExpression(arg.Key));

        result = result.AddArguments(a);
      }
      return result;
    }

    private ExpressionSyntax GeneratePreDefinedMethodcall(InvokeExpressionNode e) {
      MethodDefinitionNode methodDefinition = e.GetMethodDefinitionNode();
      if(IsNewMethod(methodDefinition)) {
        if(e.Arguments.First().Key is TypeReferenceExpressionNode) {
          TypeReferenceExpressionNode t = e.Arguments.First().Key as TypeReferenceExpressionNode;

          SeparatedSyntaxList<ArgumentSyntax> args = Syntax.SeparatedList<ArgumentSyntax>();

          if(t.Type.IsArray()) {
            var arrayType = t.Type as ArrayTypeNode;
            args = args.Add(Syntax.Argument(RoslynUtils.GetLiteralExpression(arrayType.Size)));

            while(arrayType.Typeof is ArrayTypeNode) {
              arrayType = arrayType.Typeof as ArrayTypeNode;
              args = args.Add(Syntax.Argument(RoslynUtils.GetLiteralExpression(arrayType.Size)));
            }
          } else {
            var parameter = e.Arguments.ToArray();
            for(int i = 1; i < parameter.Length; i++) {
              args = args.Add(Syntax.Argument(GenerateExpression(parameter[i].Key)));
            }
          }
          return Syntax.ObjectCreationExpression(GetTypeSyntax(t.Type))
            .WithArgumentList(Syntax.ArgumentList(args));
        } else {
          ReportError(ErrorCode.CODE_GEN_ERROR, e, "a type is required for the new method");
        }
      } else if(IsDisposeMethod(methodDefinition)) {
        return Syntax.BinaryExpression(SyntaxKind.AssignExpression,
          AddValueAccess(e.Arguments.First().Key, true),
          Syntax.LiteralExpression(SyntaxKind.NullLiteralExpression));
      } else if(IsLowMethod(methodDefinition) && e.Arguments.First().Key.GetTypeNode().IsArray()) {
        ArrayTypeNode a = e.Arguments.First().Key.GetTypeNode() as ArrayTypeNode;
        return RoslynUtils.GetLiteralExpression(a.Offset);
      } else if(IsHighMethod(methodDefinition) && e.Arguments.First().Key.GetTypeNode().IsArray()) {
        ArrayTypeNode a = e.Arguments.First().Key.GetTypeNode() as ArrayTypeNode;
        return RoslynUtils.GetLiteralExpression(a.Offset + a.Size - 1);
      } else if(IsWriteMethod(methodDefinition) && e.Arguments.Count > 1) {
        InvocationExpressionSyntax invoke = Syntax.InvocationExpression(
          GetMethodNameExpression(methodDefinition));
        StringBuilder formatStr = new StringBuilder();
        var args = e.Arguments.Values.ToArray();
        int i = 0;
        foreach(var a in args) {
          if(a is ConstExpressionNode)
            formatStr.Append((a as ConstExpressionNode).GetValue());
          else
            formatStr.AppendFormat("{{{0}}}", i++);
        }

        invoke = invoke.AddArgumentListArguments(
          Syntax.Argument(RoslynUtils.GetLiteralExpression(formatStr.ToString())));


        if(i > 0) {
          foreach(var arg in e.Arguments.Values) {
            if(!(arg is ConstExpressionNode)) {
              invoke = invoke.AddArgumentListArguments(
                Syntax.Argument(GenerateExpression(arg)));
            }
          }
        }

        return invoke;
      } else if(IsClassCastMethod(methodDefinition)) {
        return Syntax.CastExpression(
          GetTypeSyntax(root.types.Find(methodDefinition.Name)),
          AddValueAccess(e.Arguments.First().Key, false));

      }

      return Syntax.InvocationExpression(
        GetMethodNameExpression(methodDefinition),
        GenerateCallArguments(e.Arguments));
    }

    private ExpressionSyntax GenerateCallExpression(InvokeExpressionNode e) {
      MethodDefinitionNode methodDefinition = e.GetMethodDefinitionNode();
      if(methodDefinition is PreDefinedMethodNode)
        return GeneratePreDefinedMethodcall(e);

      InvocationExpressionSyntax invoke;

      if(e.Method is DereferenceMemberExpressionNode) {
        // obj->Methode()
        invoke = Syntax.InvocationExpression(GenerateExpression(e.Method));
      } else {
        ExpressionSyntax qualifier;

        if(methodDefinition.IsClassMethod()) {
          if(methodDefinition.Environment.GetClassEnvironment() == curClassEnv)
            qualifier = Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression,
              Syntax.ThisExpression(), Syntax.IdentifierName(methodDefinition.Name));
          else
            qualifier = Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression,
              Syntax.BaseExpression(), Syntax.IdentifierName(methodDefinition.Name));
        } else {
          qualifier = GetMethodNameExpression(methodDefinition);
        }

        invoke = Syntax.InvocationExpression(qualifier);
      }

      return invoke.WithArgumentList(GenerateCallArguments(e.Arguments));
    }

    #endregion

    #region PreDefined
    private bool IsWriteMethod(MethodDefinitionNode m) {
      return m is PreDefinedMethodNode && m.Name.StartsWith("Write");
    }

    private bool IsNewMethod(MethodDefinitionNode m) {
      return m is PreDefinedMethodNode && m.Name.Equals("New");
    }

    private bool IsDisposeMethod(MethodDefinitionNode m) {
      return m is PreDefinedMethodNode && m.Name.Equals("Dispose");
    }

    private bool IsLowMethod(MethodDefinitionNode m) {
      return m is PreDefinedMethodNode && m.Name.Equals("Low");
    }

    private bool IsHighMethod(MethodDefinitionNode m) {
      return m is PreDefinedMethodNode && m.Name.Equals("High");
    }

    private bool IsClassCastMethod(MethodDefinitionNode m) {
      return m is PreDefinedMethodNode && root.types.Exists(m.Name);
    }

    private ExpressionSyntax GetMethodNameExpression(MethodDefinitionNode m) {
      if(m is PreDefinedMethodNode) {
        switch(m.Name) {
          case "Int":
            return Syntax.ParseExpression("System.Convert.ToInt32");
          case "Chr":
            return Syntax.ParseExpression("System.Convert.ToChar");
          case "Real":
            return Syntax.ParseExpression("System.Convert.ToDouble");
          case "Write":
            return Syntax.ParseExpression("System.Console.Write");
          case "WriteLn":
            return Syntax.ParseExpression("System.Console.WriteLine");
          case "Read":
            return Syntax.ParseExpression("ConsoleReader.Read");
          case "Length":
            return Syntax.ParseExpression("BaseUtils.Length");
          case "Low":
            return Syntax.ParseExpression("BaseUtils.Low");
          case "High":
            return Syntax.ParseExpression("BaseUtils.High");
        }
      }

      return Syntax.IdentifierName(m.Name);
    }

    private TypeSyntax GetTypeSyntax(TypeNode t) {
      if(t is BaseTypeNode) {
        switch(t.Basetype) {
          case TypeNode.BaseType.STRING:
            return Syntax.IdentifierName("MutableString");
          case TypeNode.BaseType.BOOL:
            return Syntax.IdentifierName("Bool");
          case TypeNode.BaseType.CHAR:
            return Syntax.IdentifierName("Char");
          case TypeNode.BaseType.INT:
            return Syntax.IdentifierName("Integer");
          case TypeNode.BaseType.REAL:
            return Syntax.IdentifierName("Real");
          case TypeNode.BaseType.VOID:
            return Syntax.PredefinedType(Syntax.Token(SyntaxKind.VoidKeyword));

          default:
            throw new NotImplementedException();
        }
      } else if(t is ArrayTypeNode) {
        ArrayTypeNode a = t as ArrayTypeNode;

        while(a.Typeof is ArrayTypeNode) {
          a = a.Typeof as ArrayTypeNode;
        }

        return Syntax.GenericName(Syntax.Identifier("Array"), Syntax.TypeArgumentList(
          Syntax.SeparatedList<TypeSyntax>(GetTypeSyntax(a.Typeof))
          ));
      } else if(t is PointerTypeNode) {
        PointerTypeNode p = t as PointerTypeNode;

        return Syntax.GenericName("Pointer")
            .WithTypeArgumentList(
              Syntax.TypeArgumentList(
                Syntax.SeparatedList<TypeSyntax>(
                  GetTypeSyntax(p.PointsTo))));
      }

      return Syntax.IdentifierName(t.GetName(true));
    }
    #endregion

    public override Target[] SupportedTargets() {
      return new[] { Target.EXE, Target.CS, Target.DGML };
    }
  }
}
