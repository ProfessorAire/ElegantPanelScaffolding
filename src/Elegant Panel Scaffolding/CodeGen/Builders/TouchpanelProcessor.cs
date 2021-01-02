using EPS.Parsers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EPS.CodeGen.Builders
{
    public static class TouchpanelProcessor
    {

        public static ClassBuilder TouchpanelCore { get; set; } = new ClassBuilder(ClassType.Touchpanel);

        public static async Task<ClassBuilder?> ProcessFileAsync(Options options)
        {
            if(options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var fileName = options.ApplicationTouchpanelPath;
            return await Task.Run(async () =>
            {
                TouchpanelCore = new ClassBuilder(ClassType.Touchpanel);
                if ((!fileName.EndsWith(".vtz", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith("Environment.xml", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith(".c3p", StringComparison.OrdinalIgnoreCase)
                    ) || !System.IO.File.Exists(fileName))
                {
                    return null;
                }

                var contents = "";
                if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    contents = System.IO.File.ReadAllText(fileName);

                }
                else
                {
                    var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(fileName);
                    if (zip.FindEntry("swf/Environment.xml", true) > -1)
                    {
                        var entry = zip.GetEntry("swf/Environment.xml");
                        using (var reader = new System.IO.StreamReader(zip.GetInputStream(entry)))
                        {
                            contents = await reader.ReadToEndAsync();
                        }
                    }
                    else if (zip.FindEntry("Environment.xml", true) > -1)
                    {
                        var entry = zip.GetEntry("Environment.xml");
                        using (var reader = new System.IO.StreamReader(zip.GetInputStream(entry)))
                        {
                            contents = await reader.ReadToEndAsync();
                        }
                    }
                }

                if (string.IsNullOrEmpty(contents)) { return null; }


                var doc = XDocument.Parse(contents);

                TouchpanelCore.ClassName = doc?.Root?.Element("ObjectName")?.Value ?? "";
                TouchpanelCore.Namespace = options.RootNamespace;
                options.PanelNamespace = $"{options.RootNamespace}.{TouchpanelCore.ClassName}";

                // Projects potentially have a theme join.
                GenericParser.ParseTheme(doc?.Root?.Element("Properties"), TouchpanelCore);

                // Try to parse for background joins as well.
                GenericParser.ParseBackgroundJoins(doc?.Root?.Element("Properties"), TouchpanelCore);

                // Then we have to determine if there are hardkeys in play.
                if (options.ParseHardkeys)
                {
                    if (int.TryParse(doc?.Root?.Element("Properties")?.Element("Hardkeys")?.Element("NumHardkeys")?.Value, out var count) && count > 0)
                    {
                        var hardkeys = from hk in doc?.Root?.Element("Properties")?.Element("Hardkeys")?.Descendants()
                                       where hk?.Name == "Hardkey"
                                       select hk;
                        if (hardkeys.Any())
                        {
                            foreach (var hk in hardkeys)
                            {
                                var key = HardkeyParser.ParseElement(hk, options);
                                if (key != null)
                                {
                                    TouchpanelCore.AddJoin(key);
                                }
                            }
                        }
                    }
                }

                var pages = from p in doc?.Root?.Element("Properties")?.Element("Pages")?.Descendants()
                            where p?.Element("TargetControl")?.Value == "Page"
                            select p;
                var subpages = from sp in doc?.Root?.Element("Properties")?.Element("Pages")?.Descendants()
                               where sp?.Element("TargetControl")?.Value == "Subpage"
                               select sp;

                foreach (var page in pages)
                {
                    var pageBuilder = new ClassBuilder(ClassType.Page)
                    {
                        ClassName = page?.Element("ObjectName")?.Value ?? "",
                        NamespaceBase = $"{options.PanelNamespace}.Components"
                    };

                    if (pageBuilder.ClassName == "Subpage Reference" || pageBuilder.ClassName == "Page")
                    {
                        pageBuilder.ClassName = page?.Attribute("Name")?.Value ?? "";
                    }

                    if (string.IsNullOrEmpty(pageBuilder.ClassName))
                    {
                        throw new NullReferenceException("Unable to determine a name for a page or subpage reference.");
                    }

                    var props = page?.Element("Properties");

                    if (ushort.TryParse(props?.Element("DigitalJoin").Value, out var pageJoin) && pageJoin > 0)
                    {
                        pageBuilder.AddJoin(new JoinBuilder(pageJoin, pageBuilder.SmartJoin, "IsVisible", JoinType.Digital, JoinDirection.ToPanel));
                    }

                    if (props != null)
                    {
                        pageBuilder.DigitalOffset = GenericParser.ParseDigitalOffset(props);
                        pageBuilder.AnalogOffset = GenericParser.ParseAnalogOffset(props);
                        pageBuilder.SerialOffset = GenericParser.ParseSerialOffset(props);

                        GenericParser.ParseBackgroundJoins(props, pageBuilder);

                        var transitionJoin = GenericParser.GetTransitionCompleteJoin(props);
                        if(transitionJoin != null)
                        {
                            pageBuilder.AddJoin(transitionJoin);
                        }
                    }

                    var pageChildren = props?.Element("Children")?.Elements()?.Where(e => e?.Name.LocalName != "Subpage");
                    if (pageChildren != null)
                    {
                        foreach (var c in pageChildren)
                        {
                            GenericParser.ParseChildElement(c, pageBuilder);
                        }
                    }

                    var subpageCount = props?.Element("Children")?.Elements().Where(e => e.Name == "Subpage").Count();
                    if (subpageCount > 0)
                    {
                        foreach (var sp in page?.Element("Properties")?.Element("Children")?.Elements()?.Where(e => e.Name == "Subpage") ?? Array.Empty<XElement>())
                        {
                            var subElement = subpages.Where(sub => sub.Attribute("uid")?.Value == sp.Element("Properties")?.Element("PageID")?.Value).FirstOrDefault();
                            if (subElement == null) { continue; }
                            var subBuilder = new ClassBuilder(ClassType.Page)
                            {
                                ClassName = sp?.Element("ObjectName")?.Value ?? "",
                                Namespace = pageBuilder.Namespace
                            };

                            if (subBuilder.ClassName == "Subpage Reference" || subBuilder.ClassName == "SubpageReference" || subBuilder.ClassName == "Page")
                            {
                                var rootSubpage = subpages.Where(s => (s.Attribute("uid")?.Value ?? "null1") == (sp?.Attribute("uid").Value ?? "null2")).FirstOrDefault();
                                subBuilder.ClassName = rootSubpage?.Attribute("Name")?.Value ?? "";
                            }

                            if (string.IsNullOrEmpty(subBuilder.ClassName))
                            {
                                throw new NullReferenceException("Unable to determine a name for a page or subpage reference.");
                            }

                            var subProps = sp?.Element("Properties");

                            if (subProps != null)
                            {
                                if (ushort.TryParse(subProps?.Element("DigitalJoin").Value, out var subpageJoin) && subpageJoin > 0)
                                {
                                    subBuilder.AddJoin(new JoinBuilder(subpageJoin, subBuilder.SmartJoin, "IsVisible", JoinType.Digital, JoinDirection.ToPanel));
                                }

                                subBuilder.DigitalOffset = GenericParser.ParseDigitalOffset(subProps);
                                subBuilder.AnalogOffset = GenericParser.ParseAnalogOffset(subProps);
                                subBuilder.SerialOffset = GenericParser.ParseSerialOffset(subProps);

                                var transitionJoin = GenericParser.GetTransitionCompleteJoin(subProps);

                                if (transitionJoin != null)
                                {
                                    subBuilder.AddJoin(transitionJoin);
                                }
                            }

                            subProps = subElement.Element("Properties");

                            if (subProps != null)
                            {
                                GenericParser.ParseBackgroundJoins(subProps, subBuilder);
                                if (subProps.Element("Children")?.Elements()?.Count() > 0)
                                {
                                    foreach (var c in subProps.Element("Children").Elements().Where(e => e.Name.LocalName != "Subpage"))
                                    {
                                        GenericParser.ParseChildElement(c, subBuilder);
                                    }
                                }
                            }

                            if (subBuilder.IsValid)
                            {
                                pageBuilder.AddPage(subBuilder);
                            }
                        }
                    }

                    TouchpanelCore.AddPage(pageBuilder);
                }

                return TouchpanelCore;
            });
        }
    }


}
