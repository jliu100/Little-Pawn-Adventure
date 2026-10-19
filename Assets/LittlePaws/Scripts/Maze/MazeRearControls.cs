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
        public static Vector2Int InitialFacing(MazeLevel level)
        {
            foreach(var direction in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
                if(MazeLevels.TryStep(level,level.Start,direction,out _))return direction;
            return Vector2Int.up;
        }
    }
}
