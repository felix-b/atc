using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors.Tests.Impl
{
    [TestFixture]
    public class SupervisorActorTests
    {
        [Test]
        public void CanCreateActor()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var dependencies = SimpleDependencyContext.NewEmpty();

            var supervisor = new SupervisorActor(store, dependencies);
            supervisor.RegisterActorType<ParentActor, ParentActor.ActivationEvent>(
                ParentActor.TypeString, 
                (e, ctx) => new ParentActor(e));

            var actor = supervisor.CreateActor(id => new ParentActor.ActivationEvent(id, "ABC"));

            actor.UniqueId.Should().Be("test/parent/#1");
            actor.Get().Should().NotBeNull();
            actor.Get().Str.Should().Be("ABC");
        }

        [Test]
        public void CanCreateMultipleActors()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var dependencies = SimpleDependencyContext.NewEmpty();

            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var parent1 = ParentActor.Create(supervisor, "ABC").Get();
            var child1 = ChildActor.Create(supervisor, 123).Get();
            var parent2 = ParentActor.Create(supervisor, "DEF").Get();
            var child2 = ChildActor.Create(supervisor, 456).Get();

            parent1.UniqueId.Should().Be("test/parent/#1");
            parent1.Str.Should().Be("ABC");

            parent2.UniqueId.Should().Be("test/parent/#2");
            parent2.Str.Should().Be("DEF");

            child1.UniqueId.Should().Be("test/child/#1");
            child1.Num.Should().Be(123);

            child2.UniqueId.Should().Be("test/child/#2");
            child2.Num.Should().Be(456);
        }

        [Test]
        public void CanLookupActorByUniqueId()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var dependencies = SimpleDependencyContext.NewEmpty();

            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var parent1 = ParentActor.Create(supervisor, "ABC").Get();
            var child1 = ChildActor.Create(supervisor, 123).Get();
            var parent2 = ParentActor.Create(supervisor, "DEF").Get();
            var child2 = ChildActor.Create(supervisor, 456).Get();

            (supervisor as ISupervisorActor).GetActorByIdOrThrow<ParentActor>("test/parent/#1").Get().Should().BeSameAs(parent1);
            (supervisor as ISupervisorActor).GetActorByIdOrThrow<ParentActor>("test/parent/#2").Get().Should().BeSameAs(parent2);
            (supervisor as ISupervisorActor).GetActorByIdOrThrow<ChildActor>("test/child/#1").Get().Should().BeSameAs(child1);
            (supervisor as ISupervisorActor).GetActorByIdOrThrow<ChildActor>("test/child/#2").Get().Should().BeSameAs(child2);
        }

        [Test]
        public void CanGetAllActorsOfType()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var dependencies = SimpleDependencyContext.NewEmpty();

            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var parent1 = ParentActor.Create(supervisor, "ABC").Get();
            var child1 = ChildActor.Create(supervisor, 123).Get();
            var parent2 = ParentActor.Create(supervisor, "DEF").Get();
            var child2 = ChildActor.Create(supervisor, 456).Get();

            supervisor.GetAllActorsOfType<ChildActor>()
                .Select(r => r.Get())
                .Should().BeEquivalentTo(new[] {child1, child2});
        }
    }
}