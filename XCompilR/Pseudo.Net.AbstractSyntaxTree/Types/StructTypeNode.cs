/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class StructTypeNode : TypeNode {
    public Environment Environment;
    public string FullName { get { return Environment.FullName; } }
    public string Name { get { return Environment.Name; } }

    public StructTypeNode(Environment env, string name)
      : this(env, name, BaseType.COMPOUND) {
    }

    protected StructTypeNode(Environment env, string name, BaseType type)
      : base(type) {
      this.Environment = env;
      this.Environment.Name = name;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Environment);
    }

    public override TypeNode GetMemberType(string name) {
      return Environment.Find(name, false).GetTypeNode();
    }

    public override ExpressionNode GetMember(string name) {
      return Environment.Find(name, false);
    }

    public override bool HasMember(string name) {
      try {
        Environment.Find(name, false);
        return true;
      } catch(NotDefinedException) {
        return false;
      }
    }

    public override string ToString() {
      if(Name != null)
        return Name;
      else
        return "Anonym Compound";
    }

    public override bool HasName() {
      if(base.HasName())
        return true;

      return Name != null;
    }

    public override string GetName(bool createNameIfNotInRepo = false) {
      if(base.HasName())
        return base.GetName(createNameIfNotInRepo);

      return "µ" + Name; // Name mangeling with 'µ'
    }
  }

}
