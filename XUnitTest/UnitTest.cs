using GameServer;
using MixLibrary;
using System;
using System.Threading;
using Xunit;

namespace XUnitTest
{
    public class UnitTest
    {
        public static DatabaseService dbSvc = new DatabaseService();
        public static WorkerManager workerMgr = new WorkerManager();

        public static string dbString = "server=127.0.0.1;user=root;password=123456;port=3306;IgnorePrepare=false;Pooling=false;";
        [Fact]
        public void Test1()
        {
            //dbSvc.Start(dbString, 32);
            //workerMgr.Start(32);

            int i = 0;
            Interlocked.Increment(ref i);

        }
    }
}
