using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace InlineCompletion.VSIX
{
    [Export]
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal class CompletionTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IClassificationFormatMapService formatMap = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null)
            {
                return null;
            }

            // provide highlighting only on the top-level buffer
            if (textView.TextBuffer != buffer)
            {
                return null;
            }

            var tagger = new CompletionTagger(textView, buffer, this);
            textView.Properties[nameof(CompletionTagger)] = tagger;
            return tagger as ITagger<T>;
        }
    }
}
