/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;

namespace Pseudo.Net.Exceptions {
  public enum ErrorCode {
    SYNTAX_ERROR,
    FATAL_ERROR,
    NOT_DEFINED,
    ALREADY_DEFINED,
    INVALID_OPERATOR,
    DEREFERENCE_NON_POINTER, // a, b:int  b := a->
    CANT_SELECT_MEMBER_OF_POINTER,
    TYPE_HAS_NO_MEMBERS,
    MEMBER_DOESNT_EXISIT,
    TYPE_IS_NO_ARRAY,
    INVOKE_INVALID_PARAMETER_COUNT,
    INVOKE_INVALID_PARAMETER_TYPE,
    INVOKE_INVALID_PARAMETER_DIR,
    INVOKE_INVALID_PARAMETER,
    INVOKE_SHOULD_RETURN_NO_VALUE,
    INVOKE_SHOULD_RETURN_VALUE,
    INVOKE_RETURN_WRONG_TYPE,
    THIS_KEYWORD_NOT_ALLOWED,
    BREAK_KEYWORD_NOT_ALLOWED,
    CASE_LABEL_NOT_CONST,
    CASE_LABEL_NOT_UNIQUE,
    ARRAY_INDEXER_NOT_INT,
    ARRAY_INDEXER_OUT_OF_RANGE,
    NOT_CONSTRUCTOR_DEFINED,
    IMPLICIT_CAST_NOT_SUPPORTED,
    RANGE_NOT_SUPPORTED_FOR_TYPE,
    VALUE_NOT_CONST,
    TYPES_NOT_EQUAL,
    NOT_A_CLASS,
    ASSIGN_VALUE_TO_CONST,
    VAR_NEVER_REFERENCED,
    UNDEFINDED_TYPE,
    UNDEFINED_METHOD,
    TYPES_NOT_COMPAREABLE,
    INVALID_TYPE,
    RANGE_NOT_CONST,
    RANGE_BAD_LIMIT,
    METHOD_NAME_NOT_EQUAL,
    METHOD_SIGNATURE_NOT_EQUAL,
    MEMBER_IS_PRIVATE,

    OVERRIDE_IN_BASECLASS,
    NOTHING_TO_OVERRIDE,
    OVERRIDE_DIFFERENT_SIGNATURE,

    AMBIGUOUS_STATEMENT,
    CALL_ABSTRACT_BASE_MEMBER,
    OVERRIDE_PRIVATE,
    OVERRIDE_KEYWORD_NEEDED,
    NO_IMPL_FOR_ABSTRACT_METHOD,
    NEW_ABSTRACT,
    ABSTRACT_IN_NON_ABSTACT_CLASS,

    // CODE GEN
    POINTER_TO_BASETYPE_NOT_SUPPORTED,
    BASE_CONTRUCTOR_MUST_BE_CALLED,
    NO_BODY_DEFINED,
    CODE_GEN_WARNING,
    CODE_GEN_ERROR,
    NOT_SUPPORTED,
    CSHARP_COMPILER_ERROR,
    NO_VALUE_DEFINED,
    INDEXER_NOT_VALID_ON_TYPE,
    DIVBYZERO,
  };

//-----------------------------------------------------------------------------
//
//-----------------------------------------------------------------------------
  public class SemanticErrorException : Exception {
    public Node SourceNode { get; private set; }
    public ErrorCode Code { get; private set; }
    private static string FormatMsg(Node sourcenode, string msg) {
      return msg;
    }

    public SemanticErrorException(ErrorCode code, 
                                  Node sourcenode, 
                                  string msg)
      : this(code, FormatMsg(sourcenode, msg)) {
      this.SourceNode = sourcenode;
    }
    public SemanticErrorException(ErrorCode code, string msg) 
      : base(msg) { Code = code; }
  }

//-----------------------------------------------------------------------------
//
//-----------------------------------------------------------------------------
  public class NotDefinedException : SemanticErrorException {
    public NotDefinedException(Node sourcenode, string name) 
      : base(ErrorCode.NOT_DEFINED, sourcenode, 
        String.Format("The identifier '{0}' does not exist in the current context", name)) { }
        
    public NotDefinedException(string name)
      : base(ErrorCode.NOT_DEFINED, 
        String.Format("The identifier '{0}' does not exist in the current context", name)) { }
  }

//-----------------------------------------------------------------------------
//
//-----------------------------------------------------------------------------  
  public class AlreadyDefinedException : SemanticErrorException {
    public AlreadyDefinedException(Node sourcenode, string env, string name)
      : base(ErrorCode.ALREADY_DEFINED, sourcenode, 
        String.Format("{0} already contains a definition for '{1}'", env, name)) { }
      
    public AlreadyDefinedException(string env, string name)
      : base(ErrorCode.ALREADY_DEFINED, 
        String.Format("{0} already contains a definition for '{1}'", env, name)) { }
      
    public AlreadyDefinedException(Node sourcenode, string msg)
      : base(ErrorCode.ALREADY_DEFINED, sourcenode, msg) { }
  }

//-----------------------------------------------------------------------------
//
//-----------------------------------------------------------------------------
  public class InvalidOperatorException : SemanticErrorException {
    public InvalidOperatorException(Node sourcenode, 
                                    string operatorName, 
                                    string operantTyp)
      : base(ErrorCode.INVALID_OPERATOR, sourcenode, 
        String.Format("the operator '{0}' cannot be applied at the type '{1}'", 
          operatorName, operantTyp)) { }
      
    public InvalidOperatorException(Node sourcenode, 
                                    string operatorName, 
                                    string operantTyp1, 
                                    string operantTyp2)
      : base(ErrorCode.INVALID_OPERATOR, sourcenode, 
        String.Format("the operator '{0}' cannot be applied at '{1}' and '{2}'", 
          operatorName, operantTyp1, operantTyp2)) { }
  }

//-----------------------------------------------------------------------------
//
//-----------------------------------------------------------------------------
  public static class FaultMessageBuilder {
    private static string FormatMsg(Node sourcenode, string msg) {
      if(sourcenode != null)
        return String.Format("line {0} col {1}: {2}", sourcenode.Line, sourcenode.Col, msg);
      return msg;
    }

    public static string BuildNoMember(string membername, TypeNode sourcetype) {
      return FormatMsg(null, 
        String.Format("'{1}' has no member '{0}'", membername, sourcetype.GetName()));
    }

    public static string BuildPrivateMember(string membername, TypeNode sourcetype) {
      return FormatMsg(null, 
        String.Format("'{0}' is private in class '{1}'", membername, sourcetype.GetName()));
    }

    public static string BuildUndefinedType(string typename) {
      return FormatMsg(null, 
        String.Format("undefined type '{0}'", typename));
    }
  }
}
