﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Store;

namespace HaywireMQ.Store.Cassandra
{
    public class CassandraMessageStore : IMessageStore
    {
        public void Dispose()
        {
            // TODO
        }

        public bool CreateQueue(string queueName)
        {
            throw new NotImplementedException();
        }

        public IList<string> GetQueues()
        {
            throw new NotImplementedException();
        }

        public bool QueueExists(string queueName)
        {
            throw new NotImplementedException();
        }

        public ulong GetNextSequence(string queueName)
        {
            throw new NotImplementedException();
        }

        public ulong GetMessageCount(string queueName)
        {
            throw new NotImplementedException();
        }

        public Message GetMessage(string queueName, ulong sequence)
        {
            throw new NotImplementedException();
        }

        public void StoreMessage(string queueName, Message message)
        {
            throw new NotImplementedException();
        }
    }
}
