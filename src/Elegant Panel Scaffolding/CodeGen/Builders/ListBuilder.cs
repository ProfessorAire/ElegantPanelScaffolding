﻿using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Builders
{
    public class ListBuilder : ElementBase
    {
        private ushort DigitalStep { get; set; }
        private ushort AnalogStep { get; set; }
        private ushort SerialStep { get; set; }
        private ushort Quantity { get; set; }

        public ClassBuilder Control { get; }

        public ListBuilder(ClassBuilder control, ushort quantity, ushort digitalStep, ushort analogStep, ushort serialStep)
        {
            DigitalStep = digitalStep;
            AnalogStep = analogStep;
            SerialStep = serialStep;
            Quantity = quantity;
            Control = control;
        }

        public override (string name, ushort join)[] GetData() => new (string name, ushort join)[Quantity];

        public override List<Writers.WriterBase> GetWriters()
        {
            var fw = new Writers.FieldWriter($"Items", $"{Control.ClassName}[]")
            {
                Modifier = Modifier.ReadOnly
            };

            fw.Help.Summary = $"The array of <see cref=\"{Control.ClassName}\"/> items in the list.";
            var tw = new Writers.TextWriter($"Items = new {Control.ClassName}[{Quantity}]");
            tw.Text.Add("{");

            for (var i = 0; i < Quantity; i++)
            {
                var digital = (i * DigitalStep) + Control.DigitalOffset;
                var analog = (i * AnalogStep) + Control.AnalogOffset;
                var serial = (i * SerialStep) + Control.SerialOffset;
                tw.Text.Add($"new {Control.ClassName}(ParentPanel, {digital}, {analog}, {serial}, {i}){(i < Quantity - 1 ? "," : "")}");
            }

            tw.Text.Add("};");
            return new List<Writers.WriterBase>() { fw, tw };
        }
    }
}