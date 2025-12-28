using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using SocialNetworkAnalyzer.Core.IO;
using SocialNetworkAnalyzer.Core.Models;
using SocialNetworkAnalyzer.Core.Weights;
using SocialNetworkAnalyzer.Core.Validation;
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

        private readonly ScaleTransform _zoom = new ScaleTransform(1.0, 1.0);
        private const double ZoomStep = 1.12;
        private const double MinZoom = 0.2;
        private const double MaxZoom = 5.0;

        private readonly Dictionary<int, TextBlock> _nodeLabels = new();
        private readonly Dictionary<Edge, Line> _edgeLines = new();

        private bool _isDragging = false;
        private int _dragNodeId;
        private Vector _dragOffset; // (nodeCenter - mousePos)

        public MainWindow()
        {
            InitializeComponent();
            GraphCanvas.MouseMove += GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp += GraphCanvas_MouseLeftButtonUp;
            PerfResultsGrid.ItemsSource = _perfRows;
            GraphCanvas.LayoutTransform = _zoom;

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
            _nodeLabels.Clear();
            _edgeLines.Clear();

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
                    Opacity = 0.90
                };

                bool isPathEdge = _highlightedEdges.Contains(e);
                line.StrokeThickness = isPathEdge ? 4 : 2;
                line.Stroke = isPathEdge ? _pathEdgeStroke : _defaultEdgeStroke;

                _edgeLines[e] = line;
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

            _nodeLabels[node.Id] = label;

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

            // Drag başlat
            var mouse = e.GetPosition(GraphCanvas);
            var n = _graph.GetNode(id);

            _isDragging = true;
            _dragNodeId = id;
            _dragOffset = new Vector(n.X - mouse.X, n.Y - mouse.Y);

            GraphCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndDrag();
                return;
            }

            var mouse = e.GetPosition(GraphCanvas);
            var n = _graph.GetNode(_dragNodeId);

            // Yeni merkez konumu
            double newX = mouse.X + _dragOffset.X;
            double newY = mouse.Y + _dragOffset.Y;

            // Canvas sınırına hafif clamp (istersen kaldır)
            newX = Math.Max(NodeRadius, Math.Min(GraphCanvas.Width - NodeRadius, newX));
            newY = Math.Max(NodeRadius, Math.Min(GraphCanvas.Height - NodeRadius, newY));

            n.X = newX;
            n.Y = newY;

            UpdateNodeVisual(_dragNodeId);
            UpdateIncidentEdges(_dragNodeId);

            // Seçili node ise input X/Y de güncellensin
            if (_selectedNodeId == _dragNodeId)
            {
                InNodeX.Text = n.X.ToString("0.###");
                InNodeY.Text = n.Y.ToString("0.###");
            }
        }

        private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging) EndDrag();
        }

        private void EndDrag()
        {
            _isDragging = false;
            GraphCanvas.ReleaseMouseCapture();
        }

        private void UpdateNodeVisual(int nodeId)
        {
            var n = _graph.GetNode(nodeId);

            if (_nodeCircles.TryGetValue(nodeId, out var circle))
            {
                Canvas.SetLeft(circle, n.X - NodeRadius);
                Canvas.SetTop(circle, n.Y - NodeRadius);
            }

            if (_nodeLabels.TryGetValue(nodeId, out var label))
            {
                Canvas.SetLeft(label, n.X - 6);
                Canvas.SetTop(label, n.Y - 10);
            }
        }

        private void UpdateIncidentEdges(int nodeId)
        {
            foreach (var nb in _graph.GetNeighbors(nodeId))
            {
                var e = new Edge(nodeId, nb);
                if (!_edgeLines.TryGetValue(e, out var line)) continue;

                var a = _graph.GetNode(e.A);
                var b = _graph.GetNode(e.B);

                line.X1 = a.X; line.Y1 = a.Y;
                line.X2 = b.X; line.Y2 = b.Y;
            }
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

        private bool _componentColoringActive = false;
        private Dictionary<int, Brush> _componentFillByNode = new();

        private readonly List<Brush> _componentPalette = new()
        {
            new SolidColorBrush(Color.FromRgb(255, 99, 71)),   // tomato
            new SolidColorBrush(Color.FromRgb(135, 206, 235)), // skyblue
            new SolidColorBrush(Color.FromRgb(60, 200, 120)),  // green
            new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // gold
            new SolidColorBrush(Color.FromRgb(186, 85, 211)),  // orchid
            new SolidColorBrush(Color.FromRgb(255, 140, 0)),   // darkorange
            new SolidColorBrush(Color.FromRgb(64, 224, 208)),  // turquoise
            new SolidColorBrush(Color.FromRgb(220, 20, 60)),   // crimson
            new SolidColorBrush(Color.FromRgb(173, 255, 47)),  // greenyellow
            new SolidColorBrush(Color.FromRgb(30, 144, 255)),  // dodgerblue
        };

        private void BtnComponents_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var comps = ConnectedComponents.Find(_graph);
                sw.Stop();

                // Node -> Brush map
                _componentFillByNode.Clear();
                for (int i = 0; i < comps.Count; i++)
                {
                    var brush = _componentPalette[i % _componentPalette.Count];
                    foreach (var id in comps[i])
                        _componentFillByNode[id] = brush;
                }

                _componentColoringActive = true;

                // BFS/DFS highlight’ı karıştırmasın diye temizleyelim
                _highlightedNodes.Clear();
                AlgoResultsList.ItemsSource = null;

                ApplyHighlights();

                // Sonuç listesi
                ComponentsResultsList.ItemsSource = comps
                    .Select((c, idx) => $"Bileşen {idx + 1} ({c.Count} node): " + string.Join(", ", c))
                    .ToList();

                ShowStatus($"Bileşenler bulundu: {comps.Count} | Süre: {sw.ElapsedMilliseconds} ms", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Bileşen hatası: {ex.Message}", true);
            }
        }

        private void BtnClearComponentColors_Click(object sender, RoutedEventArgs e)
        {
            _componentColoringActive = false;
            _componentFillByNode.Clear();
            ComponentsResultsList.ItemsSource = null;

            ApplyHighlights();
            ShowStatus("Bileşen renklendirme temizlendi.", false);
        }

        private void BtnDegreeTop5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var all = Centrality.DegreeCentrality(_graph);

                // dereceye göre azalan, eşitse id artan
                var top5 = all
                    .OrderByDescending(x => x.Degree)
                    .ThenBy(x => x.NodeId)
                    .Take(5)
                    .ToList();

                sw.Stop();

                // Component boyası aktifse highlight görünmez; bu yüzden kapatalım (istersen kaldırabilirsin)
                _componentColoringActive = false;
                _componentFillByNode.Clear();
                ComponentsResultsList.ItemsSource = null;

                // Top-5'i highlight yap
                _highlightedNodes = top5.Select(x => x.NodeId).ToHashSet();
                ApplyHighlights();

                AlgoResultsList.ItemsSource = top5.Select((x, idx) =>
                    $"{idx + 1}. Node {x.NodeId} | Degree: {x.Degree} | Centrality: {x.Score:0.###}"
                ).ToList();

                ShowStatus($"Degree Centrality Top-5 hazır | Süre: {sw.ElapsedMilliseconds} ms", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Degree Centrality hatası: {ex.Message}", true);
            }
        }
        private Brush GetColorBrush(int colorIndex)
        {
            // Önce hazır paletten
            if (colorIndex >= 0 && colorIndex < _componentPalette.Count)
                return _componentPalette[colorIndex];

            // Palette yetmezse deterministic (her seferinde aynı) renk üret: golden-angle hue
            double hue = (colorIndex * 137.508) % 360.0; // golden angle
            return new SolidColorBrush(HsvToRgb(hue, 0.70, 0.95));
        }

        private Color HsvToRgb(double h, double s, double v)
        {
            // h: 0..360, s/v: 0..1
            double c = v * s;
            double x = c * (1 - System.Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r1 = 0, g1 = 0, b1 = 0;
            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte R = (byte)((r1 + m) * 255);
            byte G = (byte)((g1 + m) * 255);
            byte B = (byte)((b1 + m) * 255);
            return Color.FromRgb(R, G, B);
        }

        private void BtnWelshPowell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // 1) Bileşenleri bul
                var comps = ConnectedComponents.Find(_graph);

                // 2) Her bileşende Welsh–Powell uygula
                var colorOf = GraphColoring.WelshPowellPerComponent(_graph, comps);
                int colorCount = GraphColoring.CountColors(colorOf);

                sw.Stop();

                // 3) Canvas boyası için Node->Brush map kur
                _componentFillByNode.Clear();
                foreach (var (nodeId, colorIdx) in colorOf.OrderBy(x => x.Key))
                    _componentFillByNode[nodeId] = GetColorBrush(colorIdx);

                _componentColoringActive = true;

                // BFS/DFS highlight karışmasın
                _highlightedNodes.Clear();
                AlgoResultsList.ItemsSource = null;

                // Component listesi karışmasın (istersen kalsın)
                ComponentsResultsList.ItemsSource = null;

                ApplyHighlights();

                // 4) Sonuç tablosu
                ColoringResultsList.ItemsSource = colorOf
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"Node {kv.Key} → Color {kv.Value}")
                    .ToList();

                ShowStatus($"Welsh–Powell bitti | Kullanılan renk: {colorCount} | Süre: {sw.ElapsedMilliseconds} ms", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Welsh–Powell hatası: {ex.Message}", true);
            }
        }

        private void BtnClearColoring_Click(object sender, RoutedEventArgs e)
        {
            _componentColoringActive = false;
            _componentFillByNode.Clear();
            ColoringResultsList.ItemsSource = null;

            ApplyHighlights();
            ShowStatus("Boyama temizlendi.", false);
        }

        private HashSet<Edge> _highlightedEdges = new();

        private readonly Brush _defaultEdgeStroke = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        private readonly Brush _pathEdgeStroke = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // gold

        private void BtnDijkstra_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Start: textbox varsa onu, yoksa seçili node
                if (!TryGetStartNode(out var startId)) return;

                // Target zorunlu
                if (string.IsNullOrWhiteSpace(InTargetNodeId.Text) || !int.TryParse(InTargetNodeId.Text.Trim(), out var targetId))
                {
                    ShowStatus("Hedef Node Id gir (int).", true);
                    return;
                }

                // Boyamalar karışmasın: component coloring kapat
                _componentColoringActive = false;
                _componentFillByNode.Clear();
                ComponentsResultsList.ItemsSource = null;
                ColoringResultsList.ItemsSource = null;

                var sw = Stopwatch.StartNew();
                var weights = new DynamicWeightCalculator();
                var (path, cost) = ShortestPaths.Dijkstra(_graph, startId, targetId, weights);
                sw.Stop();

                if (path.Count == 0)
                {
                    _highlightedNodes.Clear();
                    _highlightedEdges.Clear();
                    RenderGraph();
                    AlgoResultsList.ItemsSource = new List<string> { $"Ulaşılamıyor: {startId} -> {targetId}" };
                    ShowStatus($"Dijkstra: yol yok | Süre: {sw.ElapsedMilliseconds} ms", true);
                    return;
                }

                // Node highlight
                _highlightedNodes = path.ToHashSet();

                // Edge highlight (path boyunca)
                _highlightedEdges.Clear();
                for (int i = 0; i < path.Count - 1; i++)
                    _highlightedEdges.Add(new Edge(path[i], path[i + 1]));

                // Yeniden çiz (edge kalınlığı için)
                RenderGraph();

                // Sonuç listesi: maliyet + path + kenar ağırlıkları
                var lines = new List<string>
                {
                    $"Toplam maliyet: {cost:0.###}",
                    $"Yol: {string.Join(" -> ", path)}",
                    $"Süre: {sw.ElapsedMilliseconds} ms"
                };

                // İstersen detay: her adımın ağırlığı
                for (int i = 0; i < path.Count - 1; i++)
                {
                    double w = weights.GetWeight(_graph, path[i], path[i + 1]);
                    lines.Add($"  {path[i]} - {path[i + 1]} : {w:0.###}");
                }

                AlgoResultsList.ItemsSource = lines;

                ShowStatus($"Dijkstra bitti | Yol uzunluğu: {path.Count} node | Süre: {sw.ElapsedMilliseconds} ms", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Dijkstra hatası: {ex.Message}", true);
            }
        }

        private void BtnAStar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetStartNode(out var startId)) return;

                if (string.IsNullOrWhiteSpace(InTargetNodeId.Text) || !int.TryParse(InTargetNodeId.Text.Trim(), out var targetId))
                {
                    ShowStatus("Hedef Node Id gir (int).", true);
                    return;
                }

                // Boyamalar karışmasın
                _componentColoringActive = false;
                _componentFillByNode.Clear();
                ComponentsResultsList.ItemsSource = null;
                ColoringResultsList.ItemsSource = null;

                var sw = Stopwatch.StartNew();
                var weights = new DynamicWeightCalculator();
                var (path, cost) = ShortestPaths.AStar(_graph, startId, targetId, weights);
                sw.Stop();

                if (path.Count == 0)
                {
                    _highlightedNodes.Clear();
                    _highlightedEdges.Clear();
                    RenderGraph();
                    AlgoResultsList.ItemsSource = new List<string> { $"Ulaşılamıyor: {startId} -> {targetId}" };
                    ShowStatus($"A*: yol yok | Süre: {sw.ElapsedMilliseconds} ms", true);
                    return;
                }

                _highlightedNodes = path.ToHashSet();
                _highlightedEdges.Clear();
                for (int i = 0; i < path.Count - 1; i++)
                    _highlightedEdges.Add(new Edge(path[i], path[i + 1]));

                RenderGraph();

                var lines = new List<string>
        {
            $"Toplam maliyet: {cost:0.###}",
            $"Yol: {string.Join(" -> ", path)}",
            $"Süre: {sw.ElapsedMilliseconds} ms"
        };

                for (int i = 0; i < path.Count - 1; i++)
                {
                    double w = weights.GetWeight(_graph, path[i], path[i + 1]);
                    lines.Add($"  {path[i]} - {path[i + 1]} : {w:0.###}");
                }

                AlgoResultsList.ItemsSource = lines;
                ShowStatus($"A* bitti | Yol uzunluğu: {path.Count} node | Süre: {sw.ElapsedMilliseconds} ms", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"A* hatası: {ex.Message}", true);
            }
        }

        private sealed class PerfRow
        {
            public string Algorithm { get; set; } = "";
            public double Milliseconds { get; set; }
            public double Microseconds { get; set; }
            public string Note { get; set; } = "";
        }

        private readonly ObservableCollection<PerfRow> _perfRows = new();
        private List<PerfRow> _lastBenchmarkSnapshot = new(); // CSV kaydetmek için

        private bool TryParsePositiveInt(TextBox tb, out int value, string name)
        {
            if (int.TryParse(tb.Text?.Trim(), out value) && value > 0) return true;
            ShowStatus($"{name} pozitif bir int olmalı.", true);
            return false;
        }

        private void ApplyCircleLayout()
        {
            var ids = _graph.Nodes.Keys.OrderBy(x => x).ToList();
            if (ids.Count == 0) return;

            double cx = GraphCanvas.Width / 2.0;
            double cy = GraphCanvas.Height / 2.0;
            double r = Math.Min(cx, cy) - 90;
            if (r < 60) r = 60;

            for (int i = 0; i < ids.Count; i++)
            {
                double ang = 2 * Math.PI * i / ids.Count;
                var n = _graph.GetNode(ids[i]);
                n.X = cx + r * Math.Cos(ang);
                n.Y = cy + r * Math.Sin(ang);
            }
        }

        private void GenerateRandomGraph(int n, int eCount, int? seed)
        {
            // max edge = n(n-1)/2
            long maxEdges = (long)n * (n - 1) / 2;
            if (eCount > maxEdges) eCount = (int)maxEdges;

            var rng = seed.HasValue ? new Random(seed.Value) : new Random();

            // temizle
            ClearGraph();

            // node ekle (özellikler rastgele)
            for (int i = 1; i <= n; i++)
            {
                double activity = rng.NextDouble();          // 0..1
                double interaction = rng.Next(0, 101);       // 0..100 (istersen 0..1 yapabilirsin)
                _graph.AddNode(new Node(i, i.ToString(), activity, interaction, 0, 0));
            }

            // edge ekle (duplicate/self-loop yok)
            var used = new HashSet<Edge>();

            // E büyükse rastgele deneme sayısı artmasın diye iki mod:
            double density = maxEdges == 0 ? 0 : (double)eCount / maxEdges;
            if (maxEdges <= 200_000 || density > 0.35)
            {
                // tüm çiftleri üret, karıştır, ilk E tanesini al (n küçük/orta için güvenli)
                var all = new List<Edge>((int)Math.Min(maxEdges, 200_000));
                for (int a = 1; a <= n; a++)
                    for (int b = a + 1; b <= n; b++)
                        all.Add(new Edge(a, b));

                // shuffle (Fisher-Yates)
                for (int i = all.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(0, i + 1);
                    (all[i], all[j]) = (all[j], all[i]);
                }

                for (int i = 0; i < eCount; i++)
                {
                    var e = all[i];
                    _graph.AddEdge(e.A, e.B);
                }
            }
            else
            {
                // seyrek graf: random pick
                while (used.Count < eCount)
                {
                    int a = rng.Next(1, n + 1);
                    int b = rng.Next(1, n + 1);
                    if (a == b) continue;

                    var e = new Edge(a, b);
                    if (!used.Add(e)) continue;

                    _graph.AddEdge(e.A, e.B);
                }
            }

            ApplyCircleLayout();
        }

        private void BtnGeneratePerfGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParsePositiveInt(InPerfN, out var n, "N")) return;
                if (!TryParsePositiveInt(InPerfE, out var ec, "E")) return;

                int? seed = null;
                if (!string.IsNullOrWhiteSpace(InPerfSeed.Text) && int.TryParse(InPerfSeed.Text.Trim(), out var s))
                    seed = s;

                GenerateRandomGraph(n, ec, seed);

                // UI temizle
                _highlightedNodes.Clear();
                _highlightedEdges.Clear();
                _componentColoringActive = false;
                _componentFillByNode.Clear();
                AlgoResultsList.ItemsSource = null;
                ComponentsResultsList.ItemsSource = null;
                ColoringResultsList.ItemsSource = null;
                _perfRows.Clear();
                _lastBenchmarkSnapshot.Clear();

                RenderGraph();
                ShowStatus($"Rastgele graf üretildi: N={n}, E={ec}" + (seed.HasValue ? $", Seed={seed}" : ""), false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Graf üretme hatası: {ex.Message}", true);
            }
        }

        private void AddPerf(string name, Action action, Func<string>? noteFactory = null)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();

            double ms = sw.Elapsed.TotalMilliseconds;
            double us = sw.Elapsed.TotalMilliseconds * 1000.0;

            _perfRows.Add(new PerfRow
            {
                Algorithm = name,
                Milliseconds = ms,
                Microseconds = us,
                Note = noteFactory?.Invoke() ?? ""
            });
        }

        private void BtnRunBenchmark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _perfRows.Clear();
                _lastBenchmarkSnapshot.Clear();

                if (_graph.Nodes.Count == 0)
                {
                    ShowStatus("Benchmark için önce graf oluştur veya içe aktar.", true);
                    return;
                }

                var ids = _graph.Nodes.Keys.OrderBy(x => x).ToList();
                int start = ids.First();
                int target = ids.Last();

                // BFS / DFS
                List<int> bfs = new();
                AddPerf("BFS",
                    () => bfs = Traversals.BFS(_graph, start),
                    () => $"start={start}, visited={bfs.Count}");

                List<int> dfs = new();
                AddPerf("DFS",
                    () => dfs = Traversals.DFS(_graph, start),
                    () => $"start={start}, visited={dfs.Count}");

                // Components
                List<List<int>> comps = new();
                AddPerf("Connected Components",
                    () => comps = ConnectedComponents.Find(_graph),
                    () => $"count={comps.Count}");

                // Degree centrality
                var degList = new List<(int NodeId, int Degree, double Score)>();
                AddPerf("Degree Centrality",
                    () => degList = Centrality.DegreeCentrality(_graph),
                    () => $"nodes={degList.Count}");

                // Welsh–Powell (bileşen başına)
                var colors = new Dictionary<int, int>();
                AddPerf("Welsh–Powell",
                    () => colors = GraphColoring.WelshPowellPerComponent(_graph, comps),
                    () => $"colors={GraphColoring.CountColors(colors)}");

                // Dijkstra / A*
                var weights = new DynamicWeightCalculator();

                (List<int> Path, double Cost) dj = (new(), double.PositiveInfinity);
                AddPerf("Dijkstra",
                    () => dj = ShortestPaths.Dijkstra(_graph, start, target, weights),
                    () => dj.Path.Count == 0 ? $"start={start}, target={target}, yol yok"
                                             : $"len={dj.Path.Count}, cost={dj.Cost:0.###}");

                (List<int> Path, double Cost) ast = (new(), double.PositiveInfinity);
                AddPerf("A*",
                    () => ast = ShortestPaths.AStar(_graph, start, target, weights),
                    () => ast.Path.Count == 0 ? $"start={start}, target={target}, yol yok"
                                              : $"len={ast.Path.Count}, cost={ast.Cost:0.###}");

                // Snapshot (CSV)
                _lastBenchmarkSnapshot = _perfRows.Select(r => new PerfRow
                {
                    Algorithm = r.Algorithm,
                    Milliseconds = r.Milliseconds,
                    Note = r.Note
                }).ToList();

                ShowStatus($"Benchmark bitti. N={_graph.Nodes.Count}, E={_graph.Edges.Count()}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Benchmark hatası: {ex.Message}", true);
            }
        }

        private void BtnExportBenchmarkCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_lastBenchmarkSnapshot.Count == 0)
                {
                    ShowStatus("Önce benchmark çalıştır.", true);
                    return;
                }

                var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "benchmark.csv" };
                if (dlg.ShowDialog() != true) return;

                var sb = new StringBuilder();
                sb.AppendLine("Algorithm,Milliseconds,Note");
                foreach (var r in _lastBenchmarkSnapshot)
                {
                    // basit CSV escape
                    string a = r.Algorithm.Replace("\"", "\"\"");
                    string n = r.Note.Replace("\"", "\"\"");
                    sb.AppendLine($"\"{a}\",{r.Milliseconds},\"{n}\"");
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                ShowStatus($"Benchmark CSV kaydedildi: {dlg.FileName}", false);
            }
            catch (Exception ex)
            {
                ShowStatus($"CSV kaydetme hatası: {ex.Message}", true);
            }
        }

        private void GraphScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Sadece CTRL basılıyken zoom yap
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

            e.Handled = true;

            double oldScale = _zoom.ScaleX;
            double factor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            double newScale = Math.Max(MinZoom, Math.Min(MaxZoom, oldScale * factor));
            if (Math.Abs(newScale - oldScale) < 0.000001) return;

            // Mouse’un canvas üzerindeki (zoom uygulanmamış) konumu
            Point p = e.GetPosition(GraphCanvas);

            // Scroll offsetleri "transform edilmiş ölçekte" çalıştığı için anchor düzeltmesi:
            double offX = GraphScroll.HorizontalOffset;
            double offY = GraphScroll.VerticalOffset;

            _zoom.ScaleX = _zoom.ScaleY = newScale;

            // İmleç altındaki nokta ekranda sabit kalsın
            double newOffX = offX + p.X * (newScale - oldScale);
            double newOffY = offY + p.Y * (newScale - oldScale);

            GraphScroll.ScrollToHorizontalOffset(newOffX);
            GraphScroll.ScrollToVerticalOffset(newOffY);

            ShowStatus($"Zoom: {(newScale * 100):0}% (Ctrl+Scroll)", false);
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
                if (_componentColoringActive && _componentFillByNode.TryGetValue(id, out var compBrush))
                {
                    circle.Fill = compBrush;
                }
                else
                {
                    circle.Fill = _highlightedNodes.Contains(id) ? _visitedFill : _defaultNodeFill;
                }
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
            _highlightedEdges.Clear();
            AlgoResultsList.ItemsSource = null;
            RenderGraph();
            ShowStatus("Highlight temizlendi.", false);
        }
    }
}