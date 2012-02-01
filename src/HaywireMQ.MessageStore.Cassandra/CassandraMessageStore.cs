using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaywireMQ.Server.MessageStore;

namespace HaywireMQ.MessageStore.Cassandra
{
    public class CassandraMessageStore : IMessageStore
    {
        public void Dispose()
        {
            // TODO
        }

        public IList<string> GetQueues()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
