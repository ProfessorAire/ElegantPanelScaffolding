using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.CrestronThread;
using SharpProTouchpanelDemo.UI.Core;

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
    public abstract class PanelUIBase : IDisposable, IEnumerable<BasicTriList>, IEnumerable<BasicTriListWithSmartObject>
    {
        #region CoreFunctionality

        /// <summary>
        /// Provides data on how many threads this class will use, so the threading requirements can be calculated at the program's startup.
        /// </summary>
        public const int ThreadQuantity = 2;

        /// <summary>
        /// Used internally to track if registration has been requested.
        /// </summary>
        private bool isRegisterRequested = false;

        /// <summary>
        /// The queue is used for processing all normal interactions from the panel.
        /// </summary>
        private CrestronQueue<Action> panelProcessingQueue = new CrestronQueue<Action>(30);

        /// <summary>
        /// Processing thread.
        /// </summary>
        private Thread processingThread = null;

        /// <summary>
        /// If this is subscribed to, it is raised whenever the touchpanel sends data, like a touch or slider movement.
        /// This can be used to reset things like activity timeouts.
        /// </summary>
        public event EventHandler TouchEventReceived;

        /// <summary>
        /// This is a list of panels that have been added to the panel class using the <see cref="AddPanel" /> method.
        /// </summary>
        protected List<BasicTriList> panels = new List<BasicTriList>();

        /// <summary>
        /// Holder for Actions associated with classes.
        /// </summary>
        internal PanelActions Actions { get; private set; }

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
                foreach (var p in panels.Where(p => !p.Registered))
                {
                    p.Description = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the object has been disposed.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the object has been started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PanelUIBase"/> class.
        /// </summary>
        public PanelUIBase()
        {
            Actions = new PanelActions();
        }

        /// <summary>
        /// Raised when an exception is encountered and not caught by the user's code.
        /// Provides access to the exception for debugging purposes. This is caught
        /// in order to prevent code exceptions from crashing the touchpanel process.
        /// </summary>
        public event UnhandledExceptionEventHandler UserCodeExceptionEncountered;

        /// <summary>
        /// Adds a <see cref="BasicTriList"/> to the project.
        /// </summary>
        /// <param name="panel">The <see cref="BasicTriList"/> to add to the list.</param>
        public virtual void AddPanel(BasicTriList panel)
        {
            if (!panels.Contains(panel))
            {
                ConfigureSigEvents(panel);
                panels.Add(panel);

                if (!panel.Registered)
                {
                    panel.Description = Description;
                }

                if (isRegisterRequested && !panel.Registered)
                {
                    _Register(panel);
                    panel.Register(!string.IsNullOrEmpty(panel.Description) ? panel.Description : panel.Name);
                }
            }
        }

        /// <summary>
        /// Removes a <see cref="BasicTriList"/> from the project, optionally unregistering and disposing of it.
        /// </summary>
        /// <param name="panel">The <see cref="BasicTriList"/> to remove.</param>
        /// <param name="UnregisterAndDispose">If true will unregister and dispose of the panel after it is removed.</param>
        public void RemovePanel(BasicTriList panel, bool UnregisterAndDispose)
        {
            if (panels.Contains(panel))
            {
                panel.SigChange -= Enqueue;

                foreach (var sig in panel.BooleanOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }

                foreach (var sig in panel.UShortOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }

                foreach (var sig in panel.StringOutput.Where(sig => sig.UserObject != null))
                {
                    sig.UserObject = null;
                }

                var smartPanel = panel as BasicTriListWithSmartObject;

                if (smartPanel != null)
                {
                    foreach (var smartId in Actions.SmartObjectIds)
                    {
                        smartPanel.SmartObjects[smartId].SigChange -= Enqueue;
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
        public void Pulse(uint join, int millisecondDuration)
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
        /// <param name="panel">The <see cref="BasicTriList"/> to pulse the join on.</param>
        public void Pulse(uint join, int millisecondDuration, BasicTriList panel)
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
        /// <param name="join">The Join # on the smart object to pulse.</param>
        /// <param name="millisecondDuration">The duration in milliseconds to pulse the join.</param>
        public void Pulse(uint smartID, uint join, int millisecondDuration)
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
        /// <param name="join">The Join # on the smart object to pulse.</param>
        /// <param name="millisecondDuration">The duration in milliseconds to pulse the join.</param>
        /// /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to pulse the join on.</param>
        public void Pulse(uint smartID, uint join, int millisecondDuration, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
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
        public void SendValue(uint join, bool value)
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
        /// <param name="panel">The <see cref="BasicTriList"/> to send the value to.</param>
        public void SendValue(uint join, bool value, BasicTriList panel)
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
        public void SendValue(uint join, ushort value)
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
        /// <param name="panel">The <see cref="BasicTriList"/> to send the value to.</param>
        public void SendValue(uint join, ushort value, BasicTriList panel)
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
        public void SendValue(uint join, string value)
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
        /// <param name="panel">The <see cref="BasicTriList"/> to send the value to.</param>
        public void SendValue(uint join, string value, BasicTriList panel)
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
        public void SendSmartValue(uint smartJoin, uint join, bool value)
        {
            foreach (var panel in panels.OfType<BasicTriListWithSmartObject>())
            {
                if (panel.Registered)
                {
                    panel.SmartObjects[smartJoin].BooleanInput[join].BoolValue = value;
                }
            }
        }

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        public void SendSmartValue(uint smartJoin, uint join, bool value, BasicTriListWithSmartObject panel)
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
        public void SendSmartValue(uint smartJoin, uint join, ushort value)
        {
            foreach (var panel in panels.OfType<BasicTriListWithSmartObject>())
            {
                if (panel.Registered)
                {
                    panel.SmartObjects[smartJoin].UShortInput[join].UShortValue = value;
                }
            }
        }

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        public void SendSmartValue(uint smartJoin, uint join, ushort value, BasicTriListWithSmartObject panel)
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
        public void SendSmartValue(uint smartJoin, uint join, string value)
        {
            foreach (var panel in panels.OfType<BasicTriListWithSmartObject>())
            {
                if (panel.Registered)
                {
                    panel.SmartObjects[smartJoin].StringInput[join].StringValue = value;
                }
            }
        }

        /// <summary>
        /// Sends the smart value to the specific panel.
        /// </summary>
        /// <param name="smartJoin">The join # of the smart object.</param>
        /// <param name="join">The join # to send the value to.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="panel">The <see cref="BasicTriListWithSmartObject"/> to send the value to.</param>
        public void SendSmartValue(uint smartJoin, uint join, string value, BasicTriListWithSmartObject panel)
        {
            if (panels.Contains(panel) && panel.Registered)
            {
                panel.SmartObjects[smartJoin].StringInput[join].StringValue = value;
            }
        }

        /// <summary>
        /// Registers all the panels in the internal list. Only call after you're done initializing panels.
        /// </summary>
        public void Register()
        {
            isRegisterRequested = true;
            foreach (var p in panels)
            {
                if (!p.Registered)
                {
                    _Register(p);
                    p.Register(!string.IsNullOrEmpty(p.Description) ? p.Description : p.Name);
                }

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
        /// Kills all the threads, disposes of internal objects, unregisters devices and disposes all the panels.
        /// Can be used to teardown all the touchpanel logic at once, instead of manually calling dispose on all
        /// the touchpanels.
        /// </summary>
        public void Dispose()
        {
            Disposed = true;
            StopThreads();
            panelProcessingQueue.Dispose();
            DisposeChildren();
            UnRegister();
            foreach (var p in panels)
            {
                p.Dispose();
            }
        }

        /// <summary>
        /// Returns an object from the internal panels list, using the specified index.
        /// </summary>
        /// <param name="index">The index of the object to retrieve.</param>
        /// <returns>Returns a BasicTriListWithSmartObject if any exists at the specified index.</returns>
        public BasicTriList this[int index]
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
        /// Starts all the processing threads. Without starting the threads, no touchpanel interactions will surface to the program. Also sends initial values to touchpanels.
        /// </summary>
        public void StartThreads()
        {
            IsStarted = true;
            InitializeValues();
            if (processingThread == null)
            {
                processingThread = new Thread(ProcessInputQueue, null, Thread.eThreadStartOptions.Running);
                processingThread.Name = "Panel Processing Thread";
            }
            else
            {
                processingThread.Start();
            }
        }

        /// <summary>
        /// Stops and kills all the processing threads.
        /// </summary>
        public void StopThreads()
        {
            IsStarted = false;
            if (processingThread != null && (processingThread.ThreadState == Thread.eThreadStates.ThreadRunning || processingThread.ThreadState == Thread.eThreadStates.ThreadSuspended))
            {
                processingThread.Abort();
            }
            panelProcessingQueue.Clear();
        }

        /// <summary>
        /// Optional method for running custom code when a panel is registered.
        /// </summary>
        /// <param name="panel"></param>
        protected virtual void _Register(BasicTriList panel)
        {
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
        /// Adds a basic action object to the processing queue.
        /// </summary>
        /// <param name="currentDevice"></param>
        /// <param name="args"></param>
        protected void Enqueue(GenericBase currentDevice, SigEventArgs args)
        {
            var join = args.Sig.Number;
            switch (args.Event)
            {
                case eSigEvent.BoolChange:
                    var bv = args.Sig.BoolValue;
                    if (Actions.BoolActions.ContainsKey(join))
                    {
                        panelProcessingQueue.Enqueue(() =>
                        {
                            Actions.BoolActions[join].Invoke(bv);
                        });
                    }
                    else
                    {
                        if (bv)
                        {
                            if (Actions.BoolPressActions.ContainsKey(join))
                            {
                                panelProcessingQueue.Enqueue(() =>
                                {
                                    Actions.BoolPressActions[join].Invoke(true);
                                });
                            }
                        }
                        else
                        {
                            if (Actions.BoolReleaseActions.ContainsKey(join))
                            {
                                panelProcessingQueue.Enqueue(() =>
                                {
                                    Actions.BoolReleaseActions[join].Invoke(false);
                                });
                            }
                        }
                    }
                    break;
                case eSigEvent.UShortChange:
                    var uv = args.Sig.UShortValue;
                    if (Actions.UShortActions.ContainsKey(join))
                    {
                        panelProcessingQueue.Enqueue(() =>
                        {
                            Actions.UShortActions[join].Invoke(uv);
                        });
                    }
                    break;
                case eSigEvent.StringChange:
                    var sv = args.Sig.StringValue;
                    if (Actions.StringActions.ContainsKey(join))
                    {
                        panelProcessingQueue.Enqueue(() =>
                        {
                            Actions.StringActions[join].Invoke(sv);
                        });
                    }
                    break;
            }
        }

        /// <summary>
        /// Adds a smart object action to the processing queue.
        /// </summary>
        /// <param name="currentDevice"></param>
        /// <param name="args"></param>
        protected void Enqueue(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            var key = PanelActions.GetSmartKey(args.Sig.Number, args.SmartObjectArgs.ID);

            switch (args.Event)
            {
                case eSigEvent.BoolChange:
                    var bv = args.Sig.BoolValue;
                    if (Actions.BoolSmartActions.ContainsKey(key))
                    {
                        panelProcessingQueue.Enqueue(() =>
                        {
                            Actions.BoolSmartActions[key].Invoke(bv);
                        });
                    }
                    else
                    {
                        if (bv)
                        {
                            if (Actions.BoolSmartPressActions.ContainsKey(key))
                            {
                                panelProcessingQueue.Enqueue(() =>
                                {
                                    Actions.BoolSmartPressActions[key].Invoke(true);
                                });
                            }
                        }
                        else
                        {
                            if (Actions.BoolSmartReleaseActions.ContainsKey(key))
                            {
                                panelProcessingQueue.Enqueue(() =>
                                {
                                    Actions.BoolSmartReleaseActions[key].Invoke(false);
                                });
                            }
                        }
                    }

                    break;
                case eSigEvent.UShortChange:
                    var uv = args.Sig.UShortValue;
                    if (Actions.UShortSmartActions.ContainsKey(key))
                    {
                        panelProcessingQueue.Enqueue(() =>
                        {
                            Actions.UShortSmartActions[key].Invoke(uv);
                        });
                    }
                    break;
                case eSigEvent.StringChange:
                    var sv = args.Sig.StringValue;
                    if (Actions.StringSmartActions.ContainsKey(key))
                    {
                        panelProcessingQueue.Enqueue(() =>
                        {
                            Actions.StringSmartActions[key].Invoke(sv);
                        });
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles subscribing to any sig change events required.
        /// </summary>
        /// <param name="panel">The Panel to subscribe events.</param>
        private void ConfigureSigEvents(BasicTriList panel)
        {
            panel.SigChange += Enqueue;

            var smartPanel = panel as BasicTriListWithSmartObject;

            if (smartPanel != null)
            {
                foreach (var smartId in Actions.SmartObjectIds)
                {
                    smartPanel.SmartObjects[smartId].SigChange += Enqueue;
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerator object that can iterate over all the panels in the internal list.
        /// </summary>
        /// <returns></returns>
        IEnumerator<BasicTriList> IEnumerable<BasicTriList>.GetEnumerator()
        {
            return panels.GetEnumerator();
        }

        /// <summary>
        /// Returns an IEnumerator object that can iterate over all the panels in the internal list.
        /// </summary>
        /// <returns></returns>
        IEnumerator<BasicTriListWithSmartObject> IEnumerable<BasicTriListWithSmartObject>.GetEnumerator()
        {
            return panels.OfType<BasicTriListWithSmartObject>().GetEnumerator();
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
        /// Used to check if the TouchEventReceived event needs to be invoked and hands the signal data off to be processed.
        /// </summary>
        private object ProcessInputQueue(object obj)
        {
            while (IsStarted)
            {
                try
                {
                    var action = panelProcessingQueue.Dequeue();
                    CrestronInvoke.BeginInvoke((s) => { if (TouchEventReceived != null) { TouchEventReceived.Invoke(this, new EventArgs()); } });
                    if (action != null)
                    {
                        action.Invoke();
                    }
                }
                catch (System.Threading.ThreadAbortException)
                {
                    CrestronConsole.PrintLine("Thread exiting: {0}", processingThread.Name);
                    if (IsStarted && !Disposed)
                    {
                        ErrorLog.Notice("Touchpanel Input Thread exited prematurely: {0}", processingThread.Name);
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

            return obj;
        }

        #endregion
    }
}