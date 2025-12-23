using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SocialNetworkAnalyzer.Core.Models;
using SocialNetworkAnalyzer.Core.Validation;
using Microsoft.Win32;
using SocialNetworkAnalyzer.Core.IO;
using System.Diagnostics;
using SocialNetworkAnalyzer.Core.Algorithms;

namespace SocialNetworkAnalyzer.App
{
    public partial class MainWindow : Window
    {
        private readonly Graph _graph = new Graph();
        private readonly Dictionary<int, Ellipse> _nodeCircles = new();

        private const double NodeRadius = 18;

        private int? _selectedNodeId;
        private readonly Random _rng = new Random();

        private HashSet<int> _highlightedNodes = new();
        private readonly Brush _defaultNodeFill = new SolidColorBrush(Color.FromRgb(40, 160, 240));
        private readonly Brush _visitedFill = new SolidColorBrush(Color.FromRgb(60, 200, 120));

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                BuildSampleGraph();
                RenderGraph();
                ShowStatus("Hazır. Node listeden veya canvas’tan seçebilirsin.", isError: false);
            };
        }

        // ===================== SAMPLE =====================
        private void BuildSampleGraph()
        {
            _graph.Nodes.ToList().ForEach(kv => _graph.RemoveNode(kv.Key));

            _graph.AddNode(new Node(1, "A", activity: 0.8, interaction: 12, x: 200, y: 150));
            _graph.AddNode(new Node(2, "B", activity: 0.3, interaction: 5, x: 450, y: 220));
            _graph.AddNode(new Node(3, "C", activity: 0.6, interaction: 9, x: 320, y: 420));

            _graph.AddEdge(1, 2);
            _graph.AddEdge(2, 3);
        }

        // ===================== RENDER =====================
        private void RenderGraph()
        {
            GraphCanvas.Children.Clear();
            _nodeCircles.Clear();

            // Edges first
            foreach (var e in _graph.Edges)
            {
                var a = _graph.GetNode(e.A);
                var b = _graph.GetNode(e.B);

                var line = new Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    Opacity = 0.85
                };

                GraphCanvas.Children.Add(line);
            }

            // Nodes after
            foreach (var node in _graph.Nodes.Values)
                DrawNode(node);

            UpdateNodesList();

            // Selection restore
            if (_selectedNodeId.HasValue && _graph.Nodes.ContainsKey(_selectedNodeId.Value))
                SelectNode(_selectedNodeId.Value);
            else
                ClearSelectionUI();

            ApplyHighlights();
        }

        private void DrawNode(Node node)
        {
            var circle = new Ellipse
            {
                Width = NodeRadius * 2,
                Height = NodeRadius * 2,
                Fill = _defaultNodeFill,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                Tag = node.Id
            };

            Canvas.SetLeft(circle, node.X - NodeRadius);
            Canvas.SetTop(circle, node.Y - NodeRadius);
            circle.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            var label = new TextBlock
            {
                Text = node.Id.ToString(),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(label, node.X - 6);
            Canvas.SetTop(label, node.Y - 10);

            GraphCanvas.Children.Add(circle);
            GraphCanvas.Children.Add(label);

            _nodeCircles[node.Id] = circle;
        }

        private void UpdateNodesList()
        {
            var ids = _graph.Nodes.Keys.OrderBy(x => x).ToList();
            NodesList.ItemsSource = ids;

            if (_selectedNodeId.HasValue)
                NodesList.SelectedItem = _selectedNodeId.Value;
        }

        // ===================== SELECTION =====================
        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse el) return;
            if (el.Tag is not int id) return;
            SelectNode(id);
        }

        private void NodesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NodesList.SelectedItem is int id)
                SelectNode(id);
        }

        private void SelectNode(int nodeId)
        {
            _selectedNodeId = nodeId;

            foreach (var kv in _nodeCircles)
            {
                kv.Value.StrokeThickness = 2;
                kv.Value.Stroke = Brushes.White;
            }

            if (_nodeCircles.TryGetValue(nodeId, out var circle))
            {
                circle.StrokeThickness = 4;
                circle.Stroke = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            }

            var n = _graph.GetNode(nodeId);
            TxtId.Text = n.Id.ToString();
            TxtLabel.Text = n.Label;
            TxtActivity.Text = n.Activity.ToString("0.###");
            TxtInteraction.Text = n.Interaction.ToString("0.###");
            TxtDegree.Text = _graph.Degree(nodeId).ToString();

            var neighbors = _graph.GetNeighbors(nodeId).OrderBy(x => x).ToList();
            TxtNeighbors.Text = neighbors.Count == 0 ? "-" : string.Join(", ", neighbors);

            // Hızlı güncelleme için inputları da dolduralım
            InNodeId.Text = n.Id.ToString();
            InNodeLabel.Text = n.Label;
            InNodeActivity.Text = n.Activity.ToString("0.###");
            InNodeInteraction.Text = n.Interaction.ToString("0.###");
            InNodeX.Text = n.X.ToString("0.###");
            InNodeY.Text = n.Y.ToString("0.###");
        }

        private void ClearSelectionUI()
        {
            _selectedNodeId = null;
            TxtId.Text = TxtLabel.Text = TxtActivity.Text = TxtInteraction.Text = TxtDegree.Text = TxtNeighbors.Text = "-";
        }

        // ===================== STATUS =====================
        private void ShowStatus(string message, bool isError)
        {
            TxtStatus.Text = message;
            TxtStatus.Foreground = isError ? Brushes.OrangeRed : Brushes.LightGreen;
        }

        // ===================== PARSE HELPERS =====================
        private bool TryParseInt(TextBox tb, out int value, string fieldName)
        {
            if (int.TryParse(tb.Text?.Trim(), out value)) return true;
            ShowStatus($"{fieldName} sayısal (int) olmalı.", isError: true);
            return false;
        }

        private bool TryParseDouble(TextBox tb, out double value, string fieldName)
        {
            // TR sistemde virgül olabiliyor; double.TryParse zaten kültüre göre çalışır.
            if (double.TryParse(tb.Text?.Trim(), out value)) return true;
            ShowStatus($"{fieldName} sayısal (double) olmalı.", isError: true);
            return false;
        }

        // ===================== NODE CRUD =====================
        private void BtnAddNode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseInt(InNodeId, out var id, "Node Id")) return;

                var label = (InNodeLabel.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(label))
                {
                    ShowStatus("Label boş olamaz.", isError: true);
                    return;
                }

                if (!TryParseDouble(InNodeActivity, out var activity, "Activity")) return;
                if (!TryParseDouble(InNodeInteraction, out var interaction, "Interaction")) return;

                double x, y;
                if (ChkAutoPos.IsChecked == true)
                {
                    (x, y) = GetRandomPosition();
                }
                else
                {
                    // X/Y boşsa yine otomatik verelim
                    if (!double.TryParse(InNodeX.Text?.Trim(), out x) || !double.TryParse(InNodeY.Text?.Trim(), out y))
                        (x, y) = GetRandomPosition();
                }

                _graph.AddNode(new Node(id, label, activity, interaction, x, y));
                _selectedNodeId = id;

                RenderGraph();
                ShowStatus($"Node eklendi: {id}", isError: false);
            }
            catch (GraphValidationException ex)
            {
                ShowStatus(ex.Message, isError: true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Beklenmeyen hata: {ex.Message}", isError: true);
            }
        }

        private void BtnUpdateNode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int id;
                if (int.TryParse(InNodeId.Text?.Trim(), out var parsedId)) id = parsedId;
                else if (_selectedNodeId.HasValue) id = _selectedNodeId.Value;
                else
                {
                    ShowStatus("Güncellemek için Id gir veya bir node seç.", isError: true);
                    return;
                }

                // Boş bırakılan alanlar değişmesin
                string? label = string.IsNullOrWhiteSpace(InNodeLabel.Text) ? null : InNodeLabel.Text.Trim();

                double? activity = null;
                if (!string.IsNullOrWhiteSpace(InNodeActivity.Text))
                {
                    if (!TryParseDouble(InNodeActivity, out var a, "Activity")) return;
                    activity = a;
                }

                double? interaction = null;
                if (!string.IsNullOrWhiteSpace(InNodeInteraction.Text))
                {
                    if (!TryParseDouble(InNodeInteraction, out var it, "Interaction")) return;
                    interaction = it;
                }

                _graph.UpdateNode(id, label, activity, interaction);

                // Konum güncellemesi (opsiyonel)
                if (double.TryParse(InNodeX.Text?.Trim(), out var x) && double.TryParse(InNodeY.Text?.Trim(), out var y))
                {
                    var n = _graph.GetNode(id);
                    n.X = x; n.Y = y;
                }

                _selectedNodeId = id;
                RenderGraph();
                ShowStatus($"Node güncellendi: {id}", isError: false);
            }
            catch (GraphValidationException ex)
            {
                ShowStatus(ex.Message, isError: true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Beklenmeyen hata: {ex.Message}", isError: true);
            }
        }

        private void BtnDeleteNode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int id;
                if (int.TryParse(InNodeId.Text?.Trim(), out var parsedId)) id = parsedId;
                else if (_selectedNodeId.HasValue) id = _selectedNodeId.Value;
                else
                {
                    ShowStatus("Silmek için Id gir veya bir node seç.", isError: true);
                    return;
                }

                _graph.RemoveNode(id);
                _selectedNodeId = null;

                RenderGraph();
                ShowStatus($"Node silindi: {id}", isError: false);
            }
            catch (GraphValidationException ex)
            {
                ShowStatus(ex.Message, isError: true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Beklenmeyen hata: {ex.Message}", isError: true);
            }
        }

        private (double x, double y) GetRandomPosition()
        {
            // Canvas sınırları içinde güzel bir rastgele konum
            var margin = 40.0;
            var w = Math.Max(200, GraphCanvas.Width - margin * 2);
            var h = Math.Max(200, GraphCanvas.Height - margin * 2);

            var x = margin + _rng.NextDouble() * w;
            var y = margin + _rng.NextDouble() * h;
            return (x, y);
        }

        private void BtnSample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // mevcut grafı temizle + örneği kur
                _graph.Nodes.Keys.ToList().ForEach(id => _graph.RemoveNode(id));
                BuildSampleGraph();
                _selectedNodeId = null;

                RenderGraph();
                ShowStatus("Örnek graf yüklendi.", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Beklenmeyen hata: {ex.Message}", isError: true);
            }
        }

        // ===================== EDGE CRUD =====================
        private void BtnAddEdge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseInt(InEdgeA, out var a, "Edge A")) return;
                if (!TryParseInt(InEdgeB, out var b, "Edge B")) return;

                _graph.AddEdge(a, b);
                RenderGraph();
                ShowStatus($"Edge eklendi: {a}-{b}", isError: false);
            }
            catch (GraphValidationException ex)
            {
                ShowStatus(ex.Message, isError: true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Beklenmeyen hata: {ex.Message}", isError: true);
            }
        }

        private void BtnDeleteEdge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseInt(InEdgeA, out var a, "Edge A")) return;
                if (!TryParseInt(InEdgeB, out var b, "Edge B")) return;

                _graph.RemoveEdge(a, b);
                RenderGraph();
                ShowStatus($"Edge silindi: {a}-{b}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Beklenmeyen hata: {ex.Message}", isError: true);
            }
        }

        private void ClearGraph()
        {
            _selectedNodeId = null;
            foreach (var id in _graph.Nodes.Keys.ToList())
                _graph.RemoveNode(id);
        }

        private (double x, double y) AutoPos() => GetRandomPosition();

        private void BtnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = "CSV (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*" };
                if (dlg.ShowDialog() != true) return;

                var loaded = GraphIO.LoadCsv(dlg.FileName, AutoPos);

                ClearGraph();
                foreach (var n in loaded.Nodes.Values)
                    _graph.AddNode(n);
                foreach (var ed in loaded.Edges)
                    _graph.AddEdge(ed.A, ed.B);

                RenderGraph();
                ShowStatus($"CSV içe aktarıldı: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"CSV içe aktarma hatası: {ex.Message}", true);
            }
        }

        private void BtnImportJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = "JSON (*.json)|*.json|All files (*.*)|*.*" };
                if (dlg.ShowDialog() != true) return;

                var loaded = GraphIO.LoadJson(dlg.FileName);

                ClearGraph();
                foreach (var n in loaded.Nodes.Values)
                    _graph.AddNode(n);
                foreach (var ed in loaded.Edges)
                    _graph.AddEdge(ed.A, ed.B);

                RenderGraph();
                ShowStatus($"JSON içe aktarıldı: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"JSON içe aktarma hatası: {ex.Message}", true);
            }
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "graph.csv" };
                if (dlg.ShowDialog() != true) return;

                GraphIO.SaveCsv(dlg.FileName, _graph);
                ShowStatus($"CSV dışa aktarıldı: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"CSV dışa aktarma hatası: {ex.Message}", true);
            }
        }

        private void BtnExportJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "graph.json" };
                if (dlg.ShowDialog() != true) return;

                GraphIO.SaveJson(dlg.FileName, _graph);
                ShowStatus($"JSON dışa aktarıldı: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"JSON dışa aktarma hatası: {ex.Message}", true);
            }
        }

        private void BtnExportAdjList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = "Text (*.txt)|*.txt", FileName = "adjacency_list.txt" };
                if (dlg.ShowDialog() != true) return;

                GraphIO.ExportAdjacencyList(dlg.FileName, _graph);
                ShowStatus($"Komşuluk listesi üretildi: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Komşuluk listesi hatası: {ex.Message}", true);
            }
        }

        private void BtnExportAdjMatrix_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "adjacency_matrix.csv" };
                if (dlg.ShowDialog() != true) return;

                GraphIO.ExportAdjacencyMatrixCsv(dlg.FileName, _graph);
                ShowStatus($"Komşuluk matrisi üretildi: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Komşuluk matrisi hatası: {ex.Message}", true);
            }
        }

        private void ApplyHighlights()
        {
            foreach (var (id, circle) in _nodeCircles)
            {
                circle.Fill = _highlightedNodes.Contains(id) ? _visitedFill : _defaultNodeFill;
            }
        }

        private bool TryGetStartNode(out int startId)
        {
            // TextBox doluysa onu kullan
            if (!string.IsNullOrWhiteSpace(InStartNodeId.Text) &&
                int.TryParse(InStartNodeId.Text.Trim(), out startId))
                return true;

            // yoksa seçili node
            if (_selectedNodeId.HasValue)
            {
                startId = _selectedNodeId.Value;
                return true;
            }

            startId = 0;
            ShowStatus("Başlangıç için Id gir veya bir node seç.", true);
            return false;
        }

        private void RunTraversal(string name, Func<Graph, int, List<int>> algo)
        {
            try
            {
                if (!TryGetStartNode(out var startId)) return;

                var sw = Stopwatch.StartNew();
                var order = algo(_graph, startId);
                sw.Stop();

                _highlightedNodes = order.ToHashSet();
                ApplyHighlights();

                AlgoResultsList.ItemsSource = order.Select((id, idx) => $"{idx + 1}. {id}").ToList();
                ShowStatus($"{name} bitti. Ziyaret edilen: {order.Count} | Süre: {sw.ElapsedMilliseconds} ms", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"{name} hatası: {ex.Message}", true);
            }
        }

        private void BtnBfs_Click(object sender, RoutedEventArgs e)
    => RunTraversal("BFS", Traversals.BFS);

        private void BtnDfs_Click(object sender, RoutedEventArgs e)
            => RunTraversal("DFS", Traversals.DFS);

        private void BtnClearHighlight_Click(object sender, RoutedEventArgs e)
        {
            _highlightedNodes.Clear();
            ApplyHighlights();
            AlgoResultsList.ItemsSource = null;
            ShowStatus("Highlight temizlendi.", false);
        }
    }
}