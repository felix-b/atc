using System;
using Atc.World.AI;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.World.Tests.AI
{
    [TestFixture]
    public class ImmutableStateMachineTests
    {
        [Test]
        public void CanBuildStateMachine()
        {
            Action endAction = () => { };

            var builder = new ImmutableStateMachine.Builder();
            builder.AddState("BEGIN", s => s
                .OnTrigger("A", transitionTo: "MIDDLE")
            );
            builder.AddState("MIDDLE", s => s
                .OnTrigger("B", transitionTo: "END")
                .OnTrigger("C", transitionTo: "BEGIN")
            );
            builder.AddState("MIDDLE", s => s
                .OnEnter(seq => seq.AddStep(endAction))
            );
            
            var stateMachine = builder.Build();

            stateMachine.Should().NotBeNull();
            stateMachine.State.Should().NotBeNull();
            stateMachine.State.Name.Should().Be("BEGIN");
        }
    }
}
