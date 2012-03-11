﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace HaywireMQ.Collections
{
    using System;

    //An AsyncResult that completes as soon as it is instantiated.
    class CompletedAsyncResult : AsyncResult
    {
        public CompletedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.Complete(true);
        }

        public static void End(IAsyncResult result)
        {
            AsyncResult.End<CompletedAsyncResult>(result);
        }
    }

    class CompletedAsyncResult<T> : AsyncResult
    {
        T data;

        public CompletedAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.data = data;
            this.Complete(true);
        }

        public static T End(IAsyncResult result)
        {
            CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
            return completedResult.data;
        }
    }
}