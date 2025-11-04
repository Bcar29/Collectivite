using System;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Utils
{
    /// <summary>
    /// Classe utilitaire pour construire des structures hiérarchiques (arbres)
    /// </summary>
    public static class TreeHelper
    {
        /// <summary>
        /// Construit une structure hiérarchique à partir d'une liste plate d'éléments
        /// </summary>
        /// <typeparam name="T">Type des éléments</typeparam>
        /// <typeparam name="TKey">Type de la clé (ID)</typeparam>
        /// <param name="items">Liste plate des éléments</param>
        /// <param name="getId">Fonction pour obtenir l'ID d'un élément</param>
        /// <param name="getParentId">Fonction pour obtenir l'ID du parent d'un élément</param>
        /// <param name="orderBy">Fonction optionnelle pour trier les éléments racines</param>
        /// <returns>Liste des éléments racines avec leurs enfants</returns>
        public static List<TreeNode<T>> BuildTree<T, TKey>(
            IEnumerable<T> items,
            Func<T, TKey> getId,
            Func<T, TKey?> getParentId,
            Func<IEnumerable<T>, IOrderedEnumerable<T>> orderBy = null)
            where TKey : struct
        {
            var itemsList = items.ToList();
            var lookup = itemsList.ToLookup(getParentId);

            // Fonction récursive pour construire l'arbre
            List<TreeNode<T>> BuildNodes(TKey? parentId)
            {
                var children = lookup[parentId].ToList();

                // Trier si une fonction de tri est fournie
                if (orderBy != null && parentId == null)
                {
                    children = orderBy(children).ToList();
                }

                return children.Select(item => new TreeNode<T>
                {
                    Data = item,
                    Children = BuildNodes(getId(item))
                }).ToList();
            }

            return BuildNodes(null);
        }

        /// <summary>
        /// Compte le nombre total de nœuds dans l'arbre (incluant les enfants)
        /// </summary>
        public static int CountNodes<T>(List<TreeNode<T>> nodes)
        {
            if (nodes == null || !nodes.Any())
                return 0;

            int count = nodes.Count;
            foreach (var node in nodes)
            {
                count += CountNodes(node.Children);
            }
            return count;
        }

        /// <summary>
        /// Trouve un nœud dans l'arbre par une condition
        /// </summary>
        public static TreeNode<T> FindNode<T>(List<TreeNode<T>> nodes, Func<T, bool> predicate)
        {
            if (nodes == null)
                return null;

            foreach (var node in nodes)
            {
                if (predicate(node.Data))
                    return node;

                var found = FindNode(node.Children, predicate);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Aplatit un arbre en une liste plate (parcours en profondeur)
        /// </summary>
        public static List<T> FlattenTree<T>(List<TreeNode<T>> nodes)
        {
            var result = new List<T>();

            void Flatten(List<TreeNode<T>> currentNodes)
            {
                if (currentNodes == null)
                    return;

                foreach (var node in currentNodes)
                {
                    result.Add(node.Data);
                    Flatten(node.Children);
                }
            }

            Flatten(nodes);
            return result;
        }
    }

    /// <summary>
    /// Représente un nœud dans l'arbre hiérarchique
    /// </summary>
    /// <typeparam name="T">Type des données du nœud</typeparam>
    public class TreeNode<T>
    {
        public T Data { get; set; }
        public List<TreeNode<T>> Children { get; set; } = new List<TreeNode<T>>();
        public bool HasChildren => Children != null && Children.Any();
    }
}