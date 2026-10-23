using System;

namespace Skater.Core
{
    public enum MissionType
    {
        LandTrick,
        QualityCount,
        NoBailCount,
        GrindSeconds,
        ScoreOnce,
        Observe,
        WinMatch,
        DifferentTricks,
        AirTime
    }

    public sealed class MissionSpec
    {
        public string Id;
        public string Title;
        public string Desc;
        public MissionType Type;
        public int Target;
        public Trick Trick;
        public int RewardXp;
        public string Unit;

        public string GoalText
        {
            get
            {
                switch (Type)
                {
                    case MissionType.LandTrick: return Labels.TrickKo(Trick) + " " + Target + "회";
                    case MissionType.QualityCount: return "품질 75 이상 " + Target + "회";
                    case MissionType.NoBailCount: return Target + "연속 노베일";
                    case MissionType.GrindSeconds: return "그라인드 " + (Target / 1000.0).ToString("0.0") + "초";
                    case MissionType.ScoreOnce: return "한 번에 " + Target + "점";
                    case MissionType.Observe: return "새 트릭 " + Target + "개 관찰";
                    case MissionType.WinMatch: return "S.K.A.T.E " + Target + "승";
                    case MissionType.DifferentTricks: return "서로 다른 " + Target + "트릭";
                    case MissionType.AirTime: return "체공 " + (Target / 1000.0).ToString("0.0") + "초";
                    default: return "";
                }
            }
        }
    }

    [Serializable]
    public sealed class MissionRecord
    {
        public string id;
        public int progress;
        public int streak;
        public long trickMask;
        public bool claimed;
    }

    public static class MissionCatalog
    {
        private static readonly MissionSpec[] Defs = Build();

        public static MissionSpec[] Pool
        {
            get { return Defs; }
        }

        public static MissionSpec Get(string id)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Id == id) return Defs[i];
            return Defs[0];
        }

        /// <summary>날짜 문자열로 오늘의 미션을 결정론적으로 뽑는다.</summary>
        public static MissionSpec[] Roll(string dateKey, int count)
        {
            int seed = Hash(dateKey);
            Rng rng = new Rng(seed);
            int[] order = new int[Defs.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }

            if (count < 1) count = 1;
            if (count > Defs.Length) count = Defs.Length;
            MissionSpec[] arr = new MissionSpec[count];
            for (int i = 0; i < count; i++) arr[i] = Defs[order[i]];
            return arr;
        }

        private static int Hash(string s)
        {
            uint h = 2166136261u;
            if (s == null) s = "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            int v = (int)(h & 0x7FFFFFFF);
            return v == 0 ? 1337 : v;
        }

        private static MissionSpec[] Build()
        {
            return new[]
            {
                M("m_kick3", "킥플립 3회", "킥플립을 3번 성공시키세요.",
                    MissionType.LandTrick, 3, Trick.Kickflip, 60, "회"),
                M("m_quality", "품질 75 이상 3회", "품질 75 이상으로 3번 착지하세요.",
                    MissionType.QualityCount, 3, Trick.Ollie, 70, "회"),
                M("m_nobail", "노 베일 4연속", "4번 연속으로 넘어지지 마세요.",
                    MissionType.NoBailCount, 4, Trick.Ollie, 80, "연속"),
                M("m_grind", "그라인드 3초", "렛지에서 그라인드를 누적 3초 타세요.",
                    MissionType.GrindSeconds, 3000, Trick.FiftyFifty, 80, "ms"),
                M("m_score", "한 번에 600점", "한 번의 시도로 600점을 넘기세요.",
                    MissionType.ScoreOnce, 600, Trick.Ollie, 60, "점"),
                M("m_observe", "새 트릭 관찰", "새로운 트릭 1개를 관찰하세요.",
                    MissionType.Observe, 1, Trick.Ollie, 50, "종"),
                M("m_match", "S.K.A.T.E 1승", "봇과의 S.K.A.T.E에서 1승을 거두세요.",
                    MissionType.WinMatch, 1, Trick.Ollie, 100, "승"),
                M("m_variety", "서로 다른 4트릭", "서로 다른 4개의 트릭을 성공시키세요.",
                    MissionType.DifferentTricks, 4, Trick.Ollie, 70, "종"),
                M("m_air", "체공 0.9초", "한 번의 시도에서 0.9초 이상 체공하세요.",
                    MissionType.AirTime, 900, Trick.Ollie, 60, "ms")
            };
        }

        private static MissionSpec M(string id, string title, string desc, MissionType type,
            int target, Trick trick, int xp, string unit)
        {
            MissionSpec m = new MissionSpec();
            m.Id = id;
            m.Title = title;
            m.Desc = desc;
            m.Type = type;
            m.Target = target;
            m.Trick = trick;
            m.RewardXp = xp;
            m.Unit = unit;
            return m;
        }
    }

    public static class MissionRules
    {
        public static void RegisterAttempt(MissionSpec spec, MissionRecord rec, Verdict v)
        {
            if (spec == null || rec == null) return;

            switch (spec.Type)
            {
                case MissionType.LandTrick:
                    if (v.Success && v.Trick == spec.Trick) rec.progress++;
                    break;
                case MissionType.QualityCount:
                    if (v.Success && v.Quality >= 75) rec.progress++;
                    break;
                case MissionType.NoBailCount:
                    if (v.Success)
                    {
                        rec.streak++;
                        if (rec.streak > rec.progress) rec.progress = rec.streak;
                    }
                    else
                    {
                        rec.streak = 0;
                    }
                    break;
                case MissionType.GrindSeconds:
                    if (v.Success && v.IsGrind)
                        rec.progress += (int)Math.Round(v.GrindTime * 1000.0);
                    break;
                case MissionType.ScoreOnce:
                    if (v.Score > rec.progress) rec.progress = v.Score;
                    break;
                case MissionType.DifferentTricks:
                    if (v.Success)
                    {
                        int idx = TrickCatalog.IndexOf(v.Trick);
                        rec.trickMask |= 1L << idx;
                        rec.progress = CareerRules.PopCount(rec.trickMask);
                    }
                    break;
                case MissionType.AirTime:
                    int ms = (int)Math.Round(v.AirTime * 1000.0);
                    if (ms > rec.progress) rec.progress = ms;
                    break;
            }
        }

        public static void RegisterObservation(MissionRecord rec, int add = 1)
        {
            if (rec == null) return;
            rec.progress += add;
        }

        public static void RegisterWin(MissionRecord rec)
        {
            if (rec == null) return;
            rec.progress++;
        }

        public static bool IsComplete(MissionSpec spec, MissionRecord rec)
        {
            if (spec == null || rec == null) return false;
            return rec.progress >= spec.Target;
        }

        public static string ProgressText(MissionSpec spec, MissionRecord rec)
        {
            if (spec == null || rec == null) return "";
            int p = rec.progress;
            if (p > spec.Target) p = spec.Target;
            switch (spec.Type)
            {
                case MissionType.GrindSeconds:
                case MissionType.AirTime:
                    return (p / 1000.0).ToString("0.0") + "/" + (spec.Target / 1000.0).ToString("0.0") + "초";
                case MissionType.ScoreOnce:
                    return p + "/" + spec.Target + "점";
                default:
                    return p + "/" + spec.Target;
            }
        }
    }
}
