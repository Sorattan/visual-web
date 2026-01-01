using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalyzer.Core.Models;

namespace SocialNetworkAnalyzer.Core.Algorithms;

public static class Centrality
{
    // Degree centrality (normalize): degree / (n-1)
    public static List<(int NodeId, int Degree, double Score)> DegreeCentrality(Graph graph)
    {
        int n = graph.Nodes.Count;
        double denom = (n <= 1) ? 1.0 : (n - 1);

        return graph.Nodes.Keys
            .Select(id =>
            {
                int d = graph.Degree(id);
                double score = d / denom;
                return (NodeId: id, Degree: d, Score: score);
            })
            .ToList();
    }
}