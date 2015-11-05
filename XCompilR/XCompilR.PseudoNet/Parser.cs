using System.Collections.Generic;
using Pseudo.Net;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;
using System.Globalization;



using XCompilR.Core;
using System;

namespace XCompilR.PSEUDO {



public class Parser : AParser {
	public const int _EOF = 0;
	public const int _IDENT = 1;
	public const int _REAL = 2;
	public const int _INTEGER = 3;
	public const int _STRING = 4;
	public const int _CHAR = 5;
	public const int _COLON = 6;
	public const int _SEMICOLON = 7;
	public const int _COMMA = 8;
	public const int _DEFINE = 9;
	public const int _ASSIGN = 10;
	public const int _DOT = 11;
	public const int _LEFTPAR = 12;
	public const int _RIGHTPAR = 13;
	public const int _ENDKW = 14;
	public const int _PTR = 15;
	public const int maxT = 82;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

class StringList : List<string> {}

private int GetIntegerValue(string identifier){
  ConstExpressionNode v = pp.GetConstVariable(identifier);
  if( v is IntegerNode )
    return ((IntegerNode)v).Value;
  else
    SemErr(String.Format("{0} is not an integer", identifier));

  return -1;
}

private ClassTypeNode EnterClassScope(string className){
  TypeNode type = pp.FindType(className);
  if (type is ClassTypeNode) {
      ClassTypeNode classType = type as ClassTypeNode;
      pp.EnterScope(classType.Environment);
      return classType;
  } else {
    throw new SemanticErrorException(ErrorCode.NOT_A_CLASS, 
      String.Format("'{0}' must be a class", className));
  }
}

private void CheckName(string expected, string actual, string name) {
  if(!expected.Equals(actual))
    SemErr(String.Format("{0} name should must be '{1}', not '{2}'", 
        name, expected, actual));
}

private void CheckMethodSignature(MethodDefinitionNode m1, MethodDefinitionNode m2) {
  if(!m1.EqualHeader(m2))
    SemErr("different method signature between declaration and definition at " + m1.FullName);
}

private SyntaxTreeBuilder.SearchScope GetClassScope() {
  SyntaxTreeBuilder.SearchScope scope = SyntaxTreeBuilder.SearchScope.GLOBAL;
  if(pp.InClassScope())
    scope = SyntaxTreeBuilder.SearchScope.THIS_CLASS;
  else
    SemErr("'this' is only allowed within a class");
  return scope;
}

private SyntaxTreeBuilder pp{ get; set; }
private bool isLValue = false;


// Return the n-th token after the current lookahead token
Token Peek (int n) {
    scanner.ResetPeek();
    Token x = la;
    while (n > 0) { x = scanner.Peek(); n--; }
    return x;
}


//---------- conflict resolvers ------------------------------
private bool IsMethod() {
    return Peek(1).kind == _DOT;
}

private bool IsVarSpec() {
  Token x = Peek(1);
  return (x.kind == _COMMA || x.kind == _COLON);
}

private bool IsTypeSpec() {
  Token x = Peek(1);
  return (x.kind == _DEFINE);
}

private bool IsCall() {
  return la.kind==_LEFTPAR;
}

private bool IsBaseClassSelector(string baseClassName) {
  return la.kind==_DOT && 
         pp.InClassScope() && 
         pp.GetSubClass(baseClassName)!=null;
}

private ExpressionNode GetClassMember(string className, string memberName) {
  ExpressionNode e;
  var env = pp.GetSubClass(className);
  if(IsCall())
    e = env.Find(memberName, false);
  else
    e = env.FindVariable(memberName, false);
	
	if(!e.IsPublic)
    SemErr("no public member '" + memberName +"' in '" + className + "'");
    
  return e;
}

private ExpressionNode GetExpression(string name, SyntaxTreeBuilder.SearchScope scope) {
  ExpressionNode e;
  if(IsCall())
    e = pp.Find(name, scope);
  else if(pp.ExistsType(name))
    e = new TypeReferenceExpressionNode(pp.FindType(name));
  else
    e = pp.FindVariable(name, scope);

  return e;
}

private bool NextTokenIsMembersOf(ExpressionNode n){
  if(la.kind != _IDENT)
    return false;

  if(isLValue)
    return true;

  if(n.GetTypeNode() is PointerTypeNode)  {
    PointerTypeNode p = n.GetTypeNode() as PointerTypeNode;
    Token x = Peek(1);
    switch(x.kind)
    {
      case _ASSIGN:
      case _LEFTPAR:
      case _PTR:
      {
        bool isMember = p.PointsTo.HasMember(la.val);
        bool isVariable = pp.Exists(la.val);
        if(isVariable && !isMember)
          return false;
        else
        if(!isVariable && isMember)
          return true;
        else
          throw new SemanticErrorException(ErrorCode.AMBIGUOUS_STATEMENT, 
            "Use a semicolon to specify the seperation of the statments");
      }

      default:
        return true;
    }
  }
  return false;
}



	public Parser() {
    }

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void PSEUDO() {
        pp = new SyntaxTreeBuilder();
	    ProgramRoot = pp.ProgramRoot;
		string name;
		StatementNode n;                           
		Expect(16);
		Ident(out name);
		pp.Name = name;                            
		while (StartOf(1)) {
			if (la.kind == 23) {
				ConstDecl();
			} else if (la.kind == 17) {
				TypeDecl();
			} else if (la.kind == 24 || la.kind == 25) {
				VarDecl(true);
			} else if (IsMethod()) {
				MethodDef();
			} else {
				AlgDef();
			}
		}
		pp.EnterEntryPointScope();                 
		Block(out n);
		pp.MainBody = n;
		pp.LeaveScope();                           
	}

	void Ident(out string name) {
		Expect(1);
		name = t.val;                              
	}

	void ConstDecl() {
		Expect(23);
		ConstSpec();
		while (IsTypeSpec()) {
			ConstSpec();
		}
	}

	void TypeDecl() {
		Expect(17);
		TypeSpec();
		while (IsTypeSpec()) {
			TypeSpec();
		}
	}

	void VarDecl(bool isStatic) {
		if (la.kind == 24) {
			Get();
			isStatic = true;                           
		}
		Expect(25);
		VarSpec(isStatic);
		while (IsVarSpec()) {
			VarSpec(isStatic, true);
		}
	}

	void MethodDef() {
		string className, funcname, className2;
		MethodDefinitionNode m;                      
		Ident(out className);
		Expect(11);
		EnterClassScope(className);                
		MethodeDefBody(out m);
		Ident(out className2);
		Expect(11);
		Ident(out funcname);
		CheckName(className, className2, "class");
		CheckName(m.Name, funcname, "method");
		pp.LeaveScope();                           
	}

	void AlgDef() {
		string funcname;
		StatementNode block;
		MethodDefinitionNode m =
		 new MethodDefinitionNode(pp.EnterScope()); 
		AlgHead(ref m);
		if (la.kind == 23) {
			ConstDecl();
		}
		while (la.kind == 24 || la.kind == 25) {
			VarDecl(false);
		}
		Block(out block);
		m.Body = block;                            
		Ident(out funcname);
		CheckName(m.Name, funcname, "function");
		pp.LeaveScope();
		m.IsStatic = true;
		pp.AddFunction(m);                         
	}

	void Block(out StatementNode b) {
		while (!(la.kind == 0 || la.kind == 39)) {SynErr(83); Get();}
		Expect(39);
		StatList(out b);
		Expect(14);
	}

	void TypeSpec() {
		string name, aliasto; 
		TypeNode tn;                               
		Ident(out name);
		Expect(9);
		if (la.kind == 1 || la.kind == 15) {
			Type(out aliasto);
			pp.AddAliasType(name, aliasto);            
		} else if (la.kind == 18) {
			ArrayType(out tn);
			pp.AddType(name, tn);                      
		} else if (la.kind == 22) {
			CompoundType(name, out tn);
			pp.AddType(name, tn);                      
		} else if (la.kind == 26 || la.kind == 27) {
			ClassType(name, out tn);
			pp.AddType(name, tn);                      
		} else SynErr(84);
		if (la.kind == 7) {
			Get();
		}
	}

	void Type(out string typename) {
		typename = ""; string name = "";           
		if (la.kind == 15) {
			Get();
			typename += "->";                          
		}
		Ident(out name);
		typename += name;                          
	}

	void ArrayType(out TypeNode tn) {
		ArrayTypeNode atn = new ArrayTypeNode();
		string typename; tn = atn;                 
		Expect(18);
		Expect(19);
		ArraySpec(ref atn);
		while (la.kind == 8) {
			ArrayTypeNode tmp = new ArrayTypeNode();
			atn.Typeof = tmp;
			atn = tmp;                                 
			Get();
			ArraySpec(ref atn);
		}
		Expect(20);
		Expect(21);
		Type(out typename);
		atn.Typeof = pp.FindOrCreateType(typename);
	}

	void CompoundType(string name, out TypeNode tn) {
		StructTypeNode ctn =
		new StructTypeNode(
		  pp.EnterScope(true), name);             
		Expect(22);
		CompoundSpec(ref ctn);
		while (la.kind == 1) {
			CompoundSpec(ref ctn);
		}
		Expect(14);
		tn = ctn;
		pp.LeaveScope();                           
	}

	void ClassType(string name, out TypeNode tn) {
		string baseClassName = "";
		bool isAbstract = false;
		ClassTypeNode baseClass = null;            
		if (la.kind == 26) {
			Get();
			isAbstract = true;
		}
		Expect(27);
		if (la.kind == 28) {
			Get();
			Expect(29);
			Ident(out baseClassName);
			baseClass = EnterClassScope(baseClassName);
		}
		tn = new ClassTypeNode(pp.EnterScope(true),
		                      name,
		                      baseClass,
		                      isAbstract);        
		while (StartOf(2)) {
			bool isVisible = true;                     
			if (la.kind == 23) {
				ConstDecl();
			} else {
				if (la.kind == 30 || la.kind == 31) {
					if (la.kind == 30) {
						Get();
					} else {
						Get();
						isVisible = false;                         
					}
				}
				if (IsVarSpec()) {
					VarSpec(false, isVisible);
				} else if (la.kind == 1 || la.kind == 26 || la.kind == 32) {
					MethodDecl(isVisible);
				} else SynErr(85);
			}
		}
		pp.LeaveScope(); // leave current class
		if(baseClass!=null) // leave base class
		 pp.LeaveScope();                         
		Expect(14);
	}

	void ArraySpec(ref ArrayTypeNode atn) {
		int imin = -1, imax = -1;
		string smin = "", smax = "";               
		if (la.kind == 3) {
			Integer(out imin);
		} else if (la.kind == 1) {
			Ident(out smin);
			imin = GetIntegerValue(smin);              
		} else SynErr(86);
		if (la.kind == 6) {
			Get();
			if (la.kind == 3) {
				Integer(out imax);
			} else if (la.kind == 1) {
				Ident(out smax);
				imax = GetIntegerValue(smax);              
			} else SynErr(87);
		}
		if(imax>0) atn.SetDimension(imin, imax);
		else       atn.SetDimension(imin);         
	}

	void Integer(out int ival) {
		Expect(3);
		ival = Convert.ToInt32(t.val);             
	}

	void CompoundSpec(ref StructTypeNode ctn) {
		StringList varnames; TypeNode tn = null;   
		IdentList(out varnames);
		Expect(6);
		if (la.kind == 1 || la.kind == 15) {
			string typename;                           
			Type(out typename);
			tn = pp.FindOrCreateType(typename);        
		} else if (la.kind == 18) {
			ArrayType(out tn);
		} else if (la.kind == 22) {
			CompoundType(varnames[0], out tn);
		} else SynErr(88);
		if (la.kind == 7) {
			Get();
		}
		foreach(string name in varnames)
		 pp.AddVariable(name, tn, false, true);   
	}

	void IdentList(out StringList v) {
		v = new StringList();
		string name;                               
		Ident(out name);
		v.Add(name);                               
		while (la.kind == 8) {
			Get();
			Ident(out name);
			v.Add(name);                               
		}
	}

	void ConstSpec() {
		string name;
		int ival;
		double rval;
		char cval;                                 
		Ident(out name);
		Expect(9);
		if (la.kind == 3) {
			Integer(out ival);
			pp.AddConstVariable(name, ival);           
		} else if (la.kind == 2) {
			Real(out rval);
			pp.AddConstVariable(name, rval);           
		} else if (la.kind == 5) {
			Char(out cval);
			pp.AddConstVariable(name, cval);           
		} else SynErr(89);
		if (la.kind == 7) {
			Get();
		}
	}

	void Real(out double rval) {
		Expect(2);
		rval = Double.Parse(t.val,
		        CultureInfo.InvariantCulture);    
	}

	void Char(out char c) {
		Expect(5);
		c = t.val[1];                              
	}

	void VarSpec(bool isStatic, bool isPublic=true) {
		StringList varnames;
		TypeNode tn = null;
		ExpressionNode value = null;               
		IdentList(out varnames);
		ExpectWeak(6, 3);
		if (la.kind == 1 || la.kind == 15) {
			string typename;                           
			Type(out typename);
			tn = pp.FindOrCreateType(typename);        
			if (la.kind == 10) {
				Get();
				Expr(out value);
			}
		} else if (la.kind == 18) {
			ArrayType(out tn);
		} else if (la.kind == 22) {
			CompoundType(null, out tn);
		} else SynErr(90);
		if (la.kind == 7) {
			Get();
		}
		foreach(string name in varnames)
		 pp.AddVariable(name,
		                tn,
		                isStatic,
		                isPublic,
		                value);                   
	}

	void Expr(out ExpressionNode e) {
		ExpressionNode e2;                         
		AndExpr(out e);
		while (la.kind == 57) {
			Get();
			AndExpr(out e2);
			e = new BinaryExpressionNode(
			     BinaryExpressionNode.Operator.OR,
			     e,
			     e2);                                 
		}
	}

	void MethodDecl(bool isPublic) {
		MethodDefinitionNode m =
		 new MethodDefinitionNode(pp.EnterScope()); 
		if (la.kind == 26 || la.kind == 32) {
			if (la.kind == 26) {
				Get();
				m.IsAbstract = true;                       
			} else {
				Get();
				m.IsOverride = true;                       
			}
		}
		AlgHead(ref m);
		pp.LeaveScope();
		m.IsPublic = isPublic;
		pp.AddFunction(m);                         
	}

	void AlgHead(ref MethodDefinitionNode m) {
		string funcname;
		string returntypename = "void";            
		Ident(out funcname);
		Expect(12);
		while (StartOf(4)) {
			ArgumentDirection d;
			string parname, partype;                   
			ParamKind(out d);
			Ident(out parname);
			Expect(6);
			Type(out partype);
			m.AddParameter(d, parname,
			 pp.FindOrCreateType(partype));           
		}
		Expect(13);
		if (la.kind == 6) {
			Get();
			Type(out returntypename);
		}
		if (la.kind == 7) {
			Get();
		}
		m.Name = funcname;
		m.ReturnType =
		 pp.FindOrCreateType(returntypename);     
	}

	void MethodeDefBody(out MethodDefinitionNode m) {
		MethodDefinitionNode m2 =
		 new MethodDefinitionNode(pp.EnterScope());
		StatementNode block;                       
		AlgHead(ref m2);
		pp.LeaveScope();
		m = pp.FindFunctionDefinition(m2.Name);
		CheckMethodSignature(m, m2);
		pp.EnterScope(m.Environment);              
		if (la.kind == 23) {
			ConstDecl();
		}
		while (la.kind == 24 || la.kind == 25) {
			VarDecl(false);
		}
		Block(out block);
		m.Body = block;
		pp.LeaveScope();                           
	}

	void ParamKind(out ArgumentDirection d) {
		d = ArgumentDirection.IN;                  
		if (la.kind == 33 || la.kind == 34) {
			ParIn();
		} else if (la.kind == 35 || la.kind == 36) {
			ParOut();
			d = ArgumentDirection.OUT;                 
		} else if (la.kind == 37 || la.kind == 38) {
			ParIo();
			d = ArgumentDirection.IO;                  
		} else SynErr(91);
	}

	void ParIn() {
		if (la.kind == 33) {
			ExpectWeak(33, 5);
		} else if (la.kind == 34) {
			Get();
		} else SynErr(92);
	}

	void ParOut() {
		if (la.kind == 35) {
			ExpectWeak(35, 6);
		} else if (la.kind == 36) {
			Get();
		} else SynErr(93);
	}

	void ParIo() {
		if (la.kind == 37) {
			ExpectWeak(37, 6);
		} else if (la.kind == 38) {
			Get();
		} else SynErr(94);
	}

	void StatList(out StatementNode s) {
		BlockStatementNode b = 
		new BlockStatementNode();
		StatementNode s1;                          
		while (StartOf(7)) {
			Stat(out s1);
			b.AddStatement(s1);                        
		}
		if (la.kind == 56) {
			ReturnStat(out s1);
			b.AddStatement(s1);                        
			if (la.kind == 7) {
				Get();
			}
		}
		s = b;                                     
	}

	void Stat(out StatementNode s) {
		s = null;
		ExpressionNode lval, rval;
		ArgumentList al;                           
		if (la.kind == 1 || la.kind == 81) {
			isLValue = true;                           
			Qualifier(out lval);
			isLValue = false;                          
			if (la.kind == 10) {
				Assign(out rval);
				s = new AssignStatementNode(lval, rval);   
			} else if (la.kind == 12) {
				Call(out al);
				s = new InvokeStatementNode(
				     new InvokeExpressionNode(lval, al)); 
			} else SynErr(95);
		} else if (StartOf(8)) {
			while (!(StartOf(9))) {SynErr(96); Get();}
			switch (la.kind) {
			case 40: {
				IfStat(out s);
				break;
			}
			case 47: {
				WhileStat(out s);
				break;
			}
			case 52: {
				RepeatStat(out s);
				break;
			}
			case 49: {
				ForStat(out s);
				break;
			}
			case 54: {
				BreakStat(out s);
				break;
			}
			case 55: {
				HaltStat(out s);
				break;
			}
			case 43: {
				Case(out s);
				break;
			}
			case 39: {
				Block(out s);
				break;
			}
			}
		} else SynErr(97);
		if (la.kind == 7) {
			Get();
		}
	}

	void ReturnStat(out StatementNode s) {
		ExpressionNode e = null;                   
		Expect(56);
		if (StartOf(10)) {
			Expr(out e);
		}
		s = new ReturnStatement(e);                
	}

	void Qualifier(out ExpressionNode e) {
		string classOrIdentName, name;
		ExpressionNode index;
		e = null;
		SyntaxTreeBuilder.SearchScope scope = SyntaxTreeBuilder.SearchScope.GLOBAL;    
		if (la.kind == 81) {
			Get();
			scope = GetClassScope();                   
			Expect(15);
		}
		Ident(out classOrIdentName);
		if (IsBaseClassSelector(classOrIdentName)) {
			Expect(11);
			Ident(out name);
			e = GetClassMember(classOrIdentName, name);
		} else if (StartOf(11)) {
			e = GetExpression(classOrIdentName, scope);
		} else SynErr(98);
		while (la.kind == 11 || la.kind == 15 || la.kind == 19) {
			if (la.kind == 15) {
				Get();
				if (NextTokenIsMembersOf(e)) {
					Ident(out name);
					e = new DereferenceMemberExpressionNode(e, name);
				} else if (StartOf(11)) {
					e = new DereferenceExpressionNode(e);            
				} else SynErr(99);
			} else if (la.kind == 11) {
				Get();
				Ident(out name);
				e = new MemberSelectorExpressionNode(e, name);   
			} else {
				Get();
				Expr(out index);
				e = new ArrayIndexerExpressionNode(e, index);    
				while (la.kind == 8) {
					Get();
					Expr(out index);
					e = new ArrayIndexerExpressionNode(e, index);    
				}
				Expect(20);
			}
		}
	}

	void Assign(out ExpressionNode e) {
		Expect(10);
		Expr(out e);
	}

	void Call(out ArgumentList al) {
		ArgumentDirection d;
		ExpressionNode e;
		al = new ArgumentList();                   
		Expect(12);
		while (StartOf(4)) {
			ParamKind(out d);
			Expr(out e);
			al.Add(d, e);                              
		}
		Expect(13);
	}

	void IfStat(out StatementNode s) {
		ExpressionNode e;
		StatementNode b;
		IfStatement ifstat;                        
		Expect(40);
		Expr(out e);
		ExpectWeak(41, 12);
		StatList(out b);
		ifstat = new IfStatement(e, b);            
		if (la.kind == 42) {
			Get();
			StatList(out b);
			ifstat.ElseStat = b;                       
		}
		Expect(14);
		s = ifstat;                                
	}

	void WhileStat(out StatementNode b) {
		ExpressionNode e;
		StatementNode s;                           
		Expect(47);
		Expr(out e);
		Expect(48);
		StatList(out s);
		b = new WhileStatement(e, s);              
		Expect(14);
	}

	void RepeatStat(out StatementNode b) {
		ExpressionNode e;
		StatementNode s;                           
		Expect(52);
		StatList(out s);
		Expect(53);
		Expr(out e);
		b = new RepeatStatement(e, s);             
	}

	void ForStat(out StatementNode b) {
		string name;
		StatementNode s;
		ExpressionNode start, end;
		bool down = false;                         
		Expect(49);
		Ident(out name);
		Expect(10);
		Expr(out start);
		if (la.kind == 50) {
			Get();
		} else if (la.kind == 51) {
			Get();
			down = true; 
		} else SynErr(100);
		Expr(out end);
		Expect(48);
		StatList(out s);
		Expect(14);
		b = new ForStatement(
		         pp.FindVariable(name),
		         start, end, s, down);            
	}

	void BreakStat(out StatementNode s) {
		Expect(54);
		s = new BreakStatement();                  
	}

	void HaltStat(out StatementNode s) {
		Expect(55);
		s = new HaltStatement();                   
	}

	void Case(out StatementNode s) {
		ExpressionNode e;
		StatementNode stmt;                        
		Expect(43);
		Expr(out e);
		Expect(21);
		CaseStatement caseStmt =
		 new CaseStatement(e);
		s = caseStmt;                              
		while (la.kind == 44) {
			Get();
			CaseLabelExprList(out e);
			Expect(6);
			StatList(out stmt);
			caseStmt.AddCase(e, stmt);                 
		}
		if (la.kind == 45) {
			Get();
			Expect(6);
			StatList(out stmt);
			caseStmt.SetDefaultStatement(stmt);        
		}
		Expect(14);
	}

	void CaseLabelExprList(out ExpressionNode e) {
		ExpressionNode e2;                         
		CaseLabelExpr(out e);
		if (la.kind == 8) {
			ValueCollectionNode c =
			 new ValueCollectionNode();
			c.AddValues(e);
			e = c;                                     
			Get();
			CaseLabelExpr(out e2);
			c.AddValues(e2);                           
			while (la.kind == 8) {
				Get();
				CaseLabelExpr(out e2);
				c.AddValues(e2);                           
			}
		}
	}

	void CaseLabelExpr(out ExpressionNode e) {
		Expr(out e);
		if (la.kind == 46) {
			ExpressionNode e2;                         
			Get();
			Expr(out e2);
			e = new RangeExpressionNode(e, e2);        
		}
	}

	void AndExpr(out ExpressionNode e) {
		ExpressionNode e2;                         
		RelExpr(out e);
		while (la.kind == 58) {
			Get();
			RelExpr(out e2);
			e = new BinaryExpressionNode(
			     BinaryExpressionNode.Operator.AND,
			     e,
			     e2);                                 
		}
	}

	void RelExpr(out ExpressionNode e) {
		ExpressionNode e2;
		BinaryExpressionNode.Operator op =
		 BinaryExpressionNode.Operator.UNDEF;     
		SimpleExpr(out e);
		if (StartOf(13)) {
			switch (la.kind) {
			case 9: {
				Get();
				op = BinaryExpressionNode.Operator.EQ;     
				break;
			}
			case 59: case 60: {
				if (la.kind == 59) {
					Get();
				} else {
					Get();
				}
				op = BinaryExpressionNode.Operator.NE;     
				break;
			}
			case 61: {
				Get();
				op = BinaryExpressionNode.Operator.LT;     
				break;
			}
			case 62: case 63: {
				if (la.kind == 62) {
					Get();
				} else {
					Get();
				}
				op = BinaryExpressionNode.Operator.LE;     
				break;
			}
			case 64: {
				Get();
				op = BinaryExpressionNode.Operator.GT;     
				break;
			}
			case 65: case 66: {
				if (la.kind == 65) {
					Get();
				} else {
					Get();
				}
				op = BinaryExpressionNode.Operator.GE;     
				break;
			}
			case 67: {
				Get();
				op = BinaryExpressionNode.Operator.ISA;    
				break;
			}
			}
			SimpleExpr(out e2);
			e = new BinaryExpressionNode(op, e, e2);   
		}
	}

	void SimpleExpr(out ExpressionNode e) {
		ExpressionNode e2;
		BinaryExpressionNode.Operator op =
		 BinaryExpressionNode.Operator.UNDEF;     
		Term(out e);
		while (la.kind == 68 || la.kind == 69) {
			if (la.kind == 68) {
				Get();
				op = BinaryExpressionNode.Operator.PLUS;   
			} else {
				Get();
				op = BinaryExpressionNode.Operator.MINUS;  
			}
			Term(out e2);
			e = new BinaryExpressionNode(op, e, e2);   
		}
	}

	void Term(out ExpressionNode e) {
		ExpressionNode e2;
		BinaryExpressionNode.Operator op =
		 BinaryExpressionNode.Operator.UNDEF;     
		NotFact(out e);
		while (StartOf(14)) {
			if (la.kind == 70) {
				Get();
				op = BinaryExpressionNode.Operator.MULT;   
			} else if (la.kind == 71 || la.kind == 72) {
				if (la.kind == 71) {
					Get();
				} else {
					Get();
				}
				op = BinaryExpressionNode.Operator.DIV;    
			} else {
				if (la.kind == 73) {
					Get();
				} else {
					Get();
				}
				op = BinaryExpressionNode.Operator.MOD;    
			}
			NotFact(out e2);
			e = new BinaryExpressionNode(op, e, e2);   
		}
	}

	void NestedExpr(out ExpressionNode e) {
		Expect(12);
		Expr(out e);
		Expect(13);
	}

	void NotFact(out ExpressionNode e) {
		ExpressionNode e2;
		UnaryExpressionNode.Operator op =
		 UnaryExpressionNode.Operator.UNDEF;      
		if (la.kind == 68 || la.kind == 69 || la.kind == 75) {
			if (la.kind == 75) {
				Get();
				op = UnaryExpressionNode.Operator.NOT;     
			} else if (la.kind == 68) {
				Get();
				op = UnaryExpressionNode.Operator.PLUS;    
			} else {
				Get();
				op = UnaryExpressionNode.Operator.MINUS;   
			}
		}
		Fact(out e2);
		if(op!=UnaryExpressionNode.Operator.UNDEF)
		 e = new UnaryExpressionNode(op, e2);
		else
		 e = e2;                                  
	}

	void Fact(out ExpressionNode e) {
		e = null;
		ArgumentList al;
		int ival;
		double rval;
		string sval;
		char cval;                                 
		switch (la.kind) {
		case 1: case 81: {
			Qualifier(out e);
			if (la.kind == 12) {
				Call(out al);
				e = new InvokeExpressionNode(e, al);       
			}
			break;
		}
		case 79: {
			NewStat(out e);
			break;
		}
		case 80: {
			AddrOfStat(out e);
			break;
		}
		case 3: {
			Integer(out ival);
			e = new IntegerNode(ival);                 
			break;
		}
		case 2: {
			Real(out rval);
			e = new RealNode(rval);                    
			break;
		}
		case 4: {
			StringLiteral(out sval);
			e = new StringNode(sval);                  
			break;
		}
		case 5: {
			Char(out cval);
			e = new CharNode(cval);                    
			break;
		}
		case 76: {
			Get();
			e = new BoolNode(false);                   
			break;
		}
		case 77: {
			Get();
			e = new BoolNode(true);                    
			break;
		}
		case 78: {
			Get();
			e = new NullNode();                        
			break;
		}
		case 12: {
			NestedExpr(out e);
			break;
		}
		default: SynErr(101); break;
		}
	}

	void NewStat(out ExpressionNode e) {
		string tn;
		var al = new ArgumentList();
		ExpressionNode par;                        
		Expect(79);
		Expect(12);
		ParIn();
		Type(out tn);
		al.Add(ArgumentDirection.IN,
		 new TypeReferenceExpressionNode(
		   pp.FindType(tn)));                     
		while (la.kind == 33 || la.kind == 34) {
			ParIn();
			Expr(out par);
			al.Add(ArgumentDirection.IN, par);         
		}
		Expect(13);
		e = new InvokeExpressionNode(
		 pp.FindFunction("New"), al);             
	}

	void AddrOfStat(out ExpressionNode e) {
		Expect(80);
		Expect(12);
		Expect(33);
		Qualifier(out e);
		e = new AddressOfExpressionNode(e);        
		Expect(13);
	}

	void StringLiteral(out string s) {
		Expect(4);
		s = t.val.Substring(1, t.val.Length-2);    
	}



    public override void InitParser(AScanner scanner)
    {
        this.scanner = (Scanner)scanner;
        ReInitParser();
    }

    public override void ReInitParser()
    {
        errors = new Errors();
    }

	public override void Parse(string fileName) {
	    scanner.Scan(fileName);
		la = new Token();
		la.val = "";		
		Get();
		PSEUDO();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _x,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _T,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_x,_x},
		{_T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_x,_x},
		{_x,_T,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_T,_x,_T, _T,_T,_x,_T, _T,_T,_T,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_T,_x,_x},
		{_T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_T,_T, _x,_x,_x,_T, _x,_T,_x,_x, _T,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "IDENT expected"; break;
			case 2: s = "REAL expected"; break;
			case 3: s = "INTEGER expected"; break;
			case 4: s = "STRING expected"; break;
			case 5: s = "CHAR expected"; break;
			case 6: s = "COLON expected"; break;
			case 7: s = "SEMICOLON expected"; break;
			case 8: s = "COMMA expected"; break;
			case 9: s = "DEFINE expected"; break;
			case 10: s = "ASSIGN expected"; break;
			case 11: s = "DOT expected"; break;
			case 12: s = "LEFTPAR expected"; break;
			case 13: s = "RIGHTPAR expected"; break;
			case 14: s = "ENDKW expected"; break;
			case 15: s = "PTR expected"; break;
			case 16: s = "\"program\" expected"; break;
			case 17: s = "\"type\" expected"; break;
			case 18: s = "\"array\" expected"; break;
			case 19: s = "\"[\" expected"; break;
			case 20: s = "\"]\" expected"; break;
			case 21: s = "\"of\" expected"; break;
			case 22: s = "\"compound\" expected"; break;
			case 23: s = "\"const\" expected"; break;
			case 24: s = "\"static\" expected"; break;
			case 25: s = "\"var\" expected"; break;
			case 26: s = "\"abstract\" expected"; break;
			case 27: s = "\"class\" expected"; break;
			case 28: s = "\"based\" expected"; break;
			case 29: s = "\"on\" expected"; break;
			case 30: s = "\"public\" expected"; break;
			case 31: s = "\"private\" expected"; break;
			case 32: s = "\"override\" expected"; break;
			case 33: s = "\"in\" expected"; break;
			case 34: s = "\"\u2193\" expected"; break;
			case 35: s = "\"out\" expected"; break;
			case 36: s = "\"\u2191\" expected"; break;
			case 37: s = "\"io\" expected"; break;
			case 38: s = "\"\u2193\u2191\" expected"; break;
			case 39: s = "\"begin\" expected"; break;
			case 40: s = "\"if\" expected"; break;
			case 41: s = "\"then\" expected"; break;
			case 42: s = "\"else\" expected"; break;
			case 43: s = "\"case\" expected"; break;
			case 44: s = "\"|\" expected"; break;
			case 45: s = "\"otherwise\" expected"; break;
			case 46: s = "\"..\" expected"; break;
			case 47: s = "\"while\" expected"; break;
			case 48: s = "\"do\" expected"; break;
			case 49: s = "\"for\" expected"; break;
			case 50: s = "\"to\" expected"; break;
			case 51: s = "\"downto\" expected"; break;
			case 52: s = "\"repeat\" expected"; break;
			case 53: s = "\"until\" expected"; break;
			case 54: s = "\"break\" expected"; break;
			case 55: s = "\"halt\" expected"; break;
			case 56: s = "\"return\" expected"; break;
			case 57: s = "\"or\" expected"; break;
			case 58: s = "\"and\" expected"; break;
			case 59: s = "\"!=\" expected"; break;
			case 60: s = "\"\u2260\" expected"; break;
			case 61: s = "\"<\" expected"; break;
			case 62: s = "\"<=\" expected"; break;
			case 63: s = "\"\u2264\" expected"; break;
			case 64: s = "\">\" expected"; break;
			case 65: s = "\">=\" expected"; break;
			case 66: s = "\"\u2265\" expected"; break;
			case 67: s = "\"isA\" expected"; break;
			case 68: s = "\"+\" expected"; break;
			case 69: s = "\"-\" expected"; break;
			case 70: s = "\"*\" expected"; break;
			case 71: s = "\"/\" expected"; break;
			case 72: s = "\"div\" expected"; break;
			case 73: s = "\"%\" expected"; break;
			case 74: s = "\"mod\" expected"; break;
			case 75: s = "\"not\" expected"; break;
			case 76: s = "\"false\" expected"; break;
			case 77: s = "\"true\" expected"; break;
			case 78: s = "\"null\" expected"; break;
			case 79: s = "\"New\" expected"; break;
			case 80: s = "\"AddrOf\" expected"; break;
			case 81: s = "\"this\" expected"; break;
			case 82: s = "??? expected"; break;
			case 83: s = "this symbol not expected in Block"; break;
			case 84: s = "invalid TypeSpec"; break;
			case 85: s = "invalid ClassType"; break;
			case 86: s = "invalid ArraySpec"; break;
			case 87: s = "invalid ArraySpec"; break;
			case 88: s = "invalid CompoundSpec"; break;
			case 89: s = "invalid ConstSpec"; break;
			case 90: s = "invalid VarSpec"; break;
			case 91: s = "invalid ParamKind"; break;
			case 92: s = "invalid ParIn"; break;
			case 93: s = "invalid ParOut"; break;
			case 94: s = "invalid ParIo"; break;
			case 95: s = "invalid Stat"; break;
			case 96: s = "this symbol not expected in Stat"; break;
			case 97: s = "invalid Stat"; break;
			case 98: s = "invalid Qualifier"; break;
			case 99: s = "invalid Qualifier"; break;
			case 100: s = "invalid ForStat"; break;
			case 101: s = "invalid Fact"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
	
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}