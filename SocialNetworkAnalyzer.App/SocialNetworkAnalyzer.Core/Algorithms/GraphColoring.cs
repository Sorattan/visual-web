using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalyzer.Core.Models;

namespace SocialNetworkAnalyzer.Core.Algorithms;

public static class GraphColoring
{
    // Welsh–Powell: dereceye göre azalan sırala, sırayla en küçük uygun rengi ver
    public static Dictionary<int, int> WelshPowell(Graph graph, IReadOnlyCollection<int> nodesSubset)
    {
        var subset = nodesSubset.ToHashSet();

        var order = subset.OrderByDescending(id => graph.Degree(id)).ThenBy(id => id).ToList();

        var colorOf = new Dictionary<int, int>();

        foreach (var v in order)
        {
            var used = new HashSet<int>();

            foreach (var nb in graph.GetNeighbors(v))
            {
                if (!subset.Contains(nb)) continue;
                if (colorOf.TryGetValue(nb, out var c))
                    used.Add(c);
            }

            int color = 0;
            while (used.Contains(color)) color++;
            colorOf[v] = color;
        }

        return colorOf;
    }

    public static Dictionary<int, int> WelshPowellPerComponent(Graph graph, List<List<int>> components)
    {
        var result = new Dictionary<int, int>();

        foreach (var comp in components)
        {
            var local = WelshPowell(graph, comp);
            foreach (var (nodeId, color) in local)
                result[nodeId] = color;
        }

        return result;
    }

    public static int CountColors(Dictionary<int, int> colorOf) => colorOf.Count == 0 ? 0 : (colorOf.Values.Max() + 1);
}