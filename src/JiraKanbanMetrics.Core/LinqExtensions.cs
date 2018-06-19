//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Extension methods for LINQ enumerables
    /// </summary>
    internal static class LinqExtensions
    {
        /// <summary>
        /// Retrives paged items from an enumerable
        /// </summary>
        /// <typeparam name="T">enumerable type</typeparam>
        /// <param name="source">enumerable source</param>
        /// <param name="pageSize">size of the page</param>
        /// <returns>paged enumerable</returns>
        /// <example>
        /// Retrives items from a list, three by three
        /// <code>
        /// var items = new []{1,2,3,4,5,6,7,8};
        /// var paged = items.Paged(3);
        /// // paged: [[1,2,3], [4,5,6], [7,8]]
        /// </code>
        /// </example>
        public static IEnumerable<IEnumerable<T>> Paged<T>(this IEnumerable<T> source, int pageSize)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var currentPage = new List<T>(pageSize)
                    {
                        enumerator.Current
                    };

                    while (currentPage.Count < pageSize && enumerator.MoveNext())
                    {
                        currentPage.Add(enumerator.Current);
                    }

                    yield return currentPage;
                }
            }
        }

        /// <summary>
        /// Computes the Nth percentile from a list of numbers
        /// </summary>
        /// <typeparam name="TSource">enumerable type</typeparam>
        /// <param name="source">enumerable source</param>
        /// <param name="percentile">which percentile to compute (95 = 95th percentile)</param>
        /// <param name="keySelector">a delegate which extracts 'double' values from the list</param>
        /// <returns>the calculated percentile</returns>
        public static double Percentile<TSource>(this IEnumerable<TSource> source, int percentile,
            Func<TSource, double> keySelector)
        {
            var values = source.Select(keySelector).OrderBy(_ => _).ToList();
            var qty = (values.Count * percentile / 100);
            return qty == 0 ? 0 : values[qty - 1];
        }

    }
}