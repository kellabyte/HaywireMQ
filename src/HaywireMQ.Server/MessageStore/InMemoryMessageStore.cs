using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HaywireMQ.Server.MessageStore
{
    public class InMemoryMessageStore : IMessageStore
    {
        private ConcurrentDictionary<string, ConcurrentQueue<Message>> queues;

        public InMemoryMessageStore()
        {
            queues = new ConcurrentDictionary<string, ConcurrentQueue<Message>>();
        }

        public void Dispose()
        {
        }

        public IList<string> GetQueues()
        {
            return new List<string>(queues.Keys);
        }
    }
}
