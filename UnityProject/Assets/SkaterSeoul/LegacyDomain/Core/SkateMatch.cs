using System;
using System.Collections.Generic;

namespace Skater.Core
{
    public enum MatchSide
    {
        Player,
        Bot
    }

    public enum MatchPhase
    {
        PlayerSet,
        PlayerMatch,
        BotSet,
        BotMatch,
        Finished
    }

    public enum BotPersona
    {
        Rookie,
        Amateur,
        Pro
    }

    /// <summary>
    /// 턴제 S.K.A.T.E. 규칙. 렌더링/입력과 무관하게 돌아간다.
    /// - 세터가 트릭을 성공하면 매처가 같은 트릭을 1회 시도.
    /// - 매처 실패 → 글자 1개. 세터 실패 → 글자 없이 세터만 교체.
    /// - 3글자(단판) 또는 5글자(정규).
    /// </summary>
    public sealed class SkateMatch
    {
        public const int TurnLimitDefault = 15;

        public MatchSide Setter { get; private set; }
        public MatchPhase Phase { get; private set; }
        public Trick RequiredTrick { get; private set; }
        public int PlayerLetters { get; private set; }
        public int BotLetters { get; private set; }
        public int LettersToLose { get; private set; }
        public BotPersona Persona { get; private set; }
        public bool Finished { get; private set; }
        public MatchSide Winner { get; private set; }
        public string LastEvent { get; private set; }
        public double TurnStart { get; private set; }
        public double TurnLimit { get; set; }
        public int TurnNumber { get; private set; }
        public int PlayerSetCount { get; private set; }
        public int PlayerMatchCount { get; private set; }
        public Trick LastBotSetTrick { get; private set; }
        public bool LastBotSetSucceeded { get; private set; }

        public readonly List<string> Log = new List<string>();
        public readonly List<Trick> PlayerSetTricks = new List<Trick>();
        public readonly List<Trick> BotSetTricks = new List<Trick>();

        private readonly Rng _rng;
        private readonly Trick[] _botTrickPool;
        private Trick _lastPicked;
        private bool _hasPicked;

        public SkateMatch(BotPersona persona, int lettersToLose, bool playerStarts, int seed)
        {
            Persona = persona;
            LettersToLose = lettersToLose < 3 ? 3 : lettersToLose;
            if (LettersToLose > 5) LettersToLose = 5;
            TurnLimit = TurnLimitDefault;
            LastEvent = "";
            RequiredTrick = Trick.Ollie;
            _rng = new Rng(seed == 0 ? 7717 : seed);

            if (playerStarts)
            {
                Setter = MatchSide.Player;
                Phase = MatchPhase.PlayerSet;
            }
            else
            {
                Setter = MatchSide.Bot;
                Phase = MatchPhase.BotSet;
            }
        }

        public SkateMatch(bool playerStarts) : this(BotPersona.Amateur, 3, playerStarts, 7717)
        {
        }

        public SkateMatch(BotPersona persona, int lettersToLose, bool playerStarts, int seed, Trick[] botTrickPool)
            : this(persona, lettersToLose, playerStarts, seed)
        {
            if (botTrickPool != null && botTrickPool.Length > 0)
                _botTrickPool = (Trick[])botTrickPool.Clone();
        }

        public bool IsPlayerTurn
        {
            get { return Phase == MatchPhase.PlayerSet || Phase == MatchPhase.PlayerMatch; }
        }

        public bool IsBotTurn
        {
            get { return Phase == MatchPhase.BotSet || Phase == MatchPhase.BotMatch; }
        }

        public void StartTurn(double now)
        {
            TurnStart = now;
            TurnNumber++;
        }

        public void OnPlayerAttempt(Verdict v, double now)
        {
            if (Finished || !IsPlayerTurn) return;

            if (Phase == MatchPhase.PlayerSet)
            {
                if (v.Success)
                {
                    Setter = MatchSide.Player;
                    RequiredTrick = v.Trick;
                    PlayerSetTricks.Add(v.Trick);
                    PlayerSetCount++;
                    Phase = MatchPhase.BotMatch;
                    LastEvent = "나: " + v.NameKo;
                }
                else
                {
                    Setter = MatchSide.Bot;
                    Phase = MatchPhase.BotSet;
                    LastEvent = "나: 실패 — " + Labels.Reason(v.Reason);
                }
            }
            else
            {
                if (v.Success && v.Trick == RequiredTrick)
                {
                    Setter = MatchSide.Player;
                    Phase = MatchPhase.PlayerSet;
                    PlayerMatchCount++;
                    LastEvent = "나: 매치 성공!";
                }
                else
                {
                    PlayerLetters++;
                    Setter = MatchSide.Bot;
                    Phase = MatchPhase.BotSet;
                    if (!v.Success)
                        LastEvent = "나: " + Labels.Reason(v.Reason);
                    else
                        LastEvent = "나: 다른 트릭 (" + v.NameKo + ")";
                }
            }

            Log.Add(LastEvent);
            TurnStart = now;
            EndIfNeeded();
        }

        public void OnBotTurn(double now)
        {
            if (Finished || !IsBotTurn) return;

            if (Phase == MatchPhase.BotSet)
            {
                Trick picked = PickBotTrick();
                bool ok = _rng.NextDouble() < BotChance(picked);
                LastBotSetTrick = picked;
                LastBotSetSucceeded = ok;
                if (ok)
                {
                    Setter = MatchSide.Bot;
                    RequiredTrick = picked;
                    BotSetTricks.Add(picked);
                    Phase = MatchPhase.PlayerMatch;
                    LastEvent = "상대: " + Labels.TrickKo(picked);
                }
                else
                {
                    Setter = MatchSide.Player;
                    Phase = MatchPhase.PlayerSet;
                    LastEvent = "상대: 실패 — 세터 교체";
                }
            }
            else
            {
                bool ok = _rng.NextDouble() < BotChance(RequiredTrick);
                if (ok)
                {
                    Setter = MatchSide.Bot;
                    Phase = MatchPhase.BotSet;
                    LastEvent = "상대: 매치 성공";
                }
                else
                {
                    BotLetters++;
                    Setter = MatchSide.Player;
                    Phase = MatchPhase.PlayerSet;
                    LastEvent = "상대: " + Labels.TrickKo(RequiredTrick) + " 실패";
                }
            }

            Log.Add(LastEvent);
            TurnStart = now;
            EndIfNeeded();
        }

        public void Timeout(double now)
        {
            if (Finished || !IsPlayerTurn) return;

            if (Phase == MatchPhase.PlayerMatch)
            {
                PlayerLetters++;
                Setter = MatchSide.Bot;
                Phase = MatchPhase.BotSet;
                LastEvent = "나: 시간 초과";
            }
            else
            {
                Setter = MatchSide.Bot;
                Phase = MatchPhase.BotSet;
                LastEvent = "나: 시간 초과 — 세터 교체";
            }

            Log.Add(LastEvent);
            TurnStart = now;
            EndIfNeeded();
        }

        private void EndIfNeeded()
        {
            if (PlayerLetters >= LettersToLose)
            {
                Finished = true;
                Winner = MatchSide.Bot;
                Phase = MatchPhase.Finished;
                LastEvent = "패배";
                Log.Add("패배");
            }
            else if (BotLetters >= LettersToLose)
            {
                Finished = true;
                Winner = MatchSide.Player;
                Phase = MatchPhase.Finished;
                LastEvent = "승리";
                Log.Add("승리");
            }
        }

        public string PlayerLettersText
        {
            get { return Pips(PlayerLetters); }
        }

        public string BotLettersText
        {
            get { return Pips(BotLetters); }
        }

        public string PlayerWord
        {
            get { return Word(PlayerLetters); }
        }

        public string BotWord
        {
            get { return Word(BotLetters); }
        }

        public string PersonaKo
        {
            get
            {
                switch (Persona)
                {
                    case BotPersona.Rookie: return "루키";
                    case BotPersona.Pro: return "프로";
                    default: return "아마추어";
                }
            }
        }

        public int RatingDelta(bool won)
        {
            if (won)
            {
                switch (Persona)
                {
                    case BotPersona.Rookie: return 12;
                    case BotPersona.Pro: return 40;
                    default: return 24;
                }
            }
            switch (Persona)
            {
                case BotPersona.Rookie: return -8;
                case BotPersona.Pro: return -30;
                default: return -18;
            }
        }

        private string Pips(int lost)
        {
            char[] chars = new char[LettersToLose];
            for (int i = 0; i < LettersToLose; i++)
                chars[i] = i < lost ? '●' : '○';
            return new string(chars);
        }

        private string Word(int lost)
        {
            const string letters = "SKATE";
            char[] chars = new char[LettersToLose];
            for (int i = 0; i < LettersToLose; i++)
                chars[i] = i < lost ? '×' : letters[i];
            return new string(chars);
        }

        // ------------------------------------------------------------ bot

        private Trick PickBotTrick()
        {
            Trick[] pool = _botTrickPool ?? PoolFor(Persona);
            int total = 0;
            int[] weights = new int[pool.Length];
            for (int i = 0; i < pool.Length; i++)
            {
                int w = 6 - TrickCatalog.Get(pool[i]).Tier;
                if (w < 1) w = 1;
                if (_hasPicked && pool[i] == _lastPicked) w = 1;
                weights[i] = w;
                total += w;
            }

            int roll = _rng.Next(total);
            Trick picked = pool[0];
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    picked = pool[i];
                    break;
                }
            }
            _lastPicked = picked;
            _hasPicked = true;
            return picked;
        }

        private double BotChance(Trick trick)
        {
            int mastery;
            int stat;
            int accuracy;
            switch (Persona)
            {
                case BotPersona.Rookie: mastery = 25; stat = 45; accuracy = 90; break;
                case BotPersona.Pro: mastery = 80; stat = 85; accuracy = 97; break;
                default: mastery = 50; stat = 60; accuracy = 94; break;
            }
            return TrickSuccessModel.Calculate(accuracy, mastery, stat,
                TrickSuccessModel.Difficulty(trick), 0.0, 0.0);
        }

        public static Trick[] PoolFor(BotPersona persona)
        {
            if (persona == BotPersona.Rookie)
            {
                return new[]
                {
                    Trick.Ollie, Trick.Frontside180, Trick.Backside180,
                    Trick.Kickflip, Trick.Heelflip
                };
            }
            if (persona == BotPersona.Amateur)
            {
                return new[]
                {
                    Trick.Ollie, Trick.Frontside180, Trick.Backside180,
                    Trick.Kickflip, Trick.Heelflip, Trick.Shuvit,
                    Trick.BacksideFlip, Trick.Hardflip, Trick.FrontsideFlip,
                    Trick.InwardHeelflip
                };
            }
            return new[]
            {
                Trick.Ollie, Trick.Frontside180, Trick.Backside180,
                Trick.Frontside360, Trick.Backside360,
                Trick.Kickflip, Trick.Heelflip, Trick.Shuvit, Trick.Impossible,
                Trick.BacksideFlip, Trick.Hardflip, Trick.FrontsideFlip,
                Trick.InwardHeelflip, Trick.Frontside360Shuvit, Trick.Backside360Shuvit,
                Trick.ImpossibleFrontside180, Trick.ImpossibleBackside180,
                Trick.VarialKickflip, Trick.VarialHeelflip, Trick.TreFlip, Trick.LaserFlip
            };
        }
    }
}
