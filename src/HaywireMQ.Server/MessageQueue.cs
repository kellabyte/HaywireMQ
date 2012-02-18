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

        public void Open()
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Close()
        {
            // TODO
            throw new NotImplementedException();            
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
            ulong sequence = store.GetNextSequence(this.Id);
            if (sequence == ulong.MinValue)
            {
                // TODO: Throw exception, queue wasn't found.
            }
            else
            {
                message.Id = sequence;
                store.StoreMessage("test", message);
            }
        }

        public void Dispose()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
