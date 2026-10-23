using System;
using System.Diagnostics;

namespace Skater.Core
{
    public sealed class TrickSpec
    {
        public Trick Id;
        public string NameKo;
        public string NameEn;
        public string Hint;
        public string Desc;
        public TrickCategory Category;
        public int Tier;
        public int Points;

        public bool IsGrind
        {
            get { return Category == TrickCategory.Grind; }
        }
    }

    /// <summary>
    /// 입력 → 트릭 이름을 결정론적으로 만드는 문법 엔진이자 트릭 단일 소스.
    /// 이름/점수/티어를 바꾸려면 여기만 고치면 도감·판정·대전·결과가 모두 따라온다.
    /// </summary>
    public static class TrickCatalog
    {
        private static readonly GrindKind[] GrindRoster =
        {
            GrindKind.FiftyFifty, GrindKind.Boardslide, GrindKind.Nosegrind,
            GrindKind.FiveO, GrindKind.Crooked, GrindKind.Lipslide,
            GrindKind.Noseslide, GrindKind.Tailslide, GrindKind.Bluntslide,
            GrindKind.Nosebluntslide, GrindKind.Smith, GrindKind.Feeble,
            GrindKind.Overcrook, GrindKind.Darkslide, GrindKind.Hurricane,
            GrindKind.Salad, GrindKind.Suski, GrindKind.Willy
        };

        private static readonly TrickSpec[] Specs =
        {
            Spec(Trick.Ollie, "올리", "Ollie", "팝만", "크라우치를 참았다가 립에서 떼면 기본 점프.",
                TrickCategory.Air, 0, 100),
            Spec(Trick.Frontside180, "FS 180", "Frontside 180", "몸과 보드 FS 반 바퀴",
                "몸과 보드가 함께 FS 방향으로 180도 돌아 진행 방향을 바꾼다.",
                TrickCategory.Air, 1, 220),
            Spec(Trick.Backside180, "BS 180", "Backside 180", "몸과 보드 BS 반 바퀴",
                "몸과 보드가 함께 BS 방향으로 180도 돌아 진행 방향을 바꾼다.",
                TrickCategory.Air, 1, 220),
            Spec(Trick.Kickflip, "킥플립", "Kickflip", "팝 + 오른쪽 플릭",
                "앞발로 데크를 길이축 기준 한 바퀴 플립한다.",
                TrickCategory.Flip, 1, 300),
            Spec(Trick.Heelflip, "힐플립", "Heelflip", "팝 + 왼쪽 플릭",
                "앞발 뒤꿈치로 데크를 킥플립 반대 방향으로 한 바퀴 플립한다.",
                TrickCategory.Flip, 1, 300),
            Spec(Trick.Shuvit, "팝 셔빗", "Pop Shuvit", "보드 BS 180 셔빗",
                "보드는 기본 BS 방향으로 180도 수평 회전하고 몸은 따라 돌지 않는다.",
                TrickCategory.Flip, 1, 260),
            Spec(Trick.Impossible, "임파서블", "Impossible", "팝 + 위 플릭",
                "보드가 발을 감아 돌며 세로로 한 바퀴. 고난도.",
                TrickCategory.Flip, 2, 420),
            Spec(Trick.FiftyFifty, "50-50", "50-50 Grind", "렛지 위에서 탭",
                "양쪽 트럭으로 렛지를 타는 가장 기본 그라인드.",
                TrickCategory.Grind, 1, 280),
            Spec(Trick.BacksideFlip, "백사이드 플립", "Backside Flip", "킥플립 + BS 180",
                "킥플립과 BS 180을 동시에. 어깨를 먼저 돌리면 쉽다.",
                TrickCategory.Flip, 2, 460),
            Spec(Trick.Hardflip, "하드플립", "Hardflip", "킥플립 + FS 셔빗",
                "몸은 한 바퀴 돌지 않고, 보드는 FS 반 바퀴 셔빗과 킥플립을 한다.",
                TrickCategory.Flip, 2, 460),
            Spec(Trick.Frontside360, "FS 360", "Frontside 360", "몸과 보드 FS 한 바퀴",
                "몸과 보드가 함께 FS 방향으로 360도 회전한다.",
                TrickCategory.Air, 2, 520),
            Spec(Trick.Backside360, "BS 360", "Backside 360", "몸과 보드 BS 한 바퀴",
                "몸과 보드가 함께 BS 방향으로 360도 회전한다.",
                TrickCategory.Air, 2, 520),
            Spec(Trick.FrontsideFlip, "프론트사이드 플립", "Frontside Flip", "킥플립 + FS 180",
                "킥플립과 몸·보드의 FS 180을 함께 한다.",
                TrickCategory.Flip, 2, 480),
            Spec(Trick.Boardslide, "보드슬라이드", "Boardslide", "렛지 위에서 오른쪽 플릭",
                "보드를 90도 돌려 가운데로 렛지를 탄다. 밸런스가 관건.",
                TrickCategory.Grind, 2, 380),
            Spec(Trick.Nosegrind, "노즈그라인드", "Nosegrind", "렛지 위에서 왼쪽 플릭",
                "노즈 트럭 하나로 렛지를 탄다. 무게 중심이 앞에 있다.",
                TrickCategory.Grind, 2, 380),
            Spec(Trick.FiveO, "5-0", "5-0 Grind", "렛지 위에서 아래 플릭",
                "테일 트럭으로 낮게 렛지를 탄다. 안정적이고 빠르다.",
                TrickCategory.Grind, 2, 360),
            Spec(Trick.Crooked, "크룩드", "Crooked Grind", "렛지 위에서 위 플릭",
                "노즈를 비스듬히 걸치는 그라인드. 밸런스가 어렵다.",
                TrickCategory.Grind, 3, 420),
            Spec(Trick.InwardHeelflip, "인워드 힐플립", "Inward Heelflip", "힐플립 + BS 셔빗",
                "몸은 한 바퀴 돌지 않고, 보드는 BS 반 바퀴 셔빗과 힐플립을 한다.",
                TrickCategory.Flip, 3, 520),
            Spec(Trick.Frontside360Shuvit, "FS 360 셔빗", "Frontside 360 Shuvit", "보드 FS 360 셔빗",
                "보드만 FS 방향으로 360도 회전하고 몸은 한 바퀴 돌지 않는다.",
                TrickCategory.Flip, 3, 560),
            Spec(Trick.Backside360Shuvit, "BS 360 셔빗", "Backside 360 Shuvit", "보드 BS 360 셔빗",
                "보드만 BS 방향으로 360도 회전하고 몸은 한 바퀴 돌지 않는다.",
                TrickCategory.Flip, 3, 560),
            Spec(Trick.ImpossibleFrontside180, "임파서블 FS 180", "Impossible FS 180",
                "임파서블 + 왼쪽 기울기", "세로 회전과 FS 180을 동시에. 최고 난도.",
                TrickCategory.Flip, 3, 600),
            Spec(Trick.ImpossibleBackside180, "임파서블 BS 180", "Impossible BS 180",
                "임파서블 + 오른쪽 기울기", "세로 회전과 BS 180. 착지가 까다롭다.",
                TrickCategory.Flip, 3, 600),
            Spec(Trick.VarialKickflip, "바리알 킥플립", "Varial Kickflip", "↗ 오른쪽 위 대각선 플릭",
                "킥플립과 팝 셔빗을 섞어 보드가 반 바퀴 비틀리며 돈다.",
                TrickCategory.Flip, 2, 480),
            Spec(Trick.VarialHeelflip, "바리알 힐플립", "Varial Heelflip", "↖ 왼쪽 위 대각선 플릭",
                "힐플립에 반 바퀴 셔빗을 더해 보드가 반대 방향으로 감긴다.",
                TrickCategory.Flip, 2, 480),
            Spec(Trick.TreFlip, "트레플립", "Tre Flip (360 Flip)", "↘ 오른쪽 아래 대각선 플릭",
                "킥플립과 360도 셔빗을 동시에 돌리는 대표 기술.",
                TrickCategory.Flip, 3, 650),
            Spec(Trick.LaserFlip, "레이저 플립", "Laser Flip", "↙ 왼쪽 아래 대각선 플릭",
                "힐플립과 반대 방향 360도 셔빗을 결합한다.",
                TrickCategory.Flip, 3, 650),
            Spec(Trick.Lipslide, "립슬라이드", "Lipslide", "렛지 기술 선택 또는 ↘ 플릭",
                "뒤 트럭이 렛지를 넘어가며 보드 가운데를 가로로 미끄러뜨린다.",
                TrickCategory.Grind, 2, 400),
            Spec(Trick.Noseslide, "노즈슬라이드", "Noseslide", "렛지 기술 선택 또는 ↙ 플릭",
                "노즈를 렛지 위에 얹고 앞발에 무게를 실어 미끄러진다.",
                TrickCategory.Grind, 2, 380),
            Spec(Trick.Tailslide, "테일슬라이드", "Tailslide", "렛지 기술 선택",
                "테일을 렛지 위에 얹고 뒤쪽으로 무게 중심을 옮긴다.",
                TrickCategory.Grind, 2, 380),
            Spec(Trick.Bluntslide, "블런트슬라이드", "Bluntslide", "렛지 기술 선택",
                "테일과 뒤 바퀴를 렛지 위에 걸고 보드를 세워 미끄러진다.",
                TrickCategory.Grind, 3, 440),
            Spec(Trick.Nosebluntslide, "노즈블런트슬라이드", "Nosebluntslide", "렛지 기술 선택",
                "노즈와 앞 바퀴를 렛지 위에 걸어 반대 방향으로 세워 탄다.",
                TrickCategory.Grind, 3, 460),
            Spec(Trick.Smith, "스미스 그라인드", "Smith Grind", "렛지 기술 선택 또는 ↘ 플릭",
                "뒤 트럭을 레일에 두고 앞 트럭을 낮춰 비스듬히 탄다.",
                TrickCategory.Grind, 3, 440),
            Spec(Trick.Feeble, "피블 그라인드", "Feeble Grind", "렛지 기술 선택 또는 ↙ 플릭",
                "뒤 트럭으로 레일을 타며 앞쪽 보드를 레일 반대편으로 넘긴다.",
                TrickCategory.Grind, 3, 440),
            Spec(Trick.Overcrook, "오버크룩", "Overcrook Grind", "렛지 기술 선택",
                "앞 트럭을 크룩드보다 바깥으로 틀어 노즈 쪽에 체중을 싣는다.",
                TrickCategory.Grind, 3, 460),
            Spec(Trick.Darkslide, "다크슬라이드", "Darkslide", "렛지 기술 선택",
                "보드를 뒤집어 그립테이프 반대 면으로 미끄러진다.",
                TrickCategory.Grind, 3, 500),
            Spec(Trick.Hurricane, "허리케인", "Hurricane", "렛지 기술 선택",
                "몸을 반대로 돌려 보드를 가로로 놓고 슬라이드한다.",
                TrickCategory.Grind, 3, 480),
            Spec(Trick.Salad, "샐러드 그라인드", "Salad Grind", "뒤 트럭 · 노즈를 바깥쪽으로",
                "뒤 트럭으로 잠기고 노즈를 들어 장애물 바깥쪽으로 튼다. 명칭 관례는 지역마다 다르다.",
                TrickCategory.Grind, 3, 440),
            Spec(Trick.Suski, "서스키 그라인드", "Suski Grind", "뒤 트럭 · 노즈를 안쪽으로",
                "뒤 트럭으로 잠기고 노즈를 들어 장애물 안쪽으로 튼다. Salad와 잠김 방향을 구분한다.",
                TrickCategory.Grind, 3, 440),
            Spec(Trick.Willy, "윌리 그라인드", "Willy Grind", "렛지 기술 선택",
                "노즈 쪽 트럭에 체중을 싣고 보드를 비스듬히 걸친다.",
                TrickCategory.Grind, 3, 460)
        };

        static TrickCatalog()
        {
            Debug.Assert(Specs.Length == Enum.GetValues(typeof(Trick)).Length,
                "Every trick ID needs exactly one catalog entry.");
            for (int i = 0; i < Specs.Length; i++)
                Debug.Assert((int)Specs[i].Id == i, "Trick specs must follow stable save indices.");

            for (int i = 0; i < GrindRoster.Length; i++)
            {
                Trick trick = FromGrind(GrindRoster[i]);
                Debug.Assert(Get(trick).Category == TrickCategory.Grind, "Every grind kind needs a catalog entry.");
                for (int j = 0; j < i; j++)
                    Debug.Assert(FromGrind(GrindRoster[j]) != trick, "Grind kinds must map to unique trick IDs.");
            }
        }

        public static int Count
        {
            get { return Specs.Length; }
        }

        public static Trick[] All
        {
            get
            {
                Trick[] arr = new Trick[Specs.Length];
                for (int i = 0; i < Specs.Length; i++) arr[i] = Specs[i].Id;
                return arr;
            }
        }

        public static int GrindCount { get { return GrindRoster.Length; } }

        public static GrindKind GrindAt(int index)
        {
            if (index < 0 || index >= GrindRoster.Length) return GrindKind.FiftyFifty;
            return GrindRoster[index];
        }

        public static bool IsGrindKind(GrindKind kind)
        {
            for (int i = 0; i < GrindRoster.Length; i++)
                if (GrindRoster[i] == kind) return true;
            return false;
        }

        public static GrindKind NextGrind(GrindKind current)
        {
            for (int i = 0; i < GrindRoster.Length; i++)
                if (GrindRoster[i] == current) return GrindRoster[(i + 1) % GrindRoster.Length];
            return GrindRoster[0];
        }

        public static TrickSpec Get(Trick t)
        {
            int i = (int)t;
            if (i >= 0 && i < Specs.Length && Specs[i].Id == t) return Specs[i];
            for (int k = 0; k < Specs.Length; k++)
                if (Specs[k].Id == t) return Specs[k];
            return Specs[0];
        }

        public static int IndexOf(Trick t)
        {
            for (int i = 0; i < Specs.Length; i++)
                if (Specs[i].Id == t) return i;
            return 0;
        }

        public static string NameKo(Trick t, Stance stance)
        {
            TrickSpec s = Get(t);
            if (stance == Stance.Regular) return s.NameKo;
            return Labels.StanceKo(stance) + " " + s.NameKo;
        }

        public static string NameEn(Trick t, Stance stance)
        {
            TrickSpec s = Get(t);
            if (stance == Stance.Regular) return s.NameEn;
            string prefix;
            switch (stance)
            {
                case Stance.Switch: prefix = "Switch "; break;
                case Stance.Fakie: prefix = "Fakie "; break;
                case Stance.Nollie: prefix = "Nollie "; break;
                default: prefix = ""; break;
            }
            return prefix + s.NameEn;
        }

        public static string InputHint(Trick t)
        {
            return Get(t).Hint;
        }

        public static double StanceScoreMultiplier(Stance s)
        {
            switch (s)
            {
                case Stance.Switch: return 1.10;
                case Stance.Fakie: return 1.10;
                case Stance.Nollie: return 1.15;
                default: return 1.0;
            }
        }

        public static bool IsDefaultObserved(Trick t)
        {
            return t == Trick.Ollie || t == Trick.Frontside180 || t == Trick.Backside180 ||
                   t == Trick.Kickflip;
        }

        public static FlipKind FlipFromSwipe(double dx, double dy)
        {
            double ax = Math.Abs(dx);
            double ay = Math.Abs(dy);
            if (ax == 0.0 && ay == 0.0) return FlipKind.None;
            if (Math.Min(ax, ay) >= Math.Max(ax, ay) * 0.45)
            {
                if (dx > 0.0) return dy > 0.0 ? FlipKind.VarialKickflip : FlipKind.TreFlip;
                return dy > 0.0 ? FlipKind.VarialHeelflip : FlipKind.LaserFlip;
            }
            if (ax >= ay) return dx > 0.0 ? FlipKind.Kickflip : FlipKind.Heelflip;
            return dy > 0.0 ? FlipKind.Impossible : FlipKind.Shuvit;
        }

        /// <summary>
        /// 플립 + 바디 회전 + 그라인드 → 트릭 ID.
        /// 플립과 조합할 때 바디 회전은 최대 180도다(입력 과부하 방지).
        /// </summary>
        public static Trick FromInput(FlipKind flip, double yawTarget, GrindKind grind)
        {
            if (grind != GrindKind.None)
                return FromGrind(grind);

            switch (flip)
            {
                case FlipKind.VarialKickflip: return Trick.VarialKickflip;
                case FlipKind.VarialHeelflip: return Trick.VarialHeelflip;
                case FlipKind.TreFlip: return Trick.TreFlip;
                case FlipKind.LaserFlip: return Trick.LaserFlip;
            }

            Spin spin = yawTarget > 0.5 ? Spin.Backside : (yawTarget < -0.5 ? Spin.Frontside : Spin.None);
            double abs = Math.Abs(yawTarget);

            if (flip == FlipKind.None)
            {
                if (abs >= 300.0)
                    return spin == Spin.Backside ? Trick.Backside360 : Trick.Frontside360;
                if (abs >= 90.0)
                    return spin == Spin.Backside ? Trick.Backside180 : Trick.Frontside180;
                return Trick.Ollie;
            }

            // 플립 조합은 180도까지만 이름이 존재한다.
            bool spun = abs >= 90.0;
            if (flip == FlipKind.Kickflip)
            {
                if (!spun) return Trick.Kickflip;
                return spin == Spin.Backside ? Trick.BacksideFlip : Trick.Hardflip;
            }
            if (flip == FlipKind.Heelflip)
            {
                if (!spun) return Trick.Heelflip;
                return spin == Spin.Backside ? Trick.InwardHeelflip : Trick.FrontsideFlip;
            }
            if (flip == FlipKind.Shuvit)
            {
                if (!spun) return Trick.Shuvit;
                return spin == Spin.Backside ? Trick.Backside360Shuvit : Trick.Frontside360Shuvit;
            }
            if (flip == FlipKind.Impossible)
            {
                if (!spun) return Trick.Impossible;
                return spin == Spin.Backside ? Trick.ImpossibleBackside180 : Trick.ImpossibleFrontside180;
            }
            return Trick.Ollie;
        }

        public static Trick FromGrind(GrindKind grind)
        {
            switch (grind)
            {
                case GrindKind.FiftyFifty: return Trick.FiftyFifty;
                case GrindKind.Boardslide: return Trick.Boardslide;
                case GrindKind.Nosegrind: return Trick.Nosegrind;
                case GrindKind.FiveO: return Trick.FiveO;
                case GrindKind.Crooked: return Trick.Crooked;
                case GrindKind.Lipslide: return Trick.Lipslide;
                case GrindKind.Noseslide: return Trick.Noseslide;
                case GrindKind.Tailslide: return Trick.Tailslide;
                case GrindKind.Bluntslide: return Trick.Bluntslide;
                case GrindKind.Nosebluntslide: return Trick.Nosebluntslide;
                case GrindKind.Smith: return Trick.Smith;
                case GrindKind.Feeble: return Trick.Feeble;
                case GrindKind.Overcrook: return Trick.Overcrook;
                case GrindKind.Darkslide: return Trick.Darkslide;
                case GrindKind.Hurricane: return Trick.Hurricane;
                case GrindKind.Salad: return Trick.Salad;
                case GrindKind.Suski: return Trick.Suski;
                case GrindKind.Willy: return Trick.Willy;
                default: return Trick.Ollie;
            }
        }

        /// <summary>Reverse of the live input grammar for the shared motion preview.</summary>
        public static bool TryGetInput(Trick trick, out TrickInput input)
        {
            input = new TrickInput();
            switch (trick)
            {
                case Trick.Ollie: input = new TrickInput(FlipKind.None); break;
                case Trick.Frontside180: input = new TrickInput(FlipKind.None, -180); break;
                case Trick.Backside180: input = new TrickInput(FlipKind.None, 180); break;
                case Trick.Kickflip: input = new TrickInput(FlipKind.Kickflip); break;
                case Trick.Heelflip: input = new TrickInput(FlipKind.Heelflip); break;
                case Trick.Shuvit: input = new TrickInput(FlipKind.Shuvit); break;
                case Trick.Impossible: input = new TrickInput(FlipKind.Impossible); break;
                case Trick.FiftyFifty: input = new TrickInput(FlipKind.None, 0, GrindKind.FiftyFifty); break;
                case Trick.BacksideFlip: input = new TrickInput(FlipKind.Kickflip, 180); break;
                case Trick.Hardflip: input = new TrickInput(FlipKind.Kickflip, -180); break;
                case Trick.Frontside360: input = new TrickInput(FlipKind.None, -360); break;
                case Trick.Backside360: input = new TrickInput(FlipKind.None, 360); break;
                case Trick.FrontsideFlip: input = new TrickInput(FlipKind.Heelflip, -180); break;
                case Trick.Boardslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Boardslide); break;
                case Trick.Nosegrind: input = new TrickInput(FlipKind.None, 0, GrindKind.Nosegrind); break;
                case Trick.FiveO: input = new TrickInput(FlipKind.None, 0, GrindKind.FiveO); break;
                case Trick.Crooked: input = new TrickInput(FlipKind.None, 0, GrindKind.Crooked); break;
                case Trick.InwardHeelflip: input = new TrickInput(FlipKind.Heelflip, 180); break;
                case Trick.Frontside360Shuvit: input = new TrickInput(FlipKind.Shuvit, -360); break;
                case Trick.Backside360Shuvit: input = new TrickInput(FlipKind.Shuvit, 360); break;
                case Trick.ImpossibleFrontside180: input = new TrickInput(FlipKind.Impossible, -180); break;
                case Trick.ImpossibleBackside180: input = new TrickInput(FlipKind.Impossible, 180); break;
                case Trick.VarialKickflip: input = new TrickInput(FlipKind.VarialKickflip); break;
                case Trick.VarialHeelflip: input = new TrickInput(FlipKind.VarialHeelflip); break;
                case Trick.TreFlip: input = new TrickInput(FlipKind.TreFlip); break;
                case Trick.LaserFlip: input = new TrickInput(FlipKind.LaserFlip); break;
                case Trick.Lipslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Lipslide); break;
                case Trick.Noseslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Noseslide); break;
                case Trick.Tailslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Tailslide); break;
                case Trick.Bluntslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Bluntslide); break;
                case Trick.Nosebluntslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Nosebluntslide); break;
                case Trick.Smith: input = new TrickInput(FlipKind.None, 0, GrindKind.Smith); break;
                case Trick.Feeble: input = new TrickInput(FlipKind.None, 0, GrindKind.Feeble); break;
                case Trick.Overcrook: input = new TrickInput(FlipKind.None, 0, GrindKind.Overcrook); break;
                case Trick.Darkslide: input = new TrickInput(FlipKind.None, 0, GrindKind.Darkslide); break;
                case Trick.Hurricane: input = new TrickInput(FlipKind.None, 0, GrindKind.Hurricane); break;
                case Trick.Salad: input = new TrickInput(FlipKind.None, 0, GrindKind.Salad); break;
                case Trick.Suski: input = new TrickInput(FlipKind.None, 0, GrindKind.Suski); break;
                case Trick.Willy: input = new TrickInput(FlipKind.None, 0, GrindKind.Willy); break;
                default: return false;
            }
            return FromInput(input.Flip, input.YawTarget, input.Grind) == trick;
        }

        private static TrickSpec Spec(Trick id, string ko, string en, string hint, string desc,
            TrickCategory cat, int tier, int points)
        {
            TrickSpec s = new TrickSpec();
            s.Id = id;
            s.NameKo = ko;
            s.NameEn = en;
            s.Hint = hint;
            s.Desc = desc;
            s.Category = cat;
            s.Tier = tier;
            s.Points = points;
            return s;
        }
    }

    /// <summary>시드 고정 xorshift. 판정에는 쓰지 않고 봇/미션 롤에만 쓴다.</summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(int seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : (uint)seed;
            if (_state == 0) _state = 1u;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int Next(int max)
        {
            if (max <= 0) return 0;
            return (int)(NextUInt() % (uint)max);
        }

        public double NextDouble()
        {
            return (NextUInt() & 0xFFFFFF) / 16777216.0;
        }
    }
}
