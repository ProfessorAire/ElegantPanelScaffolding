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
        /// Backing field for the <see cref="ChangeEventName"/> property.
        /// </summary>
        private string changeEventName = string.Empty;

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
        /// Gets or sets the base name of the join.
        /// </summary>
        public string JoinName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the join's change event. If an empty string this defaults to $"{JoinName}Changed"
        /// </summary>
        public string ChangeEventName
        {
            get => string.IsNullOrWhiteSpace(changeEventName) ? $"{FormatPropertyName(JoinName)}Changed" : changeEventName;
            set => changeEventName = value;
        }

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
        public JoinBuilder(uint joinNumber, string joinName, JoinType joinType, JoinDirection joinDirection)
            : this(joinNumber, 0, joinName, joinType, joinDirection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinBuilder"/> class.
        /// </summary>
        /// <param name="joinNumber">The join's number.</param>
        /// <param name="smartId">The ID of the SmartObject the join belongs to.</param>
        /// <param name="joinName">The join's name.</param>
        /// <param name="joinType">The join's type.</param>
        /// <param name="joinDirection">The join's direction.</param>
        /// <param name="joinMethod">The join's interaction method.</param>
        public JoinBuilder(uint joinNumber, uint smartId, string joinName, JoinType joinType, JoinDirection joinDirection)
        {
            JoinNumber = joinNumber;
            SmartJoinNumber = smartId;
            JoinName = joinName;
            JoinType = joinType;
            JoinDirection = joinDirection;
        }

        /// <summary>
        /// Gets a TextWriter that writes data into a class' constructor for initializing actions.
        /// </summary>
        /// <returns></returns>
        public TextWriter GetInitializers()
        {
            var writer = new TextWriter();

            if (JoinDirection == JoinDirection.FromPanel || JoinDirection == JoinDirection.Both)
            {
                var raiseMethod = $"Raise{ChangeEventName}";

                var joinType = JoinType;

                if (SmartJoinNumber > 0)
                {
                    if (joinType == JoinType.Digital)
                    {
                        joinType = JoinType.SmartDigital;
                    }
                    else if (joinType == JoinType.DigitalButton)
                    {
                        joinType = JoinType.SmartDigitalButton;
                    }
                    else if (joinType == JoinType.Analog)
                    {
                        joinType = JoinType.SmartAnalog;
                    }
                    else if (joinType == JoinType.Serial)
                    {
                        joinType = JoinType.SmartSerial;
                    }
                }

                switch (joinType)
                {
                    case JoinType.Digital:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({JoinNumber}, (value) => {raiseMethod}(this, new BooleanValueChangedEventArgs(value)));");
                        break;
                    case JoinType.DigitalButton:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({JoinNumber}, (value) => RaisePressed(this, new BooleanValueChangedEventArgs(value)), true);");
                        writer.Text.Add($"ParentPanel.Actions.AddBool({JoinNumber}, (value) => RaiseReleased(this, new BooleanValueChangedEventArgs(value)), false);");
                        break;
                    case JoinType.Analog:
                        writer.Text.Add($"ParentPanel.Actions.AddUShort({JoinNumber}, (value) => {raiseMethod}(this, new UShortValueChangedEventArgs(value)));");
                        break;
                    case JoinType.Serial:
                        writer.Text.Add($"ParentPanel.Actions.AddString({JoinNumber}, (value) => {raiseMethod}(this, new StringValueChangedEventArgs(value)));");
                        break;
                    case JoinType.SmartDigital:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({JoinNumber}, {SmartJoinNumber}, (value) => {raiseMethod}(this, new BooleanValueChangedEventArgs(value)));");
                        break;
                    case JoinType.SmartDigitalButton:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({JoinNumber}, {SmartJoinNumber}, (value) => RaisePressed(this, new BooleanValueChangedEventArgs(value)), true);");
                        writer.Text.Add($"ParentPanel.Actions.AddBool({JoinNumber}, {SmartJoinNumber}, (value) => RaiseReleased(this, new BooleanValueChangedEventArgs(value)), false);");
                        break;
                    case JoinType.SmartAnalog:
                        writer.Text.Add($"ParentPanel.Actions.AddUShort({JoinNumber}, {SmartJoinNumber}, (value) => {raiseMethod}(this, new UShortValueChangedEventArgs(value)));");
                        break;
                    case JoinType.SmartSerial:
                        writer.Text.Add($"ParentPanel.Actions.AddString({JoinNumber}, {SmartJoinNumber}, (value) => {raiseMethod}(this, new StringValueChangedEventArgs(value)));");
                        break;
                }
            }

            return writer;
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        public List<WriterBase> GetWriters()
        {
            if (JoinNumber == 0 || JoinType == JoinType.None)
            {
                return new List<WriterBase>(0);
            }

            if (JoinDirection == JoinDirection.ToPanel)
            {
                return GetWritersToPanel();
            }
            else if (JoinDirection == JoinDirection.FromPanel)
            {
                return GetWritersFromPanel();
            }
            else if (JoinDirection == JoinDirection.Both)
            {
                return GetWritersForBoth();
            }

            return new List<WriterBase>(0);
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects configured for To panel msesaging.
        /// </summary>
        /// <returns></returns>
        private List<WriterBase> GetWritersToPanel()
        {
            var result = new List<WriterBase>();

            var sigType = GetJoinTypeString();
            var sigTypeName = GetJoinTypeNameString();
            var offsetText = GetOffsetString();


            var args = $"{sigTypeName}ValueChangedEventArgs";
            var propertyName = FormatPropertyName(JoinName);
            var fieldName = FormatFieldName(JoinName);
            var smartSuffix = SmartJoinNumber > 0 ? "Smart" : string.Empty;
            var smartValue = SmartJoinNumber > 0 ? $"{SmartJoinNumber}, " : string.Empty;

            var changeEventName = ChangeEventName;

            if (JoinType == JoinType.DigitalPulse || JoinType == JoinType.AnalogSet || JoinType == JoinType.SerialSet)
            {
                var prefix = JoinType == JoinType.DigitalPulse ? "Latch" : "";
                var singleSetter = new MethodWriter($"{prefix}{propertyName}", $"Sends the value to a single touchpanel.");
                singleSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                singleSetter.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated join value on.");
                singleSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value, panel);");

                result.Add(singleSetter);

                var allSetter = new MethodWriter($"{prefix}{propertyName}", $"Sends the value to all touchpanels.");
                allSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                allSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value);");

                result.Add(allSetter);

                if (JoinType != JoinType.DigitalPulse)
                {
                    var pulseMw = new MethodWriter($"{propertyName}", $"Pulses the {propertyName} digital signal.");
                    pulseMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                    pulseMw.MethodLines.Add($"ParentPanel.Pulse({smartValue}(uint)({JoinNumber}{offsetText}), duration);");

                    result.Add(pulseMw);
                }
            }
            else
            {
                // First create the EventWriter.
                // This handles change event notifications, which are triggered when the value going to the panel is changed.
                result.Add(GetEventWriter());

                // Next create the property and backing field writers.
                var pw = GetPropertyWriter();
                var fw = GetFieldWriter();

                pw.Getter.Add($"return {fieldName};");

                if (fieldName == "value")
                {
                    fieldName = $"this.{fieldName}";
                }

                pw.Setter.Add($"{fieldName} = value;");
                pw.Setter.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value);");

                result.Add(pw);
                result.Add(fw);

                var methodSetter = new MethodWriter($"Set{propertyName}", $"Sets the value of the <see cref=\"{propertyName}\"/> join on a single touchpanel.");
                methodSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                methodSetter.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated join value on.");
                methodSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value, panel);");
                methodSetter.MethodLines.Add($"if ({changeEventName} != null)");
                methodSetter.MethodLines.Add("{");
                methodSetter.MethodLines.Add($"{changeEventName}(this, new {args}(value));");
                methodSetter.MethodLines.Add("}");

                if ((JoinType == JoinType.Digital || JoinType == JoinType.DigitalPulse || JoinType == JoinType.SmartDigital) &&
                    (JoinDirection == JoinDirection.ToPanel || JoinDirection == JoinDirection.Both))
                {
                    var pulseMw = new MethodWriter($"Pulse{propertyName}", $"Pulses the {propertyName} digital signal. Any local signal changed events won't be fired by this method.");
                    pulseMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                    pulseMw.MethodLines.Add($"ParentPanel.Pulse({smartValue}(uint)({JoinNumber}{offsetText}), duration);");
                    result.Add(pulseMw);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects, configured for From panel messaging.
        /// </summary>
        /// <returns></returns>
        private List<WriterBase> GetWritersFromPanel()
        {
            if (JoinType == JoinType.DigitalButton || JoinType == JoinType.SmartDigitalButton)
            {
                return GetButtonWritersFromPanel();
            }

            var result = new List<WriterBase>();

            var sigType = GetJoinTypeString();
            var sigTypeName = GetJoinTypeNameString();

            var args = $"{sigTypeName}ValueChangedEventArgs";

            var fieldName = FormatFieldName(JoinName);

            var changeEventName = ChangeEventName;

            // First create the EventWriter.
            // This handles change event notifications, which are triggered when the value going to the panel is changed.
            result.Add(GetEventWriter());

            // Next create the property and backing field writers.
            var pw = GetPropertyWriter();
            var fw = GetFieldWriter();

            pw.Getter.Add($"return {fieldName};");
            pw.HasSetter = false;
            result.Add(pw);
            result.Add(fw);

            if (fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            var raiseMethod = new MethodWriter($"Raise{changeEventName}", $"Raises the {changeEventName} event.")
            {
                Accessor = Accessor.Private
            };

            raiseMethod.AddParameter($"{sigType}", "value", "The new value of the property.");

            raiseMethod.MethodLines.Add($"{fieldName} = value;");
            raiseMethod.MethodLines.Add($"if ({changeEventName} != null)");
            raiseMethod.MethodLines.Add("{");
            raiseMethod.MethodLines.Add($"{changeEventName}(this, new {args}(value));");
            raiseMethod.MethodLines.Add("}");

            result.Add(raiseMethod);

            return result;
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects configured for To/From panel messaging.
        /// </summary>
        /// <returns></returns>
        private List<WriterBase> GetWritersForBoth()
        {
            var result = GetWritersToPanel();

            if (JoinType == JoinType.DigitalButton || JoinType == JoinType.SmartDigitalButton)
            {
                result.AddRange(GetButtonWritersFromPanel());
                return result;
            }

            var fieldName = FormatFieldName(JoinName);
            var changeEventName = ChangeEventName;
            var sigType = GetJoinTypeString();
            var args = $"{GetJoinTypeNameString()}ValueChangedEventArgs";

            if (fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            var raiseMethod = new MethodWriter($"Raise{changeEventName}", $"Raises the {changeEventName} event.")
            {
                Accessor = Accessor.Private
            };

            raiseMethod.AddParameter($"{sigType}", "value", "The new value of the property.");

            raiseMethod.MethodLines.Add($"{fieldName} = value;");
            raiseMethod.MethodLines.Add($"if ({changeEventName} != null)");
            raiseMethod.MethodLines.Add("{");
            raiseMethod.MethodLines.Add($"{changeEventName}(this, new {args}(value));");
            raiseMethod.MethodLines.Add("}");

            result.Add(raiseMethod);

            return result;
        }

        private List<WriterBase> GetButtonWritersFromPanel()
        {
            var sigType = GetJoinTypeString();
            var args = $"{GetJoinTypeNameString()}ValueChangedEventArgs";

            var raisePressed = new MethodWriter($"RaisePressed", $"Raises the Pressed event.")
            {
                Accessor = Accessor.Private
            };
            raisePressed.AddParameter($"{sigType}", "value", "The pressed event boolean.");
            raisePressed.MethodLines.Add($"if (Pressed != null)");
            raisePressed.MethodLines.Add("{");
            raisePressed.MethodLines.Add($"Pressed(this, new {args}(value));");
            raisePressed.MethodLines.Add("}");

            var raiseReleased = new MethodWriter($"RaiseReleased", $"Raises the Released event.")
            {
                Accessor = Accessor.Private
            };
            raiseReleased.AddParameter($"{sigType}", "value", "The Released event boolean.");
            raiseReleased.MethodLines.Add($"if (Released != null)");
            raiseReleased.MethodLines.Add("{");
            raiseReleased.MethodLines.Add($"Released(this, new {args}(value));");
            raiseReleased.MethodLines.Add("}");

            var pressedEvent = new EventWriter("Pressed")
            {
                Handler = $"EventHandler<{args}>"
            };

            pressedEvent.Help.Summary = "Raised when the button is pressed.";

            var releasedEvent = new EventWriter("Released")
            {
                Handler = $"EventHandler<{args}>"
            };

            releasedEvent.Help.Summary = "Raised when the button is released.";

            var result = new List<WriterBase>()
            {
                raisePressed,
                raiseReleased,
                pressedEvent,
                releasedEvent
            };

            return result;
        }

        /// <summary>
        /// Gets a property writer correctly prepared for use with this join.
        /// </summary>
        /// <returns>A <see cref="PropertyWriter"/> object.</returns>
        private PropertyWriter GetPropertyWriter()
        {
            var propertyName = FormatPropertyName(JoinName);

            var propertyWriter = new PropertyWriter(
                propertyName, GetJoinTypeString());

            if (!string.IsNullOrEmpty(Description))
            {
                propertyWriter.Help.Summary = Description;
            }
            else
            {
                if (propertyWriter.Type == "bool")
                {
                    propertyWriter.Help.Summary = $"Gets or sets a value indicating whether the <see cref=\"{propertyName}\"/> join was last set to true or false.";
                }
                else
                {
                    propertyWriter.Help.Summary = $"Gets or sets a value indicating what the <see cref=\"{propertyName}\"/> join was last set to.";
                }
            }

            return propertyWriter;
        }

        /// <summary>
        /// Gets an event writer prepared for use with this join.
        /// </summary>
        /// <returns>A <see cref="FieldWriter"/> object.</returns>
        private EventWriter GetEventWriter()
        {
            var propName = FormatPropertyName(JoinName);

            var eventWriter = new EventWriter(ChangeEventName)
            {
                Handler = $"EventHandler<{GetJoinTypeNameString()}ValueChangedEventArgs>"
            };

            eventWriter.Help.Summary = $"Raised when the {propName} value changes.";

            return eventWriter;
        }

        /// <summary>
        /// Gets a field writer prepared for use with this join.
        /// </summary>
        /// <returns>A <see cref="FieldWriter"/> object.</returns>
        private FieldWriter GetFieldWriter()
        {
            var fieldWriter = new FieldWriter(FormatFieldName(JoinName), GetJoinTypeString());
            fieldWriter.Help.Summary = $"Backing field for the <see cref=\"{FormatPropertyName(JoinName)}\"/> property.";

            return fieldWriter;
        }

        /// <summary>
        /// Gets the shorthand version of the signal class type.
        /// </summary>
        /// <returns>A string with the value "bool", "ushort", or "string".</returns>
        private string GetJoinTypeString()
        {
            return JoinType == JoinType.Analog || JoinType == JoinType.SmartAnalog ? "ushort" :
                JoinType == JoinType.Serial || JoinType == JoinType.SmartSerial ? "string" :
                "bool";
        }

        /// <summary>
        /// Gets the full name of a signal class type.
        /// </summary>
        /// <returns>A string with the value "Boolean", "UShort", or "String".</returns>
        private string GetJoinTypeNameString()
        {
            return JoinType == JoinType.Analog || JoinType == JoinType.SmartAnalog ? "UShort" :
                JoinType == JoinType.Serial || JoinType == JoinType.SmartSerial ? "String" :
                "Boolean";
        }

        /// <summary>
        /// Gets the offset string to use for the join's numeric value in offset calculations.
        /// </summary>
        /// <returns>A string representing the text to include in offset calculations.</returns>
        private string GetOffsetString()
        {
            if (!IsListElement)
            {
                return string.Empty;
            }

            switch (JoinType)
            {
                case JoinType.Analog:
                case JoinType.SmartAnalog:
                case JoinType.AnalogSet:
                    return " + analogOffset";
                case JoinType.Digital:
                case JoinType.DigitalButton:
                case JoinType.DigitalPulse:
                case JoinType.SmartDigital:
                case JoinType.SmartDigitalButton:
                    return " + digitalOffset";
                case JoinType.Serial:
                case JoinType.SmartSerial:
                case JoinType.SerialSet:
                    return " + serialOffset";
                case JoinType.SrlVisibility:
                    return " + itemOffset + 2010";
                case JoinType.SrlEnable:
                    return " + itemOffset + 10";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Formats a string with an uppercase first letter.
        /// </summary>
        /// <param name="name">The name to normalize as a property name.</param>
        /// <returns>A string valid to use as a field name.</returns>
        private static string FormatPropertyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

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
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

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
