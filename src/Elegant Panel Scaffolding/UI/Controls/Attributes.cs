using System;

namespace EPS.UI.Controls
{

    [AttributeUsage(AttributeTargets.Property)]
    public class CodeContentGroupAttribute : Attribute
    {
        public string GroupName { get; set; }
        public DetailPart Part { get; set; }
        public string Content { get; set; }

        public CodeContentGroupAttribute(string groupName, DetailPart part, string content)
        {
            this.GroupName = groupName;
            this.Part = part;
            this.Content = content;
        }
    }

    public enum DetailPart
    {
        codeTitle,
        codeContent,
        codeTag
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class OrderAttribute : Attribute
    {
        public double Order { get; }

        public OrderAttribute(double order)
        {
            this.Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FolderPathAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FileTypeAttribute : Attribute
    {
        public string[] ValidExtensions { get; set; }
        public string[] FileNames { get; set; }

        public FileTypeAttribute(string[] fileNames, string[] validExtensions)
        {
            this.ValidExtensions = validExtensions;
            this.FileNames = fileNames;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MaskStringAttribute : Attribute
    {
        public char MaskChar { get; set; }

        public MaskStringAttribute(char maskChar) => this.MaskChar = maskChar;
    }

}
