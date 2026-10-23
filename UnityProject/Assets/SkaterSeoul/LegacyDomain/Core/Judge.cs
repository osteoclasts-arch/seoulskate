using System;

namespace Skater.Core
{
    /// <summary>
    /// 순수 판정. 입력/스펙/스탯만으로 결정되며 서버에서도 같은 함수를 돌린다.
    /// - 판정 윈도우는 성장으로 넓히지 않는다.
    /// - 랜덤 없음. 같은 LandingSample이면 항상 같은 Verdict.
    /// </summary>
    public static class Judge
    {
        public static PopGrade Grade(double offset, Tuning t)
        {
            double a = Math.Abs(offset);
            if (a <= t.PopPerfect) return PopGrade.Perfect;
            if (a <= t.PopGood) return PopGrade.Good;
            if (a <= t.PopOk) return PopGrade.Ok;
            return PopGrade.Miss;
        }

        public static double LandingTolerance(RiderStats stats, Tuning t)
        {
            double v = t.PitchToleranceBase + stats.Balance * t.PitchToleranceBalance +
                       stats.Landing * t.PitchToleranceLanding;
            if (v < 18.0) v = 18.0;
            if (v > 36.0) v = 36.0;
            return v;
        }

        public static double FlipDuration(RiderStats stats, Tuning t)
        {
            double v = t.FlipDuration - stats.Agility * 0.0008;
            if (v < 0.22) v = 0.22;
            return v;
        }

        public static double BoardSpinDuration(RiderStats stats, Tuning t, double target = 180.0)
        {
            double scale = Math.Max(1.0, Math.Abs(target) / 240.0);
            double v = (t.BoardSpinDuration - stats.Agility * 0.0006) * scale;
            if (v < 0.19 * scale) v = 0.19 * scale;
            return v;
        }

        public static double YawDuration(double target, RiderStats stats, Tuning t)
        {
            double baseDur = Math.Abs(target) >= 300.0 ? t.Yaw360Duration : t.Yaw180Duration;
            double v = baseDur - stats.Agility * (Math.Abs(target) >= 300.0 ? 0.0010 : 0.0004);
            double min = Math.Abs(target) >= 300.0 ? 0.38 : 0.24;
            if (v < min) v = min;
            return v;
        }

        public static double PopImpulse(RiderStats stats, Tuning t)
        {
            return t.BasePopImpulse + stats.Pop * 0.0040;
        }

        public static double ApproachSpeed(RiderStats stats, Tuning t)
        {
            return t.ApproachSpeed + stats.Speed * 0.006;
        }

        public static Verdict Evaluate(LandingSample s, Tuning t, RiderStats stats, int[] masteryTable, Stance stance)
        {
            if (t == null) t = new Tuning();

            Verdict v = new Verdict();
            v.Stance = stance;
            v.Grade = s.Grade;
            v.AirTime = s.AirTime;
            v.Pitch = s.Pitch;
            v.PopOffset = s.PopOffset ?? 0.0;
            v.IsGrind = s.Grind != GrindKind.None;
            v.GrindTime = s.GrindTime;

            Trick trick = TrickCatalog.FromInput(s.Flip, s.YawTarget, s.Grind);
            double bodyYawTarget = TrickMotion.BodyYawTarget(trick, s.NaturalStance, s.Stance, s.Direction);
            v.Trick = trick;
            TrickSpec spec = TrickCatalog.Get(trick);
            v.BaseScore = spec.Points;
            int mastery = 0;
            if (masteryTable != null)
            {
                int idx = TrickCatalog.IndexOf(trick);
                if (idx >= 0 && idx < masteryTable.Length) mastery = masteryTable[idx];
            }
            if (mastery < 0) mastery = 0;
            if (mastery > 100) mastery = 100;

            double tolerance = LandingTolerance(stats, t);
            FailReason reason = ResolveReason(s, t, tolerance, trick);

            bool success = reason == FailReason.None;
            v.Success = success;
            v.Reason = reason;

            double popScore = PopScore(s.Grade);
            double execScore = ExecScore(s, t);
            double rotScore = bodyYawTarget == 0.0
                ? 1.0
                : 1.0 - Clamp01(Math.Abs(s.Yaw - bodyYawTarget) / t.YawTolerance);
            double landScore = 1.0 - Clamp01(Math.Abs(s.Pitch) / tolerance);

            double quality = 40.0 * popScore + 25.0 * execScore + 15.0 * rotScore + 20.0 * landScore;
            quality += Math.Min(5.0, mastery * 0.05);
            if (stats.Courage < spec.Tier * 18) quality -= 4.0;
            if (!success) quality = Math.Min(quality, 55.0);
            v.Quality = (int)Math.Round(Clamp(quality, 0.0, 100.0));
            double rollTarget = TrickMotion.RollTarget(trick, s.NaturalStance, s.Stance);
            v.RollCompletion = rollTarget == 0.0 ? 1.0 : Clamp01(Math.Abs(s.Roll / rollTarget));

            if (success)
            {
                double stanceMult = TrickCatalog.StanceScoreMultiplier(stance);
                double masteryMult = 1.0 + mastery * 0.002;
                double score = spec.Points * (v.Quality / 100.0) * stanceMult * masteryMult;
                v.Score = (int)Math.Round(score);
            }
            else
            {
                v.Score = 0;
            }

            v.Severity = Severity(s, spec, stats, success, t);
            return v;
        }

        private static FailReason ResolveReason(LandingSample s, Tuning t, double tolerance, Trick trick)
        {
            if (s.GrindBailed) return FailReason.SlippedGrind;
            if (s.HitLedge) return FailReason.HitLedge;
            if (s.InGap) return FailReason.FellInGap;

            if (!s.PopOffset.HasValue)
            {
                if (s.AttemptedOffset.HasValue && s.AttemptedOffset.Value > t.PopOk)
                    return FailReason.LatePop;
                return FailReason.MissedPop;
            }

            double off = s.PopOffset.Value;
            if (off > t.PopOk) return FailReason.LatePop;
            if (off < -t.PopOk) return FailReason.EarlyPop;

            if (s.WeakPop && s.AirTime < 0.42) return FailReason.WeakPop;

            if (s.Grind != GrindKind.None)
            {
                if (s.GrindTime < t.GrindMinTime) return FailReason.MissedGrind;
                if (Math.Abs(s.Pitch) > tolerance) return FailReason.BadPitch;
                return FailReason.None;
            }

            double rollTarget = Math.Abs(TrickMotion.RollTarget(trick, s.NaturalStance, s.Stance));
            if (rollTarget > 0.0 && Math.Abs(s.Roll) < rollTarget - 20.0)
                return FailReason.IncompleteFlip;
            double spinTarget = Math.Abs(TrickMotion.BoardSpinTarget(trick, s.NaturalStance, s.Stance, s.Direction));
            if (spinTarget > 0.0 && Math.Abs(s.BoardSpin) < spinTarget - 20.0)
                return FailReason.IncompleteFlip;

            double bodyYawTarget = TrickMotion.BodyYawTarget(trick, s.NaturalStance, s.Stance, s.Direction);
            if (Math.Abs(bodyYawTarget) >= 90.0)
            {
                double delta = s.Yaw - bodyYawTarget;
                if (Math.Abs(delta) > t.YawTolerance)
                {
                    if (Math.Abs(s.Yaw) < Math.Abs(bodyYawTarget)) return FailReason.UnderRotated;
                    return FailReason.OffAxis;
                }
            }

            if (Math.Abs(s.Pitch) > tolerance) return FailReason.BadPitch;
            return FailReason.None;
        }

        private static double PopScore(PopGrade g)
        {
            switch (g)
            {
                case PopGrade.Perfect: return 1.00;
                case PopGrade.Good: return 0.75;
                case PopGrade.Ok: return 0.45;
                default: return 0.00;
            }
        }

        private static double ExecScore(LandingSample s, Tuning t)
        {
            if (s.Grind != GrindKind.None)
            {
                if (s.GrindBailed) return 0.0;
                return Clamp01(s.GrindTime / 1.2);
            }
            Trick trick = TrickCatalog.FromInput(s.Flip, s.YawTarget, GrindKind.None);
            double rollTarget = Math.Abs(TrickMotion.RollTarget(trick, s.NaturalStance, s.Stance));
            double spinTarget = Math.Abs(TrickMotion.BoardSpinTarget(trick, s.NaturalStance, s.Stance, s.Direction));
            double completion = 1.0;
            if (rollTarget > 0.0) completion = Math.Min(completion, Clamp01(Math.Abs(s.Roll) / rollTarget));
            if (spinTarget > 0.0) completion = Math.Min(completion, Clamp01(Math.Abs(s.BoardSpin) / spinTarget));
            return completion;
        }

        private static BailSeverity Severity(LandingSample s, TrickSpec spec, RiderStats stats, bool success, Tuning t)
        {
            if (success) return BailSeverity.None;

            BailSeverity sev;
            if (s.InGap || s.HitLedge) sev = BailSeverity.Severe;
            else if (s.ImpactSpeed > t.ImpactSevere) sev = BailSeverity.Severe;
            else if (s.ImpactSpeed > t.ImpactMedium) sev = BailSeverity.Medium;
            else sev = BailSeverity.Light;

            // 용감함: 고티어 트릭에서 한 단계 완화.
            if (stats.Courage >= spec.Tier * 18 && sev > BailSeverity.Light)
                sev = (BailSeverity)((int)sev - 1);
            return sev;
        }

        public static double Clamp01(double v)
        {
            return Clamp(v, 0.0, 1.0);
        }

        public static double Clamp(double v, double lo, double hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
