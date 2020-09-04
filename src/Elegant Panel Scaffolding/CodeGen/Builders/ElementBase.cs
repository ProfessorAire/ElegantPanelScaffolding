using System.Collections.Generic;

namespace EPS.CodeGen.Builders
{
    public abstract class ElementBase
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        internal ushort join;
        public string ContentOverride { get; set; } = "";

        public ushort SmartJoin { get; set; }

        public ushort DigitalOffset { get; set; }
        public ushort AnalogOffset { get; set; }
        public ushort SerialOffset { get; set; }

        public abstract (string name, ushort join)[] GetData();

        public abstract List<Writers.WriterBase> GetWriters();
    }
}
