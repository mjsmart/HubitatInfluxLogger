namespace HubitatInfluxLogger.Client
{
    public class HubMessage
    {
        public string DescriptionText { get; set; }
        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string Value { get; set; }
    }
}