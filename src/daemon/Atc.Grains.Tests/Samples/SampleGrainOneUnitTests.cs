using FluentAssertions;
using NUnit.Framework;

namespace Atc.Grains.Tests.Samples;

[TestFixture]
public class SampleGrainOneUnitTests
{
    [Test]
    public void CanInitializeState()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);

        var grain = new SampleGrainOne("id1", silo.Dispatch, new SampleGrainOne.GrainState(123, "ABC"));

        var state = SiloTestDoubles.GetGrainState(grain);
        state.Num.Should().Be(123);
        state.Str.Should().Be("ABC");
    }

    [Test]
    public void CanCreateByActivation()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);
        
        var grain = silo.Grains.CreateGrain<SampleGrainOne>(grainId => 
            new SampleGrainOne.GrainActivationEvent(
                grainId, 
                Num: 456, 
                Str: "DEF"));
        
        grain.Get().Num.Should().Be(456);
        grain.Get().Str.Should().Be("DEF");
    }

    [Test]
    public void CanReduceChangeStrEvent()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);
        var grain = new SampleGrainOne("id1", silo.Dispatch, new SampleGrainOne.GrainState(123, "ABC"));

        var stateAfterChangeStr = SiloTestDoubles.InvokeGrainReduce(
            grain,
            new SampleGrainOne.GrainState(100, "UUU"),
            new SampleGrainOne.ChangeStrEvent("ZZZ"));
        
        stateAfterChangeStr.Num.Should().Be(100);
        stateAfterChangeStr.Str.Should().Be("ZZZ");
    }

    [Test]
    public void CanReduceChangeNumEvent()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);
        var grain = new SampleGrainOne("id1", silo.Dispatch, new SampleGrainOne.GrainState(123, "ABC"));

        var stateAfterChangeNum = SiloTestDoubles.InvokeGrainReduce(
            grain,
            new SampleGrainOne.GrainState(100, "UUU"),
            new SampleGrainOne.ChangeNumEvent(999));
        
        stateAfterChangeNum.Num.Should().Be(999);
        stateAfterChangeNum.Str.Should().Be("UUU");
    }

    [Test]
    public void CanChangeStr()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);
        var grain = new SampleGrainOne("id1", silo.Dispatch, new SampleGrainOne.GrainState(123, "ABC"));

        grain.ChangeStr("ZZZ");

        var newState = SiloTestDoubles.GetGrainState(grain);
        newState.Num.Should().Be(123);
        newState.Str.Should().Be("ZZZ");
    }

    [Test] 
    public void CanRequestMultiplyNum()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);
        var grain = new SampleGrainOne("id1", silo.Dispatch, new SampleGrainOne.GrainState(123, "ABC"));

        grain.RequestMultiplyNum(3);

        var workItems = SiloTestDoubles.GetWorkItemsInTaskQueue(silo).ToArray();
        workItems.Single().Should().BeOfType<SampleGrainOne.MultiplyNumWorkItem>().Which.Times.Should().Be(3);
    }

    [Test] 
    public void CanExecuteMultiplyNumWorkItem()
    {
        var silo = SiloTestDoubles.CreateSilo("test1", SampleSilo.Configure);
        var grain = new SampleGrainOne("id1", silo.Dispatch, new SampleGrainOne.GrainState(123, "ABC"));

        var result = SiloTestDoubles.InvokeGrainWorkItem(
            grain,
            stateBefore: new SampleGrainOne.GrainState(11, "ZZ"),
            workItem: new SampleGrainOne.MultiplyNumWorkItem(Times: 3),
            timedOut: false,
            out var stateAfter);

        result.Should().BeTrue();
        stateAfter.Num.Should().Be(33);
        stateAfter.Str.Should().Be("ZZ");
    }
}
