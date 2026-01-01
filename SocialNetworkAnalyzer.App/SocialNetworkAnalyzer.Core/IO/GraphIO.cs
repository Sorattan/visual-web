using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using SocialNetworkAnalyzer.Core.Models;
using SocialNetworkAnalyzer.Core.Validation;

namespace SocialNetworkAnalyzer.Core.IO;

public static class GraphIO
{
    // JSON
    private sealed class GraphDto
    {
        public List<NodeDto> Nodes { get; set; } = new();
        public List<EdgeDto> Edges { get; set; } = new();
    }

    private sealed class NodeDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public double Activity { get; set; }
        public double Interaction { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    private sealed class EdgeDto
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    public static void SaveJson(string path, Graph graph)
    {
        var dto = new GraphDto
        {
            Nodes = graph.Nodes.Values.Select(n => new NodeDto
            {
                Id = n.Id,
                Label = n.Label,
                Activity = n.Activity,
                Interaction = n.Interaction,
                X = n.X,
                Y = n.Y
            }).ToList(),
            Edges = graph.Edges.Select(e => new EdgeDto { A = e.A, B = e.B }).ToList()
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    public static Graph LoadJson(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var dto = JsonSerializer.Deserialize<GraphDto>(json) ?? throw new InvalidDataException("JSON boş/geçersiz.");

        var g = new Graph();
        foreach (var n in dto.Nodes)
            g.AddNode(new Node(n.Id, n.Label, n.Activity, n.Interaction, n.X, n.Y));

        foreach (var e in dto.Edges)
            g.AddEdge(e.A, e.B);

        return g;
    }

    // CSV
    // DugumId  Ozellik_I(Aktiflik)  Ozellik_II(Etkilesim)  Ozellik_III(BaglantiSayisi)  Komsular
    public static void SaveCsv(string path, Graph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DugumId Ozellik_I Ozellik_II Ozellik_III Komsular");

        foreach (var id in graph.Nodes.Keys.OrderBy(x => x))
        {
            var n = graph.GetNode(id);
            var neighbors = graph.GetNeighbors(id).OrderBy(x => x).ToList();
            var deg = neighbors.Count;

            sb.Append(id).Append(' ')
              .Append(n.Activity.ToString(CultureInfo.InvariantCulture)).Append(' ')
              .Append(n.Interaction.ToString(CultureInfo.InvariantCulture)).Append(' ')
              .Append(deg).Append(' ')
              .Append(string.Join(",", neighbors))
              .AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public static Graph LoadCsv(string path, Func<(double x, double y)> positionFactory)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Trim())
                        .ToList();

        if (lines.Count == 0) throw new InvalidDataException("CSV boş.");

        int start = 0;
        if (!char.IsDigit(lines[0][0])) start = 1;

        var g = new Graph();

        // Node’ları ekle
        var neighborMap = new Dictionary<int, List<int>>();

        for (int i = start; i < lines.Count; i++)
        {
            var parts = SplitCsvLineSmart(lines[i]);
            if (parts.Count < 5) throw new InvalidDataException($"CSV satırı eksik kolon: {lines[i]}");

            int id = int.Parse(parts[0], CultureInfo.InvariantCulture);
            double act = ParseDoubleAnyCulture(parts[1]);
            double inter = ParseDoubleAnyCulture(parts[2]);
            // parts[3] = bağlantı sayısı
            var neighborsRaw = parts[4];

            var (x, y) = positionFactory();
            g.AddNode(new Node(id, label: id.ToString(), activity: act, interaction: inter, x: x, y: y));

            var neighbors = ParseNeighbors(neighborsRaw);
            neighborMap[id] = neighbors;
        }

        // Edge’leri komşuluklardan kur
        foreach (var (id, nbs) in neighborMap)
        {
            foreach (var nb in nbs)
            {
                if (!g.Nodes.ContainsKey(nb))
                    throw new GraphValidationException($"CSV’de komşu olarak geçen node yok: {id} -> {nb}");

                if (id < nb)
                    g.AddEdge(id, nb);
            }
        }

        return g;
    }

    // Komşuluk Çıktıları
    public static void ExportAdjacencyList(string path, Graph graph)
    {
        var sb = new StringBuilder();
        foreach (var id in graph.Nodes.Keys.OrderBy(x => x))
        {
            var neighbors = graph.GetNeighbors(id).OrderBy(x => x);
            sb.Append(id).Append(": ").AppendLine(string.Join(", ", neighbors));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public static void ExportAdjacencyMatrixCsv(string path, Graph graph)
    {
        var ids = graph.Nodes.Keys.OrderBy(x => x).ToList();
        int n = ids.Count;

        // 0/1 matrisi
        using var sw = new StreamWriter(path, false, Encoding.UTF8);

        // Header
        sw.Write("Id");
        foreach (var id in ids) sw.Write($",{id}");
        sw.WriteLine();

        var index = ids.Select((id, idx) => (id, idx)).ToDictionary(t => t.id, t => t.idx);
        var adj = new bool[n, n];

        foreach (var e in graph.Edges)
        {
            var i = index[e.A];
            var j = index[e.B];
            adj[i, j] = true;
            adj[j, i] = true;
        }

        for (int i = 0; i < n; i++)
        {
            sw.Write(ids[i]);
            for (int j = 0; j < n; j++)
                sw.Write(adj[i, j] ? ",1" : ",0");
            sw.WriteLine();
        }
    }

    // Helpers
    private static double ParseDoubleAnyCulture(string s)
    {
        s = s.Trim();
        // hem 0.8 hem 0,8 destekle
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out v)) return v;
        return double.Parse(s, CultureInfo.CurrentCulture);
    }

    private static List<int> ParseNeighbors(string raw)
    {
        raw = (raw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw)) return new List<int>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => int.Parse(x.Trim(), CultureInfo.InvariantCulture))
                  .Distinct()
                  .ToList();
    }

    // "1 0.8 12 3 2,4,5" (boşluk) veya "1,0.8,12,3,2,4,5" (virgül)
    private static List<string> SplitCsvLineSmart(string line)
    {
        // boşluk
        var ws = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (ws.Count >= 5) return new List<string> { ws[0], ws[1], ws[2], ws[3], ws[4] };

        // virgül
        var c = line.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        if (c.Count >= 5)
        {
            var neighbors = string.Join(",", c.Skip(4));
            return new List<string> { c[0], c[1], c[2], c[3], neighbors };
        }

        // noktalı virgül
        var sc = line.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        if (sc.Count >= 5) return new List<string> { sc[0], sc[1], sc[2], sc[3], sc[4] };

        return ws;
    }
}