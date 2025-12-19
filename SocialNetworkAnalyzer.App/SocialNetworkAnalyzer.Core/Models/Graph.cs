using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalyzer.Core.Validation;

namespace SocialNetworkAnalyzer.Core.Models;

public sealed class Graph
{
    private readonly Dictionary<int, Node> _nodes = new();
    private readonly Dictionary<int, HashSet<int>> _adj = new();
    private readonly HashSet<Edge> _edges = new();

    public IReadOnlyDictionary<int, Node> Nodes => _nodes;
    public IEnumerable<Edge> Edges => _edges;

    public Node GetNode(int id)
    {
        if (!_nodes.TryGetValue(id, out var node))
            throw new GraphValidationException($"Node bulunamadı: {id}");
        return node;
    }

    public int Degree(int id) => _adj.TryGetValue(id, out var set) ? set.Count : 0;

    public IReadOnlyCollection<int> GetNeighbors(int id)
    {
        if (!_nodes.ContainsKey(id))
            throw new GraphValidationException($"Node bulunamadı: {id}");

        return _adj.TryGetValue(id, out var set) ? set.ToList() : new List<int>();
    }

    public void AddNode(Node node)
    {
        if (_nodes.ContainsKey(node.Id))
            throw new GraphValidationException($"Aynı düğüm tekrar eklenemez: {node.Id}");

        _nodes[node.Id] = node;
        _adj[node.Id] = new HashSet<int>();
    }

    public void UpdateNode(int id, string? label = null, double? activity = null, double? interaction = null)
    {
        var n = GetNode(id);
        n.Update(label, activity, interaction);
    }

    public void RemoveNode(int id)
    {
        if (!_nodes.ContainsKey(id))
            throw new GraphValidationException($"Silinecek node bulunamadı: {id}");

        var neighbors = _adj[id].ToList();
        foreach (var nb in neighbors)
            RemoveEdge(id, nb);

        _adj.Remove(id);
        _nodes.Remove(id);
    }

    public bool EdgeExists(int a, int b) => _edges.Contains(new Edge(a, b));

    public void AddEdge(int a, int b)
    {
        if (!_nodes.ContainsKey(a) || !_nodes.ContainsKey(b))
            throw new GraphValidationException("Edge eklemek için her iki node da var olmalı.");

        if (a == b)
            throw new GraphValidationException("Self-loop engellendi (A == B).");

        var e = new Edge(a, b);
        if (_edges.Contains(e))
            throw new GraphValidationException($"Aynı edge tekrar eklenemez: {e}");

        _edges.Add(e);
        _adj[a].Add(b);
        _adj[b].Add(a);
    }

    public void RemoveEdge(int a, int b)
    {
        var e = new Edge(a, b);
        if (!_edges.Remove(e))
            return;

        if (_adj.TryGetValue(a, out var sa)) sa.Remove(b);
        if (_adj.TryGetValue(b, out var sb)) sb.Remove(a);
    }
}