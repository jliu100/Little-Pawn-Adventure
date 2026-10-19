using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws.Maze
{
    public enum MazeTile { Wall, Floor, Mud }

    public sealed class MazeLevel
    {
        public int Number, Width, Height;
        public Vector2Int Start, Goal;
        public MazeTile[,] Tiles;
        public readonly List<SnakePatrol> Snakes = new List<SnakePatrol>();
        public MazeTile TileAt(Vector2Int cell)
        {
            return cell.x < 0 || cell.y < 0 || cell.x >= Width || cell.y >= Height
                ? MazeTile.Wall : Tiles[cell.x, cell.y];
        }
        public int MudCount
        {
            get
            {
                int count = 0;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        if (Tiles[x, y] == MazeTile.Mud) count++;
                return count;
            }
        }
    }

    // Straight, dry patrols move continuously from one corridor edge to the other.
    public sealed class SnakePatrol
    {
        public Vector2Int From, To;
        public float CellsPerSecond, PhaseSeconds;

        public Vector2 PositionAt(float elapsedSeconds)
        {
            Vector2 start = new Vector2(From.x, From.y);
            Vector2 end = new Vector2(To.x, To.y);
            float distance = Vector2.Distance(start, end);
            if (distance < 0.0001f || CellsPerSecond <= 0f) return start;
            double travel = Math.Max(0d, (double)elapsedSeconds + PhaseSeconds) * CellsPerSecond;
            double phase = travel % (distance * 2d);
            float fraction = (float)(phase <= distance ? phase / distance : 2d - phase / distance);
            return start + (end - start) * fraction;
        }

        public Vector2 DirectionAt(float elapsedSeconds)
        {
            Vector2 delta = new Vector2(To.x - From.x, To.y - From.y);
            float distance = delta.magnitude;
            if (distance < 0.0001f || CellsPerSecond <= 0f) return Vector2.zero;
            double travel = Math.Max(0d, (double)elapsedSeconds + PhaseSeconds) * CellsPerSecond;
            return delta / distance * (travel % (distance * 2d) < distance ? 1f : -1f);
        }
    }

    public static class MazeHazards
    {
        // Sweep relative motion instead of testing only endpoints: catches a pet
        // swapping places with a snake or crossing its path between rendered frames.
        public static bool Touches(Vector2 petFrom, Vector2 petTo, Vector2 snakeFrom, Vector2 snakeTo, float radius)
        {
            Vector2 relative = petFrom - snakeFrom;
            Vector2 motion = (petTo - petFrom) - (snakeTo - snakeFrom);
            float lengthSquared = motion.sqrMagnitude;
            float closest = lengthSquared < 0.00000001f ? 0f
                : Math.Max(0f, Math.Min(1f, -Vector2.Dot(relative, motion) / lengthSquared));
            Vector2 separation = relative + motion * closest;
            float safeRadius = Math.Max(0f, radius);
            return separation.sqrMagnitude <= safeRadius * safeRadius;
        }
    }

    public static class MazeLevels
    {
        public const int Count = 50;
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        // Fresh, deterministic data: callers may render or inspect a level without
        // sharing mutable tile arrays with another play session.
        public static MazeLevel Get(int number)
        {
            if (number < 1 || number > Count)
                throw new ArgumentOutOfRangeException("number", "Maze levels range from 1 to 50.");
            if (number == 1) return FirstLevel();
            if (number == 6) return SnakeTutorial();

            int width, height;
            if (number == 2) { width = 11; height = 9; }
            else if (number == 3) { width = 15; height = 11; }
            else if (number == 4) { width = 19; height = 13; }
            else if (number == 5 || number == 6) { width = 21; height = 15; }
            else if (number == 7) { width = 23; height = 15; }
            else if (number == 8) { width = 23; height = 17; }
            else if (number == 9) { width = 25; height = 17; }
            else { width = 25 + 2 * ((number - 10) / 5); height = 19 + 2 * ((number - 10) / 7); }

            var level = new MazeLevel
            {
                Number = number, Width = width, Height = height,
                Start = new Vector2Int(1, 1), Goal = new Vector2Int(width - 2, height - 2),
                Tiles = new MazeTile[width, height]
            };
            var random = new System.Random(17431 + number * 7919);
            var stack = new Stack<Vector2Int>();
            stack.Push(level.Start);
            level.Tiles[1, 1] = MazeTile.Floor;
            while (stack.Count > 0)
            {
                Vector2Int current = stack.Peek();
                var choices = new List<Vector2Int>(4);
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction * 2;
                    if (next.x > 0 && next.y > 0 && next.x < width - 1 && next.y < height - 1
                        && level.TileAt(next) == MazeTile.Wall) choices.Add(direction);
                }
                if (choices.Count == 0) { stack.Pop(); continue; }
                Vector2Int selected = choices[random.Next(choices.Count)];
                level.Tiles[current.x + selected.x, current.y + selected.y] = MazeTile.Floor;
                Vector2Int destination = current + selected * 2;
                level.Tiles[destination.x, destination.y] = MazeTile.Floor;
                stack.Push(destination);
            }
            if (number >= 7) AddSnakes(level, number <= 10 ? Math.Min(3, number - 6) : Math.Min(4, 2 + (number - 10) / 10));
            return level;
        }

        public static bool TryStep(MazeLevel level, Vector2Int from, Vector2Int direction, out Vector2Int landing)
        {
            landing = from;
            if (level == null || Math.Abs(direction.x) + Math.Abs(direction.y) != 1
                || level.TileAt(from) != MazeTile.Floor) return false;
            Vector2Int adjacent = from + direction;
            if (level.TileAt(adjacent) != MazeTile.Floor) return false;
            landing = adjacent;
            return true;
        }

        // Kept for existing callers. Snake avoidance uses ordinary movement;
        // the old mud-jump action is deliberately no longer available.
        public static bool TryStep(MazeLevel level, Vector2Int from, Vector2Int direction, bool jump, out Vector2Int landing)
        {
            landing = from;
            return !jump && TryStep(level, from, direction, out landing);
        }

        private static MazeLevel SnakeTutorial()
        {
            string[] rows =
            {
                "###########",
                "#####.....#",
                "#####.#####",
                "#####.#####",
                "#.........#",
                "####...####",
                "#####.#####",
                "#.....#####",
                "###########"
            };
            var level = new MazeLevel
            {
                Number = 6, Width = 11, Height = 9,
                Start = new Vector2Int(1, 1), Goal = new Vector2Int(9, 7),
                Tiles = new MazeTile[11, 9]
            };
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                    level.Tiles[x, y] = rows[level.Height - 1 - y][x] == '#' ? MazeTile.Wall : MazeTile.Floor;
            level.Snakes.Add(new SnakePatrol
            {
                From = new Vector2Int(1, 4), To = new Vector2Int(9, 4),
                CellsPerSecond = 0.75f, PhaseSeconds = 0f
            });
            return level;
        }

        private static MazeLevel FirstLevel()
        {
            // Rows are written top-to-bottom; game grid coordinates grow upward.
            string[] rows =
            {
                "#########",
                "#####...#",
                "#####.#.#",
                "#.....#.#",
                "#.#####.#",
                "#...###.#",
                "#########"
            };
            var level = new MazeLevel
            {
                Number = 1, Width = 9, Height = 7,
                Start = new Vector2Int(1, 1), Goal = new Vector2Int(7, 5),
                Tiles = new MazeTile[9, 7]
            };
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                    level.Tiles[x, y] = rows[level.Height - 1 - y][x] == '#' ? MazeTile.Wall : MazeTile.Floor;
            return level;
        }

        private static void AddSnakes(MazeLevel level, int requested)
        {
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(level.Start);
            parents[level.Start] = level.Start;
            while (queue.Count > 0 && !parents.ContainsKey(level.Goal))
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = cell + direction;
                    if (level.TileAt(next) != MazeTile.Floor || parents.ContainsKey(next)) continue;
                    parents[next] = cell;
                    queue.Enqueue(next);
                }
            }
            var path = new List<Vector2Int>();
            Vector2Int cursor = level.Goal;
            while (cursor != level.Start) { path.Add(cursor); cursor = parents[cursor]; }
            path.Reverse();
            var candidates = new List<SnakePatrol>();
            var used = new HashSet<string>();
            foreach (Vector2Int cell in path)
            {
                if (cell == level.Goal) continue;
                foreach (Vector2Int direction in new[] { Vector2Int.right, Vector2Int.up })
                {
                    Vector2Int from = cell, to = cell;
                    for (int i = 0; i < 3 && SafePatrolCell(level, from - direction); i++) from -= direction;
                    for (int i = 0; i < 3 && SafePatrolCell(level, to + direction); i++) to += direction;
                    int length = Math.Abs(to.x - from.x) + Math.Abs(to.y - from.y);
                    if (length < 2) continue;
                    string key = from.x + "," + from.y + ":" + to.x + "," + to.y;
                    if (!used.Add(key)) continue;
                    candidates.Add(new SnakePatrol { From = from, To = to });
                }
            }
            int count = Math.Min(requested, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = ((i + 1) * candidates.Count) / (count + 1);
                SnakePatrol patrol = candidates[index];
                patrol.CellsPerSecond = Math.Min(1.25f, 0.75f + (level.Number - 7) * 0.012f);
                patrol.PhaseSeconds = (i * 1.73f) + level.Number * 0.11f;
                level.Snakes.Add(patrol);
            }
        }

        private static bool SafePatrolCell(MazeLevel level, Vector2Int cell)
        {
            return cell != level.Start && cell != level.Goal && level.TileAt(cell) == MazeTile.Floor;
        }
    }
}
