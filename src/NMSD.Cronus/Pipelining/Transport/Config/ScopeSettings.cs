using NMSD.Cronus.Messaging.MessageHandleScope;

namespace NMSD.Cronus.Sample.Player.Commands.Config
{
    public class ScopeSettings
    {
        public IBatchScope BatchScope { get; set; }
        public IMessageScope MessageScope { get; set; }
        public IHandlerScope HandlerScope { get; set; }
    }
}