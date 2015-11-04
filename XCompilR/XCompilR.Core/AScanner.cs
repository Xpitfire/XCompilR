using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompilR.Core
{
    public abstract class AScanner
    {
        public abstract AScanner Scan(string fileName);
        public abstract AScanner Scan(Stream stream);
    }
}
