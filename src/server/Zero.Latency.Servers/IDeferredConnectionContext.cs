﻿using System;
using System.Collections;

namespace Zero.Latency.Servers
{
    public interface IDeferredConnectionContext<TEnvelopeOut> : IReadOnlyConnectionContext
        where TEnvelopeOut : class
    {
        void FireMessage(TEnvelopeOut outgoingMessageEnvelope);
        void RegisterObserver(IObserverSubscription observer, string? registrationKey = null);
        void DisposeObserver(string registrationKey);
        void RequestClose();
        void RequestFlush();
        SessionItems Session { get; }
    }
}
