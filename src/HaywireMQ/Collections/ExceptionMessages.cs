//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace HaywireMQ.Collections
{
    class ExceptionMessages
    {
        public const string InvalidAsyncResult =
            "The asynchronous result object used to end this operation was not the object that was returned when the operation was initiated.";

        public const string AsyncResultAlreadyEnded = "End cannot be called twice on an AsyncResult.";

        public const string TimeoutInputQueueDequeue =
            "A Dequeue operation timed out after {0}. The time allotted to this operation may have been a portion of a longer timeout.";
    }
}