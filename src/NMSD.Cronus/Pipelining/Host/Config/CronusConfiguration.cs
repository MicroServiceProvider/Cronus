using System;
using System.Collections.Generic;
using NMSD.Cronus.DomainModelling;
using NMSD.Cronus.EventSourcing;
using NMSD.Cronus.EventSourcing.Config;
using NMSD.Cronus.Hosting;
using NMSD.Cronus.Pipelining.Transport.Config;
using NMSD.Protoreg;

namespace NMSD.Cronus.Pipelining.Host.Config
{
    public class CronusConfiguration
    {
        public List<IEndpointConsumerSetting> ConsumersConfigurations = new List<IEndpointConsumerSetting>();

        public List<EventStoreSettings> EventStoreConfigurations = new List<EventStoreSettings>();

        public CronusConfiguration()
        {
            GlobalSettings = new CronusGlobalSettings();
            GlobalSettings.Protoreg = new ProtoRegistration();
            GlobalSettings.Serializer = new ProtoregSerializer(GlobalSettings.Protoreg);
            GlobalSettings.Protoreg.RegisterAssembly(typeof(EventBatchWraper));
            GlobalSettings.Protoreg.RegisterAssembly(typeof(IMessage));
        }

        public PipelineCommandPublisherSettings CommandPublisherConfiguration { get; set; }

        public PipelineEventPublisherSettings EventPublisherConfiguration { get; set; }

        public PipelineEventStorePublisherSettings EventStorePublisherConfiguration { get; set; }

        public CronusGlobalSettings GlobalSettings { get; set; }

        public CronusConfiguration Build()
        {
            if (CommandPublisherConfiguration != null)
                GlobalSettings.CommandPublisher = CommandPublisherConfiguration.Build();

            if (EventPublisherConfiguration != null)
                GlobalSettings.EventPublisher = EventPublisherConfiguration.Build();

            if (EventStorePublisherConfiguration != null)
                GlobalSettings.EventStorePublisher = EventStorePublisherConfiguration.Build();

            foreach (var esSettings in EventStoreConfigurations)
            {
                var eventStore = esSettings.Build();
                GlobalSettings.EventStores.Add(eventStore);
            }

            foreach (var consumerSettings in ConsumersConfigurations)
            {
                IEndpointConsumable consumable = consumerSettings.Build();
                GlobalSettings.Consumers.Add(consumable, consumerSettings.NumberOfWorkers);
            }

            GlobalSettings.Serializer.Build();

            return this;
        }
    }
}