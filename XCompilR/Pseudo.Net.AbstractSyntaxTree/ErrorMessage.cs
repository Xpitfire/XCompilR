/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.Common {
  public delegate void ReportErrorHandler(ErrorMessage msg);

  public class ErrorMessage {
    public bool IsWarning = false;
    public int Line = 0;
    public int Column = 0;
    public string Message = "";
    public ErrorCode Code;

    public ErrorMessage(ErrorCode code, 
                        int line, 
                        int col, 
                        string message, 
                        bool isWarning = false) {
      this.Code = code;
      this.Line = line;
      this.Column = col;
      this.Message = message;
      this.IsWarning = isWarning;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();

      sb.AppendFormat("({0},{1}) ", Line, Column);

      if(IsWarning)
        sb.Append("warning: ");
      else
        sb.Append("error: ");

      sb.Append(Message);
      return sb.ToString();
    }
  }
}
