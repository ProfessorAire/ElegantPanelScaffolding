using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

namespace SharpProTouchpanelDemo.UI.Core
{
    public class ObjectEventArgs
    {
        public object UserObject { get; set; }
        public bool BoolValue { get; set; }
        public ushort UShortValue { get; set; }
        public string StringValue { get; set; }

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
    }
}
