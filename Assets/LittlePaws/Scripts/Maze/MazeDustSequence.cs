using System;

namespace LittlePaws.Maze
{
    /// <summary>Full-map memory preview followed by a transition into local dust visibility.</summary>
    public sealed class MazeDustSequence
    {
        public const float OverviewSeconds=3.5f, TransitionSeconds=1.25f;
        public const float ClearRadiusCells=2.5f, FeatherCells=.65f;
        public bool Enabled { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsPreviewing => Enabled && Elapsed<OverviewSeconds+TransitionSeconds;
        public float Transition
        {
            get
            {
                if(!Enabled)return 0f;
                float t=Math.Max(0f,Math.Min(1f,(Elapsed-OverviewSeconds)/TransitionSeconds));
                return t*t*(3f-2f*t);
            }
        }
        public float DustOpacity => Enabled?Transition:0f;
        public static bool AppliesTo(int level) => level>=25 && level<=34;
        public void Begin(int level) { Enabled=AppliesTo(level);Elapsed=0f; }
        public void Advance(float seconds)
        {
            if(Enabled)Elapsed=Math.Min(OverviewSeconds+TransitionSeconds,Elapsed+Math.Max(0f,seconds));
        }
        public static float Obscurity(float distanceCells)
        {
            float t=Math.Max(0f,Math.Min(1f,(distanceCells-ClearRadiusCells)/FeatherCells));
            return t*t*(3f-2f*t);
        }
    }
    /// <summary>Slowly drifting cotton-like billows and a scalloped edge around the clear opening.</summary>
    public static class MazeCloudPattern
    {
        private static double Hash(int x,int y)
        {
            unchecked
            {
                int value=x*374761393+y*668265263;
                value=(value^(value>>13))*1274126177;
                return (value&0x7fffffff)/(double)int.MaxValue;
            }
        }
        public static float Brightness(float x,float y,float time)
        {
            double px=x+time*6d,py=y+Math.Sin(time*.18d)*9d;
            int cellX=(int)Math.Floor(px/110d),cellY=(int)Math.Floor(py/85d);
            double puff=0d;
            for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)
            {
                int gx=cellX+i,gy=cellY+j;
                double cx=(gx+.5d+(Hash(gx,gy)-.5d)*.85d)*110d;
                double cy=(gy+.5d+(Hash(gy,gx+31)-.5d)*.85d)*85d;
                double rx=60d+Hash(gx+71,gy-17)*40d,ry=45d+Hash(gx-29,gy+43)*30d;
                double dx=(px-cx)/rx,dy=(py-cy)/ry;
                puff=Math.Max(puff,Math.Exp(-(dx*dx+dy*dy)*1.7d));
            }
            return (float)Math.Max(0d,Math.Min(1d,puff));
        }
        // Expansion only increases the clear area: cloud animation never conceals the pet or its next jump.
        public static float EdgeExpansion(float angle,float time)
        {
            return .16f+(float)Math.Sin(angle*7f+time*.18f)*.09f+(float)Math.Sin(angle*11f-time*.13f)*.05f;
        }
    }

}
