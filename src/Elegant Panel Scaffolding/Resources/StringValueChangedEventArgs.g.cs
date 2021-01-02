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
        /// <summary>
        /// Initialize a new instance of the <see cref="StringValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="value">The string value.</param>
        public StringValueChangedEventArgs(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Value { get; private set; }
    }
}