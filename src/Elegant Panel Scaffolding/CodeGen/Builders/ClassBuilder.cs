using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EPS.CodeGen.Builders
{
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

        protected List<ClassBuilder> Pages { get; } = new List<ClassBuilder>();
        protected List<ClassBuilder> Controls { get; } = new List<ClassBuilder>();
        protected List<ListBuilder> Lists { get; } = new List<ListBuilder>();

        protected List<Writers.EventWriter> EventWriters { get; } = new List<Writers.EventWriter>();
        protected List<Writers.FieldWriter> FieldWriters { get; } = new List<Writers.FieldWriter>();
        protected List<Writers.PropertyWriter> PropertyWriters { get; } = new List<Writers.PropertyWriter>();
        protected List<Writers.MethodWriter> MethodWriters { get; } = new List<Writers.MethodWriter>();
        protected List<Writers.TextWriter> OtherWriters { get; } = new List<Writers.TextWriter>();

        protected List<JoinBuilder> Joins { get; } = new List<JoinBuilder>();

        public bool IsValid
        {
            get
            {
                if (!string.IsNullOrEmpty(ClassName) && (!ClassName.StartsWith("null", StringComparison.InvariantCultureIgnoreCase)) &&
                    (Pages.Count > 0 ||
                    Controls.Count > 0 ||
                    Joins.Count > 0))
                {
                    return true;
                }
                return false;
            }
        }

        public ClassType ClassType { get; set; } = ClassType.Touchpanel;

        public ClassBuilder(ClassType classType) => ClassType = classType;

        public void AddJoin(JoinBuilder join)
        {
            if (!Joins.Contains(join))
            {
                Joins.Add(join);
            }
        }

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

            if (writer is Writers.TextWriter tw && !OtherWriters.Where(e => e.Text == tw.Text).Any())
            {
                OtherWriters.Add(tw);
                return;
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
                NamespaceBase = Namespace;
                Namespace = $"{Namespace}.{ClassName}Components";
            }
            else
            {
                nsb = new Writers.NamespaceWriter(Namespace);
            }

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            nsb.AddHeader("// <auto-generated>");
            nsb.AddHeader($"//\t\tThis code was generated with {asm.GetName().Name}.");
            nsb.AddHeader($"//\t\tApplication Version: {asm.GetName().Version}");
            nsb.AddHeader($"//\t\tRuntime Version: {asm.ImageRuntimeVersion}");
            nsb.AddHeader("//");
            nsb.AddHeader($"//\t\tChanges to this file may break expected behavior and will be lost if the code is regenerated.");
            nsb.AddHeader($"//\t\tCreate a new file with a new partial declaration of the contents of this file and edit that instead.");
            nsb.AddHeader($"//\t\tYou can use the '_Setup()`, '_InitializeValues()', and '_Dispose()' partial methods to hook into the construction, initialization, and disposal logic.");
            nsb.AddHeader($"// </auto-generated>");

            nsb.AddUsing("System");
            nsb.AddUsing("System.Collections.Generic");

            if (ClassType != ClassType.Touchpanel)
            {
                nsb.AddUsing("Crestron.SimplSharpPro.DeviceSupport");
            }

            nsb.AddUsing($"{options.PanelNamespace}.Core");

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

            // Partial Initialize Values Method.
            var partialInit = new Writers.MethodWriter("_InitializeValues", "Implement in accompanying classes in order to send initial values to the touchpanels when the root class Threads are started.", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialInit);

            // Parent Panel.
            if (ClassType != ClassType.Touchpanel)
            {
                var parentPanel = new Writers.FieldWriter("ParentPanel", "Panel", 2);
                parentPanel.Help.Summary = $"The <see cref=\"Panel\"/> that this object belongs to.";
                mainClass.Fields.Add(parentPanel);
                ctor.MethodLines.Add("ParentPanel = parent;");
                ctor.AddParameter("Panel", "parent", "The class that is the base parent of this one.");
            }

            // Pages and Controls
            if (ClassType == ClassType.SrlElement)
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

                if (ctor.MethodLines.Last().Length > 0)
                {
                    ctor.MethodLines.Add("");
                }

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

                for (var i = 0; i < Controls.Count; i++)
                {
                    ctor.MethodLines.Add($"{Controls[i].ClassName.Replace(ClassName, "")} = new {Controls[i].ClassName}(ParentPanel, this.digitalOffset, this.analogOffset, this.serialOffset, this.itemOffset);");
                }
            }
            else if (ClassType == ClassType.Control && (AnalogOffset > 0 || DigitalOffset > 0 || SerialOffset > 0))
            {
                foreach(var c in Controls)
                {
                    c.AnalogOffset = AnalogOffset;
                    c.DigitalOffset = DigitalOffset;
                    c.SerialOffset = SerialOffset;
                }

                foreach(var j in Joins)
                {
                    j.AnalogOffset = AnalogOffset;
                    j.DigitalOffset = DigitalOffset;
                    j.SerialOffset = SerialOffset;
                }
            }
            else if (ClassType == ClassType.Touchpanel)
            {
                foreach (var p in Pages)
                {
                    ctor.MethodLines.Add($"{p.ClassName} = new Components.{p.ClassName}(this);");
                }
                foreach (var c in Controls)
                {
                    ctor.MethodLines.Add($"{c.ClassName} = new Components.{c.ClassName}(this);");
                }
            }
            else if (ClassType == ClassType.Page)
            {
                foreach (var p in Pages)
                {
                    ctor.MethodLines.Add($"{p.ClassName} = new {ClassName}Components.{p.ClassName}(ParentPanel);");
                }
                foreach (var c in Controls)
                {
                    ctor.MethodLines.Add($"{c.ClassName} = new {ClassName}Components.{c.ClassName}(ParentPanel);");
                }
            }
            
            if (ClassType == ClassType.Control || ClassType == ClassType.SmartObject)
            {
                foreach (var l in Lists)
                {
                    foreach (var w in l.GetWriters())
                    {
                        AddWriter(w);
                    }
                }
            }

            // Before adding the ctor, all the TextWriters should be providing constructor lines, so we'll add them there.
            foreach (var w in OtherWriters)
            {
                if (ctor.MethodLines.Last().Length > 0)
                {
                    ctor.MethodLines.Add("");
                }

                ctor.MethodLines.Add(w.ToString());
            }

            // For any events from the panel we need to add Actions.
            if (ctor.MethodLines.Last().Length > 0)
            {
                ctor.MethodLines.Add("");
            }

            foreach (var j in Joins)
            {
                var text = j.GetInitializers().ToString();

                if (ClassType == ClassType.Touchpanel)
                {
                    text = text.Replace("ParentPanel.Actions", "Actions");
                }
                else if (ClassType == ClassType.SrlElement && !string.IsNullOrWhiteSpace(text))
                {
                    static string ProcessText(string text, int offset)
                    {
                        var i1 = text.IndexOf('(', offset) + 1;
                        var i2 = text.IndexOf(',', offset);
                        text = text.Insert(i2, ")");
                        if (text.Contains("Bool"))
                        {
                            text = text.Insert(i1, "(uint)(this.digitalOffset + ");
                        }
                        else if (text.Contains("UShort"))
                        {
                            text = text.Insert(i1, "(uint)(this.analogOffset + ");
                        }
                        else if (text.Contains("String"))
                        {
                            text = text.Insert(i1, "(uint)(this.serialOffset + ");
                        }

                        return text;
                    }

                    var index = 0;

                    while (index > -1)
                    {
                        text = ProcessText(text, index);
                        index++;
                        index = text.IndexOf("\r", index, StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    ctor.MethodLines.Add(text);
                }
            }

            // Call _Setup Last
            if (ctor.MethodLines.Last().Length > 0)
            {
                ctor.MethodLines.Add("");
            }

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

            foreach (var join in Joins)
            {
                join.IsListElement = ClassType == ClassType.SrlElement;
                foreach (var w in join.GetWriters())
                {
                    AddWriter(w);
                }
            }

            // Other Pages
            foreach (var c in Pages)
            {
                var typeName = string.Empty;

                if (ClassType == ClassType.Touchpanel)
                {
                    typeName = $"Components.{c.ClassName}";
                }
                else
                {
                    typeName = $"{ClassName}Components.{c.ClassName}";
                }

                var fw = new Writers.FieldWriter(c.ClassName, typeName);
                fw.Help.Summary = $"Provides access to the {c.ClassName} Page.";
                FieldWriters.Add(fw);
                initValuesMethod.MethodLines.Add($"{c.ClassName}.InitializeValues();");
            }

            // Other Controls
            foreach (var c in Controls)
            {
                var typeName = string.Empty;

                if (ClassType == ClassType.Touchpanel || ClassType == ClassType.SrlElement)
                {
                    typeName = c.ClassName;
                }
                else
                {
                    typeName = $"{ClassName}Components.{c.ClassName}";
                }

                var fw = new Writers.FieldWriter(c.ClassName, typeName);
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

            // Build Clear All Event Subscriptions method.
            //var fromJoins = Joins; //.Where(j => j.JoinDirection == JoinDirection.FromPanel || j.JoinDirection == JoinDirection.Both);
            //if (fromJoins.Any())
            //{
            //    var subMW = new Writers.MethodWriter("ClearAllEventSubscriptions", "Clears all of the subscriptions on events.", "void");

            //    foreach (var j in fromJoins)
            //    {
            //        if (!(j.JoinType == JoinType.DigitalButton))
            //        {
            //            if (j.JoinType == JoinType.SmartDigitalButton || j.JoinType == JoinType.DigitalButton)
            //            {
            //                subMW.MethodLines.Add($"{j.JoinName}Pressed = null;");
            //                subMW.MethodLines.Add($"{j.JoinName}Released = null;");
            //            }
            //            else if (j.ChangeEventName != "Changed" && !string.IsNullOrEmpty(j.ChangeEventName))
            //            {
            //                subMW.MethodLines.Add($"{j.ChangeEventName} = null;");
            //            }
            //            else
            //            {
            //                System.Diagnostics.Debug.Print($"Just a Changed Event: {j.JoinName}");
            //            }
            //        }
            //        else
            //        {
            //            subMW.MethodLines.Add($"{j.JoinName}Pressed = null;");
            //            subMW.MethodLines.Add($"{j.JoinName}Released = null;");
            //        }
            //    }

            //    mainClass.Methods.Add(subMW);
            //}

            // Build Dispose Method


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
                if (ClassType == ClassType.SrlElement)
                {
                    disp.MethodLines.Add($"{c.ClassName.Replace(ClassName, "")}.Dispose();");
                }
                else
                {
                    disp.MethodLines.Add($"{c.ClassName}.Dispose();");
                }
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

            //if (fromJoins.Any())
            //{
            //    disp.MethodLines.Add($"ClearAllEventSubscriptions();");
            //}

            mainClass.Methods.Add(disp);

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

            // Add the writers to the class.
            mainClass.Properties.AddRange(PropertyWriters);
            mainClass.Fields.AddRange(FieldWriters);
            mainClass.Events.AddRange(EventWriters);
            mainClass.Methods.AddRange(MethodWriters);

            nsb.Classes.Add(mainClass);

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
