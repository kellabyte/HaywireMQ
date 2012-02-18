using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace HaywireMQ.Server.Store
{
    public class InMemoryMessageStore : IMessageStore
    {
        private class QueueEntry
        {
            public ulong Cursor { get; set; }
            public List<Message> Messages { get; set; }

            public QueueEntry()
            {
                this.Messages = new List<Message>();
            }
        }

        private ConcurrentDictionary<string, QueueEntry> queues;

        public InMemoryMessageStore()
        {
            queues = new ConcurrentDictionary<string, QueueEntry>();
        }

        public void Dispose()
        {
        }

        public bool CreateQueue(string queueName)
        {
            var entry = new QueueEntry();
            var result = queues.TryAdd(queueName, entry);
            return result;
        }

        public IList<string> GetQueues()
        {
            return new List<string>(queues.Keys);
        }

        public bool QueueExists(string queueName)
        {
            return this.queues.ContainsKey(queueName);
        }

        public Message GetMessage(string queueName, ulong sequence)
        {
            QueueEntry entry;
            if (queues.TryGetValue(queueName, out entry))
            {
                return entry.Messages[(int)sequence - 1];
            }
            return null;
        }

        public ulong GetNextSequence(string queueName)
        {
            QueueEntry entry;
            if (queues.TryGetValue(queueName, out entry))
            {
                var sequence = (long)entry.Cursor;
                Interlocked.Increment(ref sequence);
                entry.Cursor = (ulong)sequence;
                return entry.Cursor;
            }
            return ulong.MinValue;
        }

        public ulong GetMessageCount(string queueName)
        {
            QueueEntry entry;
            if (queues.TryGetValue(queueName, out entry))
            {
                return (ulong)entry.Messages.Count;
            }
            return ulong.MinValue;
        }

        public void StoreMessage(string queueName, Message message)
        {
            QueueEntry entry;
            if (queues.TryGetValue(queueName, out entry))
            {
                lock (entry.Messages)
                {
                    entry.Messages.Add(message);
                }
            }
        }
    }
}
