using Atc.Grains.Tests.Samples;
using FluentAssertions;
using NUnit.Framework;
using static Atc.Grains.Tests.Samples.SampleSilo;

namespace Atc.Grains.Tests;

[TestFixture]
public class SiloTimeTravelTests
{
    [Test]
    public void CanTakeSnapshot()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);
        
        PopulateGrains(silo);
        RunGrainEvents(silo, startStep: 0, count: 4);

        var snapshot = silo.TimeTravel.TakeSnapshot();

        snapshot.Should().NotBeNull();
        snapshot.NextDispatchSequenceNo.Should().BeGreaterThan(0);
        snapshot.NextDispatchSequenceNo.Should().Be(silo.Dispatch.NextSequenceNo);
        snapshot.OpaqueData.Should().NotBeNull();
    }

    [Test]
    public void CanRevertToEmptySnapshot()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);
        var snapshot = silo.TimeTravel.TakeSnapshot();

        PopulateGrains(silo);
        RunGrainEvents(silo, startStep: 0, count: 4);

        silo.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(2);
        silo.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(2);

        silo.TimeTravel.RestoreSnapshot(snapshot);

        silo.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(0);
        silo.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(0);
    }

    [Test]
    public void CanRestoreSnapshotIntoNewSilo()
    {
        var silo1 = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);
        PopulateGrains(silo1);
        RunGrainEvents(silo1, startStep: 0, count: 8);
        var snapshot = silo1.TimeTravel.TakeSnapshot();
        var silo2 = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);
        silo2.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(0);
        silo2.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(0);

        silo2.TimeTravel.RestoreSnapshot(snapshot);

        silo2.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(2);
        silo2.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(2);

        var one1Ref = silo2.Grains.GetRefById<SampleGrainOne>(GrainIds.One1); 
        var one2Ref = silo2.Grains.GetRefById<SampleGrainOne>(GrainIds.One2); 
        var two1Ref = silo2.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two1); 
        var two2Ref = silo2.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two2);

        one1Ref.Get().Str.Should().Be("one1-v2");
        one2Ref.Get().Str.Should().Be("one2-v2");
        two1Ref.Get().Value.Should().Be(5001.20m);
        two2Ref.Get().Value.Should().Be(5002.20m);
    }

    [Test]
    public void CanRemoveExtraGrainsOnRestore()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);

        PopulateGrains(silo);
        var snapshot = silo.TimeTravel.TakeSnapshot();
        
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 999,
            Str: "ZZZ"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 999.99m));

        silo.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(3);
        silo.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(3);
        GetGrainIdsOfType<SampleGrainOne>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.One1, GrainIds.One2, GrainIds.One3
        });
        GetGrainIdsOfType<SampleGrainTwo>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.Two1, GrainIds.Two2, GrainIds.Two3
        });
        
        silo.TimeTravel.RestoreSnapshot(snapshot);
        
        silo.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(2);
        silo.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(2);
        GetGrainIdsOfType<SampleGrainOne>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.One1, GrainIds.One2
        });
        GetGrainIdsOfType<SampleGrainTwo>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.Two1, GrainIds.Two2
        });
    }

    [Test]
    public void CanRecreateDeletedGrainsOnRestore()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);

        PopulateGrains(silo);
        RunGrainEvents(silo, startStep:0, count: 8);

        var snapshot = silo.TimeTravel.TakeSnapshot();

        silo.Grains.DeleteGrain(silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1));
        silo.Grains.DeleteGrain(silo.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two1));

        silo.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(1);
        silo.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(1);
        GetGrainIdsOfType<SampleGrainOne>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.One2
        });
        GetGrainIdsOfType<SampleGrainTwo>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.Two2
        });
        
        silo.TimeTravel.RestoreSnapshot(snapshot);
        
        silo.Grains.GetAllGrainsOfType<SampleGrainOne>().Count().Should().Be(2);
        silo.Grains.GetAllGrainsOfType<SampleGrainTwo>().Count().Should().Be(2);
        GetGrainIdsOfType<SampleGrainOne>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.One1, GrainIds.One2
        });
        GetGrainIdsOfType<SampleGrainTwo>(silo).Should().BeEquivalentTo(new[] {
            GrainIds.Two1, GrainIds.Two2
        });

        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One1).Str.Should().Be("one1-v2");
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two1).Value.Should().Be(5001.20m);
    }

    [Test]
    public void CanResetGrainStateToSnapshotOnRestore()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);

        PopulateGrains(silo);
        RunGrainEvents(silo, startStep:0, count: 4);

        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One1).Str.Should().Be("one1-v1");
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two1).Value.Should().Be(5001.10m);
        var snapshot1 = silo.TimeTravel.TakeSnapshot();

        RunGrainEvents(silo, startStep:4, count: 4);

        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One1).Str.Should().Be("one1-v2");
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two1).Value.Should().Be(5001.20m);
        var snapshot2 = silo.TimeTravel.TakeSnapshot();

        silo.TimeTravel.RestoreSnapshot(snapshot1);

        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One1).Str.Should().Be("one1-v1");
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two1).Value.Should().Be(5001.10m);

        silo.TimeTravel.RestoreSnapshot(snapshot2);

        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One1).Str.Should().Be("one1-v2");
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two1).Value.Should().Be(5001.20m);
    }

    [Test]
    public void CanReplayEvents()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);
        PopulateGrains(silo);
        RunGrainEvents(silo, startStep:0, count: 8);
        var sequenceNo = silo.Dispatch.NextSequenceNo;
        var envelopesToReplay = new GrainEventEnvelope[] {
            new (silo.SiloId, GrainIds.One1, sequenceNo + 0, DateTime.Now, new SampleGrainOne.ChangeStrEvent("ONE1-R1")),
            new (silo.SiloId, GrainIds.One2, sequenceNo + 1, DateTime.Now, new SampleGrainOne.ChangeStrEvent("ONE2-R1")),
            new (silo.SiloId, GrainIds.One1, sequenceNo + 2, DateTime.Now, new SampleGrainOne.ChangeStrEvent("ONE1-R2")),
            new (silo.SiloId, GrainIds.One2, sequenceNo + 3, DateTime.Now, new SampleGrainOne.ChangeStrEvent("ONE2-R2")),
            new (silo.SiloId, GrainIds.Two1, sequenceNo + 4, DateTime.Now, new SampleGrainTwo.ChangeValueEvent(10000.10m)),
            new (silo.SiloId, GrainIds.Two2, sequenceNo + 5, DateTime.Now, new SampleGrainTwo.ChangeValueEvent(20000.20m)),
            new (silo.SiloId, GrainIds.Two1, sequenceNo + 6, DateTime.Now, new SampleGrainTwo.ChangeValueEvent(11111.11m)),
            new (silo.SiloId, GrainIds.Two2, sequenceNo + 7, DateTime.Now, new SampleGrainTwo.ChangeValueEvent(22222.22m)),
        };
        
        silo.TimeTravel.ReplayEvents(envelopesToReplay);
        
        silo.Dispatch.NextSequenceNo.Should().Be(sequenceNo + 8);
        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One1).Str.Should().Be("ONE1-R2");
        silo.Grains.GetInstanceById<SampleGrainOne>(GrainIds.One2).Str.Should().Be("ONE2-R2");
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two1).Value.Should().Be(11111.11m);
        silo.Grains.GetInstanceById<SampleGrainTwo>(GrainIds.Two2).Value.Should().Be(22222.22m);
    }

    [Test]
    public void GrainRefSurviveRestoreOfSnapshots()
    {
        var silo = SiloTestDoubles.CreateSilo("time-travel", SampleSilo.Configure);
        PopulateGrains(silo);
        var one1Ref = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);
        
        RunGrainEvents(silo, startStep:0, count: 4);

        one1Ref.Get().Str.Should().Be("one1-v1");
        
        var snapshot1 = silo.TimeTravel.TakeSnapshot();
        RunGrainEvents(silo, startStep:4, count: 4);

        one1Ref.Get().Str.Should().Be("one1-v2");

        var snapshot2 = silo.TimeTravel.TakeSnapshot();
        silo.TimeTravel.RestoreSnapshot(snapshot1);
        
        one1Ref.Get().Str.Should().Be("one1-v1");
        
        silo.TimeTravel.RestoreSnapshot(snapshot2);

        one1Ref.Get().Str.Should().Be("one1-v2");
    }

    private void PopulateGrains(ISilo silo)
    {
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
    }
    
    void RunGrainEvents(ISilo silo, int startStep, int count)
    {
        var one1Ref = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1); 
        var one2Ref = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One2); 
        var two1Ref = silo.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two1); 
        var two2Ref = silo.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two2);

        var steps = new Action[] {
            () => one1Ref.Get().ChangeStr("one1-v1"),
            () => one2Ref.Get().ChangeStr("one2-v1"),
            () => two1Ref.Get().ChangeValue(5001.10m),
            () => two2Ref.Get().ChangeValue(5002.10m),
            () => one1Ref.Get().ChangeStr("one1-v2"),
            () => one2Ref.Get().ChangeStr("one2-v2"),
            () => two1Ref.Get().ChangeValue(5001.20m),
            () => two2Ref.Get().ChangeValue(5002.20m),
        };

        for (int i = startStep; i < startStep + count && i < steps.Length; i++)
        {
            steps[i]();
        }
    }

    private string[] GetGrainIdsOfType<T>(ISilo silo) where T : class, IGrain
    {
        return silo.Grains.GetAllGrainsOfType<T>()
            .Select(g => g.GrainId)
            .ToArray();
    }
}