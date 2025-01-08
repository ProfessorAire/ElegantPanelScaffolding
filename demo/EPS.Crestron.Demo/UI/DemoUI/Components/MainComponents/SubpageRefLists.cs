using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EPS.Demo.UI.DemoUI.Components.MainComponents
{
    public partial class SubpageRefLists
    {
        partial void InitializeUI()
        {
            var iconList = new List<IconDetails>();
            for (ushort i = 0; i < 111; i++)
            {
                iconList.Add(new IconDetails(i));
            }

            this.VerticalIconReference.ValueSource = iconList;
            this.VerticalIconReference.SelectedValueChanged += (o, a) =>
                {
                    if (a.NewValue != null)
                    {
                        this.SelectedItemIcon.IconNumber = a.NewValue.Number;
                        this.SelectedItemLabel.Text = string.Format("Icon {0}", a.NewValue.Number);
                        this.VerticalItemIsSelected.IsVisible = a.NewValue != null;
                        this.VerticalItemIsNotSelected.IsVisible = a.NewValue == null;
                    }
                    else
                    {
                        this.VerticalItemIsSelected.IsVisible = false;
                        this.VerticalItemIsNotSelected.IsVisible = true;
                    }
                };

            this.VerticalItemIsSelected.IsVisible = false;
            this.VerticalItemIsNotSelected.IsVisible = true;
        }
    }
}