using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPS.CodeGen.Builders
{
    /// <summary>
    /// Specifies what type of class a ClassBuilder is creating.
    /// </summary>
    public enum ClassType
    {
        /// <summary>
        /// Specifies a Touchpanel.
        /// </summary>
        Touchpanel,

        /// <summary>
        /// Specifies a Page or Subpage.
        /// </summary>
        Page,
       
        /// <summary>
        /// Specifies a Control.
        /// </summary>
        Control,

        /// <summary>
        /// Specifies a SmartObject Control.
        /// </summary>
        SmartObject,

        /// <summary>
        /// Specifies a Subpage Reference List element.
        /// </summary>
        SrlElement
    }
}
