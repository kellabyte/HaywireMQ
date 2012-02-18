using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace HaywireMQ.Server.MessageStore
{
    [InheritedExport]
    public interface IMessageStore : IDisposable
    {
        /// <summary>
        /// Get the ids of the available queues.
        /// </summary>
        /// <returns>Ids of available queues.</returns>
        IList<string> GetQueues();

        /// <summary>
        /// Get the next sequence id.
        /// </summary>
        /// <param name="queueName">Name of the queue to get the next sequence id from.</param>
        /// <returns>Sequence Id</returns>
        ulong GetNextSequence(string queueName);

        /// <summary>
        /// Get the count of messages in the queue.
        /// </summary>
        /// <param name="queueName">Name of the queue to get the message count from.</param>
        /// <returns>Count of messages</returns>
        ulong GetMessageCount(string queueName);

        /// <summary>
        /// Get's the next message in the Message Store.
        /// </summary>
        /// <returns>Next message</returns>
        Message GetMessage(string queueName, ulong sequence);

        /// <summary>
        /// Create a queue with the specified queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue to create.</param>
        /// <returns>Whether the queue was created.</returns>
        bool CreateQueue(string queueName);

        /// <summary>
        /// Stores the specified message in the message store.
        /// </summary>
        /// <param name="queueName">Queue to store the message in.</param>
        /// <param name="message">Message to store.</param>
        void StoreMessage(string queueName, Message message);

    }
}
