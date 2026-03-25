using System.Linq;
using System.Xml.Linq;
using EPS.CodeGen.Builders;

namespace EPS.Parsers
{
    internal class TabButtonParser
    {
        public static void ParseTabButtonControl(XElement child, ClassBuilder rootBuilder)
        {
            if (child == null || rootBuilder == null)
            {
                return;
            }

            var props = child?.Element("Properties");
            if (props == null)
            {
                return;
            }

            if (!ushort.TryParse(props?.Element("Tabs")?.Element("TabCount")?.Value ?? "0", out var tabCount) || tabCount == 0)
            {
                return;
            }

            var tabs = props?.Element("Tabs")?.Elements("Tab");
            if (tabs == null || !tabs.Any())
            {
                return;
            }

            var firstTab = tabs.FirstOrDefault();
            if (firstTab == null)
            {
                return;
            }

            _ = ushort.TryParse(firstTab?.Element("DigitalPressJoin")?.Element("JoinNumber")?.Value ?? "0", out var firstPressJoin);
            _ = ushort.TryParse(firstTab?.Element("DigitalSelectJoin")?.Element("JoinNumber")?.Value ?? "0", out var firstSelectJoin);

            var digitalIncrement = (ushort)0;
            if (tabs.Count() > 1)
            {
                var secondTab = tabs.Skip(1).FirstOrDefault();
                if (secondTab != null)
                {
                    _ = ushort.TryParse(secondTab?.Element("DigitalPressJoin")?.Element("JoinNumber")?.Value ?? "0", out var secondPressJoin);
                    if (secondPressJoin > firstPressJoin)
                    {
                        digitalIncrement = (ushort)(secondPressJoin - firstPressJoin);
                    }
                }
            }

            var tabBuilder = new ClassBuilder(ClassType.SrlElement)
            {
                ClassName = $"{rootBuilder.ClassName}Item",
                Namespace = rootBuilder.Namespace,
                SmartJoin = rootBuilder.SmartJoin
            };

            var pressSignalName = SanitizeSignalName("Press");
            var selectSignalName = SanitizeSignalName("Selected");

            tabBuilder.AddJoin(new JoinBuilder(1, tabBuilder.SmartJoin, pressSignalName, JoinType.SmartDigitalButton, JoinDirection.FromPanel));
            tabBuilder.AddJoin(new JoinBuilder(2, tabBuilder.SmartJoin, selectSignalName, JoinType.SmartDigital, JoinDirection.ToPanel));

            tabBuilder.DigitalOffset = (ushort)(firstPressJoin - 1);
            tabBuilder.AnalogOffset = 0;
            tabBuilder.SerialOffset = 0;

            var list = new ListBuilder(tabBuilder, tabCount, digitalIncrement, 0, 0)
            {
                Name = props?.Parent?.Element("ObjectName")?.Value ?? "Tabs"
            };

            rootBuilder.AddList(list);
        }

        private static string SanitizeSignalName(string signalName)
        {
            if (string.IsNullOrEmpty(signalName))
            {
                return signalName;
            }

            signalName = signalName.Replace(" ", string.Empty)
                .Replace("%i", string.Empty)
                .Replace("-", string.Empty)
                .Replace("#", "Pound")
                .Replace("*", "Star")
                .Replace("%", string.Empty)
                .Replace("@", string.Empty)
                .Replace("!", string.Empty)
                .Replace("^", string.Empty)
                .Replace("&", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace("OK", "Ok");

            if (signalName == "SetNumofItems")
            {
                signalName = "NumberOfItems";
            }
            else if (signalName == "SetNumberOfItems")
            {
                signalName = "NumberOfItems";
            }
            else if (char.IsDigit(signalName[0]))
            {
                if (signalName[0] == '1')
                {
                    signalName = signalName.Trim('1').Insert(0, "One");
                }
                else if (signalName[0] == '2')
                {
                    signalName = signalName.Trim('2').Insert(0, "Two");
                }
                else if (signalName[0] == '3')
                {
                    signalName = signalName.Trim('3').Insert(0, "Three");
                }
                else if (signalName[0] == '4')
                {
                    signalName = signalName.Trim('4').Insert(0, "Four");
                }
                else if (signalName[0] == '5')
                {
                    signalName = signalName.Trim('5').Insert(0, "Five");
                }
                else if (signalName[0] == '6')
                {
                    signalName = signalName.Trim('6').Insert(0, "Six");
                }
                else if (signalName[0] == '7')
                {
                    signalName = signalName.Trim('7').Insert(0, "Seven");
                }
                else if (signalName[0] == '8')
                {
                    signalName = signalName.Trim('8').Insert(0, "Eight");
                }
                else if (signalName[0] == '9')
                {
                    signalName = signalName.Trim('9').Insert(0, "Nine");
                }
                else if (signalName[0] == '0')
                {
                    signalName = signalName.Trim('0').Insert(0, "Zero");
                }
            }

            return signalName;
        }
    }
}
