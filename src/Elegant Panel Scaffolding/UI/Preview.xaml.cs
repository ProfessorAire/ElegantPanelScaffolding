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
            this.InitializeComponent();
            this.TextViewer.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
            this.TextViewer.IsReadOnly = true;
            this.DataContext = this.ItemList;
            this.foldingManager = FoldingManager.Install(this.TextViewer.TextArea);
            this.foldingStrategy = new XmlFoldingStrategy();
        }

        private FoldingManager foldingManager;
        private readonly XmlFoldingStrategy foldingStrategy;

        public ObservableCollection<DetailItem> ItemList { get; } = new ObservableCollection<DetailItem>();

        private void Items_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                var item = (DetailItem)this.Items.SelectedItem;
                if (item != null)
                {
                    var text = item.Name;
                    if (this.foldingManager != null)
                    {
                        FoldingManager.Uninstall(this.foldingManager);
                    }
                    this.TextViewer.Document = new ICSharpCode.AvalonEdit.Document.TextDocument(item.Content);
                    this.foldingManager = FoldingManager.Install(this.TextViewer.TextArea);
                    this.foldingStrategy.UpdateFoldings(this.foldingManager, this.TextViewer.Document);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
