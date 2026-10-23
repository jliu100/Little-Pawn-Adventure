using System;

namespace LittlePaws
{
    public enum GardenCrop { None, Corn, Tomato }
    public enum GardenGrowthStage { Empty, Seed, MidGrowth, Harvest }

    [Serializable]
    public sealed class GardenPlot
    {
        public GardenCrop Crop;
        public long PlantedUtcTicks;
    }

    /// <summary>Saved farming inventory, UTC crop clocks and a single daily scarecrow attempt.</summary>
    [Serializable]
    public sealed class GardenProgress
    {
        public const int PlotCount=4, StartingCoins=20, StartingSeeds=3, FourthPlotLevel=5;
        public int Coins=StartingCoins;
        public int CornSeeds=StartingSeeds, TomatoSeeds=StartingSeeds;
        public int CornProduce, TomatoProduce, SkillPoints;
        public GardenPlot[] Plots={new GardenPlot(),new GardenPlot(),new GardenPlot(),new GardenPlot()};
        public long LastScarecrowDay=-1;
        public bool ScarecrowGameActive;

        public int Level
        {
            get
            {
                long points=Math.Max(0,SkillPoints);
                int level=Math.Max(1,(int)((1+Math.Sqrt(1+.8d*points))/2));
                while((long)level*(level-1)*5>points)level--;
                while((long)level*(level+1)*5<=points)level++;
                return level;
            }
        }
        public int PointsInLevel=>(int)(Math.Max(0,(long)SkillPoints)-(long)Level*(Level-1)*5);
        public int PointsToNextLevel=>Level*10;
        public int UnlockedPlotCount=>Level>=FourthPlotLevel?4:3;

        public static int GrowSeconds(GardenCrop crop)=>crop==GardenCrop.Corn?120:crop==GardenCrop.Tomato?180:0;
        public static int SeedCost(GardenCrop crop)=>crop==GardenCrop.Corn?2:crop==GardenCrop.Tomato?3:0;
        public static int SellPrice(GardenCrop crop)=>crop==GardenCrop.Corn?1:crop==GardenCrop.Tomato?2:0;
        public static int Yield(GardenCrop crop)=>IsCrop(crop)?5:0;
        public static int HarvestPoints(GardenCrop crop)=>crop==GardenCrop.Corn?3:crop==GardenCrop.Tomato?4:0;

        public void Normalize()
        {
            Coins=Math.Max(0,Coins);CornSeeds=Math.Max(0,CornSeeds);TomatoSeeds=Math.Max(0,TomatoSeeds);
            CornProduce=Math.Max(0,CornProduce);TomatoProduce=Math.Max(0,TomatoProduce);SkillPoints=Math.Max(0,SkillPoints);
            if(Plots==null||Plots.Length!=PlotCount)
            {
                var repaired=new GardenPlot[PlotCount];
                if(Plots!=null)Array.Copy(Plots,repaired,Math.Min(Plots.Length,PlotCount));
                Plots=repaired;
            }
            for(int i=0;i<PlotCount;i++)
            {
                if(Plots[i]==null)Plots[i]=new GardenPlot();
                var plot=Plots[i];
                if(!IsCrop(plot.Crop)||plot.PlantedUtcTicks<0||plot.PlantedUtcTicks>DateTime.MaxValue.Ticks)
                {plot.Crop=GardenCrop.None;plot.PlantedUtcTicks=0;}
            }
            if(LastScarecrowDay< -1||LastScarecrowDay>DateTime.MaxValue.Ticks/TimeSpan.TicksPerDay)
            {LastScarecrowDay=-1;ScarecrowGameActive=false;}
            if(LastScarecrowDay<0)ScarecrowGameActive=false;
        }

        public GardenGrowthStage StageAt(int index,DateTime now)
        {
            Normalize();if(!ValidPlot(index)||!IsCrop(Plots[index].Crop))return GardenGrowthStage.Empty;
            double elapsed=Elapsed(Plots[index],now);int duration=GrowSeconds(Plots[index].Crop);
            return elapsed>=duration?GardenGrowthStage.Harvest:elapsed>=duration*.5?GardenGrowthStage.MidGrowth:GardenGrowthStage.Seed;
        }
        public double SecondsRemaining(int index,DateTime now)
        {
            Normalize();if(!ValidPlot(index)||!IsCrop(Plots[index].Crop))return 0;
            return Math.Max(0,GrowSeconds(Plots[index].Crop)-Elapsed(Plots[index],now));
        }
        public bool Plant(int index,GardenCrop crop,DateTime now)
        {
            Normalize();if(!ValidPlot(index)||index>=UnlockedPlotCount||!IsCrop(crop)||Plots[index].Crop!=GardenCrop.None)return false;
            if(crop==GardenCrop.Corn){if(CornSeeds<=0)return false;CornSeeds--;}
            else{if(TomatoSeeds<=0)return false;TomatoSeeds--;}
            Plots[index].Crop=crop;Plots[index].PlantedUtcTicks=Utc(now).Ticks;return true;
        }
        public bool Harvest(int index,DateTime now)
        {
            Normalize();if(!ValidPlot(index)||index>=UnlockedPlotCount||StageAt(index,now)!=GardenGrowthStage.Harvest)return false;
            var crop=Plots[index].Crop;int count=Yield(crop);
            if(crop==GardenCrop.Corn){if(CornProduce>int.MaxValue-count)return false;CornProduce+=count;}
            else{if(TomatoProduce>int.MaxValue-count)return false;TomatoProduce+=count;}
            AddSkillPoints(HarvestPoints(crop));Plots[index].Crop=GardenCrop.None;Plots[index].PlantedUtcTicks=0;return true;
        }
        public bool BuySeed(GardenCrop crop)
        {
            Normalize();if(!IsCrop(crop)||Coins<SeedCost(crop))return false;
            if(crop==GardenCrop.Corn){if(CornSeeds==int.MaxValue)return false;CornSeeds++;}
            else{if(TomatoSeeds==int.MaxValue)return false;TomatoSeeds++;}
            Coins-=SeedCost(crop);return true;
        }
        /// <returns>The coins received for selling every harvested item of this crop.</returns>
        public int SellProduce(GardenCrop crop)
        {
            Normalize();if(!IsCrop(crop))return 0;
            long income=(long)(crop==GardenCrop.Corn?CornProduce:TomatoProduce)*SellPrice(crop);
            if(income>int.MaxValue-Coins)return 0;
            Coins+=(int)income;if(crop==GardenCrop.Corn)CornProduce=0;else TomatoProduce=0;return (int)income;
        }
        public void AddSkillPoints(int amount)
        {if(amount>0)SkillPoints=(int)Math.Min(int.MaxValue,Math.Max(0,(long)SkillPoints)+amount);}

        public bool CanPlayScarecrow(DateTime now)
        {Normalize();return !ScarecrowGameActive&&Day(now)>LastScarecrowDay;}
        public bool TryStartScarecrowGame(DateTime now)
        {
            if(!CanPlayScarecrow(now))return false;
            LastScarecrowDay=Day(now);ScarecrowGameActive=true;return true;
        }
        /// <summary>Reward can be claimed only once for the saved active attempt. Save immediately after calling.</summary>
        public int CompleteScarecrowGame(int cropsLeft)
        {
            Normalize();if(!ScarecrowGameActive)return 0;
            ScarecrowGameActive=false;int points=Math.Max(0,Math.Min(6,cropsLeft))*2;AddSkillPoints(points);return points;
        }
        /// <summary>Closing or restarting an unfinished game consumes its daily attempt without a reward.</summary>
        public void AbandonScarecrowGame(){ScarecrowGameActive=false;}

        static bool ValidPlot(int index)=>index>=0&&index<PlotCount;
        static bool IsCrop(GardenCrop crop)=>crop==GardenCrop.Corn||crop==GardenCrop.Tomato;
        static DateTime Utc(DateTime time)=>time.Kind==DateTimeKind.Unspecified?DateTime.SpecifyKind(time,DateTimeKind.Utc):time.ToUniversalTime();
        static long Day(DateTime now)=>Utc(now).Ticks/TimeSpan.TicksPerDay;
        static double Elapsed(GardenPlot plot,DateTime now)=>Math.Max(0,(Utc(now).Ticks-plot.PlantedUtcTicks)/(double)TimeSpan.TicksPerSecond);
    }
}
