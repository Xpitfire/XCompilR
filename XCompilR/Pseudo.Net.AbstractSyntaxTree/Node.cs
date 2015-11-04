/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public delegate void NodeRemovedHandler(Node node);

  public abstract class Node : IDisposable {
    static public ReadPositionReader PositionReader = null;    
    
    public delegate void Visitor(Node parent, Node child);
    
    public delegate void FaultHandler(ErrorCode code, 
                                      Node node, 
                                      string msg, 
                                      bool error = true);
                                      
    public delegate void ReadPositionReader(out int Line, out int Column);
    
    
    public virtual bool Validate(FaultHandler handleFault) { return true; }
    
    public abstract void Visit(Visitor visitor);
    public event NodeRemovedHandler Removed;
    
    public int Line { get; private set; }
    public int Col { get; private set; }       

    public Node() {
      if(PositionReader != null) {
        int l, c;
        PositionReader(out l, out c);
        this.Line = l;
        this.Col = c;
      } else {
        this.Line = 0;
        this.Col = 0;
      }

      ReferencedFromList = new HashSet<Node>();
    }

//-----------------------------------------------------------------------------
// create reverse reference list
//-----------------------------------------------------------------------------    
    private ISet<Node> ReferencedFromList;
    internal void AddReferencer(Node r) {
      ReferencedFromList.Add(r);
      r.Removed += (node) => {
        ReferencedFromList.Remove(node);
      };
    }

    public Node[] GetReferenceFromList() {
      return ReferencedFromList.ToArray();
    }

    private void AddReferencedFrom(ISet<Node> processed) {
      foreach(Node n in GetReferenceFromList()) {
        if(!processed.Contains(n)) {
        // zyklus zwischen den Umgebungen verhindern, 
        // da alles vom root.Environment referenziert wird
          if(!(this is Environment && n is Environment)) 
          {
            processed.Add(n);
            n.AddReferencedFrom(processed);
          }
        }
      }
    }

    public Node[] GetCompleteReferencedFromList() {
      ISet<Node> processed = new HashSet<Node>();
      AddReferencedFrom(processed);
      return processed.ToArray();
    }
//-----------------------------------------------------------------------------

    public void Dispose() {
      if(Removed != null)
        Removed(this);
    }

    public Node[] GetFlatGraph() {

      ISet<Node> nodes = new HashSet<Node>();
      Visitor collector = null;

      collector = (p, c) => {
        if(!nodes.Contains(c)) {
          nodes.Add(c);
          c.Visit(collector);
        }
      };

      nodes.Add(this);
      this.Visit(collector);

      return nodes.ToArray();
    }

  }
}
