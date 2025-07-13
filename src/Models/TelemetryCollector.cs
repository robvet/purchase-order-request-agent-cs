namespace NearbyCS_API.Models
{
    // TelemetryCollector: stores telemetry for the current request
    public class TelemetryCollector
    {
        private readonly List<string> _telemetry = new();
        public void Add(string entry) => _telemetry.Add(entry);
        public IReadOnlyList<string> GetAll() => _telemetry;
    }
}
