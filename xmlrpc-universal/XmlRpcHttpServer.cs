using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

namespace Windows.Data.Xml.Rpc
{
    public class XmlRpcHttpServer : IDisposable
    {
        private const uint BufferSize = 8192;

        private readonly StreamSocketListener listener;
        private XmlRpcServerProtocol protocol;

        public XmlRpcHttpServer(int port, XmlRpcServerProtocol protocol)
        {
            this.protocol = protocol;
            this.listener = new StreamSocketListener();
            this.listener.ConnectionReceived += Listener_ConnectionReceived;
            this.listener.BindServiceNameAsync(port.ToString());
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {

            StringBuilder request = new StringBuilder();
            using (IInputStream input = args.Socket.InputStream)
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            String inmsg = request.ToString();
            String xml = inmsg.Substring(inmsg.IndexOf('<'));


            byte[] byteArray = Encoding.ASCII.GetBytes(xml);
            MemoryStream input2 = new MemoryStream(byteArray);

            Stream output = protocol.Invoke(input2);

            IOutputStream os = args.Socket.OutputStream;
            using (Stream resp = os.AsStreamForWrite())
            {
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                        "Content-Length: {0}\r\n" +
                                        "Connection: close\r\n\r\n",
                                        output.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await output.CopyToAsync(resp);
            }
        }

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }
}
