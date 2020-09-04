using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Used for events where a boolean value needs to get passed and the
    /// value of the boolean matters.
    /// </summary>
    public class BooleanValueChangedEventArgs : EventArgs
    {
        public bool Value { get; set; }
        public BooleanValueChangedEventArgs(bool value)
        {
            Value = value;
        }
    }
}