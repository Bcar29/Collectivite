using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Collectivite.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            InitializeComponent();
            AuditService auditService = new AuditService();
            
            _viewModel = new DashboardViewModel(auditService);
            DataContext = _viewModel;

            Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Dessiner les graphiques après le chargement de la page
            DrawBarChart();
            DrawLineChart();
        }

        #region Graphique en Barres

        private void DrawBarChart()
        {
            BarChartCanvas.Children.Clear();

            var canvas = BarChartCanvas;
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 600;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 300;

            // Données pour le graphique
            var data = new[]
            {
                new { Label = "Recettes\nFonctionnement", Value = 7500.0, Color = "#4CAF50", X = 80.0 },
                new { Label = "Recettes\nInvestissement", Value = 5300.0, Color = "#4CAF50", X = 200.0 },
                new { Label = "Dépenses\nFonctionnement", Value = 4800.0, Color = "#F44336", X = 340.0 },
                new { Label = "Dépenses\nInvestissement", Value = 3650.0, Color = "#F44336", X = 460.0 }
            };

            double maxValue = 8000.0;
            double chartHeight = canvasHeight - 60; // Espace pour les labels

            foreach (var item in data)
            {
                double barHeight = (item.Value / maxValue) * chartHeight;
                double barWidth = 80;

                // Créer la barre
                Rectangle bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(item.Color)!,
                    RadiusX = 6,
                    RadiusY = 6
                };

                Canvas.SetLeft(bar, item.X);
                Canvas.SetBottom(bar, 40);
                canvas.Children.Add(bar);

                // Animation de la barre
                DoubleAnimation heightAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = barHeight,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                bar.BeginAnimation(Rectangle.HeightProperty, heightAnimation);

                // Label de valeur
                TextBlock valueLabel = new TextBlock
                {
                    Text = $"{item.Value:N0}M",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.Color)!)
                };
                Canvas.SetLeft(valueLabel, item.X + (barWidth - 40) / 2);
                Canvas.SetBottom(valueLabel, barHeight + 45);
                canvas.Children.Add(valueLabel);

                // Label de catégorie
                TextBlock categoryLabel = new TextBlock
                {
                    Text = item.Label,
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    Width = barWidth,
                    Foreground = Brushes.Gray
                };
                Canvas.SetLeft(categoryLabel, item.X);
                Canvas.SetBottom(categoryLabel, 5);
                canvas.Children.Add(categoryLabel);
            }
        }

        #endregion

        #region Graphique en Courbes

        private void DrawLineChart()
        {
            LineChartCanvas.Children.Clear();

            var canvas = LineChartCanvas;
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 600;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 300;

            // Données mensuelles (en millions)
            var months = new[] { "J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
            var recettes = new[] { 800, 950, 1100, 1050, 1200, 1150, 1300, 1250, 1400, 1350, 1450, 1500 };
            var depenses = new[] { 600, 700, 750, 800, 850, 800, 900, 850, 950, 900, 980, 1000 };

            double maxValue = 1600;
            double chartHeight = canvasHeight - 40;
            double chartWidth = canvasWidth - 40;
            double stepX = chartWidth / (months.Length - 1);

            // Dessiner les axes
            DrawChartAxis(canvas, canvasWidth, canvasHeight);

            // Dessiner la courbe des recettes
            DrawLine(canvas, recettes, maxValue, chartHeight, stepX, "#2196F3", months);

            // Dessiner la courbe des dépenses
            DrawLine(canvas, depenses, maxValue, chartHeight, stepX, "#FF9800", months);
        }

        private void DrawChartAxis(Canvas canvas, double width, double height)
        {
            // Axe vertical
            Line yAxis = new Line
            {
                X1 = 30,
                Y1 = 10,
                X2 = 30,
                Y2 = height - 30,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(yAxis);

            // Axe horizontal
            Line xAxis = new Line
            {
                X1 = 30,
                Y1 = height - 30,
                X2 = width - 10,
                Y2 = height - 30,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(xAxis);
        }

        private void DrawLine(Canvas canvas, int[] values, double maxValue, double chartHeight, 
                            double stepX, string colorHex, string[] labels)
        {
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();

            for (int i = 0; i < values.Length; i++)
            {
                double x = 30 + (i * stepX);
                double y = chartHeight - ((values[i] / maxValue) * (chartHeight - 40));

                if (i == 0)
                {
                    pathFigure.StartPoint = new Point(x, y);
                }
                else
                {
                    LineSegment lineSegment = new LineSegment(new Point(x, y), true);
                    pathFigure.Segments.Add(lineSegment);
                }

                // Ajouter un point
                Ellipse point = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(point, x - 4);
                Canvas.SetTop(point, y - 4);
                canvas.Children.Add(point);

                // Ajouter le label du mois (seulement tous les 2 mois pour éviter l'encombrement)
                if (i % 2 == 0)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = labels[i],
                        FontSize = 10,
                        Foreground = Brushes.Gray
                    };
                    Canvas.SetLeft(label, x - 5);
                    Canvas.SetTop(label, chartHeight - 20);
                    canvas.Children.Add(label);
                }
            }

            pathGeometry.Figures.Add(pathFigure);

            Path path = new Path
            {
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!,
                StrokeThickness = 3,
                Data = pathGeometry,
                StrokeLineJoin = PenLineJoin.Round
            };

            canvas.Children.Add(path);

            // Animation de la ligne
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(1000),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            path.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        #endregion

        #region Animations des Cards

        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement card)
            {
                DoubleAnimation scaleAnimation = new DoubleAnimation
                {
                    To = 1.02,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                ScaleTransform scale = new ScaleTransform(1, 1);
                card.RenderTransform = scale;
                card.RenderTransformOrigin = new Point(0.5, 0.5);

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement card)
            {
                DoubleAnimation scaleAnimation = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                if (card.RenderTransform is ScaleTransform scale)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                }
            }
        }

        #endregion
    }
}
