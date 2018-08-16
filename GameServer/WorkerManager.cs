using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class WorkerManager
    {
        Worker[] workers;
        public void Start(int count = 16)
        {
            //int count = Environment.ProcessorCount * 4;
            Console.WriteLine("开启{0}个工作者线程", count);
            workers = new Worker[count];

            for (int i = 0; i < count; i++)
            {
                workers[i] = new Worker(i);
                workers[i].Start();
            }
        }

        public void Stop()
        {

        }

        public int GetWorkerCount()
        {
            return workers.Length;
        }

        public Worker AllotWorker(int hash, bool isNoTable)
        {
            int index = 0;

            if (isNoTable)
            {
                index = hash & (Program.noTableWorkerCount - 1);
                return workers[Configure.Inst.workerCount + index];
            }
                
            index = hash & (Configure.Inst.workerCount - 1);

            return workers[index];
        }
    }
}
