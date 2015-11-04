/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class TypeRepository : Node, IEnumerable<TypeNode> {
    private IDictionary<string, TypeNode> repository;
    private IDictionary<string, TypeNode> unresolved;

    public TypeRepository() {
      unresolved = new Dictionary<string, TypeNode>();
      repository = new Dictionary<string, TypeNode>();
      repository.Add("int", BaseTypeNode.IntegerTypeNode);
      repository.Add("real", BaseTypeNode.RealTypeNode);
      repository.Add("bool", BaseTypeNode.BoolTypeNode);
      repository.Add("string", BaseTypeNode.StringTypeNode);
      repository.Add("char", BaseTypeNode.CharTypeNode);
      repository.Add("void", BaseTypeNode.VoidTypeNode);
    }

    public void Replace(string typename, TypeNode type) {
      repository[typename] = type;
    }

    private void CheckExists(string name) {
      if(repository.ContainsKey(name))
        throw new AlreadyDefinedException("the program", name);
    }

    public void AddType(string name, TypeNode type) {
      CheckExists(name);
      type.SetRepository(this);
      repository.Add(name, type);
    }

    public TypeNode AddAliasType(string name, string aliasto) {
      CheckExists(name);
      if(repository.ContainsKey(aliasto)) {
        repository.Add(name, repository[aliasto]);
      } else if(aliasto.StartsWith("->")) {
        string pointsTo = aliasto.Substring(2);
        TypeNode t;
        if(repository.ContainsKey(pointsTo)) {
          t = new PointerTypeNode(this, repository[pointsTo]);
        } else {

          t = new PointerTypeNode(this, new UndefinedTypeNode(this, pointsTo));
        }
        t.SetRepository(this);
        repository.Add(name, t);
      } else {
        TypeNode t = new UndefinedTypeNode(this, aliasto);
        t.SetRepository(this);
        repository.Add(name, t);
      }

      return repository[name];
    }

    public TypeNode FindOrCreate(string typename) {
      if(repository.ContainsKey(typename))
        return repository[typename];

      TypeNode t;
      if(typename.StartsWith("->")) {
        string pointsTo = typename.Substring(2);

        if(repository.ContainsKey(pointsTo)) {
          t = new PointerTypeNode(repository[pointsTo]);
        } else {
          t = new PointerTypeNode(new UndefinedTypeNode(this, pointsTo));
        }
      } else {
        t = new UndefinedTypeNode(this, typename);
      }
      
      return t;
    }

    public bool Exists(string typename) {
      return repository.ContainsKey(typename);
    }

    public TypeNode Find(string typename) {
      if(Exists(typename))
        return repository[typename];
      else
        throw new NotDefinedException(typename);
    }

    public string GetName(TypeNode type) {
      try {
        return repository.Where((v) => {
          return type.Equals(v.Value);
        }).First().Key;
      } catch(InvalidOperationException) {
        return type.ToString();
      }
    }

    public override void Visit(Node.Visitor visitor) {
      foreach(var t in repository.Values) {
        visitor(this, t);
      }
    }

    public override string ToString() {
      return "TypeRepo";
    }

    public IEnumerator<TypeNode> GetEnumerator() {
      foreach(var t in repository) {
        yield return t.Value;
      }
    }

    System.Collections.IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }

}
