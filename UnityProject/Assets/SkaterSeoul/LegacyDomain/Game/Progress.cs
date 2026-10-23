using System;
using System.Collections.Generic;
using UnityEngine;
using Skater.Core;

namespace Skater.Game
{
    [Serializable]
    public sealed class SettingsData
    {
        public const float DefaultMusicVolume = 0.22f;
        public int controlMode = 1;
        public bool sound = true;
        public float musicVolume = DefaultMusicVolume;
        public bool haptics = true;
        public int inputOffsetMs = 0;
        public bool screenShake = true;
    }

    [Serializable]
    public sealed class TotalsData
    {
        public int attempts;
        public int success;
        public int bails;
        public int bestScore;
        public int bestAirMs;
        public int bestGrindMs;
        public int bestStreak;
        public int streak;
        public int careerStars;
        public int matches;
    }

    [Serializable]
    public sealed class ProgressData
    {
        public int version = 13;
        public int level = 1;
        public int xp;
        public int boardingUsed;
        public int condition = 100;
        public int stance = -1;
        public bool goofy;
        public int[] stats = { 10, 10, 10, 10, 10, 10 };
        public int[] mastery = new int[TrickCatalog.Count];
        public int[] observed = new int[TrickCatalog.Count];
        public int[] learned = new int[TrickCatalog.Count];
        public int[] trickAttempts = new int[TrickCatalog.Count];
        public int wins;
        public int losses;
        public int rating = 1000;
        public ChallengeRecord[] career = new ChallengeRecord[12];
        public MissionRecord[] missions = new MissionRecord[3];
        public string day = "";
        public string missionDay = "";
        public int loginStreak;
        public bool tutorialDone;
        public bool tutorialSkipped;
        public int tutorialStage;
        public int tutorialFailures;
        public bool careerDone;
        public int freeSpot = 4;
        public int freeTrick;
        public int freeStance;
        public SettingsData settings = new SettingsData();
        public TotalsData totals = new TotalsData();
    }

    [Serializable]
    internal sealed class ProgressDataV1
    {
        public int level = 1;
        public int xp;
        public int boardingUsed;
        public int condition = 100;
        public int stance = -1;
        public int[] stats = new int[6];
        public int[] mastery = new int[6];
        public int wins;
        public int losses;
        public int rating = 1000;
        public int missionTrick = 1;
        public int missionTarget = 3;
        public int missionProgress;
        public string day = "";
    }

    public struct PracticeResult
    {
        public int MasteryBefore;
        public int MasteryAfter;
        public int XpGain;
        public int LevelUps;
        public bool NewlyObserved;
        public bool MissionComplete;
        public string MissionTitle;
        public int MissionXp;
    }

    public struct CareerOutcome
    {
        public bool Registered;
        public bool NewlyCompleted;
        public bool StarUp;
        public int Stars;
        public int XpGain;
        public Trick[] Unlocked;
    }

    /// <summary>
    /// 성장/자원/커리어/미션/설정을 로컬에 저장한다. (PlayerPrefs + JsonUtility)
    /// 구버전 저장은 진행을 보존하며 v4 스팟, v5 그라인드, v7 레슨, v8 튜토리얼, v9 마스크, v10 실패 보호, v11 음악 음량, v12 스탠스 분리, v13 튜토리얼 재설계를 반영한다.
    /// </summary>
    public sealed class Progress
    {
        public const int MaxBoarding = 60;
        public const int MaxStat = 100;
        public const int XpPerLevel = 120;

        private const string Key = "skater_progress_v3";
        private const string KeyV2 = "skater_progress_v2";
        private const string KeyV1 = "skater_progress_v1";

        public static readonly string[] StatNames =
        {
            "알리 높이", "밸런스", "속도", "착지", "용감함", "민첩성"
        };

        public ProgressData Data;

        public int PendingLevelUp;
        public int PendingMissionXp;
        public string PendingMissionTitle = "";

        public static Progress Load()
        {
            Progress p = new Progress();
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) json = PlayerPrefs.GetString(KeyV2, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { p.Data = JsonUtility.FromJson<ProgressData>(json); }
                catch (Exception) { p.Data = null; }
            }

            if (p.Data == null)
            {
                string old = PlayerPrefs.GetString(KeyV1, "");
                if (!string.IsNullOrEmpty(old))
                {
                    try
                    {
                        ProgressDataV1 v1 = JsonUtility.FromJson<ProgressDataV1>(old);
                        if (v1 != null) p.Data = Migrate(v1);
                    }
                    catch (Exception) { p.Data = null; }
                }
            }

            if (p.Data == null) p.Data = new ProgressData();
            bool addedFlipTricks = p.Data.version < 3;
            bool migrateDefaultSpot = p.Data.version < 4 && p.Data.freeSpot == 0;
            bool addedSlideGrinds = p.Data.version < 6;
            bool addedLearnedState = p.Data.version < 7;
            bool addedTutorialStages = p.Data.version < 8;
            bool widenedTrickMasks = p.Data.version < 9;
            bool addedMusicVolume = p.Data.version < 11;
            bool separatedGoofyFromSwitch = p.Data.version < 12 && p.Data.stance == 1 &&
                p.Data.freeStance == 1 && !p.Data.tutorialDone;
            bool redesignedTutorial = p.Data.version < 13;
            p.Normalize();
            if (addedFlipTricks)
            {
                Trick[] added = { Trick.VarialKickflip, Trick.VarialHeelflip, Trick.TreFlip, Trick.LaserFlip };
                for (int i = 0; i < added.Length; i++) p.Data.observed[TrickCatalog.IndexOf(added[i])] = 1;
                p.Data.version = 3;
            }
            if (migrateDefaultSpot) p.Data.freeSpot = Spots.IndexOf("flatground");
            if (addedSlideGrinds) p.MigrateSlideGrindUnlocks();
            if (addedLearnedState)
            {
                for (int i = 0; i < TrickCatalog.Count; i++)
                    if (p.Data.observed[i] > 0) p.Data.learned[i] = 1;
                p.MigrateZeroStats();
            }
            if (addedTutorialStages) p.Data.tutorialStage = p.Data.tutorialDone ? 2 : 1;
            if (widenedTrickMasks) p.MigrateLegacyTrickMasks();
            if (addedMusicVolume) p.Data.settings.musicVolume = SettingsData.DefaultMusicVolume;
            // The old onboarding reused Goofy's value for Switch; Goofy now starts in natural footing.
            if (separatedGoofyFromSwitch) p.Data.freeStance = 0;
            if (redesignedTutorial && !p.Data.tutorialDone) p.Data.tutorialStage = 0;
            p.Data.version = 13;
            if (!p.IsLearned((Trick)p.Data.freeTrick)) p.Data.freeTrick = p.FirstLearnedIndex();
            p.CheckDay();
            p.RecalcCareerTotals();
            p.RefreshMissions();
            p.ClaimCompletedMissions();
            p.Save();
            return p;
        }

        private static ProgressData Migrate(ProgressDataV1 v1)
        {
            ProgressData d = new ProgressData();
            d.version = 2;
            d.level = v1.level;
            d.xp = v1.xp;
            d.boardingUsed = v1.boardingUsed;
            d.condition = v1.condition;
            d.stance = v1.stance;
            d.goofy = v1.stance == 1;
            d.wins = v1.wins;
            d.losses = v1.losses;
            d.rating = v1.rating;
            d.day = "";
            d.tutorialDone = false;

            if (v1.stats != null)
                for (int i = 0; i < 6 && i < v1.stats.Length; i++) d.stats[i] = v1.stats[i];

            // v1 카탈로그: Ollie, Kickflip, Heelflip, FS180, BS180, BacksideFlip
            Trick[] oldOrder =
            {
                Trick.Ollie, Trick.Kickflip, Trick.Heelflip,
                Trick.Frontside180, Trick.Backside180, Trick.BacksideFlip
            };
            if (v1.mastery != null)
            {
                for (int i = 0; i < oldOrder.Length && i < v1.mastery.Length; i++)
                {
                    int idx = TrickCatalog.IndexOf(oldOrder[i]);
                    if (idx >= 0) d.mastery[idx] = v1.mastery[i];
                    if (v1.mastery[i] > 0) d.observed[idx] = 1;
                }
            }
            return d;
        }

        private void Normalize()
        {
            if (Data.stats == null || Data.stats.Length != 6) Data.stats = new[] { 10, 10, 10, 10, 10, 10 };
            if (Data.mastery == null || Data.mastery.Length != TrickCatalog.Count)
            {
                int[] m = new int[TrickCatalog.Count];
                if (Data.mastery != null)
                    for (int i = 0; i < m.Length && i < Data.mastery.Length; i++) m[i] = Data.mastery[i];
                Data.mastery = m;
            }
            if (Data.observed == null || Data.observed.Length != TrickCatalog.Count)
            {
                int[] o = new int[TrickCatalog.Count];
                if (Data.observed != null)
                    for (int i = 0; i < o.Length && i < Data.observed.Length; i++) o[i] = Data.observed[i];
                Data.observed = o;
            }
            if (Data.learned == null || Data.learned.Length != TrickCatalog.Count)
            {
                int[] learned = new int[TrickCatalog.Count];
                if (Data.learned != null)
                    for (int i = 0; i < learned.Length && i < Data.learned.Length; i++) learned[i] = Data.learned[i];
                Data.learned = learned;
            }
            if (Data.trickAttempts == null || Data.trickAttempts.Length != TrickCatalog.Count)
            {
                int[] attempts = new int[TrickCatalog.Count];
                if (Data.trickAttempts != null)
                    for (int i = 0; i < attempts.Length && i < Data.trickAttempts.Length; i++) attempts[i] = Data.trickAttempts[i];
                Data.trickAttempts = attempts;
            }
            if (Data.career == null || Data.career.Length != Career.Count)
            {
                ChallengeRecord[] c = new ChallengeRecord[Career.Count];
                if (Data.career != null)
                    for (int i = 0; i < c.Length && i < Data.career.Length; i++) c[i] = Data.career[i];
                Data.career = c;
            }
            for (int i = 0; i < Data.career.Length; i++)
                if (Data.career[i] == null) Data.career[i] = new ChallengeRecord();

            if (Data.missions == null || Data.missions.Length != 3)
            {
                MissionRecord[] arr = new MissionRecord[3];
                if (Data.missions != null)
                    for (int i = 0; i < 3 && i < Data.missions.Length; i++) arr[i] = Data.missions[i];
                Data.missions = arr;
            }
            if (Data.settings == null) Data.settings = new SettingsData();
            if (Data.totals == null) Data.totals = new TotalsData();
            Data.settings.musicVolume = Mathf.Clamp01(Data.settings.musicVolume);

            if (Data.level < 1) Data.level = 1;
            if (Data.condition < 0) Data.condition = 0;
            if (Data.condition > 100) Data.condition = 100;
            if (Data.rating < 0) Data.rating = 0;
            if (Data.stance < -1 || Data.stance > 1) Data.stance = -1;
            if (Data.stance == 1) Data.goofy = true;
            if (Data.tutorialStage < 0 || Data.tutorialStage > 4) Data.tutorialStage = Data.tutorialDone ? 4 : 0;
            if (Data.tutorialFailures < 0) Data.tutorialFailures = 0;
            if (Data.freeTrick < 0 || Data.freeTrick >= TrickCatalog.Count) Data.freeTrick = 0;
            if (Data.freeSpot < 0 || Data.freeSpot >= Spots.All.Length) Data.freeSpot = 0;
            if (Data.freeStance < 0 || Data.freeStance > 3) Data.freeStance = 0;

            for (int i = 0; i < TrickCatalog.Count; i++)
            {
                if (Data.mastery[i] < 0) Data.mastery[i] = 0;
                if (Data.mastery[i] > 100) Data.mastery[i] = 100;
                if (Data.observed[i] < 0 || Data.observed[i] > 1) Data.observed[i] = Data.observed[i] > 0 ? 1 : 0;
                if (Data.learned[i] < 0 || Data.learned[i] > 1) Data.learned[i] = Data.learned[i] > 0 ? 1 : 0;
                if (Data.trickAttempts[i] < 0) Data.trickAttempts[i] = 0;
                if (Data.trickAttempts[i] == 0 && Data.mastery[i] > 0)
                    Data.trickAttempts[i] = Math.Max(1, Data.mastery[i] / Math.Max(1, 3 + TrickCatalog.Get((Trick)i).Tier));
                if (TrickCatalog.IsDefaultObserved((Trick)i)) Data.observed[i] = 1;
                if (Data.learned[i] > 0) Data.observed[i] = 1;
            }
            if (Data.version >= 7) Data.learned[TrickCatalog.IndexOf(Trick.Ollie)] = 1;
            if (Data.version >= 7 && !IsLearned((Trick)Data.freeTrick)) Data.freeTrick = FirstLearnedIndex();
        }

        private void MigrateZeroStats()
        {
            for (int i = 0; i < Data.stats.Length; i++)
                if (Data.stats[i] == 0) Data.stats[i] = 10;
        }

        private void MigrateSlideGrindUnlocks()
        {
            ChallengeSpec[] ledgeChallenges = Career.ForSpot("ledge");
            for (int i = 0; i < ledgeChallenges.Length; i++)
            {
                ChallengeRecord record = RecordFor(ledgeChallenges[i]);
                if (record == null || record.stars < 1 || ledgeChallenges[i].Unlocks == null) continue;
                for (int j = 0; j < ledgeChallenges[i].Unlocks.Length; j++)
                    Observe(ledgeChallenges[i].Unlocks[j]);
            }
        }

        private void MigrateLegacyTrickMasks()
        {
            for (int i = 0; i < Data.career.Length; i++)
                if (Data.career[i] != null && Data.career[i].trickMask < 0)
                    Data.career[i].trickMask = unchecked((uint)Data.career[i].trickMask);
            for (int i = 0; i < Data.missions.Length; i++)
                if (Data.missions[i] != null && Data.missions[i].trickMask < 0)
                    Data.missions[i].trickMask = unchecked((uint)Data.missions[i].trickMask);
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public bool CheckDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            DateTime previousDate;
            if (DateTime.TryParseExact(Data.day, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out previousDate) && DateTime.Today <= previousDate.Date)
                return false;

            // shortcut: local date can be changed by the player, use trusted server time before adding daily rewards.
            if (Data.day != today)
            {
                string previous = Data.day;
                Data.day = today;
                Data.boardingUsed = 0;
                Data.condition = Math.Min(100, Data.condition + 30);

                if (!string.IsNullOrEmpty(previous))
                {
                    DateTime prev;
                    if (DateTime.TryParseExact(previous, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out prev))
                    {
                        int gap = (DateTime.Today - prev.Date).Days;
                        Data.loginStreak = gap == 1 ? Data.loginStreak + 1 : 1;
                    }
                    else Data.loginStreak = 1;
                }
                else Data.loginStreak = 1;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------ state

        public int Level
        {
            get { return 1 + Data.xp / XpPerLevel; }
        }

        public int XpIntoLevel
        {
            get { return Data.xp % XpPerLevel; }
        }

        public float LevelProgress
        {
            get { return XpIntoLevel / (float)XpPerLevel; }
        }

        public bool CanMatch
        {
            get { return Data.boardingUsed + 2 <= MaxBoarding && Data.condition > 0; }
        }

        public bool LowBoarding
        {
            get { return BoardingLeft <= 10; }
        }

        public int BoardingLeft
        {
            get { return Math.Max(0, MaxBoarding - Data.boardingUsed); }
        }

        public RiderStats Stats
        {
            get { return RiderStats.FromArray(Data.stats); }
        }

        public int[] Mastery
        {
            get { return Data.mastery; }
        }

        public int MasteryOf(Trick t)
        {
            return Data.mastery[TrickCatalog.IndexOf(t)];
        }

        public int MasteryOf(int index)
        {
            if (index < 0 || index >= Data.mastery.Length) return 0;
            return Data.mastery[index];
        }

        public bool IsObserved(Trick t)
        {
            return Data.observed[TrickCatalog.IndexOf(t)] > 0;
        }

        public bool IsLearned(Trick t)
        {
            int idx = TrickCatalog.IndexOf(t);
            return idx >= 0 && idx < Data.learned.Length && Data.learned[idx] > 0;
        }

        public bool IsMastered(Trick t)
        {
            int idx = TrickCatalog.IndexOf(t);
            return IsLearned(t) && Data.mastery[idx] >= 60;
        }

        public string TrickState(Trick t)
        {
            if (!IsObserved(t)) return "미관찰";
            if (IsMastered(t)) return "숙련";
            if (IsLearned(t)) return "학습";
            return "관찰";
        }

        public int ObservedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Data.observed.Length; i++) if (Data.observed[i] > 0) n++;
                return n;
            }
        }

        public bool Observe(Trick t, bool save = false)
        {
            int idx = TrickCatalog.IndexOf(t);
            if (Data.observed[idx] > 0) return false;
            Data.observed[idx] = 1;
            RegisterMissionObservation();
            if (save) Save();
            return true;
        }

        public bool LearnTrick(Trick t)
        {
            int idx = TrickCatalog.IndexOf(t);
            if (idx < 0 || idx >= Data.learned.Length || Data.observed[idx] <= 0 || Data.learned[idx] > 0)
                return false;
            Data.learned[idx] = 1;
            Save();
            return true;
        }

        public void SetStance(int stance)
        {
            Data.stance = stance;
            Data.goofy = stance == 1;
            Save();
        }

        public void SetFree(int spot, Trick trick, int stance)
        {
            Data.freeSpot = Mathf.Clamp(spot, 0, Spots.All.Length - 1);
            int trickIndex = TrickCatalog.IndexOf(trick);
            Data.freeTrick = trickIndex >= 0 && IsLearned(trick) ? trickIndex : FirstLearnedIndex();
            Data.freeStance = Mathf.Clamp(stance, 0, 3);
            Save();
        }

        // ------------------------------------------------------------ practice

        public PracticeResult AddPractice(Verdict v, bool freeLesson = false, bool lessonTarget = true)
        {
            PracticeResult result = new PracticeResult();
            int idx = TrickCatalog.IndexOf(v.Trick);
            bool earnsProgress = !freeLesson || (lessonTarget && v.Success);
            result.MasteryBefore = Data.mastery[idx];
            Data.trickAttempts[idx]++;

            Data.totals.attempts++;
            if (v.Success)
            {
                Data.totals.success++;
                Data.totals.streak++;
                if (Data.totals.streak > Data.totals.bestStreak) Data.totals.bestStreak = Data.totals.streak;
            }
            else
            {
                Data.totals.bails++;
                Data.totals.streak = 0;
            }
            if (v.Score > Data.totals.bestScore) Data.totals.bestScore = v.Score;
            int airMs = (int)Math.Round(v.AirTime * 1000.0);
            if (airMs > Data.totals.bestAirMs) Data.totals.bestAirMs = airMs;
            int grindMs = (int)Math.Round(v.GrindTime * 1000.0);
            if (grindMs > Data.totals.bestGrindMs) Data.totals.bestGrindMs = grindMs;

            int gain = earnsProgress ? (v.Success ? (3 + TrickCatalog.Get(v.Trick).Tier) : 1) : 0;
            Data.mastery[idx] = Math.Min(100, Data.mastery[idx] + gain);
            result.MasteryAfter = Data.mastery[idx];

            if (v.Success && earnsProgress)
            {
                GainStats(v);
                result.XpGain = 8 + v.Score / 20 + TrickCatalog.Get(v.Trick).Tier * 2;
            }
            else if (!v.Success && earnsProgress)
            {
                result.XpGain = 2;
            }

            result.LevelUps = AddXpInternal(result.XpGain);
            if (earnsProgress) RegisterMissions(v);
            result.MissionComplete = PendingMissionXp > 0;
            result.MissionTitle = PendingMissionTitle;
            result.MissionXp = PendingMissionXp;
            PendingMissionTitle = "";
            PendingMissionXp = 0;

            Save();
            return result;
        }

        private void GainStats(Verdict v)
        {
            TrickSpec spec = TrickCatalog.Get(v.Trick);
            int primary;
            if (spec.Category == TrickCategory.Grind) primary = 1;
            else if (v.Trick == Trick.Ollie) primary = 0;
            else if (v.Trick == Trick.Frontside180 || v.Trick == Trick.Backside180 ||
                     v.Trick == Trick.Frontside360 || v.Trick == Trick.Backside360) primary = 1;
            else if (v.Trick == Trick.Kickflip || v.Trick == Trick.Heelflip) primary = 5;
            else primary = 5;

            Data.stats[primary] = Math.Min(MaxStat, Data.stats[primary] + (spec.Tier >= 2 ? 2 : 1));
            Data.stats[3] = Math.Min(MaxStat, Data.stats[3] + 1);
            if (spec.Tier >= 2 || spec.Category == TrickCategory.Grind)
                Data.stats[4] = Math.Min(MaxStat, Data.stats[4] + 1);
        }

        // ------------------------------------------------------------ match

        public int AddMatch(bool win, int ratingDelta, IList<Trick> observedByPlayer)
        {
            if (win) Data.wins++;
            else Data.losses++;
            Data.rating = Math.Max(0, Data.rating + ratingDelta);
            Data.boardingUsed = Math.Min(MaxBoarding, Data.boardingUsed + 2);
            Data.condition = Math.Max(0, Data.condition - 3);
            Data.totals.matches++;

            int observed = 0;
            if (observedByPlayer != null)
            {
                for (int i = 0; i < observedByPlayer.Count; i++)
                    if (Observe(observedByPlayer[i])) observed++;
            }

            if (win)
            {
                RegisterMissionWin();
                AddXpInternal(30);
            }
            else
            {
                AddXpInternal(10);
            }

            ClaimCompletedMissions();
            Save();
            return observed;
        }

        public int AddFriendMatch(IList<Trick> observedByPlayer)
        {
            Data.boardingUsed = Math.Min(MaxBoarding, Data.boardingUsed + 2);
            Data.condition = Math.Max(0, Data.condition - 3);
            Data.totals.matches++;
            int observed = 0;
            if (observedByPlayer != null)
                for (int i = 0; i < observedByPlayer.Count; i++)
                    if (Observe(observedByPlayer[i])) observed++;
            Save();
            return observed;
        }

        // ------------------------------------------------------------ career

        public ChallengeRecord RecordFor(int index)
        {
            if (index < 0 || index >= Data.career.Length) return new ChallengeRecord();
            if (Data.career[index] == null) Data.career[index] = new ChallengeRecord();
            return Data.career[index];
        }

        public ChallengeRecord RecordFor(ChallengeSpec spec)
        {
            return RecordFor(Career.IndexOf(spec.Id));
        }

        public CareerOutcome RegisterCareer(ChallengeSpec spec, Verdict v)
        {
            CareerOutcome outcome = new CareerOutcome();
            int idx = Career.IndexOf(spec.Id);
            if (idx < 0) return outcome;

            ChallengeRecord rec = RecordFor(idx);
            bool wasComplete = rec.completed;
            int beforeStars = rec.stars;

            bool changed = CareerRules.Register(spec, rec, v);
            outcome.Registered = true;
            outcome.Stars = rec.stars;
            outcome.StarUp = rec.stars > beforeStars;
            outcome.NewlyCompleted = !wasComplete && rec.completed;

            if (outcome.NewlyCompleted)
            {
                outcome.XpGain = spec.RewardXp;
                AddXpInternal(spec.RewardXp);
                List<Trick> unlocked = new List<Trick>();
                if (spec.Unlocks != null)
                {
                    for (int i = 0; i < spec.Unlocks.Length; i++)
                    {
                        if (Observe(spec.Unlocks[i], false)) unlocked.Add(spec.Unlocks[i]);
                    }
                }
                outcome.Unlocked = unlocked.ToArray();
                RecalcCareerTotals();
                Save();
            }
            else if (changed)
            {
                RecalcCareerTotals();
                Save();
            }

            Data.careerDone = AllCareerStars() > 0 && NextAvailableChallenge() == null;
            Save();
            return outcome;
        }

        public int AllCareerStars()
        {
            int n = 0;
            for (int i = 0; i < Data.career.Length; i++) n += Data.career[i] != null ? Data.career[i].stars : 0;
            return n;
        }

        public void RecalcCareerTotals()
        {
            Data.totals.careerStars = AllCareerStars();
        }

        public bool SpotUnlocked(int spotIndex)
        {
            if (spotIndex <= 0) return true;
            if (spotIndex >= Spots.All.Length) return false;
            SpotDef prev = Spots.All[spotIndex - 1];
            ChallengeSpec[] list = Career.ForSpot(prev.Id);
            for (int i = 0; i < list.Length; i++)
            {
                ChallengeRecord rec = RecordFor(list[i]);
                if (rec == null || rec.stars < 1) return false;
            }
            return true;
        }

        public bool ChallengeUnlocked(ChallengeSpec spec)
        {
            int spotIdx = Spots.IndexOf(spec.SpotId);
            if (!SpotUnlocked(spotIdx)) return false;
            ChallengeSpec[] list = Career.ForSpot(spec.SpotId);
            if (list.Length == 0 || list[0] == spec) return true;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == spec)
                {
                    ChallengeRecord prev = RecordFor(list[i - 1]);
                    return prev != null && prev.stars >= 1;
                }
            }
            return false;
        }

        public ChallengeSpec NextAvailableChallenge()
        {
            for (int s = 0; s < Spots.All.Length; s++)
            {
                if (!SpotUnlocked(s)) continue;
                ChallengeSpec[] list = Career.ForSpot(Spots.All[s].Id);
                for (int i = 0; i < list.Length; i++)
                {
                    ChallengeRecord rec = RecordFor(list[i]);
                    if (rec.stars < 1) return list[i];
                }
            }
            return null;
        }

        public ChallengeSpec NextInSpot(string spotId)
        {
            ChallengeSpec[] list = Career.ForSpot(spotId);
            for (int i = 0; i < list.Length; i++)
            {
                ChallengeRecord rec = RecordFor(list[i]);
                if (rec.stars < 1) return list[i];
            }
            return null;
        }

        // ------------------------------------------------------------ missions

        public MissionSpec[] MissionSpecs()
        {
            if (Data.missions == null || Data.missions.Length != 3)
                RefreshMissions();

            MissionSpec[] arr = new MissionSpec[3];
            for (int i = 0; i < 3; i++)
            {
                string id = Data.missions[i] != null ? Data.missions[i].id : "";
                arr[i] = string.IsNullOrEmpty(id) ? MissionCatalog.Pool[i] : MissionCatalog.Get(id);
            }
            return arr;
        }

        public MissionRecord MissionRecordAt(int i)
        {
            if (Data.missions == null || i < 0 || i >= Data.missions.Length) return new MissionRecord();
            return Data.missions[i];
        }

        public void RefreshMissions()
        {
            string today = string.IsNullOrEmpty(Data.day) ? DateTime.Now.ToString("yyyy-MM-dd") : Data.day;
            bool valid = Data.missions != null && Data.missions.Length == 3;
            if (valid)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Data.missions[i] == null || string.IsNullOrEmpty(Data.missions[i].id)) valid = false;
                }
            }

            if (!valid || Data.missionDay != today)
            {
                MissionSpec[] roll = MissionCatalog.Roll(today, 3);
                MissionRecord[] arr = new MissionRecord[3];
                for (int i = 0; i < 3; i++)
                {
                    arr[i] = new MissionRecord();
                    arr[i].id = roll[i].Id;
                }
                Data.missions = arr;
                Data.missionDay = today;
            }
        }

        private void RegisterMissions(Verdict v)
        {
            MissionSpec[] specs = MissionSpecs();
            for (int i = 0; i < 3; i++)
            {
                MissionRecord rec = MissionRecordAt(i);
                if (rec == null || rec.claimed) continue;
                MissionRules.RegisterAttempt(specs[i], rec, v);
            }
            ClaimCompletedMissions();
        }

        private void RegisterMissionObservation()
        {
            MissionSpec[] specs = MissionSpecs();
            for (int i = 0; i < 3; i++)
            {
                MissionRecord rec = MissionRecordAt(i);
                if (rec == null || rec.claimed) continue;
                if (specs[i].Type == MissionType.Observe) MissionRules.RegisterObservation(rec, 1);
            }
            ClaimCompletedMissions();
        }

        private void RegisterMissionWin()
        {
            MissionSpec[] specs = MissionSpecs();
            for (int i = 0; i < 3; i++)
            {
                MissionRecord rec = MissionRecordAt(i);
                if (rec == null || rec.claimed) continue;
                if (specs[i].Type == MissionType.WinMatch) MissionRules.RegisterWin(rec);
            }
        }

        public void ClaimCompletedMissions()
        {
            MissionSpec[] specs = MissionSpecs();
            for (int i = 0; i < 3; i++)
            {
                MissionRecord rec = MissionRecordAt(i);
                if (rec == null || rec.claimed) continue;
                if (MissionRules.IsComplete(specs[i], rec))
                {
                    rec.claimed = true;
                    AddXpInternal(specs[i].RewardXp);
                    PendingMissionXp += specs[i].RewardXp;
                    PendingMissionTitle = specs[i].Title;
                }
            }
        }

        public bool MissionClaimed(int i)
        {
            MissionRecord rec = MissionRecordAt(i);
            return rec != null && rec.claimed;
        }

        // ------------------------------------------------------------ economy

        public void Rest()
        {
            Data.boardingUsed = Math.Min(MaxBoarding, Data.boardingUsed + 30);
            Data.condition = Math.Min(100, Data.condition + 35);
            Save();
        }

        public bool NextDay()
        {
            if (!CheckDay()) return false;
            RefreshMissions();
            Save();
            return true;
        }

        private int FirstLearnedIndex()
        {
            for (int i = 0; i < Data.learned.Length; i++)
                if (Data.learned[i] > 0) return i;
            return TrickCatalog.IndexOf(Trick.Ollie);
        }

        private int AddXpInternal(int amount)
        {
            if (amount <= 0) return 0;
            int before = Level;
            Data.xp += amount;
            int after = Level;
            int ups = after - before;
            if (ups > 0)
            {
                Data.condition = Math.Min(100, Data.condition + 5 * ups);
                PendingLevelUp += ups;
            }
            return ups;
        }

        // ------------------------------------------------------------ settings

        public void SetControlMode(int mode)
        {
            Data.settings.controlMode = mode == 1 ? 1 : 0;
            Save();
        }

        public void SetSound(bool on)
        {
            Data.settings.sound = on;
            Save();
        }

        public void SetMusicVolume(float volume)
        {
            Data.settings.musicVolume = Mathf.Clamp01(volume);
            Save();
        }

        public void SetHaptics(bool on)
        {
            Data.settings.haptics = on;
            Save();
        }

        public void SetShake(bool on)
        {
            Data.settings.screenShake = on;
            Save();
        }

        public void SetInputOffset(int ms)
        {
            Data.settings.inputOffsetMs = Mathf.Clamp(ms, -80, 80);
            Save();
        }

        public void StartTutorial()
        {
            Data.tutorialDone = false;
            Data.tutorialSkipped = false;
            Data.tutorialStage = 1;
            Save();
        }

        public void AdvanceTutorial(int stage)
        {
            Data.tutorialStage = Math.Max(1, Math.Min(4, stage));
            Save();
        }

        public void SkipTutorial()
        {
            Data.tutorialSkipped = true;
            Save();
        }

        public void SetTutorialDone(bool done)
        {
            Data.tutorialDone = done;
            Data.tutorialSkipped = false;
            Data.tutorialStage = done ? 4 : 0;
            Save();
        }

        public void ResetTutorial()
        {
            Data.tutorialDone = false;
            Data.tutorialSkipped = false;
            Data.tutorialStage = 0;
            Save();
        }

        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(KeyV1);
            PlayerPrefs.Save();
            Data = new ProgressData();
            Normalize();
            Data.day = DateTime.Now.ToString("yyyy-MM-dd");
            Data.missionDay = "";
            RefreshMissions();
            Data.stance = -1;
            Save();
        }

        public int PopularTrick
        {
            get
            {
                int best = 0;
                for (int i = 1; i < Data.mastery.Length; i++)
                    if (Data.mastery[i] > Data.mastery[best]) best = i;
                return best;
            }
        }
    }
}
