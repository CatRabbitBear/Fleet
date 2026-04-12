namespace Fleet.Runtime.Contracts;

public enum MessageType
{
    System,
    User,
    Assistant
}

public class AgentRequest
{
    public string? AgentId { get; set; }
    public List<AgentRequestItem> History { get; set; } = [];
}

public class AgentRequestItem
{
    public MessageType Role { get; set; }
    public string Content { get; set; } = default!;
}

public class AgentResponse
{
    public string Result { get; set; } = default!;
    public string? FilePath { get; set; }
    public string? RunId { get; set; }
}
