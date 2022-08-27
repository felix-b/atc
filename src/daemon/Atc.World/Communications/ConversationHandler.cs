using System.Collections.Immutable;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public class ConversationHandler
{
    public static ConversationState CreateInitialState(string initialContextId) => 
        new ConversationState(
            ContextsInOrder: ImmutableSortedSet
                .Create<ContextEntry>(new ContextComparer())
                .Add(new ContextEntry(initialContextId, ConversationContextRelevance.Foreground))
        );
    
    public static ConversationHandlerBuilder<TInput, TOutput> CreateBuilder<TInput, TOutput>()
        where TInput : class
        where TOutput : class
    {
        return new ConversationHandlerBuilder<TInput, TOutput>();
    }
    
    public record ContextEntry(
        string Id,
        ConversationContextRelevance Relevance
    );
    
    public record ConversationState(
        ImmutableSortedSet<ContextEntry> ContextsInOrder
    );
    
    public class ContextComparer : IComparer<ContextEntry>
    {
        public int Compare(ContextEntry? x, ContextEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            var byRelevance = -((int)x.Relevance).CompareTo((int)y.Relevance);
            if (byRelevance != 0)
            {
                return byRelevance;
            }

            return String.Compare(x.Id, y.Id, StringComparison.InvariantCulture);
        }
    }
}

public class ConversationHandlerBuilder<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    private readonly Dictionary<string, ConversationContextCallback<TInput>> _callbackByContextId = new();

    public void Add(string contextId, ConversationContextCallback<TInput> handler)
    {
        _callbackByContextId.Add(contextId, handler);
    }
    
    public ConversationHandler<TInput, TOutput> Build()
    {
        var allContexts = _callbackByContextId.Select(
            kvp => new MappedConversationContext<TInput, TOutput>(kvp.Key, kvp.Value));
        
        return new ConversationHandler<TInput, TOutput>(allContexts);
    }
}

public class ConversationHandler<TInput, TOutput> : ConversationHandler
    where TInput : class
    where TOutput : class
{
    private readonly ImmutableDictionary<string, ConversationContext<TInput, TOutput>> _contextById;

    public ConversationHandler(IEnumerable<ConversationContext<TInput, TOutput>> contexts)
    {
        _contextById = contexts.ToImmutableDictionary(ctx => ctx.Id);
    }
    
    public bool TryProcessInput(
        ConversationState state, 
        TInput input,
        out ConversationState newState,
        out TOutput? output)
    {
        foreach (var contextEntry in state.ContextsInOrder)
        {
            var context = _contextById[contextEntry.Id];
            if (TryProcessInContext(context, out output, out var newStateFromContext))
            {
                newState = newStateFromContext ?? state;
                return true;
            }
        }

        output = null;
        newState = state;
        return false;

        bool TryProcessInContext(
            ConversationContext<TInput, TOutput> context, 
            out TOutput? outputFromContext,
            out ConversationState? newStateFromContext)
        {
            outputFromContext = null;
            newStateFromContext = state; 
            
            foreach (var result in context.Process(input))
            {
                switch (result)
                {
                    case ConversationContext.ProvideOutputResult<TOutput> provideOutput:
                        outputFromContext = provideOutput.Output;
                        break;
                    case ConversationContext.AddContextResult add:
                        newStateFromContext = newStateFromContext with {
                            ContextsInOrder = newStateFromContext.ContextsInOrder.Add(new ContextEntry(
                                add.ContextId, 
                                add.Relevance)) 
                        };
                        break;
                    case ConversationContext.RemoveContextResult remove:
                        var entryToRemove = newStateFromContext.ContextsInOrder.FirstOrDefault(e => e.Id == remove.ContextId);
                        if (entryToRemove != null)
                        {
                            newStateFromContext = newStateFromContext with {
                                ContextsInOrder = newStateFromContext.ContextsInOrder.Remove(entryToRemove)
                            };
                        }
                        break;
                    case ConversationContext.UpdateContextRelevanceResult update:
                        var entryToUpdate = newStateFromContext.ContextsInOrder.FirstOrDefault(e => e.Id == update.ContextId);
                        if (entryToUpdate != null)
                        {
                            newStateFromContext = newStateFromContext with {
                                ContextsInOrder = newStateFromContext.ContextsInOrder
                                    .Remove(entryToUpdate)
                                    .Add(new ContextEntry(update.ContextId, update.NewRelevance))
                            };
                        }
                        break;
                    case ConversationContext.GiveUpResult:
                        outputFromContext = null;
                        break;
                }
            }

            return (outputFromContext != null);
        }
    }
}

public class MappedConversationContext<TInput, TOutput> : ConversationContext<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    private readonly ConversationContextCallback<TInput> _callback;

    public MappedConversationContext(string id, ConversationContextCallback<TInput> callback)
        : base(id)
    {
        _callback = callback;
    }

    public override IEnumerable<Result> Process(TInput input)
    {
        return _callback(input);
    }
}
