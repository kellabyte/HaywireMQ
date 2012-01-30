using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaywireMQ.MessageStore;

namespace HaywireMQ.Server
{
    public class HaywireServer
    {
        private readonly IMessageStore store;

        public HaywireServer(IMessageStore messageStore)
        {
            if (store == null)
                throw new ArgumentNullException("messageStore");

            this.store = store;
        }
    }
}
