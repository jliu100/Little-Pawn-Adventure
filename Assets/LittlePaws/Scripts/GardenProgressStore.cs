using UnityEngine;

namespace LittlePaws
{
    public static class GardenProgressStore
    {
        const string SaveKey="LittlePaws.Garden.Progress.v1";
        public static GardenProgress Load()
        {
            GardenProgress progress=null;
            try{progress=JsonUtility.FromJson<GardenProgress>(PlayerPrefs.GetString(SaveKey,""));}catch{}
            if(progress==null)progress=new GardenProgress();progress.Normalize();return progress;
        }
        public static void Save(GardenProgress progress)
        {
            if(progress==null||!CharacterProgressStore.RewardsAllowed)return;
            progress.Normalize();PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(progress));PlayerPrefs.Save();
        }
    }
}
