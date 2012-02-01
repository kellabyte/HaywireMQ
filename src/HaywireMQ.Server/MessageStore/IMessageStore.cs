using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace HaywireMQ.Server.MessageStore
{
    [InheritedExport]
    public interface IMessageStore : IDisposable
    {
        /// <summary>
        /// Get the ids of the available queues.
        /// </summary>
        /// <returns>Ids of available queues.</returns>
        IList<string> GetQueues();
    }
}
