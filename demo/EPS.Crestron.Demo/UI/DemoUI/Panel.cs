using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EPS.Crestron.Demo.UI.DemoUI
{
    public partial class Panel
    {
        partial void SetupUI()
        {
            Main.Menu.Options.ItemClickedChanged += (o, a) =>
                {
                    SelectPage((DemoPage)a.Value);
                };
        }

        partial void InitializeUI()
        {
            Main.Menu.Options.Items[0].Text = "Info";
            Main.Menu.Options.Items[1].Text = "Subpage Reference Lists";
        }

        public void SelectPage(DemoPage page)
        {
            switch (page)
            {
                case DemoPage.SubpageReferenceLists:
                    Main.Menu.Options.Items[1].IsSelected = true;
                    Main.SubpageRefLists.IsVisible = true;
                    break;
                default:
                    // Startup here.
                    break;
            }
        }

        private void ClearPageSelections()
        {
            Main.SubpageRefLists.IsVisible = false;
            
            for (var i = 0; i < Main.Menu.Options.Items.Length; i++)
            {
                Main.Menu.Options.Items[i].IsSelected = false;
            }
        }
    }
}