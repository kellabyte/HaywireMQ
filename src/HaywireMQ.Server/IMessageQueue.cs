using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaywireMQ.Server
{
    public interface IMessageQueue : IDisposable
    {
        /// <summary>
        /// Unique identifier of the message queue.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Open the Message Queue.
        /// </summary>
        void Open();

        /// <summary>
        /// Close the Message Queue.
        /// </summary>
        void Close();

        /// <summary>
        /// Peek the next message from the message store.
        /// </summary>
        /// <returns>Message</returns>
        Message Peek();

        /// <summary>
        /// Dequeue the next message from the message store.
        /// </summary>
        /// <returns>Message</returns>
        Message Dequeue();

        /// <summary>
        /// Enqueue a message in the message store.
        /// </summary>
        /// <param name="message"></param>
        void Enqueue(Message message);
    }
}
