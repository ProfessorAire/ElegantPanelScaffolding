using EPS.CodeGen.Writers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace EPS.CodeGen.Builders
{
    /// <summary>
    /// Used for all joins, to create writers for code generation.
    /// </summary>
    public class JoinBuilder
    {
        /// <summary>
        /// Gets or sets the number that the join uses.
        /// </summary>
        public uint JoinNumber { get; set; }

        /// <summary>
        /// Gets or sets the number for the SmartObject join #, if the join belongs to a SmartObject.
        /// </summary>
        public uint SmartJoinNumber { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="JoinType"/> associated with the join.
        /// </summary>
        public JoinType JoinType { get; set; } = JoinType.None;

        /// <summary>
        /// Gets or sets the <see cref="JoinDirection"/> the join uses.
        /// </summary>
        public JoinDirection JoinDirection { get; set; } = JoinDirection.Unknown;

        /// <summary>
        /// Gets or sets the <see cref="JoinMethod"/> the join uses. Defaults to <see cref="JoinMethod.Property"/>.
        /// </summary>
        public JoinMethod JoinMethod { get; set; } = JoinMethod.Property;

        /// <summary>
        /// Gets or sets the base name of the join.
        /// </summary>
        public string JoinName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the join's change event. If an empty string this defaults to $"{JoinName}Changed"
        /// </summary>
        public string ChangeEventName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets content to override the builder using. If this is not empty it's the only text that will get output.
        /// </summary>
        public string ContentOverride { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a description to use for help text in certain cases.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the join belongs to a list element that uses offset numbers.
        /// </summary>
        public bool IsListElement { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinBuilder"/> class.
        /// </summary>
        /// <param name="joinNumber">The join's number.</param>
        /// <param name="joinName">The join's name.</param>
        /// <param name="joinType">The join's type.</param>
        /// <param name="joinDirection">The join's direction.</param>
        /// <param name="joinMethod">The join's interaction method.</param>
        public JoinBuilder(uint joinNumber, string joinName, JoinType joinType, JoinDirection joinDirection, JoinMethod joinMethod)
        {
            JoinNumber = joinNumber;
            JoinName = joinName;
            JoinType = joinType;
            JoinDirection = joinDirection;
            JoinMethod = joinMethod;
        }

        public List<TextWriter> GetInitializers()
        {

        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        public List<WriterBase> GetWriters()
        {
            if (!string.IsNullOrEmpty(ContentOverride))
            {
                var tw = new TextWriter(ContentOverride);
                tw.Help.Summary = Description;
                return new List<WriterBase>() { tw };
            }

            if (JoinNumber == 0 || JoinType == JoinType.None)
            {
                return new List<WriterBase>(0);
            }

            switch (JoinType)
            {
                case JoinType.Analog:
                case JoinType.SmartAnalog:
                    return GetAnalogWriters();
                case JoinType.Digital:
                case JoinType.SmartDigital:
                    return GetDigitalWriters();
                case JoinType.DigitalButton:
                case JoinType.SmartDigitalButton:
                    return GetDigitalButtonWriters();
                case JoinType.Serial:
                case JoinType.SmartSerial:
                    return GetSerialWriters();
                case JoinType.SrlEnable:
                case JoinType.SrlVisibility:
                    return GetSpecialtyWriters();
            }
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the analog join data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        private List<WriterBase> GetAnalogWriters()
        {
            var sigType = "ushort";
            var sigTypeName = "UShort";
            var offsetText = IsListElement ? " + analogOffset" : "";

            var result = new List<WriterBase>();

            if (JoinDirection == JoinDirection.ToPanel)
            {
                result.AddRange(GetWritersToPanel(sigType, sigTypeName, offsetText));
            }

            return result;
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the digital data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        private List<WriterBase> GetDigitalWriters()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the digital button data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        private List<WriterBase> GetDigitalButtonWriters()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the serial data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        private List<WriterBase> GetSerialWriters()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the specialty data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        private List<WriterBase> GetSpecialtyWriters()
        {
            throw new NotImplementedException();
        }

        private List<WriterBase> GetWritersToPanel(string sigType, string sigTypeName, string offsetText)
        {
            var result = new List<WriterBase>();

            var args = $"{sigTypeName}ValueChangedEventArgs";
            var propertyName = FormatPropertyName(JoinName);
            var fieldName = FormatFieldName(JoinName);

            // First create the EventWriter.
            // This handles change event notifications, which are triggered when the value going to the panel is changed.
            var ew = new EventWriter($"{ChangeEventName}")
            {
                Handler = $"EventHandler<{args}>"
            };

            if (string.IsNullOrEmpty(ChangeEventName))
            {
                ew.Name = $"{propertyName}Changed";
            }

            ew.Help.Summary = $"Raised when the {propertyName} value changes.";
            
            result.Add(ew);

            // Next create the property and backing field writers.
            var pw = new PropertyWriter(propertyName, sigType);
            var fw = new FieldWriter(fieldName, sigType);

            if (!string.IsNullOrEmpty(Description))
            {
                pw.Help.Summary = Description;
            }
            else
            {
                if (sigType == "bool")
                {
                    pw.Help.Summary = $"Gets or sets a value indicating whether the <see cref=\"{propertyName}\"/> join was last set to true or false.";
                }
                else
                {
                    pw.Help.Summary = $"Gets or sets a value indicating what the <see cref=\"{propertyName}\"/> join was last set to.";
                }
            }

            fw.Help.Summary = $"Backing field for the <see cref=\"{propertyName}\"/> property.";

            pw.Getter.Add($"return {fieldName};");

            if(fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            pw.Setter.Add($"{fieldName} = value;");

            var smartSuffix = SmartJoinNumber > 0 ? "Smart" : string.Empty;
            var smartValue = SmartJoinNumber > 0 ? $"{SmartJoinNumber}, " : string.Empty;

            pw.Setter.Add($"{fieldName} = value;");
            pw.Setter.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value);");

            result.Add(pw);
            result.Add(fw);

            var methodSetter = new MethodWriter($"Set{propertyName}", $"Sets the value of the <see cref=\"{propertyName}\"/> join on a single touchpanel.");
            methodSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
            methodSetter.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated join value on.");
            methodSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value, panel);");
            methodSetter.MethodLines.Add($"if ({propertyName}Changed != null)");
            methodSetter.MethodLines.Add("{");
            methodSetter.MethodLines.Add($"{propertyName}Changed(this, new {args}(value));");
            methodSetter.MethodLines.Add("}");

            if (JoinType == JoinType.Digital && 
                (JoinDirection == JoinDirection.ToPanel || JoinDirection == JoinDirection.Both))
            {
                var pulseMw = new MethodWriter($"Pulse{propertyName}", $"Pulses the {propertyName} digital signal. Any local signal changed events won't be fired by this method.");
                pulseMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                pulseMw.MethodLines.Add($"ParentPanel.Pulse({smartSuffix}(uint)({JoinNumber}{offsetText}), duration);");
                result.Add(pulseMw);
            }
            return result;
        }

        /// <summary>
        /// Formats a string with an uppercase first letter.
        /// </summary>
        /// <param name="name">The name to normalize as a property name.</param>
        /// <returns>A string valid to use as a field name.</returns>
        private static string FormatPropertyName(string name)
        {
            return SanitizeString($"{name.ToUpperInvariant()[0]}{name.Substring(1)}");
        }

        /// <summary>
        /// Formats a string with a lowercase first letter.
        /// </summary>
        /// <param name="name">The name to normalize as a fieldName.</param>
        /// <returns>A string valid to use as a field name.</returns>
        private static string FormatFieldName(string name)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            return SanitizeString($"{name.ToLower(CultureInfo.InvariantCulture)[0]}{name.Substring(1)}");
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        /// <summary>
        /// Sanitizes a string to ensure it's a valid value.
        /// </summary>
        /// <param name="value">The string to sanitize.</param>
        /// <returns>A sanitized string.</returns>
        private static string SanitizeString(string value)
        {
            return value
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("*", "Star")
                .Replace("#", "Pound")
                .Replace("!", "ExMark")
                .Replace("@", "AtSign")
                .Replace("$", "Dollar")
                .Replace("%", "Percent")
                .Replace("^", "Carat")
                .Replace("&", "Ampersand")
                .Replace(".", "Dot")
                .Replace("-", "Minus")
                .Replace("=", "Equals")
                .Replace("+", "Plus");
        }
    }
}
