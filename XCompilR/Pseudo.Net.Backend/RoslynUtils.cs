/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.CSharp;

namespace Pseudo.Net.Backend.Roslyn {
  internal static class RoslynUtils {
    public static LiteralExpressionSyntax GetLiteralExpression(string s) {
      return Syntax.LiteralExpression(
        SyntaxKind.StringLiteralExpression, Syntax.Literal(s));
    }

    public static LiteralExpressionSyntax GetLiteralExpression(char s) {
      return Syntax.LiteralExpression(
        SyntaxKind.CharacterLiteralExpression, Syntax.Literal(s));
    }

    public static LiteralExpressionSyntax GetLiteralExpression(int s) {
      return Syntax.LiteralExpression(
        SyntaxKind.NumericLiteralExpression, Syntax.Literal(s));
    }

    public static LiteralExpressionSyntax GetLiteralExpression(double s) {
      return Syntax.LiteralExpression(
        SyntaxKind.NumericLiteralExpression, Syntax.Literal(s));
    }

    public static LiteralExpressionSyntax GetLiteralExpression(bool s) {
      if(s)
        return Syntax.LiteralExpression(SyntaxKind.TrueLiteralExpression);

      return Syntax.LiteralExpression(SyntaxKind.FalseLiteralExpression);
    }

  }
}
