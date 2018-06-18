using System.Security;
using System.Xml.Linq;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Configuration parameters
    /// </summary>
    public class JiraKanbanConfig
    {
        /// <summary>
        /// Base HTTP address of your jira instance. E.g.: "https:/jira.yourcompany.com/jira/"
        /// </summary>
        public string JiraInstanceBaseAddress { get; set; }

        /// <summary>
        /// Username for Jira authentication
        /// </summary>
        public string JiraUsername { get; set; }

        /// <summary>
        /// Password for Jira authentication
        /// </summary>
        public SecureString JiraPassword { get; set; }

        /// <summary>
        /// Types of issues to consider as defects
        /// </summary>
        public string[] DefectIssueTypes { get; set; } = {"Defect"};

        /// <summary>
        /// Types of issues to ignore completely
        /// </summary>
        public string[] IgnoredIssueTypes { get; set; } = new string[0];

        /// <summary>
        /// Keys of issues to ignore completely
        /// </summary>
        public string[] IgnoredIssueKeys { get; set; } = new string[0];

        /// <summary>
        /// Column names to consider as queues
        /// </summary>
        public string[] QueueColumns { get; set; } = {"To Do"};

        /// <summary>
        /// Column names to consider as the "commitment start point"
        /// </summary>
        public string[] CommitmentStartColumns { get; set; } = {"To Do"};

        /// <summary>
        /// Column names to consider as the "in progress"
        /// </summary>
        public string[] InProgressStartColumns { get; set; } = {"In progress"};

        /// <summary>
        /// Column names to consider as "Done"
        /// </summary>
        public string[] DoneColumns { get; set; } = {"Done"};

        /// <summary>
        /// Name of the "backlog" column
        /// </summary>
        public string BacklogColumnName { get; set; } = "Backlog";

        /// <summary>
        /// Serializes this configuration to XML, using an encrypted password
        /// </summary>
        /// <returns>the XML</returns>
        public XElement ToXml()
        {
            return new XElement("JiraKanbanConfig",
                new XElement("JiraInstanceBaseAddress", JiraInstanceBaseAddress),
                new XElement("JiraUsername", JiraUsername),
                new XElement("JiraPassword", Crypto.Encrypt(JiraPassword)),
                new XElement("DefectIssueTypes", string.Join(",", DefectIssueTypes)),
                new XElement("IgnoredIssueTypes", string.Join(",", IgnoredIssueTypes)),
                new XElement("IgnoredIssueKeys", string.Join(",", IgnoredIssueKeys)),
                new XElement("QueueColumns", string.Join(",", QueueColumns)),
                new XElement("CommitmentStartColumns", string.Join(",", CommitmentStartColumns)),
                new XElement("InProgressStartColumns", string.Join(",", InProgressStartColumns)),
                new XElement("DoneColumns", string.Join(",", DoneColumns)),
                new XElement("BacklogColumnName", BacklogColumnName),
                null
            );
        }
        
        /// <summary>
        /// Parses configuration from file
        /// </summary>
        /// <param name="file">file path</param>
        /// <returns>configuration data</returns>
        public static JiraKanbanConfig ParseXml(string file)
        {
            var xml = XElement.Load(file);
            return new JiraKanbanConfig
            {
                JiraInstanceBaseAddress = xml.Element("JiraInstanceBaseAddress")?.Value,
                JiraUsername = xml.Element("JiraUsername")?.Value,
                JiraPassword = Crypto.Decrypt(xml.Element("JiraPassword")?.Value),
                DefectIssueTypes = (xml.Element("DefectIssueTypes")?.Value ?? "Defect").Split(','),
                IgnoredIssueTypes = (xml.Element("IgnoredIssueTypes")?.Value ?? "").Split(','),
                IgnoredIssueKeys = (xml.Element("IgnoredIssueKeys")?.Value ?? "").Split(','),
                QueueColumns = (xml.Element("QueueColumns")?.Value ?? "To Do").Split(','),
                CommitmentStartColumns = (xml.Element("CommitmentStartColumns")?.Value ?? "To Do").Split(','),
                InProgressStartColumns = (xml.Element("InProgressStartColumns")?.Value ?? "In Progress").Split(','),
                DoneColumns = (xml.Element("DoneColumns")?.Value ?? "Done").Split(','),
                BacklogColumnName = xml.Element("BacklogColumnName")?.Value ?? "Backlog",
            };
        }

    }
}