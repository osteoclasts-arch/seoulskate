using System;

namespace Skater.Core
{
    public enum GoalType
    {
        LandTrick,
        LandCount,
        NoBailStreak,
        ScoreAtLeast,
        GrindSeconds,
        DifferentTricks,
        QualityCount
    }

    public sealed class ChallengeSpec
    {
        public string Id;
        public string SpotId;
        public string Title;
        public string Desc;
        public GoalType Type;
        public Trick TargetTrick;
        public Trick AltTrick;
        public bool HasAlt;
        public int Target;
        public int MinQuality;
        public int RewardXp;
        public int Star2;
        public int Star3;
        public Trick[] Unlocks;

        public string GoalText
        {
            get
            {
                switch (Type)
                {
                    case GoalType.LandTrick:
                        if (HasAlt)
                            return Labels.TrickKo(TargetTrick) + " 또는 " + Labels.TrickKo(AltTrick) + " 성공";
                        return Labels.TrickKo(TargetTrick) + " 성공";
                    case GoalType.LandCount:
                        return "트릭 " + Target + "회 성공";
                    case GoalType.NoBailStreak:
                        return Target + "연속 노베일";
                    case GoalType.ScoreAtLeast:
                        return "한 번에 " + Target + "점";
                    case GoalType.GrindSeconds:
                        return "그라인드 누적 " + (Target / 1000.0).ToString("0.0") + "초";
                    case GoalType.DifferentTricks:
                        return "서로 다른 " + Target + "트릭 성공";
                    case GoalType.QualityCount:
                        return "품질 " + MinQuality + " 이상 " + Target + "회";
                    default:
                        return "";
                }
            }
        }

        public string RewardText
        {
            get
            {
                string s = "XP " + RewardXp;
                if (Unlocks == null || Unlocks.Length == 0) return s;
                s += " · ";
                for (int i = 0; i < Unlocks.Length; i++)
                {
                    if (i > 0) s += ", ";
                    s += Labels.TrickKo(Unlocks[i]);
                }
                return s;
            }
        }
    }

    /// <summary>저장되는 챌린지 기록. JsonUtility가 그대로 직렬화한다.</summary>
    [Serializable]
    public sealed class ChallengeRecord
    {
        public int stars;
        public int count;
        public int bestQuality;
        public int bestScore;
        public int grindMs;
        public int streak;
        public int bestStreak;
        public long trickMask;
        public bool completed;
    }

    public static class CareerRules
    {
        /// <summary>시도 결과를 기록에 반영한다. 새로 완료/별 상승 시 true.</summary>
        public static bool Register(ChallengeSpec spec, ChallengeRecord rec, Verdict v)
        {
            if (spec == null || rec == null) return false;

            bool before = rec.completed;
            int beforeStars = rec.stars;

            switch (spec.Type)
            {
                case GoalType.LandTrick:
                    if (v.Success && (v.Trick == spec.TargetTrick || (spec.HasAlt && v.Trick == spec.AltTrick)))
                        rec.count++;
                    break;

                case GoalType.LandCount:
                    if (v.Success) rec.count++;
                    break;

                case GoalType.NoBailStreak:
                    if (v.Success)
                    {
                        rec.streak++;
                        if (rec.streak > rec.bestStreak) rec.bestStreak = rec.streak;
                        rec.count = rec.bestStreak;
                    }
                    else
                    {
                        rec.streak = 0;
                    }
                    break;

                case GoalType.ScoreAtLeast:
                    if (v.Score > rec.bestScore) rec.bestScore = v.Score;
                    rec.count = rec.bestScore;
                    break;

                case GoalType.GrindSeconds:
                    if (v.Success && v.IsGrind)
                        rec.grindMs += (int)Math.Round(v.GrindTime * 1000.0);
                    rec.count = rec.grindMs;
                    break;

                case GoalType.DifferentTricks:
                    if (v.Success)
                    {
                        int idx = TrickCatalog.IndexOf(v.Trick);
                        rec.trickMask |= 1L << idx;
                        rec.count = PopCount(rec.trickMask);
                    }
                    break;

                case GoalType.QualityCount:
                    if (v.Success && v.Quality >= spec.MinQuality) rec.count++;
                    break;
            }

            if (v.Success && v.Quality > rec.bestQuality) rec.bestQuality = v.Quality;
            if (v.Success && v.Score > rec.bestScore) rec.bestScore = v.Score;

            if (!rec.completed && IsComplete(spec, rec)) rec.completed = true;
            if (rec.completed) rec.stars = Math.Max(rec.stars, Stars(spec, rec));

            return (!before && rec.completed) || rec.stars > beforeStars;
        }

        public static bool IsComplete(ChallengeSpec spec, ChallengeRecord rec)
        {
            switch (spec.Type)
            {
                case GoalType.ScoreAtLeast: return rec.bestScore >= spec.Target;
                case GoalType.GrindSeconds: return rec.grindMs >= spec.Target;
                case GoalType.DifferentTricks: return PopCount(rec.trickMask) >= spec.Target;
                default: return rec.count >= spec.Target;
            }
        }

        public static int Stars(ChallengeSpec spec, ChallengeRecord rec)
        {
            if (!rec.completed) return 0;
            int stars = 1;
            if (spec.Type == GoalType.ScoreAtLeast)
            {
                if (rec.bestScore >= spec.Star2) stars = 2;
                if (rec.bestScore >= spec.Star3) stars = 3;
            }
            else
            {
                if (rec.bestQuality >= spec.Star2) stars = 2;
                if (rec.bestQuality >= spec.Star3) stars = 3;
            }
            return stars;
        }

        public static int PopCount(long mask)
        {
            // shortcut: one 64-bit word, move to a multiword mask before adding trick 65.
            int n = 0;
            ulong bits = (ulong)mask;
            while (bits != 0)
            {
                bits &= bits - 1;
                n++;
            }
            return n;
        }
    }

    public static class Career
    {
        private static readonly ChallengeSpec[] Defs = Build();

        public static ChallengeSpec[] All
        {
            get { return Defs; }
        }

        public static int Count
        {
            get { return Defs.Length; }
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Id == id) return i;
            return -1;
        }

        public static ChallengeSpec Get(string id)
        {
            int i = IndexOf(id);
            return i < 0 ? Defs[0] : Defs[i];
        }

        public static int CountForSpot(string spotId)
        {
            int n = 0;
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].SpotId == spotId) n++;
            return n;
        }

        public static ChallengeSpec[] ForSpot(string spotId)
        {
            ChallengeSpec[] arr = new ChallengeSpec[CountForSpot(spotId)];
            int k = 0;
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].SpotId == spotId) arr[k++] = Defs[i];
            return arr;
        }

        private static ChallengeSpec[] Build()
        {
            ChallengeSpec[] arr = new[]
            {
                C("g1", "gap", "스타일 180", "프론트사이드 180으로 첫 회전을 완성하자.",
                    GoalType.LandTrick, Trick.Frontside180, 1, 0, 40, 70, 88,
                    new[] { Trick.Heelflip }),
                C("g2", "gap", "킥플립", "스케이트의 상징. 오른쪽 플릭으로 보드를 튕긴다.",
                    GoalType.LandTrick, Trick.Kickflip, 1, 0, 50, 70, 88,
                    new[] { Trick.Shuvit }),
                C("g3", "gap", "셔빗 타임", "보드만 돌리는 셔빗으로 그라인드의 감각을 예열한다.",
                    GoalType.LandTrick, Trick.Shuvit, 1, 0, 60, 70, 88,
                    new[] { Trick.Impossible, Trick.FiftyFifty, Trick.VarialKickflip }),

                C("d1", "drop", "계단 힐플립", "1.2m 낙차에서 힐플립을 버텨라.",
                    GoalType.LandTrick, Trick.Heelflip, 1, 0, 70, 70, 88,
                    new[] { Trick.BacksideFlip }),
                C("d2", "drop", "노 베일", "계단에서 3번 연속으로 넘어지지 않는다.",
                    GoalType.NoBailStreak, Trick.Ollie, 3, 0, 80, 70, 88,
                    new[] { Trick.Hardflip, Trick.Frontside360, Trick.Backside360, Trick.VarialHeelflip }),
                C("d3", "drop", "드롭 스코어", "한 번의 시도로 500점을 넘긴다.",
                    GoalType.ScoreAtLeast, Trick.Ollie, 500, 0, 90, 600, 750,
                    new[] { Trick.FrontsideFlip }),

                C("l1", "ledge", "렛지 입문", "렛지 위에서 탭해 50-50을 탄다.",
                    GoalType.LandTrick, Trick.FiftyFifty, 1, 0, 100, 65, 85,
                    new[] { Trick.Boardslide, Trick.Lipslide, Trick.Noseslide, Trick.Tailslide,
                        Trick.Darkslide, Trick.Hurricane }),
                C("l2", "ledge", "보드슬라이드", "렛지에서 오른쪽 플릭으로 보드를 90도 돌린다.",
                    GoalType.LandTrick, Trick.Boardslide, 1, 0, 110, 65, 85,
                    new[] { Trick.Nosegrind, Trick.Bluntslide, Trick.Nosebluntslide,
                        Trick.Salad, Trick.Suski }),
                C("l3", "ledge", "그라인드 세션", "렛지에서 그라인드를 누적 3초 유지한다.",
                    GoalType.GrindSeconds, Trick.FiftyFifty, 3000, 0, 120, 65, 85,
                    new[] { Trick.FiveO, Trick.Crooked, Trick.Smith, Trick.Feeble,
                        Trick.Overcrook, Trick.Willy }),

                C("q1", "quarter", "빅에어", "쿼터에서 한 번에 700점을 만든다.",
                    GoalType.ScoreAtLeast, Trick.Ollie, 700, 0, 130, 850, 1000,
                    new[] { Trick.InwardHeelflip, Trick.TreFlip }),
                C("q2", "quarter", "360", "프론트 또는 백사이드 360을 완성한다.",
                    GoalType.LandTrick, Trick.Frontside360, 1, 0, 150, 75, 90,
                    new[] { Trick.Frontside360Shuvit, Trick.Backside360Shuvit }),
                C("q3", "quarter", "파이널 라인", "서로 다른 5개의 트릭을 성공시킨다.",
                    GoalType.DifferentTricks, Trick.Ollie, 5, 0, 200, 80, 92,
                    new[] { Trick.ImpossibleFrontside180, Trick.ImpossibleBackside180, Trick.LaserFlip })
            };

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Id == "q2")
                {
                    arr[i].AltTrick = Trick.Backside360;
                    arr[i].HasAlt = true;
                }
            }
            return arr;
        }

        private static ChallengeSpec C(string id, string spot, string title, string desc,
            GoalType type, Trick trick, int target, int minQuality, int xp, int star2, int star3,
            Trick[] unlocks)
        {
            ChallengeSpec c = new ChallengeSpec();
            c.Id = id;
            c.SpotId = spot;
            c.Title = title;
            c.Desc = desc;
            c.Type = type;
            c.TargetTrick = trick;
            c.Target = target;
            c.MinQuality = minQuality;
            c.RewardXp = xp;
            c.Star2 = star2;
            c.Star3 = star3;
            c.Unlocks = unlocks;
            return c;
        }

    }
}
