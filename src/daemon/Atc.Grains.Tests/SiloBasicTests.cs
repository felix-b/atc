using Atc.Grains.Tests.Samples;
using FluentAssertions;
using FluentAssertions.Formatting;
using NUnit.Framework;

namespace Atc.Grains.Tests;

public class SiloBasicTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void CanCreateGrain()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure); 

        var grainRef = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));

        grainRef.GrainId.Should().Be("SampleGrainOne/#1");
        grainRef.CanGet.Should().Be(true);
    }

    [Test]
    public void CanGetCreatedGrainObject()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        var grainRef = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        var grain = grainRef.Get();

        grain.Should().NotBeNull();
        grain.GrainId.Should().Be("SampleGrainOne/#1");
        grain.GrainType.Should().Be("SampleGrainOne");
        grain.Num.Should().Be(123);
        grain.Str.Should().Be("ABC");
    }

    [Test]
    public void CanMakeUniqueGrainIds()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        var one1 = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        var two1 = silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));
        var one2 = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));
        var two2 = silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 678.90m));

        one1.GrainId.Should().Be("SampleGrainOne/#1");
        one2.GrainId.Should().Be("SampleGrainOne/#2");
        two1.GrainId.Should().Be("SampleGrainTwo/#1");
        two2.GrainId.Should().Be("SampleGrainTwo/#2");
    }
    
    [Test]
    public void CanGetGrainObjectsOfMultipleTypes()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        var oneRef1 = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        var twoRef1 = silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));
        var oneRef2 = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));
        var twoRef2 = silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 678.90m));

        var one1 = oneRef1.Get();
        var one2 = oneRef2.Get();
        var two1 = twoRef1.Get();
        var two2 = twoRef2.Get();

        one1.Num.Should().Be(123);
        one1.Str.Should().Be("ABC");
        one2.Num.Should().Be(456);
        one2.Str.Should().Be("DEF");
        two1.Value.Should().Be(123.45m);
        two2.Value.Should().Be(678.90m);
    }

    [Test]
    public void CanDispatchGrainActivationEvents()
    {
        var eventWriter = new SiloTestDoubles.TestEventStreamWriter();
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure, eventWriter: eventWriter);
        var initialEventCount = eventWriter.Events.Count;        
        
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 678.90m));

        eventWriter.Events.Count.Should().Be(initialEventCount + 4);

        eventWriter.Events[initialEventCount + 0].Event.Should()
            .BeOfType<SampleGrainOne.GrainActivationEvent>()
            .Which.GrainId.Should().Be("SampleGrainOne/#1");
        eventWriter.Events[initialEventCount + 1].Event.Should()
            .BeOfType<SampleGrainTwo.GrainActivationEvent>()
            .Which.GrainId.Should().Be("SampleGrainTwo/#1");
        eventWriter.Events[initialEventCount + 2].Event.Should()
            .BeOfType<SampleGrainOne.GrainActivationEvent>()
            .Which.GrainId.Should().Be("SampleGrainOne/#2");
        eventWriter.Events[initialEventCount + 3].Event.Should()
            .BeOfType<SampleGrainTwo.GrainActivationEvent>()
            .Which.GrainId.Should().Be("SampleGrainTwo/#2");
    }

    [Test]
    public void CanDispatchGrainEvent()
    {
        var eventWriter = new SiloTestDoubles.TestEventStreamWriter();
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure, eventWriter: eventWriter);
        var initialEventCount = eventWriter.Events.Count;
        var one1Ref = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ORIGINAL"));
        one1Ref.Get().Str.Should().Be("ORIGINAL");
        eventWriter.Events.Count.Should().Be(initialEventCount + 1);

        one1Ref.Get().ChangeStr("CHANGED");

        one1Ref.Get().Str.Should().Be("CHANGED");
        eventWriter.Events.Count.Should().Be(initialEventCount + 2);
        eventWriter.Events[initialEventCount + 1].Event.Should()
            .BeOfType<SampleGrainOne.ChangeStrEvent>()
            .Which.NewStr.Should().Be("CHANGED");
    }
    
    [Test]
    public void CanGetGrainById()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));

        var one1Ref = silo.Grains.GetRefById<SampleGrainOne>("SampleGrainOne/#1");
        var one2Ref = silo.Grains.GetRefById<SampleGrainOne>("SampleGrainOne/#2");
        var two1Ref = silo.Grains.GetRefById<SampleGrainTwo>("SampleGrainTwo/#1");

        one1Ref.Get().GrainId.Should().Be("SampleGrainOne/#1");
        one2Ref.Get().GrainId.Should().Be("SampleGrainOne/#2");
        two1Ref.Get().GrainId.Should().Be("SampleGrainTwo/#1");
    }

    [Test]
    public void GetGrainById_MultipleTimes_ReturnSameInstance()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));

        var one2Ref1 = silo.Grains.GetRefById<SampleGrainOne>("SampleGrainOne/#2");
        var one2Ref2 = silo.Grains.GetRefById<SampleGrainOne>("SampleGrainOne/#2");

        var obj1 = one2Ref1.Get();
        var obj2 = one2Ref1.Get();
        var obj3 = one2Ref2.Get();

        obj1.Should().BeSameAs(obj2);
        obj1.Should().BeSameAs(obj3);
    }

    [Test]
    public void CanGetGrainObjectById()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));

        var one1 = silo.Grains.GetInstanceById<SampleGrainOne>("SampleGrainOne/#1");
        var one2 = silo.Grains.GetInstanceById<SampleGrainOne>("SampleGrainOne/#2");
        var two1 = silo.Grains.GetInstanceById<SampleGrainTwo>("SampleGrainTwo/#1");

        one1.GrainId.Should().Be("SampleGrainOne/#1");
        one2.GrainId.Should().Be("SampleGrainOne/#2");
        two1.GrainId.Should().Be("SampleGrainTwo/#1");
    }

    [Test]
    public void GetGrainByIdOrThrow_NotFound_Throw()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));

        Assert.Throws<GrainNotFoundException>(() => {
            silo.Grains.GetInstanceById<SampleGrainOne>("SampleGrainOne/#ZZZ");
        });
    }

    [Test]
    public void CanDeleteGrain()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ABC"));
        var two1Ref = silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 123.45m));
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 456,
            Str: "DEF"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 678.90m));

        silo.Grains.TryGetRefById<SampleGrainTwo>("SampleGrainTwo/#1", out _).Should().BeTrue();
        GetGrainIdsOfType<SampleGrainOne>().Should().BeEquivalentTo(new[] {
            "SampleGrainOne/#1",
            "SampleGrainOne/#2",
        });
        GetGrainIdsOfType<SampleGrainTwo>().Should().BeEquivalentTo(new[] {
            "SampleGrainTwo/#1",
            "SampleGrainTwo/#2",
        });

        silo.Grains.DeleteGrain(two1Ref);

        silo.Grains.TryGetRefById<SampleGrainTwo>("SampleGrainTwo/#1", out _).Should().BeFalse();
        GetGrainIdsOfType<SampleGrainOne>().Should().BeEquivalentTo(new[] {
            "SampleGrainOne/#1",
            "SampleGrainOne/#2",
        });
        GetGrainIdsOfType<SampleGrainTwo>().Should().BeEquivalentTo(new[] {
            "SampleGrainTwo/#2",
        });
        
        string[] GetGrainIdsOfType<T>() where T : class, IGrain
        {
            return silo.Grains.GetAllGrainsOfType<T>()
                .Select(g => g.GrainId)
                .ToArray();
        }
    }

    [Test]
    public void CannotDispatchGrainEventOnDifferentThread()
    {
        var eventWriter = new SiloTestDoubles.TestEventStreamWriter();
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure, eventWriter: eventWriter);
        var one1Ref = silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 123,
            Str: "ORIGINAL"));

        Exception? exception = null;
        
        var thread = new Thread(unused => {
            try
            {
                one1Ref.Get().ChangeStr("CHANGED");
            }
            catch (Exception e)
            {
                exception = e;
            }
        });
        thread.Start();
        thread.Join();

        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public void CannotExecuteReadyWorkItemsOnDifferentThread()
    {
        var eventWriter = new SiloTestDoubles.TestEventStreamWriter();
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure, eventWriter: eventWriter);

        Exception? exception = null;
        
        var thread = new Thread(unused => {
            try
            {
                silo.ExecuteReadyWorkItems();
            }
            catch (Exception e)
            {
                exception = e;
            }
        });
        thread.Start();
        thread.Join();

        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
    }
}
