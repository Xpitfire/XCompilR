using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace XCompilR.IntelliSense
{
    /// <summary>
    /// Tutorial: https://msdn.microsoft.com/en-us/library/ee372314.aspx
    /// </summary>
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("plaintext")]
    [Name("token completion")]
    public class XCompileCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new XCompileCompletionSource(this, textBuffer);
        }
    }
}
