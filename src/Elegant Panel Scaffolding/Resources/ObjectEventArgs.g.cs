using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Generic object event args.
    /// </summary>
    public class ObjectEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEventArgs"/> class.
        /// </summary>
        /// <param name="args">The smart signal event args.</param>
        public ObjectEventArgs(SmartObjectEventArgs args)
        {
            UserObject = args.Sig.UserObject;
            switch (args.Event)
            {
                case eSigEvent.BoolChange:
                    BoolValue = args.Sig.BoolValue ? true : false;
                    break;
                case eSigEvent.UShortChange:
                    UShortValue = args.Sig.UShortValue;
                    break;
                case eSigEvent.StringChange:
                    StringValue = args.Sig.StringValue;
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEventArgs"/> class.
        /// </summary>
        /// <param name="args">The signal event args.</param>
        public ObjectEventArgs(SigEventArgs args)
        {
            UserObject = args.Sig.UserObject;
            switch (args.Event)
            {
                case eSigEvent.BoolChange:
                    BoolValue = args.Sig.BoolValue ? true : false;
                    break;
                case eSigEvent.UShortChange:
                    UShortValue = args.Sig.UShortValue;
                    break;
                case eSigEvent.StringChange:
                    StringValue = args.Sig.StringValue;
                    break;
            }
        }

        /// <summary>
        /// Gets the user object.
        /// </summary>
        public object UserObject { get; private set; }

        /// <summary>
        /// Gets the boolean value.
        /// </summary>
        public bool BoolValue { get; private set; }

        /// <summary>
        /// Gets the ushort value.
        /// </summary>
        public ushort UShortValue { get; private set; }

        /// <summary>
        /// Gets the string value.
        /// </summary>
        public string StringValue { get; private set; }
    }
}
