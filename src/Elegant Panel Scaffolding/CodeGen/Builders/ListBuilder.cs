using EPS.CodeGen.Writers;
using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Builders
{
    public class ListBuilder
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ContentOverride { get; set; } = "";

        public ushort SmartJoin { get; set; }

        public ushort DigitalOffset { get; set; }
        public ushort AnalogOffset { get; set; }
        public ushort SerialOffset { get; set; }

        private ushort DigitalStep { get; set; }
        private ushort AnalogStep { get; set; }
        private ushort SerialStep { get; set; }
        private ushort Quantity { get; set; }

        public ClassBuilder Control { get; }

        public ListBuilder(ClassBuilder control, ushort quantity, ushort digitalStep, ushort analogStep, ushort serialStep)
        {
            this.DigitalStep = digitalStep;
            this.AnalogStep = analogStep;
            this.SerialStep = serialStep;
            this.Quantity = quantity;
            this.Control = control;
        }

        public List<WriterBase> GetWriters()
        {
            var epw = new PropertyWriter($"Evands.EPS.Common.IListItemProvider<{this.Control.ClassName}>.Items", $"System.Collections.ObjectModel.ReadOnlyCollection<{this.Control.ClassName}>", false)
            {
                HasGetter = true,
                HasSetter = false,
                Accessor = Accessor.None,
                ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged,
            };

            epw.Getter.Add($"return new System.Collections.ObjectModel.ReadOnlyCollection<{this.Control.ClassName}>(this.Items);");

            epw.Help.Summary = $"Gets an enumeration of <see cref=\"{this.Control.ClassName}\"/> items the list contains.";

            var pw = new PropertyWriter($"Items", $"{this.Control.ClassName}[]", false)
            {
                PrivateGetter = false,
                PrivateSetter = true,
                HasSetter = true,
                HasGetter = true,
                ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged
            };

            pw.Help.Summary = $"Gets the array of <see cref=\"{this.Control.ClassName}\"/> items in the list.";

            var tw = new TextWriter($"Items = new {this.Control.ClassName}[{this.Quantity}]");
            tw.Text.Add("{");

            for (var i = 0; i < this.Quantity; i++)
            {
                var digital = (i * this.DigitalStep) + this.Control.DigitalOffset;
                var analog = (i * this.AnalogStep) + this.Control.AnalogOffset;
                var serial = (i * this.SerialStep) + this.Control.SerialOffset;
                tw.Text.Add($"\tnew {this.Control.ClassName}(ParentPanel, {digital}, {analog}, {serial}, {i}){(i < this.Quantity - 1 ? "," : "")}");
            }

            tw.Text.Add("};");
            return new List<WriterBase>() { epw, pw, tw };
        }
    }
}