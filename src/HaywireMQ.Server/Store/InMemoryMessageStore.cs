using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace HaywireMQ.Server.Store
{
    /// <summary>
    /// Implementation of <see cref="IMessageStore"/> that stores messages in memory.
    /// </summary>
    public class InMemoryMessageStore : IMessageStore
    {
        /// <summary>
        /// An entry that represents a queue in the in memory message store.
        /// </summary>
        private class QueueEntry
        {
            /// <summary>
            /// Current sequence number of the cursor.
            /// </summary>
            public ulong Cursor { get; set; }

            /// <summary>
            /// Collection of messages stored.
            /// </summary>
            public List<Message> Messages { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="QueueEntry"/> class.
            /// </summary>
            public QueueEntry()
            {
                this.Messages = new List<Message>();
            }
        }

        private ConcurrentDictionary<string, QueueEntry> queues;

        /// <summary>
        /// Iniitalizes a new instance of <see cref="InMemoryMessageStore"/> class.
        /// </summary>
        public InMemoryMessageStore()
        {
            queues = new ConcurrentDictionary<string, QueueEntry>();
        }

        /// <summary>
        /// Disposes the <see cref="InMemoryMessageStore"/>.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Creates a message queue with the specified name.
        /// </summary>
        /// <param name="queueName">Name of the message queue to create.</param>
        /// <returns>Returns whether the message queue was created.</returns>
        public bool CreateQueue(string queueName)
        {
            var entry = new QueueEntry();
            var result = queues.TryAdd(queueName, entry);
            return result;
        }

        /// <summary>
        /// Gets the available message queues.
        /// </summary>
        /// <returns>Returns list of message queues.</returns>
        public IList<string> GetQueues()
        {
            return new List<string>(queues.Keys);
        }

        /// <summary>
        /// Checks whether a message queue with the specified name exists.
        /// </summary>
        /// <param name="queueName">Name of the message queue to check.</param>
        /// <returns>Whether the message queue exists.</returns>
        public bool QueueExists(string queueName)
        {
            return this.queues.ContainsKey(queueName);
        }

        /// <summary>
        /// Gets the message from the specified message queue with the specified sequence number.
        /// </summary>
        /// <param name="queueName">Name of the message queue.</param>
        /// <param name="sequence">Sequence of the message.</param>
        /// <returns>The message retrieved from the message queue.</returns>
        public Message GetMessage(string queueName, ulong sequence)
        {
            QueueEntry entry;
            if (queues.TryGetValue(queueName, out entry))
            {
                return entry.Messages[(int)sequence - 1];
            }
            return null;
        }

        /// <summary>
        /// Get the next sequence from the specified message queue.
        /// </summary>
        /// <param name="queueName">Name of the message queue to get the next sequence from.</param>
        /// <returns>Returns the next sequence.</returns>
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

        /// <summary>
        /// Gets the count of messages in the specified message queue.
        /// </summary>
        /// <param name="queueName">Name of the message queue to get the message count from.</param>
        /// <returns>Returns the count of messages.</returns>
        public ulong GetMessageCount(string queueName)
        {
            QueueEntry entry;
            if (queues.TryGetValue(queueName, out entry))
            {
                return (ulong)entry.Messages.Count;
            }
            return ulong.MinValue;
        }

        /// <summary>
        /// Stores the specified message in the specified message queue.
        /// </summary>
        /// <param name="queueName">Name of the message queue.</param>
        /// <param name="message">Message to store.</param>
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
