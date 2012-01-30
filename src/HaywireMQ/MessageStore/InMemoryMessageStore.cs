using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HaywireMQ.MessageStore
{
    public class InMemoryMessageStore : IMessageStore
    {
        private ConcurrentQueue<Message> queue;

        public InMemoryMessageStore()
        {
            queue = new ConcurrentQueue<Message>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources
            }
        }
    }
}
