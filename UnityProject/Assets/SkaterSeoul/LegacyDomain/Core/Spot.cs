using System;

namespace Skater.Core
{
    public enum SpotKind
    {
        Gap,
        Drop,
        Ledge,
        Quarter,
        Flatground,
        Street
    }

    /// <summary>
    /// 스팟 지형. x는 미터, 높이는 미터. HeightAt은 지면이 없는 구간(갭)에서 null.
    /// 렌더링은 이 정의를 읽어 전경을 조립한다.
    /// </summary>
    public sealed class SpotDef
    {
        public string Id;
        public string NameKo;
        public string NameEn;
        public string Desc;
        public SpotKind Kind;

        public double RampStartX;
        public double LipX;
        public double LipHeight;
        public double PopX;
        public double LandingX;
        public double LandingHeight;

        public double GrindX0;
        public double GrindX1;
        public double GrindStartHeight;
        public double GrindEndHeight;

        public double RunOutX = 13.0;
        public int StairCount = 5;
        public double WorldOffset;

        public SpotDef AtOffset(double x)
        {
            SpotDef copy = new SpotDef();
            copy.Id = Id;
            copy.NameKo = NameKo;
            copy.NameEn = NameEn;
            copy.Desc = Desc;
            copy.Kind = Kind;
            copy.RampStartX = RampStartX + x;
            copy.LipX = LipX + x;
            copy.LipHeight = LipHeight;
            copy.PopX = PopX + x;
            copy.LandingX = LandingX + x;
            copy.LandingHeight = LandingHeight;
            copy.GrindX0 = GrindX0 + x;
            copy.GrindX1 = GrindX1 + x;
            copy.GrindStartHeight = GrindStartHeight;
            copy.GrindEndHeight = GrindEndHeight;
            copy.RunOutX = RunOutX + x;
            copy.StairCount = StairCount;
            copy.WorldOffset = WorldOffset + x;
            return copy;
        }

        public double GapWidth
        {
            get { return LandingX - LipX; }
        }

        public bool HasGap
        {
            get { return Kind == SpotKind.Gap || Kind == SpotKind.Quarter; }
        }

        public bool HasGrind
        {
            get { return Kind == SpotKind.Ledge || Kind == SpotKind.Street; }
        }

        public double? HeightAt(double x)
        {
            if (Kind == SpotKind.Flatground) return 0.0;
            if (Kind == SpotKind.Ledge)
            {
                if (x >= GrindX0 && x <= GrindX1) return GrindHeightAt(x);
                return 0.0;
            }

            if (x < RampStartX) return 0.0;
            if (x < LipX) return RampHeight(x);
            if ((Kind == SpotKind.Drop || Kind == SpotKind.Street) && x < LandingX)
            {
                int step = (int)((x - LipX) / GapWidth * StairCount);
                return StairTopAt(step);
            }
            if (x < LandingX) return null;
            return LandingHeight;
        }

        public double StairTopAt(int step)
        {
            int count = Math.Max(1, StairCount);
            if (step < 0) step = 0;
            if (step >= count) step = count - 1;
            double baseY = Math.Min(0.0, LandingHeight);
            return LipHeight + (baseY - LipHeight) * (step + 1) / count;
        }

        public bool InGap(double x)
        {
            return HasGap && x >= LipX && x < LandingX;
        }

        public bool OnLedge(double x)
        {
            return Kind == SpotKind.Ledge && x >= GrindX0 && x <= GrindX1;
        }

        public bool NearLedge(double x, double margin)
        {
            return HasGrind && x >= GrindX0 - margin && x <= GrindX1 + margin;
        }

        public double GrindHeightAt(double x)
        {
            double t = (x - GrindX0) / (GrindX1 - GrindX0);
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;
            return GrindStartHeight + (GrindEndHeight - GrindStartHeight) * t;
        }

        public double RampHeight(double x)
        {
            double t = (x - RampStartX) / (LipX - RampStartX);
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;
            if (Kind == SpotKind.Quarter) t = t * t * (3.0 - 2.0 * t);
            return LipHeight * t;
        }

        public double SlopeAt(double x)
        {
            if (Kind == SpotKind.Ledge || Kind == SpotKind.Flatground) return 0.0;
            if (x >= RampStartX && x < LipX)
            {
                double d = 0.01;
                double a = x - d < RampStartX ? RampStartX : x - d;
                double b = x + d > LipX ? LipX : x + d;
                return (RampHeight(b) - RampHeight(a)) / (b - a);
            }
            return 0.0;
        }

        public double LipTime(Tuning t)
        {
            return t.ReadyDuration + (PopX - t.StartX) / t.ApproachSpeed;
        }

        /// <summary>팝 없이 립을 넘었을 때의 발사 속도. 실패를 자연스럽게 만든다.</summary>
        public double LaunchVy(Tuning t)
        {
            return t.ApproachSpeed * SlopeAt(LipX - 0.001);
        }

        public string KindKo
        {
            get
            {
                switch (Kind)
                {
                    case SpotKind.Drop: return "낙차";
                    case SpotKind.Ledge: return "렛지";
                    case SpotKind.Quarter: return "쿼터";
                    case SpotKind.Flatground: return "플랫";
                    case SpotKind.Street: return "스트릿";
                    default: return "갭";
                }
            }
        }
    }

    public sealed class SkateparkDef
    {
        public readonly double Length;
        public readonly SpotDef Ground;
        public readonly SpotDef[] Features;

        public SkateparkDef(double length, SpotDef ground, SpotDef[] features)
        {
            Length = length;
            Ground = ground;
            Features = features;
        }

        public SpotDef SpotAt(double x)
        {
            for (int i = 0; i < Features.Length; i++)
                if (x >= Features[i].WorldOffset && x <= Features[i].RunOutX)
                    return Features[i];
            return Ground;
        }

        public SpotDef Find(string id)
        {
            for (int i = 0; i < Features.Length; i++)
                if (Features[i].Id == id) return Features[i];
            return null;
        }

        public double StartX(SpotDef feature)
        {
            return Math.Max(feature.WorldOffset, feature.RampStartX - 2.0);
        }
    }

    public static class Spots
    {
        private static readonly SpotDef[] Defs = Build();
        private static SkateparkDef _park;

        public static SkateparkDef Park
        {
            get
            {
                if (_park == null)
                    _park = new SkateparkDef(112.0, Get("flatground").AtOffset(0.0), new[]
                    {
                        Get("drop").AtOffset(8.0),
                        Get("ledge").AtOffset(27.0),
                        Get("street").AtOffset(47.0),
                        Get("gap").AtOffset(67.0),
                        Get("quarter").AtOffset(88.0)
                    });
                return _park;
            }
        }

        public static SpotDef[] All
        {
            get { return Defs; }
        }

        public static SpotDef Get(string id)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Id == id) return Defs[i];
            return Defs[0];
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Id == id) return i;
            return 0;
        }

        public static SpotDef Gap
        {
            get { return Defs[0]; }
        }

        private static SpotDef[] Build()
        {
            SpotDef gap = new SpotDef();
            gap.Id = "gap";
            gap.NameKo = "강변 갭";
            gap.NameEn = "Riverside Gap";
            gap.Desc = "램프에서 3.6m 갭을 넘는 기본 스팟. 에어와 플립을 익히기 좋다.";
            gap.Kind = SpotKind.Gap;
            gap.RampStartX = 3.5;
            gap.LipX = 5.0;
            gap.LipHeight = 0.45;
            gap.PopX = 5.0;
            gap.LandingX = 8.6;
            gap.LandingHeight = 0.0;
            gap.RunOutX = 13.2;

            SpotDef drop = new SpotDef();
            drop.Id = "drop";
            drop.NameKo = "시청 계단";
            drop.NameEn = "City Hall Drop";
            drop.Desc = "평탄한 진입로에서 1.2m 계단을 타고 내려가는 스팟. 착지와 임팩트 관리가 핵심.";
            drop.Kind = SpotKind.Drop;
            drop.RampStartX = 0.0;
            drop.LipX = 4.8;
            drop.LipHeight = 0.0;
            drop.PopX = 4.8;
            drop.LandingX = 7.4;
            drop.LandingHeight = -1.2;
            drop.RunOutX = 13.0;

            SpotDef ledge = new SpotDef();
            ledge.Id = "ledge";
            ledge.NameKo = "홍대 렛지";
            ledge.NameEn = "Hongdae Ledge";
            ledge.Desc = "4.1m 콘크리트 렛지. 여러 슬라이드와 트럭별 그라인드를 연습한다.";
            ledge.Kind = SpotKind.Ledge;
            ledge.RampStartX = 3.0;
            ledge.LipX = 5.2;
            ledge.LipHeight = 0.0;
            ledge.PopX = 3.75;
            ledge.LandingX = 9.3;
            ledge.LandingHeight = 0.0;
            ledge.GrindX0 = 5.2;
            ledge.GrindX1 = 9.3;
            ledge.GrindStartHeight = 0.62;
            ledge.GrindEndHeight = 0.62;
            ledge.RunOutX = 13.4;

            SpotDef quarter = new SpotDef();
            quarter.Id = "quarter";
            quarter.NameKo = "한강 쿼터";
            quarter.NameEn = "Hangang Quarter";
            quarter.Desc = "1.55m 대형 램프. 긴 체공으로 회전과 플립을 노린다.";
            quarter.Kind = SpotKind.Quarter;
            quarter.RampStartX = 2.8;
            quarter.LipX = 4.8;
            quarter.LipHeight = 1.55;
            quarter.PopX = 4.8;
            quarter.LandingX = 9.2;
            quarter.LandingHeight = 0.0;
            quarter.RunOutX = 13.6;

            SpotDef flatground = new SpotDef();
            flatground.Id = "flatground";
            flatground.NameKo = "플랫 연습장";
            flatground.NameEn = "Flatground Court";
            flatground.Desc = "평탄한 코트에서 올리, 플립, 셔빗의 팝과 착지를 연습한다.";
            flatground.Kind = SpotKind.Flatground;
            flatground.RampStartX = 0.0;
            flatground.LipX = 5.2;
            flatground.PopX = 5.2;
            flatground.LandingX = 12.8;
            flatground.LandingHeight = 0.0;
            flatground.RunOutX = 13.6;

            SpotDef street = new SpotDef();
            street.Id = "street";
            street.NameKo = "시청 스트릿";
            street.NameEn = "City Hall Street Set";
            street.Desc = "계단을 타고 내려가거나 다운 레일 위에 그라인드를 건다.";
            street.Kind = SpotKind.Street;
            street.RampStartX = 0.0;
            street.LipX = 4.9;
            street.LipHeight = 0.0;
            street.PopX = 4.72;
            street.LandingX = 7.9;
            street.LandingHeight = -0.9;
            street.GrindX0 = 4.65;
            street.GrindX1 = 8.15;
            street.GrindStartHeight = 0.58;
            street.GrindEndHeight = -0.82;
            street.RunOutX = 13.6;

            System.Diagnostics.Debug.Assert(street.HeightAt(street.LipX).HasValue);
            System.Diagnostics.Debug.Assert(Math.Abs(street.HeightAt(street.LandingX - 0.001).Value - street.StairTopAt(street.StairCount - 1)) < 0.001);
            System.Diagnostics.Debug.Assert(!street.InGap(street.LipX));
            System.Diagnostics.Debug.Assert(drop.HeightAt(drop.LipX).HasValue);
            System.Diagnostics.Debug.Assert(Math.Abs(drop.HeightAt(drop.LandingX - 0.001).Value - drop.LandingHeight) < 0.001);
            System.Diagnostics.Debug.Assert(!drop.InGap(drop.LipX));

            return new[] { gap, drop, ledge, quarter, flatground, street };
        }
    }
}
