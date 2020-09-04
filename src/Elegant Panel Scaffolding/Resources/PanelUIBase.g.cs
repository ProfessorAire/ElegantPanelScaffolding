using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.CrestronThread;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Base class for a touchpanel project. Multiple touchpanels can inherit the same project.
    /// You can use a single instance of this class with multiple panels and they will operate as a group, all showing the same things.
    /// If you use multiple instances of this class, the panels associated with each will operate separately.
    /// This makes it easy to configure a piece of equipment to subscribe to multiple panel's commands, but
    /// simplify navigation and other feedback by using a unique instance of helpers (like this project's
    /// Navigation class) to control things like page selections.
    /// </summary>
    public abstract class PanelUIBase : IDisposable, IEnumerable<BasicTriListWithSmartObject>
    {

        #region CoreFunctionality
        /// <summary>
        /// Raised when an exception is encountered and not caught by the user's code.
        /// Provides access to the exception for debugging purposes. This is caught
        /// in order to prevent code exceptions from crashing the touchpanel process.
        /// </summary>
        public event UnhandledExceptionEventHandler UserCodeExceptionEncountered;

        /// <summary>
        /// The basicQueue is used for processing all normal interactions from the panel.
        /// </summary>
        private CrestronQueue<ObjectEventArgs> basicQueue = new CrestronQueue<ObjectEventArgs>(30);
        private Thread basicThread = null;

        /// <summary>
        /// The smartQueue is used for processing all SmartObject interactions from the panel.
        /// </summary>
        private CrestronQueue<ObjectEventArgs> smartQueue = new CrestronQueue<ObjectEventArgs>(30);
        private Thread smartThread = null;

        /// <summary>
        /// If this is subscribed to, it is raised whenever the touchpanel sends data, like a touch or slider movement.
        /// This can be used to reset things like activity timeouts.
        /// </summary>
        public event EventHandler TouchEventReceived;

        /// <summary>
        /// This is a list of panels that have been added to the panel class using the <see cref="AddPanel" /> method.
        /// </summary>
        protected List<BasicTriListWithSmartObject> panels = new List<BasicTriListWithSmartObject>();


        private string description = "";
        /// <summary>
        /// Gets/Sets the Description of the panel. This description will be set on all panels registered with this.
        /// </summary>
        /// <remarks>
        /// If you want to give a touchpanel a unique description you need to set the description after this description is set and you've registered the panel.
        /// </remarks>
        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                foreach (var p in panels.Where(p => p.Registered))
                {
                    p.Description = value;
                }
            }
        }

        /// <summary>
        /// Used by implementing classes to dispose of all child objects as needed.
        /// </summary>
        protected abstract void DisposeChildren();

        /// <summary>
        /// Used by implementing classes to initialize values as needed.
        /// </summary>
        protected abstract void InitializeValues();

        /// <summary>
        /// Adds a <see cref="BasicTriListWithSmartObject"/> to the project.
        /// </summary>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to add to the list.</param>
        public void AddPanel(BasicTriListWithSmartObject panel)
        {
            if (!panels.Contains(panel))
            {
                InitializePanel(panel);
                panels.Add(panel);
                if (isRegisterRequested && !panel.Registered)
                {
                    panel.Register(panel.Name);
                }
                if (panel.Registered)
                {
                    panel.Description = Description;
                }
            }
        }

        /// <summary>
        /// Removes a <see cref="BasicTriListWithSmartObject"/> from the project, optionally unregistering and disposing of it.
        /// </summary>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to remove.</param>
        /// <param name="UnregisterAndDispose">If true will unregister and dispose of the panel after it is removed.</param>
       public void RemovePanel(BasicTriListWithSmartObject panel, bool UnregisterAndDispose)
        {
            if (panels.Contains(panel))
            {
                foreach (var sig in panel.BooleanOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }
                foreach(var sig in panel.UShortOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }
                foreach(var sig in panel.StringOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }
                foreach (var so in panel.SmartObjects)
                {
                    foreach (var sig in so.Value.BooleanOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }
                foreach(var sig in so.Value.UShortOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }
                foreach(var sig in so.Value.StringOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }
                }
                panels.Remove(panel);
                if (UnregisterAndDispose)
                {
                    panel.UnRegister(panel.Name);
                    panel.Dispose();
                }
            }
        }

        /// <summary>
        /// Pulses a basic boolean input on every touchpanel for a period of time.
        /// </summary>
        /// <param name="join">The join # to pulse.</param>
        /// <param name="millisecondDuration">The duration in milliseconds to pulse the join.</param>
        {MethodAccessor} void Pulse(uint join, int millisecondDuration)
        {
            foreach (var panel in panels)
            {
                if (panel.Registered)
                {
                    CrestronInvoke.BeginInvoke(
                        (o) => panel.BooleanInput[join].Pulse(millisecondDuration));
                }
            }
        }

        /// <summary>
        /// Pulses a join on a specific touchpanel for a period of time.
        /// </summary>
        /// <param name="join">The join # to pulse.</param>
        /// <param name="millisecondDuration">The duration in milliseconds to pulse the join.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to pulse the join on.</param>
        {MethodAccessor} void Pulse(uint join, int millisecondDuration, BasicTriListWithSmartObject panel)
        {
            if (panel.Registered)
            {
                CrestronInvoke.BeginInvoke(
                    (o) => panel.BooleanInput[join].Pulse(millisecondDuration));
            }
}

        /// <summary>
        /// Pulses a boolean input on a smart object on every panel for a period of time.
        /// </summary>
        /// <param name="smartID">The ID of the smart object.</param>
        /// <param name="value">The Join # on the smart object to pulse.</param>
        /// <param name="millisecondDuration">The duration in milliseconds to pulse the join.</param>
        {MethodAccessor} void Pulse(uint smartID, uint join, int millisecondDuration)
        {
            foreach (var panel in panels.OfType<BasicTriListWithSmartObject>())
            {
                if (panel.Registered)
                {
                    CrestronInvoke.BeginInvoke(
                        (o) => panel.SmartObjects[smartID].BooleanInput[join].Pulse(millisecondDuration));
                }
            }
}

        /// <summary>
        /// Pulses a boolean input on a smart object on a specific panel for a period of time.
        /// </summary>
        /// <param name="smartID">The ID of the smart object.</param>
        /// <param name="value">The Join # on the smart object to pulse.</param>
        /// <param name="millisecondDuration">The duration in milliseconds to pulse the join.</param>
        /// /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to pulse the join on.</param>
        {MethodAccessor} void Pulse(uint smartID, uint join, int millisecondDuration, BasicTriListWithSmartObject panel)
        {
            if (panel.Registered)
            {
                CrestronInvoke.BeginInvoke(
                    (o) => panel.SmartObjects[smartID].BooleanInput[join].Pulse(millisecondDuration));
            }
}

        /// <summary>
        /// Sends the value to every panel.
        /// </summary>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        {MethodAccessor} void SendValue(uint join, bool value)
        {
            panels.ForEach(panel =>
            {
                if (panel.Registered)
                {
                    panel.BooleanInput[join].BoolValue = value;
                }
            });
}

        /// <summary>
        /// Sends the value to the specific panel.
        /// </summary>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        {MethodAccessor} void SendValue(uint join, bool value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.BooleanInput[join].BoolValue = value;
            }
}

        /// <summary>
        /// Sends the value to every panel.
        /// </summary>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        {MethodAccessor} void SendValue(uint join, ushort value)
        {
            panels.ForEach(panel =>
            {
                if (panel.Registered)
                {
                    panel.UShortInput[join].UShortValue = value;
                }
            });
}

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        {MethodAccessor} void SendValue(uint join, ushort value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.UShortInput[join].UShortValue = value;
            }
}

        /// <summary>
        /// Sends the value to every panel.
        /// </summary>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        {MethodAccessor} void SendValue(uint join, string value)
        {
            panels.ForEach(panel =>
            {
                if (panel.Registered)
                {
                    panel.StringInput[join].StringValue = value;
                }
            });
}

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        {MethodAccessor} void SendValue(uint join, string value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.StringInput[join].StringValue = value;
            }
}

        /// <summary>
        /// Sends the smart value to every panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        {MethodAccessor} void SendSmartValue(uint smartJoin, uint join, bool value)
        {
            panels.ForEach(panel =>
            {
                if (panel.Registered)
                {
                    panel.SmartObjects[smartJoin].BooleanInput[join].BoolValue = value;
                }
            });
}

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        {MethodAccessor} void SendSmartValue(uint smartJoin, uint join, bool value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.SmartObjects[smartJoin].BooleanInput[join].BoolValue = value;
            }
}

        /// <summary>
        /// Sends the smart value to every panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        {MethodAccessor} void SendSmartValue(uint smartJoin, uint join, ushort value)
        {
            panels.ForEach(panel =>
            {
                if (panel.Registered)
                {
                    panel.SmartObjects[smartJoin].UShortInput[join].UShortValue = value;
                }
            });
}

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        {MethodAccessor} void SendSmartValue(uint smartJoin, uint join, ushort value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.SmartObjects[smartJoin].UShortInput[join].UShortValue = value;
            }
}

        /// <summary>
        /// Sends the smart value to every panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        {MethodAccessor} void SendSmartValue(uint smartJoin, uint join, string value)
        {
            panels.ForEach(panel =>
            {
                if (panel.Registered)
                {
                    panel.SmartObjects[smartJoin].StringInput[join].StringValue = value;
                }
            });
}

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        {MethodAccessor} void SendSmartValue(uint smartJoin, uint join, string value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.SmartObjects[smartJoin].StringInput[join].StringValue = value;
            }
        }

        /// <summary>
        /// Adds a SigEventArgs object to the basic processing queue.
        /// </summary>
        /// <param name="currentDevice"></param>
        /// <param name="args"></param>
        protected void Enqueue(GenericBase currentDevice, SigEventArgs args)
        {
            basicQueue.Enqueue(new ObjectEventArgs(args));
        }

        /// <summary>
        /// Adds a SmartObjectEventArgs object to the smart objects processing queue.
        /// </summary>
        /// <param name="currentDevice"></param>
        /// <param name="args"></param>
        protected void Enqueue(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            smartQueue.Enqueue(new ObjectEventArgs(args));
        }

        /// <summary>
        /// Provides data on how many threads this class will use, so the threading requirements can be calculated at the program's startup.
        /// </summary>
        public const int ThreadQuantity = 2;

        /// <summary>
        /// Used internally to track if registration has been requested.
        /// </summary>
        private bool isRegisterRequested = false;

        /// <summary>
        /// Registers all the panels in the internal list. Only call after you're done initializing panels.
        /// </summary>
        public void Register()
        {
            isRegisterRequested = true;
            foreach (var p in panels)
            {
                p.Register(p.Name);
                if (p.Registered)
                {
                    p.Description = Description;
                }
            }
        }

        /// <summary>
        /// UnRegisters all the panels in the internal list.
        /// </summary>
        public void UnRegister()
        {
            isRegisterRequested = false;
            foreach (var p in panels)
            {
                p.UnRegister(p.Name);
            }
        }

        /// <summary>
        /// Used to track if calling disposing, which can be used to ignore certain exceptions.
        /// </summary>
        private bool isDisposing = false;

        /// <summary>
        /// Kills all the threads, disposes of internal objects, unregisters devices and disposes all the panels.
        /// Can be used to teardown all the touchpanel logic at once, instead of manually calling dispose on all
        /// the touchpanels.
        /// </summary>
        public void Dispose()
        {
            isDisposing = true;
            KillThreads();
            smartQueue.Dispose();
            basicQueue.Dispose();
            DisposeChildren();
            UnRegister();
            foreach (var p in panels)
            {
                p.Dispose();
            }
            isDisposing = false;
        }

        /// <summary>
        /// Returns an object from the internal panels list, using the specified index.
        /// </summary>
        /// <param name="index">The index of the object to retrieve.</param>
        /// <returns>Returns a BasicTriListWithSmartObject if any exists at the specified index.</returns>
        public BasicTriListWithSmartObject this[int index]
        {
            get
            {
                if (panels.Count() > 0 && index < panels.Count())
                {
                    return panels[index];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerator object that can iterate over all the panels in the internal list.
        /// </summary>
        /// <returns></returns>
        IEnumerator<BasicTriListWithSmartObject> IEnumerable<BasicTriListWithSmartObject>.GetEnumerator()
        {
            return panels.GetEnumerator();
        }

        /// <summary>
        /// Returns an IEnumerator object that can iterate over all the panels in the internal list.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator IEnumerable.GetEnumerator()
        {
            return panels.GetEnumerator();
        }

        /// <summary>
        /// Starts all the processing threads. Without starting the threads, no touchpanel interactions will surface to the program. Also sends initial values to touchpanels.
        /// </summary>
        public void StartThreads()
        {
            InitializeValues();
            if (basicThread == null)
            {
                basicThread = new Thread(ProcessInputQueue, null, Thread.eThreadStartOptions.Running);
                basicThread.Name = "Panel Processing Thread";
            }
            else
            {
                basicThread.Start();
            }
            if (smartThread == null)
            {
                smartThread = new Thread(ProcessSmartInputQueue, null, Thread.eThreadStartOptions.Running);
                smartThread.Name = "Panel Smart Processing Thread";
            }
            else
            {
                smartThread.Start();
            }
        }

        /// <summary>
        /// Stops and kills all the processing threads.
        /// </summary>
        public void KillThreads()
        {
            if (basicThread != null)
            {
                basicThread.Abort();
                basicQueue.Clear();
            }
            if (smartThread != null)
            {
                smartThread.Abort();
                smartQueue.Clear();
            }
        }

        /// <summary>
        /// Used to check if the TouchEventReceived event needs to be invoked and hands the signal data off to be processed.
        /// </summary>
        private object ProcessInputQueue(object obj)
        {
            while (true)
            {
                try
                {
                    var args = basicQueue.Dequeue();
                    if (args.GetType() == typeof(ObjectEventArgs))
                    {
                        CrestronInvoke.BeginInvoke((s) => { if (TouchEventReceived != null) { TouchEventReceived.Invoke(this, new EventArgs()); } });
                        ProcessSignal(args);
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (System.Threading.ThreadAbortException)
                {
                    CrestronConsole.PrintLine("Thread exiting: {0}", basicThread.Name);
                    if (!isDisposing)
                    {
                        ErrorLog.Notice("Touchpanel Standard Input Thread exited prematurely: {0}", basicThread.Name);
                    }
                }
                catch (Exception ex)
                {
                    if(UserCodeExceptionEncountered != null)
                    {
                         UserCodeExceptionEncountered(this, new UnhandledExceptionEventArgs(ex, false));
                    }
                }
            }
        }

        /// <summary>
        /// Used to check if the TouchEventReceived event needs to be invoked and hands the smart object data off to be processed.
        /// Uses a queue object which blocks the thread while it is waiting for an object to be queued, if none are queued.
        /// </summary>
        private object ProcessSmartInputQueue(object obj)
        {
            while (true)
            {
                try
                {
                    var args = smartQueue.Dequeue();

                    if (args.GetType() == typeof(ObjectEventArgs))
                    {
                        CrestronInvoke.BeginInvoke((s) => { if (TouchEventReceived != null) { TouchEventReceived.Invoke(this, new EventArgs()); } });
                        ProcessSmartSignal(args);
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (System.Threading.ThreadAbortException)
                {
                    CrestronConsole.PrintLine("Thread exited: {0}", smartThread.Name);
                    if (!isDisposing)
                    {
                        ErrorLog.Notice("Touchpanel Smart Input Thread exited prematurely: {0}", smartThread.Name);
                    }
                }
                catch (Exception ex)
                {
                    if (UserCodeExceptionEncountered != null)
                    {
                        UserCodeExceptionEncountered(this, new UnhandledExceptionEventArgs(ex, false));
                    }
                }   
            }

        }

        /// <summary>
        /// Checks to see if the signal has an associated UserObject. If there is no object it returns without doing anything.
        /// The UserObject is the method that gets assigned to it when a panel is being initialized.
        /// This method is used to raise the corresponding event that can be subscribed to for notifications of touchpanel events.
        /// </summary>
        private void ProcessSignal(ObjectEventArgs args)
        {
            if (args.UserObject == null)
            {
                return;
            }
            var methods = (Action<ObjectEventArgs>[])args.UserObject;
            foreach (var method in methods)
            {
                if (method != null)
                {
                    method.Invoke(args);
                }
            }
        }

        /// <summary>
        /// Checks to see if the smart object signal has an associated UserObject. If there is no object it returns without doing anything.
        /// The UserObject is the method that gets assigned to it when a panel is being initialized.
        /// This method is used to raise the corresponding event that can be subscribed to for notifications of touchpanel events.
        /// </summary>
        private void ProcessSmartSignal(ObjectEventArgs args)
        {
            if (args.UserObject == null)
            {
                return;
            }
            var methods = (Action<ObjectEventArgs>[])args.UserObject;
            foreach (var method in methods)
            {
                if (method != null)
                {
                    method.Invoke(args);
                }
            }
        }

        #endregion

        #region SetupAndConfigurationProcessing

        /// <summary>
        /// Tracks all of the <see cref="PanelUIData"/> objects associated with the project.
        /// </summary>
        private List<PanelUIData> PanelData { get; set; }

        /// <summary>
        /// Adds the associated <see cref="PanelUIData"/> object to the project.
        /// </summary>
        /// <param name="data">The <see cref="PanelUIData"/> object to add.</param>
        /// <remarks>Used internally by generated code.</remarks>
        internal void AddData(PanelUIData data)
        {
            if (PanelData == null)
            {
                PanelData = new List<PanelUIData>();
            }
            PanelData.Add(data);
        }

        /// <summary>
        /// Gets a count of the number of boolean outputs are in the panel data list.
        /// </summary>
        /// <param name="join">The join # to check.</param>
        /// <returns>A count of the number found.</returns>
        private int GetBooleanOutputCount(ushort join)
        {
            var count = 0;
            foreach (var data in PanelData)
            {
                if (data.boolOutputs != null)
                {
                    count += data.boolOutputs.Where(o => o.Value == join).Count();
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a count of the number of ushort outputs are in the panel data list.
        /// </summary>
        /// <param name="join">The join # to check.</param>
        /// <returns>A count of the number found.</returns>
        private int GetUShortOutputCount(ushort join)
        {
            var count = 0;
            foreach (var data in PanelData)
            {
                if (data.ushortOutputs != null)
                {
                    count += data.ushortOutputs.Where(o => o.Value == join).Count();
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a count of the number of string outputs are in the panel data list.
        /// </summary>
        /// <param name="join">The join # to check.</param>
        /// <returns>A count of the number found.</returns>
        private int GetStringOutputCount(ushort join)
        {
            var count = 0;
            foreach (var data in PanelData)
            {
                if (data.stringOutputs != null)
                {
                    count += data.stringOutputs.Where(o => o.Value == join).Count();
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a count of the number of smart object boolean outputs are in the panel data list.
        /// </summary>
        /// <param name="smartID">The smart object join # to look on.</param>
        /// <param name="join">The join # to check.</param>
        /// <returns>A count of the number found.</returns>
        private int GetSmartBooleanOutputCount(ushort smartID, ushort join)
        {
            var count = 0;
            foreach (var data in PanelData)
            {
                if (data.smartObjectBoolOutputs != null && data.smartObjectJoin == smartID)
                {
                    count += data.smartObjectBoolOutputs.Where(o => o.Value == join).Count();
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a count of the number of smart object ushort outputs are in the panel data list.
        /// </summary>
        /// <param name="smartID">The smart object join # to look on.</param>
        /// <param name="join">The join # to check.</param>
        /// <returns>A count of the number found.</returns>
        private int GetSmartUShortOutputCount(ushort smartID, ushort join)
        {
            var count = 0;
            foreach (var data in PanelData)
            {
                if (data.smartObjectUShortOutputs != null && data.smartObjectJoin == smartID)
                {
                    count += data.smartObjectUShortOutputs.Where(o => o.Value == join).Count();
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a count of the number of smart object string outputs are in the panel data list.
        /// </summary>
        /// <param name="smartID">The smart object join # to look on.</param>
        /// <param name="join">The join # to check.</param>
        /// <returns>A count of the number found.</returns>
        private int GetSmartStringOutputCount(ushort smartID, ushort join)
        {
            var count = 0;
            foreach (var data in PanelData)
            {
                if (data.smartObjectStringOutputs != null && data.smartObjectJoin == smartID)
                {
                    count += data.smartObjectStringOutputs.Where(o => o.Value == join).Count();
                }
            }
            return count;
        }

        /// <summary>
        /// Handles processing all the data parsing for the touchpanel when it is being added to the project.
        /// </summary>
        /// <param name="targetPanel">The <see cref="BasicTriListWithSmartObject"/> to initialize data for.</param>
        protected void InitializePanel(BasicTriListWithSmartObject targetPanel)
        {
            if (PanelData != null)
            {
                foreach (var data in PanelData)
                {
                    if (data == null)
                    {
                        continue;
                    }
                    var thisType = data.AssociatedClass.GetType().GetCType();
                    if (thisType == null)
                    {
                        continue;
                    }
                    ProcessBoolOutputs(data, thisType, targetPanel);
                    ProcessUShortOutputs(data, thisType, targetPanel);
                    ProcessStringOutputs(data, thisType, targetPanel);

                    if (data.smartObjectJoin > 0)
                    {
                        ProcessSmartBoolOutputs(data, thisType, targetPanel);
                        ProcessSmartUShortOutputs(data, thisType, targetPanel);
                        ProcessSmartStringOutputs(data, thisType, targetPanel);
                    }
                }
                var smartJoins = PanelData.Select((o, a) => o.smartObjectJoin).Where((o, a) => o > 0).Distinct();
                foreach (var sj in smartJoins)
                {
                    targetPanel.SmartObjects[sj].SigChange += Enqueue;
                }
                targetPanel.SigChange += Enqueue;
            }
        }

        /// <summary>
        /// Processes the list of boolean data in the DemoPanelData class.
        /// Since booleans go high and then low, this class assumes that your
        /// list will have a Press and Release method for each join. This is
        /// done so that the press or release event can be fired from within
        /// the same action, instead of requiring multiple actions be fired,
        /// each testing the value of the boolean.
        /// This makes more sense, since you only subscribe to the events
        /// you care about, presses and/or releases, and you don't have to
        /// worry about checking the boolean state.
        /// </summary>
        private void ProcessBoolOutputs(PanelUIData data, CType thisType, BasicTriList targetPanel)
        {
            try
            {
                if (data.boolOutputs == null) { return; }
                for (var i = 0; i < data.boolOutputs.Count; i++)
                {
                    var methodName = data.boolOutputs.ElementAt(i).Key;
                    var item = data.boolOutputs.ElementAt(i);

                    //Find the methods with the associated names in this class. Must be non-public and unique to each class instance.
                    //This is the method that will raise the associated event, and might be null.
                    var eventMethod = thisType.GetMethod(methodName,
                        BindingFlags.NonPublic
                        | BindingFlags.Instance);

                    if (eventMethod == null)
                    {
                        continue;
                    }

                    //Create the action that will be stored in the UserObject.
                    //This action is the code that will be raised whenever the
                    //corresponding join is triggered.
                    var isButton = methodName.Contains("Pressed") || methodName.Contains("Released");
                    var valueCheck = true;
                    if (methodName.Contains("Released")) { valueCheck = false; }
                    Action<ObjectEventArgs> action;
                    if (isButton)
                    {
                        action = new Action<ObjectEventArgs>((b) =>
                        {
                            if (b.BoolValue == valueCheck && eventMethod != null)
                            {
                                //If the join is equal to the check and the event method was found (not null)
                                //then invoke the method that was found. This method will raise the
                                //corresponding event, which is what can be subscribed to by other classes.
                                eventMethod.Invoke(data.AssociatedClass, null);
                            }
                        });
                    }
                    else
                    {
                        action = new Action<ObjectEventArgs>(b =>
                        {
                            if (eventMethod != null)
                            {
                                eventMethod.Invoke(data.AssociatedClass, new object[] { b.BoolValue });
                            }
                        });
                    }
                    //This checks to find the quantity of boolean events in the data list that have the same join #
                    var count = GetBooleanOutputCount(item.Value); //data.boolOutputs.Where((k) => k.Value == item.Value).Count();

                    //Prepare an array of Actions.
                    Action<ObjectEventArgs>[] uo;

                    //If the UserObject for the associated join is null then create a new array, using the object count above.
                    if (targetPanel.BooleanOutput[item.Value].UserObject == null)
                    {
                        uo = new Action<ObjectEventArgs>[count];
                        targetPanel.BooleanOutput[item.Value].UserObject = uo;
                    }
                    //If the UserObject for the associated join isn't null, then retrieve that array.
                    else
                    {
                        uo = (Action<ObjectEventArgs>[])targetPanel.BooleanOutput[item.Value].UserObject;
                    }

                    //Add the Action created above to the list of actions in the next availabe slot.
                    uo[uo.Where((e) => e != null).Count()] = action;

                    //Why use multiple actions? Since buttons on different pages could have the same join, it is up
                    //to the programmer to know where this happens and if they want different actions fired from the
                    //same join. Every action associated with this join will be raised, everytime the join is fired.
                    //There are cases (albeit edge cases) where you may want to be able to subscribe to different events
                    //for different equipment, where it is easier to remember what you want to subscribe to by name,
                    //and having two different events with different names fired from the same join is easier to
                    //remember than an event with a name unrelated to a piece of equipment.

                }
            }
            catch (Exception ex)
            {
                ErrorLog.Notice("Unable to configure Boolean Outputs");
                throw ex;
            }
        }

        /// <summary>
        /// Process the list of ushort value changes on the panel. Unlike
        /// boolean events, only one event is needed, since it is the
        /// value that matters and needs to be passed to equipment.
        /// </summary>
        private void ProcessUShortOutputs(PanelUIData data, CType thisType, BasicTriList targetPanel)
        {
            try
            {
                if (data.ushortOutputs == null) { return; }
                foreach (var item in data.ushortOutputs)
                {
                    var methodName = item.Key;

                    var valueEvent = thisType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (valueEvent == null) { continue; }

                    var action = new Action<ObjectEventArgs>((b) =>
                    {
                        valueEvent.Invoke(data.AssociatedClass, new object[] { b.UShortValue });
                    });

                    var count = GetUShortOutputCount(item.Value); //data.ushortOutputs.Where((k) => k.Value == item.Value).Count();

                    Action<ObjectEventArgs>[] uo;
                    if (targetPanel.UShortOutput[item.Value].UserObject == null)
                    {
                        uo = new Action<ObjectEventArgs>[count];
                        targetPanel.UShortOutput[item.Value].UserObject = uo;
                    }
                    else
                    {
                        uo = (Action<ObjectEventArgs>[])targetPanel.UShortOutput[item.Value].UserObject;
                    }

                    uo[uo.Where((e) => e != null).Count()] = action;

                }
            }
            catch (Exception ex)
            {
                ErrorLog.Notice("Unable to configure Analog Outputs");
                throw ex;
            }
        }

        /// <summary>
        /// Process the list of string value changes on the panel. Unlike
        /// boolean events, only one event is needed, since it is the
        /// value that matters and needs to be passed to equipment.
        /// </summary>
        private void ProcessStringOutputs(PanelUIData data, CType thisType, BasicTriList targetPanel)
        {
            try
            {
                if (data.stringOutputs == null) { return; }
                foreach (var item in data.stringOutputs)
                {
                    if (item.Key == "" || item.Value <= 0)
                    {
                        continue;
                    }
                    var methodName = item.Key;

                    var valueEvent = thisType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (valueEvent == null) { continue; }

                    var action = new Action<ObjectEventArgs>((b) =>
                    {
                        valueEvent.Invoke(data.AssociatedClass, new object[] { b.StringValue });
                    });

                    var count = GetStringOutputCount(item.Value); //data.stringOutputs.Where((k) => k.Value == item.Value).Count();

                    Action<ObjectEventArgs>[] uo;
                    if (targetPanel.StringOutput[item.Value].UserObject == null)
                    {
                        uo = new Action<ObjectEventArgs>[count];
                        targetPanel.StringOutput[item.Value].UserObject = uo;
                    }
                    else
                    {
                        uo = (Action<ObjectEventArgs>[])targetPanel.StringOutput[item.Value].UserObject;
                    }
                    uo[uo.Where((e) => e != null).Count()] = action;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Notice("Unable to configure String Outputs\n" + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Processes the list of SmartObject boolean data.
        /// </summary>
        private void ProcessSmartBoolOutputs(PanelUIData data, CType thisType, BasicTriListWithSmartObject targetPanel)
        {
            try
            {
                if (data.smartObjectBoolOutputs == null) { return; }
                for (var i = 0; i < data.smartObjectBoolOutputs.Count; i += 1)
                {
                    var item = data.smartObjectBoolOutputs.ElementAt(i);
                    var methodName = item.Key;


                    var eventPress = thisType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

                    var isButton = methodName.Contains("Pressed") || methodName.Contains("Released");
                    var valueCheck = true;
                    if (methodName.Contains("Released")) { valueCheck = false; }
                    Action<ObjectEventArgs> action;
                    if (isButton)
                    {
                        action = new Action<ObjectEventArgs>((b) =>
                        {
                            if (b.BoolValue == valueCheck && eventPress != null)
                            {
                                eventPress.Invoke(data.AssociatedClass, null);
                            }
                        });
                    }
                    else
                    {
                        action = new Action<ObjectEventArgs>(b =>
                        {
                            if (eventPress != null)
                            {
                                eventPress.Invoke(data.AssociatedClass, new object[] { b.BoolValue });
                            }
                        });
                    }
                    Action<ObjectEventArgs>[] uo;
                    if (targetPanel.SmartObjects[data.smartObjectJoin].BooleanOutput[item.Value].UserObject == null)
                    {
                        var count = GetSmartBooleanOutputCount(data.smartObjectJoin, item.Value);
                        uo = new Action<ObjectEventArgs>[count];
                        targetPanel.SmartObjects[data.smartObjectJoin].BooleanOutput[item.Value].UserObject = uo;
                    }
                    else
                    {
                        uo = (Action<ObjectEventArgs>[])targetPanel.SmartObjects[data.smartObjectJoin].BooleanOutput[item.Value].UserObject;
                    }

                    uo[uo.Where((e) => e != null).Count()] = action;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Notice("Unable to configure Smart Boolean Outputs");
                throw ex;
            }
        }

        /// <summary>
        /// Processes the list of SmartObject ushort data.
        /// </summary>
        private void ProcessSmartUShortOutputs(PanelUIData data, CType thisType, BasicTriListWithSmartObject targetPanel)
        {
            try
            {
                if (data.smartObjectUShortOutputs == null) { return; }
                foreach (var item in data.smartObjectUShortOutputs)
                {
                    var methodName = item.Key;

                    var valueEvent = thisType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (valueEvent == null) { continue; }

                    var action = new Action<ObjectEventArgs>((b) =>
                    {
                        valueEvent.Invoke(data.AssociatedClass, new object[] { b.UShortValue });
                    });

                    Action<ObjectEventArgs>[] uo;
                    if (targetPanel.SmartObjects[data.smartObjectJoin].UShortOutput[item.Value].UserObject == null)
                    {
                        var count = GetSmartUShortOutputCount(data.smartObjectJoin, item.Value);
                        uo = new Action<ObjectEventArgs>[count];
                        targetPanel.SmartObjects[data.smartObjectJoin].UShortOutput[item.Value].UserObject = uo;
                    }
                    else
                    {
                        uo = (Action<ObjectEventArgs>[])targetPanel.SmartObjects[data.smartObjectJoin].UShortOutput[item.Value].UserObject;
                    }

                    uo[uo.Where((e) => e != null).Count()] = action;

                }
            }
            catch (Exception ex)
            {
                ErrorLog.Notice("Unable to configure Smart Analog Outputs");
                throw ex;
            }
        }

        /// <summary>
        /// Processes the list of SmartObject string data.
        /// </summary>
        private void ProcessSmartStringOutputs(PanelUIData data, CType thisType, BasicTriListWithSmartObject targetPanel)
        {
            try
            {
                if (data.smartObjectStringOutputs == null) { return; }
                foreach (var item in data.smartObjectStringOutputs)
                {
                    var methodName = item.Key;

                    var valueEvent = thisType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (valueEvent == null) { continue; }

                    var action = new Action<ObjectEventArgs>((b) =>
                    {
                        valueEvent.Invoke(data.AssociatedClass, new object[] { b.StringValue });
                    });

                    Action<ObjectEventArgs>[] uo;
                    if (targetPanel.SmartObjects[data.smartObjectJoin].StringOutput[item.Value].UserObject == null)
                    {
                        var count = GetSmartStringOutputCount(data.smartObjectJoin, item.Value);
                        uo = new Action<ObjectEventArgs>[count];
                        targetPanel.SmartObjects[data.smartObjectJoin].StringOutput[item.Value].UserObject = uo;
                    }
                    else
                    {
                        uo = (Action<ObjectEventArgs>[])targetPanel.SmartObjects[data.smartObjectJoin].StringOutput[item.Value].UserObject;
                    }

                    uo[uo.Where((e) => e != null).Count()] = action;

                }
            }
            catch (Exception ex)
            {
                ErrorLog.Notice("Unable to configure Smart String Outputs");
                throw ex;
            }
        }

        #endregion
    }
}