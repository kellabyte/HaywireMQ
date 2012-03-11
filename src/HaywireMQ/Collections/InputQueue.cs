//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace HaywireMQ.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public sealed class InputQueue<T> : IDisposable where T : class
    {
        static WaitCallback completeOutstandingReadersCallback;
        static WaitCallback completeWaitersFalseCallback;
        static WaitCallback completeWaitersTrueCallback;
        static WaitCallback onDispatchCallback;
        static WaitCallback onInvokeDequeuedCallback;

        readonly ItemQueue itemQueue;

        readonly Queue<IQueueReader> readerQueue;

        readonly List<IQueueWaiter> waiterList;
        QueueState queueState;

        public InputQueue()
        {
            this.itemQueue = new ItemQueue();
            this.readerQueue = new Queue<IQueueReader>();
            this.waiterList = new List<IQueueWaiter>();
            this.queueState = QueueState.Open;
        }

        public InputQueue(Func<Action<AsyncCallback, IAsyncResult>> asyncCallbackGenerator)
            : this()
        {
            this.AsyncCallbackGenerator = asyncCallbackGenerator;
        }

        public int PendingCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.itemQueue.ItemCount;
                }
            }
        }

        public Action<T> DisposeItemCallback { get; set; }

        Func<Action<AsyncCallback, IAsyncResult>> AsyncCallbackGenerator { get; set; }

        object ThisLock
        {
            get { return this.itemQueue; }
        }

        public void Dispose()
        {
            bool dispose = false;

            lock (this.ThisLock)
            {
                if (this.queueState != QueueState.Closed)
                {
                    this.queueState = QueueState.Closed;
                    dispose = true;
                }
            }

            if (dispose)
            {
                while (this.readerQueue.Count > 0)
                {
                    IQueueReader reader = this.readerQueue.Dequeue();
                    reader.Set(default(Item));
                }

                while (this.itemQueue.HasAnyItem)
                {
                    Item item = this.itemQueue.DequeueAnyItem();
                    this.DisposeItem(item);
                    InvokeDequeuedCallback(item.DequeuedCallback);
                }
            }
        }

        public IAsyncResult BeginDequeue(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Item item = default(Item);

            lock (this.ThisLock)
            {
                if (this.queueState == QueueState.Open)
                {
                    if (this.itemQueue.HasAvailableItem)
                    {
                        item = this.itemQueue.DequeueAvailableItem();
                    }
                    else
                    {
                        var reader = new AsyncQueueReader(this, timeout, callback, state);
                        this.readerQueue.Enqueue(reader);
                        return reader;
                    }
                }
                else if (this.queueState == QueueState.Shutdown)
                {
                    if (this.itemQueue.HasAvailableItem)
                    {
                        item = this.itemQueue.DequeueAvailableItem();
                    }
                    else if (this.itemQueue.HasAnyItem)
                    {
                        var reader = new AsyncQueueReader(this, timeout, callback, state);
                        this.readerQueue.Enqueue(reader);
                        return reader;
                    }
                }
            }

            InvokeDequeuedCallback(item.DequeuedCallback);
            return new CompletedAsyncResult<T>(item.GetValue(), callback, state);
        }

        public IAsyncResult BeginWaitForItem(TimeSpan timeout, AsyncCallback callback, object state)
        {
            lock (this.ThisLock)
            {
                if (this.queueState == QueueState.Open)
                {
                    if (!this.itemQueue.HasAvailableItem)
                    {
                        var waiter = new AsyncQueueWaiter(timeout, callback, state);
                        this.waiterList.Add(waiter);
                        return waiter;
                    }
                }
                else if (this.queueState == QueueState.Shutdown)
                {
                    if (!this.itemQueue.HasAvailableItem && this.itemQueue.HasAnyItem)
                    {
                        var waiter = new AsyncQueueWaiter(timeout, callback, state);
                        this.waiterList.Add(waiter);
                        return waiter;
                    }
                }
            }

            return new CompletedAsyncResult<bool>(true, callback, state);
        }

        public void Close()
        {
            this.Dispose();
        }

        public T Dequeue(TimeSpan timeout)
        {
            T value;

            if (!this.Dequeue(timeout, out value))
            {
                throw new TimeoutException(ExceptionMessages.TimeoutInputQueueDequeue);
            }

            return value;
        }

        public bool Dequeue(TimeSpan timeout, out T value)
        {
            WaitQueueReader reader = null;
            var item = new Item();

            lock (this.ThisLock)
            {
                if (this.queueState == QueueState.Open)
                {
                    if (this.itemQueue.HasAvailableItem)
                    {
                        item = this.itemQueue.DequeueAvailableItem();
                    }
                    else
                    {
                        reader = new WaitQueueReader(this);
                        this.readerQueue.Enqueue(reader);
                    }
                }
                else if (this.queueState == QueueState.Shutdown)
                {
                    if (this.itemQueue.HasAvailableItem)
                    {
                        item = this.itemQueue.DequeueAvailableItem();
                    }
                    else if (this.itemQueue.HasAnyItem)
                    {
                        reader = new WaitQueueReader(this);
                        this.readerQueue.Enqueue(reader);
                    }
                    else
                    {
                        value = default(T);
                        return true;
                    }
                }
                else // queueState == QueueState.Closed
                {
                    value = default(T);
                    return true;
                }
            }

            if (reader != null)
            {
                return reader.Wait(timeout, out value);
            }
            else
            {
                InvokeDequeuedCallback(item.DequeuedCallback);
                value = item.GetValue();
                return true;
            }
        }

        public void Dispatch()
        {
            IQueueReader reader = null;
            var item = new Item();
            IQueueReader[] outstandingReaders = null;
            IQueueWaiter[] waiters = null;
            bool itemAvailable = true;

            lock (this.ThisLock)
            {
                itemAvailable = !((this.queueState == QueueState.Closed) || (this.queueState == QueueState.Shutdown));
                this.GetWaiters(out waiters);

                if (this.queueState != QueueState.Closed)
                {
                    this.itemQueue.MakePendingItemAvailable();

                    if (this.readerQueue.Count > 0)
                    {
                        item = this.itemQueue.DequeueAvailableItem();
                        reader = this.readerQueue.Dequeue();

                        if (this.queueState == QueueState.Shutdown && this.readerQueue.Count > 0 &&
                            this.itemQueue.ItemCount == 0)
                        {
                            outstandingReaders = new IQueueReader[this.readerQueue.Count];
                            this.readerQueue.CopyTo(outstandingReaders, 0);
                            this.readerQueue.Clear();

                            itemAvailable = false;
                        }
                    }
                }
            }

            if (outstandingReaders != null)
            {
                if (completeOutstandingReadersCallback == null)
                {
                    completeOutstandingReadersCallback = new WaitCallback(CompleteOutstandingReadersCallback);
                }

                SimpleIOThreadScheduler.ScheduleCallback(completeOutstandingReadersCallback, null, outstandingReaders);
            }

            if (waiters != null)
            {
                CompleteWaitersLater(itemAvailable, waiters);
            }

            if (reader != null)
            {
                InvokeDequeuedCallback(item.DequeuedCallback);
                reader.Set(item);
            }
        }

        public bool TryEndDequeue(IAsyncResult result, out T value)
        {
            var typedResult = result as CompletedAsyncResult<T>;

            if (typedResult != null)
            {
                value = CompletedAsyncResult<T>.End(result);
                return true;
            }

            return AsyncQueueReader.End(result, out value);
        }

        public T EndDequeue(IAsyncResult result)
        {
            T value;

            if (!this.TryEndDequeue(result, out value))
            {
                throw new TimeoutException();
            }

            return value;
        }

        public bool EndWaitForItem(IAsyncResult result)
        {
            var typedResult = result as CompletedAsyncResult<bool>;
            if (typedResult != null)
            {
                return CompletedAsyncResult<bool>.End(result);
            }

            return AsyncQueueWaiter.End(result);
        }

        public void EnqueueAndDispatch(T item)
        {
            this.EnqueueAndDispatch(item, null);
        }

        // dequeuedCallback is called as an item is dequeued from the InputQueue.  The 
        // InputQueue lock is not held during the callback.  However, the user code will
        // not be notified of the item being available until the callback returns.  If you
        // are not sure if the callback will block for a long time, then first call 
        // IOThreadScheduler.ScheduleCallback to get to a "safe" thread.
        public void EnqueueAndDispatch(T item, Action dequeuedCallback)
        {
            EnqueueAndDispatch(item, dequeuedCallback, true);
        }

        public void EnqueueAndDispatch(Exception exception, Action dequeuedCallback, bool canDispatchOnThisThread)
        {
            this.EnqueueAndDispatch(new Item(exception, dequeuedCallback), canDispatchOnThisThread);
        }

        public void EnqueueAndDispatch(T item, Action dequeuedCallback, bool canDispatchOnThisThread)
        {
            this.EnqueueAndDispatch(new Item(item, dequeuedCallback), canDispatchOnThisThread);
        }

        public bool EnqueueWithoutDispatch(T item, Action dequeuedCallback)
        {
            return this.EnqueueWithoutDispatch(new Item(item, dequeuedCallback));
        }

        public bool EnqueueWithoutDispatch(Exception exception, Action dequeuedCallback)
        {
            return this.EnqueueWithoutDispatch(new Item(exception, dequeuedCallback));
        }


        public void Shutdown()
        {
            this.Shutdown(null);
        }

        // Don't let any more items in. Differs from Close in that we keep around
        // existing items in our itemQueue for possible future calls to Dequeue
        public void Shutdown(Func<Exception> pendingExceptionGenerator)
        {
            IQueueReader[] outstandingReaders = null;
            lock (this.ThisLock)
            {
                if (this.queueState == QueueState.Shutdown)
                {
                    return;
                }

                if (this.queueState == QueueState.Closed)
                {
                    return;
                }

                this.queueState = QueueState.Shutdown;

                if (this.readerQueue.Count > 0 && this.itemQueue.ItemCount == 0)
                {
                    outstandingReaders = new IQueueReader[this.readerQueue.Count];
                    this.readerQueue.CopyTo(outstandingReaders, 0);
                    this.readerQueue.Clear();
                }
            }

            if (outstandingReaders != null)
            {
                for (int i = 0; i < outstandingReaders.Length; i++)
                {
                    Exception exception = (pendingExceptionGenerator != null) ? pendingExceptionGenerator() : null;
                    outstandingReaders[i].Set(new Item(exception, null));
                }
            }
        }

        public bool WaitForItem(TimeSpan timeout)
        {
            WaitQueueWaiter waiter = null;
            bool itemAvailable = false;

            lock (this.ThisLock)
            {
                if (this.queueState == QueueState.Open)
                {
                    if (this.itemQueue.HasAvailableItem)
                    {
                        itemAvailable = true;
                    }
                    else
                    {
                        waiter = new WaitQueueWaiter();
                        this.waiterList.Add(waiter);
                    }
                }
                else if (this.queueState == QueueState.Shutdown)
                {
                    if (this.itemQueue.HasAvailableItem)
                    {
                        itemAvailable = true;
                    }
                    else if (this.itemQueue.HasAnyItem)
                    {
                        waiter = new WaitQueueWaiter();
                        this.waiterList.Add(waiter);
                    }
                    else
                    {
                        return true;
                    }
                }
                else // queueState == QueueState.Closed
                {
                    return true;
                }
            }

            if (waiter != null)
            {
                return waiter.Wait(timeout);
            }
            else
            {
                return itemAvailable;
            }
        }

        void DisposeItem(Item item)
        {
            T value = item.Value;
            if (value != null)
            {
                if (value is IDisposable)
                {
                    ((IDisposable) value).Dispose();
                }
                else
                {
                    Action<T> disposeItemCallback = this.DisposeItemCallback;
                    if (disposeItemCallback != null)
                    {
                        disposeItemCallback(value);
                    }
                }
            }
        }

        static void CompleteOutstandingReadersCallback(object state)
        {
            var outstandingReaders = (IQueueReader[]) state;

            for (int i = 0; i < outstandingReaders.Length; i++)
            {
                outstandingReaders[i].Set(default(Item));
            }
        }

        static void CompleteWaiters(bool itemAvailable, IQueueWaiter[] waiters)
        {
            for (int i = 0; i < waiters.Length; i++)
            {
                waiters[i].Set(itemAvailable);
            }
        }

        static void CompleteWaitersFalseCallback(object state)
        {
            CompleteWaiters(false, (IQueueWaiter[]) state);
        }

        static void CompleteWaitersLater(bool itemAvailable, IQueueWaiter[] waiters)
        {
            if (itemAvailable)
            {
                if (completeWaitersTrueCallback == null)
                {
                    completeWaitersTrueCallback = new WaitCallback(CompleteWaitersTrueCallback);
                }

                SimpleIOThreadScheduler.ScheduleCallback(completeWaitersTrueCallback, null, waiters);
            }
            else
            {
                if (completeWaitersFalseCallback == null)
                {
                    completeWaitersFalseCallback = new WaitCallback(CompleteWaitersFalseCallback);
                }

                SimpleIOThreadScheduler.ScheduleCallback(completeWaitersFalseCallback, null, waiters);
            }
        }

        static void CompleteWaitersTrueCallback(object state)
        {
            CompleteWaiters(true, (IQueueWaiter[]) state);
        }

        static void InvokeDequeuedCallback(Action dequeuedCallback)
        {
            if (dequeuedCallback != null)
            {
                dequeuedCallback();
            }
        }

        static void InvokeDequeuedCallbackLater(Action dequeuedCallback)
        {
            if (dequeuedCallback != null)
            {
                if (onInvokeDequeuedCallback == null)
                {
                    onInvokeDequeuedCallback = new WaitCallback(OnInvokeDequeuedCallback);
                }

                SimpleIOThreadScheduler.ScheduleCallback(onInvokeDequeuedCallback, null, dequeuedCallback);
            }
        }

        static void OnDispatchCallback(object state)
        {
            ((InputQueue<T>) state).Dispatch();
        }

        static void OnInvokeDequeuedCallback(object state)
        {
            var dequeuedCallback = (Action) state;
            dequeuedCallback();
        }

        void EnqueueAndDispatch(Item item, bool canDispatchOnThisThread)
        {
            bool disposeItem = false;
            IQueueReader reader = null;
            bool dispatchLater = false;
            IQueueWaiter[] waiters = null;
            bool itemAvailable = true;

            lock (this.ThisLock)
            {
                itemAvailable = !((this.queueState == QueueState.Closed) || (this.queueState == QueueState.Shutdown));
                this.GetWaiters(out waiters);

                if (this.queueState == QueueState.Open)
                {
                    if (canDispatchOnThisThread)
                    {
                        if (this.readerQueue.Count == 0)
                        {
                            this.itemQueue.EnqueueAvailableItem(item);
                        }
                        else
                        {
                            reader = this.readerQueue.Dequeue();
                        }
                    }
                    else
                    {
                        if (this.readerQueue.Count == 0)
                        {
                            this.itemQueue.EnqueueAvailableItem(item);
                        }
                        else
                        {
                            this.itemQueue.EnqueuePendingItem(item);
                            dispatchLater = true;
                        }
                    }
                }
                else // queueState == QueueState.Closed || queueState == QueueState.Shutdown
                {
                    disposeItem = true;
                }
            }

            if (waiters != null)
            {
                if (canDispatchOnThisThread)
                {
                    CompleteWaiters(itemAvailable, waiters);
                }
                else
                {
                    CompleteWaitersLater(itemAvailable, waiters);
                }
            }

            if (reader != null)
            {
                InvokeDequeuedCallback(item.DequeuedCallback);
                reader.Set(item);
            }

            if (dispatchLater)
            {
                if (onDispatchCallback == null)
                {
                    onDispatchCallback = new WaitCallback(OnDispatchCallback);
                }

                SimpleIOThreadScheduler.ScheduleCallback(onDispatchCallback, null, this);
            }
            else if (disposeItem)
            {
                InvokeDequeuedCallback(item.DequeuedCallback);
                this.DisposeItem(item);
            }
        }

        // This will not block, however, Dispatch() must be called later if this function
        // returns true.
        bool EnqueueWithoutDispatch(Item item)
        {
            lock (this.ThisLock)
            {
                // Open
                if (this.queueState != QueueState.Closed && this.queueState != QueueState.Shutdown)
                {
                    if (this.readerQueue.Count == 0 && this.waiterList.Count == 0)
                    {
                        this.itemQueue.EnqueueAvailableItem(item);
                        return false;
                    }
                    else
                    {
                        this.itemQueue.EnqueuePendingItem(item);
                        return true;
                    }
                }
            }

            this.DisposeItem(item);
            InvokeDequeuedCallbackLater(item.DequeuedCallback);
            return false;
        }

        void GetWaiters(out IQueueWaiter[] waiters)
        {
            if (this.waiterList.Count > 0)
            {
                waiters = this.waiterList.ToArray();
                this.waiterList.Clear();
            }
            else
            {
                waiters = null;
            }
        }

        // Used for timeouts. The InputQueue must remove readers from its reader queue to prevent
        // dispatching items to timed out readers.
        bool RemoveReader(IQueueReader reader)
        {
            lock (this.ThisLock)
            {
                if (this.queueState == QueueState.Open || this.queueState == QueueState.Shutdown)
                {
                    bool removed = false;

                    for (int i = this.readerQueue.Count; i > 0; i--)
                    {
                        IQueueReader temp = this.readerQueue.Dequeue();
                        if (ReferenceEquals(temp, reader))
                        {
                            removed = true;
                        }
                        else
                        {
                            this.readerQueue.Enqueue(temp);
                        }
                    }

                    return removed;
                }
            }

            return false;
        }


        class AsyncQueueReader : AsyncResult, IQueueReader
        {
            static readonly TimerCallback timerCallback = TimerCallback;

            readonly InputQueue<T> inputQueue;
            readonly Timer timer;
            bool expired;
            T item;

            public AsyncQueueReader(InputQueue<T> inputQueue, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                if (inputQueue.AsyncCallbackGenerator != null)
                {
                    base.VirtualCallback = inputQueue.AsyncCallbackGenerator();
                }
                this.inputQueue = inputQueue;
                if (timeout != TimeSpan.MaxValue)
                {
                    this.timer = new Timer(timerCallback, this, timeout, TimeSpan.FromMilliseconds(-1));
                }
            }


            public void Set(Item item)
            {
                this.item = item.Value;
                if (this.timer != null)
                {
                    this.timer.Change(-1, -1);
                }
                this.Complete(false, item.Exception);
            }


            public static bool End(IAsyncResult result, out T value)
            {
                AsyncQueueReader readerResult = AsyncResult.End<AsyncQueueReader>(result);

                if (readerResult.expired)
                {
                    value = default(T);
                    return false;
                }
                else
                {
                    value = readerResult.item;
                    return true;
                }
            }

            static void TimerCallback(object state)
            {
                var thisPtr = (AsyncQueueReader) state;
                if (thisPtr.inputQueue.RemoveReader(thisPtr))
                {
                    thisPtr.expired = true;
                    thisPtr.Complete(false);
                }
            }
        }

        class AsyncQueueWaiter : AsyncResult, IQueueWaiter
        {
            static readonly TimerCallback timerCallback = TimerCallback;

            readonly object thisLock = new object();

            readonly Timer timer;
            bool itemAvailable;

            public AsyncQueueWaiter(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
            {
                if (timeout != TimeSpan.MaxValue)
                {
                    this.timer = new Timer(timerCallback, this, timeout, TimeSpan.FromMilliseconds(-1));
                }
            }

            object ThisLock
            {
                get { return this.thisLock; }
            }


            public void Set(bool itemAvailable)
            {
                bool timely;

                lock (this.ThisLock)
                {
                    timely = (this.timer == null) || this.timer.Change(-1, -1);
                    this.itemAvailable = itemAvailable;
                }

                if (timely)
                {
                    this.Complete(false);
                }
            }


            public static bool End(IAsyncResult result)
            {
                AsyncQueueWaiter waiterResult = AsyncResult.End<AsyncQueueWaiter>(result);
                return waiterResult.itemAvailable;
            }

            static void TimerCallback(object state)
            {
                var thisPtr = (AsyncQueueWaiter) state;
                thisPtr.Complete(false);
            }
        }

        interface IQueueReader
        {
            void Set(Item item);
        }


        interface IQueueWaiter
        {
            void Set(bool itemAvailable);
        }

        struct Item
        {
            readonly Action dequeuedCallback;
            readonly Exception exception;
            readonly T value;

            public Item(T value, Action dequeuedCallback)
                : this(value, null, dequeuedCallback)
            {
            }

            public Item(Exception exception, Action dequeuedCallback)
                : this(null, exception, dequeuedCallback)
            {
            }

            Item(T value, Exception exception, Action dequeuedCallback)
            {
                this.value = value;
                this.exception = exception;
                this.dequeuedCallback = dequeuedCallback;
            }

            public Action DequeuedCallback
            {
                get { return this.dequeuedCallback; }
            }

            public Exception Exception
            {
                get { return this.exception; }
            }

            public T Value
            {
                get { return this.value; }
            }

            public T GetValue()
            {
                if (this.exception != null)
                {
                    throw this.exception;
                }

                return this.value;
            }
        }

        class ItemQueue
        {
            int head;
            Item[] items;
            int pendingCount;
            int totalCount;

            public ItemQueue()
            {
                this.items = new Item[1];
            }

            public bool HasAnyItem
            {
                get { return this.totalCount > 0; }
            }

            public bool HasAvailableItem
            {
                get { return this.totalCount > this.pendingCount; }
            }

            public int ItemCount
            {
                get { return this.totalCount; }
            }

            public Item DequeueAnyItem()
            {
                if (this.pendingCount == this.totalCount)
                {
                    this.pendingCount--;
                }
                return this.DequeueItemCore();
            }

            public Item DequeueAvailableItem()
            {
                return this.DequeueItemCore();
            }

            public void EnqueueAvailableItem(Item item)
            {
                this.EnqueueItemCore(item);
            }

            public void EnqueuePendingItem(Item item)
            {
                this.EnqueueItemCore(item);
                this.pendingCount++;
            }

            public void MakePendingItemAvailable()
            {
                this.pendingCount--;
            }

            Item DequeueItemCore()
            {
                Item item = this.items[this.head];
                this.items[this.head] = new Item();
                this.totalCount--;
                this.head = (this.head + 1)%this.items.Length;
                return item;
            }

            void EnqueueItemCore(Item item)
            {
                if (this.totalCount == this.items.Length)
                {
                    var newItems = new Item[this.items.Length*2];
                    for (int i = 0; i < this.totalCount; i++)
                    {
                        newItems[i] = this.items[(this.head + i)%this.items.Length];
                    }
                    this.head = 0;
                    this.items = newItems;
                }
                int tail = (this.head + this.totalCount)%this.items.Length;
                this.items[tail] = item;
                this.totalCount++;
            }
        }

        enum QueueState
        {
            Open,
            Shutdown,
            Closed
        }

        class WaitQueueReader : IQueueReader
        {
            readonly InputQueue<T> inputQueue;

            readonly ManualResetEvent waitEvent;
            Exception exception;
            T item;

            public WaitQueueReader(InputQueue<T> inputQueue)
            {
                this.inputQueue = inputQueue;
                this.waitEvent = new ManualResetEvent(false);
            }

            public void Set(Item item)
            {
                lock (this)
                {
                    this.exception = item.Exception;
                    this.item = item.Value;
                    this.waitEvent.Set();
                }
            }

            public bool Wait(TimeSpan timeout, out T value)
            {
                bool isSafeToClose = false;
                try
                {
                    if (!TimeoutHelper.WaitOne(this.waitEvent, timeout))
                    {
                        if (this.inputQueue.RemoveReader(this))
                        {
                            value = default(T);
                            isSafeToClose = true;
                            return false;
                        }
                        else
                        {
                            this.waitEvent.WaitOne();
                        }
                    }

                    isSafeToClose = true;
                }
                finally
                {
                    if (isSafeToClose)
                    {
                        this.waitEvent.Close();
                    }
                }

                if (this.exception != null)
                {
                    throw this.exception;
                }

                value = this.item;
                return true;
            }
        }

        class WaitQueueWaiter : IQueueWaiter
        {
            readonly ManualResetEvent waitEvent;
            bool itemAvailable;

            public WaitQueueWaiter()
            {
                this.waitEvent = new ManualResetEvent(false);
            }

            public void Set(bool itemAvailable)
            {
                lock (this)
                {
                    this.itemAvailable = itemAvailable;
                    this.waitEvent.Set();
                }
            }

            public bool Wait(TimeSpan timeout)
            {
                if (!TimeoutHelper.WaitOne(this.waitEvent, timeout))
                {
                    return false;
                }

                return this.itemAvailable;
            }
        }
    }
}