﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging.Tests
{
    [TestFixture]
    public class AsyncLocalAssumptionsTests
    {
        [Test]
        public void ReturnsSameContextInSynchronousCode()
        {
            var holder = new MyContextHolder();
            var context1 = holder.GetContext();
            var context2 = holder.GetContext();
            
            Assert.That(context1, Is.SameAs(context2));
        }

        [Test]
        public async Task ReturnsSameContextAfterAwait()
        {
            var holder = new MyContextHolder();
            var context1 = holder.GetContext();

            await Task.Delay(10);
            
            var context2 = holder.GetContext();
            Assert.That(context1, Is.SameAs(context2));
        }

        [Test]
        public async Task ReturnsDifferentContextsInParallelTasks()
        {
            var holder = new MyContextHolder();
            var contextsByTaskKey = new Dictionary<int, List<MyContext>>();
            var syncRoot = new object();
            var canEnterEvent = new TaskCompletionSource();
            var canExitEvent = new TaskCompletionSource();
            var taskReadyEvents = new Dictionary<int, TaskCompletionSource>() {
                { 111, new TaskCompletionSource() },
                { 222, new TaskCompletionSource() },
            };
            var taskDoneEvents = new Dictionary<int, TaskCompletionSource>() {
                { 111, new TaskCompletionSource() },
                { 222, new TaskCompletionSource() },
            };

            var task1 = RunTask(111);
            var task2 = RunTask(222);
            
            await Task.WhenAll(taskReadyEvents[111].Task, taskReadyEvents[222].Task);
            canEnterEvent.SetResult();
            await Task.WhenAll(taskDoneEvents[111].Task, taskDoneEvents[222].Task);
            canExitEvent.SetResult();

            await task1;
            await task2;

            Assert.That(contextsByTaskKey.Count, Is.EqualTo(2));
            Assert.That(contextsByTaskKey.ContainsKey(111));
            Assert.That(contextsByTaskKey.ContainsKey(222));
            Assert.That(contextsByTaskKey[111].Count, Is.EqualTo(2));
            Assert.That(contextsByTaskKey[111][0], Is.SameAs(contextsByTaskKey[111][1]));
            Assert.That(contextsByTaskKey[222].Count, Is.EqualTo(2));
            Assert.That(contextsByTaskKey[222][0], Is.SameAs(contextsByTaskKey[222][1]));
            Assert.That(contextsByTaskKey[111][0], Is.Not.SameAs(contextsByTaskKey[222][0]));
            
            async Task RunTask(int taskKey)
            {
                taskReadyEvents![taskKey].SetResult();
                await canEnterEvent!.Task;
                var context1 = holder!.GetContext();
                var context2 = holder.GetContext();
                RememberContextInCurrentThread(context1, taskKey);
                await Task.Delay(10);
                RememberContextInCurrentThread(context2, taskKey);
                taskDoneEvents![taskKey].SetResult();
                await canExitEvent!.Task;
            }
            
            void RememberContextInCurrentThread(MyContext context, int threadKey)
            {
                lock (syncRoot!)
                {
                    if (!contextsByTaskKey!.TryGetValue(threadKey, out var targetList))
                    {
                        targetList = new List<MyContext>();
                        contextsByTaskKey.Add(threadKey, targetList);
                    }
                    targetList.Add(context);
                }
            }
        }

        [Test]
        public async Task ReturnsDifferentContextsInDifferentThreads()
        {
            var holder = new MyContextHolder();
            var contextsByThreadKey = new Dictionary<int, List<MyContext>>();
            var syncRoot = new object();
            var canEnterEvent = new ManualResetEvent(false);
            var canExitEvent = new ManualResetEvent(false);
            var threadReadyEvents = new Dictionary<int, ManualResetEvent>() {
                { 111, new ManualResetEvent(false) },
                { 222, new ManualResetEvent(false) },
            };
            var threadDoneEvents = new Dictionary<int, ManualResetEvent>() {
                { 111, new ManualResetEvent(false) },
                { 222, new ManualResetEvent(false) },
            };
            var threads = new Thread[] {
                new Thread(RunThread),
                new Thread(RunThread)
            };
            threads[0].Start(111);
            threads[1].Start(222);

            WaitHandle.WaitAll(new WaitHandle[] { threadReadyEvents[111], threadReadyEvents[222] });
            canEnterEvent.Set();
            WaitHandle.WaitAll(new WaitHandle[] { threadDoneEvents[111], threadDoneEvents[222] });
            canExitEvent.Set();

            threads[0].Join();
            threads[1].Join();

            Assert.That(contextsByThreadKey.Count, Is.EqualTo(2));
            Assert.That(contextsByThreadKey.ContainsKey(111));
            Assert.That(contextsByThreadKey.ContainsKey(222));
            Assert.That(contextsByThreadKey[111].Count, Is.EqualTo(2));
            Assert.That(contextsByThreadKey[111][0], Is.SameAs(contextsByThreadKey[111][1]));
            Assert.That(contextsByThreadKey[222].Count, Is.EqualTo(2));
            Assert.That(contextsByThreadKey[222][0], Is.SameAs(contextsByThreadKey[222][1]));
            Assert.That(contextsByThreadKey[111][0], Is.Not.SameAs(contextsByThreadKey[222][0]));
            
            void RunThread(object state)
            {
                var threadKey = (int) state;
                threadReadyEvents![threadKey].Set();
                canEnterEvent!.WaitOne();
                var context1 = holder!.GetContext();
                var context2 = holder.GetContext();
                RememberContextInCurrentThread(context1, threadKey);
                RememberContextInCurrentThread(context2, threadKey);
                threadDoneEvents![threadKey].Set();
                canExitEvent!.WaitOne();
            }
            
            void RememberContextInCurrentThread(MyContext context, int threadKey)
            {
                lock (syncRoot!)
                {
                    if (!contextsByThreadKey!.TryGetValue(threadKey, out var targetList))
                    {
                        targetList = new List<MyContext>();
                        contextsByThreadKey.Add(threadKey, targetList);
                    }
                    targetList.Add(context);
                }
            }
        }

        private class MyContextHolder
        {
            private int _nextContextId = 1;
            private readonly AsyncLocal<MyContext?> _myAsyncLocal = new();
            
            public MyContext GetContext() 
            {
                var context = _myAsyncLocal.Value;
                if (context == null)
                {
                    context = new MyContext($"{Interlocked.Increment(ref _nextContextId)}");
                    _myAsyncLocal.Value = context;
                }
                return context;
            }
        }

        private class MyContext
        {
            public MyContext(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }
    }
}