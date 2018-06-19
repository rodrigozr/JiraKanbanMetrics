//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;

namespace JiraKanbanMetrics.Core.Data
{
    /// <summary>
    /// Represents a single issue that has reached the "Done"
    /// state, and thus has full metrics available
    /// </summary>
    [Serializable]
    public class DoneIssue
    {
        /// <summary>
        /// Issue key
        /// </summary>
        public string IssueKey { get; set; }
        
        /// <summary>
        /// Issue type
        /// </summary>
        public string IssueType { get; set; }
        
        /// <summary>
        /// Calculated issue lead time
        /// </summary>
        public int LeadTime { get; set; }
        
        /// <summary>
        /// Issue touch time
        /// </summary>
        public int TouchTime { get; set; }
        
        /// <summary>
        /// Issue queue time
        /// </summary>
        public int QueueTime { get; set; }
        
        /// <summary>
        /// Date of when this issue has entered its "commitment point"
        /// </summary>
        public DateTime ToDo { get; set; }
        
        /// <summary>
        /// Date of when this issue has started activity
        /// </summary>
        public DateTime InProgress { get; set; }
        
        /// <summary>
        /// Date of when this issue as been considered "Done"
        /// </summary>
        public DateTime Done { get; set; }
        
        /// <summary>
        /// Individual column statistics for this issue
        /// </summary>
        public Column[] Columns { get; set; }
    }
}