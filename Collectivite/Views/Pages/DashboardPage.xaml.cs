using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
                return;
            }

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

            // Trouver la valeur maximale pour l'échelle
            double maxValue = data.Max(d => d.Value);
            if (maxValue == 0)
            {
                return;
            }

            // Arrondir à la centaine supérieure pour un meilleur affichage
            maxValue = Math.Ceiling(maxValue / 100) * 100;
            if (maxValue < 100) maxValue = 100;

            double chartHeight = canvasHeight - 60;

            foreach (var item in data)
            {
                if (item.Value == 0) continue;

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
        }

        #endregion

        #region Graphique en Courbes avec Tooltips

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
                return;
            }

            // Séparer les recettes et dépenses
            var recettesData = lineChartData.Where(d => d.Category == "Recettes").OrderBy(d => d.Label).ToList();
            var depensesData = lineChartData.Where(d => d.Category == "Dépenses").OrderBy(d => d.Label).ToList();

            if (recettesData.Count == 0 || depensesData.Count == 0)
            {
                return;
            }

            // Extraire les mois et valeurs
            var months = recettesData.Select(d => d.Label).ToArray();
            var recettes = recettesData.Select(d => d.Value).ToArray();
            var depenses = depensesData.Select(d => d.Value).ToArray();

            // Trouver la valeur maximale pour l'échelle
            double maxValue = Math.Max(recettes.Max(), depenses.Max());
            if (maxValue == 0)
            {
                maxValue = 1;
            }

            // Arrondir à la centaine supérieure
            maxValue = Math.Ceiling(maxValue / 100) * 100;
            if (maxValue < 100) maxValue = 100;

            double chartHeight = canvasHeight - 40;
            double chartWidth = canvasWidth - 60; // Ajusté pour laisser place aux labels Y
            double stepX = months.Length > 1 ? chartWidth / (months.Length - 1) : 0;

            // Dessiner les axes avec graduations
            DrawChartAxis(canvas, canvasWidth, canvasHeight, maxValue);

            // Dessiner la courbe des recettes avec tooltips
            DrawLineWithTooltips(canvas, recettes, maxValue, chartHeight, stepX, "#2196F3", months, "Recettes");

            // Dessiner la courbe des dépenses avec tooltips
            DrawLineWithTooltips(canvas, depenses, maxValue, chartHeight, stepX, "#FF9800", months, "Dépenses");
        }

        private void DrawChartAxis(Canvas canvas, double width, double height, double maxValue)
        {
            // Axe vertical
            Line yAxis = new Line
            {
                X1 = 50,
                Y1 = 10,
                X2 = 50,
                Y2 = height - 30,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(yAxis);

            // Axe horizontal
            Line xAxis = new Line
            {
                X1 = 50,
                Y1 = height - 30,
                X2 = width - 10,
                Y2 = height - 30,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(xAxis);

            // ═══════════════════════════════════════════════════════════
            // 🆕 GRADUATIONS SUR L'AXE VERTICAL AVEC MONTANTS
            // ═══════════════════════════════════════════════════════════
            int nombreGraduations = 5;
            double chartHeight = height - 40;

            for (int i = 0; i <= nombreGraduations; i++)
            {
                // Calculer la position Y
                double y = 10 + (chartHeight / nombreGraduations) * i;

                // Calculer la valeur correspondante (inversée car Y va de haut en bas)
                double valeur = maxValue - (maxValue / nombreGraduations) * i;

                // Ligne de grille horizontale (pointillée)
                Line gridLine = new Line
                {
                    X1 = 50,
                    Y1 = y,
                    X2 = width - 10,
                    Y2 = y,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")!),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                canvas.Children.Add(gridLine);

                // Petit trait sur l'axe
                Line tick = new Line
                {
                    X1 = 46,
                    Y1 = y,
                    X2 = 50,
                    Y2 = y,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(tick);

                // Label du montant
                TextBlock label = new TextBlock
                {
                    Text = FormatMontantAxe(valeur),
                    FontSize = 9,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")!),
                    TextAlignment = TextAlignment.Right,
                    Width = 42
                };
                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, y - 7);
                canvas.Children.Add(label);
            }
        }

        /// <summary>
        /// Formate le montant pour l'axe vertical (format court)
        /// </summary>
        private string FormatMontantAxe(double value)
        {
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000:N1}Md";
            if (value >= 1_000_000)
                return $"{value / 1_000_000:N1}M";
            if (value >= 1_000)
                return $"{value / 1_000:N0}K";
            if (value == 0)
                return "0";
            return $"{value:N0}";
        }

        /// <summary>
        /// Dessine une ligne avec des points interactifs et tooltips
        /// </summary>
        private void DrawLineWithTooltips(Canvas canvas, double[] values, double maxValue, double chartHeight,
                                          double stepX, string colorHex, string[] labels, string category)
        {
            if (values.Length == 0)
                return;

            var color = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();

            for (int i = 0; i < values.Length; i++)
            {
                // Position X ajustée (50 au lieu de 30 pour laisser place aux labels Y)
                double x = 50 + (i * stepX);
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

                // ═══════════════════════════════════════════════════════════
                // 🆕 POINT INTERACTIF AVEC TOOLTIP
                // ═══════════════════════════════════════════════════════════
                var pointData = new ChartPointData
                {
                    X = x,
                    Y = y,
                    Value = values[i],
                    Month = i < labels.Length ? labels[i] : "",
                    Category = category,
                    Color = colorHex
                };

                // Créer le point interactif
                Ellipse point = CreateInteractivePoint(pointData, color);
                Canvas.SetLeft(point, x - 7);
                Canvas.SetTop(point, y - 7);
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
                    Canvas.SetLeft(label, x - 10);
                    Canvas.SetTop(label, chartHeight - 15);
                    canvas.Children.Add(label);
                }
            }

            pathGeometry.Figures.Add(pathFigure);

            Path path = new Path
            {
                Stroke = color,
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

        /// <summary>
        /// Crée un point interactif avec tooltip et effet de survol
        /// </summary>
        private Ellipse CreateInteractivePoint(ChartPointData pointData, SolidColorBrush color)
        {
            Ellipse point = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = Brushes.White,
                Stroke = color,
                StrokeThickness = 3,
                Cursor = Cursors.Hand
            };

            // Créer le tooltip personnalisé
            point.ToolTip = CreateCustomTooltip(pointData);

            // Effet de survol : agrandir le point
            point.MouseEnter += (s, e) =>
            {
                point.Width = 18;
                point.Height = 18;
                Canvas.SetLeft(point, pointData.X - 9);
                Canvas.SetTop(point, pointData.Y - 9);
            };

            point.MouseLeave += (s, e) =>
            {
                point.Width = 14;
                point.Height = 14;
                Canvas.SetLeft(point, pointData.X - 7);
                Canvas.SetTop(point, pointData.Y - 7);
            };

            return point;
        }

        /// <summary>
        /// Crée un tooltip personnalisé avec style Material Design
        /// </summary>
        private ToolTip CreateCustomTooltip(ChartPointData pointData)
        {
            // Couleur selon la catégorie
            var headerColor = (Color)ColorConverter.ConvertFromString(pointData.Color)!;

            var tooltip = new ToolTip
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Placement = PlacementMode.Mouse,
                HasDropShadow = true
            };

            // Container principal avec bordure colorée en haut
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(headerColor),
                BorderThickness = new Thickness(0, 4, 0, 0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                MinWidth = 180,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 3,
                    Opacity = 0.25,
                    Color = Colors.Black
                }
            };

            var stack = new StackPanel();

            // ═══════════════════════════════════════════════════════════
            // EN-TÊTE : Icône + Type (Recettes/Dépenses)
            // ═══════════════════════════════════════════════════════════
            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Icône dans un cercle coloré
            var iconBorder = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(headerColor),
                Margin = new Thickness(0, 0, 10, 0)
            };

            var iconText = new TextBlock
            {
                Text = pointData.Category == "Recettes" ? "↗" : "↘",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;

            var typeLabel = new TextBlock
            {
                Text = pointData.Category,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(headerColor),
                VerticalAlignment = VerticalAlignment.Center
            };

            header.Children.Add(iconBorder);
            header.Children.Add(typeLabel);
            stack.Children.Add(header);

            // ═══════════════════════════════════════════════════════════
            // PÉRIODE (Mois)
            // ═══════════════════════════════════════════════════════════
            var monthPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6)
            };
            monthPanel.Children.Add(new TextBlock
            {
                Text = "Période : ",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575")!)
            });
            monthPanel.Children.Add(new TextBlock
            {
                Text = pointData.Month,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")!)
            });
            stack.Children.Add(monthPanel);

            // ═══════════════════════════════════════════════════════════
            // MONTANT
            // ═══════════════════════════════════════════════════════════
            var amountPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            amountPanel.Children.Add(new TextBlock
            {
                Text = "Montant : ",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575")!)
            });

            // Formater le montant
            string formattedAmount = FormatMontant(pointData.Value);

            amountPanel.Children.Add(new TextBlock
            {
                Text = formattedAmount,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(headerColor)
            });
            stack.Children.Add(amountPanel);

            border.Child = stack;
            tooltip.Content = border;

            return tooltip;
        }

        /// <summary>
        /// Formate le montant pour l'affichage
        /// </summary>
        private string FormatMontant(double value)
        {
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000:N2} Md GNF";
            if (value >= 1_000_000)
                return $"{value / 1_000_000:N2} M GNF";
            if (value >= 1_000)
                return $"{value / 1_000:N0} K GNF";
            return $"{value:N0} GNF";
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

    #region Classes Helper

    /// <summary>
    /// Données d'un point du graphique pour le tooltip
    /// </summary>
    public class ChartPointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
        public string Month { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    #endregion
}