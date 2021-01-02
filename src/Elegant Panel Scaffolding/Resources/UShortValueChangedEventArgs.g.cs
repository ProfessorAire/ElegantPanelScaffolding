using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Used for all events where a ushort value is changed from the touchpanel. (Sliders, etc.)
    /// </summary>
    public class UShortValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UShortValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="value">The ushort value.</param>
        public UShortValueChangedEventArgs(ushort value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public ushort Value { get; private set; }
    }
}