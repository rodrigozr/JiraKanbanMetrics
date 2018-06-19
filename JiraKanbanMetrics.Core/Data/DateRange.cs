//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;

namespace JiraKanbanMetrics.Core.Data
{
    /// <summary>
    /// Represent a range of dates where a ticket entered and exited a particular state
    /// </summary>
    [Serializable]
    public class DateRange
    {
        /// <summary>
        /// Date that the ticket entered this state
        /// </summary>
        public DateTime Entered { get; set; }
        
        /// <summary>
        /// Date that the ticket exited this state
        /// </summary>
        public DateTime Exited { get; set; }

        /// <summary>
        /// Determines if the provided date is within this date range (Entered=inclusive, Exited=exclusive)
        /// </summary>
        /// <param name="date">the date to check</param>
        /// <returns>true if the provided date is within this range</returns>
        public bool IsDateWithin(DateTime date)
        {
            return date >= Entered && date < Exited;
        }
    }
}