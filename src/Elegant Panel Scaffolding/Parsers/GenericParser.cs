using EPS.CodeGen.Builders;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace EPS.Parsers
{
    public static class GenericParser
    {
        public static ushort ParseDigitalOffset(XElement? propertiesElement)
        {
            if (ushort.TryParse(propertiesElement?.Element("DigitalJoinOffset").Value, out var offset) && offset > 0)
            {
                return offset;
            }
            return 0;
        }

        public static ushort ParseAnalogOffset(XElement? propertiesElement)
        {
            if (ushort.TryParse(propertiesElement?.Element("AnalogJoinOffset").Value, out var offset) && offset > 0)
            {
                return offset;
            }
            return 0;
        }

        public static ushort ParseSerialOffset(XElement? propertiesElement)
        {
            if (ushort.TryParse(propertiesElement?.Element("SerialJoinOffset").Value, out var offset) && offset > 0)
            {
                return offset;
            }
            return 0;
        }

        public static EventElement? GetTransitionCompleteProperty(XElement? propertiesElement)
        {
            if (ushort.TryParse(propertiesElement?.Element("TransitionCompleteJoin").Value, out var join) && join > 0)
            {
                return new EventElement("TransitionComplete", join, 0, JoinType.None);
            }
            return null;
        }

        public static void ParseTheme(XElement? propertiesElement, ClassBuilder builder)
        {
            if(builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (ushort.TryParse(propertiesElement?.Element("Themes")?.Element("ProjectThemeJoin")?.Value, out var joinNumber) && joinNumber > 0)
            {
                var theme = new PropertyElement("Theme", joinNumber, builder.SmartJoin, JoinType.Analog)
                {
                    Description = "Gets/Sets the theme number used for the project."
                };
                builder.AddProperty(theme);
            }
        }

        public static void ParseBackgroundJoins(XElement? propertiesElement, ClassBuilder builder)
        {
            if(builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // First dynamic serial join
            if (ushort.TryParse(propertiesElement?.Element("Backgrounds")?.Element("DynamicGraphicsSerial")?.Value, out var backgroundSerialJoin) && backgroundSerialJoin > 0)
            {
                builder.AddProperty(new PropertyElement("BackgroundPath", backgroundSerialJoin, builder.SmartJoin, JoinType.Serial) { Description = "Gets/Sets the serial join for the background dynamic graphic path." });
            }
            // Then analog join
            if (ushort.TryParse(propertiesElement?.Element("Backgrounds")?.Element("BackgroundAnalogJoin")?.Value, out var backgroundAnalogJoin) && backgroundSerialJoin > 0)
            {
                builder.AddProperty(new PropertyElement("BackgroundNumber", backgroundAnalogJoin, builder.SmartJoin, JoinType.Analog) { Description = "Gets/Sets the analog join for background selection." });
            }

            // Projects also potentially have rotated background joins.
            // First dynamic serial join
            if (ushort.TryParse(propertiesElement?.Element("RotatedBackgrounds")?.Element("DynamicGraphicsSerial")?.Value, out var rotatedBackgroundSerialJoin) && rotatedBackgroundSerialJoin > 0)
            {
                builder.AddProperty(new PropertyElement("RotatedBackgroundPath", rotatedBackgroundSerialJoin, builder.SmartJoin, JoinType.Serial) { Description = "Gets/Sets the serial join for the background dynamic graphic path." });
            }
            // Then analog join
            if (ushort.TryParse(propertiesElement?.Element("RotatedBackgrounds")?.Element("BackgroundAnalogJoin")?.Value, out var rotatedBackgroundAnalogJoin) && rotatedBackgroundSerialJoin > 0)
            {
                builder.AddProperty(new PropertyElement("RotatedBackgroundNumber", rotatedBackgroundAnalogJoin, builder.SmartJoin, JoinType.Analog) { Description = "Gets/Sets the analog join for background selection." });
            }
        }

        public static void ParseChildElement(XElement child, ClassBuilder rootBuilder)
        {
            if(rootBuilder == null)
            {
                throw new ArgumentNullException(nameof(rootBuilder));
            }

            if(child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (ushort.TryParse(child.Element("Properties")?.Element("ControlJoin")?.Value, out var result))
            {
                SmartObjectParser.ParseSmartObject(child, rootBuilder);
                return;
            }
            var isSubpageRefItem = rootBuilder.ClassType == ClassType.SrlElement;
            var builder = new ClassBuilder(ClassType.Control) { ClassName = $"{(isSubpageRefItem ? rootBuilder.ClassName : "")}{child.Element("ObjectName")?.Value ?? ""}", NamespaceBase = rootBuilder.NamespaceBase };
            builder.DigitalOffset = rootBuilder.DigitalOffset;
            builder.AnalogOffset = rootBuilder.AnalogOffset;
            builder.SerialOffset = rootBuilder.SerialOffset;
            if (rootBuilder.SmartJoin > 0)
            {
                builder.SmartJoin = rootBuilder.SmartJoin;
            }

            CIPTagParser.ParseCIP(child, builder);

            var controlType = child?.Element("TargetControl").Value.ToUpperInvariant();

            if (child?.Element("Properties")?.Descendants().Count() > 0)
            {
                foreach (var e in child.Element("Properties").Descendants().
                    Where(e => e.Name.LocalName.ToUpperInvariant().Contains("JOIN") || e.Name.LocalName.ToUpperInvariant().Contains("FEEDBACK")))
                {
#pragma warning disable CA1308 // Normalize strings to uppercase
                    var name = e.Name.LocalName.ToLower(CultureInfo.InvariantCulture);
#pragma warning restore CA1308 // Normalize strings to uppercase
                    if (ushort.TryParse(e.Value, out var join) && join > 0)
                    {
                        if (name.StartsWith("digitalpresshigh", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"Raise", join, builder.SmartJoin, JoinType.Digital, true));
                        }
                        else if (name.StartsWith("digitalpresslow", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"Lower", join, builder.SmartJoin, JoinType.Digital, true));
                        }
                        else if (name.StartsWith("digitalpress", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"", join, builder.SmartJoin, JoinType.Digital, true));
                            if (child.Element("Properties")?.Element("ShowSelectFeedback")?.Value == "true")
                            {
                                builder.AddProperty(new PropertyElement($"IsActive", join, builder.SmartJoin, JoinType.Digital));
                            }
                        }
                        else if (name.StartsWith("digitalenable", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"IsEnabled", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.StartsWith("digitalvisibility", StringComparison.InvariantCulture) || name.StartsWith("visibility", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"IsVisible", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.StartsWith("digitalonoff", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"OnOff", join, builder.SmartJoin, JoinType.Digital, false));
                        }
                        else if (name.StartsWith("butoffdigitalpress", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"Off", join, builder.SmartJoin, JoinType.Digital, true));
                            builder.AddProperty(new PropertyElement($"OffIsActive", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.StartsWith("butondigitalpress", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"On", join, builder.SmartJoin, JoinType.Digital, true));
                            builder.AddProperty(new PropertyElement($"OnIsActive", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.StartsWith("checkedstate", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"Toggle", join, builder.SmartJoin, JoinType.Digital, true));
                            builder.AddProperty(new PropertyElement($"IsChecked", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.StartsWith("indirecttext", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"Text", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name.Contains("butonindirecttext"))
                        {
                            builder.AddProperty(new PropertyElement($"OnText", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name.Contains("butofindirecttext"))
                        {
                            builder.AddProperty(new PropertyElement($"OffText", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name.Contains("analogmode"))
                        {
                            builder.AddProperty(new PropertyElement($"Mode", join, builder.SmartJoin, JoinType.Analog));
                        }
                        else if (name == "analogchildpositionjoin")
                        {
                            builder.AddProperty(new PropertyElement($"ChildPosition", join, builder.SmartJoin, JoinType.Analog));
                        }
                        else if (name == "primarylabeljoin")
                        {
                            builder.AddProperty(new PropertyElement($"LeftLabel", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name == "primarychildlabeljoin")
                        {
                            builder.AddProperty(new PropertyElement($"LeftChildLabel", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name == "secondarylabeljoin")
                        {
                            builder.AddProperty(new PropertyElement($"CenterLabel", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name == "secondarychildlabeljoin")
                        {
                            builder.AddProperty(new PropertyElement($"CenterChildLabel", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name == "tertiarylabeljoin")
                        {
                            builder.AddProperty(new PropertyElement($"RightLabel", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name == "tertiarychildlabeljoin")
                        {
                            builder.AddProperty(new PropertyElement($"RightChildLabel", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name.Contains("digitalswipeup"))
                        {
                            builder.AddEvent(new EventElement($"SwipedUp", join, builder.SmartJoin, JoinType.Digital, false));
                        }
                        else if (name.Contains("digitalswipedown"))
                        {
                            builder.AddEvent(new EventElement($"SwipedDown", join, builder.SmartJoin, JoinType.Digital, false));
                        }
                        else if (name.Contains("digitalswipeleft"))
                        {
                            builder.AddEvent(new EventElement($"SwipedLeft", join, builder.SmartJoin, JoinType.Digital, false));
                        }
                        else if (name.Contains("digitalswiperight"))
                        {
                            builder.AddEvent(new EventElement($"SwipedRight", join, builder.SmartJoin, JoinType.Digital, false));
                        }
                        else if (name.StartsWith("analogfeedbackjoin1", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"LowValue", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.Both));
                            //builder.AddEvent(new EventElement("LowValue", join, builder.SmartJoin, JoinType.Analog));
                        }
                        else if (name.StartsWith("analogfeedbackjoin2", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"HighValue", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.Both));
                            //builder.AddEvent(new EventElement("HighValue", join, builder.SmartJoin, JoinType.Analog));
                        }
                        else if (name.StartsWith("analogminvalue", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"MinValue", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                        }
                        else if (name.StartsWith("analogmaxvalue", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"MaxValue", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                        }
                        else if (name.StartsWith("digitalanimation", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"IsAnimating", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.StartsWith("serialmode", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement($"Mode", join, builder.SmartJoin, JoinType.Analog));
                        }
                        else if (name.StartsWith("enterkeypress", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"Enter", join, builder.SmartJoin, JoinType.Digital, true));
                        }
                        else if (name.StartsWith("esckeypress", StringComparison.InvariantCulture))
                        {
                            builder.AddEvent(new EventElement($"Escape", join, builder.SmartJoin, JoinType.Digital, true));
                        }
                        else if (name == "serialjoin")
                        {
                            builder.AddProperty(new PropertyElement($"TextOverride", join, builder.SmartJoin, JoinType.Serial, PropertyMethod.ToPanel, true));
                        }
                        else if (name == "serialindirecttextjoin")
                        {
                            builder.AddProperty(new PropertyElement($"Text", join, builder.SmartJoin, JoinType.Serial));
                        }
                        else if (name == "serialoutputjoin")
                        {
                            builder.AddEvent(new EventElement($"TextChanged", join, builder.SmartJoin, JoinType.Serial, false));
                        }
                        else if (name == "setfocusjoinon")
                        {
                            var focuson = new PropertyElement($"Focus", join, builder.SmartJoin, JoinType.Digital, PropertyMethod.Void)
                            {
                                ContentOverride = $"public void {child.Element("ObjectName").Value}Focus()\n\t{{\n\t\tPulse({join}, 100);"
                            };
                            builder.AddProperty(focuson);
                        }
                        else if (name == "setfocusjoinoff")
                        {
                            var focusoff = new PropertyElement($"Focus", join, builder.SmartJoin, JoinType.Digital, PropertyMethod.Void)
                            {
                                ContentOverride = $"public void {child.Element("ObjectName").Value}Unfocus()\n\t{{\n\t\tPulse({join}, 100);"
                            };
                            builder.AddProperty(focusoff);
                        }
                        else if (name == "hasfocusjoin")
                        {
                            if ((child.Element("Properties")?.Element("SetFocusJoinOn")?.Value == "0" || string.IsNullOrEmpty(child.Element("Properties")?.Element("SetFocusJoinOn")?.Value)) && (child.Element("Properties")?.Element("SetFocusJoinOff")?.Value == "0" || string.IsNullOrEmpty(child.Element("Properties")?.Element("SetFocusJoinOff")?.Value)))
                            {
                                var focus1 = new PropertyElement($"IsFocused", join, builder.SmartJoin, JoinType.Digital, PropertyMethod.Void)
                                {
                                    ContentOverride = $"public void Focus()\n{{\n\tParentPanel.Pulse({(builder.SmartJoin > 0 ? builder.SmartJoin + ", " : "")}{join}, 100);\n}}"
                                };
                                builder.AddProperty(focus1);
                            }
                            builder.AddEvent(new EventElement($"IsFocusedChanged", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name == "prependtextjoin")
                        {
                            builder.AddProperty(new PropertyElement($"PrependText", join, builder.SmartJoin, JoinType.Serial, PropertyMethod.Void));
                        }
                        else if (name == "appendtextjoin")
                        {
                            builder.AddProperty(new PropertyElement($"AppendText", join, builder.SmartJoin, JoinType.Serial, PropertyMethod.Void));
                        }
                        else if (name == "serialgraphicsjoin")
                        {
                            builder.AddProperty(new PropertyElement($"GraphicsPath", join, builder.SmartJoin, JoinType.Serial, PropertyMethod.Both));
                        }
                        else if (name == "serialdynamicgraphicsjoin")
                        {
                            builder.AddProperty(new PropertyElement($"GraphicsPath", join, builder.SmartJoin, JoinType.Serial, PropertyMethod.Both));
                        }
                        else if (name == "serialdynamiciconjoin")
                        {
                            builder.AddProperty(new PropertyElement($"IconName", join, builder.SmartJoin, JoinType.Serial, PropertyMethod.ToPanel));
                        }
                        else if (name == "analogdynamiciconjoin")
                        {
                            builder.AddProperty(new PropertyElement($"IconNumber", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                        }
                        else if (name == "analogredjoin")
                        {
                            if (controlType == "COLOR_CHIP")
                            {
                                builder.AddProperty(new PropertyElement($"Red", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                            }
                            else
                            {
                                builder.AddProperty(new PropertyElement($"Red", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.Both));
                            }
                        }
                        else if (name == "analoggreenjoin")
                        {
                            if (controlType == "COLOR_CHIP")
                            {
                                builder.AddProperty(new PropertyElement($"Green", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                            }
                            else
                            {
                                builder.AddProperty(new PropertyElement($"Green", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.Both));
                            }
                        }
                        else if (name == "analogbluejoin")
                        {
                            if (controlType == "COLOR_CHIP")
                            {
                                builder.AddProperty(new PropertyElement($"Blue", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.ToPanel));
                            }
                            else
                            {
                                builder.AddProperty(new PropertyElement($"Blue", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.Both));
                            }
                        }
                        else if (name.StartsWith("velocity", StringComparison.InvariantCulture))
                        {
                            builder.AddProperty(new PropertyElement(e.Name.LocalName.Replace("Join", ""), join, builder.SmartJoin, JoinType.Analog, PropertyMethod.FromPanel));
                        }
                        else if (name.Contains("analogtouchfeedback"))
                        {
                            builder.AddProperty(new PropertyElement(e.Name.LocalName.Replace("Feedback", ""), join, builder.SmartJoin, JoinType.Analog, PropertyMethod.FromPanel));
                        }
                        else if (name == "holdjoin")
                        {
                            builder.AddEvent(new EventElement("Hold", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name == "clickjoin")
                        {
                            builder.AddEvent(new EventElement("Click", join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name == "negxdigitaljoin" || name == "negydigitaljoin" || name == "posxdigitaljoin" || name == "posydigitaljoin")
                        {
                            var tempName = name == "negxdigitaljoin" ? "GestureLeft" :
                                name == "negydigitaljoin" ? "GestureDown" :
                                name == "posxdigitaljoin" ? "GestureRight" :
                                name == "posydigitaljoin" ? "GestureUp" : "";

                            builder.AddEvent(new EventElement(tempName, join, builder.SmartJoin, JoinType.Digital));
                        }
                        else if (name.Contains("analogfeedback") || name == "analogjoin")
                        {
                            builder.AddProperty(new PropertyElement($"Value", join, builder.SmartJoin, JoinType.Analog, PropertyMethod.Both));
                        }
                    }

                    var useModes = child.Element("ModesOrDynamicGraphics")?.Element("UseModes");
                    if (ushort.TryParse(useModes?.Element("AnalogModeJoin")?.Value, out var modeJoin))
                    {
                        builder.AddProperty(new PropertyElement($"ControlMode", modeJoin, builder.SmartJoin, JoinType.Analog));
                    }
                }
            }
            rootBuilder.AddControl(builder);
        }
    }
}
