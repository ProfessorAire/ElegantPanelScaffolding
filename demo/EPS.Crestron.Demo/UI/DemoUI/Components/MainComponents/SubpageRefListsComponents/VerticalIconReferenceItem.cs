using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EPS.Demo.UI.DemoUI.Components.MainComponents.SubpageRefListsComponents
{
    public partial class VerticalIconReferenceItem : Evands.EPS.Lists.ListItemBase<IconDetails>
    {
        partial void SetupUI()
        {
            this.Button.Pressed += (o, a) => this.IsSelected = !this.IsSelected;
            this.IsSelectedChanged += (o, a) => this.Button.IsActive = a.IsSelected;
        }

        protected override void ValueIsChanging(IconDetails newValue, IconDetails oldValue)
        {
            if (newValue != null)
            {
                this.DynamicIcon.IconNumber = newValue.Number;
                this.Button.Text = string.Format("Icon {0}", newValue.Number);
                this.IsVisible = true;
            }
            else
            {
                this.IsVisible = false;
                this.DynamicIcon.IconNumber = 0;
                this.Button.Text = string.Empty;
            }
        }
    }
}