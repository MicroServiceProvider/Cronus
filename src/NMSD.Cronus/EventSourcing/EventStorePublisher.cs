using System;
using System.Linq;
using NMSD.Cronus.DomainModelling;
using NMSD.Cronus.Messaging;
using NMSD.Cronus.Transports;
using NMSD.Protoreg;

namespace NMSD.Cronus.EventSourcing
{
    public class EventStorePublisher : Publisher<DomainMessageCommit>
    {
        private readonly IPipelineFactory pipelineFactory;

        public EventStorePublisher(IPipelineFactory pipelineFactory, ProtoregSerializer serializer)
        {
            this.pipelineFactory = pipelineFactory;
            this.serializer = serializer;
        }

        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(EventStorePublisher));

        private readonly ProtoregSerializer serializer;

        protected override bool PublishInternal(DomainMessageCommit message)
        {
            var firstEventInCommitType = message.Events.First().GetType();
            var endpointMessage = message.AsEndpointMessage(serializer);
            var commitBoundedContext = firstEventInCommitType.GetAssemblyAttribute<BoundedContextAttribute>().BoundedContextName;
            endpointMessage.Headers.Add(commitBoundedContext, String.Empty);
            pipelineFactory
                .GetPipeline(firstEventInCommitType)
                .Push(endpointMessage);
            return true;
        }
    }
}