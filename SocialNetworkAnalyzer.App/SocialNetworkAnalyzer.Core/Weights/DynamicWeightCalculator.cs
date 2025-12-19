using System;
using SocialNetworkAnalyzer.Core.Models;

namespace SocialNetworkAnalyzer.Core.Weights;

public sealed class DynamicWeightCalculator : IWeightCalculator
{
    public double GetWeight(Graph graph, int nodeAId, int nodeBId)
    {
        var a = graph.GetNode(nodeAId);
        var b = graph.GetNode(nodeBId);

        double dAct = a.Activity - b.Activity;
        double dInt = a.Interaction - b.Interaction;

        double degA = graph.Degree(nodeAId);
        double degB = graph.Degree(nodeBId);
        double dDeg = degA - degB;

        double distance = Math.Sqrt((dAct * dAct) + (dInt * dInt) + (dDeg * dDeg));
        return 1.0 / (1.0 + distance);
    }
}