namespace MealPenalty.Api.Controllers;

public static class ActorHeader
{
    public const string HeaderName = "X-Actor-Name";

    /// <summary>Every write endpoint requires this header so audit rows always have a real actor.</summary>
    public static bool TryGet(HttpRequest request, out string actor)
    {
        actor = request.Headers[HeaderName].ToString().Trim();
        return !string.IsNullOrEmpty(actor);
    }
}
