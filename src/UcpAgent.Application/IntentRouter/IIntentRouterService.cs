namespace UcpAgent.Application.IntentRouter;

public interface IIntentRouterService
{
    IntentResult Detect(string input, string? sessionId = null);
}
