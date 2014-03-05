using System;
using System.Collections.Generic;
using NMSD.Cronus.DomainModelling;
using NMSD.Cronus.Messaging;
using NMSD.Cronus.Transports;

namespace NMSD.Cronus.Pipelining
{
    public interface IEndpointConsumer : ITransportIMessage
    {
        bool Consume(IEndpoint endpoint);
        IEnumerable<Type> GetRegisteredHandlers { get; }
    }

    public interface IEndpointConsumer<out T> : IEndpointConsumer where T : IMessage
    {

    }
}