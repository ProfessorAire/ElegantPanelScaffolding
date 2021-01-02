using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace SharpProTouchpanelDemo.UI.Core
{
    /// <summary>
    /// Panel actions class.
    /// </summary>
    public class PanelActions
    {
        /// <summary>
        /// List of smart object ids.
        /// </summary>
        private List<uint> smartObjectIds = new List<uint>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PanelActions"/> class.
        /// </summary>
        public PanelActions()
        {
            BoolActions = new Dictionary<uint, Action<bool>>();
            BoolPressActions = new Dictionary<uint, Action<bool>>();
            BoolReleaseActions = new Dictionary<uint, Action<bool>>();
            UShortActions = new Dictionary<uint, Action<ushort>>();
            StringActions = new Dictionary<uint, Action<string>>();
            BoolSmartActions = new Dictionary<KeyValuePair<uint, uint>, Action<bool>>();
            BoolSmartPressActions = new Dictionary<KeyValuePair<uint, uint>, Action<bool>>();
            BoolSmartReleaseActions = new Dictionary<KeyValuePair<uint, uint>, Action<bool>>();
            UShortSmartActions = new Dictionary<KeyValuePair<uint, uint>, Action<ushort>>();
            StringSmartActions = new Dictionary<KeyValuePair<uint, uint>, Action<string>>();
        }

        /// <summary>
        /// List of boolean actions.
        /// </summary>
        public Dictionary<uint, Action<bool>> BoolActions { get; private set; }

        /// <summary>
        /// List of boolean press actions.
        /// </summary>
        public Dictionary<uint, Action<bool>> BoolPressActions { get; private set; }

        /// <summary>
        /// List of boolean release actions.
        /// </summary>
        public Dictionary<uint, Action<bool>> BoolReleaseActions { get; private set; }

        /// <summary>
        /// List of ushort actions.
        /// </summary>
        public Dictionary<uint, Action<ushort>> UShortActions { get; private set; }

        /// <summary>
        /// List of string actions.
        /// </summary>
        public Dictionary<uint, Action<string>> StringActions { get; private set; }

        /// <summary>
        /// List of boolean smart actions.
        /// </summary>
        public Dictionary<KeyValuePair<uint, uint>, Action<bool>> BoolSmartActions { get; private set; }

        /// <summary>
        /// List of boolean smart press actions.
        /// </summary>
        public Dictionary<KeyValuePair<uint, uint>, Action<bool>> BoolSmartPressActions { get; private set; }

        /// <summary>
        /// List of boolean smart release actions.
        /// </summary>
        public Dictionary<KeyValuePair<uint, uint>, Action<bool>> BoolSmartReleaseActions { get; private set; }

        /// <summary>
        /// List of ushort smart actions.
        /// </summary>
        public Dictionary<KeyValuePair<uint, uint>, Action<ushort>> UShortSmartActions { get; private set; }

        /// <summary>
        /// List of string smart actions.
        /// </summary>
        public Dictionary<KeyValuePair<uint, uint>, Action<string>> StringSmartActions { get; private set; }

        /// <summary>
        /// Read only collection of smart object Ids.
        /// </summary>
        public ReadOnlyCollection<uint> SmartObjectIds { get { return smartObjectIds.AsReadOnly(); } }

        /// <summary>
        /// Adds a boolean action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="action">The action</param>
        /// <param name="isPress">Whether it is a press event.</param>
        public void AddBool(uint join, Action<bool> action, bool isPress)
        {
            if (isPress)
            {
                if (BoolPressActions == null)
                {
                    BoolPressActions = new Dictionary<uint, Action<bool>>();
                }

                if (!BoolPressActions.ContainsKey(join))
                {
                    BoolPressActions.Add(join, action);
                }
                else
                {
                    BoolPressActions[join] += action;
                }
            }
            else
            {
                if (BoolReleaseActions == null)
                {
                    BoolReleaseActions = new Dictionary<uint, Action<bool>>();
                }

                if (!BoolReleaseActions.ContainsKey(join))
                {
                    BoolReleaseActions.Add(join, action);
                }
                else
                {
                    BoolReleaseActions[join] += action;
                }
            }
        }
        
        /// <summary>
        /// Adds a boolean action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="action">The action.</param>
        public void AddBool(uint join, Action<bool> action)
        {
            if (BoolActions == null)
            {
                BoolActions = new Dictionary<uint, Action<bool>>();
            }

            if (!BoolActions.ContainsKey(join))
            {
                BoolActions.Add(join, action);
            }
            else
            {
                BoolActions[join] += action;
            }
        }

        /// <summary>
        /// Adds a ushort action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="action">The action.</param>
        public void AddUShort(uint join, Action<ushort> action)
        {
            if (UShortActions == null)
            {
                UShortActions = new Dictionary<uint, Action<ushort>>();
            }

            if (!UShortActions.ContainsKey(join))
            {
                UShortActions.Add(join, action);
            }
            else
            {
                UShortActions[join] += action;
            }
        }

        /// <summary>
        /// Adds a string action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="action">The action.</param>
        public void AddString(uint join, Action<string> action)
        {
            if (StringActions == null)
            {
                StringActions = new Dictionary<uint, Action<string>>();
            }

            if (!StringActions.ContainsKey(join))
            {
                StringActions.Add(join, action);
            }
            else
            {
                StringActions[join] += action;
            }
        }

        /// <summary>
        /// Adds a smart boolean action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="smartId">The smart object id.</param>
        /// <param name="action">The action.</param>
        public void AddBool(uint join, uint smartId, Action<bool> action)
        {
            if (BoolSmartActions == null)
            {
                BoolSmartActions = new Dictionary<KeyValuePair<uint, uint>, Action<bool>>();
            }

            var key = GetSmartKey(join, smartId);

            if (!BoolSmartActions.ContainsKey(key))
            {
                BoolSmartActions.Add(key, action);
            }
            else
            {
                BoolSmartActions[key] += action;
            }

            if (!smartObjectIds.Contains(smartId))
            {
                smartObjectIds.Add(smartId);
            }
        }

        /// <summary>
        /// Adds a smart boolean action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="smartId">The smart object id.</param>
        /// <param name="action">The action.</param>
        /// <param name="isPress">Whether it is a press or release.</param>
        public void AddBool(uint join, uint smartId, Action<bool> action, bool isPress)
        {
            var key = GetSmartKey(join, smartId);

            if (isPress)
            {
                if (BoolSmartPressActions == null)
                {
                    BoolSmartPressActions = new Dictionary<KeyValuePair<uint, uint>, Action<bool>>();
                }

                if (!BoolSmartPressActions.ContainsKey(key))
                {
                    BoolSmartPressActions.Add(key, action);
                }
                else
                {
                    BoolSmartPressActions[key] += action;
                }
            }
            else
            {
                if (BoolSmartReleaseActions == null)
                {
                    BoolSmartPressActions = new Dictionary<KeyValuePair<uint, uint>, Action<bool>>();
                }

                if (!BoolSmartReleaseActions.ContainsKey(key))
                {
                    BoolSmartReleaseActions.Add(key, action);
                }
                else
                {
                    BoolSmartReleaseActions[key] += action;
                }
            }

            if (!smartObjectIds.Contains(smartId))
            {
                smartObjectIds.Add(smartId);
            }
        }

        /// <summary>
        /// Adds a smart ushort action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="smartId">The smart object id.</param>
        /// <param name="action">The action.</param>
        public void AddUShort(uint join, uint smartId, Action<ushort> action)
        {
            if (UShortSmartActions == null)
            {
                UShortSmartActions = new Dictionary<KeyValuePair<uint, uint>, Action<ushort>>();
            }

            var key = GetSmartKey(join, smartId);

            if (!UShortSmartActions.ContainsKey(key))
            {
                UShortSmartActions.Add(key, action);
            }
            else
            {
                UShortSmartActions[key] += action;
            }

            if (!smartObjectIds.Contains(smartId))
            {
                smartObjectIds.Add(smartId);
            }
        }

        /// <summary>
        /// Adds a smart string action.
        /// </summary>
        /// <param name="join">The join number.</param>
        /// <param name="smartId">The smart object id.</param>
        /// <param name="action">The action.</param>
        public void AddString(uint join, uint smartId, Action<string> action)
        {
            if (StringSmartActions == null)
            {
                StringSmartActions = new Dictionary<KeyValuePair<uint, uint>, Action<string>>();
            }

            var key = GetSmartKey(join, smartId);

            if (!StringSmartActions.ContainsKey(key))
            {
                StringSmartActions.Add(key, action);
            }
            else
            {
                StringSmartActions[key] += action;
            }

            if (!smartObjectIds.Contains(smartId))
            {
                smartObjectIds.Add(smartId);
            }
        }

        /// <summary>
        /// Gets a key value pair for a join and smart object id.
        /// </summary>
        /// <param name="join">The Join number.</param>
        /// <param name="smartId">The smart object id.</param>
        /// <returns>A key value pair.</returns>
        public static KeyValuePair<uint, uint> GetSmartKey(uint join, uint smartId)
        {
            return new KeyValuePair<uint, uint>(smartId, join);
        }

    }
}