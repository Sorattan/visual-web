using SocialNetworkAnalyzer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SocialNetworkAnalyzer.App
{
    public partial class MainWindow : Window
    {
        private readonly Graph _graph = new Graph();

        // NodeId -> Ellipse (seçiliyi highlight yapmak için)
        private readonly Dictionary<int, Ellipse> _nodeCircles = new();

        // Çizim ayarları
        private const double NodeRadius = 18;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                BuildSampleGraph();
                RenderGraph();
            };
        }

        private void BuildSampleGraph()
        {
            // Örnek 3 node + 2 edge
            // X,Y canvas koordinatı (A* için de işimize yarayacak)
            _graph.AddNode(new Node(1, "A", activity: 0.8, interaction: 12, x: 200, y: 150));
            _graph.AddNode(new Node(2, "B", activity: 0.3, interaction: 5, x: 450, y: 220));
            _graph.AddNode(new Node(3, "C", activity: 0.6, interaction: 9, x: 320, y: 420));

            _graph.AddEdge(1, 2);
            _graph.AddEdge(2, 3);
        }

        private void RenderGraph()
        {
            GraphCanvas.Children.Clear();
            _nodeCircles.Clear();

            // 1) Önce kenarları çiz (arkada kalsın)
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

            // 2) Sonra node’ları çiz
            foreach (var node in _graph.Nodes.Values)
            {
                DrawNode(node);
            }
        }

        private void DrawNode(Node node)
        {
            // Daire
            var circle = new Ellipse
            {
                Width = NodeRadius * 2,
                Height = NodeRadius * 2,
                Fill = new SolidColorBrush(Color.FromRgb(40, 160, 240)),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                Tag = node.Id // click’te id yakalayacağız
            };

            // Canvas konumlandırma: merkez (X,Y) olacak şekilde
            Canvas.SetLeft(circle, node.X - NodeRadius);
            Canvas.SetTop(circle, node.Y - NodeRadius);

            circle.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            // ID yazısı
            var label = new System.Windows.Controls.TextBlock
            {
                Text = node.Id.ToString(),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false // tıklamayı circle yakalasın
            };

            // Yazıyı ortala (yaklaşık)
            Canvas.SetLeft(label, node.X - 5);
            Canvas.SetTop(label, node.Y - 10);

            GraphCanvas.Children.Add(circle);
            GraphCanvas.Children.Add(label);

            _nodeCircles[node.Id] = circle;
        }

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse el) return;
            if (el.Tag is not int id) return;

            SelectNode(id);
        }

        private void SelectNode(int nodeId)
        {
            // Eski highlight’ları temizle
            foreach (var kv in _nodeCircles)
            {
                kv.Value.StrokeThickness = 2;
                kv.Value.Stroke = Brushes.White;
            }

            // Seçiliyi highlight yap
            if (_nodeCircles.TryGetValue(nodeId, out var circle))
            {
                circle.StrokeThickness = 4;
                circle.Stroke = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            }

            // Sağ paneli doldur
            var n = _graph.GetNode(nodeId);
            TxtId.Text = n.Id.ToString();
            TxtLabel.Text = n.Label;
            TxtActivity.Text = n.Activity.ToString("0.###");
            TxtInteraction.Text = n.Interaction.ToString("0.###");

            var degree = _graph.Degree(nodeId);
            TxtDegree.Text = degree.ToString();

            var neighbors = _graph.GetNeighbors(nodeId).OrderBy(x => x).ToList();
            TxtNeighbors.Text = neighbors.Count == 0 ? "-" : string.Join(", ", neighbors);
        }
    }
}
