using System;

namespace Skater.Core
{
    public enum Phase
    {
        Ready,
        Riding,
        Air,
        Grind,
        Landed,
        Bailed
    }

    /// <summary>오른손 플릭의 종류. 보드 회전축을 결정한다.</summary>
    public enum FlipKind
    {
        None,
        Kickflip,
        Heelflip,
        Shuvit,
        Impossible,
        VarialKickflip,
        VarialHeelflip,
        TreFlip,
        LaserFlip
    }

    /// <summary>팝 타이밍 등급. 숫자가 클수록 좋다.</summary>
    public enum PopGrade
    {
        Miss = 0,
        Ok = 1,
        Good = 2,
        Perfect = 3
    }

    public enum BailSeverity
    {
        None,
        Light,
        Medium,
        Severe
    }

    public enum Spin
    {
        None,
        Frontside,
        Backside
    }

    public enum TrickCategory
    {
        Air,
        Flip,
        Grind
    }

    /// <summary>
    /// 코어 트릭. 순서는 저장/도감 인덱스이므로 함부로 바꾸지 않는다.
    /// (새 트릭은 배열 끝에 추가하고 Progress 버전을 올린다)
    /// </summary>
    public enum Trick
    {
        Ollie,
        Frontside180,
        Backside180,
        Kickflip,
        Heelflip,
        Shuvit,
        Impossible,
        FiftyFifty,
        BacksideFlip,
        Hardflip,
        Frontside360,
        Backside360,
        FrontsideFlip,
        Boardslide,
        Nosegrind,
        FiveO,
        Crooked,
        InwardHeelflip,
        Frontside360Shuvit,
        Backside360Shuvit,
        ImpossibleFrontside180,
        ImpossibleBackside180,
        VarialKickflip,
        VarialHeelflip,
        TreFlip,
        LaserFlip,
        Lipslide,
        Noseslide,
        Tailslide,
        Bluntslide,
        Nosebluntslide,
        Smith,
        Feeble,
        Overcrook,
        Darkslide,
        Hurricane,
        Salad,
        Suski,
        Willy
    }

    public enum GrindKind
    {
        None,
        FiftyFifty,
        Boardslide,
        Nosegrind,
        FiveO,
        Crooked,
        Lipslide,
        Noseslide,
        Tailslide,
        Bluntslide,
        Nosebluntslide,
        Smith,
        Feeble,
        Overcrook,
        Darkslide,
        Hurricane,
        Salad,
        Suski,
        Willy
    }

    public struct TrickInput
    {
        public FlipKind Flip;
        public double YawTarget;
        public GrindKind Grind;

        public TrickInput(FlipKind flip, double yawTarget = 0.0, GrindKind grind = GrindKind.None)
        {
            Flip = flip;
            YawTarget = yawTarget;
            Grind = grind;
        }
    }

    // Trick stance relative to the player's natural footing; Regular here means the default trick stance.
    public enum Stance
    {
        Regular,
        Switch,
        Fakie,
        Nollie
    }

    public enum NaturalStance
    {
        Regular,
        Goofy
    }

    public enum FailReason
    {
        None,
        FellInGap,
        MissedPop,
        LatePop,
        EarlyPop,
        IncompleteFlip,
        BadPitch,
        OffAxis,
        WeakPop,
        SlippedGrind,
        MissedGrind,
        HitLedge,
        UnderRotated,
        BadLanding,
        UnstableLanding
    }

    /// <summary>판정에 쓰는 6능력치. 0~100.</summary>
    public struct RiderStats
    {
        public int Pop;
        public int Balance;
        public int Speed;
        public int Landing;
        public int Courage;
        public int Agility;

        public static RiderStats Default
        {
            get
            {
                RiderStats s;
                s.Pop = 10;
                s.Balance = 10;
                s.Speed = 10;
                s.Landing = 10;
                s.Courage = 10;
                s.Agility = 10;
                return s;
            }
        }

        public static RiderStats FromArray(int[] stats)
        {
            RiderStats s = Default;
            if (stats == null || stats.Length < 6) return s;
            s.Pop = Clamp(stats[0]);
            s.Balance = Clamp(stats[1]);
            s.Speed = Clamp(stats[2]);
            s.Landing = Clamp(stats[3]);
            s.Courage = Clamp(stats[4]);
            s.Agility = Clamp(stats[5]);
            return s;
        }

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > 100) return 100;
            return v;
        }
    }

    /// <summary>순수 판정 결과. 화면/저장/대전이 모두 이 값을 쓴다.</summary>
    public struct Verdict
    {
        public bool Success;
        public Trick Trick;
        public int Quality;
        public int BaseScore;
        public int Score;
        public BailSeverity Severity;
        public FailReason Reason;
        public PopGrade Grade;
        public double AirTime;
        public double Pitch;
        public double RollCompletion;
        public bool IsGrind;
        public double GrindTime;
        public Stance Stance;
        public double PopOffset;
        public double InputAccuracy;
        public double SuccessProbability;
        public double SuccessRoll;
        public int AttemptId;

        public string NameKo
        {
            get { return TrickCatalog.NameKo(Trick, Stance); }
        }
    }

    public enum SimEventKind
    {
        Pop,
        Lip,
        GrindStart,
        GrindEnd,
        Land,
        Reset
    }

    public struct SimEvent
    {
        public SimEventKind Kind;
        public PopGrade Grade;
        public double Value;
        public GrindKind Grind;
        public Trick Trick;
        public Verdict Verdict;
    }

    /// <summary>판정에 필요한 순간값만 모은 것. 서버로 보낼 기록도 이 형태.</summary>
    public struct LandingSample
    {
        public double? PopOffset;
        public double? AttemptedOffset;
        public PopGrade Grade;
        public FlipKind Flip;
        public double Roll;
        public double BoardSpin;
        public double Yaw;
        public double YawTarget;
        public int Direction;
        public double Pitch;
        public double ImpactSpeed;
        public double AirTime;
        public bool InGap;
        public bool WeakPop;
        public GrindKind Grind;
        public double GrindTime;
        public double GrindBalance;
        public bool HitLedge;
        public bool GrindBailed;
        public Stance Stance;
        public NaturalStance NaturalStance;
        public double InputAccuracy;
        public double ObstacleRisk;
        public double Fatigue;
    }

    public struct FriendAttemptReport
    {
        public int RuleVersion;
        public int AttemptId;
        public int TurnNumber;
        public Trick Trick;
        public LandingSample Sample;
        public RiderStats Stats;
        public int Mastery;
    }

    public static class Labels
    {
        public static string Grade(PopGrade g)
        {
            switch (g)
            {
                case PopGrade.Perfect: return "PERFECT";
                case PopGrade.Good: return "GOOD";
                case PopGrade.Ok: return "OK";
                default: return "MISS";
            }
        }

        public static string GradeKo(PopGrade g)
        {
            switch (g)
            {
                case PopGrade.Perfect: return "완벽!";
                case PopGrade.Good: return "좋아요";
                case PopGrade.Ok: return "아쉬워요";
                default: return "놓쳤어요";
            }
        }

        public static string TrickKo(Trick t)
        {
            return TrickCatalog.NameKo(t, Stance.Regular);
        }

        public static string TrickKo(Trick t, Stance stance)
        {
            return TrickCatalog.NameKo(t, stance);
        }

        public static string InputHint(Trick t)
        {
            return TrickCatalog.InputHint(t);
        }

        public static string StanceKo(Stance s)
        {
            switch (s)
            {
                case Stance.Switch: return "스위치";
                case Stance.Fakie: return "페이키";
                case Stance.Nollie: return "널리";
                default: return "자연";
            }
        }

        public static string StanceShort(Stance s)
        {
            switch (s)
            {
                case Stance.Switch: return "SW";
                case Stance.Fakie: return "FK";
                case Stance.Nollie: return "NL";
                default: return "RG";
            }
        }

        public static string CategoryKo(TrickCategory c)
        {
            switch (c)
            {
                case TrickCategory.Flip: return "플립";
                case TrickCategory.Grind: return "그라인드";
                default: return "에어";
            }
        }

        public static string Reason(FailReason r)
        {
            switch (r)
            {
                case FailReason.FellInGap: return "갭에 떨어졌어요";
                case FailReason.MissedPop: return "팝을 못 했어요";
                case FailReason.LatePop: return "팝이 늦었어요";
                case FailReason.EarlyPop: return "팝이 너무 빨랐어요";
                case FailReason.IncompleteFlip: return "보드가 덜 돌았어요";
                case FailReason.BadPitch: return "보드가 기울었어요";
                case FailReason.OffAxis: return "몸이 덜 돌았어요";
                case FailReason.WeakPop: return "팝이 약했어요";
                case FailReason.SlippedGrind: return "그라인드에서 균형을 잃었어요";
                case FailReason.MissedGrind: return "렛지에 못 올라갔어요";
                case FailReason.HitLedge: return "렛지에 부딪혔어요";
                case FailReason.UnderRotated: return "회전이 부족했어요";
                case FailReason.BadLanding: return "착지가 불안정했어요";
                case FailReason.UnstableLanding: return "트릭은 맞았지만 착지가 불안정했어요";
                default: return "";
            }
        }

        public static string Coaching(FailReason r)
        {
            switch (r)
            {
                case FailReason.FellInGap: return "립에서 조금 더 세게, 조금 더 일찍 팝하세요";
                case FailReason.MissedPop: return "립에 닿을 때 왼손을 떼세요";
                case FailReason.LatePop: return "0.1초만 더 빨리 떼보세요";
                case FailReason.EarlyPop: return "크라우치를 조금 더 참아보세요";
                case FailReason.IncompleteFlip: return "플립을 조금 더 일찍 시작하세요";
                case FailReason.BadPitch: return "공중에서 왼손을 가운데로 되돌리세요";
                case FailReason.OffAxis: return "보드와 어깨를 같이 돌리세요";
                case FailReason.WeakPop: return "크라우치를 더 깊게 유지하세요";
                case FailReason.SlippedGrind: return "밸런스가 기우는 반대로 살짝 당기세요";
                case FailReason.MissedGrind: return "렛지 위에서 오른손을 탭하세요";
                case FailReason.HitLedge: return "렛지 앞에서 더 일찍 팝하세요";
                case FailReason.UnderRotated: return "기울기를 끝까지 유지하세요";
                case FailReason.BadLanding: return "착지 직전에 무게를 뒤로 빼세요";
                case FailReason.UnstableLanding: return "정확한 입력을 유지하고 착지 연습을 더 해보세요";
                default: return "";
            }
        }
    }
}
