using System;

namespace HaywireMQ
{
    public class Message
    {
        public string Id { get; set; }
        public string CorrelationId { get; set; }

        public Message()
        {
            this.Id = Guid.NewGuid().ToString();
        }
    }
}