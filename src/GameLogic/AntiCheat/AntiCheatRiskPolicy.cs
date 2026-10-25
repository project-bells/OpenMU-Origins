// <copyright file="AntiCheatRiskPolicy.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// Categories of server-observed anti-cheat evidence.
/// </summary>
public enum AntiCheatCategory
{
    MovementSpeed,
    Teleport,
    AttackSpeed,
    SkillCooldown,
    AttackRange,
    PacketFrequency,
    PacketSequence,
    ItemIntegrity,
    TradeIntegrity,
    CurrencyIntegrity,
    OfflineHunting,
    BotBehavior,
    MultiClient,
}

/// <summary>
/// The risk level produced from accumulated evidence.
/// </summary>
public enum AntiCheatRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Confirmed,
}

/// <summary>
/// A normalized, immutable anti-cheat observation. Facts must not contain secrets or raw credentials.
/// </summary>
/// <param name="Category">The violated invariant.</param>
/// <param name="Points">The positive evidence weight.</param>
/// <param name="Code">A stable machine-readable evidence code.</param>
/// <param name="Facts">A compact description of server-observed facts.</param>
/// <param name="ObservedAt">The server timestamp.</param>
public sealed record AntiCheatEvidence(
    AntiCheatCategory Category,
    double Points,
    string Code,
    string Facts,
    DateTimeOffset ObservedAt);

/// <summary>
/// The result of applying evidence to a risk state.
/// </summary>
/// <param name="Score">The decayed score after adding the evidence.</param>
/// <param name="Level">The resulting risk level.</param>
/// <param name="RejectAction">Whether the current client action must be rejected.</param>
/// <param name="Disconnect">Whether the connection should be closed.</param>
/// <param name="RequiresManualReview">Whether an administrator must review the evidence.</param>
public sealed record AntiCheatDecision(
    double Score,
    AntiCheatRiskLevel Level,
    bool RejectAction,
    bool Disconnect,
    bool RequiresManualReview);

/// <summary>
/// Pure policy for decaying and classifying anti-cheat evidence.
/// </summary>
public static class AntiCheatRiskPolicy
{
    public const double MediumThreshold = 25.0;
    public const double HighThreshold = 60.0;
    public const double ConfirmedThreshold = 100.0;
    public const double DefaultHalfLifeMinutes = 15.0;
    public const int MinimumCategoriesForConfirmation = 2;

    /// <summary>
    /// Applies exponential decay to a risk score.
    /// </summary>
    public static double Decay(double score, TimeSpan elapsed, double halfLifeMinutes = DefaultHalfLifeMinutes)
    {
        if (score <= 0 || elapsed <= TimeSpan.Zero)
        {
            return Math.Max(0, score);
        }

        double effectiveHalfLife = halfLifeMinutes > 0 ? halfLifeMinutes : DefaultHalfLifeMinutes;
        return score * Math.Pow(0.5, elapsed.TotalMinutes / effectiveHalfLife);
    }

    /// <summary>
    /// Classifies a score. Confirmation requires corroboration by independent categories to avoid
    /// turning one noisy timing signal into a ban candidate.
    /// </summary>
    public static AntiCheatRiskLevel Classify(double score, int distinctCategories)
    {
        if (score >= ConfirmedThreshold && distinctCategories >= MinimumCategoriesForConfirmation)
        {
            return AntiCheatRiskLevel.Confirmed;
        }

        if (score >= HighThreshold)
        {
            return AntiCheatRiskLevel.High;
        }

        if (score >= MediumThreshold)
        {
            return AntiCheatRiskLevel.Medium;
        }

        return score > 0 ? AntiCheatRiskLevel.Low : AntiCheatRiskLevel.None;
    }

    /// <summary>
    /// Creates an enforcement decision. Impossible actions are rejected independently of the score;
    /// a permanent ban is intentionally not an automatic outcome of this policy.
    /// </summary>
    public static AntiCheatDecision Decide(double score, int distinctCategories, bool impossibleAction)
    {
        var level = Classify(score, distinctCategories);
        return new AntiCheatDecision(
            score,
            level,
            impossibleAction,
            level is AntiCheatRiskLevel.High or AntiCheatRiskLevel.Confirmed,
            level == AntiCheatRiskLevel.Confirmed);
    }
}
