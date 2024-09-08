using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using RedUtils.Math;

namespace RedUtils
{
    public class BoostNetwork
    {
        private const int PadZ = 33;

        record Node(
            int Index,
            Vec3 Location,
            int? BoostPadIndex
        )
        {
            public readonly List<int> Neighbours = new() { -2 };
            public int from;
            public float cost;
        };

        private List<Node> _nodes = new()
        {
            new(0, new(0f, -4240f, PadZ), 0),
            new(1, new(-1792f, -4184f, PadZ), 1),
            new(2, new(1792f, -4184f, PadZ), 2),
            new(3, new(-3072f, -4096f, PadZ), 3),
            new(4, new(3072f, -4096f, PadZ), 4),
            new(5, new(-940f, -3308f, PadZ), 5),
            new(6, new(940f, -3308f, PadZ), 6),
            new(7, new(0f, -2816f, PadZ), 7),
            new(8, new(-3584f, -2484f, PadZ), 8),
            new(9, new(3584f, -2484f, PadZ), 9),
            new(10, new(-1788f, -2300f, PadZ), 10),
            new(11, new(1788f, -2300f, PadZ), 11),
            new(12, new(-2048f, -1036f, PadZ), 12),
            new(13, new(0f, -1024f, PadZ), 13),
            new(14, new(2048f, -1036f, PadZ), 14),
            new(15, new(-3584f, 0f, PadZ), 15),
            new(16, new(-1024f, 0f, PadZ), 16),
            new(17, new(1024f, 0f, PadZ), 17),
            new(18, new(3584f, 0f, PadZ), 18),
            new(19, new(-2048f, 1036f, PadZ), 19),
            new(20, new(0f, 1024f, PadZ), 20),
            new(21, new(2048f, 1036f, PadZ), 21),
            new(22, new(-1788f, 2300f, PadZ), 22),
            new(23, new(1788f, 2300f, PadZ), 23),
            new(24, new(-3584f, 2484f, PadZ), 24),
            new(25, new(3584f, 2484f, PadZ), 25),
            new(26, new(0f, 2816f, PadZ), 26),
            new(27, new(-940f, 3310f, PadZ), 27),
            new(28, new(940f, 3308f, PadZ), 28),
            new(29, new(-3072f, 4096f, PadZ), 29),
            new(30, new(3072f, 4096f, PadZ), 30),
            new(31, new(-1792f, 4184f, PadZ), 31),
            new(32, new(1792f, 4184f, PadZ), 32),
            new(33, new(0f, 4240f, PadZ), 33),
        };

        private void AddEdge(int i, int j)
        {
            _nodes[i].Neighbours.Add(j);
            _nodes[j].Neighbours.Add(i);
        }

        public BoostNetwork()
        {
            AddEdge(0, 1);
            AddEdge(0, 2);
            AddEdge(0, 5);
            AddEdge(0, 6);
            AddEdge(0, 7);
            AddEdge(1, 3);
            AddEdge(1, 5);
            AddEdge(2, 4);
            AddEdge(2, 6);
            AddEdge(3, 8);
            AddEdge(4, 9);
            AddEdge(5, 7);
            AddEdge(5, 10);
            AddEdge(6, 7);
            AddEdge(6, 11);
            AddEdge(7, 10);
            AddEdge(7, 11);
            AddEdge(7, 12);
            AddEdge(7, 13);
            AddEdge(7, 14);
            AddEdge(8, 10);
            AddEdge(8, 12);
            AddEdge(8, 15);
            AddEdge(9, 11);
            AddEdge(9, 14);
            AddEdge(9, 18);
            AddEdge(10, 12);
            AddEdge(10, 13);
            AddEdge(10, 16);
            AddEdge(11, 13);
            AddEdge(11, 14);
            AddEdge(11, 17);
            AddEdge(12, 13);
            AddEdge(12, 15);
            AddEdge(12, 16);
            AddEdge(12, 19);
            AddEdge(13, 16);
            AddEdge(13, 17);
            AddEdge(13, 20);
            AddEdge(14, 13);
            AddEdge(14, 17);
            AddEdge(14, 18);
            AddEdge(14, 21);
            AddEdge(15, 16);
            AddEdge(15, 19);
            AddEdge(15, 24);
            AddEdge(16, 19);
            AddEdge(16, 20);
            AddEdge(16, 22);
            AddEdge(17, 18);
            AddEdge(17, 20);
            AddEdge(17, 21);
            AddEdge(17, 23);
            AddEdge(18, 21);
            AddEdge(18, 25);
            AddEdge(19, 20);
            AddEdge(19, 22);
            AddEdge(19, 26);
            AddEdge(19, 24);
            AddEdge(20, 21);
            AddEdge(20, 22);
            AddEdge(20, 23);
            AddEdge(20, 26);
            AddEdge(21, 23);
            AddEdge(21, 25);
            AddEdge(21, 26);
            AddEdge(22, 24);
            AddEdge(22, 26);
            AddEdge(22, 27);
            AddEdge(23, 25);
            AddEdge(23, 26);
            AddEdge(23, 28);
            AddEdge(24, 29);
            AddEdge(25, 30);
            AddEdge(26, 27);
            AddEdge(26, 28);
            AddEdge(26, 33);
            AddEdge(27, 31);
            AddEdge(27, 33);
            AddEdge(28, 32);
            AddEdge(28, 33);
            AddEdge(29, 31);
            AddEdge(30, 32);
            AddEdge(31, 33);
            AddEdge(32, 33);
        }

        public void Draw(ExtendedRenderer draw)
        {
            draw.Color = Color.Chocolate;
            foreach (Node node in _nodes)
            {
                foreach (int neighbourIndex in node.Neighbours)
                {
                    if (node.Index < neighbourIndex)
                    {
                        draw.Line3D(node.Location, _nodes[neighbourIndex].Location);
                    }
                }
            }
        }

        public List<Vec3> FindRotation(Car car, Vec3 end)
        {
            // A* algorithm
            Vec3 start = car.Location;
            
            foreach (Node node in _nodes)
            {
                node.cost = float.MaxValue;
                node.from = int.MinValue;
            }

            // Special nodes:
            // -1 is start
            // -2 is end
            
            PriorityQueue<int, float> queue = new();
            queue.Enqueue(-1, 0);
            Node endNode = new Node(-2, end, null);
            endNode.cost = float.MaxValue;
            endNode.from = int.MaxValue;
            
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                
                if (index == -2)
                {
                    // Done. Build path
                    List<Vec3> path = new();
                    path.Add(end);
                    int prev = endNode.from;
                    while (prev != -1)
                    {
                        path.Add(_nodes[prev].Location);
                        prev = _nodes[prev].from;
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                List<int> neighbours = index != -1
                    ? _nodes[index].Neighbours
                    // Start neighbours
                    : _nodes.Where(n => 700f * Utils.Cap(car.Forward.Angle(n.Location - start) * 0.99f, 0f, 1f) < start.Dist(n.Location) && start.Dist(n.Location) < 2000f)
                        .Select(n => n.Index)
                        .ToList();
                if (index == -1) neighbours.Add(-2);

                foreach (int nid in neighbours)
                {
                    Vec3 prevLoc = index == -1 ? start : _nodes[index].Location;
                    Vec3 neiLoc = nid == -2 ? end : _nodes[nid].Location;
                    
                    float dist = prevLoc.Dist(neiLoc);
                    
                    // Bonus for driving over boost pads, penalty for not
                    int? boostId = nid == -2 ? null : _nodes[nid].BoostPadIndex;
                    if (boostId == null) dist *= 1.5f;
                    else if (Field.Boosts[(int)boostId].IsLarge) dist *= 1.0f;
                    else dist *= 1.15f;
                    
                    // Avoid the ball
                    Vec3 ballProj = Ball.Location.ProjToLineSegment(prevLoc, neiLoc);
                    float ballDist2 = Ball.Location.DistSquared(ballProj);
                    float ballPenalty = 1150f * 750f * 750f / (ballDist2 * ballDist2); // 600 at dist=1000, 1600 at dist=600, 6000 at dist=300

                    // Avoid allies
                    float allyPenalty = 0f;
                    foreach (Car ally in Cars.AlliesNotMe)
                    {
                        Vec3 soonLoc = ally.Location + ally.Velocity * 0.1f;
                        Vec3 allyProj = soonLoc.ProjToLineSegment(prevLoc, neiLoc);
                        float allyDist2 = ally.Location.DistSquared(allyProj);
                        allyPenalty += 300f * 750f * 750f / (allyDist2 * allyDist2); // 160 at dist=1000, 400 at dist=600, 1600 at dist=300
                    }
                    
                    float newCost = index == -1 ? 0f : _nodes[index].cost; // Cost so far ...
                    newCost += dist;
                    newCost += ballPenalty;
                    
                    float neiOldCost = nid == -2 ? endNode.cost : _nodes[nid].cost;
                    if (newCost < neiOldCost)
                    {
                        if (nid != -2)
                        {
                            _nodes[nid].from = index;
                            _nodes[nid].cost = newCost;
                        }
                        else
                        {
                            endNode.from = index;
                            endNode.cost = newCost;
                        }
                        float heuristic = neiLoc.Dist(end);
                        queue.Enqueue(nid, newCost + heuristic);
                    }
                }
            }

            return new List<Vec3> { start, end };
        }
    }
}
