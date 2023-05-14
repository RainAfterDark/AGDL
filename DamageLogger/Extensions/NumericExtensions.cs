namespace DamageLogger.Extensions;

public static class NumericExtensions
{
    public static float SafeDivision(this float numerator, float denominator)
    {
        return (denominator == 0) ? 0 : numerator / denominator;
    }
}