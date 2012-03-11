using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.Store;

namespace HaywireMQ.Server
{
    /// <summary>
    /// Factory that creates and returns instances of <see cref="MessageQueue"/>.
    /// </summary>
    public class MessageQueueFactory
    {
        private readonly IMessageStore messageStore;
        private readonly IMessageChannel messageChannel;

        /// <summary>
        /// Initialiazes a new instance of the <see cref="MessageQueueFactory"/> class.
        /// </summary>
        /// <param name="messageStore">The message store that created <see cref="MessageQueue"/> will use.</param>
        /// <param name="messageChannel">The message channel that created <see cref="MessageQueue"/> will use.</param>
        public MessageQueueFactory(IMessageStore messageStore, IMessageChannel messageChannel)
        {
            if (messageStore == null)
            {
                throw new ArgumentNullException("messageStore");
            }
            if (messageChannel == null)
            {
                throw new ArgumentNullException("messageChannel");
            }

            this.messageStore = messageStore;
            this.messageChannel = messageChannel;
        }

        /// <summary>
        /// Creates specified queue in the message store and returns a <see cref="MessageQueue"/>.
        /// </summary>
        /// <param name="queueName">Name of the message queue to create.</param>
        /// <returns>Created <see cref="MessageQueue"/></returns>
        public MessageQueue Create(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("queueName");
            }
            
            // Check if the queue already exists.
            if (messageStore.QueueExists(queueName))
            {
                throw new DuplicateNameException(queueName);
            }
            else
            {
                if (messageStore.CreateQueue(queueName))
                {
                    var queue = new MessageQueue(queueName, messageStore, messageChannel);
                    return queue;
                }
                else
                {
                    throw new InvalidOperationException(queueName);
                }
            }
        }
    }
}
