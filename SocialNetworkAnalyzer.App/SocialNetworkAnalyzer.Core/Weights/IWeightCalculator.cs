using SocialNetworkAnalyzer.Core.Models;

namespace SocialNetworkAnalyzer.Core.Weights;

public interface IWeightCalculator
{
    double GetWeight(Graph graph, int nodeAId, int nodeBId);
}