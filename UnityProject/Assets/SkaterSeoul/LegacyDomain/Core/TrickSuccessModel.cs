using System;

namespace Skater.Core
{
    public static class TrickSuccessModel
    {
        public static double Calculate(double inputAccuracy, int mastery, double relevantStat,
            double difficulty, double obstacleRisk, double fatigue, Tuning tuning = null)
        {
            Tuning t = tuning ?? new Tuning();
            double m = Clamp(mastery, 0.0, 100.0);
            double stat = Clamp(relevantStat, 0.0, 100.0);
            double accuracy = Clamp(inputAccuracy, 0.0, 100.0);
            double hard = Clamp(difficulty, 0.0, 100.0);
            double risk = Clamp(obstacleRisk, 0.0, 100.0);
            double tired = Clamp(fatigue, 0.0, 100.0);

            double chance = t.SuccessBase + t.SuccessMasteryWeight * (m - 5.0) / 95.0 +
                t.SuccessStatWeight * (stat - 5.0) / 95.0 +
                t.SuccessDifficultyRelief * (100.0 - hard) / 100.0 -
                t.SuccessAccuracyPenalty * (100.0 - accuracy) / 100.0 -
                t.SuccessObstaclePenalty * risk / 100.0 -
                t.SuccessFatiguePenalty * tired / 100.0;
            return Clamp(chance, t.SuccessMin, t.SuccessMax);
        }

        public static double RelevantStat(Trick trick, RiderStats stats)
        {
            TrickCategory category = TrickCatalog.Get(trick).Category;
            if (category == TrickCategory.Grind) return (stats.Balance + stats.Landing) * 0.5;
            if (category == TrickCategory.Flip) return (stats.Agility + stats.Landing) * 0.5;
            return (stats.Pop + stats.Landing) * 0.5;
        }

        public static double Difficulty(Trick trick)
        {
            return Clamp(TrickCatalog.Get(trick).Tier * (100.0 / 3.0), 0.0, 100.0);
        }

        public static Verdict Apply(Verdict candidate, LandingSample sample, RiderStats stats,
            int mastery, Tuning tuning, double roll, int attemptId)
        {
            candidate.InputAccuracy = EffectiveAccuracy(sample.InputAccuracy, sample.Grade);
            candidate.AttemptId = attemptId;
            candidate.SuccessRoll = -1.0;
            candidate.SuccessProbability = 0.0;
            if (!candidate.Success) return candidate;

            candidate.SuccessProbability = Calculate(candidate.InputAccuracy, mastery,
                RelevantStat(candidate.Trick, stats), Difficulty(candidate.Trick),
                sample.ObstacleRisk, sample.Fatigue, tuning);
            candidate.SuccessRoll = Clamp(roll, 0.0, 1.0);
            if (candidate.SuccessRoll < candidate.SuccessProbability) return candidate;

            candidate.Success = false;
            candidate.Reason = FailReason.UnstableLanding;
            candidate.Score = 0;
            candidate.Quality = Math.Min(candidate.Quality, 55);
            candidate.Severity = sample.ImpactSpeed > (tuning ?? new Tuning()).ImpactMedium
                ? BailSeverity.Medium : BailSeverity.Light;
            return candidate;
        }

        public static double EffectiveAccuracy(double directionAccuracy, PopGrade grade)
        {
            double timing;
            switch (grade)
            {
                case PopGrade.Perfect: timing = 100.0; break;
                case PopGrade.Good: timing = 75.0; break;
                case PopGrade.Ok: timing = 45.0; break;
                default: timing = 0.0; break;
            }
            return Math.Min(Clamp(directionAccuracy, 0.0, 100.0), timing);
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
