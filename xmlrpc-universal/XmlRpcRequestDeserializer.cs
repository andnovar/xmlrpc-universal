using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Windows.Data.Xml.Rpc
{
    class XmlRpcRequestDeserializer : XmlRpcDeserializer
    {
        public XmlRpcRequest DeserializeRequest(Stream stm, Type svcType)
        {
            if (stm == null)
                throw new ArgumentNullException("stm",
                  "XmlRpcSerializer.DeserializeRequest");
            XmlReader xmlRdr = XmlRpcXmlReader.Create(stm);
            return DeserializeRequest(xmlRdr, svcType);
        }

        public XmlRpcRequest DeserializeRequest(TextReader txtrdr, Type svcType)
        {
            if (txtrdr == null)
                throw new ArgumentNullException("txtrdr",
                  "XmlRpcSerializer.DeserializeRequest");
            XmlReader xmlRdr = XmlRpcXmlReader.Create(txtrdr);
            return DeserializeRequest(xmlRdr, svcType);
        }

        public XmlRpcRequest DeserializeRequest(XmlReader rdr, Type svcType)
        {
            try
            {
                String a = rdr.ToString();
                XmlRpcRequest request = new XmlRpcRequest();
                XmlRpcParser parser = new XmlRpcParser();
                IEnumerable<Node> enode = parser.ParseRequest(rdr);
                IEnumerator<Node> iter = enode.GetEnumerator();

                iter.MoveNext();
                string methodName = (iter.Current as MethodName).Name;
                request.method = methodName;

                request.mi = null;
                ParameterInfo[] pis = null;
                if (svcType != null)
                {
                    // retrieve info for the method which handles this XML-RPC method
                    XmlRpcServiceInfo svcInfo
                      = XmlRpcServiceInfo.CreateServiceInfo(svcType);
                    request.mi = svcInfo.GetMethodInfo(request.method);
                    // if a service type has been specified and we cannot find the requested
                    // method then we must throw an exception
                    if (request.mi == null)
                    {
                        string msg = String.Format("unsupported method called: {0}",
                                                    request.method);
                        throw new XmlRpcUnsupportedMethodException(msg);
                    }
                    // method must be marked with XmlRpcMethod attribute
                    //Attribute attr = Attribute.GetCustomAttribute(request.mi,
                    //  typeof(XmlRpcMethodAttribute));

                    Attribute attr = request.mi.GetCustomAttribute<XmlRpcMethodAttribute>();

                    if (attr == null)
                    {
                        throw new XmlRpcMethodAttributeException(
                          "Method must be marked with the XmlRpcMethod attribute.");
                    }
                    pis = request.mi.GetParameters();
                }

                bool gotParams = iter.MoveNext();
                if (!gotParams)
                {
                    if (svcType != null)
                    {
                        if (pis.Length == 0)
                        {
                            request.args = new object[0];
                            return request;
                        }
                        else
                        {
                            throw new XmlRpcInvalidParametersException(
                              "Method takes parameters and params element is missing.");
                        }
                    }
                    else
                    {
                        request.args = new object[0];
                        return request;
                    }
                }

                int paramsPos = pis != null ? GetParamsPos(pis) : -1;
                Type paramsType = null;
                if (paramsPos != -1)
                    paramsType = pis[paramsPos].ParameterType.GetElementType();
                int minParamCount = pis == null ? int.MaxValue
                  : (paramsPos == -1 ? pis.Length : paramsPos);
                MappingStack mappingStack = new MappingStack("request");
                MappingAction mappingAction = MappingAction.Error;
                var objs = new List<object>();
                var paramsObjs = new List<object>();
                int paramCount = 0;


                while (iter.MoveNext())
                {
                    paramCount++;
                    if (svcType != null && paramCount > minParamCount && paramsPos == -1)
                        throw new XmlRpcInvalidParametersException(
                          "Request contains too many param elements based on method signature.");
                    if (paramCount <= minParamCount)
                    {
                        if (svcType != null)
                        {
                            mappingStack.Push(String.Format("parameter {0}", paramCount));
                            // TODO: why following commented out?
                            //          parseStack.Push(String.Format("parameter {0} mapped to type {1}", 
                            //            i, pis[i].ParameterType.Name));
                            var obj = MapValueNode(iter,
                              pis[paramCount - 1].ParameterType, mappingStack, mappingAction);
                            objs.Add(obj);
                        }
                        else
                        {
                            mappingStack.Push(String.Format("parameter {0}", paramCount));
                            var obj = MapValueNode(iter, null, mappingStack, mappingAction);
                            objs.Add(obj);
                        }
                        mappingStack.Pop();
                    }
                    else
                    {
                        mappingStack.Push(String.Format("parameter {0}", paramCount + 1));
                        var paramsObj = MapValueNode(iter, paramsType, mappingStack, mappingAction);
                        paramsObjs.Add(paramsObj);
                        mappingStack.Pop();
                    }
                }

                if (svcType != null && paramCount < minParamCount)
                    throw new XmlRpcInvalidParametersException(
                      "Request contains too few param elements based on method signature.");

                if (paramsPos != -1)
                {
                    Object[] args = new Object[1];
                    args[0] = paramCount - minParamCount;
                    Array varargs = (Array)Activator.CreateInstance(pis[paramsPos].ParameterType, args);
                    for (int i = 0; i < paramsObjs.Count; i++)
                        varargs.SetValue(paramsObjs[i], i);
                    objs.Add(varargs);
                }
                request.args = objs.ToArray();
                return request;
            }
            catch (XmlException ex)
            {
                Debug.WriteLine(ex.Message);
                throw new XmlRpcIllFormedXmlException("Request contains invalid XML", ex);
            }
        }

        int GetParamsPos(ParameterInfo[] pis)
        {
            if (pis.Length == 0)
                return -1;
            //if (Attribute.IsDefined(pis[pis.Length - 1], typeof(ParamArrayAttribute)))
            if(pis[pis.Length - 1].IsDefined(typeof(ParamArrayAttribute)))
            {
                return pis.Length - 1;
            }
            else
                return -1;
        }
    }
}
