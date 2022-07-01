using Atc.Grains.Impl;
using Atc.Grains.Tests.Samples;
using FluentAssertions;
using NUnit.Framework;
using static Atc.Grains.Tests.Samples.SampleSilo;

namespace Atc.Grains.Tests;

[TestFixture]
public class SiloTaskQueueTests
{
    [Test]
    public void CanCompareWorkItemsByTiming()
    {
        var dummyRef = new GrainRef<IGrain>();
        var dummyWorkItem = new DummyWorkItem();
        var utcNow = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);

        var itemWithNoConstraints = new TaskQueueGrain.WorkItemEntry(
            1, dummyRef, dummyWorkItem, NotEarlierThanUtc: null, NotLaterThanUtc: null, HasPredicate: false);
        var anotherItemWithNoConstraints = new TaskQueueGrain.WorkItemEntry(
            10, dummyRef, dummyWorkItem, NotEarlierThanUtc: null, NotLaterThanUtc: null, HasPredicate: false);

        var itemWithNotEarlierThan1Hour = new TaskQueueGrain.WorkItemEntry(
            1, dummyRef, dummyWorkItem, NotEarlierThanUtc: utcNow.AddHours(1), NotLaterThanUtc: null, HasPredicate: false);

        var itemWithNotEarlierThan2Hours = new TaskQueueGrain.WorkItemEntry(
            1, dummyRef, dummyWorkItem, NotEarlierThanUtc: utcNow.AddHours(2), NotLaterThanUtc: null, HasPredicate: false);
        var anotherItemWithNotEarlierThan2Hours = new TaskQueueGrain.WorkItemEntry(
            10, dummyRef, dummyWorkItem, NotEarlierThanUtc: utcNow.AddHours(2), NotLaterThanUtc: null, HasPredicate: false);

        var comparer = new TaskQueueGrain.WorkItemEntryComparer();

        // an item always equals to itself
        comparer.Compare(itemWithNoConstraints, itemWithNoConstraints).Should().Be(0);
        comparer.Compare(itemWithNotEarlierThan2Hours, itemWithNotEarlierThan2Hours).Should().Be(0);

        // equal timing, compare by IDs
        comparer.Compare(itemWithNoConstraints, anotherItemWithNoConstraints).Should().BeLessThan(0);
        comparer.Compare(anotherItemWithNoConstraints, itemWithNoConstraints).Should().BeGreaterThan(0);
        
        // item with no constraints is less that item with not-earlier-than constraint
        comparer.Compare(anotherItemWithNoConstraints, itemWithNotEarlierThan1Hour).Should().BeLessThan(0);
        comparer.Compare(itemWithNotEarlierThan1Hour, anotherItemWithNoConstraints).Should().BeGreaterThan(0);

        // two items with not-earlier-than constraint are compared by the constraint
        comparer.Compare(itemWithNotEarlierThan1Hour, itemWithNotEarlierThan2Hours).Should().BeLessThan(0);
        comparer.Compare(itemWithNotEarlierThan2Hours, itemWithNotEarlierThan1Hour).Should().BeGreaterThan(0);

        // equal timing, compare by IDs
        comparer.Compare(itemWithNotEarlierThan2Hours, anotherItemWithNotEarlierThan2Hours).Should().BeLessThan(0);
        comparer.Compare(anotherItemWithNotEarlierThan2Hours, itemWithNotEarlierThan2Hours).Should().BeGreaterThan(0);
    }
    
    [Test]
    public void CanExecuteWorkItem()
    {
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure);
        PopulateGrains(silo);

        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);
        grainRef.Get().Num.Should().Be(111);
        
        grainRef.Get().RequestMultiplyNum(3);

        grainRef.Get().Num.Should().Be(111);

        silo.ExecuteReadyWorkItems();
        
        grainRef.Get().Num.Should().Be(333);
    }

    [Test]
    public void WorkItemIsExecutedOnlyOnce()
    {
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure);
        PopulateGrains(silo);

        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);
        grainRef.Get().RequestMultiplyNum(3);

        grainRef.Get().Num.Should().Be(111);
        
        silo.ExecuteReadyWorkItems();
        
        grainRef.Get().Num.Should().Be(333);

        silo.ExecuteReadyWorkItems();

        grainRef.Get().Num.Should().Be(333);
    }

    [Test]
    public void CanExecuteAllReadyWorkItems()
    {
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure);
        PopulateGrains(silo);

        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        grainRef.Get().RequestMultiplyNum(3);
        grainRef.Get().RequestMultiplyNum(2);

        grainRef.Get().Num.Should().Be(111);
        
        silo.ExecuteReadyWorkItems();
        
        grainRef.Get().Num.Should().Be(666);

        silo.ExecuteReadyWorkItems();
        
        grainRef.Get().Num.Should().Be(666);
    }

    [Test]
    public void CanExecuteWorkItemsForMultipleGrains()
    {
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure);
        PopulateGrains(silo);

        var grainRef1 = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);
        var grainRef2 = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One2);

        grainRef1.Get().RequestMultiplyNum(3);
        grainRef2.Get().RequestMultiplyNum(4);

        grainRef1.Get().Num.Should().Be(111);
        grainRef2.Get().Num.Should().Be(222);
        
        silo.ExecuteReadyWorkItems();
        
        grainRef1.Get().Num.Should().Be(333);
        grainRef2.Get().Num.Should().Be(888);

        silo.ExecuteReadyWorkItems();
        
        grainRef1.Get().Num.Should().Be(333);
        grainRef2.Get().Num.Should().Be(888);
    }

    [Test]
    public void CanExecuteWorkItemNotEarlierThan()
    {
        var environment = new SiloTestDoubles.TestEnvironment();
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);

        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        grainRef.Get().DeferredDuplicateStr(new DateTime(2022, 10, 10, 8, 30, 00, DateTimeKind.Utc));

        grainRef.Get().Str.Should().Be("ABC");
        
        environment.UtcNow = new DateTime(2022, 10, 10, 8, 29, 59, DateTimeKind.Utc);
        silo.ExecuteReadyWorkItems();
        
        grainRef.Get().Str.Should().Be("ABC");
        
        environment.UtcNow = new DateTime(2022, 10, 10, 8, 30, 00, DateTimeKind.Utc);
        silo.ExecuteReadyWorkItems();
        
        grainRef.Get().Str.Should().Be("ABC|ABC");
    }

    [Test]
    public void CanExecuteWorkItemWhenPredicateIsTrue()
    {
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure);
        PopulateGrains(silo);

        var grainRef = silo.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two1);

        grainRef.Get().ChangeValue(99.0m);
        grainRef.Get().ArmDivideBy10WhenGreaterThan100();

        silo.ExecuteReadyWorkItems();

        grainRef.Get().Value.Should().Be(99.0m);
        grainRef.Get().ChangeValue(120.0m);

        silo.ExecuteReadyWorkItems();

        grainRef.Get().Value.Should().Be(12.0m);
    }

    [Test]
    public void PredicateFalse_NotLaterThanReached_ExecuteWithTimeoutFlag()
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var deadlineUtc = startUtc.AddMinutes(10);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };

        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);

        var grainRef = silo.Grains.GetRefById<SampleGrainTwo>(GrainIds.Two1);

        grainRef.Get().ChangeValue(99.0m);
        grainRef.Get().ArmDivideBy10WhenGreaterThan100(notLaterThanUtc: deadlineUtc);

        silo.ExecuteReadyWorkItems();

        grainRef.Get().Value.Should().Be(99.0m);

        environment.UtcNow = deadlineUtc.AddMilliseconds(1);
        silo.ExecuteReadyWorkItems();

        grainRef.Get().Value.Should().Be(-1.0m); // the grain sets -1 in case of work item timeout
    }

    [Test]
    public void NextWorkItemUtc_ItemHasNoConstraints_ReturnUtcNow() 
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);
        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(1));
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem());
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(2));
        
        silo.NextWorkItemAtUtc.Should().Be(environment.UtcNow);
    }

    [Test]
    public void NextWorkItemUtc_ItemHasNotEarlierThan_ReturnNotEarlierThan() 
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);
        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(1));
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddMinutes(30));
        
        silo.NextWorkItemAtUtc.Should().Be(startUtc.AddMinutes(30));
    }

    [Test]
    public void NextWorkItemUtc_ItemHasNotLaterThan_ReturnUtcNow() 
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);
        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(1));
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notLaterThanUtc: startUtc.AddMinutes(30));
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notLaterThanUtc: startUtc.AddHours(2));
        
        silo.NextWorkItemAtUtc.Should().Be(startUtc);
    }

    [Test]
    public void NextWorkItemUtc_ItemHasNotEarlierThanAndPredicate_ReturnNotEarlierThan() 
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);
        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(1), withPredicate: false);
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddMinutes(30), withPredicate: true);
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(2), withPredicate: false);
        
        silo.NextWorkItemAtUtc.Should().Be(startUtc.AddMinutes(30));
    }

    [Test]
    public void NextWorkItemUtc_ItemHasNotLaterThanAndPredicate_ReturnUtcNow() 
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);
        var grainRef = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);

        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notEarlierThanUtc: startUtc.AddHours(1));
        silo.TaskQueue.Defer(grainRef.Get(), new DummyWorkItem(), notLaterThanUtc: startUtc.AddHours(2), withPredicate: true);
        
        silo.NextWorkItemAtUtc.Should().Be(startUtc);
    }

    [Test]
    public void CanCancelWorkItem() 
    {
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("test-task-queue", SampleSilo.Configure, environment: environment);
        PopulateGrains(silo);

        var grainRef1 = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One1);
        var grainRef2 = silo.Grains.GetRefById<SampleGrainOne>(GrainIds.One2);

        var handle1 = grainRef1.Get().DeferredDuplicateStr(startUtc.AddHours(1));
        var handle2 = grainRef1.Get().RequestMultiplyNum(2);
        var handle3 = grainRef2.Get().DeferredDuplicateStr(startUtc.AddHours(1));
        var handle4 = grainRef2.Get().RequestMultiplyNum(3);

        environment.UtcNow = startUtc.AddHours(2);

        silo.TaskQueue.CancelWorkItem(handle2);
        silo.TaskQueue.CancelWorkItem(handle3);

        silo.ExecuteReadyWorkItems();

        grainRef1.Get().Str.Should().Be("ABC|ABC"); // duplicated - work item executed
        grainRef1.Get().Num.Should().Be(111);       // unchanged - work item cancelled
        grainRef2.Get().Str.Should().Be("DEF");     // unchanged - work item cancelled
        grainRef2.Get().Num.Should().Be(666);       // multiplied - work item executed
    }
    
    private void PopulateGrains(ISilo silo)
    {
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 111,
            Str: "ABC"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 111.11m));
        silo.Grains.CreateGrain(grainId => new SampleGrainOne.GrainActivationEvent(
            grainId,
            Num: 222,
            Str: "DEF"));
        silo.Grains.CreateGrain(grainId => new SampleGrainTwo.GrainActivationEvent(
            grainId,
            Value: 222.22m));
    }

    public record DummyWorkItem : IGrainWorkItem;
}