using RateShield.Core.RateLimiting;

namespace RateShield.Gateway.Tests.Middleware;

public sealed class FakeRateLimitEvaluator : IRateLimitEvaluator
{
    private readonly Queue<RateLimitDecision> _decisions;

    public FakeRateLimitEvaluator(IEnumerable<RateLimitDecision> decisions)
    {
        _decisions = new Queue<RateLimitDecision>(decisions);
    }

    public RateLimitEvaluationRequest? LastRequest { get; private set; }

    public RateLimitDecision Evaluate(RateLimitEvaluationRequest request)
    {
        //   throw new NotImplementedException();
        LastRequest = request;

        return _decisions.Count > 0
            ? _decisions.Dequeue()
            : RateLimitDecision.Allowed(
                policyName: "Default",
                limit: 100,
                remaining: 99,
                resetAt: DateTimeOffset.UtcNow.AddSeconds(1)
            );
    }
}
