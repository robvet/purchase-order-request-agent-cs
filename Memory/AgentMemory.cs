public class AgentMemory
{
    public RoutePlan? RoutePlan { get; set; }
    public TrafficWindow? TrafficWindow { get; set; }
    public RouteRiskAssessment? RouteRiskAssessment { get; set; }
    public CustomerNotificationResult? CustomerNotificationResult { get; set; }
    public ComplianceCheckResult? ComplianceCheckResult { get; set; }
    public IncidentReport? IncidentReport { get; set; }
    public SupportDispatch? SupportDispatch { get; set; }

    public void Update<T>(T data)
    {
        switch (data)
        {
            case RoutePlan plan:
                RoutePlan = plan; break;
            case TrafficWindow window:
                TrafficWindow = window; break;
            case RouteRiskAssessment risk:
                RouteRiskAssessment = risk; break;
            case CustomerNotificationResult notification:
                CustomerNotificationResult = notification; break;
            case ComplianceCheckResult compliance:
                ComplianceCheckResult = compliance; break;
            case IncidentReport incident:
                IncidentReport = incident; break;
            case SupportDispatch support:
                SupportDispatch = support; break;
            default:
                throw new InvalidOperationException("Unsupported type for update");
        }
    }
}