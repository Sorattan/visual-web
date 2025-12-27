using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalyzer.Core.Models;

namespace SocialNetworkAnalyzer.Core.Algorithms;

public static class ConnectedComponents
{
    public static List<List<int>> Find(Graph graph)
    {
        var visited = new HashSet<int>();
        var components = new List<List<int>>();

        var nodes = graph.Nodes.Keys.OrderBy(x => x).ToList();

        foreach (var start in nodes)
        {
            if (visited.Contains(start)) continue;

            var comp = new List<int>();
            var q = new Queue<int>();
            q.Enqueue(start);
            visited.Add(start);

            while (q.Count > 0)
            {
                var v = q.Dequeue();
                comp.Add(v);

                foreach (var nb in graph.GetNeighbors(v))
                {
                    if (visited.Add(nb))
                        q.Enqueue(nb);
                }
            }

            comp.Sort();
            components.Add(comp);
        }

        return components;
    }
}