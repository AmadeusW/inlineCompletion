using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using PythiaComposeVS.ExtensionState;
using PythiaComposeVS.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

namespace PythiaComposeVS.UI
{
    internal class JinusTagger : ITagger<IntraTextAdornmentTag>, IDisposable
    {
        private readonly ITextView view;
        private readonly ITextBuffer buffer;
        private readonly TextBlock textBlock = new TextBlock();
        private readonly CompletionState state = CompletionState.Instance;

        private static bool caretInitialized = false;
        private SnapshotPoint curCaretBufferPosition;
        private bool disposedValue;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        //////////////////////////////Move to other class///////////////////////////////////////////
        private static readonly HashSet<string> queried = new HashSet<string>();
        private static readonly HttpClient client = new HttpClient();
        private static string lineCompletionEndpoint { get; set; } =
            "https://deeplc.microsoft.com:443/api/v1/service/completion-service-dogfood/score";
        ////////////////////////////////////////////////////////////////////////////////////////////

        public AutocompleteTagger(ITextView view,
                                  ITextBuffer buffer,
                                  IClassificationFormatMapService mapService)
        {
            this.view = view;
            this.buffer = buffer;

            var formatMap = mapService.GetClassificationFormatMap(view);
            textBlock.FontFamily =
                formatMap.DefaultTextProperties.Typeface.FontFamily;
            textBlock.FontSize = formatMap.DefaultTextProperties.FontRenderingEmSize;
            textBlock.Opacity = 0.6;
            textBlock.Foreground = formatMap.DefaultTextProperties.ForegroundBrush;

            this.view.Caret.PositionChanged += CaretPositionChanged;
            ((ITextBuffer2)this.buffer).ChangedOnBackground += TextBufferChangedOnBackground;
            this.view.LayoutChanged += ViewLayoutChanged;
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0 || !caretInitialized || state.ActiveView != view)
            {
                yield break;
            }

            textBlock.Text = state.Sb.ToString();
            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity,
                                                      double.PositiveInfinity));

            var tag = new IntraTextAdornmentTag(textBlock,
                                                null,
                                                PositionAffinity.Successor);
            yield return new TagSpan<IntraTextAdornmentTag>(new SnapshotSpan(curCaretBufferPosition,
                                                                             0),
                                                            tag);
        }

        private void TextBufferChangedOnBackground(object sender, TextContentChangedEventArgs e)
        {
            if (e.Changes.Count == 0)
            {
                return;
            }

            if (e.Changes[0].NewLength == 0)
            {
                state.Sb.Clear();
                return;
            }

            // Check if user is typing into what we're trying to suggest
            if (state.Sb.ToString().StartsWith(e.Changes[0].NewText))
            {
                state.Sb.Remove(0, e.Changes[0].NewLength);
                return;
            }

            // 2.If user is typing a completely different thing, check cache
            //   a. Synchronous cache check
            //   b. If nothing, async query (the function might not query, but whatever)
            var codeBeforeCaret = new SnapshotSpan(view.TextSnapshot,
                                                   0,
                                                   curCaretBufferPosition).GetText();
            var normalized = DocumentProcessor.NormalizeCode(codeBeforeCaret) + e.Changes[0].NewText;
            var completion = state.Cache.GetCompletion(normalized);
            state.Sb.Clear();
            state.Sb.Append(completion);
            if (completion.Length == 0)
            {
                // Used to grab the filename
                view.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument),
                                                          out ITextDocument doc);
                _ = Task.Run(() => RequestServer(normalized, doc));

            }
            else
            {
                state.Sb.Remove(0, e.Changes[0].NewLength);
            }
        }

        private async void RequestServer(string normalized, ITextDocument doc)
        {
            var timeForRequest = Stopwatch.StartNew();
            // Decide if we want to query or not
            if (!IsWithinWord(normalized) && !queried.Contains(normalized))
            {
                queried.Add(normalized);
                var pythiaRequestSetting = new ServerRequestSetting
                {
                    CodeBefore = normalized,
                    FileName = doc == null ? "fileName.cs" : doc.FilePath,
                    NumberOfRecommendations = 8,
                    ReturnRaw = true,
                    SearchDepth = 16
                };

                var content = new StringContent(pythiaRequestSetting.ToString(),
                                                Encoding.UTF8,
                                                "application/json");
                logger.Log("Request: " + await content.ReadAsStringAsync());
                var response = await client.PostAsync(lineCompletionEndpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                state.Cache.DecodeAndSetCompletion(normalized, responseString);
                timeForRequest.Stop();

                var sb = new StringBuilder("Request complete: ");
                sb.Append((timeForRequest.ElapsedMilliseconds).ToString());
                sb.Append("ms");
                logger.Log(sb.ToString());
            }
        }

        private bool IsWithinWord(string normalized)
        {
            var lastChar = normalized[normalized.Length - 1];
            return (Char.IsLetterOrDigit(lastChar) || lastChar == '_');
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            state.ActiveView = e.TextView;
            if (e.OldPosition.BufferPosition.GetContainingLine().Start
                != e.NewPosition.BufferPosition.GetContainingLine().Start
                || e.NewPosition.BufferPosition < e.OldPosition.BufferPosition)
            {
                state.Sb.Clear();
            }
            else if (e.NewPosition.BufferPosition > e.OldPosition.BufferPosition + 1)
            {
                state.Sb.Clear();
            }
            UpdateAtCaretPosition(view.Caret.Position);
        }

        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // makes sure that there really has been a change
            if (e.NewSnapshot != e.OldViewState.EditSnapshot)
            {
                UpdateAtCaretPosition(view.Caret.Position);
            }
        }

        private void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            caretInitialized = true;
            curCaretBufferPosition = caretPosition.BufferPosition;

            // Personal Note:
            // What is this;; Creates lots of InvalidOperations w/o it...;;
            TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(
                        new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.view.Caret.PositionChanged -= CaretPositionChanged;
                    ((ITextBuffer2)this.buffer).ChangedOnBackground -= TextBufferChangedOnBackground;
                    this.view.LayoutChanged -= ViewLayoutChanged;
                    this.view.Properties.RemoveProperty(typeof(AutocompleteTagger));
                    this.state.Sb.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AutocompleteTagger()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
