Based on XMLRPC dot NET by Charles Cook
Source: http://xml-rpc.net/ and https://code.google.com/p/xmlrpcnet/

To install in your project:
```sh
PM> Install-Package xmlrpc-universal
```



XMLRPC-Universal is a library for consuming XML RPC services from a Windows UWP application.  For each web service, you should create a Proxy class that looks like this:




```cs
using Windows.Data.Xml.Rpc;

[XmlRpcUrl("http://www.cookcomputing.com/xmlrpcsamples/RPC2.ashx")]
class TestProxy : XmlRpcClientProtocol
{
    //Declare async RPC methods here
}
```


To implement the RPC methods, the declarations should look like this:

```cs
[XmlRpcMethod("examples.getStateName")]
public async Task<String> GetStateName(int stateIndex)
{
    //You must pass your parameters as an object array.
    return await InvokeAsync<String>(new object[] { number });
    
}
```

There is a sample application included.


XMLRPC-Universal can also start an StreamSocketListener server for incoming requests

```cs
const int port = 8000;
StateNameListenerService statelistener = new StateNameListenerService();
XmlRpcHttpServer server = new XmlRpcHttpServer(port, statelistener);
```

The declarations for the methods should look like this:

```cs
public class StateNameListenerService : XmlRpcServerProtocol
    {
        [XmlRpcMethod("examples.getStateName")]
        public string GetStateName(int stateNumber)
        {
            if (stateNumber < 1 || stateNumber > m_stateNames.Length)
                throw new XmlRpcFaultException(1, "Invalid state number");
            return m_stateNames[stateNumber - 1];
        }

        string[] m_stateNames
          = { "Alabama", "Alaska", "Arizona", "Arkansas",
        "California", "Colorado", "Connecticut", "Delaware", "Florida",
        "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa",
        "Kansas", "Kentucky", "Lousiana", "Maine", "Maryland", "Massachusetts",
        "Michigan", "Minnesota", "Mississipi", "Missouri", "Montana",
        "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico",
        "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma",
        "Oregon", "Pennsylviania", "Rhose Island", "South Carolina",
        "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia",
        "Washington", "West Virginia", "Wisconsin", "Wyoming" };
    }
```