using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaywireMQ.MessageStore;

namespace HaywireMQ.Server
{
    public class HaywireServer
    {
        private readonly IMessageStore messageStore;

        public HaywireServer(IMessageStore messageStore)
        {
            if (messageStore == null)
                throw new ArgumentNullException("messageStore");

            this.messageStore = messageStore;
        }
    }
}
