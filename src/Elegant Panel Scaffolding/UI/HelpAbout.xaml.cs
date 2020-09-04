using EPS.Help;
using Markdig;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace EPS.UI
{

    /// <summary>
    /// Interaction logic for HelpAbout.xaml
    /// </summary>
    public partial class HelpAbout : Window
    {
        private readonly string[] licenses = System.Array.Empty<string>();

        public HelpAbout()
        {
            InitializeComponent();
            var assembly = Assembly.GetCallingAssembly();

            Version.Text = $"Release - {Options.Current.Version}";

            var items = assembly.GetManifestResourceNames().Where((o) => o.EndsWith(".License.txt", System.StringComparison.InvariantCulture));
            var index = 1;
            licenses = new string[items.Count()];
            foreach (var license in items.Where((o) => o.EndsWith("License.txt", System.StringComparison.InvariantCulture)))
            {
                if (license.Contains("Elegant Panel Scaffolding.License.txt"))
                {
                    licenses[0] = GetText(assembly.GetManifestResourceStream(license));
                    LicenseSelection.Items.Insert(0, license.Replace(".License.txt", " License").Replace("EPS.Licenses.", ""));
                }
                else
                {
                    licenses[index] = GetText(assembly.GetManifestResourceStream(license));
                    _ = LicenseSelection.Items.Add(license.Replace(".License.txt", " License").Replace("EPS.Licenses.", ""));
                    index++;
                }
            }

            LicenseSelection.SelectedIndex = 0;


            var pipeline = new MarkdownPipelineBuilder().
                UseAdvancedExtensions().
                UseColorizer().
                Build();

            var html = Markdown.ToHtml(
                GetText(assembly.GetManifestResourceStream("EPS.Help.Help.md")), pipeline);

            var markdownCss = GetText(assembly.GetManifestResourceStream("EPS.Help.Help.css"));
            var syntaxCss = GetText(assembly.GetManifestResourceStream("EPS.Help.syntax.css"));
            // var syntaxJs = GetText(assembly.GetManifestResourceStream("EPS.Help.highlight.pack.js"));

            html = $"<html>\n<head>\n" +
                //$"<script type=\"text/javascript\">\n{syntaxJs}\n</script>\n" +
                $"<style>\n{markdownCss}\n</style>\n" +
                $"<style>\n{syntaxCss}\n</style>\n" +
                $"</head>\n" +
                $"<body>\n{html}\n" +
                //"<script>hljs.initHighlightingOnLoad();</script>\n" +
                $"</body>\n</html>";

            Tips.NavigateToString(html);


        }

        private static string GetText(System.IO.Stream stream)
        {
            try
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return "";
            }
        }

        private void LicenseSelection_SelectionChanged(object sender, SelectionChangedEventArgs e) => LicenseView.Text = licenses[LicenseSelection.SelectedIndex];
    }
}
