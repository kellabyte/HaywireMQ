using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaywireMQ.MessageStore
{
    public interface IMessageStore : IDisposable
    {
        /// <summary>
        /// Peek the next message in the input queue.
        /// </summary>
        /// <returns>Message</returns>
        Message PeekInputQueue();

        /// <summary>
        /// Dequeue the next message in the input queue.
        /// </summary>
        /// <returns>Message</returns>
        Message DequeueInputQueue();

        /// <summary>
        /// Gets the list of output queues.
        /// </summary>
        /// <returns>List of queues</returns>
        IList<string> GetOutputQueues();

        /// <summary>
        /// Peek the next message in the specified output queue.
        /// </summary>
        /// <param name="queueAddress">Address of the output queue to peek.</param>
        /// <returns>Message</returns>
        Message PeekOutputQueue(string queueAddress);

        /// <summary>
        /// Dequeue the next message in the specified output queue.
        /// </summary>
        /// <param name="queueAddress">Address of the output queue to dequeue.</param>
        /// <returns>Message</returns>
        Message DequeueOutputQueue(string queueAddress);
    }
}
