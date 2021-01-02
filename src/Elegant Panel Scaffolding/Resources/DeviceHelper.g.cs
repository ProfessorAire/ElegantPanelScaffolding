using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpProInternal;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Device helper methods.
    /// </summary>
    public static class DeviceHelper
    {
        /// <summary>
        /// Attempts device registration, passing feedback to the console and error log if it fails.
        /// </summary>
        /// <param name="dev">The device to register.</param>
        /// <param name="deviceDescriptor">A description of the device for use if it fails and an error is printed.</param>
        /// <returns>Returns true if the registration succeeds, false if it fails for any reason.</returns>
        public static bool Register(this GenericBase dev, string deviceDescriptor)
        {
            if (dev == null)
            {
                CrestronConsole.PrintLine("Couldn't register {0}, the device was null.", deviceDescriptor);
                ErrorLog.Error("Coudln't reigster {0}, the device was null.", deviceDescriptor);
                return false;
            }
            if (dev.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                CrestronConsole.PrintLine("Couldn't register {0} for the reason: {1}", deviceDescriptor, dev.RegistrationFailureReason);
                ErrorLog.Error("Coudln't reigster {0} for the reason: {1}.", deviceDescriptor, dev.RegistrationFailureReason);
                return false;
            }
            CrestronConsole.PrintLine("Registered: {0}", deviceDescriptor);
            return true;
        }

        /// <summary>
        /// Attempts device registration, passing feedback to the console and error log if it fails.
        /// </summary>
        /// <param name="dev">The device to register.</param>
        /// <param name="deviceDescriptor">A description of the device for use if it fails and an error is printed.</param>
        /// <returns>Returns true if the registration succeeds, false if it fails for any reason.</returns>
        public static bool Register(this PortDevice dev, string deviceDescriptor)
        {
            if (dev == null)
            {
                CrestronConsole.PrintLine("Couldn't register {0}, the device was null.", deviceDescriptor);
                ErrorLog.Error("Coudln't reigster {0}, the device was null.", deviceDescriptor);
                return false;
            }
            if (dev.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                CrestronConsole.PrintLine("Couldn't register {0} for the reason: {1}", deviceDescriptor, dev.DeviceRegistrationFailureString);
                ErrorLog.Error("Coudln't reigster {0} for the reason: {1}.", deviceDescriptor, dev.DeviceRegistrationFailureString);
                return false;
            }
            CrestronConsole.PrintLine("Registered: {0}", deviceDescriptor);
            return true;
        }

        /// <summary>
        /// Attempts device unregistration, passing feedback to the console and error log if it fails.
        /// </summary>
        /// <param name="dev">The device to unregister.</param>
        /// <param name="deviceDescriptor">A description of the device for use if it fails and an error is printed.</param>
        /// <returns>Returns true if the unregistration succeeds, false if it fails for any reason.</returns>
        public static bool UnRegister(this GenericBase dev, string deviceDescriptor)
        {
            if (dev == null)
            {
                CrestronConsole.PrintLine("Couldn't unregister {0}, the device was null.", deviceDescriptor);
                ErrorLog.Error("Coudln't unreigster {0}, the device was null.", deviceDescriptor);
                return false;
            }
            if (dev.UnRegister() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                CrestronConsole.PrintLine("Couldn't unregister {0} for the reason: {1}", deviceDescriptor, dev.RegistrationFailureReason);
                ErrorLog.Error("Couldn't unregister {0} for the reason: {1}", deviceDescriptor, dev.RegistrationFailureReason);
                return false;
            }
            CrestronConsole.PrintLine("Unregistered: {0}", deviceDescriptor);
            return true;
        }

        /// <summary>
        /// Disposes a device, passing feedback to the console. Logs an error if it fails.
        /// </summary>
        /// <param name="device">The device to dispose.</param>
        /// <param name="deviceDescriptor">A description of the device for use in the console and error log.</param>
        public static void Dispose(this GenericBase device, string deviceDescriptor)
        {
            try
            {
                if (device == null)
                {
                    CrestronConsole.PrintLine("Couldn't dispose {0}, the device was null.", deviceDescriptor);
                    ErrorLog.Error("Couldn't dispose {0}, the device was null.", deviceDescriptor);
                    return;
                }
                device.Dispose();
                CrestronConsole.PrintLine("Disposed of the {0}", deviceDescriptor);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("The device {0} potentially failed to dispose, due to an exception: {1}", deviceDescriptor, ex.Message);
                ErrorLog.Error("The device {0} potentially failed to dispose, due to an exception: {1}", deviceDescriptor, ex.Message);
            }
        }

        /// <summary>
        /// Disposes a device, passing feedback to the console. Logs an error if it fails.
        /// </summary>
        /// <param name="device">The device to dispose.</param>
        /// <param name="deviceDescriptor">A description of the device for use in the console and error log.</param>
        public static void Dispose(this IDisposable device, string deviceDescriptor)
        {
            try
            {
                if (device == null)
                {
                    CrestronConsole.PrintLine("Couldn't dispose {0}, the device was null.", deviceDescriptor);
                    ErrorLog.Error("Couldn't dispose {0}, the device was null.", deviceDescriptor);
                    return;
                }
                device.Dispose();
                CrestronConsole.PrintLine("Disposed of the {0}", deviceDescriptor);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("The device {0} potentially failed to dispose, due to an exception: {1}", deviceDescriptor, ex.Message);
                ErrorLog.Error("The device {0} potentially failed to dispose, due to an exception: {1}", deviceDescriptor, ex.Message);
            }
        }
    }
}