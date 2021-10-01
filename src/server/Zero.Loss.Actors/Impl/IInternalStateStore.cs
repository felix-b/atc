namespace Zero.Loss.Actors.Impl
{
    public interface IInternalStateStore : IStateStore
    {
        void ResetNextSequenceNo(ulong value);
        ulong NextSequenceNo { get; }
    }
}
