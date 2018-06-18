using System;
using System.Linq;

namespace JiraKanbanMetrics.Core.Data
{
    /// <summary>
    /// Represents a single issue, along with its metrics
    /// </summary>
    [Serializable]
    public class Issue
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
        /// Date and time of when the issue was created
        /// </summary>
        public DateTime Created { get; set; }
        
        /// <summary>
        /// Information about this issue on each board column
        /// </summary>
        public Column[] Columns { get; set; } = new Column[0];

        /// <summary>
        /// Retrieves a column information with the given name, or creates a new one if it does not exist
        /// </summary>
        /// <param name="columnName">column name</param>
        /// <returns>column data</returns>
        public Column Col(string columnName)
        {
            return Columns.FirstOrDefault(_ => _.Name == columnName) ?? new Column { Name = columnName };
        }

        /// <summary>
        /// Retrieves the date when a ticket has entered any of the given columns
        /// </summary>
        /// <param name="columnNames">column names</param>
        /// <returns>date or null if none</returns>
        public DateTime? Entered(params string[] columnNames)
        {
            return columnNames.Select(col => Col(col).Entered).FirstOrDefault(ts => ts.HasValue);
        }

        /// <summary>
        /// Retrieves the date when a ticket has first entered any of the given columns
        /// </summary>
        /// <param name="columnNames">column names</param>
        /// <returns>date or null if none</returns>
        public DateTime? FirstEntered(params string[] columnNames)
        {
            return columnNames.SelectMany(col => Col(col).Transitions).Select(_ => _.Entered).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the date when a ticket has exited any of the given columns
        /// </summary>
        /// <param name="columnNames">column names</param>
        /// <returns>date or null if none</returns>
        public DateTime? Exited(params string[] columnNames)
        {
            return columnNames.Select(col => Col(col).Exited).FirstOrDefault(ts => ts.HasValue);
        }

        /// <summary>
        /// Calculates the lead time for this issue, considering the given configuration
        /// </summary>
        /// <param name="config">kanban configuration</param>
        /// <returns>issue lead time, in days</returns>
        public int LeadTime(JiraKanbanConfig config)
        {
            var start = Entered(config.CommitmentStartColumns);
            var end = FirstEntered(config.DoneColumns);
            if (!start.HasValue || !end.HasValue) return 0;
            if (end < start) end = Entered(config.DoneColumns);
            if (!end.HasValue) return 0;
            return (int)Math.Round((end.Value - start.Value).TotalDays);
        }

        /// <summary>
        /// Calculates the "touch time" for this issue, considering the given configuration
        /// </summary>
        /// <param name="config">kanban configuration</param>
        /// <returns>issue touch time, in days</returns>
        public int TouchTime(JiraKanbanConfig config)
        {
            var leadTime = LeadTime(config);
            if (leadTime == 0) return 0;
            var queueTime = Math.Min(leadTime, QueueTime(config));
            return (leadTime - queueTime);
        }

        /// <summary>
        /// Calculates the "queue time" for this issue, considering the given configuration
        /// </summary>
        /// <param name="config">kanban configuration</param>
        /// <returns>issue queue time, in days</returns>
        public int QueueTime(JiraKanbanConfig config)
        {
            // ReSharper disable PossibleInvalidOperationException
            return (int)Math.Round(Columns
                .Where(c => c.Entered.HasValue && c.Exited.HasValue)
                .Where(c => config.QueueColumns.Any(queue => c.Name.Equals(queue, StringComparison.InvariantCultureIgnoreCase)))
                .Sum(c => (c.Exited.Value - c.Entered.Value).TotalDays));
            // ReSharper restore PossibleInvalidOperationException
        }
    }
}