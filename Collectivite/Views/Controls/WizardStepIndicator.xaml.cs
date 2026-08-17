using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Controls
{
    /// <summary>
    /// Barre de progression réutilisable pour un formulaire par étapes (wizard) :
    /// une rangée de cercles numérotés reliés par une ligne, l'étape active et les
    /// étapes déjà validées étant mises en évidence.
    /// </summary>
    public partial class WizardStepIndicator : UserControl
    {
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(nameof(Steps), typeof(IEnumerable), typeof(WizardStepIndicator),
                new PropertyMetadata(null, OnStepsOrCurrentChanged));

        public static readonly DependencyProperty CurrentStepIndexProperty =
            DependencyProperty.Register(nameof(CurrentStepIndex), typeof(int), typeof(WizardStepIndicator),
                new PropertyMetadata(0, OnStepsOrCurrentChanged));

        private static readonly DependencyProperty StepItemsProperty =
            DependencyProperty.Register(nameof(StepItems), typeof(IReadOnlyList<StepItem>), typeof(WizardStepIndicator),
                new PropertyMetadata(new List<StepItem>()));

        public IEnumerable Steps
        {
            get => (IEnumerable)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        public int CurrentStepIndex
        {
            get => (int)GetValue(CurrentStepIndexProperty);
            set => SetValue(CurrentStepIndexProperty, value);
        }

        public IReadOnlyList<StepItem> StepItems => (IReadOnlyList<StepItem>)GetValue(StepItemsProperty);

        public WizardStepIndicator()
        {
            InitializeComponent();
        }

        private static void OnStepsOrCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WizardStepIndicator)d).RebuildStepItems();
        }

        private void RebuildStepItems()
        {
            var items = new List<StepItem>();

            if (Steps != null)
            {
                int index = 0;
                var titles = new List<string>();
                foreach (var step in Steps)
                {
                    titles.Add(step?.ToString() ?? "");
                    index++;
                }

                for (int i = 0; i < titles.Count; i++)
                {
                    items.Add(new StepItem
                    {
                        Index = i,
                        Title = titles[i],
                        DisplayNumber = i + 1,
                        IsCurrent = i == CurrentStepIndex,
                        IsCompleted = i < CurrentStepIndex,
                        HasPrevious = i > 0,
                        HasNext = i < titles.Count - 1
                    });
                }
            }

            SetValue(StepItemsProperty, items);
        }

        public class StepItem
        {
            public int Index { get; set; }
            public string Title { get; set; } = "";
            public int DisplayNumber { get; set; }
            public bool IsCurrent { get; set; }
            public bool IsCompleted { get; set; }
            public bool HasPrevious { get; set; }
            public bool HasNext { get; set; }
        }
    }
}
