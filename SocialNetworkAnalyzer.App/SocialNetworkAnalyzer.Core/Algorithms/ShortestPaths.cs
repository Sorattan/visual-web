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
}