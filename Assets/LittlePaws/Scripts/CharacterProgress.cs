using System;
namespace LittlePaws
{
    /// <summary>Character experience is separate from maze level unlocks and shared across companions.</summary>
    [Serializable]
    public sealed class CharacterProgress
    {
        public const int ExperiencePerLevel=100, GardenUnlockLevel=20, MaxLevel=100, MazeCount=50;
        public int TotalExperience;
        public bool HasVegetablePet;
        public long LastBathDay;
        public bool[] MazeFirstClears=new bool[MazeCount];
        public long[] MazeRewardDays=new long[MazeCount];
        public int Level=>Math.Min(MaxLevel,1+Math.Max(0,TotalExperience)/ExperiencePerLevel);
        public int ExperienceInLevel=>Level==MaxLevel?0:Math.Max(0,TotalExperience)%ExperiencePerLevel;
        public bool GardenUnlocked=>Level>=GardenUnlockLevel;
        public void Normalize()
        {
            TotalExperience=Math.Max(0,Math.Min((MaxLevel-1)*ExperiencePerLevel,TotalExperience));
            if(MazeFirstClears==null||MazeFirstClears.Length!=MazeCount)
            {
                var repaired=new bool[MazeCount];if(MazeFirstClears!=null)Array.Copy(MazeFirstClears,repaired,Math.Min(MazeFirstClears.Length,MazeCount));MazeFirstClears=repaired;
            }
            if(MazeRewardDays==null||MazeRewardDays.Length!=MazeCount)
            {
                var repaired=new long[MazeCount];if(MazeRewardDays!=null)Array.Copy(MazeRewardDays,repaired,Math.Min(MazeRewardDays.Length,MazeCount));MazeRewardDays=repaired;
            }
        }
        public int RewardBath(DateTime utcNow)
        {
            Normalize();long day=Day(utcNow);if(day<=LastBathDay)return 0;
            LastBathDay=day;return AddExperience(25);
        }
        public int RewardMaze(int maze,DateTime utcNow)
        {
            Normalize();if(maze<1||maze>MazeCount)return 0;
            int index=maze-1;long day=Day(utcNow);
            if(MazeFirstClears[index]&&day<=MazeRewardDays[index])return 0;
            bool first=!MazeFirstClears[index];MazeFirstClears[index]=true;MazeRewardDays[index]=Math.Max(day,MazeRewardDays[index]);
            return AddExperience(first?100:25);
        }
        public bool TryAdoptVegetablePet()
        {
            Normalize();if(!GardenUnlocked||HasVegetablePet)return false;
            HasVegetablePet=true;return true;
        }
        int AddExperience(int amount)
        {
            int before=TotalExperience;TotalExperience=Math.Min((MaxLevel-1)*ExperiencePerLevel,TotalExperience+amount);return TotalExperience-before;
        }
        static long Day(DateTime utcNow)=>utcNow.ToUniversalTime().Date.Ticks/TimeSpan.TicksPerDay;
    }
}
