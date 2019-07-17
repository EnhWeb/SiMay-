using SiMay.RemoteService.NewCore.ControlService;
using SiMay.RemoteService.NewCore.Extensions;
using SiMay.RemoteService.NewCore.ServiceSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SiMay.RemoteService.NewCore
{
    public class SysUtil
    {
        public class ServiceTypeContext
        {
            public string ServiceKey { get; set; }
            public Type CtrlType { get; set; }
        }
        public static List<ServiceTypeContext> ControlTypes { get; set; }
        static SysUtil()
        {
            List<ServiceTypeContext> controlTypes = new List<ServiceTypeContext>();
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                if (typeof(IServiceSource).IsAssignableFrom(type) && type.IsClass)
                {
                    if (typeof(ServiceManager).IsSubclassOf(type))
                        throw new Exception(type.FullName + " type must inherit ServiceManager base class!");

                    var context = new ServiceTypeContext()
                    {
                        ServiceKey = type.GetServiceKey() ?? throw new Exception(type.Name + ":The serviceKey cannot be empty!"),
                        CtrlType = type
                    };
                    controlTypes.Add(context);
                }
            }
            ControlTypes = controlTypes;
        }
    }
}
