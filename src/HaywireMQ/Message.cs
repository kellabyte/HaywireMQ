using System;

namespace HaywireMQ
{
    public class Message
    {
        public ulong Id { get; set; }
        public string CorrelationId { get; set; }

        public Message()
        {
        }

        public Message(ulong id)
        {
            this.Id = id;
        }
    }
}