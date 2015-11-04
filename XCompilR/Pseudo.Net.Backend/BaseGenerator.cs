/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Common;

namespace Pseudo.Net.Backend {
  public enum Target { Default, CS, VB, CPP, EXE, FS, DGML, GV };

  public abstract class BaseGenerator {
    protected ProgramRootNode root;
    protected ReportErrorHandler errorReporter;

    public BaseGenerator(ProgramRootNode root, ReportErrorHandler errorHandler) {
      this.root = root;
      this.errorReporter = errorHandler;
    }

    public readonly static Dictionary<Target, string> 
      TargetExtensions = new Dictionary<Target, string>() { 
      {Target.CS,   "cs"},
      {Target.VB,   "vb"},
      {Target.EXE,  "exe"},
      {Target.CPP,  "cpp"},
      {Target.FS,   "fs"},
      {Target.DGML, "dgml"},
      {Target.GV,   "gv"}
    };

    protected string path;

    public virtual void Generate(string path, Target target) {
      this.path = path;
      using(Stream sw = File.Open(path, FileMode.Create)) {
        Generate(sw, target);
        sw.Close();
      }
    }

    public abstract void Generate(Stream stream, Target target);

    public virtual bool SupportsTarget(Target t) {
      return SupportedTargets().Contains(t);
    }

    public abstract Target[] SupportedTargets();
    public virtual Target DefaultTarget() {
      return SupportedTargets()[0];
    }
  }
}
