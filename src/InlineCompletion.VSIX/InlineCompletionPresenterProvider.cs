using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InlineCompletion.VSIX
{
    [Export(typeof(ICompletionPresenterProvider))]
    [ContentType("text")]
    [Order(Before = PredefinedCompletionNames.DefaultCompletionPresenter)]
    internal class InlineCompletionPresenterProvider : ICompletionPresenterProvider
    {
        public CompletionPresenterOptions Options { get; } = new CompletionPresenterOptions(resultsPerPage: 1);

        public ICompletionPresenter GetOrCreate(ITextView textView)
        {
            return new InlineCompletionPresenter(this);
        }
    }
}
