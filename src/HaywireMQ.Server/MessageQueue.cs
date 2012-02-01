using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.MessageStore;

namespace HaywireMQ.Server
{
    public class MessageQueue : IMessageQueue
    {
        private readonly IMessageStore store;
        private readonly IMessageChannel channel;

        public string Id { get; private set; }

        public MessageQueue(string id, IMessageStore store, IMessageChannel channel)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("id");
            }

            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            this.Id = id;
            this.store = store;
            this.channel = channel;
        }

        public Message Peek()
        {
            // TODO
            throw new NotImplementedException();
        }

        public Message Dequeue()
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Enqueue(Message message)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
