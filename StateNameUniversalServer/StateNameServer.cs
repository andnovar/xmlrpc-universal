﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Rpc;

namespace StateNameUniversalServer
{
    class StateNameServer : IStateName
    {
        string[] m_stateNames = { "Alabama", "Alaska", "Arizona", "Arkansas",
        "California", "Colorado", "Connecticut", "Delaware", "Florida",
        "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa",
        "Kansas", "Kentucky", "Lousiana", "Maine", "Maryland", "Massachusetts",
        "Michigan", "Minnesota", "Mississipi", "Missouri", "Montana",
        "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico",
        "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma",
        "Oregon", "Pennsylviania", "Rhose Island", "South Carolina",
        "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia",
        "Washington", "West Virginia", "Wisconsin", "Wyoming" };

        public string GetStateName(int stateNumber)
        {
            if (stateNumber < 1 || stateNumber > m_stateNames.Length)
                throw new XmlRpcFaultException(1, "Invalid state number");
            return m_stateNames[stateNumber - 1];
        }
    }
}