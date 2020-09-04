using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Used for all events where a string value is changed by the touchpanel. (Text entry events, etc.)
    /// </summary>
    public class StringValueChangedEventArgs : EventArgs
    {
        public string Value { get; set; }
        public StringValueChangedEventArgs(string value)
        {
            Value = value;
        }
    }
}