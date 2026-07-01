namespace RateShield.Core.RateLimiting;

public interface IRateLimitEvaluator
{
    RateLimitDecision Evaluate(RateLimitEvaluationRequest request);
}
