using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors.Tests
{
    [TestFixture]
    public class TimeTravelTests
    {
        [Test]
        public void CanRewindToInitialStateAndBack()
        {
            var eventLog = new List<StateEventEnvelope>();
            var store = new StateStore(new NoopStateStoreLogger());
            store.AddEventListener((in StateEventEnvelope e) => eventLog.Add(e), out _);
            
            var dependencies = SimpleDependencyContext.NewEmpty();
            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var initialSnapshot = supervisor.TakeSnapshot();

            var parent1 = ParentActor.Create(supervisor, "ONE");
            var parent2 = ParentActor.Create(supervisor, "TWO");

            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            var eventCountBeforeRewind = eventLog.Count;
            eventCountBeforeRewind.Should().BeGreaterOrEqualTo(2);
            
            supervisor.RestoreSnapshot(initialSnapshot);

            Assert.Throws<ActorNotFoundException>(() => parent1.Get());
            Assert.Throws<ActorNotFoundException>(() => parent2.Get());

            var copyOfEventLog = eventLog.ToArray();
            eventLog.Clear();
            
            supervisor.ReplayEvents(copyOfEventLog);
            
            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            eventLog.Count.Should().Be(eventCountBeforeRewind);
        }
    }
}
