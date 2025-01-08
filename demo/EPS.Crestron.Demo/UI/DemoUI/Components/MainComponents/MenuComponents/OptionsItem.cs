using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EPS.Demo.UI.DemoUI.Components.MainComponents.MenuComponents
{
    public partial class OptionsItem
    {
        partial void SetupUI()
        {
            this.TextChanged += (o, a) => this.IsVisible = !string.IsNullOrEmpty(a.Value);
        }
    }
}