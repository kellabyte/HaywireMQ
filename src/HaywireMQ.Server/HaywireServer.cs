using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.MessageStore;

namespace HaywireMQ.Server
{
    public class HaywireServer
    {
        private DriverCatalog catalog = null;
        public IMessageChannel MessageChannel { get; private set; }
        public IMessageStore MessageStore { get; private set; }
        public List<IMessageQueue> MessageQueues { get; private set; }

        public HaywireServer()
            : this(new DriverCatalog())
        {
        }

        public HaywireServer(DriverCatalog catalog)
        {
            this.catalog = catalog;
        }

        public void Start()
        {
            this.MessageQueues = new List<IMessageQueue>();

            InitializeMessageChannel();
            InitializeMessageStore();

            LoadMessageQueues();
        }

        private void InitializeMessageChannel()
        {
            if (catalog != null && catalog.MessageChannels != null)
            {
                // Try and find a message store that isn't the InMemoryMessageStore.
                var channels = catalog.MessageChannels.Where(x => x.GetType() != typeof(InMemoryMessageChannel));

                if (channels.Count() == 1)
                {
                    // If only one message store is present, use it.
                    this.MessageChannel = channels.FirstOrDefault();
                }
                else
                {
#if DEBUG
                    // No message store found. Let's default to the InMemoryMessageStore.
                    this.MessageChannel = catalog.MessageChannels.Where(x => x.GetType() == typeof(InMemoryMessageChannel)).FirstOrDefault();
#endif
                }
            }
            if (this.MessageChannel == null)
            {
                throw new ApplicationException("Unable to load message channel driver");
            }
        }

        private void InitializeMessageStore()
        {
            if (catalog != null && catalog.MessageStores != null)
            {
                // Try and find a message store that isn't the InMemoryMessageStore.
                var stores = catalog.MessageStores.Where(x => x.GetType() != typeof(InMemoryMessageStore));

                if (stores.Count() == 1)
                {
                    // If only one message store is present, use it.
                    this.MessageStore = stores.FirstOrDefault();
                }
                else
                {
#if DEBUG
                    // No message store found. Let's default to the InMemoryMessageStore.
                    this.MessageStore = catalog.MessageStores.Where(x => x.GetType() == typeof(InMemoryMessageStore)).FirstOrDefault();
#endif
                }
            }
            if (this.MessageStore == null)
            {
                throw new ApplicationException("Unable to load message storage driver");
            }
        }

        private void LoadMessageQueues()
        {
            this.MessageQueues.Clear();

            var ids = this.MessageStore.GetQueues();
            foreach (var id in ids)
            {
                var queue = new MessageQueue(id, this.MessageStore, this.MessageChannel);
                this.MessageQueues.Add(queue);
            }
        }
    }
}
