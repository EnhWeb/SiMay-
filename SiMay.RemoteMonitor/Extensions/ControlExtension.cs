using SiMay.RemoteMonitor.Attributes;
using SiMay.RemoteMonitor.ControlSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiMay.Basic;

namespace SiMay.RemoteMonitor.Extensions
{
    public static class ControlExtension
    {
        public static string GetControlDisplayName(this Type type)
        {
            var attr = type.GetCustomAttribute<ControlSourceAttribute>(true);
            return attr.Name;
        }
        public static string GetIconResourceName(this Type type)
        {
            var attr = type.GetCustomAttribute<ControlSourceAttribute>(true);
            return attr.ResourceName;
        }

        public static string GetControlKey(this Type type)
        {
            var attr = type.GetCustomAttribute<ControlSourceAttribute>(true);
            return attr.ContorlKey;
        }

        public static int GetRank(this Type type)
        {
            var attr = type.GetCustomAttribute<ControlSourceAttribute>(true);
            return attr.Rank;
        }
    }
}
