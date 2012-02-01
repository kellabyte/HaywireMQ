using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.MessageStore;

namespace HaywireMQ.Server
{
    public class ModuleCatalog
    {
        private CompositionContainer container = null;

        [ImportMany]
        public List<IMessageStore> MessageStores { get; set; }

        [ImportMany]
        public List<IMessageChannel> MessageChannels { get; set; }

        public ModuleCatalog()
        {
            this.MessageStores = new List<IMessageStore>();
            this.MessageChannels = new List<IMessageChannel>();
            InitializeComposition();
        }

        private void InitializeComposition()
        {
            var catalogs = new List<ComposablePartCatalog>();
            catalogs.Add(new AssemblyCatalog(this.GetType().Assembly));

            try
            {
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
                container.ComposeParts(this);
            }
            catch (Exception e)
            {
            }
        }
    }
}
