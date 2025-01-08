using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EPS.Demo
{
    public class IconDetails
    {
        public IconDetails(ushort number)
        {
            Number = number;
        }

        public ushort Number { get; private set; }
    }
}