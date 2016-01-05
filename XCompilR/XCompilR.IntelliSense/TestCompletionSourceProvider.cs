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
    /// http://stackoverflow.com/questions/9133887/how-to-extend-intellisense-items
    /// </summary>
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("plaintext")]
    [Name("token completion")]
    public class TestCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) => new TestCompletionSource(this, textBuffer);
    }
}
