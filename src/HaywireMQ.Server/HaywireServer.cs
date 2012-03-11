using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.Store;

namespace HaywireMQ.Server
{
    /// <summary>
    /// Service that manages all the message queues.
    /// </summary>
    public class HaywireServer
    {
        private readonly DriverCatalog catalog = null;
        private MessageQueueFactory queueFactory = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="HaywireServer"/> class.
        /// </summary>
        public HaywireServer()
            : this(new DriverCatalog())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HaywireServer"/> class.
        /// </summary>
        /// <param name="catalog">Catalog of drivers to use.</param>
        public HaywireServer(DriverCatalog catalog)
        {
            this.catalog = catalog;
        }

        /// <summary>
        /// Gets the current message channel.
        /// </summary>
        public IMessageChannel MessageChannel { get; private set; }

        /// <summary>
        /// Gets the current message store.
        /// </summary>
        public IMessageStore MessageStore { get; private set; }

        /// <summary>
        /// Gets the list of message queues.
        /// </summary>
        public List<IMessageQueue> MessageQueues { get; private set; }

        /// <summary>
        /// Start the <see cref="HaywireServer"/>. Initializes and starts message channels, message store and message queues.
        /// </summary>
        public void Start()
        {
            this.MessageQueues = new List<IMessageQueue>();

            InitializeMessageChannel();
            InitializeMessageStore();

            this.queueFactory = new MessageQueueFactory(this.MessageStore, this.MessageChannel);

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
