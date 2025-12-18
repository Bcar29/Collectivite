using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
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

        public DashboardPage(AuthService authService)
        {
            InitializeComponent();
            AuditService auditService = new AuditService();

            _viewModel = new DashboardViewModel(auditService, authService);
            DataContext = _viewModel;

            // S'abonner aux changements de collections
            _viewModel.BarChartData.CollectionChanged += (s, e) => DrawBarChart();
            _viewModel.LineChartData.CollectionChanged += (s, e) => DrawLineChart();

            Loaded += DashboardPage_Loaded;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Attendre un court instant pour que les données soient chargées
            await System.Threading.Tasks.Task.Delay(500);

            // Dessiner les graphiques après le chargement de la page
            DrawBarChart();
            DrawLineChart();
        }

        #region Graphique en Barres

        private void DrawBarChart()
        {
            // Vérifier si le canvas est prêt
            if (BarChartCanvas == null || !BarChartCanvas.IsLoaded)
                return;

            BarChartCanvas.Children.Clear();

            var canvas = BarChartCanvas;
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 600;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 300;

            // Récupérer les données depuis le ViewModel
            var barChartData = _viewModel.BarChartData.ToList();

            if (barChartData.Count == 0)
            {
                //System.Diagnostics.Debug.WriteLine("BarChartData est vide");
                return;
            }

            //System.Diagnostics.Debug.WriteLine($"BarChartData count: {barChartData.Count}");
            //foreach (var item in barChartData)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Category: {item.Category}, Label: {item.Label}, Value: {item.Value}");
            //}

            // Organiser les données par catégorie et label
            var recettesFonctionnement = barChartData.FirstOrDefault(d => d.Category == "Recettes" && d.Label == "Fonctionnement");
            var recettesInvestissement = barChartData.FirstOrDefault(d => d.Category == "Recettes" && d.Label == "Investissement");
            var depensesFonctionnement = barChartData.FirstOrDefault(d => d.Category == "Dépenses" && d.Label == "Fonctionnement");
            var depensesInvestissement = barChartData.FirstOrDefault(d => d.Category == "Dépenses" && d.Label == "Investissement");

            var data = new[]
            {
                new { Label = "Recettes\nFonctionnement", Value = recettesFonctionnement?.Value ?? 0, Color = "#4CAF50", X = 80.0 },
                new { Label = "Recettes\nInvestissement", Value = recettesInvestissement?.Value ?? 0, Color = "#4CAF50", X = 200.0 },
                new { Label = "Dépenses\nFonctionnement", Value = depensesFonctionnement?.Value ?? 0, Color = "#F44336", X = 340.0 },
                new { Label = "Dépenses\nInvestissement", Value = depensesInvestissement?.Value ?? 0, Color = "#F44336", X = 460.0 }
            };

            //System.Diagnostics.Debug.WriteLine($"Data to draw: {string.Join(", ", data.Select(d => $"{d.Label}={d.Value}"))}");

            // Trouver la valeur maximale pour l'échelle
            double maxValue = data.Max(d => d.Value);
            if (maxValue == 0)
            {
                //System.Diagnostics.Debug.WriteLine("MaxValue est 0, pas de données à afficher");
                return;
            }

            // Arrondir à la centaine supérieure pour un meilleur affichage
            maxValue = Math.Ceiling(maxValue / 100) * 100;
            if (maxValue < 100) maxValue = 100; // Minimum pour l'échelle

            //System.Diagnostics.Debug.WriteLine($"MaxValue: {maxValue}");

            double chartHeight = canvasHeight - 60; // Espace pour les labels

            foreach (var item in data)
            {
                if (item.Value == 0) continue; // Ne pas dessiner les barres avec valeur 0

                double barHeight = (item.Value / maxValue) * chartHeight;
                double barWidth = 80;

                //System.Diagnostics.Debug.WriteLine($"Drawing bar: {item.Label}, height={barHeight}, value={item.Value}");

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
                    Text = $"{item.Value:N1}M",
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

            //System.Diagnostics.Debug.WriteLine($"Total children in canvas: {canvas.Children.Count}");
        }

        #endregion

        #region Graphique en Courbes

        private void DrawLineChart()
        {
            // Vérifier si le canvas est prêt
            if (LineChartCanvas == null || !LineChartCanvas.IsLoaded)
                return;

            LineChartCanvas.Children.Clear();

            var canvas = LineChartCanvas;
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 600;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 300;

            // Récupérer les données depuis le ViewModel
            var lineChartData = _viewModel.LineChartData.ToList();

            if (lineChartData.Count == 0)
            {
                //System.Diagnostics.Debug.WriteLine("LineChartData est vide");
                return;
            }

            //System.Diagnostics.Debug.WriteLine($"LineChartData count: {lineChartData.Count}");

            // Séparer les recettes et dépenses
            var recettesData = lineChartData.Where(d => d.Category == "Recettes").OrderBy(d => d.Label).ToList();
            var depensesData = lineChartData.Where(d => d.Category == "Dépenses").OrderBy(d => d.Label).ToList();

            if (recettesData.Count == 0 || depensesData.Count == 0)
            {
                //MessageBox.Show($"Pas assez de données: Recettes={recettesData.Count}, Dépenses={depensesData.Count}");
                //System.Diagnostics.Debug.WriteLine($"Pas assez de données: Recettes={recettesData.Count}, Dépenses={depensesData.Count}");
                return;
            }

            // Extraire les mois et valeurs
            var months = recettesData.Select(d => d.Label).ToArray();
            var recettes = recettesData.Select(d => d.Value).ToArray();
            var depenses = depensesData.Select(d => d.Value).ToArray();

            System.Diagnostics.Debug.WriteLine($"Months: {string.Join(", ", months)}");
            System.Diagnostics.Debug.WriteLine($"Recettes: {string.Join(", ", recettes)}");
            System.Diagnostics.Debug.WriteLine($"Depenses: {string.Join(", ", depenses)}");

            // Trouver la valeur maximale pour l'échelle
            double maxValue = Math.Max(recettes.Max(), depenses.Max());
            if (maxValue == 0)
            {
                //System.Diagnostics.Debug.WriteLine("MaxValue est 0 pour line chart");
                maxValue = 1;
            }

            // Arrondir à la centaine supérieure
            maxValue = Math.Ceiling(maxValue / 100) * 100;
            if (maxValue < 100) maxValue = 100;

            //System.Diagnostics.Debug.WriteLine($"Line chart MaxValue: {maxValue}");

            double chartHeight = canvasHeight - 40;
            double chartWidth = canvasWidth - 40;
            double stepX = months.Length > 1 ? chartWidth / (months.Length - 1) : 0;

            // Dessiner les axes
            DrawChartAxis(canvas, canvasWidth, canvasHeight);

            // Dessiner la courbe des recettes
            DrawLine(canvas, recettes, maxValue, chartHeight, stepX, "#2196F3", months);

            // Dessiner la courbe des dépenses
            DrawLine(canvas, depenses, maxValue, chartHeight, stepX, "#FF9800", months);

            //System.Diagnostics.Debug.WriteLine($"Line chart drawn with {canvas.Children.Count} elements");
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

        private void DrawLine(Canvas canvas, double[] values, double maxValue, double chartHeight,
                            double stepX, string colorHex, string[] labels)
        {
            if (values.Length == 0)
                return;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();

            for (int i = 0; i < values.Length; i++)
            {
                double x = 30 + (i * stepX);
                double y = maxValue > 0 ? chartHeight - ((values[i] / maxValue) * (chartHeight - 40)) : chartHeight - 40;

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

                // Ajouter le label du mois 
                if (i < labels.Length)
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