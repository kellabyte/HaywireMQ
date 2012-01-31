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

        /// <summary>
        /// Peek the next message from the message store.
        /// </summary>
        /// <returns>Message</returns>
        public Message Peek()
        {
            Message message;
            queue.TryPeek(out message);
            return message;
        }

        /// <summary>
        /// Dequeue the next message from the message store.
        /// </summary>
        /// <returns>Message</returns>
        public Message Dequeue()
        {
            Message message;
            queue.TryDequeue(out message);
            return message;
        }

        /// <summary>
        /// Enqueue a message in the message store.
        /// </summary>
        /// <param name="message">Message</param>
        public void Enqueue(Message message)
        {
            queue.Enqueue(message);
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
