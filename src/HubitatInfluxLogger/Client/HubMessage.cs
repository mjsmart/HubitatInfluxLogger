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
        public string LocationId { get; set; }
        public string HubId { get; set; }
        public string InstalledAppId { get; set; }
        public string Unit { get; set; }
    }
}