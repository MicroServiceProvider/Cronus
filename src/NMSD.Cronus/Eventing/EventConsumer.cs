using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NMSD.Cronus.Commanding;
using NMSD.Cronus.Messaging;
using NMSD.Cronus.Multithreading.Work;
using NMSD.Cronus.Transports;
using NMSD.Cronus.Transports.RabbitMQ;
using NMSD.Protoreg;

namespace NMSD.Cronus.Eventing
{
    public class EventConsumer : BaseInMemoryConsumer<IEvent, IMessageHandler>
    {
        private readonly IPublisher<ICommand> commandPublisher;

        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(EventConsumer));

        private readonly IEndpointFactory endpointFactory;

        private List<WorkPool> pools;

        private readonly ProtoregSerializer serialiser;

        public EventConsumer(IEndpointFactory endpointFactory, ProtoregSerializer serialiser, IPublisher<ICommand> commandPublisher)
        {
            this.commandPublisher = commandPublisher;
            this.endpointFactory = endpointFactory;
            this.serialiser = serialiser;
        }

        public void RegisterAllHandlersInAssembly(Assembly assemblyContainingMessageHandlers)
        {
            RegisterAllHandlersInAssembly(assemblyContainingMessageHandlers, x => (IMessageHandler)FastActivator.CreateInstance(x));
        }
        public void RegisterAllHandlersInAssembly(Assembly assemblyContainingMessageHandlers, Func<Type, IMessageHandler> messageHandlerFactory)
        {
            MessageHandlerRegistrations.RegisterAllHandlersInAssembly<IMessageHandler>(this, assemblyContainingMessageHandlers, x =>
            {
                var handler = FastActivator.CreateInstance(x);
                var port = handler as IPort;
                if (port != null)
                    port.CommandPublisher = commandPublisher;

                return (port ?? handler) as IMessageHandler;
            });
        }


        public override void Start(int numberOfWorkers)
        {
            pools = new List<WorkPool>();
            var endpoints = endpointFactory.GetEndpointDefinitions(base.RegisteredHandlers.Keys.ToArray());

            foreach (var endpoint in endpoints)
            {
                var pool = new WorkPool(String.Format("Workpoll {0}", endpoint.EndpointName), numberOfWorkers);
                for (int i = 0; i < numberOfWorkers; i++)
                {
                    pool.AddWork(new ConsumerWork(this, endpointFactory.CreateEndpoint(endpoint)));
                }
                pools.Add(pool);
                pool.StartCrawlers();
            }
        }

        public override void Stop()
        {
            foreach (WorkPool pool in pools)
            {
                pool.Stop();
            }
        }

        private class ConsumerWork : IWork
        {
            static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ConsumerWork));
            private EventConsumer consumer;
            private readonly IEndpoint endpoint;

            public ConsumerWork(EventConsumer consumer, IEndpoint endpoint)
            {
                this.endpoint = endpoint;
                this.consumer = consumer;
            }

            public DateTime ScheduledStart { get; set; }

            public void Start()
            {
                try
                {
                    endpoint.Open();
                    while (true)
                    {
                        using (var unitOfWork = consumer.UnitOfWorkFactory.NewBatch())
                        {
                            for (int i = 0; i < 100; i++)
                            {

                                EndpointMessage message;
                                if (endpoint.BlockDequeue(30, out message))
                                {
                                    IEvent @event;
                                    using (var stream = new MemoryStream(message.Body))
                                    {
                                        @event = consumer.serialiser.Deserialize(stream) as IEvent;
                                    }
                                    try
                                    {
                                        if (consumer.Handle(@event, unitOfWork))
                                            endpoint.Acknowledge(message);
                                    }
                                    catch (Exception ex)
                                    {
                                        string error = String.Format("Error while handling event '{0}'", @event.ToString());
                                        log.Error(error, ex);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (EndpointClosedException ex)
                {
                    log.Error("Endpoint Closed", ex);
                }
                catch (Exception ex)
                {
                    log.Error("Unexpected Error.", ex);
                }
                finally
                {
                    endpoint.Close();
                    ScheduledStart = DateTime.UtcNow.AddMilliseconds(30);
                }
            }

        }
    }
}