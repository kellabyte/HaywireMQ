using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using HaywireMQ.Collections;

namespace HaywireMQ.Server.Channel
{
    public class InMemoryMessageChannel : IMessageChannel
    {
        private static Dictionary<string, InputQueue<Message>> queues;

        /// <summary>
        /// Initializes an instance of the <see cref="InMemoryMessageChannel"/> class.
        /// </summary>
        public InMemoryMessageChannel(string queueId)
        {
            if (string.IsNullOrWhiteSpace(queueId))
            {
                throw new ArgumentNullException("queueId");
            }

            this.QueueId = queueId;
            if (queues == null)
            {
                queues = new Dictionary<string, InputQueue<Message>>();
            }
            queues.Add(queueId, new InputQueue<Message>());
        }

        /// <summary>
        /// Id of the queue related to the message channel.
        /// </summary>
        public string QueueId { get; private set; }

        public void Send(string queueAddress, Message message)
        {
            InputQueue<Message> queue;
            if (queues.TryGetValue(queueAddress, out queue))
            {
                queue.EnqueueAndDispatch(message);    
            }
            else
            {
                throw new InstanceNotFoundException(queueAddress);
            }
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            InputQueue<Message> queue;
            if (queues.TryGetValue(this.QueueId, out queue))
            {
                queue.BeginDequeue(timeout, EndDequeue, callback);

                //var task = Task<Message>.Factory.FromAsync<TimeSpan>(
                //    queue.BeginDequeue,
                //    queue.EndDequeue,
                //    timeout,
                //    state);

                // ExecuteSynchronously ensures that if InputQueue dispatches on the IO thread
                // we continue with the same thread.
                //task.ContinueWith(x => callback(x), TaskContinuationOptions.ExecuteSynchronously);

                //return task;
                return null;
            }
            else
            {
                throw new InstanceNotFoundException(this.QueueId);
            }
        }

        public Message EndReceive(IAsyncResult result)
        {
            //var task = (Task<Message>) result;
            //return task.Result;
            return (Message)result.AsyncState;
        }

        private void EndDequeue(IAsyncResult result)
        {
            var callback = result.AsyncState as AsyncCallback;
            InputQueue<Message> queue;
            if (queues.TryGetValue(this.QueueId, out queue))
            {
                var msg = queue.EndDequeue(result);
                var result2 = new MessageResult(msg);
                callback(result2);
            }
            else
            {
                throw new InstanceNotFoundException(this.QueueId);
            }
        }

        public class MessageResult : IAsyncResult
        {
            public MessageResult(object state)
            {
                this.AsyncState = state;
            }

            public object AsyncState { get; private set; }

            public System.Threading.WaitHandle AsyncWaitHandle
            {
                get { throw new NotImplementedException(); }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return true; }
            }
        }
    }
}
