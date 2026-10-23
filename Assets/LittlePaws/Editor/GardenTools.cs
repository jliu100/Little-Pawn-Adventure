using System;
using UnityEditor;
using UnityEngine;
using LittlePaws;

public static class GardenTools
{
    [MenuItem("Little Paws/Check Garden farming")]
    public static void CheckProgression()
    {
        var now=new DateTime(2026,10,23,12,0,0,DateTimeKind.Utc);var farm=new GardenProgress();
        Require(farm.UnlockedPlotCount==3,"Three starting fields");
        Require(farm.Plant(0,GardenCrop.Corn,now)&&farm.Plant(1,GardenCrop.Tomato,now),"Starter seeds usable");
        Require(farm.StageAt(0,now.AddSeconds(60))==GardenGrowthStage.MidGrowth,"Corn middle stage at one minute");
        Require(farm.StageAt(1,now.AddSeconds(90))==GardenGrowthStage.MidGrowth,"Tomato middle stage at 90 seconds");
        Require(farm.Harvest(0,now.AddSeconds(120))&&farm.CornProduce==5&&farm.SkillPoints==3,"Corn yield and skill");
        Require(farm.Harvest(1,now.AddSeconds(180))&&farm.TomatoProduce==5&&farm.SkillPoints==7,"Tomato yield and skill");
        Require(farm.SellProduce(GardenCrop.Corn)==5&&farm.SellProduce(GardenCrop.Tomato)==10,"Market prices");
        var saved=JsonUtility.FromJson<GardenProgress>(JsonUtility.ToJson(farm));saved.Normalize();
        Require(saved.Coins==35&&saved.SkillPoints==7,"Farm save JSON round trip");
        saved.AddSkillPoints(93);Require(saved.Level==5&&saved.UnlockedPlotCount==4,"Fourth field at gardening level 5");
        Require(saved.TryStartScarecrowGame(now),"First daily round starts");
        saved=JsonUtility.FromJson<GardenProgress>(JsonUtility.ToJson(saved));saved.Normalize();
        Require(saved.CompleteScarecrowGame(6)==12&&!saved.CanPlayScarecrow(now),"Daily result once only");
        Require(saved.CompleteScarecrowGame(6)==0&&saved.CanPlayScarecrow(now.AddDays(1)),"No duplicate reward; new UTC day unlocks");
        Debug.Log("Garden farming checks passed: growth, shop, saves, field unlock and daily game.");
    }
    [MenuItem("Little Paws/Testing/Reset today's scarecrow attempt")]
    static void ResetDailyAttempt()
    {
        if(EditorApplication.isPlaying){Debug.Log("Stop Play mode first, then reset the daily scarecrow attempt.");return;}
        var farm=GardenProgressStore.Load();farm.LastScarecrowDay=-1;farm.ScarecrowGameActive=false;GardenProgressStore.Save(farm);
        Debug.Log("Scarecrow daily attempt reset for testing. Coins, fields and skill are unchanged.");
    }
    static void Require(bool condition,string reason){if(!condition)throw new Exception(reason);}
}
