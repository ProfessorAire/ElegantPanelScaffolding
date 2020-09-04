namespace EPS.CodeGen
{
    internal static class Extensions
    {
        /// <summary>
        /// Gets a string of tabs as long as the indentLevel.
        /// </summary>
        /// <param name="indentLevel">The number of tabs to return.</param>
        /// <returns>A string of tabs.</returns>
        public static string GetTabs(this int indentLevel)
        {
            var tabs = "";
            for (var i = 1; i <= indentLevel; i++)
            {
                tabs += "\t";
            }
            return tabs;
        }

        public static string GetTextValue(this Modifier mod)
        {
            return mod switch
            {
                Modifier.Override => "override ",
                Modifier.Partial => "partial ",
                Modifier.Abstract => "abstract ",
                Modifier.New => "new ",
                Modifier.ReadOnly => "readonly ",
                _ => "",
            };
        }

        public static string GetTextValue(this Accessor acc)
        {
            return acc switch
            {
                Accessor.Public => "public ",
                Accessor.Private => "private ",
                Accessor.Protected => "protected ",
                Accessor.Internal => "internal ",
                _ => "",
            };
        }
    }
}
