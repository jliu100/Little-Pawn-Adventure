using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws
{
    /// <summary>Routes a pet across a rectangular floor while keeping its radius clear of furniture.</summary>
    public sealed class FloorPathfinder
    {
        private const float Epsilon = 0.0001f;
        private readonly float minX, maxX, minZ, maxZ, spacing;
        private readonly int columns, rows;
        private readonly List<Bounds> obstacles = new List<Bounds>();
        private readonly Vector3[] points;
        private readonly bool[] walkable;

        public FloorPathfinder(Bounds floorBounds, IList<Bounds> obstacles,
            float radius = .48f, float cellSize = .25f)
        {
            radius = Mathf.Max(0f, radius);
            spacing = Mathf.Max(.05f, cellSize);
            minX = floorBounds.min.x + radius;
            maxX = floorBounds.max.x - radius;
            minZ = floorBounds.min.z + radius;
            maxZ = floorBounds.max.z - radius;

            if (obstacles != null)
            {
                foreach (Bounds original in obstacles)
                {
                    Bounds expanded = original;
                    expanded.Expand(new Vector3(radius * 2f, 0f, radius * 2f));
                    this.obstacles.Add(expanded);
                }
            }

            columns = maxX >= minX ? Mathf.FloorToInt((maxX - minX) / spacing) + 1 : 0;
            rows = maxZ >= minZ ? Mathf.FloorToInt((maxZ - minZ) / spacing) + 1 : 0;
            points = new Vector3[columns * rows];
            walkable = new bool[points.Length];
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < columns; x++)
            {
                int index = z * columns + x;
                points[index] = new Vector3(minX + x * spacing, 0f, minZ + z * spacing);
                walkable[index] = IsWalkable(points[index]);
            }
        }

        public bool IsWalkable(Vector3 point)
        {
            if (float.IsNaN(point.x) || float.IsNaN(point.z) ||
                float.IsInfinity(point.x) || float.IsInfinity(point.z) ||
                point.x < minX || point.x > maxX || point.z < minZ || point.z > maxZ)
                return false;

            foreach (Bounds obstacle in obstacles)
            {
                if (point.x >= obstacle.min.x - Epsilon && point.x <= obstacle.max.x + Epsilon &&
                    point.z >= obstacle.min.z - Epsilon && point.z <= obstacle.max.z + Epsilon)
                    return false;
            }
            return true;
        }

        public bool TryPath(Vector3 from, Vector3 destination, out List<Vector3> path)
        {
            path = new List<Vector3>();
            from.y = destination.y = 0f;
            // Reject blocked and outside taps instead of moving their destination.
            if (!IsWalkable(from) || !IsWalkable(destination)) return false;
            if (SegmentClear(from, destination))
            {
                path.Add(destination);
                return true;
            }

            int count = points.Length;
            float[] costs = new float[count];
            int[] previous = new int[count];
            bool[] closed = new bool[count];
            for (int i = 0; i < count; i++)
            {
                costs[i] = float.PositiveInfinity;
                previous[i] = -1;
                // Visible starting nodes allow a new tap midway along any previous route.
                if (walkable[i] && SegmentClear(from, points[i]))
                    costs[i] = Vector3.Distance(from, points[i]);
            }

            int reached = -1;
            while (true)
            {
                int current = -1;
                float bestScore = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    if (closed[i] || float.IsPositiveInfinity(costs[i])) continue;
                    float score = costs[i] + Vector3.Distance(points[i], destination);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        current = i;
                    }
                }
                if (current < 0) break;
                if (SegmentClear(points[current], destination))
                {
                    reached = current;
                    break;
                }

                closed[current] = true;
                int currentX = current % columns;
                int currentZ = current / columns;
                for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int x = currentX + dx, z = currentZ + dz;
                    if (x < 0 || x >= columns || z < 0 || z >= rows) continue;
                    int next = z * columns + x;
                    if (!walkable[next] || closed[next]) continue;
                    // Do not cut diagonally between blocked grid cells.
                    if (dx != 0 && dz != 0 &&
                        (!walkable[currentZ * columns + x] || !walkable[z * columns + currentX]))
                        continue;
                    if (!SegmentClear(points[current], points[next])) continue;
                    float cost = costs[current] + Vector3.Distance(points[current], points[next]);
                    if (cost >= costs[next]) continue;
                    costs[next] = cost;
                    previous[next] = current;
                }
            }

            if (reached < 0) return false;
            var reversed = new List<Vector3>();
            for (int node = reached; node >= 0; node = previous[node])
                reversed.Add(points[node]);
            reversed.Reverse();
            reversed.Add(destination);

            // Remove grid zigzags only when the entire replacement segment is safe.
            Vector3 anchor = from;
            int cursor = 0;
            while (cursor < reversed.Count)
            {
                int furthest = reversed.Count - 1;
                while (furthest > cursor && !SegmentClear(anchor, reversed[furthest])) furthest--;
                if (!SegmentClear(anchor, reversed[furthest]))
                {
                    path.Clear();
                    return false;
                }
                path.Add(reversed[furthest]);
                anchor = reversed[furthest];
                cursor = furthest + 1;
            }
            return true;
        }

        private bool SegmentClear(Vector3 from, Vector3 to)
        {
            if (!IsWalkable(from) || !IsWalkable(to)) return false;
            foreach (Bounds obstacle in obstacles)
            {
                float entry = 0f, exit = 1f;
                if (IntersectAxis(from.x, to.x - from.x, obstacle.min.x - Epsilon,
                        obstacle.max.x + Epsilon, ref entry, ref exit) &&
                    IntersectAxis(from.z, to.z - from.z, obstacle.min.z - Epsilon,
                        obstacle.max.z + Epsilon, ref entry, ref exit))
                    return false;
            }
            // The inset floor is convex, so endpoints inside it imply the segment is inside.
            return true;
        }

        private static bool IntersectAxis(float origin, float direction, float minimum, float maximum,
            ref float entry, ref float exit)
        {
            if (Mathf.Abs(direction) < Epsilon) return origin >= minimum && origin <= maximum;
            float a = (minimum - origin) / direction;
            float b = (maximum - origin) / direction;
            entry = Mathf.Max(entry, Mathf.Min(a, b));
            exit = Mathf.Min(exit, Mathf.Max(a, b));
            return entry <= exit;
        }
    }
}
