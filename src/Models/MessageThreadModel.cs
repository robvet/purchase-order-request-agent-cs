using Azure.AI.OpenAI;
using common.Enums;

namespace SingleAgent.Models
{
    public class MessageThreadModel
    {
        //public MessageThreadModel(Role role, string content, int tokens)
        public MessageThreadModel(Role role, string content, int tokens)
        {
            Role = role;
            Content = content;
            Tokens = tokens;
            TimeStamp = DateTime.UtcNow;
        }
        public string Name { get; } = "";

        public string Content { get; } = "";
        public Role Role { get; }

        public DateTime TimeStamp { get;  }
        public int Tokens { get;  }
        public string FunctionCall { get;  } = "";
    }
}
