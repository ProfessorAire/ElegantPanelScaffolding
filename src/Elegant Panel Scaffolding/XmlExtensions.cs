using System.Xml.Linq;

namespace EPS
{
    public static class XmlExtensions
    {
        public static string GetParentObjectName(this XElement element)
        {
            while (element != null)
            {
                if (element?.Name == "Child" || element?.Name == "Subpage" || element?.Name == "Page")
                {
                    var name = element.Element("ObjectName")?.Value;

                    if (string.IsNullOrWhiteSpace(name) || name == null)
                    {
                        return "Unknown";
                    }

                    return name;
                }

                if (element != null)
                {
                    element = element.Parent;
                }
            }

            return "Unknown";
        }
    }
}
