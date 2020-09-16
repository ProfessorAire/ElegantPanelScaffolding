using EPS.CodeGen.Builders;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace EPS.Parsers
{
    internal class SmartObjectParser
    {
        public static void ParseSmartObject(XElement child, ClassBuilder rootBuilder)
        {
            if (child == null || rootBuilder == null) { return; }
            var builder = new ClassBuilder(ClassType.SmartObject)
            {
                ClassName = $"{child?.Element("ObjectName")?.Value ?? ""}",
                Namespace = rootBuilder.Namespace
            };

            var props = child?.Element("Properties");

            if (ushort.TryParse(props?.Element("ControlJoin").Value, out var controlJoin))
            {
                builder.SmartJoin = controlJoin;
            }

            if (ushort.TryParse(props?.Element("DigitalEnableJoin")?.Value, out var enableJoin) && enableJoin > 0)
            {
                builder.AddJoin(new JoinBuilder(enableJoin, "IsEnabled", JoinType.Digital, JoinDirection.ToPanel));
            }
            else if (ushort.TryParse(props?.Element("EnableDigitalJoin")?.Value, out var enableJoin2) && enableJoin2 > 0)
            {
                builder.AddJoin(new JoinBuilder(enableJoin2, "IsEnabled", JoinType.Digital, JoinDirection.ToPanel));
            }

            if (ushort.TryParse(props?.Element("DigitalVisibilityJoin")?.Value, out var visibilityJoin) && visibilityJoin > 0)
            {
                builder.AddJoin(new JoinBuilder(visibilityJoin, "IsVisible", JoinType.Digital, JoinDirection.ToPanel));
            }
            else if (ushort.TryParse(props?.Element("VisibilityDigitalJoin")?.Value, out var visibilityJoin2) && visibilityJoin2 > 0)
            {
                builder.AddJoin(new JoinBuilder(visibilityJoin2, "IsVisible", JoinType.Digital, JoinDirection.ToPanel));
            }

            var joinProps = props?.Elements().Where(e => e.Attribute("Type")?.Value == "JoinCommand");
            if (joinProps != null)
            {
                foreach (var j in joinProps)
                {
                    var joinTypeText = j?.Element("JoinType")?.Value;
                    var joinName = SanitizeSignalName(j?.Element("SignalName")?.Value ?? "");
                    _ = ushort.TryParse(j?.Element("JoinNumber")?.Value, out var joinNumber);
                    var isIn = false;
                    var isOut = false;
                    if (j?.Element("Direction") != null && j.Element("Direction").Value.Contains("In"))
                    {
                        isIn = true;
                    }
                    if (j?.Element("Direction") != null && j.Element("Direction").Value.Contains("Out"))
                    {
                        isOut = true;
                    }


                    var joinType = JoinType.Analog;
                    if (joinTypeText == "Serial")
                    {
                        joinType = JoinType.Serial;
                    }
                    else if (joinTypeText == "Digital")
                    {
                        joinType = JoinType.Digital;
                    }

                    var joinDirection = JoinDirection.ToPanel;
                    if (isIn && isOut)
                    {
                        joinDirection = JoinDirection.Both;
                    }
                    else if (isIn && !isOut)
                    {
                        joinDirection = JoinDirection.ToPanel;
                    }
                    else if (!isIn && isOut)
                    {
                        joinDirection = JoinDirection.FromPanel;
                    }

                    if (j?.Name?.LocalName?.Contains("Button") ?? false)
                    {
                        joinType = JoinType.DigitalButton;
                    }

                    if (!string.IsNullOrEmpty(joinName))
                    {
                        builder.AddJoin(
                            new JoinBuilder(joinNumber, builder.SmartJoin, joinName, joinType, joinDirection));
                    }
                }
            }

            //Check for specific type overrides and parse separately.
            if (child?.Element("TargetControl")?.Value == "SubpageReference_List" && props != null)
            {
                SubpageReferenceListParser.Parse(props, builder);
                rootBuilder.AddControl(builder);
                return;
            }


            joinProps = props?.Elements().Where(e => e.Attribute("Type")?.Value == "EncapsulatedJoin");

            if (joinProps != null)
            {
                foreach (var j in joinProps)
                {
                    var joinTypeText = j?.Element("JoinType")?.Value;
                    _ = ushort.TryParse(j?.Element("JoinNumber")?.Value, out var joinNumber);
                    var isIn = false;
                    var isOut = false;
                    var inName = "";
                    var outName = "";
                    if (j?.Element("Direction") != null && j.Element("Direction").Value.Contains("In"))
                    {
                        isIn = true;
                        inName = SanitizeSignalName(j?.Element("InCueName")?.Value ?? "");
                    }
                    if (j?.Element("Direction") != null && j.Element("Direction").Value.Contains("Out"))
                    {
                        isOut = true;
                        outName = SanitizeSignalName(j?.Element("OutCueName")?.Value ?? "");
                    }


                    var joinType = JoinType.Analog;
                    if (joinTypeText == "Serial")
                    {
                        joinType = JoinType.Serial;
                    }
                    else if (joinTypeText == "Digital")
                    {
                        joinType = JoinType.Digital;
                    }

                    var joinDirection = JoinDirection.ToPanel;
                    if (isIn && isOut)
                    {
                        joinDirection = JoinDirection.Both;
                    }
                    else if (isIn && !isOut)
                    {
                        joinDirection = JoinDirection.ToPanel;
                    }
                    else if (!isIn && isOut)
                    {
                        joinDirection = JoinDirection.FromPanel;
                    }

                    if (j?.Name?.LocalName?.Contains("Button") ?? false)
                    {
                        joinType = JoinType.DigitalButton;
                    }

                    builder.AddJoin(
                        new JoinBuilder(joinNumber, builder.SmartJoin, inName, joinType, joinDirection));
                }
            }

            if (((child?.Element("TargetControl")?.Value.Contains("List") ?? false) || (child?.Element("TargetControl")?.Value.Contains("Tab_Button") ?? false)) && props != null)
            {
                var groupElement = GetEncapsulatedGroupJoins(props);
                if (groupElement != null)
                {
                    var listElementName = groupElement?.Name?.LocalName.Remove(groupElement.Name.LocalName.Length - 1, 1);

                    var itemCount = ushort.Parse(groupElement?.Elements().Where(e => e.Name.LocalName.ToUpperInvariant().Contains("COUNT"))?.FirstOrDefault()?.Value ?? "0", NumberFormatInfo.InvariantInfo);

                    if (itemCount == 0)
                    {
                        if (!ushort.TryParse(groupElement?.Parent?.Elements().Where(e => e.Name.LocalName.ToUpperInvariant().Contains("COUNT"))?.FirstOrDefault()?.Value, out itemCount))
                        {
                            return;
                        }
                    }

                    var itemBuilder = new ClassBuilder(ClassType.SrlElement)
                    {
                        SmartJoin = builder.SmartJoin
                    };

                    var groupJoins = GetEncapsulatedGroupJoins(props)?.Elements();
                    var isDynamicList = false;
                    if (groupJoins != null)
                    {
                        if (groupJoins?.FirstOrDefault()?.Element("InCueName") != null)
                        {
                            isDynamicList = true;
                            foreach (var j in groupJoins)
                            {

                                var inName = j?.Element("InCueName")?.Value ?? "";
                                var outName = j?.Element("OutCueName")?.Value ?? "";
                                var isIn = j?.Element("Direction")?.Value.Contains("In") ?? false;
                                var isOut = j?.Element("Direction")?.Value.Contains("Out") ?? false;
                                var startJoin = ushort.Parse(j?.Element("StartJoinNumber")?.Value ?? "0", NumberFormatInfo.InvariantInfo);
                                var jt = j?.Element("JoinType")?.Value ?? "";
                                var joinType = JoinType.None;
                                if (jt == "Digital")
                                {
                                    joinType = JoinType.SmartDigital;
                                }
                                else if (jt == "Analog")
                                {
                                    joinType = JoinType.SmartAnalog;
                                }
                                else if (jt == "Serial")
                                {
                                    joinType = JoinType.SmartSerial;
                                }

                                if (inName.StartsWith("Set Item %i ", StringComparison.InvariantCulture))
                                {
                                    inName = inName.Replace("Set Item %i ", "");
                                }
                                else if (inName.StartsWith("Item %i ", StringComparison.InvariantCulture))
                                {
                                    inName = inName.Replace("Item %i ", "Is");
                                }
                                if (inName == "IsPressed")
                                {
                                    inName = "Pressed";
                                }

                                inName = SanitizeSignalName(inName);

                                if (outName.StartsWith("Item %i ", StringComparison.InvariantCulture))
                                {
                                    outName = outName.Replace("Item %i ", "");
                                }

                                if (outName.ToUpperInvariant().Contains("PRESSED") || outName.ToUpperInvariant().Contains("RELEASED"))
                                {
                                    joinType = JoinType.SmartDigitalButton;
                                }

                                var joinDirection = JoinDirection.Unknown;

                                if (isIn && !isOut)
                                {
                                    joinDirection = JoinDirection.ToPanel;
                                }
                                else if (!isIn && isOut)
                                {
                                    joinDirection = JoinDirection.FromPanel;
                                }
                                else if (isIn && isOut)
                                {
                                    joinDirection = JoinDirection.Both;
                                }

                                if (outName.ToUpperInvariant() == "PRESSED")
                                {
                                    outName = "";
                                }

                                if (outName.ToUpperInvariant() == "RELEASED")
                                {
                                    outName = "";
                                }

                                outName = SanitizeSignalName(outName);

                                itemBuilder.AddJoin(new JoinBuilder(startJoin, itemBuilder.SmartJoin, inName, joinType, joinDirection) { ChangeEventName = outName });
                            }
                        }
                        else if (groupJoins?.FirstOrDefault()?.Element("SignalName") != null)
                        {
                            foreach (var j in groupJoins)
                            {
                                var joinName = j?.Element("SignalName")?.Value ?? "";
                                var isIn = j?.Element("Direction")?.Value.Contains("In") ?? false;
                                var isOut = j?.Element("Direction")?.Value.Contains("Out") ?? false;
                                var joinNumber = ushort.Parse(j?.Element("JoinNumber")?.Value ?? "0", NumberFormatInfo.InvariantInfo);
                                var jt = j?.Element("JoinType")?.Value ?? "";
                                var joinType = JoinType.None;

                                if (jt == "Digital")
                                {
                                    joinType = JoinType.SmartDigital;
                                }
                                else if (jt == "Analog")
                                {
                                    joinType = JoinType.SmartAnalog;
                                }
                                else if (jt == "Serial")
                                {
                                    joinType = JoinType.SmartSerial;
                                }

                                var joinDirection = JoinDirection.Unknown;

                                if (isIn && !isOut)
                                {
                                    joinDirection = JoinDirection.ToPanel;
                                }
                                else if (!isIn && isOut)
                                {
                                    joinDirection = JoinDirection.FromPanel;
                                }
                                else if (isIn && isOut)
                                {
                                    joinDirection = JoinDirection.Both;
                                }

                                if (joinName.ToUpperInvariant().Contains("PRESS"))
                                {
                                    joinName = "";
                                    joinType = JoinType.SmartDigitalButton;
                                }
                                else if (joinName.ToUpperInvariant().Contains("SELECT"))
                                {
                                    joinName = "Selected";
                                }

                                if (j?.Name?.LocalName?.Contains("Button") ?? false)
                                {
                                    joinType = JoinType.DigitalButton;
                                }

                                joinName = SanitizeSignalName(joinName);

                                builder.AddJoin(
                                    new JoinBuilder(joinNumber, builder.SmartJoin, joinName, joinType, joinDirection));
                            }
                        }
                        else
                        {
                            if (groupJoins != null)
                            {
                                foreach (var j in groupJoins)
                                {
                                    var joinName = j?.Element("SignalName")?.Value ?? "";

                                    if (string.IsNullOrEmpty(joinName))
                                    {
                                        continue;
                                    }

                                    var isIn = j?.Element("Direction")?.Value.Contains("In") ?? false;
                                    var isOut = j?.Element("Direction")?.Value.Contains("Out") ?? false;
                                    var joinNumber = ushort.Parse(j?.Element("JoinNumber")?.Value ?? "0", NumberFormatInfo.InvariantInfo);
                                    var jt = j?.Element("JoinType")?.Value ?? "";
                                    var joinType = JoinType.None;

                                    if (jt == "Digital")
                                    {
                                        joinType = JoinType.SmartDigital;
                                    }
                                    else if (jt == "Analog")
                                    {
                                        joinType = JoinType.SmartAnalog;
                                    }
                                    else if (jt == "Serial")
                                    {
                                        joinType = JoinType.SmartSerial;
                                    }

                                    if (joinName.StartsWith("Set Item %i ", StringComparison.InvariantCulture))
                                    {
                                        joinName = joinName.Replace("Set Item %i ", "");
                                    }
                                    else if (joinName.StartsWith("Item %i ", StringComparison.InvariantCulture))
                                    {
                                        joinName = joinName.Replace("Item %i ", "Is");
                                    }

                                    if (joinName == "IsPressed")
                                    {
                                        joinName = "Pressed";
                                    }

                                    if (joinName.ToUpperInvariant().Contains("PRESS"))
                                    {
                                        joinName = "";
                                        joinType = JoinType.SmartDigitalButton;
                                    }
                                    else if (joinName.ToUpperInvariant().Contains("SELECT"))
                                    {
                                        joinName = "Selected";
                                    }

                                    joinName = SanitizeSignalName(joinName);

                                    if (joinName.StartsWith("Is", StringComparison.InvariantCulture))
                                    {
                                        joinName = joinName.Remove(0, 2);
                                    }

                                    var joinDirection = JoinDirection.Unknown;

                                    if (isIn && !isOut)
                                    {
                                        joinDirection = JoinDirection.ToPanel;
                                    }
                                    else if (!isIn && isOut)
                                    {
                                        joinDirection = JoinDirection.FromPanel;
                                    }
                                    else if (isIn && isOut)
                                    {
                                        joinDirection = JoinDirection.Both;
                                    }

                                    itemBuilder.AddJoin(new JoinBuilder(joinNumber, builder.SmartJoin, joinName, joinType, joinDirection));
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(itemBuilder.ClassName))
                        {
                            itemBuilder.ClassName = builder.ClassName + "Item";
                        }
                        else
                        {
                            itemBuilder.ClassName += "Item";
                        }
                        ListBuilder list;
                        if (isDynamicList)
                        {
                            list = new ListBuilder(itemBuilder, itemCount, 1, 1, 1);
                        }
                        else
                        {
                            list = new ListBuilder(itemBuilder, itemCount, 0, 0, 0);
                        }

                        if (list != null)
                        {
                            list.Name = props?.Parent?.Element("ObjectName")?.Value.Replace(" ", "") ?? "List";// + "Item" ?? "ListItem";
                            builder.AddList(list);
                        }
                    }
                }
            }

            CIPTagParser.ParseCIP(child, builder);
            rootBuilder.AddControl(builder);
        }

        private static XElement? GetEncapsulatedGroupJoins(XElement? element)
        {
            if (element == null)
            {
                return null;
            }
            if (element.HasElements)
            {
                foreach (var e in element.Elements())
                {
                    if (e.HasElements && (e?.Elements()?.Where(o => o.HasAttributes && o.Attribute("Type")?.Value == "EncapsulatedJoinGroup")?.Count() > 0
                                      || e?.Elements()?.Where(o => o.HasAttributes && (o.Attribute("Type")?.Value?.Contains("Join") ?? false))?.Count() > 0))
                    {
                        return e;
                    }
                    else
                    {
                        var item = GetEncapsulatedGroupJoins(e);
                        if (item != null)
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }

        //private XElement FindListElements

        private static string SanitizeSignalName(string signalName)
        {
            if (string.IsNullOrEmpty(signalName))
            {
                return signalName;
            }
            signalName = signalName.Replace(" ", "")
                .Replace("-", "")
                .Replace("#", "Pound")
                .Replace("*", "Star")
                .Replace("%", "")
                .Replace("@", "")
                .Replace("!", "")
                .Replace("^", "")
                .Replace("&", "")
                .Replace("(", "")
                .Replace(")", "");

            //Override various names here.
            if (signalName == "SetNumofItems")
            {
                signalName = "NumberOfItems";
            }
            else if (char.IsDigit(signalName[0]))
            {
                if (signalName[0] == '1') { signalName = signalName.Trim('1').Insert(0, "One"); }
                else if (signalName[0] == '2') { signalName = signalName.Trim('2').Insert(0, "Two"); }
                else if (signalName[0] == '3') { signalName = signalName.Trim('3').Insert(0, "Three"); }
                else if (signalName[0] == '4') { signalName = signalName.Trim('4').Insert(0, "Four"); }
                else if (signalName[0] == '5') { signalName = signalName.Trim('5').Insert(0, "Five"); }
                else if (signalName[0] == '6') { signalName = signalName.Trim('6').Insert(0, "Six"); }
                else if (signalName[0] == '7') { signalName = signalName.Trim('7').Insert(0, "Seven"); }
                else if (signalName[0] == '8') { signalName = signalName.Trim('8').Insert(0, "Eight"); }
                else if (signalName[0] == '9') { signalName = signalName.Trim('9').Insert(0, "Nine"); }
                else if (signalName[0] == '0') { signalName = signalName.Trim('0').Insert(0, "Zero"); }
            }
            return signalName;
        }
    }
}
