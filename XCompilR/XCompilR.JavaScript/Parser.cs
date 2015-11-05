using System.Collections;
using System.Text;
using System.Reflection;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;



using System;
using System.Dynamic;
using XCompilR.Core;

namespace XCompilR.JavaScript {



public class Parser : AParser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _intCon = 2;
	public const int _realCon = 3;
	public const int _charCon = 4;
	public const int _stringCon = 5;
	public const int _break = 6;
	public const int _bool = 7;
	public const int _case = 8;
	public const int _catch = 9;
	public const int _continue = 10;
	public const int _char = 11;
	public const int _default = 12;
	public const int _do = 13;
	public const int _double = 14;
	public const int _else = 15;
	public const int _false = 16;
	public const int _finally = 17;
	public const int _float = 18;
	public const int _for = 19;
	public const int _function = 20;
	public const int _goto = 21;
	public const int _if = 22;
	public const int _int = 23;
	public const int _long = 24;
	public const int _new = 25;
	public const int _null = 26;
	public const int _object = 27;
	public const int _return = 28;
	public const int _string = 29;
	public const int _switch = 30;
	public const int _this = 31;
	public const int _throw = 32;
	public const int _true = 33;
	public const int _try = 34;
	public const int _typeof = 35;
	public const int _var = 36;
	public const int _void = 37;
	public const int _while = 38;
	public const int _and = 39;
	public const int _assgn = 40;
	public const int _colon = 41;
	public const int _comma = 42;
	public const int _dec = 43;
	public const int _div = 44;
	public const int _dot = 45;
	public const int _eq = 46;
	public const int _eqtyp = 47;
	public const int _gt = 48;
	public const int _gte = 49;
	public const int _inc = 50;
	public const int _lbrace = 51;
	public const int _lbrack = 52;
	public const int _lpar = 53;
	public const int _lt = 54;
	public const int _lte = 55;
	public const int _minus = 56;
	public const int _mod = 57;
	public const int _neq = 58;
	public const int _not = 59;
	public const int _or = 60;
	public const int _plus = 61;
	public const int _rbrace = 62;
	public const int _rbrack = 63;
	public const int _rpar = 64;
	public const int _scolon = 65;
	public const int _tilde = 66;
	public const int _times = 67;
	public const int _xor = 68;
	public const int maxT = 82;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;

    public ExpressionStatementSyntax ExpressionStatementSyntax;
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

enum TypeKind {simple, array, pointer}

/*------------------------- modifier handling -----------------------------*/

[Flags]
enum Modifier {
	/* available modifiers (reserve one bit per modifier) */
	@new     = 0x0001, @function = 0x0002, 

	/* sets of modifiers that can be attached to certain program elements    *
	 * e.g., "constants" marks all modifiers that may be used with constants */
	none          = 0x0000,
	fields        = @new,
	propEvntMeths = @function,
	all           = 0x3fff
}

class Modifiers {
	private Modifier cur = Modifier.none;
	private Parser parser;
	
	public Modifiers(Parser parser) {
		this.parser = parser;
	}
	
	public void Add (Modifier m) {
		if ((cur & m) == 0) cur |= m;
		else parser.Error("modifier " + m + " already defined");
	}
	
	public void Add (Modifiers m) { Add(m.cur); }

	public bool IsNone { get { return cur == Modifier.none; } }

	public void Check (Modifier allowed) {
		Modifier wrong = cur & (allowed ^ Modifier.all);
		if (wrong != Modifier.none)
		  parser.Error("modifier(s) " + wrong + " not allowed here");
  }
}

/*----------------------------- token sets -------------------------------*/

const int maxTerminals = 160;  // set size

static BitArray NewSet(params int[] values) {
	BitArray a = new BitArray(maxTerminals);
	foreach (int x in values) a[x] = true;
	return a;
}

static BitArray
	unaryOp      = NewSet(_plus, _minus, _not, _tilde, _inc, _dec, _true, _false),
	binaryOp     = NewSet(_plus, _minus, _times, _div, _mod, _and, _or, _xor,
	               _eq, _eqtyp, _neq, _gt, _lt, _gte, _lte),
	typeKW       = NewSet(_char, _bool, _object, _string, _int, _long, _float, _double),
	unaryHead    = NewSet(_plus, _minus, _not, _tilde, _times, _inc, _dec, _and),
	assnStartOp  = NewSet(_plus, _minus, _not, _tilde, _times),
	castFollower = NewSet(_tilde, _not, _lpar, _ident,
	               /* literals */
	               _intCon, _realCon, _charCon, _stringCon,
	               /* any keyword expect as and is */
	               _bool, _break, _case, _catch, _char, _continue, _default,
				   _do, _double, _else, _false, _finally, _float, _for, _function,
				   _goto, _if, _int, _long, _new, _null, _object,
				   _return, _string, _switch, _this, _throw,
				   _true, _try, _typeof, _var, _void, _while
	               );

/*---------------------------- auxiliary methods ------------------------*/

void Error (string s) {
	if (errDist >= minErrDist) errors.SemErr(la.line, la.col, s);
	errDist = 0;
}

bool IsTypeCast () {
	if (la.kind != _lpar) return false;
	if (IsSimpleTypeCast()) return true;
	return GuessTypeCast();
}

// "(" typeKW ")"
bool IsSimpleTypeCast () {
	// assert: la.kind == _lpar
	scanner.ResetPeek();
	Token pt1 = scanner.Peek();
	Token pt = scanner.Peek();
	return typeKW[pt1.kind] && pt.kind == _rpar;
}

// "(" Type ")" castFollower
bool GuessTypeCast () {
	// assert: la.kind == _lpar
	string id;
	scanner.ResetPeek();
	Token pt = scanner.Peek();
	if (typeKW[pt.kind]) {
		pt = scanner.Peek();
	} else if (pt.kind==_void) {
		pt = scanner.Peek();
		if (pt.kind != _times) {
			return false;
		}
		pt = scanner.Peek();
	} else if(IsQualident(ref pt, out id)) {
		// nothing to do
	} else {
		return false;
	}
	if (IsPointerOrDims(ref pt) && pt.kind==_rpar)
	{
		pt = scanner.Peek(); // check successor
		return castFollower[pt.kind];
	} else {
		return false;
	}
}

/* Checks whether the next sequence of tokens is a qualident *
 * and returns the qualident string                          *
 * !!! Proceeds from current peek position !!!               */
bool IsQualident (ref Token pt, out string qualident) {
	qualident = "";
	if (pt.kind == _ident) {
		qualident = pt.val;
		pt = scanner.Peek();
		while (pt.kind == _dot) {
			pt = scanner.Peek();
			if (pt.kind != _ident) return false;
			qualident += "." + pt.val;
			pt = scanner.Peek();
		}
		return true;
	} else return false;
}

// Return the n-th token after the current lookahead token
Token Peek (int n) {
	scanner.ResetPeek();
	Token x = la;
	while (n > 0) { x = scanner.Peek(); n--; }
	return x;
}

/*-----------------------------------------------------------------*
 * Resolver routines to resolve LL(1) conflicts:                   *                                                  *
 * These routines return a boolean value that indicates            *
 * whether the alternative at hand shall be choosen or not.        *
 * They are used in IF ( ... ) expressions.                        *       
 *-----------------------------------------------------------------*/

// ident "="
bool IsAssignment () {
	return la.kind == _ident && Peek(1).kind == _assgn;
}

// ident ("," | "=" | ";")
bool IsFieldDecl () {
	int peek = Peek(1).kind;
	int peek2 = Peek(2).kind;
	return la.kind == _var && peek == _ident && 
	       (peek2 == _comma || peek2 == _assgn || peek2 == _scolon);
}

/* True, if the comma is not a trailing one, *
 * like the last one in: a, b, c,            */
bool NotFinalComma () {
	int peek = Peek(1).kind;
	return la.kind == _comma && peek != _rbrace && peek != _rbrack;
}

// "void" "*"
bool NotVoidPointer () {
	return la.kind == _void && Peek(1).kind != _times;
}

// "." ident
bool DotAndIdent () {
	return la.kind == _dot && Peek(1).kind == _ident;
}

// ident ":"
bool IsLabel () {
	return la.kind == _ident && Peek(1).kind == _colon;
}

// ident "("
bool IdentAndLPar () {
	return la.kind == _ident && Peek(1).kind == _lpar;
}


// "[" ("," | "]")
bool IsDims () {
	int peek = Peek(1).kind;
	return la.kind == _lbrack && (peek == _comma || peek == _rbrack);
}

// "*" | "[" ("," | "]")
bool IsPointerOrDims () {
	return la.kind == _times || IsDims();
}

// skip: { "[" { "," } "]" | "*" }
// !!! Proceeds from current peek position !!!
bool IsPointerOrDims (ref Token pt) {
	for (;;) {
		if (pt.kind == _lbrack) {
			do pt = scanner.Peek();
			while (pt.kind == _comma);
			if (pt.kind != _rbrack) return false;
		} else if (pt.kind != _times) break;
		pt = scanner.Peek();
	}
	return true;
}

// Type ident (Type can be void*)
bool IsLocalVarDecl () {
	string ignore;
	Token pt = la;
	scanner.ResetPeek();
	
	if (typeKW[la.kind] || la.kind == _void) {
	  pt = scanner.Peek();
	  if (la.kind == _void && pt.kind != _times) { return false; }
	} else if (la.kind == _ident && !IsQualident(ref pt, out ignore)) {
		return false;
	}
	
	return IsPointerOrDims(ref pt) && pt.kind == _ident;
}



/* True, if lookahead is a local attribute target specifier, *
 * i.e. one of "event", "return", "field", "method",         *
 *             "module", "param", "property", or "type"      */
bool IsLocalAttrTarget () {
	int cur = la.kind;
	string val = la.val;
	return cur == _return ||
	       (Peek(1).kind == _colon &&
	         (val == "field" || val == "method"  ||
	          val == "param" || val == "type"));
}

/*------------------------------------------------------------------------*
 *----- SCANNER DESCRIPTION ----------------------------------------------*
 *------------------------------------------------------------------------*/



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

	
	void JavaScript() {
		while (la.kind == 20 || la.kind == 36) {
			ScriptMember();
		}
	}

	void ScriptMember() {
		Modifiers m = new Modifiers(this);
		string id; 
		if (IsFieldDecl()) {
			Expect(36);
			Field(m);
			while (la.kind == 42) {
				Get();
				Field(m);
			}
			Expect(65);
		} else if (la.kind == 20) {
			Get();
			Qualident(out id);
			m.Check(Modifier.propEvntMeths); 
			Expect(53);
			if (StartOf(1)) {
				FormalParams();
			}
			Expect(64);
			if (la.kind == 51) {
				Block();
			} else if (la.kind == 65) {
				Get();
			} else SynErr(83);
		} else SynErr(84);
	}

	void Field(Modifiers m) {
		Expect(1);
		if (la.kind == 40) {
			Get();
			m.Check(Modifier.fields); 
			Init();
			BindingObject.Ident = t.val;
		}
	}

	void Qualident(out string qualident) {
		Expect(1);
		qualident = t.val; 
		while (DotAndIdent()) {
			Expect(45);
			Expect(1);
			qualident += "." + t.val; 
		}
	}

	void FormalParams() {
		Par();
		if (la.kind == 42) {
			Get();
			FormalParams();
		}
	}

	void Block() {
		Expect(51);
		while (StartOf(2)) {
			Statement();
		}
		Expect(62);
	}

	void Init() {
		if (StartOf(3)) {
			Expr();
		} else if (la.kind == 51) {
			ArrayInit();
		} else SynErr(85);
	}

	void LocalVarDecl() {
		TypeKind dummy; 
		Type(out dummy);
		LocalVar();
		while (la.kind == 42) {
			Get();
			LocalVar();
		}
	}

	void Type(out TypeKind type) {
		type = TypeKind.simple; 
		if (StartOf(4)) {
			SimpleType();
		} else if (StartOf(5)) {
			ClassType();
			type = TypeKind.pointer; 
		} else SynErr(86);
		while (IsPointerOrDims()) {
			if (la.kind == 67) {
				Get();
				type = TypeKind.pointer; 
			} else if (la.kind == 52) {
				Get();
				while (la.kind == 42) {
					Get();
				}
				Expect(63);
				type = TypeKind.array; 
			} else SynErr(87);
		}
	}

	void LocalVar() {
		TypeKind dummy; 
		Expect(1);
		if (la.kind == 40) {
			Get();
			Init();
		}
	}

	void Expr() {
		Unary();
		if (StartOf(6)) {
			OrExpr();
			if (la.kind == 79) {
				Get();
				Expr();
				Expect(41);
				Expr();
			}
		} else if (StartOf(7)) {
			AssignOp();
			Expr();
		} else SynErr(88);
	}

	void ArrayInit() {
		Expect(51);
		if (StartOf(8)) {
			Init();
			while (NotFinalComma()) {
				Expect(42);
				Init();
			}
			if (la.kind == 42) {
				Get();
			}
		}
		Expect(62);
	}

	void Par() {
		TypeKind dummy; 
		Type(out dummy);
		Expect(1);
	}

	void Argument() {
		Expr();
	}

	void SimpleType() {
		if (la.kind == 11 || la.kind == 23 || la.kind == 24) {
			IntType();
		} else if (la.kind == 18) {
			Get();
		} else if (la.kind == 14) {
			Get();
		} else if (la.kind == 69) {
			Get();
		} else SynErr(89);
	}

	void ClassType() {
		string id; 
		if (la.kind == 1) {
			Qualident(out id);
		} else if (la.kind == 27) {
			Get();
		} else if (la.kind == 29) {
			Get();
		} else if (la.kind == 36) {
			Get();
		} else SynErr(90);
	}

	void IntType() {
		if (la.kind == 23) {
			Get();
		} else if (la.kind == 24) {
			Get();
		} else if (la.kind == 11) {
			Get();
		} else SynErr(91);
	}

	void Statement() {
		TypeKind dummy; 
		if (IsLabel()) {
			Expect(1);
			Expect(41);
			Statement();
		} else if (la.kind == 70) {
			Get();
			Type(out dummy);
			Expect(1);
			Expect(40);
			Expr();
			while (la.kind == 42) {
				Get();
				Expect(1);
				Expect(40);
				Expr();
			}
			Expect(65);
		} else if (IsLocalVarDecl()) {
			LocalVarDecl();
			Expect(65);
		} else if (StartOf(9)) {
			EmbeddedStatement();
		} else SynErr(92);
	}

	void EmbeddedStatement() {
		TypeKind type; 
		if (la.kind == 51) {
			Block();
		} else if (la.kind == 65) {
			Get();
		} else if (la.kind == 51) {
			Block();
		} else if (StartOf(3)) {
			StatementExpr();
			Expect(65);
		} else if (la.kind == 22) {
			Get();
			Expect(53);
			Expr();
			Expect(64);
			EmbeddedStatement();
			if (la.kind == 15) {
				Get();
				EmbeddedStatement();
			}
		} else if (la.kind == 30) {
			Get();
			Expect(53);
			Expr();
			Expect(64);
			Expect(51);
			while (la.kind == 8 || la.kind == 12) {
				SwitchSection();
			}
			Expect(62);
		} else if (la.kind == 38) {
			Get();
			Expect(53);
			Expr();
			Expect(64);
			EmbeddedStatement();
		} else if (la.kind == 13) {
			Get();
			EmbeddedStatement();
			Expect(38);
			Expect(53);
			Expr();
			Expect(64);
			Expect(65);
		} else if (la.kind == 19) {
			Get();
			Expect(53);
			if (StartOf(10)) {
				ForInit();
			}
			Expect(65);
			if (StartOf(3)) {
				Expr();
			}
			Expect(65);
			if (StartOf(3)) {
				ForInc();
			}
			Expect(64);
			EmbeddedStatement();
		} else if (la.kind == 6) {
			Get();
			Expect(65);
		} else if (la.kind == 10) {
			Get();
			Expect(65);
		} else if (la.kind == 28) {
			Get();
			if (StartOf(3)) {
				Expr();
			}
			Expect(65);
		} else if (la.kind == 32) {
			Get();
			if (StartOf(3)) {
				Expr();
			}
			Expect(65);
		} else if (la.kind == 21) {
			GotoStatement();
		} else if (la.kind == 34) {
			TryStatement();
			Expect(53);
			Type(out type);
			if (type != TypeKind.pointer) Error("can only fix pointer types"); 
			Expect(1);
			Expect(40);
			Expr();
			while (la.kind == 42) {
				Get();
				Expect(1);
				Expect(40);
				Expr();
			}
			Expect(64);
			EmbeddedStatement();
		} else SynErr(93);
	}

	void StatementExpr() {
		bool isAssignment = assnStartOp[la.kind] || IsTypeCast(); 
		Unary();
		if (StartOf(7)) {
			AssignOp();
			Expr();
		} else if (la.kind == 42 || la.kind == 64 || la.kind == 65) {
			if (isAssignment) Error("error in assignment."); 
		} else SynErr(94);
	}

	void SwitchSection() {
		SwitchLabel();
		while (la.kind == 8 || la.kind == 12) {
			SwitchLabel();
		}
		Statement();
		while (StartOf(2)) {
			Statement();
		}
	}

	void ForInit() {
		if (IsLocalVarDecl()) {
			LocalVarDecl();
		} else if (StartOf(3)) {
			StatementExpr();
			while (la.kind == 42) {
				Get();
				StatementExpr();
			}
		} else SynErr(95);
	}

	void ForInc() {
		StatementExpr();
		while (la.kind == 42) {
			Get();
			StatementExpr();
		}
	}

	void GotoStatement() {
		Expect(21);
		if (la.kind == 1) {
			Get();
			Expect(65);
		} else if (la.kind == 8) {
			Get();
			Expr();
			Expect(65);
		} else if (la.kind == 12) {
			Get();
			Expect(65);
		} else SynErr(96);
	}

	void TryStatement() {
		Expect(34);
		Block();
		if (la.kind == 9) {
			CatchClauses();
			if (la.kind == 17) {
				Get();
				Block();
			}
		} else if (la.kind == 17) {
			Get();
			Block();
		} else SynErr(97);
	}

	void Unary() {
		TypeKind dummy; 
		while (unaryHead[la.kind] || IsTypeCast()) {
			switch (la.kind) {
			case 61: {
				Get();
				break;
			}
			case 56: {
				Get();
				break;
			}
			case 59: {
				Get();
				break;
			}
			case 66: {
				Get();
				break;
			}
			case 67: {
				Get();
				break;
			}
			case 50: {
				Get();
				break;
			}
			case 43: {
				Get();
				break;
			}
			case 39: {
				Get();
				break;
			}
			case 53: {
				Get();
				Type(out dummy);
				Expect(64);
				break;
			}
			default: SynErr(98); break;
			}
		}
		Primary();
	}

	void AssignOp() {
		switch (la.kind) {
		case 40: {
			Get();
			break;
		}
		case 71: {
			Get();
			break;
		}
		case 72: {
			Get();
			break;
		}
		case 73: {
			Get();
			break;
		}
		case 74: {
			Get();
			break;
		}
		case 75: {
			Get();
			break;
		}
		case 76: {
			Get();
			break;
		}
		case 77: {
			Get();
			break;
		}
		case 78: {
			Get();
			break;
		}
		default: SynErr(99); break;
		}
	}

	void SwitchLabel() {
		if (la.kind == 8) {
			Get();
			Expr();
			Expect(41);
		} else if (la.kind == 12) {
			Get();
			Expect(41);
		} else SynErr(100);
	}

	void CatchClauses() {
		Expect(9);
		if (la.kind == 51) {
			Block();
		} else if (la.kind == 53) {
			Get();
			if (la.kind == 1) {
				Get();
			}
			Expect(64);
			Block();
			if (la.kind == 9) {
				CatchClauses();
			}
		} else SynErr(101);
	}

	void OrExpr() {
		AndExpr();
		while (la.kind == 80) {
			Get();
			Unary();
			AndExpr();
		}
	}

	void AndExpr() {
		BitOrExpr();
		while (la.kind == 81) {
			Get();
			Unary();
			BitOrExpr();
		}
	}

	void BitOrExpr() {
		BitXorExpr();
		while (la.kind == 60) {
			Get();
			Unary();
			BitXorExpr();
		}
	}

	void BitXorExpr() {
		BitAndExpr();
		while (la.kind == 68) {
			Get();
			Unary();
			BitAndExpr();
		}
	}

	void BitAndExpr() {
		EqlExpr();
		while (la.kind == 39) {
			Get();
			Unary();
			EqlExpr();
		}
	}

	void EqlExpr() {
		RelExpr();
		while (la.kind == 46 || la.kind == 47 || la.kind == 58) {
			if (la.kind == 58) {
				Get();
			} else if (la.kind == 46) {
				Get();
			} else {
				Get();
			}
			Unary();
			RelExpr();
		}
	}

	void RelExpr() {
		TypeKind dummy; 
		AddExpr();
		while (StartOf(11)) {
			if (la.kind == 54) {
				Get();
			} else if (la.kind == 48) {
				Get();
			} else if (la.kind == 55) {
				Get();
			} else {
				Get();
			}
			Unary();
			AddExpr();
		}
	}

	void AddExpr() {
		MulExpr();
		while (la.kind == 56 || la.kind == 61) {
			if (la.kind == 61) {
				Get();
			} else {
				Get();
			}
			Unary();
			MulExpr();
		}
	}

	void MulExpr() {
		while (la.kind == 44 || la.kind == 57 || la.kind == 67) {
			if (la.kind == 67) {
				Get();
			} else if (la.kind == 44) {
				Get();
			} else {
				Get();
			}
			Unary();
		}
	}

	void Primary() {
		TypeKind type; bool isArrayCreation = false; 
		switch (la.kind) {
		case 1: {
			Get();
			break;
		}
		case 2: case 3: case 4: case 5: case 16: case 26: case 33: {
			Literal();
			break;
		}
		case 53: {
			Get();
			Expr();
			Expect(64);
			break;
		}
		case 31: {
			Get();
			break;
		}
		case 25: {
			Get();
			Type(out type);
			if (la.kind == 53) {
				Get();
				if (StartOf(3)) {
					Argument();
					while (la.kind == 42) {
						Get();
						Argument();
					}
				}
				Expect(64);
			} else if (la.kind == 52) {
				isArrayCreation = true; 
				Get();
				Expr();
				while (la.kind == 42) {
					Get();
					Expr();
				}
				Expect(63);
				while (IsDims()) {
					Expect(52);
					while (la.kind == 42) {
						Get();
					}
					Expect(63);
				}
				if (la.kind == 51) {
					ArrayInit();
				}
			} else if (la.kind == 51) {
				if (type != TypeKind.array) Error("array type expected");
				isArrayCreation = true; 
				ArrayInit();
			} else SynErr(102);
			break;
		}
		case 35: {
			Get();
			Expect(53);
			Type(out type);
			Expect(64);
			break;
		}
		default: SynErr(103); break;
		}
		while (StartOf(12)) {
			if (la.kind == 50) {
				Get();
			} else if (la.kind == 43) {
				Get();
			} else if (la.kind == 45) {
				Get();
				Expect(1);
			} else if (la.kind == 53) {
				Get();
				if (StartOf(3)) {
					Argument();
					while (la.kind == 42) {
						Get();
						Argument();
					}
				}
				Expect(64);
			} else {
				if (isArrayCreation) Error("element access not allow on array creation"); 
				Get();
				Expr();
				while (la.kind == 42) {
					Get();
					Expr();
				}
				Expect(63);
			}
		}
	}

	void Literal() {
		switch (la.kind) {
		case 2: {
			Get();
			break;
		}
		case 3: {
			Get();
			break;
		}
		case 4: {
			Get();
			break;
		}
		case 5: {
			Get();
			break;
		}
		case 33: {
			Get();
			break;
		}
		case 16: {
			Get();
			break;
		}
		case 26: {
			Get();
			break;
		}
		default: SynErr(104); break;
		}
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

    public override void Parse(string fileName)
    {
        ExpressionStatementSyntax = Syntax.ExpressionStatement(
    Syntax.InvocationExpression(
        Syntax.MemberAccessExpression(
            SyntaxKind.MemberAccessExpression,
            Syntax.IdentifierName("Console"),
            name: Syntax.IdentifierName("WriteLine")),
        Syntax.ArgumentList(
            arguments: Syntax.SeparatedList<ArgumentSyntax>(
                Syntax.Argument(
                    expression: Syntax.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Syntax.Literal(
                            text: @"""Goodbye everyone!""",
                            value: "Goodbye everyone!")))))));
        scanner.Scan(fileName);
		la = new Token();
		la.val = "";		
		Get();
		JavaScript();
		Expect(0);
	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _x,_x,_T,_x, _x,_x,_x,_T, _T,_x,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_T,_T, _x,_T,_T,_x, _T,_x,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_T,_T, _x,_T,_x,_x, _T,_x,_x,_T, _x,_T,_x,_x, _x,_T,_T,_T, _x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_x, _x,_x,_x,_T, _x,_T,_x,_T, _x,_x,_x,_T, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_T,_x, _x,_T,_x,_x, _T,_x,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _x,_x,_T,_x, _x,_x,_x,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_T,_x, _T,_x,_T,_T, _T,_T,_x,_x, _x,_x,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_x,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_x, _x,_x,_x,_T, _x,_T,_x,_T, _x,_x,_x,_T, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_T,_T, _x,_T,_x,_x, _T,_x,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_T,_x, _x,_T,_x,_x, _T,_x,_x,_T, _x,_T,_T,_x, _x,_T,_T,_x, _T,_x,_T,_T, _T,_T,_T,_T, _x,_x,_T,_T, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_T,_T, _x,_T,_x,_x, _T,_x,_x,_T, _x,_T,_x,_x, _x,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _T,_x,_T,_x, _x,_x,_x,_T, _T,_T,_T,_T, _x,_T,_x,_T, _x,_T,_x,_T, _T,_x,_x,_T, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_T,_x, _x,_T,_x,_x, _T,_x,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_x,_x, _x,_x,_T,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}

	};
} // end aParser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "intCon expected"; break;
			case 3: s = "realCon expected"; break;
			case 4: s = "charCon expected"; break;
			case 5: s = "stringCon expected"; break;
			case 6: s = "break expected"; break;
			case 7: s = "bool expected"; break;
			case 8: s = "case expected"; break;
			case 9: s = "catch expected"; break;
			case 10: s = "continue expected"; break;
			case 11: s = "char expected"; break;
			case 12: s = "default expected"; break;
			case 13: s = "do expected"; break;
			case 14: s = "double expected"; break;
			case 15: s = "else expected"; break;
			case 16: s = "false expected"; break;
			case 17: s = "finally expected"; break;
			case 18: s = "float expected"; break;
			case 19: s = "for expected"; break;
			case 20: s = "function expected"; break;
			case 21: s = "goto expected"; break;
			case 22: s = "if expected"; break;
			case 23: s = "int expected"; break;
			case 24: s = "long expected"; break;
			case 25: s = "new expected"; break;
			case 26: s = "null expected"; break;
			case 27: s = "object expected"; break;
			case 28: s = "return expected"; break;
			case 29: s = "string expected"; break;
			case 30: s = "switch expected"; break;
			case 31: s = "this expected"; break;
			case 32: s = "throw expected"; break;
			case 33: s = "true expected"; break;
			case 34: s = "try expected"; break;
			case 35: s = "typeof expected"; break;
			case 36: s = "var expected"; break;
			case 37: s = "void expected"; break;
			case 38: s = "while expected"; break;
			case 39: s = "and expected"; break;
			case 40: s = "assgn expected"; break;
			case 41: s = "colon expected"; break;
			case 42: s = "comma expected"; break;
			case 43: s = "dec expected"; break;
			case 44: s = "div expected"; break;
			case 45: s = "dot expected"; break;
			case 46: s = "eq expected"; break;
			case 47: s = "eqtyp expected"; break;
			case 48: s = "gt expected"; break;
			case 49: s = "gte expected"; break;
			case 50: s = "inc expected"; break;
			case 51: s = "lbrace expected"; break;
			case 52: s = "lbrack expected"; break;
			case 53: s = "lpar expected"; break;
			case 54: s = "lt expected"; break;
			case 55: s = "lte expected"; break;
			case 56: s = "minus expected"; break;
			case 57: s = "mod expected"; break;
			case 58: s = "neq expected"; break;
			case 59: s = "not expected"; break;
			case 60: s = "or expected"; break;
			case 61: s = "plus expected"; break;
			case 62: s = "rbrace expected"; break;
			case 63: s = "rbrack expected"; break;
			case 64: s = "rpar expected"; break;
			case 65: s = "scolon expected"; break;
			case 66: s = "tilde expected"; break;
			case 67: s = "times expected"; break;
			case 68: s = "xor expected"; break;
			case 69: s = "\"boolean\" expected"; break;
			case 70: s = "\"const\" expected"; break;
			case 71: s = "\"+=\" expected"; break;
			case 72: s = "\"-=\" expected"; break;
			case 73: s = "\"*=\" expected"; break;
			case 74: s = "\"/=\" expected"; break;
			case 75: s = "\"%=\" expected"; break;
			case 76: s = "\"&=\" expected"; break;
			case 77: s = "\"|=\" expected"; break;
			case 78: s = "\"^=\" expected"; break;
			case 79: s = "\"?\" expected"; break;
			case 80: s = "\"||\" expected"; break;
			case 81: s = "\"&&\" expected"; break;
			case 82: s = "??? expected"; break;
			case 83: s = "invalid ScriptMember"; break;
			case 84: s = "invalid ScriptMember"; break;
			case 85: s = "invalid Init"; break;
			case 86: s = "invalid Type"; break;
			case 87: s = "invalid Type"; break;
			case 88: s = "invalid Expr"; break;
			case 89: s = "invalid SimpleType"; break;
			case 90: s = "invalid ClassType"; break;
			case 91: s = "invalid IntType"; break;
			case 92: s = "invalid Statement"; break;
			case 93: s = "invalid EmbeddedStatement"; break;
			case 94: s = "invalid StatementExpr"; break;
			case 95: s = "invalid ForInit"; break;
			case 96: s = "invalid GotoStatement"; break;
			case 97: s = "invalid TryStatement"; break;
			case 98: s = "invalid Unary"; break;
			case 99: s = "invalid AssignOp"; break;
			case 100: s = "invalid SwitchLabel"; break;
			case 101: s = "invalid CatchClauses"; break;
			case 102: s = "invalid Primary"; break;
			case 103: s = "invalid Primary"; break;
			case 104: s = "invalid Literal"; break;

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