using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalyzer.Core.Models;
using SocialNetworkAnalyzer.Core.Validation;
using SocialNetworkAnalyzer.Core.Weights;

namespace SocialNetworkAnalyzer.Core.Algorithms;

public static class ShortestPaths
{
    public static (List<int> Path, double Cost) Dijkstra(
        Graph graph,
        int startId,
        int targetId,
        IWeightCalculator weights)
    {
        if (!graph.Nodes.ContainsKey(startId))
            throw new GraphValidationException($"Başlangıç node yok: {startId}");
        if (!graph.Nodes.ContainsKey(targetId))
            throw new GraphValidationException($"Hedef node yok: {targetId}");

        var dist = new Dictionary<int, double>();
        var prev = new Dictionary<int, int?>();

        foreach (var id in graph.Nodes.Keys)
        {
            dist[id] = double.PositiveInfinity;
            prev[id] = null;
        }

        dist[startId] = 0;

        // .NET 6+ PriorityQueue var
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(startId, 0);

        while (pq.Count > 0)
        {
            var v = pq.Dequeue();

            if (v == targetId) break;

            var dv = dist[v];
            if (double.IsPositiveInfinity(dv)) break;

            foreach (var nb in graph.GetNeighbors(v))
            {
                var w = weights.GetWeight(graph, v, nb); // dinamik ağırlık
                var alt = dv + w;

                if (alt < dist[nb])
                {
                    dist[nb] = alt;
                    prev[nb] = v;
                    pq.Enqueue(nb, alt);
                }
            }
        }

        if (double.IsPositiveInfinity(dist[targetId]))
            return (new List<int>(), double.PositiveInfinity);

        // Path reconstruct
        var path = new List<int>();
        int cur = targetId;
        while (true)
        {
            path.Add(cur);
            if (cur == startId) break;

            var p = prev[cur];
            if (p is null) break; // güvenlik
            cur = p.Value;
        }
        path.Reverse();

        return (path, dist[targetId]);
    }

    public static (List<int> Path, double Cost) AStar(
    Graph graph,
    int startId,
    int targetId,
    IWeightCalculator weights)
    {
        if (!graph.Nodes.ContainsKey(startId))
            throw new GraphValidationException($"Başlangıç node yok: {startId}");
        if (!graph.Nodes.ContainsKey(targetId))
            throw new GraphValidationException($"Hedef node yok: {targetId}");

        // Heuristic'i "admissible" tutmak için küçük ölçekli kullanacağız:
        // h(n) = (EuclidDistance(n, target) / diagonal) * minEdgeWeight
        // Böylece h her zaman gerçek kalan maliyetten büyük olmaz (en kötü ihtimalle Dijkstra'ya yaklaşır).
        double minEdgeWeight = double.PositiveInfinity;
        foreach (var e in graph.Edges)
        {
            var w = weights.GetWeight(graph, e.A, e.B);
            if (w < minEdgeWeight) minEdgeWeight = w;
        }
        if (double.IsPositiveInfinity(minEdgeWeight)) minEdgeWeight = 0; // edge yoksa

        // koordinat aralığından diagonal
        var xs = graph.Nodes.Values.Select(n => n.X).ToList();
        var ys = graph.Nodes.Values.Select(n => n.Y).ToList();
        double diag = 1.0;
        if (xs.Count > 0)
        {
            double dx = xs.Max() - xs.Min();
            double dy = ys.Max() - ys.Min();
            diag = Math.Sqrt(dx * dx + dy * dy);
            if (diag < 1e-9) diag = 1.0;
        }

        double Heuristic(int nodeId)
        {
            var n = graph.GetNode(nodeId);
            var t = graph.GetNode(targetId);
            double dx = n.X - t.X;
            double dy = n.Y - t.Y;
            double eu = Math.Sqrt(dx * dx + dy * dy);
            return (eu / diag) * minEdgeWeight;
        }

        var cameFrom = new Dictionary<int, int?>();
        var gScore = new Dictionary<int, double>();
        var fScore = new Dictionary<int, double>();

        foreach (var id in graph.Nodes.Keys)
        {
            cameFrom[id] = null;
            gScore[id] = double.PositiveInfinity;
            fScore[id] = double.PositiveInfinity;
        }

        gScore[startId] = 0;
        fScore[startId] = Heuristic(startId);

        var open = new PriorityQueue<int, double>();
        open.Enqueue(startId, fScore[startId]);

        var closed = new HashSet<int>();

        while (open.Count > 0)
        {
            var current = open.Dequeue();

            if (closed.Contains(current))
                continue;

            if (current == targetId)
            {
                // Path reconstruct
                var path = new List<int>();
                int cur = targetId;
                while (true)
                {
                    path.Add(cur);
                    if (cur == startId) break;
                    var p = cameFrom[cur];
                    if (p is null) break;
                    cur = p.Value;
                }
                path.Reverse();
                return (path, gScore[targetId]);
            }

            closed.Add(current);

            foreach (var nb in graph.GetNeighbors(current))
            {
                if (closed.Contains(nb)) continue;

                double tentative = gScore[current] + weights.GetWeight(graph, current, nb);
                if (tentative < gScore[nb])
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentative;
                    fScore[nb] = tentative + Heuristic(nb);
                    open.Enqueue(nb, fScore[nb]);
                }
            }
        }
        return (new List<int>(), double.PositiveInfinity);
    }
}