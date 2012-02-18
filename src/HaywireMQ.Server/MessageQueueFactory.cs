using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.Store;

namespace HaywireMQ.Server
{
    public class MessageQueueFactory
    {
        private readonly IMessageStore messageStore;
        private readonly IMessageChannel messageChannel;

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
