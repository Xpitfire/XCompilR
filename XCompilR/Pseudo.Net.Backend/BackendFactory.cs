/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Backend.Roslyn;
using Pseudo.Net.Common;

namespace Pseudo.Net.Backend {
  public enum Engine { GraphViz, CSharp, CodeDom, Roslyn };

  static public class BackendFactory {
    public static BaseGenerator CreateGenerator(Engine engine, 
                                                ProgramRootNode root, 
                                                ReportErrorHandler errorhandler) {
      BaseGenerator generator;
      switch(engine) {
        case Engine.GraphViz:
          generator = new GraphVizGenerator(root, errorhandler);
          break;
        default:
        case Engine.Roslyn:
          generator = new RoslynGenerator(root, errorhandler);
          break;
 //       case Engine.CodeDom:
 //         generator = new CodeDomGenerator(root, errorhandler);
 //         break;
 //       case Engine.CSharp:
 //         generator = new CSharpCodeGenerator(root, errorhandler);
 //         break;
      }

      return generator;
    }
  }
}
