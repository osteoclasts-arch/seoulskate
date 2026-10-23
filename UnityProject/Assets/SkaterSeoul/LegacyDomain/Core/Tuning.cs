using System;

namespace Skater.Core
{
    /// <summary>
    /// 밸런스 수치. 전부 여기서 조정한다. 판정 윈도우는 성장으로 넓히지 않는다.
    /// </summary>
    public sealed class Tuning
    {
        public int TickHz = 60;

        // 흐름
        public double ReadyDuration = 0.55;
        public double ResultHold = 1.6;

        // 월드
        public double Gravity = 16.5;
        public double StartX = -2.0;
        public double ApproachSpeed = 7.0;
        public double ImpactSevere = 8.2;
        public double ImpactMedium = 6.2;

        // 팝 판정 (초)
        public double PopPerfect = 0.035;
        public double PopGood = 0.075;
        public double PopOk = 0.120;
        public double BasePopImpulse = 4.65;
        public double MinCrouchTime = 0.10;
        public double CrouchFillTime = 0.32;

        // 회전
        public double FlipDuration = 0.30;
        public double BoardSpinDuration = 0.26;
        public double Yaw180Duration = 0.30;
        public double Yaw360Duration = 0.50;
        public double YawTolerance = 22.0;
        public double PitchRate = 110.0;
        public double PitchToleranceBase = 20.0;
        public double PitchToleranceBalance = 0.10;
        public double PitchToleranceLanding = 0.05;
        public double AirPitchCorrect = 38.0;
        public double AirPitchLean = 46.0;

        // 그라인드
        public double GrindSpeedFactor = 0.86;
        public double GrindExitImpulse = 3.4;
        public double GrindBalanceLimit = 1.0;
        public double GrindDrift = 0.62;
        public double GrindLockRange = 0.95;
        public double GrindLockExpire = 0.55;
        public double GrindMinTime = 0.20;

        // Initial success model hypothesis; values are normalized to 0..1 at evaluation.
        public double SuccessBase = 0.54;
        public double SuccessMasteryWeight = 0.27;
        public double SuccessStatWeight = 0.19;
        public double SuccessDifficultyRelief = 0.18;
        public double SuccessAccuracyPenalty = 0.30;
        public double SuccessObstaclePenalty = 0.12;
        public double SuccessFatiguePenalty = 0.12;
        public double SuccessMin = 0.01;
        public double SuccessMax = 0.995;

        public double Dt { get { return 1.0 / TickHz; } }
    }
}
