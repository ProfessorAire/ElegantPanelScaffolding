using EPS.CodeGen.Builders;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EPS.Parsers
{
    internal class CIPTagParser
    {
        public static void ParseCIP(XElement? element, ClassBuilder builder)
        {
            if (element == null || builder == null) { return; }

            static List<XElement> GetLabels(XElement subElement)
            {
                var its = new List<XElement>();
                var l = subElement.Elements("Label");
                if (l != null && l.Any())
                {
                    its.AddRange(l);
                }
                foreach (var el in subElement.Elements())
                {
                    its.AddRange(GetLabels(el));
                }
                return its;
            }

            var labels = GetLabels(element);

            foreach (var label in labels)
            {
                if (label.Value.Contains("cipd"))
                {
                    ParseDigitalCIP(label, builder);
                }
                if (label.Value.Contains("cipa"))
                {
                    ParseAnalogCIP(label, builder);
                }
                if (label.Value.Contains("cips"))
                {
                    ParseSerialCIP(label, builder);
                }
            }

        }

        public static void ParseTemplateCIP(XElement? element, ClassBuilder? builder, int? quantity)
        {
            if (element == null || builder == null || quantity == null) { return; }
            for (uint i = 0; i < quantity; i++)
            {
                if (element.Value.Contains("cipd"))
                {
                    ParseDigitalCIP(element, builder);
                }
                if (element.Value.Contains("cipa"))
                {
                    ParseAnalogCIP(element, builder);
                }
                if (element.Value.Contains("cips"))
                {
                    ParseSerialCIP(element, builder);
                }
            }
        }

        private static void ParseSerialCIP(XElement element, ClassBuilder builder)
        {
            var items = element.Value.Split('>');

            var count = 1;
            foreach (var item in items)
            {
                if (item.EndsWith("/cips", System.StringComparison.InvariantCulture))
                {
                    var index = item.IndexOf('?');
                    if (index < 0)
                    {
                        index = item.IndexOf('<');
                    }
                    if (index >= 0)
                    {
                        var joinText = item.Substring(0, index);
                        if (ushort.TryParse(joinText, out var join))
                        {
                            builder.AddProperty(new PropertyElement($"String{count}", join, builder.SmartJoin, JoinType.Serial));
                            count++;
                        }
                    }
                }
            }
        }

        private static void ParseAnalogCIP(XElement element, ClassBuilder builder)
        {
            var items = element.Value.Split('>');

            var count = 1;
            foreach (var item in items)
            {
                if (item.EndsWith("/cipa", System.StringComparison.InvariantCulture))
                {
                    var index = item.IndexOf('?');
                    if (index < 0)
                    {
                        index = item.IndexOf('<');
                    }
                    if (index >= 0)
                    {
                        var joinText = item.Substring(0, index);
                        if (ushort.TryParse(joinText, out var join))
                        {
                            builder.AddProperty(new PropertyElement($"UShort{count}", join, builder.SmartJoin, JoinType.Analog));
                            count++;
                        }
                    }
                }
            }
        }

        private static void ParseDigitalCIP(XElement element, ClassBuilder builder)
        {
            var items = element.Value.Split('>');

            var count = 1;
            foreach (var item in items)
            {
                if (item.EndsWith("/cipd", System.StringComparison.InvariantCulture))
                {
                    var index = item.IndexOf('?');
                    if (index < 0)
                    {
                        index = item.IndexOf('<');
                    }
                    if (index >= 0)
                    {
                        var joinText = item.Substring(0, index);
                        if (ushort.TryParse(joinText, out var join))
                        {
                            builder.AddProperty(new PropertyElement($"Bool{count}", join, builder.SmartJoin, JoinType.Digital));
                            count++;
                        }
                    }
                }
            }
        }
    }
}
