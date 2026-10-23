using System;
using System.Diagnostics;

namespace Skater.Core
{
    public struct TrickMotionFrame
    {
        public double BoardX;
        public double BoardY;
        public double BoardTilt;
        public double RiderY;
        public double RiderTilt;
        public double BodyYaw;
        public double BoardYaw;
        public double BoardRoll;
        public double BoardPitch;
        public TrickContactMode ContactMode;
        public TrickContactPoint ContactPoint;
        public TrickObstacleSide ObstacleSide;
        public TrickEntryCrossing EntryCrossing;
        public TrickFootFlick FootFlick;
        public bool Scoops;
        public double ContactOffsetX;
        public double ContactOffsetY;
        public bool BoardInverted;
        public double FootContactWeight;
    }

    public enum TrickMotionPhase
    {
        Riding, Setup, Crouch, Pop, Ascent, Flick, Apex, Catch, Descent, Landing, Recovery, Bail,
        GrindApproach, GrindEntry, Grind, GrindExit
    }

    public struct OllieMotionFrame
    {
        public double BoardY;
        public double BoardTilt;
        public double TailPivotWeight;
        public double FootContactWeight;
        public TrickMotionPhase Phase;
    }

    public static class OllieMotion
    {
        static OllieMotion()
        {
            Debug.Assert(ContactAt(0.0) == 1.0 && ContactAt(0.5) == 0.0 && ContactAt(1.0) == 1.0,
                "Foot contact must release at the apex and return for the catch.");
            Debug.Assert(PhaseAt(0.0) == TrickMotionPhase.Pop && PhaseAt(0.5) == TrickMotionPhase.Apex &&
                PhaseAt(0.64) == TrickMotionPhase.Catch && PhaseAt(0.88) == TrickMotionPhase.Descent &&
                PhaseAt(1.0) == TrickMotionPhase.Landing, "Air phases must align with the catch window.");
        }

        private struct Key
        {
            public double Time, BoardY, BoardTilt, TailPivotWeight;

            public Key(double time, double boardY, double boardTilt, double tailPivotWeight)
            {
                Time = time;
                BoardY = boardY;
                BoardTilt = boardTilt;
                TailPivotWeight = tailPivotWeight;
            }
        }

        // Board-local +X is the nose; positive Z rotation lifts it in the side view.
        private static readonly Key[] Keys =
        {
            new Key(0.00, 0.000,  0.0, 1.00),
            new Key(0.12, 0.015, 18.0, 0.78),
            new Key(0.28, 0.040, 11.0, 0.18),
            new Key(0.48, 0.055,  0.0, 0.00),
            new Key(0.70, 0.040,  0.0, 0.00),
            new Key(0.88, 0.012,  0.0, 0.00),
            new Key(1.00, 0.000,  0.0, 0.00)
        };

        public static OllieMotionFrame Sample(double progress)
        {
            progress = Math.Max(0.0, Math.Min(1.0, progress));
            if (progress <= 0.0) return From(Keys[0]);
            if (progress >= 1.0) return From(Keys[Keys.Length - 1]);

            for (int i = 1; i < Keys.Length; i++)
            {
                Key next = Keys[i];
                if (progress > next.Time) continue;
                Key previous = Keys[i - 1];
                double t = (progress - previous.Time) / (next.Time - previous.Time);
                t = t * t * (3.0 - 2.0 * t);
                OllieMotionFrame frame = new OllieMotionFrame();
                frame.BoardY = Lerp(previous.BoardY, next.BoardY, t);
                frame.BoardTilt = Lerp(previous.BoardTilt, next.BoardTilt, t);
                frame.TailPivotWeight = Lerp(previous.TailPivotWeight, next.TailPivotWeight, t);
                frame.FootContactWeight = ContactAt(progress);
                frame.Phase = PhaseAt(progress);
                return frame;
            }

            return From(Keys[Keys.Length - 1]);
        }

        public static double FlightDuration(double y, double verticalSpeed, double elapsed, double groundY, double gravity)
        {
            double g = Math.Max(0.1, gravity);
            double height = Math.Max(0.0, y - groundY);
            double remaining = (verticalSpeed + Math.Sqrt(verticalSpeed * verticalSpeed + 2.0 * g * height)) / g;
            return Math.Max(0.08, elapsed + Math.Max(0.0, remaining));
        }

        private static OllieMotionFrame From(Key key)
        {
            OllieMotionFrame frame = new OllieMotionFrame();
            frame.BoardY = key.BoardY;
            frame.BoardTilt = key.BoardTilt;
            frame.TailPivotWeight = key.TailPivotWeight;
            frame.FootContactWeight = ContactAt(key.Time);
            frame.Phase = PhaseAt(key.Time);
            return frame;
        }

        private static double ContactAt(double progress)
        {
            if (progress <= 0.16) return 1.0;
            if (progress < 0.42) return Smooth(1.0 - (progress - 0.16) / 0.26);
            if (progress <= 0.64) return 0.0;
            if (progress < 0.88) return Smooth((progress - 0.64) / 0.24);
            return 1.0;
        }

        private static TrickMotionPhase PhaseAt(double progress)
        {
            if (progress < 0.06) return TrickMotionPhase.Pop;
            if (progress < 0.22) return TrickMotionPhase.Ascent;
            if (progress < 0.42) return TrickMotionPhase.Flick;
            if (progress < 0.60) return TrickMotionPhase.Apex;
            if (progress < 0.72) return TrickMotionPhase.Catch;
            if (progress < 0.90) return TrickMotionPhase.Descent;
            return TrickMotionPhase.Landing;
        }

        private static double Smooth(double value)
        {
            double t = Math.Max(0.0, Math.Min(1.0, value));
            return t * t * (3.0 - 2.0 * t);
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }
    }

    public enum TrickMotionPath { Ollie, Flip, Shuvit, ImpossibleWrap, Combination, ThreeSixtyFlip, BodySpin, Grind, Slide }
    public enum TrickContactMode { None, Grind, Slide }
    public enum TrickContactPoint { None, BothTrucks, FrontTruck, RearTruck, DeckCenter, Nose, Tail, InvertedDeck }
    public enum TrickObstacleSide { None, Near, Far }
    public enum TrickEntryCrossing { None, FrontTruck, RearTruck }
    public enum TrickFootFlick { None, Kickflip, Heelflip, ImpossibleWrap }
    public enum TrickPopEnd { Tail, Nose }
    public enum SkateFoot { Left, Right }

    public struct TrickMotionDefinition
    {
        public Trick Id;
        public TrickMotionPath Path;
        public double FlipTarget;
        // BoardYawTarget is the world-facing total; BoardSpinTarget is its offset from body yaw.
        public double BoardYawTarget;
        public double BodyYawTarget;
        public double FlipX, FlipY, FlipPitch;
        public double SpinX, SpinY, SpinPitch;
        public double RiderY, RiderTilt, RiderSpinTilt;
        public double GrindX, GrindPitch, GrindRiderY, GrindRiderTilt, GrindFrequency;
        // Transient rotation from the approach angle to the locked-in board yaw.
        public double EntryYaw, ContactOffsetX, ContactOffsetY;
        public TrickContactMode ContactMode;
        public TrickContactPoint ContactPoint;
        public TrickObstacleSide ObstacleSide;
        public TrickEntryCrossing EntryCrossing;
        public TrickFootFlick FootFlick;
        public bool BoardInverted;
        public bool Scoops;
        public bool WrapsFoot;
    }

    /// <summary>One physical definition drives simulation targets and the board/rider projection.</summary>
    public static class TrickMotion
    {
        // Board-local +Y points up: truck axles sit below the deck and slide contacts use its underside.
        public const double TruckAxleDrop = 0.145;
        public const double DeckSurfaceOffset = 0.04;

        private static readonly TrickMotionDefinition[] Definitions =
        {
            Air(Trick.Ollie, TrickMotionPath.Ollie),
            Air(Trick.Frontside180, TrickMotionPath.BodySpin, boardYaw: -180, body: -180, x: -0.035, riderTilt: -5),
            Air(Trick.Backside180, TrickMotionPath.BodySpin, boardYaw: 180, body: 180, x: 0.035, riderTilt: 5),
            Air(Trick.Kickflip, TrickMotionPath.Flip, flip: 360, x: 0.08, y: 0.10, pitch: 12, riderY: 0.04),
            Air(Trick.Heelflip, TrickMotionPath.Flip, flip: -360, x: -0.08, y: 0.08, pitch: -12, riderY: 0.02, riderTilt: -4),
            Air(Trick.Shuvit, TrickMotionPath.Shuvit, boardYaw: 180, x: 0.09, spinY: 0.025, riderSpinTilt: 3),
            Air(Trick.Impossible, TrickMotionPath.ImpossibleWrap, flip: 360, x: 0.12, y: 0.13, pitch: 20, riderY: 0.05, wrapsFoot: true),
            Grind(Trick.FiftyFifty, TrickContactMode.Grind, TrickContactPoint.BothTrucks, frequency: 4.0, riderTilt: 2),
            Air(Trick.BacksideFlip, TrickMotionPath.Flip, flip: 360, boardYaw: 180, body: 180, x: 0.11, y: 0.05, pitch: 16, riderTilt: 7),
            Air(Trick.Hardflip, TrickMotionPath.Combination, flip: 360, boardYaw: -180, x: -0.11, y: 0.07, pitch: -18, spinX: -0.08, riderTilt: -7),
            Air(Trick.Frontside360, TrickMotionPath.BodySpin, boardYaw: -360, body: -360, x: -0.06, y: 0.035, riderTilt: -8),
            Air(Trick.Backside360, TrickMotionPath.BodySpin, boardYaw: 360, body: 360, x: 0.06, y: 0.035, riderTilt: 8),
            Air(Trick.FrontsideFlip, TrickMotionPath.Flip, flip: 360, boardYaw: -180, body: -180, x: -0.10, y: 0.06, pitch: -15, riderY: 0.03),
            Grind(Trick.Boardslide, TrickContactMode.Slide, TrickContactPoint.DeckCenter, pitch: 90,
                boardYaw: 90, entryYaw: -90,
                frequency: 4.5, riderTilt: 5, entryCrossing: TrickEntryCrossing.FrontTruck),
            Grind(Trick.Nosegrind, TrickContactMode.Grind, TrickContactPoint.FrontTruck, x: -0.035, pitch: -18, riderY: 0.02, riderTilt: -4, frequency: 4.0),
            Grind(Trick.FiveO, TrickContactMode.Grind, TrickContactPoint.RearTruck, x: 0.035, pitch: 16, riderY: 0.015, riderTilt: 4, frequency: 4.2),
            Grind(Trick.Crooked, TrickContactMode.Grind, TrickContactPoint.FrontTruck, x: -0.05,
                pitch: -32, boardYaw: 35, entryYaw: -35, riderY: 0.025, riderTilt: -8, frequency: 4.8),
            Air(Trick.InwardHeelflip, TrickMotionPath.Combination, flip: -360, boardYaw: 180, x: 0.10, y: 0.11, pitch: 20, spinX: 0.08, riderTilt: 5),
            Air(Trick.Frontside360Shuvit, TrickMotionPath.Shuvit, boardYaw: -360, x: -0.10, spinY: 0.035, spinPitch: -8, riderSpinTilt: -5),
            Air(Trick.Backside360Shuvit, TrickMotionPath.Shuvit, boardYaw: 360, x: 0.10, spinY: 0.045, spinPitch: 10, riderSpinTilt: 6),
            Air(Trick.ImpossibleFrontside180, TrickMotionPath.ImpossibleWrap, flip: 360, boardYaw: -180, body: -180, x: -0.13, y: 0.12, pitch: -22, riderTilt: -9, wrapsFoot: true),
            Air(Trick.ImpossibleBackside180, TrickMotionPath.ImpossibleWrap, flip: 360, boardYaw: 180, body: 180, x: 0.13, y: 0.15, pitch: 24, riderTilt: 9, wrapsFoot: true),
            Air(Trick.VarialKickflip, TrickMotionPath.Combination, flip: 360, boardYaw: 180, x: 0.10, y: 0.09, pitch: 15, spinX: 0.08, riderSpinTilt: 2),
            Air(Trick.VarialHeelflip, TrickMotionPath.Combination, flip: -360, boardYaw: -180, x: -0.10, y: 0.07, pitch: -16, spinX: -0.08, riderSpinTilt: -2),
            Air(Trick.TreFlip, TrickMotionPath.ThreeSixtyFlip, flip: 360, boardYaw: 360, x: 0.11, y: 0.13, spinY: 0.05, pitch: 18, riderY: 0.04),
            Air(Trick.LaserFlip, TrickMotionPath.ThreeSixtyFlip, flip: -360, boardYaw: -360, x: -0.11, y: 0.11, spinY: 0.04, pitch: -20, riderY: 0.03),
            Grind(Trick.Lipslide, TrickContactMode.Slide, TrickContactPoint.DeckCenter, pitch: -90,
                boardYaw: -90, entryYaw: 90,
                riderY: 0.02, riderTilt: -8, frequency: 4.4,
                entryCrossing: TrickEntryCrossing.RearTruck),
            Grind(Trick.Noseslide, TrickContactMode.Slide, TrickContactPoint.Nose, x: -0.065, pitch: -15, riderY: 0.022, riderTilt: -6, frequency: 4.1),
            Grind(Trick.Tailslide, TrickContactMode.Slide, TrickContactPoint.Tail, x: 0.065, pitch: 15, riderY: 0.02, riderTilt: 7, frequency: 4.3),
            Grind(Trick.Bluntslide, TrickContactMode.Slide, TrickContactPoint.Tail, x: 0.07, pitch: 72, riderY: -0.025, riderTilt: 9, frequency: 4.8),
            Grind(Trick.Nosebluntslide, TrickContactMode.Slide, TrickContactPoint.Nose, x: -0.07, pitch: -72, riderY: -0.025, riderTilt: -9, frequency: 4.6),
            Grind(Trick.Smith, TrickContactMode.Grind, TrickContactPoint.RearTruck, x: 0.055, pitch: -24,
                riderY: 0.02, riderTilt: 8, frequency: 4.0, obstacleSide: TrickObstacleSide.Near),
            Grind(Trick.Feeble, TrickContactMode.Grind, TrickContactPoint.RearTruck, x: -0.055, pitch: 24,
                riderY: 0.02, riderTilt: -7, frequency: 4.2, obstacleSide: TrickObstacleSide.Far),
            Grind(Trick.Overcrook, TrickContactMode.Grind, TrickContactPoint.FrontTruck, x: -0.07,
                pitch: -42, boardYaw: -35, entryYaw: 35, riderY: 0.025, riderTilt: -10, frequency: 4.5,
                obstacleSide: TrickObstacleSide.Far),
            Grind(Trick.Darkslide, TrickContactMode.Slide, TrickContactPoint.InvertedDeck, riderY: -0.018, riderTilt: 4, frequency: 4.1, inverted: true),
            Grind(Trick.Hurricane, TrickContactMode.Grind, TrickContactPoint.RearTruck, x: 0.025, pitch: 88,
                boardYaw: 180, entryYaw: -180, riderY: 0.02, riderTilt: 12, frequency: 4.4),
            Grind(Trick.Salad, TrickContactMode.Grind, TrickContactPoint.RearTruck, x: 0.055, pitch: 26,
                riderY: 0.02, riderTilt: 7, frequency: 4.0, obstacleSide: TrickObstacleSide.Far),
            Grind(Trick.Suski, TrickContactMode.Grind, TrickContactPoint.RearTruck, x: 0.04, pitch: 21,
                riderY: 0.022, riderTilt: -12, frequency: 4.3, obstacleSide: TrickObstacleSide.Near),
            Grind(Trick.Willy, TrickContactMode.Grind, TrickContactPoint.FrontTruck, x: -0.075,
                pitch: 38, riderY: 0.024, riderTilt: -8, frequency: 4.6, obstacleSide: TrickObstacleSide.Near)
        };

        static TrickMotion()
        {
            Debug.Assert(Definitions.Length == TrickCatalog.Count, "Every trick needs a physical motion definition.");
            for (int i = 0; i < Definitions.Length; i++)
                Debug.Assert((int)Definitions[i].Id == i, "Motion definitions must follow stable trick IDs.");
        }

        public static int Count { get { return Definitions.Length; } }

        public static TrickMotionDefinition Definition(Trick trick)
        {
            int i = (int)trick;
            return i >= 0 && i < Definitions.Length && Definitions[i].Id == trick
                ? Definitions[i] : Definitions[0];
        }

        public static double RollTarget(Trick trick) { return Definition(trick).FlipTarget; }

        public static double RollTarget(Trick trick, NaturalStance natural, Stance mode)
        {
            return Definition(trick).FlipTarget;
        }

        public static double BoardYawTarget(Trick trick) { return Definition(trick).BoardYawTarget; }

        public static double BoardYawTarget(Trick trick, NaturalStance natural, Stance mode, int direction = 1)
        {
            return Definition(trick).BoardYawTarget * HeadingSign(natural, mode, direction);
        }

        public static double BoardSpinTarget(Trick trick)
        {
            TrickMotionDefinition d = Definition(trick);
            return d.BoardYawTarget - d.BodyYawTarget;
        }

        public static double BoardSpinTarget(Trick trick, NaturalStance natural, Stance mode, int direction = 1)
        {
            return BoardYawTarget(trick, natural, mode, direction) -
                BodyYawTarget(trick, natural, mode, direction);
        }

        public static double BodyYawTarget(Trick trick)
        {
            return Definition(trick).BodyYawTarget;
        }

        public static double BodyYawTarget(Trick trick, NaturalStance natural, Stance mode, int direction = 1)
        {
            return Definition(trick).BodyYawTarget * HeadingSign(natural, mode, direction);
        }

        public static TrickPopEnd PopEnd(Stance mode)
        {
            return mode == Stance.Nollie ? TrickPopEnd.Nose : TrickPopEnd.Tail;
        }

        public static SkateFoot LeadFoot(NaturalStance natural, Stance mode)
        {
            bool left = natural == NaturalStance.Regular;
            if (mode == Stance.Switch) left = !left;
            return left ? SkateFoot.Left : SkateFoot.Right;
        }

        public static TrickMotionFrame Sample(Trick trick, double roll, double spin, double yaw, double time = 0.0)
        {
            return Sample(trick, roll, spin, yaw, time, NaturalStance.Regular, Stance.Regular, 1);
        }

        public static TrickMotionFrame Sample(Trick trick, double roll, double spin, double yaw, double time,
            NaturalStance natural, Stance mode, int direction, bool grindExit = false,
            double grindExitTime = 0.0, bool grindApproach = false)
        {
            TrickMotionDefinition d = Definition(trick);
            double flipTarget = RollTarget(trick, natural, mode);
            double spinTarget = BoardSpinTarget(trick, natural, mode, direction);
            double boardYawTarget = BoardYawTarget(trick, natural, mode, direction);
            double entryYaw = d.EntryYaw * HeadingSign(natural, mode, direction);
            double r = flipTarget == 0.0 ? 0.0 : Clamp01(roll / flipTarget);
            double s = spinTarget == 0.0 ? 0.0 : Clamp01(spin / spinTarget);
            double entryBlend = grindApproach ? Smooth01(time / 0.12) : 1.0;
            double exitBlend = grindExit ? Smooth01(grindExitTime / 0.12) : 0.0;
            double grindPoseWeight = 1.0 - exitBlend;
            double flipArc = Math.Sin(Math.PI * r);
            double spinArc = Math.Sin(Math.PI * s);
            double approachYaw = Math.Abs(yaw + spin) > 0.01 ? yaw + spin : boardYawTarget + entryYaw;
            double grindBoardYaw = grindExit ? boardYawTarget * (1.0 - exitBlend) :
                (grindApproach ? approachYaw + (boardYawTarget - approachYaw) * entryBlend : boardYawTarget);
            TrickMotionFrame f = new TrickMotionFrame
            {
                BodyYaw = yaw,
                BoardYaw = d.ContactMode == TrickContactMode.None ? yaw + spin : grindBoardYaw,
                BoardRoll = roll,
                ContactMode = grindExit || grindApproach ? TrickContactMode.None : d.ContactMode,
                ContactPoint = grindExit || grindApproach ? TrickContactPoint.None : d.ContactPoint,
                ObstacleSide = d.ObstacleSide,
                EntryCrossing = d.EntryCrossing,
                FootFlick = d.FootFlick,
                Scoops = d.Scoops,
                ContactOffsetX = d.ContactOffsetX,
                ContactOffsetY = d.ContactOffsetY,
                BoardInverted = d.BoardInverted,
                FootContactWeight = ContactWeight(d.Path, flipArc, spinArc)
            };

            switch (d.Path)
            {
                case TrickMotionPath.Flip:
                    f.BoardX = d.FlipX * flipArc;
                    f.BoardY = d.FlipY * flipArc;
                    f.BoardTilt = d.FlipPitch * flipArc;
                    f.RiderY = d.RiderY * flipArc;
                    f.RiderTilt = d.RiderTilt * flipArc;
                    break;
                case TrickMotionPath.Shuvit:
                    f.BoardX = d.FlipX * spinArc;
                    f.BoardY = d.SpinY * Math.Sin(2.0 * Math.PI * s);
                    f.BoardTilt = d.SpinPitch * spinArc;
                    f.RiderTilt = d.RiderSpinTilt * spinArc;
                    break;
                case TrickMotionPath.ImpossibleWrap:
                    f.BoardX = d.FlipX * Math.Sin(2.0 * Math.PI * r);
                    f.BoardY = d.FlipY * flipArc;
                    f.BoardTilt = d.FlipPitch * Math.Sin(2.0 * Math.PI * r);
                    f.RiderY = d.RiderY * flipArc;
                    f.RiderTilt = d.RiderTilt * flipArc;
                    break;
                case TrickMotionPath.Combination:
                    f.BoardX = d.FlipX * flipArc + d.SpinX * spinArc;
                    f.BoardY = d.FlipY * flipArc + d.SpinY * spinArc;
                    f.BoardTilt = d.FlipPitch * flipArc + d.SpinPitch * spinArc;
                    f.RiderY = d.RiderY * flipArc;
                    f.RiderTilt = d.RiderTilt * flipArc + d.RiderSpinTilt * spinArc;
                    break;
                case TrickMotionPath.ThreeSixtyFlip:
                    f.BoardX = d.FlipX * Math.Sin(2.0 * Math.PI * s);
                    f.BoardY = d.FlipY * flipArc + d.SpinY * spinArc;
                    f.BoardTilt = d.FlipPitch * flipArc;
                    f.RiderY = d.RiderY * flipArc;
                    break;
                case TrickMotionPath.BodySpin:
                    double body = Math.Sin(yaw * Math.PI / 180.0);
                    f.BoardX = d.FlipX * body;
                    f.BoardY = d.FlipY * Math.Abs(body);
                    f.RiderTilt = d.RiderTilt * Math.Abs(body);
                    break;
                case TrickMotionPath.Grind:
                case TrickMotionPath.Slide:
                    double crossingOffset = d.EntryCrossing == TrickEntryCrossing.FrontTruck ? 0.16 :
                        (d.EntryCrossing == TrickEntryCrossing.RearTruck ? -0.16 : 0.0);
                    crossingOffset *= HeadingSign(natural, mode, direction);
                    double approachWeight = grindApproach ? 1.0 - entryBlend : 0.0;
                    f.BoardX = (d.GrindX + crossingOffset * approachWeight) * grindPoseWeight;
                    // The board rises for entry, then its selected contact point stays on the obstacle.
                    f.BoardY = 0.04 * approachWeight * grindPoseWeight;
                    f.BoardTilt = d.GrindPitch * grindPoseWeight;
                    f.RiderY = (d.GrindRiderY + 0.02 * Math.Sin(time * d.GrindFrequency)) * grindPoseWeight;
                    f.RiderTilt = (d.GrindRiderTilt + 3.0 * Math.Sin(time * d.GrindFrequency * 0.62)) * grindPoseWeight;
                    break;
            }
            f.BoardPitch = f.BoardTilt;
            return f;
        }

        private static TrickMotionDefinition Air(Trick id, TrickMotionPath path, double flip = 0, double boardYaw = 0,
            double body = 0, double x = 0, double y = 0, double pitch = 0, double riderY = 0,
            double riderTilt = 0, double spinX = 0, double spinY = 0, double spinPitch = 0,
            double riderSpinTilt = 0, bool wrapsFoot = false)
        {
            return new TrickMotionDefinition
            {
                Id = id, Path = path, FlipTarget = flip, BoardYawTarget = boardYaw, BodyYawTarget = body,
                FlipX = x, FlipY = y, FlipPitch = pitch, SpinX = spinX, SpinY = spinY, SpinPitch = spinPitch,
                RiderY = riderY, RiderTilt = riderTilt, RiderSpinTilt = riderSpinTilt, WrapsFoot = wrapsFoot,
                FootFlick = wrapsFoot ? TrickFootFlick.ImpossibleWrap :
                    (flip > 0.0 ? TrickFootFlick.Kickflip : (flip < 0.0 ? TrickFootFlick.Heelflip : TrickFootFlick.None)),
                Scoops = boardYaw != body
            };
        }

        private static TrickMotionDefinition Grind(Trick id, TrickContactMode mode, TrickContactPoint point,
            double x = 0, double pitch = 0, double riderY = 0, double riderTilt = 0,
            double frequency = 4.0, double boardYaw = 0, double entryYaw = 0, bool inverted = false,
            TrickObstacleSide obstacleSide = TrickObstacleSide.None,
            TrickEntryCrossing entryCrossing = TrickEntryCrossing.None)
        {
            double contactX = 0, contactY = -DeckSurfaceOffset;
            switch (point)
            {
                case TrickContactPoint.BothTrucks: contactY = -TruckAxleDrop; break;
                case TrickContactPoint.FrontTruck: contactX = 0.305; contactY = -TruckAxleDrop; break;
                case TrickContactPoint.RearTruck: contactX = -0.305; contactY = -TruckAxleDrop; break;
                case TrickContactPoint.Nose: contactX = 0.675; break;
                case TrickContactPoint.Tail: contactX = -0.675; break;
                case TrickContactPoint.InvertedDeck: contactY = DeckSurfaceOffset; inverted = true; break;
            }
            return new TrickMotionDefinition
            {
                Id = id, Path = mode == TrickContactMode.Slide ? TrickMotionPath.Slide : TrickMotionPath.Grind,
                BoardYawTarget = boardYaw,
                ContactMode = mode, ContactPoint = point, GrindX = x, GrindPitch = pitch,
                GrindRiderY = riderY, GrindRiderTilt = riderTilt, GrindFrequency = frequency,
                EntryYaw = entryYaw, ContactOffsetX = contactX, ContactOffsetY = contactY, BoardInverted = inverted,
                ObstacleSide = obstacleSide, EntryCrossing = entryCrossing
            };
        }

        private static double HeadingSign(NaturalStance natural, Stance mode, int direction)
        {
            double sign = direction < 0 ? -1.0 : 1.0;
            if (natural == NaturalStance.Goofy) sign = -sign;
            if (mode == Stance.Fakie) sign = -sign;
            return sign;
        }

        private static double ContactWeight(TrickMotionPath path, double flipArc, double spinArc)
        {
            switch (path)
            {
                case TrickMotionPath.Ollie:
                case TrickMotionPath.BodySpin:
                case TrickMotionPath.Grind:
                case TrickMotionPath.Slide:
                    return 1.0;
                case TrickMotionPath.Shuvit:
                    return 1.0 - spinArc;
                case TrickMotionPath.Flip:
                case TrickMotionPath.ImpossibleWrap:
                case TrickMotionPath.ThreeSixtyFlip:
                    return 1.0 - flipArc;
                default:
                    return 1.0 - Math.Max(flipArc, spinArc);
            }
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        private static double Smooth01(double value)
        {
            double t = Clamp01(value);
            return t * t * (3.0 - 2.0 * t);
        }
    }
}
