using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using System;

namespace InlineCompletion.VSIX
{
    internal class InlineCompletionPresenter : ICompletionPresenter
    {
        private InlineCompletionPresenterProvider factory;

        public InlineCompletionPresenter(InlineCompletionPresenterProvider factory)
        {
            this.factory = factory;
        }

        public event EventHandler<CompletionFilterChangedEventArgs> FiltersChanged;
        public event EventHandler<CompletionItemSelectedEventArgs> CompletionItemSelected;
        public event EventHandler<CompletionItemEventArgs> CommitRequested;
        public event EventHandler<CompletionClosedEventArgs> CompletionClosed;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Open(IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
        {
            throw new NotImplementedException();
        }

        public void Update(IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
        {
            throw new NotImplementedException();
        }
    }
}