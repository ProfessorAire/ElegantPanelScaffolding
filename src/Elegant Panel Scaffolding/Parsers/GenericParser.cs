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

        public static JoinBuilder? GetTransitionCompleteProperty(XElement? propertiesElement)
        {
            if (ushort.TryParse(propertiesElement?.Element("TransitionCompleteJoin").Value, out var join) && join > 0)
            {
                return new JoinBuilder(join, "TransitionComplete", JoinType.Digital, JoinDirection.FromPanel);
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
                builder.AddJoin(
                    new JoinBuilder(joinNumber, builder.SmartJoin, "Theme", JoinType.Analog, JoinDirection.Both)
                    {
                        Description = "Gets or sets the theme number used for the project."
                    });
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
                builder.AddJoin(
                    new JoinBuilder(backgroundSerialJoin, builder.SmartJoin, "BackgroundPath", JoinType.Serial, JoinDirection.Both)
                    {
                        Description = "Gets or sets the the background path used for the page or subpage."
                    });
            }
            // Then analog join
            if (ushort.TryParse(propertiesElement?.Element("Backgrounds")?.Element("BackgroundAnalogJoin")?.Value, out var backgroundAnalogJoin) && backgroundSerialJoin > 0)
            {
                builder.AddJoin(
                    new JoinBuilder(backgroundAnalogJoin, builder.SmartJoin, "BackgroundNumber", JoinType.Analog, JoinDirection.Both)
                    {
                        Description = "Gets or sets the analog number used for background selection."
                    });
            }

            // Projects also potentially have rotated background joins.
            // First dynamic serial join
            if (ushort.TryParse(propertiesElement?.Element("RotatedBackgrounds")?.Element("DynamicGraphicsSerial")?.Value, out var rotatedBackgroundSerialJoin) && rotatedBackgroundSerialJoin > 0)
            {
                builder.AddJoin(
                    new JoinBuilder(rotatedBackgroundSerialJoin, builder.SmartJoin, "RotatedBackgroundPath", JoinType.Serial, JoinDirection.Both)
                    {
                        Description = "Gets or sets the the background path used for the rotated page or subpage."
                    });
            }
            // Then analog join
            if (ushort.TryParse(propertiesElement?.Element("RotatedBackgrounds")?.Element("BackgroundAnalogJoin")?.Value, out var rotatedBackgroundAnalogJoin) && rotatedBackgroundSerialJoin > 0)
            {
                builder.AddJoin(
                    new JoinBuilder(rotatedBackgroundAnalogJoin, builder.SmartJoin, "RotatedBackgroundNumber", JoinType.Analog, JoinDirection.Both)
                    {
                        Description = "Gets or sets the analog number used for rotated background selection."
                    });
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
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Raise", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.StartsWith("digitalpresslow", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Lower", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.StartsWith("digitalpress", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(new JoinBuilder(join, builder.SmartJoin, $"", JoinType.DigitalButton, JoinDirection.FromPanel));
                            if (child.Element("Properties")?.Element("ShowSelectFeedback")?.Value == "true")
                            {
                                builder.AddJoin(
                                    new JoinBuilder(join, $"IsActive", JoinType.Digital, JoinDirection.ToPanel));
                            }
                        }
                        else if (name.StartsWith("digitalenable", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IsEnabled", JoinType.Digital, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("digitalvisibility", StringComparison.InvariantCulture) || name.StartsWith("visibility", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IsVisible", JoinType.Digital, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("digitalonoff", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "OnOffToggle", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.StartsWith("butoffdigitalpress", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Off", JoinType.DigitalButton, JoinDirection.FromPanel));
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "OffIsActive", JoinType.Digital, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("butondigitalpress", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "On", JoinType.DigitalButton, JoinDirection.FromPanel));
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "OnIsActive", JoinType.Digital, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("checkedstate", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Toggle", JoinType.DigitalButton, JoinDirection.FromPanel));
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IsChecked", JoinType.Digital, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("indirecttext", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Text", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name.Contains("butonindirecttext"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "OnText", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name.Contains("butofindirecttext"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "OffText", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name.Contains("analogmode"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Mode", JoinType.Analog, JoinDirection.ToPanel));
                        }
                        else if (name == "analogchildpositionjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "ChildPosition", JoinType.Analog, JoinDirection.ToPanel));
                        }
                        else if (name == "primarylabeljoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "LeftLabel", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "primarychildlabeljoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "LeftChildLabel", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "secondarylabeljoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "CenterLabel", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "secondarychildlabeljoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "CenterChildLabel", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "tertiarylabeljoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "RightLabel", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "tertiarychildlabeljoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "RightChildLabel", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name.Contains("digitalswipeup"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "SwipedUp", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.Contains("digitalswipedown"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "SwipedDown", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.Contains("digitalswipeleft"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "SwipedLeft", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.Contains("digitalswiperight"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "SwipedRight", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.StartsWith("analogfeedbackjoin1", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "LowValue", JoinType.Analog, JoinDirection.Both));
                        }
                        else if (name.StartsWith("analogfeedbackjoin2", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "HighValue", JoinType.Analog, JoinDirection.Both));
                        }
                        else if (name.StartsWith("analogminvalue", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "MinValue", JoinType.Analog, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("analogmaxvalue", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "MaxValue", JoinType.Analog, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("digitalanimation", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IsAnimating", JoinType.Digital, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("serialmode", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Mode", JoinType.Analog, JoinDirection.ToPanel));
                        }
                        else if (name.StartsWith("enterkeypress", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Enter", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name.StartsWith("esckeypress", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Escape", JoinType.DigitalButton, JoinDirection.FromPanel));
                        }
                        else if (name == "serialjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "TextOverride", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "serialindirecttextjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Text", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "serialoutputjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Text", JoinType.Serial, JoinDirection.FromPanel));
                        }
                        else if (name == "setfocusjoinon")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "SetFocus", JoinType.DigitalPulse, JoinDirection.ToPanel));

                            //var focuson = new PropertyElement($"Focus", join, builder.SmartJoin, JoinType.Digital, PropertyMethod.Void)
                            //{
                            //    ContentOverride = $"public void {child.Element("ObjectName").Value}Focus()\n\t{{\n\t\tPulse({join}, 100);"
                            //};
                            //builder.AddProperty(focuson);
                        }
                        else if (name == "setfocusjoinoff")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "RemoveFocus", JoinType.DigitalPulse, JoinDirection.ToPanel));

                            //var focusoff = new PropertyElement($"Focus", join, builder.SmartJoin, JoinType.Digital, PropertyMethod.Void)
                            //{
                            //    ContentOverride = $"public void {child.Element("ObjectName").Value}Unfocus()\n\t{{\n\t\tPulse({join}, 100);"
                            //};
                            //builder.AddProperty(focusoff);
                        }
                        else if (name == "hasfocusjoin")
                        {
                            //if ((child.Element("Properties")?.Element("SetFocusJoinOn")?.Value == "0" || string.IsNullOrEmpty(child.Element("Properties")?.Element("SetFocusJoinOn")?.Value)) && (child.Element("Properties")?.Element("SetFocusJoinOff")?.Value == "0" || string.IsNullOrEmpty(child.Element("Properties")?.Element("SetFocusJoinOff")?.Value)))
                            //{
                            //    var focus1 = new PropertyElement($"IsFocused", join, builder.SmartJoin, JoinType.Digital, PropertyMethod.Void)
                            //    {
                            //        ContentOverride = $"public void Focus()\n{{\n\tParentPanel.Pulse({(builder.SmartJoin > 0 ? builder.SmartJoin + ", " : "")}{join}, 100);\n}}"
                            //    };
                            //    builder.AddProperty(focus1);
                            //}
                            //builder.AddEvent(new EventElement($"IsFocusedChanged", join, builder.SmartJoin, JoinType.Digital));

                            var direction = JoinDirection.Both;
                            
                            if ((child.Element("Properties")?.Element("SetFocusJoinOn")?.Value == "0" || string.IsNullOrEmpty(child.Element("Properties")?.Element("SetFocusJoinOn")?.Value)) && (child.Element("Properties")?.Element("SetFocusJoinOff")?.Value == "0" || string.IsNullOrEmpty(child.Element("Properties")?.Element("SetFocusJoinOff")?.Value)))
                            {
                                direction = JoinDirection.FromPanel;
                            }

                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IsFocused", JoinType.Digital, direction));
                        }
                        else if (name == "prependtextjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "PrependText", JoinType.SerialSet, JoinDirection.ToPanel));
                        }
                        else if (name == "appendtextjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "AppendText", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "serialgraphicsjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "GraphicsPath", JoinType.Serial, JoinDirection.Both));
                        }
                        else if (name == "serialdynamicgraphicsjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "GraphicsPath", JoinType.Serial, JoinDirection.Both));
                        }
                        else if (name == "serialdynamiciconjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IconName", JoinType.Serial, JoinDirection.ToPanel));
                        }
                        else if (name == "analogdynamiciconjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "IconNumber", JoinType.Analog, JoinDirection.ToPanel));
                        }
                        else if (name == "analogredjoin")
                        {
                            if (controlType == "COLOR_CHIP")
                            {
                                builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Red", JoinType.Analog, JoinDirection.ToPanel));
                            }
                            else
                            {
                                builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Red", JoinType.Analog, JoinDirection.Both));
                            }
                        }
                        else if (name == "analoggreenjoin")
                        {
                            if (controlType == "COLOR_CHIP")
                            {
                                builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Green", JoinType.Analog, JoinDirection.ToPanel));
                            }
                            else
                            {
                                builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Green", JoinType.Analog, JoinDirection.Both));
                            }
                        }
                        else if (name == "analogbluejoin")
                        {
                            if (controlType == "COLOR_CHIP")
                            {
                                builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Blue", JoinType.Analog, JoinDirection.ToPanel));
                            }
                            else
                            {
                                builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Blue", JoinType.Analog, JoinDirection.Both));
                            }
                        }
                        else if (name.StartsWith("velocity", StringComparison.InvariantCulture))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, e.Name.LocalName.Replace("Join", ""), JoinType.Analog, JoinDirection.FromPanel));
                        }
                        else if (name.Contains("analogtouchfeedback"))
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, e.Name.LocalName.Replace("Feedback", ""), JoinType.Analog, JoinDirection.FromPanel));
                        }
                        else if (name == "holdjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Hold", JoinType.Digital, JoinDirection.FromPanel));
                        }
                        else if (name == "clickjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Click", JoinType.Digital, JoinDirection.FromPanel));
                        }
                        else if (name == "negxdigitaljoin" || name == "negydigitaljoin" || name == "posxdigitaljoin" || name == "posydigitaljoin")
                        {
                            var tempName = name == "negxdigitaljoin" ? "GestureLeft" :
                                name == "negydigitaljoin" ? "GestureDown" :
                                name == "posxdigitaljoin" ? "GestureRight" :
                                name == "posydigitaljoin" ? "GestureUp" : "";

                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, tempName, JoinType.Digital, JoinDirection.FromPanel));
                        }
                        else if (name.Contains("analogfeedback") || name == "analogjoin")
                        {
                            builder.AddJoin(
                                new JoinBuilder(join, builder.SmartJoin, "Value", JoinType.Analog, JoinDirection.Both));
                        }
                    }

                    var useModes = child.Element("ModesOrDynamicGraphics")?.Element("UseModes");
                    if (ushort.TryParse(useModes?.Element("AnalogModeJoin")?.Value, out var modeJoin))
                    {
                        builder.AddProperty(new PropertyElement($"ControlMode", modeJoin, builder.SmartJoin, JoinType.Analog));
                        builder.AddJoin(
                                new JoinBuilder(modeJoin, builder.SmartJoin, "ControlMode", JoinType.Analog, JoinDirection.ToPanel));
                    }
                }
            }

            rootBuilder.AddControl(builder);
        }
    }
}
