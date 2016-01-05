using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace XCompilR.IntelliSense
{
    
    public class TestCompletionSource : ICompletionSource
    {
        private readonly TestCompletionSourceProvider _sourceProvider;
        private readonly ITextBuffer _textBuffer;
        private List<Completion> _compList;

        public TestCompletionSource(TestCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            _sourceProvider = sourceProvider;
            _textBuffer = textBuffer;
        }

        #region ICompletionSource Members

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            //var completions = new CompletionSet();
            //completions.Completions.Add(new Completion("Test"));
            //completionSets.Add(completions);
            List<string> strList = new List<string>();
            strList.Add("addition");
            strList.Add("adaptation");
            strList.Add("subtraction");
            strList.Add("summation");
            _compList = new List<Completion>();
            foreach (string str in strList)
                _compList.Add(new Completion(str, str, str, null, null));

            completionSets.Add(new CompletionSet(
                "Tokens",    //the non-localized title of the tab
                "Tokens",    //the display title of the tab
                FindTokenSpanAtPosition(session.GetTriggerPoint(_textBuffer),
                    session),
                _compList,
                null));
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = _sourceProvider.NavigatorService.GetTextStructureNavigator(_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }


        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }

        #endregion
    }
}
