using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Data.Xml.Rpc
{
    class XmlRpcServiceInfo
    {
        public static XmlRpcServiceInfo CreateServiceInfo(Type type)
        {
            XmlRpcServiceInfo svcInfo = new XmlRpcServiceInfo();
            // extract service info
            //XmlRpcServiceAttribute svcAttr = (XmlRpcServiceAttribute)
            //  Attribute.GetCustomAttribute(type, typeof(XmlRpcServiceAttribute));

            XmlRpcServiceAttribute svcAttr = type.GetTypeInfo().GetCustomAttribute<XmlRpcServiceAttribute>();

            if (svcAttr != null && svcAttr.Description != "")
                svcInfo.doc = svcAttr.Description;
            if (svcAttr != null && svcAttr.Name != "")
                svcInfo.Name = svcAttr.Name;
            else
                svcInfo.Name = type.Name;
            // extract method info
            var methods = new Dictionary<string, XmlRpcMethodInfo>();

            foreach (Type itf in type.GetInterfaces())
            {

                //XmlRpcServiceAttribute itfAttr = (XmlRpcServiceAttribute)
                //  Attribute.GetCustomAttribute(itf, typeof(XmlRpcServiceAttribute));

                XmlRpcServiceAttribute itfAttr = itf.GetTypeInfo().GetCustomAttribute<XmlRpcServiceAttribute>();

                if (itfAttr != null)
                    svcInfo.doc = itfAttr.Description;
#if (!COMPACT_FRAMEWORK)

                //InterfaceMapping imap = type.GetInterfaceMap(itf);

                InterfaceMapping imap = type.GetTypeInfo().GetRuntimeInterfaceMap(itf);
                foreach (MethodInfo mi in imap.InterfaceMethods)
                {
                    ExtractMethodInfo(methods, mi, itf);
                }
#else
        foreach (MethodInfo mi in itf.GetMethods())
        {
          ExtractMethodInfo(methods, mi, itf);
        }
#endif
            }

            foreach (MethodInfo mi in type.GetMethods())
            {
                var mthds = new List<MethodInfo>();
                mthds.Add(mi);
                MethodInfo curMi = mi;
                while (true)
                {
                    MethodInfo baseMi = curMi.GetBaseDefinition();
                    if (baseMi.DeclaringType == curMi.DeclaringType)
                        break;
                    mthds.Insert(0, baseMi);
                    curMi = baseMi;
                }
                foreach (MethodInfo mthd in mthds)
                {
                    ExtractMethodInfo(methods, mthd, type);
                }
            }
            svcInfo.methodInfos = new XmlRpcMethodInfo[methods.Count];
            methods.Values.CopyTo(svcInfo.methodInfos, 0);
            Array.Sort(svcInfo.methodInfos);
            return svcInfo;
        }

        public MethodInfo GetMethodInfo(string xmlRpcMethodName)
        {
            foreach (XmlRpcMethodInfo xmi in methodInfos)
            {
                if (xmlRpcMethodName == xmi.XmlRpcName)
                {
                    return xmi.MethodInfo;
                }
            }
            return null;
        }

        static void ExtractMethodInfo(Dictionary<string, XmlRpcMethodInfo> methods,
          MethodInfo mi, Type type)
        {
            //XmlRpcMethodAttribute attr = (XmlRpcMethodAttribute)
            //  Attribute.GetCustomAttribute(mi,
            //  typeof(XmlRpcMethodAttribute));

            XmlRpcMethodAttribute attr = mi.GetCustomAttribute<XmlRpcMethodAttribute>();

            if (attr == null)
                return;
            XmlRpcMethodInfo mthdInfo = new XmlRpcMethodInfo();
            mthdInfo.MethodInfo = mi;
            mthdInfo.XmlRpcName = XmlRpcTypeInfo.GetXmlRpcMethodName(mi);
            mthdInfo.MiName = mi.Name;
            mthdInfo.Doc = attr.Description;
            mthdInfo.IsHidden = attr.IntrospectionMethod | attr.Hidden;
            // extract parameters information
            var parmList = new List<XmlRpcParameterInfo>();
            ParameterInfo[] parms = mi.GetParameters();
            foreach (ParameterInfo parm in parms)
            {
                XmlRpcParameterInfo parmInfo = new XmlRpcParameterInfo();
                parmInfo.Name = parm.Name;
                parmInfo.Type = parm.ParameterType;
                parmInfo.XmlRpcType = XmlRpcTypeInfo.GetXmlRpcTypeString(parm.ParameterType);
                // retrieve optional attributed info
                parmInfo.Doc = "";
                //XmlRpcParameterAttribute pattr = (XmlRpcParameterAttribute)
                //  Attribute.GetCustomAttribute(parm,
                //  typeof(XmlRpcParameterAttribute));

                XmlRpcParameterAttribute pattr = parm.GetCustomAttribute<XmlRpcParameterAttribute>();

                if (pattr != null)
                {
                    parmInfo.Doc = pattr.Description;
                    parmInfo.XmlRpcName = pattr.Name;
                }
                
                //parmInfo.IsParams = Attribute.IsDefined(parm,
                //  typeof(ParamArrayAttribute));
                
                parmInfo.IsParams = parm.IsDefined(typeof(ParamArrayAttribute));
                parmList.Add(parmInfo);
            }
            mthdInfo.Parameters = parmList.ToArray();
            // extract return type information
            mthdInfo.ReturnType = mi.ReturnType;
            mthdInfo.ReturnXmlRpcType = XmlRpcTypeInfo.GetXmlRpcTypeString(mi.ReturnType);
            //object[] orattrs = mi.ReturnTypeCustomAttributes.GetCustomAttributes(
            //  typeof(XmlRpcReturnValueAttribute), false);

            object[] orattrs = mi.GetCustomAttributes<XmlRpcReturnValueAttribute>(false).ToArray();

            if (orattrs.Length > 0)
            {
                mthdInfo.ReturnDoc = ((XmlRpcReturnValueAttribute)orattrs[0]).Description;
            }

            if (methods.ContainsKey(mthdInfo.XmlRpcName))
            {
                throw new XmlRpcDupXmlRpcMethodNames(String.Format("Method "
                  + "{0} in type {1} has duplicate XmlRpc method name {2}",
                  mi.Name, type.Name, mthdInfo.XmlRpcName));
            }
            else
                methods.Add(mthdInfo.XmlRpcName, mthdInfo);
        }

        public string GetMethodName(string XmlRpcMethodName)
        {
            foreach (XmlRpcMethodInfo methodInfo in methodInfos)
            {
                if (methodInfo.XmlRpcName == XmlRpcMethodName)
                    return methodInfo.MiName;
            }
            return null;
        }

        public String Doc
        {
            get { return doc; }
            set { doc = value; }
        }

        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        public XmlRpcMethodInfo[] Methods
        {
            get { return methodInfos; }
        }

        public XmlRpcMethodInfo GetMethod(
          String methodName)
        {
            foreach (XmlRpcMethodInfo mthdInfo in methodInfos)
            {
                if (mthdInfo.XmlRpcName == methodName)
                    return mthdInfo;
            }
            return null;
        }

        private XmlRpcServiceInfo()
        {
        }

        XmlRpcMethodInfo[] methodInfos;
        String doc;
        string name;
    }
}
