using System.Collections.Immutable;

namespace Atc.World.Contracts.Communications;

public abstract class ConversationContext
{
    public static GiveUpResult GiveUp() => 
        new GiveUpResult();
    
    public static AddContextResult AddContext(string id, ConversationContextRelevance relevance) => 
        new AddContextResult(id, relevance);
    
    public static RemoveContextResult RemoveContext(string id) => 
        new RemoveContextResult(id);

    public static UpdateContextRelevanceResult RemoveContext(string id, ConversationContextRelevance newRelevance) => 
        new UpdateContextRelevanceResult(id, newRelevance);

    public static ProvideOutputResult<T> ProvideOutput<T>(T output)
        where T : class
        => new ProvideOutputResult<T>(output);

    public abstract record Result();
    public record GiveUpResult() : Result;
    public record AddContextResult(
        string ContextId,
        ConversationContextRelevance Relevance
    ) : Result;
    public record RemoveContextResult(
        string ContextId
    ) : Result;
    public record UpdateContextRelevanceResult(
        string ContextId,
        ConversationContextRelevance NewRelevance
    ) : Result;
    public record ProvideOutputResult<T>(
        T Output
    ) : Result where T : class;
}

public enum ConversationContextRelevance
{
    FadingOut = 0,
    Background = 1,
    Foreground = 2,
}

public abstract class ConversationContext<TInput, TOutput> : ConversationContext 
    where TInput : class
    where TOutput : class
{
    protected ConversationContext(string id)
    {
        Id = id;
    }
    
    public abstract IEnumerable<Result> Process(TInput input);

    public string Id { get; }

}

public delegate IEnumerable<ConversationContext.Result> ConversationContextCallback<TInput>(TInput input);
