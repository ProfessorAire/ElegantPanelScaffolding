using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace EPS.UI
{

    /// <summary>
    /// Interaction logic for Preview.xaml
    /// </summary>
    public partial class Preview : Window
    {
        public Preview()
        {
            InitializeComponent();
            TextViewer.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
            TextViewer.IsReadOnly = true;
            DataContext = ItemList;
            foldingManager = FoldingManager.Install(TextViewer.TextArea);
            foldingStrategy = new XmlFoldingStrategy();
        }

        private FoldingManager foldingManager;
        private readonly XmlFoldingStrategy foldingStrategy;

        public ObservableCollection<DetailItem> ItemList { get; } = new ObservableCollection<DetailItem>();

        private void Items_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                var item = (DetailItem)Items.SelectedItem;
                if (item != null)
                {
                    var text = item.Name;
                    if (foldingManager != null)
                    {
                        FoldingManager.Uninstall(foldingManager);
                    }
                    TextViewer.Document = new ICSharpCode.AvalonEdit.Document.TextDocument(item.Content);
                    foldingManager = FoldingManager.Install(TextViewer.TextArea);
                    foldingStrategy.UpdateFoldings(foldingManager, TextViewer.Document);
                }

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
