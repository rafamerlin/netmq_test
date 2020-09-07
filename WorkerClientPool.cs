using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace netmq_example
{
    public class WorkerClientPool
    {
        private int _nextPortNumber = 20000;
        private BlockingCollection<RequestWrapper> _blockingQueue;
        private List<WorkerClientPair> _workerClientPairs;
        public WorkerClientPool()
        {
            _blockingQueue = new BlockingCollection<RequestWrapper>(new ConcurrentQueue<RequestWrapper>());
            _workerClientPairs = new List<WorkerClientPair>();
        }

        public void StartWorkerClientPair(int numberOfWorkers)
        {
            for (int i = 0; i < numberOfWorkers; i++)
            {
                var wcp = new WorkerClientPair(_nextPortNumber++, _blockingQueue);
                wcp.StartWorker();
                _workerClientPairs.Add(wcp);
            }
        }

        public async Task<IEnumerable<string>> SendRequests(IEnumerable<string> requests)
        {
            var responses = requests.Select(r =>
                Task.Run(() =>
                {
                    var wrap = new RequestWrapper
                    {
                        Request = r,
                        Finished = new ManualResetEventSlim(false)
                    };

                    _blockingQueue.Add(wrap);
                    wrap.Finished.Wait();

                    return wrap.Response;
                })
            ).ToList();

            return (await Task.WhenAll(responses));
        }

    }
}
