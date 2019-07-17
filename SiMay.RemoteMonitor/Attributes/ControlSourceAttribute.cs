using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiMay.RemoteMonitor.Attributes
{
    public class ControlSourceAttribute : Attribute
    {
        /// <summary>
        /// 组件源特性
        /// </summary>
        /// <param name="rank">序号</param>
        /// <param name="name">组件名称</param>
        /// <param name="ctrlKey">组件ID</param>
        /// <param name="resourceName">图片资源名称</param>
        public ControlSourceAttribute(int rank, string name, string ctrlKey, string resourceName)
        {
            Rank = rank;
            Name = name;
            ContorlKey = ctrlKey;
            ResourceName = resourceName;
        }
        public int Rank { get; set; }
        public string Name { get; set; }
        public string ContorlKey { get; set; }
        public string ResourceName { get; set; }
    }
}
