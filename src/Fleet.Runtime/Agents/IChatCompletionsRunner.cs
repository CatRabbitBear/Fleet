using Fleet.Runtime.Contracts;

namespace Fleet.Runtime.Agents;

public interface IChatCompletionsRunner
{
    Task<AgentResponse> RunTaskAsync(List<AgentRequestItem> history);
}
