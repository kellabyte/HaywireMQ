using System;
using System.Threading.Tasks;

namespace HaywireMQ
{
    public interface IMessageQueue
    {
        Task<Message> ReceiveAsync();
        Task<Message> ReceiveAsync(TimeSpan timeout);
        void Send(Message message);
    }
}