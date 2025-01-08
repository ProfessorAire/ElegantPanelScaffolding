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

        public uint DigitalOffset { get; set; }

        public uint AnalogOffset { get; set; }

        public uint SerialOffset { get; set; }

        /// <summary>
        /// Gets or sets the name of the join's change event. If an empty string this defaults to $"{JoinName}Changed"
        /// </summary>
        public string ChangeEventName
        {
            get => string.IsNullOrWhiteSpace(this.changeEventName) ? this.JoinType is JoinType.DigitalButton or JoinType.SmartDigitalButton ? string.Empty : $"{FormatPropertyName(this.JoinName)}Changed" : this.changeEventName;
            set => this.changeEventName = value;
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
        /// <param name="smartId">The ID of the SmartObject the join belongs to.</param>
        /// <param name="joinName">The join's name.</param>
        /// <param name="joinType">The join's type.</param>
        /// <param name="joinDirection">The join's direction.</param>
        /// <param name="joinMethod">The join's interaction method.</param>
        public JoinBuilder(uint joinNumber, uint smartId, string joinName, JoinType joinType, JoinDirection joinDirection)
        {
            this.JoinNumber = joinNumber;
            this.SmartJoinNumber = smartId;
            this.JoinName = joinName;
            this.JoinType = joinType;
            this.JoinDirection = joinDirection;
        }

        /// <summary>
        /// Gets a TextWriter that writes data into a class' constructor for initializing actions.
        /// </summary>
        /// <returns></returns>
        public TextWriter GetInitializers()
        {
            var writer = new TextWriter();

            if (this.JoinDirection is JoinDirection.FromPanel or JoinDirection.Both)
            {
                var raiseMethod = $"Raise{this.ChangeEventName}";

                var joinType = this.JoinType;

                if (this.SmartJoinNumber > 0)
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

                var offset = this.GetOffsetString();

                switch (joinType)
                {
                    case JoinType.Digital:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({this.JoinNumber}, (value) => {raiseMethod}(value));");
                        break;
                    case JoinType.DigitalButton:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({this.JoinNumber}{offset}, (value) => Raise{this.ChangeEventName}Pressed(value), true);");
                        writer.Text.Add($"ParentPanel.Actions.AddBool({this.JoinNumber}{offset}, (value) => Raise{this.ChangeEventName}Released(value), false);");
                        break;
                    case JoinType.Analog:
                        writer.Text.Add($"ParentPanel.Actions.AddUShort({this.JoinNumber}{offset}, (value) => {raiseMethod}(value));");
                        break;
                    case JoinType.Serial:
                        writer.Text.Add($"ParentPanel.Actions.AddString({this.JoinNumber}{offset}, (value) => {raiseMethod}(value));");
                        break;
                    case JoinType.SmartDigital:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({this.JoinNumber}, {this.SmartJoinNumber}, (value) => {raiseMethod}(value));");
                        break;
                    case JoinType.SmartDigitalButton:
                        writer.Text.Add($"ParentPanel.Actions.AddBool({this.JoinNumber}, {this.SmartJoinNumber}, (value) => Raise{this.ChangeEventName}Pressed(value), true);");
                        writer.Text.Add($"ParentPanel.Actions.AddBool({this.JoinNumber}, {this.SmartJoinNumber}, (value) => Raise{this.ChangeEventName}Released(value), false);");
                        break;
                    case JoinType.SmartAnalog:
                        writer.Text.Add($"ParentPanel.Actions.AddUShort({this.JoinNumber}, {this.SmartJoinNumber}, (value) => {raiseMethod}(value));");
                        break;
                    case JoinType.SmartSerial:
                        writer.Text.Add($"ParentPanel.Actions.AddString({this.JoinNumber}, {this.SmartJoinNumber}, (value) => {raiseMethod}(value));");
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
            if (this.JoinNumber == 0 || this.JoinType == JoinType.None)
            {
                return new List<WriterBase>(0);
            }

            if (this.JoinDirection == JoinDirection.ToPanel)
            {
                return this.GetWritersToPanel();
            }
            else if (this.JoinDirection == JoinDirection.FromPanel)
            {
                return this.GetWritersFromPanel();
            }
            else if (this.JoinDirection == JoinDirection.Both)
            {
                return this.GetWritersForBoth();
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

            var sigType = this.GetJoinTypeString();
            var sigTypeName = this.GetJoinTypeNameString();
            var offsetText = this.GetOffsetString();


            var args = $"{sigTypeName}ValueChangedEventArgs";
            var propertyName = FormatPropertyName(this.JoinName);
            var fieldName = FormatFieldName(this.JoinName);
            var smartSuffix = this.SmartJoinNumber > 0 ? "Smart" : string.Empty;
            var smartValue = this.SmartJoinNumber > 0 ? $"{this.SmartJoinNumber}, " : string.Empty;

            var changeEventName = this.ChangeEventName;

            if (this.JoinType is JoinType.DigitalPulse or JoinType.AnalogSet or JoinType.SerialSet)
            {
                var prefix = this.JoinType == JoinType.DigitalPulse ? "Latch" : "";
                var singleSetter = new MethodWriter($"{prefix}{propertyName}", $"Sends the value to a single touchpanel.");
                singleSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                singleSetter.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated join value on.");
                singleSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({this.JoinNumber}{offsetText}), value, panel);");

                result.Add(singleSetter);

                var allSetter = new MethodWriter($"{prefix}{propertyName}", $"Sends the value to all touchpanels.");
                allSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                allSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({this.JoinNumber}{offsetText}), value);");
                allSetter.MethodLines.Add($"var changeEvent = {changeEventName};");
                allSetter.MethodLines.Add($"if (changeEvent != null)");
                allSetter.MethodLines.Add("{");
                allSetter.MethodLines.Add($"changeEvent.Invoke(this, new {args}(value));");
                allSetter.MethodLines.Add("}");

                result.Add(allSetter);

                if (this.JoinType == JoinType.DigitalPulse)
                {
                    var pulseMw = new MethodWriter($"{propertyName}", $"Pulses the {propertyName} digital signal.");
                    pulseMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                    pulseMw.MethodLines.Add($"ParentPanel.Pulse({smartValue}(uint)({this.JoinNumber}{offsetText}), duration);");

                    result.Add(pulseMw);
                }
            }
            else
            {
                // First create the EventWriter.
                // This handles change event notifications, which are triggered when the value going to the panel is changed.
                result.Add(this.GetEventWriter());

                // Next create the property and backing field writers.
                var pw = this.GetPropertyWriter(fieldName);

                pw.Setter.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({this.JoinNumber}{offsetText}), value);");

                result.Add(pw);

                if (this.JoinType == JoinType.Analog || this.JoinType == JoinType.SmartAnalog)
                {
                    var methodSetAll = new MethodWriter($"Set{propertyName}Feedback", $"Sets the value of the <see cref=\"{propertyName}\"/> join on all touchpanels.");
                    methodSetAll.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                    methodSetAll.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({this.JoinNumber}{offsetText}), value);");
                    result.Add(methodSetAll);
                }

                var methodSetter = new MethodWriter($"Set{propertyName}", $"Sets the value of the <see cref=\"{propertyName}\"/> join on a single touchpanel.");
                methodSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
                methodSetter.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated join value on.");
                methodSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({this.JoinNumber}{offsetText}), value, panel);");

                if (this.JoinDirection == JoinDirection.ToPanel)
                {
                    methodSetter.MethodLines.Add($"var changeEvent = {changeEventName};");
                    methodSetter.MethodLines.Add($"if (changeEvent != null)");
                    methodSetter.MethodLines.Add("{");
                    methodSetter.MethodLines.Add($"changeEvent.Invoke(this, new {args}(value));");
                    methodSetter.MethodLines.Add("}");
                }

                result.Add(methodSetter);

                if ((this.JoinType == JoinType.Digital || this.JoinType == JoinType.DigitalPulse || this.JoinType == JoinType.SmartDigital) &&
                    (this.JoinDirection == JoinDirection.ToPanel || this.JoinDirection == JoinDirection.Both))
                {
                    var pulseMw = new MethodWriter($"Pulse{propertyName}", $"Pulses the {propertyName} digital signal. Any local signal changed events won't be fired by this method.");
                    pulseMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                    pulseMw.MethodLines.Add($"ParentPanel.Pulse({smartValue}(uint)({this.JoinNumber}{offsetText}), duration);");
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
            if (this.JoinType is JoinType.DigitalButton or JoinType.SmartDigitalButton)
            {
                return this.GetButtonWritersFromPanel();
            }

            var result = new List<WriterBase>();

            var sigType = this.GetJoinTypeString();
            var fieldName = FormatFieldName(this.JoinName);

            var changeEventName = this.ChangeEventName;

            // First create the EventWriter.
            // This handles change event notifications, which are triggered when the value going to the panel is changed.
            result.Add(this.GetEventWriter());

            if (fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            // Next create the property and backing field writers.
            var pw = this.GetPropertyWriter(fieldName);

            pw.PrivateSetter = true;

            result.Add(pw);

            var raiseMethod = new MethodWriter($"Raise{changeEventName}", $"Raises the {changeEventName} event.")
            {
                Accessor = Accessor.Private
            };

            raiseMethod.AddParameter($"{sigType}", "value", "The new value of the property.");

            raiseMethod.MethodLines.Add($"this.{this.JoinName} = value;");
            //raiseMethod.MethodLines.Add($"var changeEvent = {changeEventName};");
            //raiseMethod.MethodLines.Add($"if (changeEvent != null)");
            //raiseMethod.MethodLines.Add("{");
            //raiseMethod.MethodLines.Add($"changeEvent.Invoke(this, new {args}(value));");
            //raiseMethod.MethodLines.Add("}");

            result.Add(raiseMethod);

            return result;
        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects configured for To/From panel messaging.
        /// </summary>
        /// <returns></returns>
        private List<WriterBase> GetWritersForBoth()
        {
            var result = this.GetWritersToPanel();

            result.AddRange(this.GetWritersFromPanel());
            return result;
            //if (JoinType == JoinType.DigitalButton || JoinType == JoinType.SmartDigitalButton)
            //{
            //    result.AddRange(GetButtonWritersFromPanel());
            //    return result;
            //}

            //var fieldName = FormatFieldName(JoinName);
            //var changeEventName = ChangeEventName;
            //var sigType = GetJoinTypeString();
            //var args = $"{GetJoinTypeNameString()}ValueChangedEventArgs";

            //if (fieldName == "value")
            //{
            //    fieldName = $"this.{fieldName}";
            //}

            //var raiseMethod = new MethodWriter($"Raise{changeEventName}", $"Raises the {changeEventName} event.")
            //{
            //    Accessor = Accessor.Private
            //};

            //raiseMethod.AddParameter($"{sigType}", "value", "The new value of the property.");

            //raiseMethod.MethodLines.Add($"{fieldName} = value;");
            //raiseMethod.MethodLines.Add($"var changeEvent = {changeEventName};");
            //raiseMethod.MethodLines.Add($"if (changeEvent != null)");
            //raiseMethod.MethodLines.Add("{");
            //raiseMethod.MethodLines.Add($"changeEvent.Invoke(this, new {args}(value));");
            //raiseMethod.MethodLines.Add("}");

            //result.Add(raiseMethod);

            //return result;
        }

        private List<WriterBase> GetButtonWritersFromPanel()
        {
            var sigType = this.GetJoinTypeString();
            var args = $"{this.GetJoinTypeNameString()}ValueChangedEventArgs";

            var buttonState = new PropertyWriter($"{this.ChangeEventName}PressState", sigType, true)
            {
                ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged,
                PrivateSetter = true
            };

            buttonState.Help.Summary = $"Gets a value indicating whether the {this.ChangeEventName}PressState is pressed or released.";

            var raisePressed = new MethodWriter($"Raise{this.ChangeEventName}Pressed", $"Raises the {this.ChangeEventName}Pressed event.")
            {
                Accessor = Accessor.Private
            };

            raisePressed.AddParameter($"{sigType}", "value", "The pressed event boolean.");
            raisePressed.MethodLines.Add($"this.{this.ChangeEventName}PressState = true;");
            raisePressed.MethodLines.Add($"var changeEvent = this.{this.ChangeEventName}Pressed;");
            raisePressed.MethodLines.Add($"if (changeEvent != null)");
            raisePressed.MethodLines.Add("{");
            raisePressed.MethodLines.Add($"changeEvent.Invoke(this, new {args}(true));");
            raisePressed.MethodLines.Add("}");

            var raiseReleased = new MethodWriter($"Raise{this.ChangeEventName}Released", $"Raises the {this.ChangeEventName}Released event.")
            {
                Accessor = Accessor.Private
            };

            raiseReleased.AddParameter($"{sigType}", "value", "The released event boolean.");
            raiseReleased.MethodLines.Add($"this.{this.ChangeEventName}PressState = false;");
            raiseReleased.MethodLines.Add($"var changeEvent = this.{this.ChangeEventName}Released;");
            raiseReleased.MethodLines.Add($"if (changeEvent != null)");
            raiseReleased.MethodLines.Add("{");
            raiseReleased.MethodLines.Add($"changeEvent.Invoke(this, new {args}(false));");
            raiseReleased.MethodLines.Add("}");

            var pressedEvent = new EventWriter($"{this.ChangeEventName}Pressed")
            {
                Handler = $"EventHandler<{args}>"
            };

            pressedEvent.Help.Summary = "Raised when the button is pressed.";

            var releasedEvent = new EventWriter($"{this.ChangeEventName}Released")
            {
                Handler = $"EventHandler<{args}>"
            };

            releasedEvent.Help.Summary = "Raised when the button is released.";

            var changedEvent = new EventWriter($"{this.ChangeEventName}PressStateChanged")
            {
                Handler = $"EventHandler<{args}>"
            };

            var result = new List<WriterBase>()
            {
                buttonState,
                raisePressed,
                raiseReleased,
                pressedEvent,
                releasedEvent,
                changedEvent
            };

            return result;
        }

        /// <summary>
        /// Gets a property writer correctly prepared for use with this join.
        /// </summary>
        /// <param name="fieldName">The name of the backing field.</param>
        /// <returns>A <see cref="PropertyWriter"/> object.</returns>
        private PropertyWriter GetPropertyWriter(string fieldName)
        {
            var propertyName = FormatPropertyName(this.JoinName);

            var propertyWriter = new PropertyWriter(
                propertyName,
                this.GetJoinTypeString(),
                true)
            {
                ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged
            };

            if (propertyWriter.BackingFieldWriter != null)
            {
                propertyWriter.BackingFieldWriter.Name = fieldName;
            }

            if (!string.IsNullOrEmpty(this.Description))
            {
                propertyWriter.Help.Summary = this.Description;
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
            var propName = FormatPropertyName(this.JoinName);

            var eventWriter = new EventWriter(this.ChangeEventName)
            {
                Handler = $"EventHandler<{this.GetJoinTypeNameString()}ValueChangedEventArgs>"
            };

            eventWriter.Help.Summary = $"Raised when the {propName} value changes.";

            return eventWriter;
        }

        ///// <summary>
        ///// Gets a field writer prepared for use with this join.
        ///// </summary>
        ///// <returns>A <see cref="FieldWriter"/> object.</returns>
        //private FieldWriter GetFieldWriter()
        //{
        //    var fieldWriter = new FieldWriter(FormatFieldName(JoinName), GetJoinTypeString())
        //    {
        //        Accessor = Accessor.Private
        //    };

        //    fieldWriter.Help.Summary = $"Backing field for the <see cref=\"{FormatPropertyName(JoinName)}\"/> property.";

        //    return fieldWriter;
        //}

        /// <summary>
        /// Gets the shorthand version of the signal class type.
        /// </summary>
        /// <returns>A string with the value "bool", "ushort", or "string".</returns>
        private string GetJoinTypeString()
        {
            return this.JoinType is JoinType.Analog or JoinType.SmartAnalog ? "ushort" :
                this.JoinType is JoinType.Serial or JoinType.SmartSerial ? "string" :
                "bool";
        }

        /// <summary>
        /// Gets the full name of a signal class type.
        /// </summary>
        /// <returns>A string with the value "Boolean", "UShort", or "String".</returns>
        private string GetJoinTypeNameString()
        {
            return this.JoinType is JoinType.Analog or JoinType.SmartAnalog ? "UShort" :
                this.JoinType is JoinType.Serial or JoinType.SmartSerial ? "String" :
                "Boolean";
        }

        /// <summary>
        /// Gets the offset string to use for the join's numeric value in offset calculations.
        /// </summary>
        /// <returns>A string representing the text to include in offset calculations.</returns>
        private string GetOffsetString()
        {
            if (!this.IsListElement)
            {
                return this.JoinType switch
                {
                    JoinType.Analog or JoinType.AnalogSet => this.AnalogOffset > 0 ? $" + {this.AnalogOffset}" : string.Empty,
                    JoinType.Digital or JoinType.DigitalButton or JoinType.DigitalPulse => this.DigitalOffset > 0 ? $" + {this.DigitalOffset}" : string.Empty,
                    JoinType.Serial or JoinType.SerialSet => this.SerialOffset > 0 ? $" + {this.SerialOffset}" : string.Empty,
                    _ => string.Empty,
                };
            }

            return this.JoinType switch
            {
                JoinType.Analog or JoinType.SmartAnalog or JoinType.AnalogSet => " + this.analogOffset",
                JoinType.Digital or JoinType.DigitalButton or JoinType.DigitalPulse or JoinType.SmartDigital or JoinType.SmartDigitalButton => " + this.digitalOffset",
                JoinType.Serial or JoinType.SmartSerial or JoinType.SerialSet => " + this.serialOffset",
                JoinType.SrlVisibility => " + itemOffset + 2010",
                JoinType.SrlEnable => " + itemOffset + 10",
                _ => string.Empty,
            };
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
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return SanitizeString($"{name.ToLowerInvariant()[0]}{name.Substring(1)}");
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
