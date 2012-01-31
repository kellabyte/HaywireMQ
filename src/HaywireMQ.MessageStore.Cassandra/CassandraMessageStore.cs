using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaywireMQ.MessageStore.Cassandra
{
    public class CassandraMessageStore : IMessageStore
    {
        public Message Peek()
        {
            // TODO
            return null;
        }

        public Message Dequeue()
        {
            // TODO
            return null;
        }

        public void Enqueue(Message message)
        {
            // TODO
        }

        public void Dispose()
        {
            // TODO
        }
    }
}
