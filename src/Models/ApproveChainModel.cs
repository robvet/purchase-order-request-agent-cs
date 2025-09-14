namespace SingleAgent.Models
{
    public class ApproveChainModel
    {
        public string SupervisorEmail { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public List<string> ApproverEmails { get; set; } = new();
    }
}
