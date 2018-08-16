using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixLibrary;

namespace HallServer
{
    public class NetMessage
    {
        public int action;  //=0 Accepted =1 Received =2 Closed
        public HallServerSession session;
        public string content;
        public string closedCause;
        public bool isClosedInternalCause;

        public NetMessage(int action, HallServerSession session, string content = "", string closedCause = "", bool isClosedInternalCause = false)
        {
            this.action = action;
            this.session = session;
            this.content = content;
            this.closedCause = closedCause;
            this.isClosedInternalCause = isClosedInternalCause;
        }
    }
}
