using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

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
        public string ClassName { get => SanitizeName(this.className); set => this.className = value; }
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
                if (!string.IsNullOrEmpty(this.ClassName) && (!this.ClassName.StartsWith("null", StringComparison.InvariantCultureIgnoreCase)) &&
                    (this.Pages.Count > 0 ||
                    this.Controls.Count > 0 ||
                    this.Joins.Count > 0))
                {
                    return true;
                }
                return false;
            }
        }

        public ClassType ClassType { get; set; } = ClassType.Touchpanel;

        public ClassBuilder(ClassType classType) => this.ClassType = classType;

        public void AddJoin(JoinBuilder join)
        {
            if (!this.Joins.Contains(join))
            {
                this.Joins.Add(join);
            }
        }

        public void AddWriter(Writers.WriterBase writer)
        {
            if (writer is Writers.EventWriter ew && !this.EventWriters.Where(e => e.Name == ew.Name).Any())
            {
                this.EventWriters.Add(ew);
                return;
            }

            if (writer is Writers.FieldWriter fw && !this.FieldWriters.Where(e => e.Name == fw.Name).Any())
            {
                this.FieldWriters.Add(fw);
                return;
            }

            if (writer is Writers.PropertyWriter pw && !this.PropertyWriters.Where(e => e.Name == pw.Name).Any())
            {
                this.PropertyWriters.Add(pw);
                return;
            }

            if (writer is Writers.MethodWriter mw && !this.MethodWriters.Where(e => e.Name == mw.Name).Any())
            {
                this.MethodWriters.Add(mw);
                return;
            }

            if (writer is Writers.TextWriter tw && !this.OtherWriters.Where(e => e.Text == tw.Text).Any())
            {
                this.OtherWriters.Add(tw);
                return;
            }

        }

        public void AddPage(ClassBuilder page)
        {
            if (page?.IsValid ?? false && !page.ClassName.ToUpperInvariant().Contains("NULL"))
            {
                if (page != null)
                {
                    this.Pages.Add(page);
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
                this.Controls.Add(control);
            }
        }

        public void AddList(ListBuilder list)
        {
            if (list != null && !list.Name.ToUpperInvariant().Contains("NULL"))
            {
                this.Lists.Add(list);
            }
        }

        public List<(string className, string classPath, Writers.NamespaceWriter nameSpace)> Build(string rootNamespace = "", string ParentPanelClass = "PanelUIBase")
        {
            var options = Options.Current;
            var items = new List<(string className, string classPath, Writers.NamespaceWriter nameSpace)>();
            if (options == null) { return items; }
            if (!this.Namespace.StartsWith(this.NamespaceBase, StringComparison.InvariantCulture))
            {
                this.Namespace = this.NamespaceBase;
            }
            if (!string.IsNullOrEmpty(rootNamespace))
            {
                this.Namespace = rootNamespace;
            }

            Writers.NamespaceWriter nsb;

            if (this.ClassType == ClassType.Touchpanel)
            {
                nsb = new Writers.NamespaceWriter($"{this.Namespace}.{this.ClassName}");
                this.Namespace = $"{this.Namespace}.{this.ClassName}.Components";
                this.NamespaceBase = this.ClassName;
            }
            else if (this.ClassType == ClassType.Page)
            {
                nsb = new Writers.NamespaceWriter($"{this.Namespace}");
                this.NamespaceBase = this.Namespace;
                this.Namespace = $"{this.Namespace}.{this.ClassName}Components";
            }
            else
            {
                nsb = new Writers.NamespaceWriter(this.Namespace);
            }

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            nsb.AddHeader("// <auto-generated>");
            nsb.AddHeader($"//\t\tThis code was generated with {asm.GetName().Name}.");
            nsb.AddHeader($"//\t\tApplication Version: {asm.GetName().Version}");
            nsb.AddHeader($"//\t\tRuntime Version: {asm.ImageRuntimeVersion}");
            nsb.AddHeader("//");
            nsb.AddHeader($"//\t\tChanges to this file may break expected behavior and will be lost if the code is regenerated.");
            nsb.AddHeader($"//\t\tCreate a new file with a new partial declaration of the contents of this file and edit that instead.");
            nsb.AddHeader($"//\t\tYou can use the 'SetupUi()`, 'InitializeUi()', and 'DisposeUi()' partial methods to hook into the construction, initialization, and disposal logic.");
            nsb.AddHeader($"// </auto-generated>");

            nsb.AddUsing("System");
            nsb.AddUsing("System.Collections.Generic");

            if (this.ClassType != ClassType.Touchpanel)
            {
                nsb.AddUsing("Crestron.SimplSharpPro.DeviceSupport");
            }

            nsb.AddUsing("Evands.EPS.Common");

            Writers.ClassWriter mainClass;

            if (this.ClassType == ClassType.Touchpanel)
            {
                this.ClassName = "Panel";
                mainClass = new Writers.ClassWriter("Panel") { ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged };
            }
            else if (this.ClassType == ClassType.SrlElement)
            {
                mainClass = new Writers.ClassWriter($"{this.ClassName}") { ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged };
            }
            else
            {
                mainClass = new Writers.ClassWriter(this.ClassName) { ImplementINotifyPropertyChanged = Options.Current.ImplementINotifyPropertyChanged };
            }

            mainClass.Modifier = Modifier.Partial;
            if (this.ClassType == ClassType.Touchpanel)
            {
                mainClass.Implements.Add("PanelUIBase");
            }
            else
            {
                mainClass.Implements.Add(nameof(IDisposable));
            }

            // Main Class Constructor
            var ctor = new Writers.MethodWriter(this.ClassName, "Creates a new instance of the class.", "", 2);

            // Partial SetupUi method.
            var partialSetup = new Writers.MethodWriter("SetupUi", "Implement this in accompanying classes in order to setup functionality on the construction of the root class.\nNo values should be sent to this touchpanel in this method.", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialSetup);

            // Partial DisposeUi method.
            var partialDispose = new Writers.MethodWriter("DisposeUi", "Implement this in accompanying classes in order to dispose of objects as needed.", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialDispose);

            // Partial Initialize Values Method.
            var partialInit = new Writers.MethodWriter("InitializeUi", "Implement in accompanying classes in order to send initial values to the touchpanels when the root class Threads are started.", "void", 2)
            {
                Modifier = Modifier.Partial,
                Accessor = Accessor.None
            };
            mainClass.Methods.Add(partialInit);

            // Parent Panel.
            if (this.ClassType != ClassType.Touchpanel)
            {
                var parentPanel = new Writers.FieldWriter("ParentPanel", "Panel", 2);
                parentPanel.Help.Summary = $"The <see cref=\"Panel\"/> that this object belongs to.";
                mainClass.Fields.Add(parentPanel);
                ctor.MethodLines.Add("ParentPanel = parent;");
                ctor.AddParameter("Panel", "parent", "The class that is the base parent of this one.");
            }

            // Pages and Controls
            if (this.ClassType == ClassType.SrlElement)
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

                for (var i = 0; i < this.Controls.Count; i++)
                {
                    ctor.MethodLines.Add($"{this.Controls[i].ClassName.Replace(this.ClassName, "")} = new {this.Controls[i].ClassName}(ParentPanel, this.digitalOffset, this.analogOffset, this.serialOffset, this.itemOffset);");
                }
            }
            else if (this.ClassType == ClassType.Control && (this.AnalogOffset > 0 || this.DigitalOffset > 0 || this.SerialOffset > 0))
            {
                foreach (var c in this.Controls)
                {
                    c.AnalogOffset = this.AnalogOffset;
                    c.DigitalOffset = this.DigitalOffset;
                    c.SerialOffset = this.SerialOffset;
                }

                foreach (var j in this.Joins)
                {
                    j.AnalogOffset = this.AnalogOffset;
                    j.DigitalOffset = this.DigitalOffset;
                    j.SerialOffset = this.SerialOffset;
                }
            }
            else if (this.ClassType == ClassType.Touchpanel)
            {
                foreach (var p in this.Pages)
                {
                    ctor.MethodLines.Add($"{p.ClassName} = new Components.{p.ClassName}(this);");
                }
                foreach (var c in this.Controls)
                {
                    ctor.MethodLines.Add($"{c.ClassName} = new Components.{c.ClassName}(this);");
                }
            }
            else if (this.ClassType == ClassType.Page)
            {
                foreach (var p in this.Pages)
                {
                    ctor.MethodLines.Add($"{p.ClassName} = new {this.ClassName}Components.{p.ClassName}(ParentPanel);");
                }
                foreach (var c in this.Controls)
                {
                    ctor.MethodLines.Add($"{c.ClassName} = new {this.ClassName}Components.{c.ClassName}(ParentPanel);");
                }
            }

            if (this.ClassType is ClassType.Control or ClassType.SmartObject)
            {
                foreach (var l in this.Lists)
                {
                    foreach (var w in l.GetWriters())
                    {
                        this.AddWriter(w);
                    }

                    mainClass.Implements.Add($"Evands.EPS.Common.IListItemProvider<{l.Control.ClassName}>");
                }
            }

            // Before adding the ctor, all the TextWriters should be providing constructor lines, so we'll add them there.
            foreach (var w in this.OtherWriters)
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

            foreach (var j in this.Joins)
            {
                var text = j.GetInitializers().ToString();

                if (this.ClassType == ClassType.Touchpanel)
                {
                    text = text.Replace("ParentPanel.Actions", "Actions");
                }
                else if (this.ClassType == ClassType.SrlElement && !string.IsNullOrWhiteSpace(text))
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

            // Call SetupUi Last
            if (ctor.MethodLines.Last().Length > 0)
            {
                ctor.MethodLines.Add("");
            }

            ctor.MethodLines.Add("SetupUi();");

            mainClass.Constructors.Add(ctor);

            // Completed Constructor

            // Initialize Values Method
            var initValuesMethod = new Writers.MethodWriter("InitializeValues", "Attempts to initialize values for the current class.", "void", 2);

            if (this.ClassType == ClassType.Touchpanel)
            {
                initValuesMethod.Accessor = Accessor.Protected;
                initValuesMethod.Modifier = Modifier.Override;
            }
            else
            {
                initValuesMethod.Accessor = Accessor.Internal;
            }

            initValuesMethod.MethodLines.Add("InitializeUi();");

            foreach (var join in this.Joins)
            {
                join.IsListElement = this.ClassType == ClassType.SrlElement;
                foreach (var w in join.GetWriters())
                {
                    this.AddWriter(w);
                }
            }

            // Other Pages
            foreach (var c in this.Pages)
            {
                var typeName = string.Empty;

                if (this.ClassType == ClassType.Touchpanel)
                {
                    typeName = $"Components.{c.ClassName}";
                }
                else
                {
                    typeName = $"{this.ClassName}Components.{c.ClassName}";
                }

                var fw = new Writers.FieldWriter(c.ClassName, typeName);
                fw.Help.Summary = $"Provides access to the {c.ClassName} Page.";
                this.FieldWriters.Add(fw);
                initValuesMethod.MethodLines.Add($"{c.ClassName}.InitializeValues();");
            }

            // Other Controls
            foreach (var c in this.Controls)
            {
                var typeName = string.Empty;

                if (this.ClassType is ClassType.Touchpanel or ClassType.SrlElement)
                {
                    typeName = c.ClassName;
                }
                else
                {
                    typeName = $"{this.ClassName}Components.{c.ClassName}";
                }

                var fw = new Writers.FieldWriter(c.ClassName, typeName);
                if (this.ClassType is ClassType.SrlElement)
                {
                    fw.Name = c.ClassName.Replace(this.ClassName, "");
                }

                fw.Help.Summary = $"Provides access to the {fw.Name} Control.";
                this.FieldWriters.Add(fw);
                initValuesMethod.MethodLines.Add($"{fw.Name}.InitializeValues();");
            }

            initValuesMethod.MethodLines.Add("this.IsUiInitialized = true;");

            // Add initialize values method
            mainClass.Methods.Add(initValuesMethod);

            // Dispose Method
            Writers.MethodWriter disp;
            if (this.ClassType == ClassType.Touchpanel)
            {
                disp = new Writers.MethodWriter("DisposeChildren", "Calls the partial void DisposeUi in order to allow disposing of custom objects.", "void", 2)
                {
                    Accessor = Accessor.Protected,
                    Modifier = Modifier.Override
                };
            }
            else
            {
                disp = new Writers.MethodWriter("Dispose", "Calls the partial void DisposeUi in order to allow disposing of custom objects.", "void", 2)
                {
                    Accessor = Accessor.Public,
                    Modifier = Modifier.None
                };
            }

            disp.MethodLines.Add("DisposeUi();");

            foreach (var p in this.Pages)
            {
                disp.MethodLines.Add($"{p.ClassName}.Dispose();");
            }

            foreach (var c in this.Controls)
            {
                if (this.ClassType == ClassType.SrlElement)
                {
                    disp.MethodLines.Add($"{c.ClassName.Replace(this.ClassName, "")}.Dispose();");
                }
                else
                {
                    disp.MethodLines.Add($"{c.ClassName}.Dispose();");
                }
            }

            if (this.Lists.Count > 0)
            {
                disp.MethodLines.Add($"foreach (var i in Items)");
                disp.MethodLines.Add("{");
                foreach (var l in this.Lists)
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

            foreach (var c in this.Controls)
            {
                if (this.ClassType == ClassType.SrlElement)
                {
                    c.ClassType = ClassType.SrlElement;
                }
                var built = c.Build($"{this.Namespace}", ParentPanelClass);
                foreach (var builder in built)
                {
                    items.Add(builder);
                }
            }

            foreach (var l in this.Lists)
            {
                var built = l.Control.Build($"{this.Namespace}", ParentPanelClass);
                foreach (var builder in built)
                {
                    items.Add(builder);
                }
            }

            // Add the writers to the class.
            mainClass.Properties.AddRange(this.PropertyWriters);
            mainClass.Properties.Add(new Writers.PropertyWriter("IsUiInitialized", "bool", false) { Accessor = Accessor.Protected, PrivateSetter = true, Help = new Writers.HelpWriter { Summary = "Gets a value indicating whether the InitializeUi method has been called." } });
            mainClass.Fields.AddRange(this.FieldWriters);
            mainClass.Events.AddRange(this.EventWriters);
            mainClass.Methods.AddRange(this.MethodWriters);

            nsb.Classes.Add(mainClass);

            if (mainClass.Properties.Count > 0)
            {
                nsb.AddUsing("System.ComponentModel");
            }

            var path = "";
            if (this.ClassType == ClassType.Touchpanel)
            {
                path = $"{options?.CompilePath}\\{this.NamespaceBase}\\Panel.g.cs";
            }
            else if (this.ClassType == ClassType.Page)
            {
                path = $"{options?.CompilePath}\\{this.NamespaceBase.Replace(options?.RootNamespace, "").Trim('.').Replace(".", "\\")}\\{this.ClassName}.g.cs";
            }
            else
            {
                path = $"{options?.CompilePath}\\{(this.ClassType != ClassType.Touchpanel ? $"{this.Namespace.Replace(options?.RootNamespace, "").Trim('.').Replace(".", "\\")}\\" : "")}{this.ClassName}.g.cs";
            }


            // This is the end!
            items.Add((this.ClassName, path, nsb));

            foreach (var cb in this.Pages)
            {
                foreach (var i in cb.Build($"{this.Namespace}", ParentPanelClass))
                {
                    items.Add(i);
                }
            }
            return items;
        }

        private static string SanitizeName(string name)
        {
            var saniRegex = new Regex(@"(?:\[.*\](?<g2>[^\]]+))|(?:\[(?<g1>[^[\]]*)])");
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Replace(" ", "").Replace(".", "").Replace("-", "");

                var match = saniRegex.Match(name);
                if (match.Success)
                {
                    if (match.Groups["g1"].Success)
                    {
                        name = match.Groups["g1"].Value;
                    }
                    else if (match.Groups["g2"].Success)
                    {
                        name = match.Groups["g2"].Value;
                    }
                }

                name = name.Replace("[", "").Replace("]", "");
                return name;
            }

            return string.Empty;
        }
    }
}
