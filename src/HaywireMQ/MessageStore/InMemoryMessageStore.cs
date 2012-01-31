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
        private ConcurrentQueue<Message> inputQueue;
        private Dictionary<string, ConcurrentQueue<Message>> outputQueues;

        public InMemoryMessageStore()
        {
            inputQueue = new ConcurrentQueue<Message>();
            outputQueues = new Dictionary<string, ConcurrentQueue<Message>>();
        }

        /// <summary>
        /// Peek the next message in the input queue.
        /// </summary>
        /// <returns>Message</returns>
        public Message PeekInputQueue()
        {
            Message message;
            inputQueue.TryPeek(out message);
            return message;
        }

        /// <summary>
        /// Dequeue the next message in the input queue.
        /// </summary>
        /// <returns>Message</returns>
        public Message DequeueInputQueue()
        {
            Message message;
            inputQueue.TryDequeue(out message);
            return message;
        }

        /// <summary>
        /// Gets the list of output queues.
        /// </summary>
        /// <returns>List of queues</returns>
        public IList<string> GetOutputQueues()
        {
            return outputQueues.Keys.ToList();
        }

        /// <summary>
        /// Peek the next message in the specified output queue.
        /// </summary>
        /// <param name="queueAddress">Address of the output queue to peek.</param>
        /// <returns>Message</returns>
        public Message PeekOutputQueue(string queueAddress)
        {
            if (string.IsNullOrWhiteSpace(queueAddress))
                throw new ArgumentException("queueAddress");

            ConcurrentQueue<Message> queue;
            if (outputQueues.TryGetValue(queueAddress, out queue))
            {
                Message message;
                queue.TryPeek(out message);
                return message;
            }
            return null;
        }

        /// <summary>
        /// Dequeue the next message in the specified output queue.
        /// </summary>
        /// <param name="queueAddress">Address of the output queue to dequeue.</param>
        /// <returns>Message</returns>
        public Message DequeueOutputQueue(string queueAddress)
        {
            if (string.IsNullOrWhiteSpace(queueAddress))
                throw new ArgumentException("queueAddress");

            ConcurrentQueue<Message> queue;
            if (outputQueues.TryGetValue(queueAddress, out queue))
            {
                Message message;
                queue.TryDequeue(out message);
                return message;
            }
            return null;
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
