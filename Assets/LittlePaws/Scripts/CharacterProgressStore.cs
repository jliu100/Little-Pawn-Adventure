using System;
using UnityEngine;
namespace LittlePaws
{
    public static class CharacterProgressStore
    {
        const string SaveKey="LittlePaws.Character.Progress.v1";
        public const string GardenPreviewPreference="LittlePaws.Garden.PreviewLevel20";
        public static bool RewardsAllowed
        {
            get
            {
#if UNITY_EDITOR
                return !UnityEditor.SessionState.GetBool("LittlePaws.SmokeRunning",false)
                    &&!UnityEditor.SessionState.GetBool("LittlePaws.MazeCheck.Running",false);
#else
                return true;
#endif
            }
        }
        public static bool GardenPreviewEnabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(GardenPreviewPreference,false);
#else
                return false;
#endif
            }
        }
        public static CharacterProgress Load()
        {
            CharacterProgress profile=null;
            try{profile=JsonUtility.FromJson<CharacterProgress>(PlayerPrefs.GetString(SaveKey,""));}catch{}
            if(profile==null)profile=new CharacterProgress();profile.Normalize();
            // Credit genuine earlier maze completions once. Editor practice unlocks are never imported.
            bool changed=false;
            if(RewardsAllowed)
            {
                try
                {
                    var maze=JsonUtility.FromJson<Maze.MazeProgress>(PlayerPrefs.GetString("LittlePaws.EscapeMaze.Progress.v1",""));
                    if(maze!=null)
                    {
                        maze.Normalize(DateTime.UtcNow);
                        for(int i=0;i<CharacterProgress.MazeCount;i++)
                            if(maze.Completed[i]&&!profile.MazeFirstClears[i]){profile.RewardMaze(i+1,DateTime.UtcNow);changed=true;}
                    }
                }
                catch{}
            }
            if(changed)Save(profile);return profile;
        }
        public static void Save(CharacterProgress profile)
        {
            if(profile==null||!RewardsAllowed)return;
            profile.Normalize();PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(profile));PlayerPrefs.Save();
        }
    }
}
