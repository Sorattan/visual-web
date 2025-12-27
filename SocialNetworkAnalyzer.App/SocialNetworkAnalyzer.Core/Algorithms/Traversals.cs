using System.Collections.Generic;
using SocialNetworkAnalyzer.Core.Models;
using SocialNetworkAnalyzer.Core.Validation;

namespace SocialNetworkAnalyzer.Core.Algorithms;

public static class Traversals
{
    public static List<int> BFS(Graph graph, int startId)
    {
        if (!graph.Nodes.ContainsKey(startId))
            throw new GraphValidationException($"Başlangıç node yok: {startId}");

        var order = new List<int>();
        var visited = new HashSet<int>();
        var q = new Queue<int>();

        visited.Add(startId);
        q.Enqueue(startId);

        while (q.Count > 0)
        {
            var v = q.Dequeue();
            order.Add(v);

            foreach (var nb in graph.GetNeighbors(v))
            {
                if (visited.Add(nb))
                    q.Enqueue(nb);
            }
        }

        return order;
    }

    public static List<int> DFS(Graph graph, int startId)
    {
        if (!graph.Nodes.ContainsKey(startId))
            throw new GraphValidationException($"Başlangıç node yok: {startId}");

        var order = new List<int>();
        var visited = new HashSet<int>();
        var stack = new Stack<int>();

        stack.Push(startId);

        while (stack.Count > 0)
        {
            var v = stack.Pop();
            if (!visited.Add(v)) continue;

            order.Add(v);

            // Stabil sonuç için: büyükten küçüğe push -> küçük önce çıkar
            var nbs = new List<int>(graph.GetNeighbors(v));
            nbs.Sort();
            for (int i = nbs.Count - 1; i >= 0; i--)
                if (!visited.Contains(nbs[i]))
                    stack.Push(nbs[i]);
        }

        return order;
    }
}