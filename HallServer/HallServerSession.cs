using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixLibrary;

namespace HallServer
{
    public class HallServerSession : TcpSession
    {
        public string imageVCText = "";
        public string show_id = "";
        public string ip = "";
        public string mac = "";

        public string vc = "";

        public DateTime vcTime;
    }
}
