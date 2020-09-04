using System.Xml.Linq;

namespace EPS.Parsers
{
    public abstract class ControlParserBase
    {
        public abstract string ParseElement(XElement element);
    }
}
