using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EPS.CodeGen.Builders
{
    public enum ClassType
    {
        Touchpanel,
        Page,
        Control,
        SmartObject,
        SrlElement
    }

    public class ClassBuilder
    {
        public ushort DigitalOffset { get; set; }
        public ushort AnalogOffset { get; set; }
        public ushort SerialOffset { get; set; }
        public ushort SmartJoin { get; set; }
        public ushort ItemOffset { get; set; }
        private string className = "";
        public string ClassName { get => SanitizeName(className); set => className = value; }
        public string Namespace { get; set; } = "";
        public string NamespaceBase { get; set; } = "";
        protected List<ElementBase> Properties { get; } = new List<ElementBase>();
        protected List<ElementBase> Events { get; } = new List<ElementBase>();

        protected List<ClassBuilder> Pages { get; } = new List<ClassBuilder>();
        protected List<ClassBuilder> Controls { get; } = new List<ClassBuilder>();
        protected List<ListBuilder> Lists { get; } = new List<ListBuilder>();

        protected List<Writers.EventWriter> EventWriters { get; } = new List<Writers.EventWriter>();
        protected List<Writers.FieldWriter> FieldWriters { get; } = new List<Writers.FieldWriter>();
        protected List<Writers.PropertyWriter> PropertyWriters { get; } = new List<Writers.PropertyWriter>();
        protected List<Writers.MethodWriter> MethodWriters { get; } = new List<Writers.MethodWriter>();
        protected List<Writers.TextWriter> OtherWriters { get; } = new List<Writers.TextWriter>();

        public bool IsValid
        {
            get
            {
                if (!string.IsNullOrEmpty(ClassName) &&
                    (Properties.Count > 0 ||
                    Events.Count > 0 ||
                    Pages.Count > 0 ||
                    Controls.Count > 0))
                {
                    return true;
                }
                return false;
            }
        }

        public ClassType ClassType { get; set; } = ClassType.Touchpanel;

        public ClassBuilder(ClassType classType) => ClassType = classType;

        public void AddWriter(Writers.WriterBase writer)
        {
            if (writer is Writers.EventWriter ew && !EventWriters.Where(e => e.Name == ew.Name).Any())
            {
                EventWriters.Add(ew);
                return;
            }

            if (writer is Writers.FieldWriter fw && !FieldWriters.Where(e => e.Name == fw.Name).Any())
            {
                FieldWriters.Add(fw);
                return;
            }

            if (writer is Writers.PropertyWriter pw && !PropertyWriters.Where(e => e.Name == pw.Name).Any())
            {
                PropertyWriters.Add(pw);
                return;
            }

            if (writer is Writers.MethodWriter mw && !MethodWriters.Where(e => e.Name == mw.Name).Any())
            {
                MethodWriters.Add(mw);
                return;
            }

            if (writer is Writers.TextWriter tw)
            {
                OtherWriters.Add(tw);
                return;
            }

        }

        public void AddProperty(ElementBase element)
        {
            if (element != null && !element.Name.ToUpperInvariant().Contains("NULL"))
            {
                element.AnalogOffset = AnalogOffset;
                element.DigitalOffset = DigitalOffset;
                element.SerialOffset = SerialOffset;
                Properties.Add(element);
            }
        }

        public void AddEvent(ElementBase element)
        {
            if (element != null && !element.Name.ToUpperInvariant().Contains("NULL"))
            {
                element.AnalogOffset = AnalogOffset;
                element.DigitalOffset = DigitalOffset;
                element.SerialOffset = SerialOffset;
                Events.Add(element);
            }
        }

        public void AddPage(ClassBuilder page)
        {
            if (page?.IsValid ?? false && !page.ClassName.ToUpperInvariant().Contains("NULL"))
            {
                if (page != null)
                {
                    Pages.Add(page);
                }
            }
        }

        public void AddControl(ClassBuilder control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (control.IsValid && !control.ClassName.ToUpperInvariant().Contains("NULL"))
            {
                Controls.Add(control);
            }
        }

        public void AddList(ListBuilder list)
        {
            if (list != null && !list.Name.ToUpperInvariant().Contains("NULL"))
            {
                Lists.Add(list);
            }
        }

        public List<(string className, string classPath, Writers.NamespaceWriter nameSpace)> Build(string rootNamespace = "", string ParentPanelClass = "PanelUIBase")
        {
            var options = Options.Current;
            var items = new List<(string className, string classPath, Writers.NamespaceWriter nameSpace)>();
            if (options == null) { return items; }
            if (!Namespace.StartsWith(NamespaceBase, StringComparison.InvariantCulture))
            {
                Namespace = NamespaceBase;
            }
            if (!string.IsNullOrEmpty(rootNamespace))
            {
                Namespace = rootNamespace;
            }

            Writers.NamespaceWriter nsb;
            if (ClassType == ClassType.Touchpanel)
            {
                nsb = new Writers.NamespaceWriter($"{Namespace}.{ClassName}");
                Namespace = $"{Namespace}.{ClassName}.Components";
                NamespaceBase = ClassName;
            }
            else if (ClassType == ClassType.Page)
            {
                nsb = new Writers.NamespaceWriter($"{Namespace}");
                nsb.AddUsing($"{Namespace}.{ClassName}Components");
                NamespaceBase = Namespace;
                Namespace = $"{Namespace}.{ClassName}Components";
            }
            else
            {
                nsb = new Writers.NamespaceWriter(Namespace);
            }

            nsb.AddUsing("System");
            nsb.AddUsing("System.Collections.Generic");

            if (ClassType != ClassType.Touchpanel)
            {
                nsb.AddUsing("Crestron.SimplSharpPro.DeviceSupport");
            }

            //if (ClassType == ClassType.Page)
            //{
            //    nsb.AddUsing($"{Namespace}");
            //}

            nsb.AddUsing($"{options.PanelNamespace}.Core");
            if (ClassType == ClassType.Touchpanel)
            {
                nsb.AddUsing($"{options.PanelNamespace}.Components");
            }
            Writers.ClassWriter mainClass;

            if (ClassType == ClassType.Touchpanel)
            {
                ClassName = "Panel";
                mainClass = new Writers.ClassWriter("Panel");
            }
            else if (ClassType == ClassType.SrlElement)
            {
                mainClass = new Writers.ClassWriter($"{ClassName}");
            }
            else
            {
                mainClass = new Writers.ClassWriter(ClassName);
            }
            mainClass.Modifier = Modifier.Partial;
            if (ClassType == ClassType.Touchpanel)
            {
                mainClass.Implements.Add("PanelUIBase");
            }
            else
            {
                mainClass.Implements.Add(nameof(IDisposable));
            }

            // Main Class Constructor
            var ctor = new Writers.MethodWriter(ClassName, "Creates a new instance of the class.", "", 2);

            // Partial _Setup method.
            var partialSetup = new Writers.MethodWriter("_Setup", "Implement this in accompanying classes in order to setup functionality on the construction of the root class.\nNo values should be sent to this touchpanel in this method!", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialSetup);

            // Partial _Dispose method.
            var partialDispose = new Writers.MethodWriter("_Dispose", "Implement this in accompanying classes in order to dispose of objects as needed.", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialDispose);

            // Dispose Method
            Writers.MethodWriter disp;
            if (ClassType == ClassType.Touchpanel)
            {
                disp = new Writers.MethodWriter("DisposeChildren", "Calls the partial void _Dispose in order to allow disposing of custom objects.", "void", 2)
                {
                    Accessor = Accessor.Protected,
                    Modifier = Modifier.Override
                };
            }
            else
            {
                disp = new Writers.MethodWriter("Dispose", "Calls the partial void _Dispose in order to allow disposing of custom objects.", "void", 2)
                {
                    Accessor = Accessor.Public,
                    Modifier = Modifier.None
                };
            }
            disp.MethodLines.Add("_Dispose();");
            foreach (var p in Pages)
            {
                disp.MethodLines.Add($"{p.ClassName}.Dispose();");
            }
            foreach (var c in Controls)
            {
                disp.MethodLines.Add($"{c.ClassName.Replace(ClassName, "")}.Dispose();");
            }
            if (Lists.Count > 0)
            {
                disp.MethodLines.Add($"foreach (var i in Items)");
                disp.MethodLines.Add("{");
                foreach (var l in Lists)
                {
                    disp.MethodLines.Add($"i.Dispose();");
                }
                disp.MethodLines.Add("}");
            }
            mainClass.Methods.Add(disp);

            // Partial Initialize Values Method.
            var partialInit = new Writers.MethodWriter("_InitializeValues", "Implement in accompanying classes in order to send initial values to the touchpanels when the root class Threads are started.", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialInit);

            // Data Property
            var classDataProp = new Writers.PropertyWriter("data", $"{ClassName}Data", 2);
            classDataProp.Help.Summary = "Holds the data about the class. Registered with the parent panel.";
            classDataProp.Accessor = Accessor.Internal;
            mainClass.Properties.Add(classDataProp);

            // Parent Panel.
            if (ClassType != ClassType.Touchpanel)
            {
                var parentPanel = new Writers.FieldWriter("ParentPanel", "Panel", 2);
                parentPanel.Help.Summary = $"The <see cref=\"Panel\"/> that this object belongs to.";
                mainClass.Fields.Add(parentPanel);
                ctor.MethodLines.Add("ParentPanel = parent;");
                ctor.AddParameter("Panel", "parent", "The class that is the base parent of this one.");
            }

            // Data Bits
            if (ClassType != ClassType.SrlElement)
            {
                ctor.MethodLines.Add($"data = new {ClassName}Data() {{ AssociatedClass = this }};");
            }

            // Pages and Controls
            if (ClassType == ClassType.Touchpanel)
            {
                ctor.MethodLines.Add("AddData(data);");
                foreach (var p in Pages)
                {
                    ctor.MethodLines.Add($"{p.ClassName} = new {p.ClassName}(this);");
                }
                foreach (var c in Controls)
                {
                    ctor.MethodLines.Add($"{c.ClassName} = new {c.ClassName}(this);");
                }
            }
            else if (ClassType == ClassType.Page)
            {
                ctor.MethodLines.Add("ParentPanel.AddData(data);");
                foreach (var p in Pages)
                {
                    ctor.MethodLines.Add($"{p.ClassName} = new {p.ClassName}(ParentPanel);");
                }
                foreach (var c in Controls)
                {
                    ctor.MethodLines.Add($"{c.ClassName} = new {c.ClassName}(ParentPanel);");
                }
            }
            else if (ClassType == ClassType.Control || ClassType == ClassType.SmartObject)
            {
                ctor.MethodLines.Add("ParentPanel.AddData(data);");
                foreach (var l in Lists)
                {
                    //ctor.MethodLines.Add(l.GetElementText(options, $"{classNamePrefix}{l.Name}"));
                    foreach (var w in l.GetWriters())
                    {
                        AddWriter(w);
                    }
                }
            }
            else if (ClassType == ClassType.SrlElement)
            {
                var digitalOffsetField = new Writers.FieldWriter("digitalOffset", "ushort", 2) { Accessor = Accessor.Private, DefaultValue = "0" };
                digitalOffsetField.Help.Summary = "The offset amount this item uses for its digital joins.";
                var analogOffsetField = new Writers.FieldWriter("analogOffset", "ushort", 2) { Accessor = Accessor.Private, DefaultValue = "0" };
                analogOffsetField.Help.Summary = "The offset amount this item uses for its analog joins.";
                var serialOffsetField = new Writers.FieldWriter("serialOffset", "ushort", 2) { Accessor = Accessor.Private, DefaultValue = "0" };
                serialOffsetField.Help.Summary = "The offset amount this item uses for its serial joins.";
                var itemOffsetField = new Writers.FieldWriter("itemOffset", "ushort", 2) { Accessor = Accessor.Private, DefaultValue = "0" };
                itemOffsetField.Help.Summary = "The offset amount this item has.";

                mainClass.Fields.Add(digitalOffsetField);
                mainClass.Fields.Add(analogOffsetField);
                mainClass.Fields.Add(serialOffsetField);
                mainClass.Fields.Add(itemOffsetField);

                ctor.AddParameter("ushort", "digitalOffset", "The offset amount this item uses for its digital joins.");
                ctor.AddParameter("ushort", "analogOffset", "The offset amount this item uses for its analog joins.");
                ctor.AddParameter("ushort", "serialOffset", "The offset amount this item uses for its serial joins.");
                ctor.AddParameter("ushort", "itemOffset", "The offset amount this item has.");

                ctor.MethodLines.Add("this.digitalOffset = digitalOffset;");
                ctor.MethodLines.Add("this.analogOffset = analogOffset;");
                ctor.MethodLines.Add("this.serialOffset = serialOffset;");
                ctor.MethodLines.Add("this.itemOffset = itemOffset;");
                ctor.MethodLines.Add("");

                ctor.MethodLines.Add("if (this.digitalOffset == 0 && this.analogOffset == 0 && this.serialOffset == 0)");
                ctor.MethodLines.Add("{");
                ctor.MethodLines.Add("this.digitalOffset = itemOffset;");
                ctor.MethodLines.Add("this.analogOffset = itemOffset;");
                ctor.MethodLines.Add("this.serialOffset = itemOffset;");
                ctor.MethodLines.Add("}");
                ctor.MethodLines.Add("");
                
                ctor.MethodLines.Add($"data = new {ClassName}Data(this.digitalOffset, this.analogOffset, this.serialOffset) {{ AssociatedClass = this }};");
                ctor.MethodLines.Add($"ParentPanel.AddData(data);");
                ctor.MethodLines.Add("");


                for (var i = 0; i < Controls.Count; i++)
                {
                    ctor.MethodLines.Add($"{Controls[i].ClassName.Replace(ClassName, "")} = new {Controls[i].ClassName}(ParentPanel, digitalOffset, analogOffset, serialOffset, itemOffset);");
                }
            }

            // Before adding the ctor, all the TextWriters should be providing constructor lines, so we'll add them there.
            foreach (var w in OtherWriters)
            {
                ctor.MethodLines.Add(w.ToString());
            }

            // Call _Setup Last
            ctor.MethodLines.Add("_Setup();");

            mainClass.Constructors.Add(ctor);

            // Completed Constructor

            // Initialize Values Method
            var initValuesMethod = new Writers.MethodWriter("InitializeValues", "Attempts to initialize values for the current class.", "void", 2);

            if (ClassType == ClassType.Touchpanel)
            {
                initValuesMethod.Accessor = Accessor.Protected;
                initValuesMethod.Modifier = Modifier.Override;
            }
            else
            {
                initValuesMethod.Accessor = Accessor.Internal;
            }
            initValuesMethod.MethodLines.Add("_InitializeValues();");

            // Other Events
            foreach (var e in Events.OfType<EventElement>())
            {
                foreach (var w in e.GetWriters())
                {
                    AddWriter(w);
                }
            }

            // Other Properties
            foreach (var p in Properties.OfType<PropertyElement>())
            {
                if (ClassType == ClassType.SrlElement)
                {
                    p.IsListElement = true;
                }
                foreach (var w in p.GetWriters())
                {
                    AddWriter(w);
                }
            }

            // Other Pages
            foreach (var c in Pages)
            {
                var fw = new Writers.FieldWriter(c.ClassName, c.ClassName);
                fw.Help.Summary = $"Provides access to the {c.ClassName} Page.";
                FieldWriters.Add(fw);
                initValuesMethod.MethodLines.Add($"{c.ClassName}.InitializeValues();");
            }

            // Other Controls
            foreach (var c in Controls)
            {
                var fw = new Writers.FieldWriter(c.ClassName, c.ClassName);
                if (ClassType == ClassType.SrlElement)
                {
                    fw.Name = c.ClassName.Replace(ClassName, "");
                }
                fw.Help.Summary = $"Provides access to the {fw.Name} Control.";
                FieldWriters.Add(fw);
                initValuesMethod.MethodLines.Add($"{fw.Name}.InitializeValues();");
            }

            // Add initialize values method
            mainClass.Methods.Add(initValuesMethod);

            if (Properties.Count > 0 || Events.Count > 0)
            {
                var subMW = new Writers.MethodWriter("ClearAllEventSubscriptions", "Clears all of the subscriptions on events.", "void");
                foreach (var p in Properties.OfType<PropertyElement>())
                {
                    subMW.MethodLines.Add($"{p.Name}Changed = null;");
                }
                foreach (var e in Events.OfType<EventElement>())
                {
                    var names = e.GetEventNames();
                    foreach (var name in names)
                    {
                        subMW.MethodLines.Add($"{name} = null;");
                    }
                }
            }

            foreach (var c in Controls)
            {
                if (ClassType == ClassType.SrlElement)
                {
                    c.ClassType = ClassType.SrlElement;
                }
                var built = c.Build($"{Namespace}", ParentPanelClass);
                foreach (var builder in built)
                {
                    items.Add(builder);
                }
            }

            foreach (var l in Lists)
            {
                var built = l.Control.Build($"{Namespace}", ParentPanelClass);
                foreach (var builder in built)
                {
                    items.Add(builder);
                }
            }

            // Add the writers to the classes.
            mainClass.Properties.AddRange(PropertyWriters);
            mainClass.Fields.AddRange(FieldWriters);
            mainClass.Events.AddRange(EventWriters);
            mainClass.Methods.AddRange(MethodWriters);

            nsb.Classes.Add(mainClass);

            var dataClass = new Writers.ClassWriter($"{ClassName}Data : PanelUIData");
            dataClass.Help.Summary = $"Data class for the <see cref=\"{Namespace}.{ClassName}\"/> object.";

            var dataCtor = new Writers.MethodWriter($"{ClassName}Data", "Creates a new instance of the data class object.", "");

            var useOffsets = ClassType == ClassType.SrlElement;

            if (useOffsets)
            {
                dataCtor.AddParameter("int", "digitalOffset", "The number of digital joins provided for this reference list item.");
                dataCtor.AddParameter("int", "analogOffset", "The number of analog joins provided for this reference list item.");
                dataCtor.AddParameter("int", "serialOffset", "The number of serial joins provided for this reference list item.");
            }

            if (SmartJoin > 0)
            {
                dataCtor.MethodLines.Add($"smartObjectJoin = {SmartJoin};");
            }

            if (Events.OfType<EventElement>().Where(ev => ev.ValueType == JoinType.Digital || ev.ValueType == JoinType.SmartDigital).Any() ||
                Properties.OfType<PropertyElement>().Where(pr => (pr.PropertyType == JoinType.Digital || pr.PropertyType == JoinType.Digital) && (pr.PropertyMethod == PropertyMethod.Both || pr.PropertyMethod == PropertyMethod.FromPanel)).Any())
            {
                dataCtor.MethodLines.Add($"{(SmartJoin > 0 ? "smartObjectBool" : "bool")}Outputs = new Dictionary<string, ushort>()");
                dataCtor.MethodLines.Add("{");
                var events = Events.OfType<EventElement>().Where(ev => ev.ValueType == JoinType.Digital || ev.ValueType == JoinType.SmartDigital).ToArray();
                var properties = Properties.OfType<PropertyElement>().Where(pr => (pr.PropertyType == JoinType.Digital || pr.PropertyType == JoinType.SmartDigital) && (pr.PropertyMethod == PropertyMethod.Both || pr.PropertyMethod == PropertyMethod.FromPanel)).ToArray();

                for (var i = 0; i < events.Length; i++)
                {
                    var data = events[i].GetData();
                    for (var j = 0; j < data.Length; j++)
                    {
                        var (name, join) = data[j];
                        dataCtor.MethodLines.Add($"{{\"{name}\", (ushort)({join}{(useOffsets ? " + digitalOffset" : "")})}}{((j < data.Length - 1 || i < events.Length - 1) ? "," : "")}");
                    }
                }

                for (var i = 0; i < properties.Length; i++)
                {
                    var data = properties[i].GetData();
                    for (var j = 0; j < data.Length; j++)
                    {
                        var (name, join) = data[j];
                        dataCtor.MethodLines.Add($"{{\"{name}\", (ushort)({join}{(useOffsets ? " + digitalOffset" : "")})}}{((j < data.Length - 1 || i < properties.Length - 1) ? "," : "")}");
                    }
                }

                dataCtor.MethodLines.Add("};");
            }

            if (Events.OfType<EventElement>().Where(ev => ev.ValueType == JoinType.Analog || ev.ValueType == JoinType.SmartAnalog).Any() ||
                Properties.OfType<PropertyElement>().Where(pr => (pr.PropertyType == JoinType.Analog || pr.PropertyType == JoinType.SmartAnalog) && (pr.PropertyMethod == PropertyMethod.Both || pr.PropertyMethod == PropertyMethod.FromPanel)).Any())
            {
                dataCtor.MethodLines.Add($"{(SmartJoin > 0 ? "smartObjectUShort" : "ushort")}Outputs = new Dictionary<string, ushort>()");
                dataCtor.MethodLines.Add("{");
                var events = Events.OfType<EventElement>().Where(ev => ev.ValueType == JoinType.Analog || ev.ValueType == JoinType.SmartAnalog).ToArray();
                var properties = Properties.OfType<PropertyElement>().Where(pr => (pr.PropertyType == JoinType.Analog || pr.PropertyType == JoinType.SmartAnalog) && (pr.PropertyMethod == PropertyMethod.Both || pr.PropertyMethod == PropertyMethod.FromPanel)).ToArray();
                for (var i = 0; i < events.Length; i++)
                {
                    var data = events[i].GetData();
                    for (var j = 0; j < data.Length; j++)
                    {
                        var (name, join) = data[j];
                        dataCtor.MethodLines.Add($"{{\"{name}\", (ushort)({join}{(useOffsets ? " + analogOffset" : "")})}}{((j < data.Length - 1 || i < events.Length - 1) ? "," : "")}");
                    }
                }

                for (var i = 0; i < properties.Length; i++)
                {
                    var data = properties[i].GetData();
                    for (var j = 0; j < data.Length; j++)
                    {
                        var (name, join) = data[j];
                        dataCtor.MethodLines.Add($"{{\"{name}\", (ushort)({join}{(useOffsets ? " + analogOffset" : "")})}}{((j < data.Length - 1 || i < properties.Length - 1) ? "," : "")}");
                    }
                }

                dataCtor.MethodLines.Add("};");
            }

            if (Events.OfType<EventElement>().Where(ev => ev.ValueType == JoinType.Serial || ev.ValueType == JoinType.SmartSerial).Any() ||
                Properties.OfType<PropertyElement>().Where(pr => (pr.PropertyType == JoinType.Serial || pr.PropertyType == JoinType.SmartSerial) && (pr.PropertyMethod == PropertyMethod.Both || pr.PropertyMethod == PropertyMethod.FromPanel)).Any())
            {
                dataCtor.MethodLines.Add($"{(SmartJoin > 0 ? "smartObjectString" : "string")}Outputs = new Dictionary<string, ushort>()");
                dataCtor.MethodLines.Add("{");
                var events = Events.OfType<EventElement>().Where(ev => ev.ValueType == JoinType.Serial || ev.ValueType == JoinType.SmartSerial).ToArray();
                var properties = Properties.OfType<PropertyElement>().Where(pr => (pr.PropertyType == JoinType.Serial || pr.PropertyType == JoinType.SmartSerial) && (pr.PropertyMethod == PropertyMethod.Both || pr.PropertyMethod == PropertyMethod.FromPanel)).ToArray();

                for (var i = 0; i < events.Length; i++)
                {
                    var data = events[i].GetData();
                    for (var j = 0; j < data.Length; j++)
                    {
                        var (name, join) = data[j];
                        dataCtor.MethodLines.Add($"{{\"{name}\", (ushort)({join}{(useOffsets ? " + serialOffset" : "")})}}{((j < data.Length - 1 || i < events.Length - 1) ? "," : "")}");
                    }
                }

                for (var i = 0; i < properties.Length; i++)
                {
                    var data = properties[i].GetData();
                    for (var j = 0; j < data.Length; j++)
                    {
                        var (name, join) = data[j];
                        dataCtor.MethodLines.Add($"{{\"{name}\", (ushort)({join}{(useOffsets ? " + serialOffset" : "")})}}{((j < data.Length - 1 || i < properties.Length - 1) ? "," : "")}");
                    }
                }

                dataCtor.MethodLines.Add("};");
            }

            dataClass.Constructors.Add(dataCtor);

            nsb.Classes.Add(dataClass);

            var path = "";
            if (ClassType == ClassType.Touchpanel)
            {
                path = $"{options?.CompilePath}\\{NamespaceBase}\\Panel.g.cs";
            }
            else if (ClassType == ClassType.Page)
            {
                path = $"{options?.CompilePath}\\{NamespaceBase.Replace(options?.RootNamespace, "").Trim('.').Replace(".", "\\")}\\{ClassName}.g.cs";
            }
            else
            {
                path = $"{options?.CompilePath}\\{(ClassType != ClassType.Touchpanel ? $"{Namespace.Replace(options?.RootNamespace, "").Trim('.').Replace(".", "\\")}\\" : "")}{ClassName}.g.cs";
            }


            // This is the end!
            items.Add((ClassName, path, nsb));

            foreach (var cb in Pages)
            {
                foreach (var i in cb.Build($"{Namespace}", ParentPanelClass))
                {
                    items.Add(i);
                }
            }
            return items;
        }

        private static string SanitizeName(string name)
        {
            name = name.Replace(" ", "").Replace(".", "").Replace("-", "");
            while (name.Contains('[') && name.Contains(']'))
            {
                name = name.Replace(name.Substring(name.IndexOf('['), name.IndexOf(']') - name.IndexOf('[')), "");
            }
            name = name.Replace("[", "").Replace("]", "");
            return name;
        }
    }
}
