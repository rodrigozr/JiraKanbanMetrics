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
        /// Serializes this configuration to XML, using an encrypted password
        /// </summary>
        /// <returns>the XML</returns>
        public XElement ToXml()
        {
            return new XElement("JiraKanbanConfig",
                new XElement("JiraInstanceBaseAddress", JiraInstanceBaseAddress),
                new XElement("JiraUsername", JiraUsername),
                new XElement("JiraPassword", Crypto.Encrypt(JiraPassword))
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
                JiraPassword = Crypto.Decrypt(xml.Element("JiraPassword")?.Value)
            };
        }

    }
}