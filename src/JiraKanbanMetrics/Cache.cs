//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System.Collections.Generic;
using System.Reflection;

namespace JiraKanbanMetrics
{
    /// <summary>
    /// On-disk cache contract
    /// </summary>
    public class Cache
    {
        public string Version { get; set; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
        public List<CacheItem> Items { get; set; } = new List<CacheItem>();
    }
}