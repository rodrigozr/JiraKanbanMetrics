//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;
using JiraKanbanMetrics.Core.Data;

namespace JiraKanbanMetrics
{
    /// <summary>
    /// Individual cached results
    /// </summary>
    public class CacheItem
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public int BoardId { get; set; }
        public int[] QuickFilters { get; set; } = new int[0];
        public Issue[] Issues { get; set; }
    }
}