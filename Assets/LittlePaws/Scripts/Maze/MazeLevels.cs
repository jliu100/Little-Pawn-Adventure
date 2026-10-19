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

    // A side-corridor patrol reaches one crossing, then retreats. The main
    // corridor cells before and after that crossing are always safe to wait on.
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
            if (number == 25) return CloudMaze();

            int width, height;
            if (number == 2) { width = 11; height = 9; }
            else if (number == 3) { width = 15; height = 11; }
            else if (number == 4) { width = 19; height = 13; }
            else if (number == 5) { width = 21; height = 15; }
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
            if (number >= 10 && number <= 15) AddMud(level, 2 + (number - 10) / 2);
            else if (number >= 26 && number <= 34) AddMud(level, 3 + (number - 26) / 3);
            return level;
        }

        public static bool TryStep(MazeLevel level, Vector2Int from, Vector2Int direction, out Vector2Int landing)
        {
            landing = from;
            if (level == null || Math.Abs(direction.x) + Math.Abs(direction.y) != 1
                || !IsWalkable(level.TileAt(from))) return false;
            Vector2Int adjacent = from + direction;
            if (!IsWalkable(level.TileAt(adjacent))) return false;
            landing = adjacent;
            return true;
        }

        public static bool IsWalkable(MazeTile tile) => tile == MazeTile.Floor || tile == MazeTile.Mud;

        // A jump crosses two floor cells. Both the crossed cell and the landing
        // must be walkable, so it can never cut through a hedge or map boundary.
        public static bool TryStep(MazeLevel level, Vector2Int from, Vector2Int direction, bool jump, out Vector2Int landing)
        {
            if (!jump) return TryStep(level, from, direction, out landing);
            landing = from;
            if (!TryStep(level, from, direction, out var middle)
                || !TryStep(level, middle, direction, out var destination)) return false;
            landing = destination;
            return true;
        }

        // A compact medium maze combines the established hazards under white clouds.
        // Snake patrols touch only one route cell. Puddles have two dry tiles on each side.
        private static MazeLevel CloudMaze()
        {
            string[] rows =
            {
                "#####################",
                "#########.....#.....#",
                "#############...#####",
                "#.....#########...#.#",
                "#.###############...#",
                "#.#.........#####.###",
                "#...###.###.###.#...#",
                "###...#...#.#.#.#.###",
                "###.#######M#.#...###",
                "###.....###...###.###",
                "#######.###.#####.###",
                "##....#...#.......###",
                "##.####.#.####.######",
                "#...M...#.####.....##",
                "#####################"
            };
            var level=new MazeLevel
            {
                Number=25,Width=21,Height=15,Start=new Vector2Int(1,1),Goal=new Vector2Int(19,13),
                Tiles=new MazeTile[21,15]
            };
            for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
            {
                char tile=rows[level.Height-1-y][x];
                level.Tiles[x,y]=tile=='#'?MazeTile.Wall:tile=='M'?MazeTile.Mud:MazeTile.Floor;
            }
            level.Snakes.Add(new SnakePatrol{From=new Vector2Int(5,7),To=new Vector2Int(3,7),CellsPerSecond=.85f,PhaseSeconds=0f});
            level.Snakes.Add(new SnakePatrol{From=new Vector2Int(19,8),To=new Vector2Int(17,8),CellsPerSecond=.95f,PhaseSeconds=1.1f});
            return level;
        }

        private static MazeLevel SnakeTutorial()
        {
            string[] rows =
            {
                "###########",
                "#####.....#",
                "#####.#####",
                "#####.#####",
                "###...#####",
                "#####.#####",
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
                From = new Vector2Int(3, 4), To = new Vector2Int(5, 4),
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
            var route = new HashSet<Vector2Int>(path);
            route.Add(level.Start);
            var used = new List<Vector2Int>();
            for (int i = 0; i < requested; i++)
            {
                int target = ((i + 1) * path.Count) / (requested + 1);
                var order = new List<int>();
                for (int j = 1; j + 1 < path.Count; j++) order.Add(j);
                order.Sort((a, b) => {
                    int comparison = Math.Abs(a - target).CompareTo(Math.Abs(b - target));
                    return comparison != 0 ? comparison : a.CompareTo(b);
                });
                bool placed = false;
                foreach (int j in order)
                {
                    Vector2Int crossing = path[j], approach = crossing - path[j - 1];
                    if (path[j + 1] - crossing != approach) continue;
                    if (Nearby(crossing, level.Start, 3) || Nearby(crossing, level.Goal, 3)) continue;
                    bool crowded = false;
                    foreach (Vector2Int previous in used)
                        if (Nearby(crossing, previous, 4)) { crowded = true; break; }
                    if (crowded) continue;
                    Vector2Int side = new Vector2Int(-approach.y, approach.x);
                    for (int sign = 1; sign >= -1; sign -= 2)
                    {
                        Vector2Int middle = crossing + side * sign;
                        Vector2Int end = crossing + side * (2 * sign);
                        if (!PatrolSideCell(level, route, middle) || !PatrolSideCell(level, route, end)) continue;
                        // Only these two cells are added, and the snake walks both.
                        // No passing lane or open room: A -> B -> C stays narrow.
                        level.Tiles[middle.x, middle.y] = MazeTile.Floor;
                        level.Tiles[end.x, end.y] = MazeTile.Floor;
                        level.Snakes.Add(new SnakePatrol
                        {
                            From = end, To = crossing,
                            CellsPerSecond = Math.Min(1.25f, 0.75f + (level.Number - 7) * 0.012f),
                            PhaseSeconds = (i * 1.73f) + level.Number * 0.11f
                        });
                        used.Add(crossing); placed = true; break;
                    }
                    if (placed) break;
                }
            }
        }

        private static void AddMud(MazeLevel level, int requested)
        {
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(level.Start); parents[level.Start] = level.Start;
            while (queue.Count > 0 && !parents.ContainsKey(level.Goal))
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = cell + direction;
                    if (!IsWalkable(level.TileAt(next)) || parents.ContainsKey(next)) continue;
                    parents[next] = cell; queue.Enqueue(next);
                }
            }
            var path = new List<Vector2Int>();
            for (Vector2Int cell = level.Goal; ; cell = parents[cell])
            { path.Add(cell); if (cell == level.Start) break; }
            path.Reverse();
            var selected = new List<Vector2Int>();
            for (int i = 0; i < requested; i++)
            {
                int target = ((i + 1) * path.Count) / (requested + 1), best = -1, distance = int.MaxValue;
                for (int j = 3; j < path.Count - 3; j++)
                {
                    Vector2Int cell = path[j], direction = cell - path[j - 1];
                    // Leave a straight run-up and run-out. A single dry tile at
                    // a corner is not enough: turning normally also takes a step.
                    if (cell - path[j - 2] != direction * 2 || path[j + 1] - cell != direction
                        || path[j + 2] - cell != direction * 2) continue;
                    bool safe = true;
                    for (int offset = -2; offset <= 2; offset++)
                        if (level.TileAt(path[j + offset]) != MazeTile.Floor) safe = false;
                    foreach (SnakePatrol snake in level.Snakes)
                    {
                        Vector2Int step = new Vector2Int(Math.Sign(snake.To.x - snake.From.x), Math.Sign(snake.To.y - snake.From.y));
                        for (Vector2Int at = snake.From; ; at += step)
                        { if (Nearby(cell, at, 2)) safe = false; if (at == snake.To) break; }
                    }
                    foreach (Vector2Int other in selected)
                        if (Nearby(cell, other, 3)) safe = false;
                    int candidateDistance = Math.Abs(j - target);
                    if (safe && candidateDistance < distance) { best = j; distance = candidateDistance; }
                }
                if (best < 0) break;
                Vector2Int puddle = path[best];
                level.Tiles[puddle.x, puddle.y] = MazeTile.Mud; selected.Add(puddle);
            }
        }

        private static bool PatrolSideCell(MazeLevel level, HashSet<Vector2Int> route, Vector2Int cell)
        {
            return cell.x > 0 && cell.y > 0 && cell.x < level.Width - 1 && cell.y < level.Height - 1
                && !route.Contains(cell);
        }

        private static bool Nearby(Vector2Int a, Vector2Int b, int radius)
        {
            return Math.Abs(a.x - b.x) <= radius && Math.Abs(a.y - b.y) <= radius;
        }

    }
    public static class MazeMovementRules
    {
        public const float WalkSeconds = .13f, JumpSeconds = .38f, JumpCooldown = .65f;
        public const float MudSlowSeconds = 3f, MudSpeedMultiplier = .5f;
        public const float TakeoffGroundedUntil = .18f, LandingGroundedFrom = .82f;
        public static float StepSeconds(bool muddy) => WalkSeconds / (muddy ? MudSpeedMultiplier : 1f);
        public static float JumpHeight(float progress)
        {
            if (progress <= 0f || progress >= 1f) return 0f;
            return (float)Math.Sin(progress * Math.PI) * .75f;
        }
    }

    // Both walking and jumping report the tile actually landed on. Crossing mud
    // in the air never calls LandOn for the skipped tile.
    public sealed class MazeMudEffect
    {
        private float until;
        public void LandOn(MazeTile tile,float now)
        { if(tile==MazeTile.Mud)until=now+MazeMovementRules.MudSlowSeconds; }
        public float Remaining(float now) => Math.Max(0f,until-now);
        public bool IsSlowed(float now) => Remaining(now)>0f;
        public void Clear() { until=0f; }
    }

}
