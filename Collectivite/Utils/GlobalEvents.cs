using System;

namespace Collectivite.Utils
{
    public static class GlobalEvents
    {
        /// <summary>
        /// Événement déclenché quand la liste des exercices doit être rafraîchie
        /// </summary>
        public static event EventHandler? ExercicesListChanged;

        /// <summary>
        /// Déclenche l'événement de rafraîchissement des exercices
        /// </summary>
        public static void NotifyExercicesListChanged()
        {
            ExercicesListChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}