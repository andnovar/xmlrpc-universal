using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Data.Xml.Rpc
{
    public class XmlRpcServerProtocol : SystemMethodsBase
    {
        public Stream Invoke(Stream requestStream)
        {
            try
            {
                var serializer = new XmlRpcResponseSerializer();
                var deserializer = new XmlRpcRequestDeserializer();
                Type type = this.GetType();

                //XmlRpcServiceAttribute serviceAttr = (XmlRpcServiceAttribute)
                //  Attribute.GetCustomAttribute(this.GetType(),
                //  typeof(XmlRpcServiceAttribute));

                XmlRpcServiceAttribute serviceAttr = this.GetType().GetTypeInfo().GetCustomAttribute<XmlRpcServiceAttribute>();

                if (serviceAttr != null)
                {
                    if (serviceAttr.XmlEncoding != null)
                        serializer.XmlEncoding = Encoding.GetEncoding(serviceAttr.XmlEncoding);
                    serializer.UseEmptyParamsTag = serviceAttr.UseEmptyElementTags;
                    serializer.UseIntTag = serviceAttr.UseIntTag;
                    serializer.UseStringTag = serviceAttr.UseStringTag;
                    serializer.UseIndentation = serviceAttr.UseIndentation;
                    serializer.Indentation = serviceAttr.Indentation;
                }
                XmlRpcRequest xmlRpcReq
                  = deserializer.DeserializeRequest(requestStream, this.GetType());
                XmlRpcResponse xmlRpcResp = Invoke(xmlRpcReq);
                Stream responseStream = new MemoryStream();
                serializer.SerializeResponse(responseStream, xmlRpcResp);
                responseStream.Seek(0, SeekOrigin.Begin);
                return responseStream;
            }
            catch (Exception ex)
            {
                XmlRpcFaultException fex;
                if (ex is XmlRpcException)
                    fex = new XmlRpcFaultException(0, ((XmlRpcException)ex).Message);
                else if (ex is XmlRpcFaultException)
                    fex = (XmlRpcFaultException)ex;
                else
                    fex = new XmlRpcFaultException(0, ex.Message);
                XmlRpcSerializer serializer = new XmlRpcSerializer();
                Stream responseStream = new MemoryStream();
                serializer.SerializeFaultResponse(responseStream, fex);
                responseStream.Seek(0, SeekOrigin.Begin);
                return responseStream;
            }
        }

        /// <summary>
        /// Check if the invoke is returning 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public XmlRpcResponse Invoke(XmlRpcRequest request)
        {
            MethodInfo mi = null;
            if (request.mi != null)
            {
                mi = request.mi;
            }
            else
            {
                mi = this.GetType().GetMethod(request.method);
            }
            // exceptions thrown during an MethodInfo.Invoke call are
            // package as inner of 
            Object reto;
            try
            {
                reto = mi.Invoke(this, request.args);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw ex;
            }
            // methods which have void return type always return integer 0
            // because XML-RPC doesn't support no return type (could use nil
            // but want to maintain backwards compatibility in this area)
            if (mi != null && mi.ReturnType == typeof(void))
                reto = 0;
            // Added mi method info to the XmlRpcResponse if null will give fault code
            XmlRpcResponse response = new XmlRpcResponse(reto, mi);
            return response;
        }

        bool IsVisibleXmlRpcMethod(MethodInfo mi)
        {
            bool ret = false;
            //Attribute attr = Attribute.GetCustomAttribute(mi,
            //  typeof(XmlRpcMethodAttribute));

            Attribute attr = mi.GetCustomAttribute<XmlRpcMethodAttribute>();

            if (attr != null)
            {
                XmlRpcMethodAttribute mattr = (XmlRpcMethodAttribute)attr;
                ret = !mattr.Hidden;
            }
            return ret;
        }
    }
}
