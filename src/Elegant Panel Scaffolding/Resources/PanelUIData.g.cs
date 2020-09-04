using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace SharpProTouchpanelDemo.UI.Core
{
    public abstract class PanelUIData
    {
        public object AssociatedClass = null;

        /// <summary>
        /// A list of boolean data COMING FROM the touchpanel. The names used on these dictionaries
        /// actually refer to the methods for raising events.
        /// </summary>
        public Dictionary<string, ushort> boolOutputs;

        /// <summary>
        /// A list of ushort values COMING FROM the touchpanel.
        /// </summary>
        public Dictionary<string, ushort> ushortOutputs;

        /// <summary>
        /// A list of string values COMING FROM the touchpanel.
        /// </summary>
        public Dictionary<string, ushort> stringOutputs;

        /// <summary>
        /// A list of ushort data coming from the smart object on the touchpanel.
        /// </summary>
        public Dictionary<string, ushort> smartObjectUShortOutputs;

        /// <summary>
        /// A list of boolean data coming from the smart object on the touchpanel.
        /// </summary>
        public Dictionary<string, ushort> smartObjectBoolOutputs;
        
        /// <summary>
        /// A list of string data coming from the smart object on the touchpanel.
        /// </summary>
        public Dictionary<string, ushort> smartObjectStringOutputs;

        public ushort smartObjectJoin = 0;

    }
}