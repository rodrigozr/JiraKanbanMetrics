using System;
using System.Linq;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Extension methods for strings
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Determines if a given issue type is a defect, given a configuration
        /// </summary>
        /// <param name="issueType">issue type</param>
        /// <param name="config">kanban configuration</param>
        /// <returns>true if the issue type is a defect</returns>
        public static bool IsDefect(this string issueType, JiraKanbanConfig config)
        {
            var defects = config.DefectIssueTypes;
            return defects.Any(s => s.Equals(issueType, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Equals with StringComparison.InvariantCultureIgnoreCase
        /// </summary>
        /// <param name="text">first text</param>
        /// <param name="other">other text</param>
        /// <returns>true if equals ignoring case</returns>
        public static bool EqualsIgnoreCase(this string text, string other)
        {
            return text.Equals(other, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}