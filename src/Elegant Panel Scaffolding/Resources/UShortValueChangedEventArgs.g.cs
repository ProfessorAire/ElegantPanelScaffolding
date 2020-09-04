using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace SharpProTouchpanelDemo.UI.Core
{
    public class UShortValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Used for all events where a ushort value is changed from the touchpanel. (Sliders, etc.)
        /// </summary>
        public ushort Value { get; set; }
        public UShortValueChangedEventArgs(ushort value)
        {
            Value = value;
        }
    }
}