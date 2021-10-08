﻿// https://medium.com/@cilliemalan/how-to-await-a-cancellation-token-in-c-cbfc88f28fa2
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Just.Utility
{
    public static class AsyncExtensions
    {
        public static Task SafeContinueWith(this Task? source, Func<Task> next)
        {
            return source?.ContinueWith(t => next()) ?? next();
        }

        public static Task SafeContinueWith(this Task? source, Action next)
        {
            if (source != null)
            {
                return source.ContinueWith(t => next());
            }

            next();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Allows a cancellation token to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct)
        {
            // return our special awaiter
            return new CancellationTokenAwaiter
            {
                CancellationToken = ct
            };
        }

        /// <summary>
        /// The awaiter for cancellation tokens.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
        {
            public CancellationTokenAwaiter(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
            }

            internal CancellationToken CancellationToken;

            public object GetResult()
            {
                // this is called by compiler generated methods when the
                // task has completed. Instead of returning a result, we 
                // just throw an exception.
                if (IsCompleted) throw new OperationCanceledException();
                else throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
            }

            // called by compiler generated/.net internals to check
            // if the task has completed.
            public bool IsCompleted => CancellationToken.IsCancellationRequested;

            // The compiler will generate stuff that hooks in
            // here. We hook those methods directly into the
            // cancellation token.
            public void OnCompleted(Action continuation) =>
                CancellationToken.Register(continuation);
            public void UnsafeOnCompleted(Action continuation) =>
                CancellationToken.Register(continuation);
        }
    }
}