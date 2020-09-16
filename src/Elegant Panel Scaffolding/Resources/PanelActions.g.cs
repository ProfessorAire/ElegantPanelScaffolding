using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantHome.UI.OfficePanel.Core
{
    public class PanelActions
    {
        public Dictionary<uint, Action<bool>> BoolActions { get; private set; }

        public Dictionary<uint, Action<bool>> BoolPressActions { get; private set; }

        public Dictionary<uint, Action<bool>> BoolReleaseActions { get; private set; }

        public Dictionary<uint, Action<ushort>> UShortActions { get; private set; }

        public Dictionary<uint, Action<string>> StringActions { get; private set; }

        public Dictionary<KeyValuePair<uint, uint>, Action<bool>> BoolSmartActions { get; private set; }

        public Dictionary<KeyValuePair<uint, uint>, Action<bool>> BoolSmartPressActions { get; private set; }

        public Dictionary<KeyValuePair<uint, uint>, Action<bool>> BoolSmartReleaseActions { get; private set; }

        public Dictionary<KeyValuePair<uint, uint>, Action<ushort>> UShortSmartActions { get; private set; }

        public Dictionary<KeyValuePair<uint, uint>, Action<string>> StringSmartActions { get; private set; }

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
        }

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
        }


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
        }


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
        }

        public static KeyValuePair<uint, uint> GetSmartKey(uint join, uint smartId)
        {
            return new KeyValuePair<uint, uint>(smartId, join);
        }

    }
}