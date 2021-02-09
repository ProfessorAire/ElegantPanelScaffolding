using EPS.CodeGen.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EPS.Parsers
{
    internal class CIPTagParser
    {
        private static readonly Regex standardRegex = new Regex("<CIP(?<type>[ASD])>\\D{0,2}(?<join>\\d+)[?:].*?(?:<\\/CIP\\1>)", RegexOptions.Compiled);

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
                MatchCipTags(label, builder, labels.Count);
            }
        }

        private static void MatchCipTags(XElement? element, ClassBuilder? builder, int? quantity)
        {
            if (element == null || builder == null || quantity == null)
            {
                return;
            }

            var result = standardRegex.Matches(element.Value.ToUpperInvariant());

            var digitalCount = 0;
            var analogCount = 0;
            var serialCount = 0;

            for(var i = 0; i < result.Count; i++)
            {
                if (result[i].Success)
                {
                    try
                    {
                        var type = result[i].Groups["type"].Value;

                        var tag = string.Empty;
                        var count = 0;

                        if (type.ToUpperInvariant() == "A")
                        {
                            analogCount++;
                            count = analogCount;
                            tag = "UShort";
                        }
                        else if (type.ToUpperInvariant() == "D")
                        {
                            digitalCount++;
                            count = digitalCount;
                            tag = "Boolean";
                        }
                        else if (type.ToUpperInvariant() == "S")
                        {
                            serialCount++;
                            count = serialCount;
                            tag = "String";
                        }

                        var join = Convert.ToUInt16(result[i].Groups["join"].Value, System.Globalization.CultureInfo.InvariantCulture);

                        builder.AddJoin(
                            new JoinBuilder(
                                join,
                                builder.SmartJoin,
                                $"{tag}{count}",
                                tag == "UShort" ? JoinType.Analog :
                                tag == "Boolean" ? JoinType.Digital :
                                tag == "String" ? JoinType.Serial : JoinType.None,
                                JoinDirection.ToPanel));
                    }
                    catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception encountered while parsing CIP tag object.");
                    }
                }
            }
        }
    }
}
