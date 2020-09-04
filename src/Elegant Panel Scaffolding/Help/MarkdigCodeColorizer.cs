using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using System;

namespace EPS.Help
{
    public class MarkdigCodeColorizer : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        { }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer html)
            {
                var oldRenderer = html.ObjectRenderers.FindExact<CodeBlockRenderer>();
                if (oldRenderer != null)
                {
                    _ = html.ObjectRenderers.Remove(oldRenderer);
                    html.ObjectRenderers.AddIfNotAlready(new ColorizingCodeRenderer());
                }

                var oldInline = html.ObjectRenderers.FindExact<CodeInlineRenderer>();
                if (oldInline != null)
                {
                    _ = html.ObjectRenderers.Remove(oldInline);
                    html.ObjectRenderers.AddIfNotAlready(new ColorizingInlineCodeRenderer());
                }
            }
        }
    }

    public static class MarkdigCodeColorzerExtensions
    {
        public static MarkdownPipelineBuilder UseColorizer(this MarkdownPipelineBuilder pipeline)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            pipeline.Extensions.Add(new MarkdigCodeColorizer());

            return pipeline;
        }
    }
}
