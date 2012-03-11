//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace HaywireMQ.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    // Executes a callback on a process thread pool I/O completion port.
    static class SimpleIOThreadScheduler
    {
        static object mutex = new object();
        static bool isCompletionQueued;
        static Queue<ManagedCallback> managedCallbacks = new Queue<ManagedCallback>();
        static OverlappedIOCallback overlappedIoCallback = new OverlappedIOCallback(CompletionCallback, ExceptionHandler);

        static void CompletionCallback(object state)
        {
            lock (mutex)
            {
                isCompletionQueued = false;
            }
            ProcessManagedCallbacks();
        }

        static bool ExceptionHandler(Exception exception)
        {
            // absorb all exceptions here
            return true;
        }

        static void ProcessManagedCallbacks()
        {
            while (true)
            {
                ManagedCallback managedCallback = null;

                lock (mutex)
                {
                    if (managedCallbacks.Count == 0)
                    {
                        break;
                    }

                    managedCallback = managedCallbacks.Dequeue();
                    if (!isCompletionQueued && managedCallbacks.Count > 0)
                    {
                        QueueCompletionCallback();
                    }
                }
                managedCallback.Invoke();
            }
        }

        static unsafe void QueueCompletionCallback()
        {
            ThreadPool.UnsafeQueueNativeOverlapped(overlappedIoCallback.nativeOverlapped);
            isCompletionQueued = true;
        }

        public static void ScheduleCallback(WaitCallback callback, ExceptionCallback exceptionCallback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            object workItemState = state;
            lock (mutex)
            {
                managedCallbacks.Enqueue(new ManagedCallback(callback, exceptionCallback, workItemState));
                if (!isCompletionQueued)
                {
                    QueueCompletionCallback();
                }
            }
        }

        unsafe class IOCompletionThunk
        {
            IOCompletionCallback callback;
            ExceptionCallback exceptionCallback;

            public IOCompletionThunk(IOCompletionCallback callback, ExceptionCallback exceptionCallback)
            {
                if (callback == null) throw new ArgumentNullException("callback");
                if (exceptionCallback == null) throw new ArgumentNullException("exceptionCallback");

                this.callback = callback;
                this.exceptionCallback = exceptionCallback;
            }

            public IOCompletionCallback ThunkFrame
            {
                get { return this.UnhandledExceptionFrame; }
            }

            void UnhandledExceptionFrame(uint error, uint bytesRead, NativeOverlapped* nativeOverlapped)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    this.callback(error, bytesRead, nativeOverlapped);
                }
                catch (Exception exception)
                {
                    if (!this.exceptionCallback(exception))
                    {
                        throw;
                    }
                }
            }
        }

        class ManagedCallback
        {
            WaitCallback callback;
            ExceptionCallback exceptionCallback;
            object state;

            public ManagedCallback(WaitCallback callback, ExceptionCallback exceptionCallback, object state)
            {
                this.callback = callback;
                this.exceptionCallback = exceptionCallback;
                this.state = state;
            }

            public void Invoke()
            {
                try
                {
                    this.callback(this.state);
                }
                catch (Exception e)
                {
                    if (this.exceptionCallback == null ||
                        !this.exceptionCallback(e))
                    {
                        throw;
                    }
                }
            }
        }

        class OverlappedIOCallback
        {
            WaitCallback callback;
            public unsafe NativeOverlapped* nativeOverlapped;

            public unsafe OverlappedIOCallback(WaitCallback callback, ExceptionCallback exceptionCallback)
            {
                Overlapped overlapped = new Overlapped(0, 0, IntPtr.Zero, null);
                this.nativeOverlapped = overlapped.UnsafePack(new IOCompletionThunk(this.IOCallback, exceptionCallback).ThunkFrame, null);
                this.callback = callback;
            }

            unsafe void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                this.callback(null);
            }
        }
    }

    public delegate bool ExceptionCallback(Exception exception);
}