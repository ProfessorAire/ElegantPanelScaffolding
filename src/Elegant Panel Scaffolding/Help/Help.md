# Help

## Preparing your touchpanel project

To obtain a functional set of classes it requires preparing the touchpanel project correctly.

1. All controls must have a name. Use the `Object Name` property in the property pane of the VT-Pro application.
2. The base namespace for the touchpanel classes will be the name of the panel, as set by the `Project Name` property in the VT-Pro application.
3. If a control's join properties are all `0` then the control will be omitted from the compiled classes.
4. If a control's `Object Name` property is set to `null` then it will likewise be omitted from the compiled classes.
   * In this manner an object can be easily omitted if it uses duplicate joins, allowing a join to be set from only one location.
5. Due to the way the classes are generated, it isn't necessary to keep track of the joins being used. It's entirely possible to use nothing but the auto-assign button for assigning join numbers.
6. Traditionally creating complex subpage reference lists involves providing gaps in the numbers used, in case there's need to add additional functionality in the future. Due to the way the classes are generated there's no need to pad the incrementation of join numbers in SRLs, as regenerating the classes with any updates will fix the joins used in the backend.


## Application functionality

1. The `Root Namespace` property should be set to the namespace you want the compiled namespace to be rooted in.
   * If you have a `Root Namespace` of `MyProject.UI.Panels` and the touchpanel's `Object Name` is set to `MyPanel` then the resulting class will have the namespace and path of: `MyProject.UI.Panels.MyPanel.Panel`
2. If including hardkeys (which is optional) you can choose to give each of them names, or provide a common prefix. If no names are provided the prefix is used, so a prefix of `"Hardkey"` will result in events such as `Hardkey1_Pressed` and `Hardkey2_Pressed`. If you do provide names, the events will instead be named like `Power_Pressed` and `Home_Pressed`. Currently these names are provided as a comma separated list, which requires a placeholder for every button, even those not being used.
   * Future updates will change this feature, to provide a more customizable method for providing only certain buttons.

## Generated classes

1. Generated class files are named with the following format `ClassName.g.cs`.
2. All classes are generated as Partial classes, which allows you to create additional partial implementations to place UI logic where it belongs, without having to worry about overwriting changes when regenerating the classes.
   * If working in Visual Studio 2017 or 2019 you can use the [File Nesting](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.FileNesting) extension by Mads Kristensen to configure the generated files to nest underneath any files you create, which helps keeps your Solution Explorer more organized.
3. General class structure is as follows:
   * `PanelNamespace`
     * `Panel.g.cs`
     * `Components`
       * `PageName1.g.cs`
         * `PageName1Components`
           * `Control1.g.cs`
           * `Control2.g.cs`
           * `Subpage1.g.cs`
           * `Subpage1Components`
             * `Control1.g.cs`
             * `Control2.g.cs`  
       * `PageName2.g.cs`
         * `PageName2Components`
           * `Control3.g.cs`
           * `Control2.g.cs`
           * `Control1.g.cs`
4. Duplicate names are allowed, but the application does not check for them! If you have two controls on the same page or subpage with the same name, you'll only get generated code for one of those controls. This will lead to potential conflicts. Make sure you only use duplicate names across different pages or subpages.
5. The following `partial` methods are generated for each class.
   * `partial void _Setup()`
     * This can be declared in any custom class implementation and it will get called at the end of the class' constructor. This happens *before* any connections to touchpanels are made, so you have to be careful to *not* attempt to set any properties that would send information to a touchpanel. This information will not make it to the touchpanel and you may notice the system being out of sync from what you expect.
   * `partial void _Dispose()`
     * All generated classes implement IDisposable, in order to allow a single call to the root panel's `Dispose()` method to properly walk through the children disposing of them. If you need to dispose of any objects you declare in your partial class declarations, this is the way to do so.
   * ` partial void _InitializeValues()`
     * This method is called whenever the root touchpanel object is started via the `StartThreads()` method. Use this when you need to explicitly set a startup value for control properties.

## Registering and starting/stopping the panel functionality

```csharp
public void DemonstratePanelFunctionality()
{
    //Initializes the class.
    MainPanels = new MyNamespace.UI.MainPanel.Panel();

    // Adds as many physical hardware panels as are associated with the class.
    MainPanels.AddPanel(new Tsw1060(0x03, this));
    MainPanels.AddPanel(new XPanelForSmartGraphics(0x04, this));

    // The panels added are accessible via the indexer (and enumerable) so you can do things like...
    MainPanels[0].Description = "Kitchen Panel";
    MainPanels[1].Description = "Kitchen XPanel";

    // Or...
    foreach (var panel in OfficePanels)
    {
        panel.OnlineStatusChange += (p, a) => CrestronConsole.PrintLine("The {0} with Id {1} is {2}.",
            string.IsNullOrEmpty(p.Description) ? "Panel" : p.Description,
            p.ID,
            a.DeviceOnLine ? "Online" : "Offline");
    }

    // Registers all the panels at once.
    MainPanels.Register();

    // Starts the threads.
    MainPanels.StartThreads();

    // Stops the panel threads. At this point any incoming data from the panel will be lost and not make it to the program. (Button presses, slider changes, text entry, etc.)
    MainPanels.KillThreads();

    // Unregisters all of the panels at once.
    MainPanels.UnRegister();
}

// Make sure to dispose of the panel when the program is stopping. This ensures that the threads are properly stopped and the underlying panels unregistered and disposed of.
void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
{
    switch (programStatusEventType)
    {
        case (eProgramStatusEventType.Stopping):
            MainPanels.Dispose();
            break;
    }
}
                    
```