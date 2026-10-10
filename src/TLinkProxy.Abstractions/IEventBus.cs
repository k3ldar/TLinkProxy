namespace TLinkProxy.Abstractions
{
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : class;

        void Publish<TEvent>(TEvent @event) where TEvent : class;
    }
}
