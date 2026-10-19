using System;

namespace LittlePaws.Maze
{
    /// <summary>Serializable progress. Time is UTC; attempt state is deliberately not persisted.</summary>
    [Serializable]
    public sealed class MazeProgress
    {
        public const int LevelCount = 50;
        public const int MaxEnergy = 50;
        public const int StartCost = 5;
        // Provisional balancing: one point per five minutes, also while the app is closed.
        public const int EnergyRecoverySeconds = 300;
        private const long RecoveryTicks = EnergyRecoverySeconds * TimeSpan.TicksPerSecond;

        public int Energy = MaxEnergy;
        public int HighestUnlocked = 1;
        public long EnergyUpdatedUtcTicks;
        public bool[] Completed = new bool[LevelCount];

        public int CurrentAttempt { get; private set; }

        public static MazeProgress Create(DateTime utcNow)
        {
            var progress = new MazeProgress();
            progress.Normalize(utcNow);
            return progress;
        }

        /// <summary>Repair missing or malformed saved values before using them.</summary>
        public void Normalize(DateTime utcNow)
        {
            long nowTicks = UtcTicks(utcNow);
            Energy = Math.Max(0, Math.Min(MaxEnergy, Energy));
            HighestUnlocked = Math.Max(1, Math.Min(LevelCount, HighestUnlocked));
            if (Completed == null || Completed.Length != LevelCount)
            {
                var repaired = new bool[LevelCount];
                if (Completed != null)
                    Array.Copy(Completed, repaired, Math.Min(Completed.Length, LevelCount));
                Completed = repaired;
            }
            // A malformed save must not complete levels which have never been unlocked.
            for (int i = HighestUnlocked; i < LevelCount; i++) Completed[i] = false;
            if (CurrentAttempt < 1 || CurrentAttempt > HighestUnlocked) CurrentAttempt = 0;
            if (EnergyUpdatedUtcTicks <= 0 || EnergyUpdatedUtcTicks > nowTicks)
                EnergyUpdatedUtcTicks = nowTicks;
        }

        public void RefreshEnergy(DateTime utcNow)
        {
            Normalize(utcNow);
            long nowTicks = UtcTicks(utcNow);
            if (Energy == MaxEnergy)
            {
                // Time at full energy cannot be banked for use after starting a level.
                EnergyUpdatedUtcTicks = nowTicks;
                return;
            }
            long elapsed = nowTicks - EnergyUpdatedUtcTicks;
            long recovered = elapsed / RecoveryTicks;
            if (recovered <= 0) return;
            if (recovered >= MaxEnergy - Energy)
            {
                Energy = MaxEnergy;
                EnergyUpdatedUtcTicks = nowTicks;
            }
            else
            {
                Energy += (int)recovered;
                // Carry the partial point forward across refreshes and additional starts.
                EnergyUpdatedUtcTicks += recovered * RecoveryTicks;
            }
        }

        public bool CanStart(int level, DateTime utcNow)
        {
            RefreshEnergy(utcNow);
            return level >= 1 && level <= HighestUnlocked && level <= LevelCount && Energy >= StartCost;
        }

        /// <summary>Call only for explicit Start/Replay/Retry. Opening a selector is free.</summary>
        public bool TryStart(int level, DateTime utcNow)
        {
            if (!CanStart(level, utcNow)) return false;
            Energy -= StartCost;
            CurrentAttempt = level;
            return true;
        }

        public bool Complete(int level)
        {
            if (level < 1 || level > LevelCount || level > HighestUnlocked || CurrentAttempt != level)
                return false;
            Completed[level - 1] = true;
            HighestUnlocked = Math.Max(HighestUnlocked, Math.Min(LevelCount, level + 1));
            CurrentAttempt = 0;
            return true;
        }

        public void AbandonAttempt()
        {
            CurrentAttempt = 0;
        }

        public int SecondsToNextEnergy(DateTime utcNow)
        {
            RefreshEnergy(utcNow);
            if (Energy == MaxEnergy) return 0;
            long remaining = RecoveryTicks - (UtcTicks(utcNow) - EnergyUpdatedUtcTicks);
            return (int)((remaining + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond);
        }

        private static long UtcTicks(DateTime value)
        {
            return value.Kind == DateTimeKind.Local ? value.ToUniversalTime().Ticks : value.Ticks;
        }
    }
}
