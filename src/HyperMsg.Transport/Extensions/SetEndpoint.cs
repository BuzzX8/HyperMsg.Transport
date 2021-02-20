namespace HyperMsg.Transport.Extensions
{
    internal struct SetEndpoint<T>
    {
        internal SetEndpoint(T endpoint) => Endpoint = endpoint;

        internal T Endpoint { get; }
    }
}
