using EPS.CodeGen.Builders;
using System;
using System.Xml.Linq;

namespace EPS.Parsers
{
    public static class HardkeyParser
    {
        public static JoinBuilder? ParseElement(XElement hardkeyElement, Options options)
        {
            if(options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (hardkeyElement?.Name.LocalName == "Hardkey")
            {
                if (int.TryParse(hardkeyElement?.Attribute("uid").Value, out var keyNumber) && keyNumber > 0 &&
                    ushort.TryParse(hardkeyElement?.Element("JoinNumber")?.Value, out var joinNumber) && joinNumber > 0)
                {
                    var keys = Options.Current.HardkeyNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (keys.Length > keyNumber - 1)
                    {
                        return new JoinBuilder(joinNumber, 0, $"{keys[keyNumber - 1]}", JoinType.DigitalButton, JoinDirection.FromPanel);
                    }
                    else
                    {
                        return new JoinBuilder(joinNumber, 0, $"{options.HardkeyPrefix}{keyNumber}", JoinType.DigitalButton, JoinDirection.FromPanel);
                    }
                }
            }
            return null;
        }
    }
}
