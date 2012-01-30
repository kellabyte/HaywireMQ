using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaywireMQ
{
    public class MessageQueue : IMessageQueue
    {
        public Task<Message> ReceiveAsync()
        {
            return null;
        }

        public Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            return null;
        }

        public void Send(Message message)
        {
        }
    }
}
