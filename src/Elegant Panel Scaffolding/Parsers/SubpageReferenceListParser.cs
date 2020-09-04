using EPS.CodeGen.Builders;
using System.Linq;
using System.Xml.Linq;

namespace EPS.Parsers
{
    internal class SubpageReferenceListParser
    {

        public static void Parse(XElement props, ClassBuilder builder)
        {
            if (props == null || builder == null)
            {
                return;
            }

            var useSetQuantity = bool.Parse(props?.Element("UseSetNumItems")?.Value ?? "false");
            var useEnabled = bool.Parse(props?.Element("UseEnabledItems")?.Value ?? "false");
            var useVisible = bool.Parse(props?.Element("UseVisibleItems")?.Value ?? "false");

            _ = ushort.TryParse(props?.Element("DigitalJoinIncrement")?.Value ?? "0", out var digitalIncrement);
            _ = ushort.TryParse(props?.Element("AnalogJoinIncrement")?.Value ?? "0", out var analogIncrement);
            _ = ushort.TryParse(props?.Element("SerialJoinIncrement")?.Value ?? "0", out var serialIncrement);

            _ = ushort.TryParse(props?.Element("NumSubpageReferences")?.Value ?? "0", out var pageQuantity);

            _ = ushort.TryParse(props?.Element("ItemEnableJoinGroup")?.Element("StartJoinNumber")?.Value ?? "0", out var enableStart);


            _ = ushort.TryParse(props?.Element("ItemVisibilityJoinGroup")?.Element("StartJoinNumber")?.Value ?? "0", out var visStart);

            _ = ushort.TryParse(props?.Element("DigitalTriListJoinGroup")?.Element("StartJoinNumber")?.Value ?? "0", out var digStart);

            _ = ushort.TryParse(props?.Element("AnalogTriListJoinGroup")?.Element("StartJoinNumber")?.Value ?? "0", out var analogStart);

            _ = ushort.TryParse(props?.Element("SerialTriListJoinGroup")?.Element("StartJoinNumber")?.Value ?? "0", out var serialStart);

            var pageReference = props?.Element("Subpage")?.Element("PageID").Value ?? "0";
            if (pageReference == "0")
            {
                return;
            }

            if (useSetQuantity)
            {
                if (ushort.TryParse(props?.Element("AnalogNumberOfItemsJoin")?.Element("JoinNumber")?.Value ?? "0", out var quantityJoin) && quantityJoin > 0)
                {
                    builder.AddProperty(new PropertyElement("ItemQuantity", quantityJoin, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                }
            }

            if (ushort.TryParse(props?.Element("AnalogSelectJoin")?.Element("JoinNumber")?.Value ?? "0", out var selectJoin) && selectJoin > 0)
            {
                builder.AddEvent(new EventElement("ItemSelectionChanged", selectJoin, builder.SmartJoin, JoinType.Analog, false));
            }

            if (ushort.TryParse(props?.Element("AnalogScrollJoin")?.Element("JoinNumber")?.Value ?? "0", out var scrollJoin) && selectJoin > 0)
            {
                builder.AddProperty(new PropertyElement("ScrollToItem", scrollJoin, builder.SmartJoin, JoinType.Analog, PropertyMethod.Void));
            }

            var subpage = props?.Document?.Root?.Element("Properties")?.Element("Pages")?.Elements()?.Where(e => e.Name.LocalName.ToUpperInvariant() == "PAGE" && e.Element("ControlName")?.Value.ToUpperInvariant() == "SUBPAGE" && e.Attribute("uid")?.Value == pageReference)?.FirstOrDefault() ?? null;
            if (subpage == null)
            {
                return;
            }


            var subBuilder = new ClassBuilder(ClassType.SrlElement)
            {
                ClassName = $"{builder.ClassName}Item", //$"Item", //$"{(subpage?.Element("ObjectName")?.Value ?? "Subpage")}",
                Namespace = builder.Namespace,
                //DigitalOffset = (ushort)(digStart - 1),
                //AnalogOffset = (ushort)(analogStart - 1),
                //SerialOffset = (ushort)(serialStart - 1),
                SmartJoin = builder.SmartJoin
            };

            foreach (var c in subpage?.Element("Properties")?.Element("Children")?.Elements()?.
                Where(e => e.Name.LocalName != "Subpage") ?? System.Array.Empty<XElement>())
            {
                GenericParser.ParseChildElement(c, subBuilder);
            }

            if (useVisible)
            {
                subBuilder.AddProperty(new PropertyElement("IsVisible", 1, subBuilder.SmartJoin, JoinType.SrlVisibility, PropertyMethod.ToPanel));
            }
            if (useEnabled)
            {
                subBuilder.AddProperty(new PropertyElement("IsEnabled", 1, subBuilder.SmartJoin, JoinType.SrlEnable, PropertyMethod.ToPanel));
            }

            subBuilder.DigitalOffset = (ushort)(digStart - 1);
            subBuilder.AnalogOffset = (ushort)(analogStart - 1);
            subBuilder.SerialOffset = (ushort)(serialStart - 1);

            var list = new ListBuilder(subBuilder, pageQuantity, digitalIncrement, analogIncrement, serialIncrement)
            {
                Name = props?.Parent?.Element("ObjectName")?.Value ?? ""
            };

            builder.AddList(list);
        }

    }
}
