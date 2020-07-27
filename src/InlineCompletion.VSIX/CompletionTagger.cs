using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace InlineCompletion.VSIX
{
    internal class CompletionTagger : ITagger<IntraTextAdornmentTag>
    {
        private ITextView textView;
        private ITextBuffer buffer;
        private CompletionTaggerProvider factory;

        internal bool IsActive { get; set; } = false;
        internal ITrackingSpan Location { get; set; }
        public string Text { get; internal set; }

        public CompletionTagger(ITextView textView, ITextBuffer buffer, CompletionTaggerProvider factory)
        {
            this.textView = textView;
            this.buffer = buffer;
            this.factory = factory;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.IsActive)
            {
                yield return new 
            }
            else
            {
                yield break;
            }
        }
    }
}