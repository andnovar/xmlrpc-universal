using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Rpc;

namespace StateNameUniversalServer
{
    public interface IStateName
    {
        [XmlRpcMethod("examples.getStateName")]
        string GetStateName(int stateNumber);
    }
}
