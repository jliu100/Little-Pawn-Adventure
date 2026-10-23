using System;
using UnityEngine;

namespace LittlePaws.Maze
{
    public static class MazeRearControls
    {
        public const float WalkSeconds=.34f, TurnRepeatSeconds=.3f;
        public static bool AppliesTo(int level) => level>=35 && level<=MazeLevels.Count;
        public static Vector2Int TurnLeft(Vector2Int facing) => new Vector2Int(-facing.y,facing.x);
        public static Vector2Int TurnRight(Vector2Int facing) => new Vector2Int(facing.y,-facing.x);
        public static bool IsForward(Vector2Int facing,Vector2Int movement) => facing==movement;
        public static Vector2 MapPosition(Vector2 cell,MazeLevel level,Vector2 size)
        {
            return new Vector2(((cell.x+.5f)/level.Width-.5f)*size.x,((cell.y+.5f)/level.Height-.5f)*size.y);
        }
        // Grid-space camera collision includes the near plane's width, so neither
        // the camera nor its visible edges can cross a hedge during a turn.
        public const float CameraWallClearance=.1f;
        // Look down the route from above the pet's head, even when a rear wall
        // pushes the camera forward. Distances here are world metres.
        public const float CameraLookAhead=1.2f, CameraAimHeight=.5f;
        public static float CameraHeight(float distanceMetres) => 2.9f+.95f*distanceMetres;
        public static float CameraFieldOfView(float distanceMetres)
        {
            float blend=Math.Max(0,Math.Min(1,(distanceMetres-1f)/2.9f));
            return 72f-10f*blend;
        }
        public static float CameraDistance(MazeLevel level,Vector2 origin,Vector2 backwards,float desiredDistance)
        {
            float nearest=desiredDistance;
            float half=.5f+CameraWallClearance;
            for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
            {
                if(level.Tiles[x,y]!=MazeTile.Wall)continue;
                float enter=0,exit=nearest;
                if(ClipCameraAxis(origin.x,backwards.x,x-half,x+half,ref enter,ref exit)
                    &&ClipCameraAxis(origin.y,backwards.y,y-half,y+half,ref enter,ref exit))
                    nearest=Math.Max(0,enter-.001f);
            }
            return nearest;
        }
        private static bool ClipCameraAxis(float origin,float direction,float min,float max,ref float enter,ref float exit)
        {
            if(Math.Abs(direction)<.000001f)return origin>=min&&origin<=max;
            float first=(min-origin)/direction,last=(max-origin)/direction;
            if(first>last){float swap=first;first=last;last=swap;}
            enter=Math.Max(enter,first);exit=Math.Min(exit,last);
            return enter<=exit;
        }
        public static Vector2Int InitialFacing(MazeLevel level)
        {
            foreach(var direction in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
                if(MazeLevels.TryStep(level,level.Start,direction,out _))return direction;
            return Vector2Int.up;
        }
    }
}
