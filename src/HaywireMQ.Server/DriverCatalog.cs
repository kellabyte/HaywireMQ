using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.Store;

namespace HaywireMQ.Server
{
    public class DriverCatalog
    {
        private CompositionContainer container = null;

        [ImportMany]
        public List<IMessageStore> MessageStores { get; set; }

        [ImportMany]
        public List<IMessageChannel> MessageChannels { get; set; }

        public DriverCatalog()
            : this(null, null)
        {
        }

        public DriverCatalog(IMessageStore messageStore, IMessageChannel messageChannel)
        {
            this.MessageStores = new List<IMessageStore>();
            this.MessageChannels = new List<IMessageChannel>();

            if (messageStore != null)
            {
                this.MessageStores.Add(messageStore);
            }
            if (messageChannel != null)
            {
                this.MessageChannels.Add(messageChannel);
            }
            InitializeComposition();            
        }

        private void InitializeComposition()
        {
            var messageStores = new List<IMessageStore>(this.MessageStores);
            var messageChannels = new List<IMessageChannel>(this.MessageChannels);

            var catalogs = new List<ComposablePartCatalog>();
            catalogs.Add(new AssemblyCatalog(this.GetType().Assembly));

            try
            {
                // Add a directory catalog to load drivers from via MEF.
                catalogs.Add(new DirectoryCatalog(@".\Drivers\MessageStore\"));
            }
            catch (Exception)
            {
                // TODO: Put some logging here.
                //throw;
            }
            var catalog = new AggregateCatalog(catalogs);
            container = new CompositionContainer(catalog);

            try
            {
                // Compose the drivers from MEF.
                container.ComposeParts(this);

                // Add back the drivers we were given.
                this.MessageStores.InsertRange(0, messageStores);
                this.MessageChannels.InsertRange(0, messageChannels);
            }
            catch (Exception e)
            {
            }
        }
    }
}
