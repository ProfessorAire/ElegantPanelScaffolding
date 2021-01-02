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
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        public BooleanValueChangedEventArgs(bool value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value of the boolean event.
        /// </summary>
        public bool Value { get; private set; }
    }
}