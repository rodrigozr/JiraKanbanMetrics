//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;

namespace JiraKanbanMetrics.Core.Data
{
    /// <summary>
    /// Represents the state of a ticket in a particular Jira board column
    /// </summary>
    [Serializable]
    public class Column
    {
        /// <summary>
        /// Board column name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Date when the ticket has entered the column (null if it hasn't)
        /// </summary>
        public DateTime? Entered { get; set; }
        
        /// <summary>
        /// Date when the ticket has exited the column (null if it hasn't)
        /// </summary>
        public DateTime? Exited { get; set; }

        /// <summary>
        /// List of all transitions for this particular column
        /// </summary>
        public DateRange[] Transitions { get; set; } = new DateRange[0];
    }
}