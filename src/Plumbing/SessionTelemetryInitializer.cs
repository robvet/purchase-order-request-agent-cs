using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace SingleAgent.Plubming
{
    internal class SessionTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.ContainsKey("SessionId") == true)
            {
                var sessionId = context.Items["SessionId"]?.ToString();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    telemetry.Context.GlobalProperties["SessionId"] = sessionId;
                }
            }
        }
    }
}
