using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace InlineCompletion.VSIX
{
    internal class InlineCompletionPresenter : ICompletionPresenter
    {
        private readonly InlineCompletionPresenterProvider factory;
        private readonly ITextView textView;

        public InlineCompletionPresenter(InlineCompletionPresenterProvider factory, ITextView textView)
        {
            this.factory = factory;
            this.textView = textView;
        }

        public event EventHandler<CompletionFilterChangedEventArgs> FiltersChanged;
        public event EventHandler<CompletionItemSelectedEventArgs> CompletionItemSelected;
        public event EventHandler<CompletionItemEventArgs> CommitRequested;
        public event EventHandler<CompletionClosedEventArgs> CompletionClosed;

        public void Close()
        {
            if (!this.textView.Properties.TryGetProperty(nameof(CompletionTagger), out CompletionTagger tagger))
            {
                return;
            }
            tagger.IsActive = false;
            tagger.Location = default;
        }

        public void Dispose()
        {
        }

        public void Open(IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
        {
            if (!this.textView.Properties.TryGetProperty(nameof(CompletionTagger), out CompletionTagger tagger))
            {
                return;
            }
            tagger.IsActive = true;
            tagger.Location = presentation.ApplicableToSpan;
            tagger.Text = presentation.Items[presentation.SelectedItemIndex].CompletionItem.DisplayText;
        }

        public void Update(IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
        {
            if (!this.textView.Properties.TryGetProperty(nameof(CompletionTagger), out CompletionTagger tagger))
            {
                return;
            }
            tagger.IsActive = true;
            tagger.Text = presentation.Items[presentation.SelectedItemIndex].CompletionItem.DisplayText;
        }
    }
}