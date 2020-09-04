using ColorCode;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;

namespace EPS.Help
{
    internal class ColorizingCodeRenderer : HtmlObjectRenderer<CodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            if (obj is FencedCodeBlock)
            {
                var text = GetText(obj);
                var formatter = new HtmlClassFormatter();

                var lang = Languages.CSharp;

                var html = formatter.GetHtmlString(text, lang);
                html = html.
                    Replace("<pre>", "<code class=\"codeblock\">").
                    Replace("</pre>", "</code>").
                    Replace("div style=\"color:#000000;background-color:#FFFFFF;\"", "pre class=\"csharp\"").
                    Replace("</div>", "</pre>").
                    Replace("<div class=\"csharp\"", "<pre class=\"csharp\"").
                    Replace("    ", "<span style=\"display:inline-block;margin-left:2.5em;\"></span>").Replace("\r\n", "<br>");

                _ = renderer.Write(html);
            }
            else if (obj is CodeBlock)
            {
                var text = GetText(obj);
                var formatter = new HtmlClassFormatter();
                var html = formatter.GetHtmlString(text, Languages.CSharp);
                html = html.
                    Replace("<pre>", "<code class=\"codeblock\">").
                    Replace("</pre>", "</code>").
                    Replace("div style=\"color:#000000;background-color:#FFFFFF;\"", "pre class=\"csharp\"").
                    Replace("</div>", "</pre>").
                    Replace("<div class=\"csharp\"", "<pre class=\"csharp\"").
                    Replace("    ", "<span style=\"display:inline-block;margin-left:2.5em;\"></span>");

                _ = renderer.Write(html);
            }
        }

        private static string GetText(CodeBlock obj)
        {
            var sb = new StringBuilder();
            var breaks = false;
            string temp;
            foreach (var l in obj.Lines)
            {
                temp = l.ToString();
                if (string.IsNullOrEmpty(temp) && !breaks)
                {
                    _ = sb.AppendLine(temp);
                    breaks = true;
                }
                else if (!string.IsNullOrEmpty(temp))
                {
                    _ = sb.AppendLine(temp);
                    breaks = false;
                }
            }
            return sb.ToString();
        }
    }

    internal class ColorizingInlineCodeRenderer : HtmlObjectRenderer<CodeInline>
    {
        protected override void Write(HtmlRenderer renderer, CodeInline obj)
        {
            if (obj is CodeInline)
            {
                var text = obj.Content;
                var formatter = new HtmlClassFormatter();
                var html = formatter.GetHtmlString(text, Languages.CSharp);
                html = html.
                    Replace("<pre>", "<code>").
                    Replace("</pre>", "</code>").
                    Replace("div style=\"color:#000000;background-color:#FFFFFF;\"", "pre class=\"csharp\"").
                    Replace("</div>", "</pre>").
                    Replace("<div class=\"csharp\"", "<pre class=\"csharp\"");
                _ = renderer.Write(html);
            }
        }
    }
}
