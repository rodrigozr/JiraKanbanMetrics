using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JiraKanbanMetrics.Core
{
    public class MetricsExtractor : IDisposable
    {
        private HttpClient _client;

        public MetricsExtractor(JiraKanbanConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.JiraInstanceBaseAddress)) throw new ArgumentNullException(nameof(config.JiraInstanceBaseAddress));
            if (string.IsNullOrWhiteSpace(config.JiraUsername)) throw new ArgumentNullException(nameof(config.JiraUsername));
            if (config.JiraPassword == null || config.JiraPassword.Length == 0) throw new ArgumentNullException(nameof(config.JiraPassword));

            _client = new HttpClient
            {
                BaseAddress = new Uri(config.JiraInstanceBaseAddress)
            };

            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.JiraUsername}:{Crypto.GetStr(config.JiraPassword)}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        public async Task<Issue[]> GetBoardDataAsync(int boardId, int[] quickFilters)
        {
            var filters = quickFilters.Length == 0 ? "" : string.Join("&activeQuickFilters=", new[] { "" }.Concat(quickFilters.Select(_ => _.ToString())));
            var res = await _client.GetAsync($"rest/greenhopper/1.0/xboard/work/allData.json?rapidViewId={boardId}{filters}");
            var responseBody = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception("Error connecting to Jira: " + res.StatusCode);
            var json = JObject.Parse(responseBody);

            var issues = await GetIssues(json);
            return issues.ToArray();
        }

        private static Column[] GetIssueColumns(JObject issue, Dictionary<string, int[]> boardColumns)
        {
            var statusToColumn = boardColumns.SelectMany(kv => kv.Value.Select(id => (id, kv.Key))).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

            var transitions = (
                from history in issue["changelog"]["histories"]
                from item in history["items"]
                where (string)item["field"] == "status"
                let fromId = (int)item["from"]
                let toId = (int)item["to"]
                let fromColumn = statusToColumn.ContainsKey(fromId) ? statusToColumn[fromId] : null
                let toColumn = statusToColumn.ContainsKey(toId) ? statusToColumn[toId] : null
                where fromColumn != toColumn
                select new
                {
                    Timestamp = ((DateTime)history["created"]).ToUniversalTime(),
                    From = fromColumn,
                    To = toColumn,
                }).ToList();

            var columns = (
                from name in boardColumns.Keys
                let allEntered = transitions.Where(_ => _.To == name).OrderBy(_ => _.Timestamp).Select(_ => _.Timestamp).ToList()
                let allExited = transitions.Where(_ => _.From == name).OrderBy(_ => _.Timestamp).Select(_ => _.Timestamp).Concat(new DateTime[] { DateTime.MaxValue }).ToList()
                let entered = transitions.Where(_ => _.To == name).OrderByDescending(_ => _.Timestamp).Select(_ => (DateTime?)_.Timestamp).FirstOrDefault()
                let exited = transitions.Where(_ => _.From == name).OrderByDescending(_ => _.Timestamp).Select(_ => (DateTime?)_.Timestamp).FirstOrDefault()
                select new Column
                {
                    Name = name,
                    Entered = entered,
                    Exited = exited,
                    Transitions = allEntered.Select(e => new DateRange
                    {
                        Entered = e,
                        Exited = allExited.First(date => date >= e)
                    }).ToArray()
                }
            ).ToArray();

            return columns;
        }

        private static Dictionary<string, int[]> GetAllColumnsStatusIds(JObject json)
        {
            return json["columnsData"]["columns"]
                .Select(col => new KeyValuePair<string, int[]>((string)col["name"], col["statusIds"].ToObject<int[]>()))
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        private async Task<IEnumerable<Issue>> GetIssues(JObject json)
        {
            var boardColumns = GetAllColumnsStatusIds(json);

            var issueKeys = json["issuesData"]["issues"].Select(issue => (string)issue["key"]).OrderBy(_ => _).ToList();
            Console.WriteLine($"Analysing {issueKeys.Count} issues...");
            var pageSize = 100;
            var pagedKeys = issueKeys.Paged(pageSize);
            var result = new List<Issue>();
            foreach (var keys in pagedKeys)
            {
                var keysJql = string.Join(",", keys);
                var res = await _client.GetAsync($"rest/api/2/search?jql=key%20in%20({keysJql})&expand=changelog&fields=issuetype,created&maxResults={pageSize}");
                var issuesJson = JObject.Parse((await res.Content.ReadAsStringAsync()));
                Console.WriteLine($"Retrieved details for {issuesJson["issues"].Count()} issues...");
                result.AddRange(issuesJson["issues"]
                    .Select(issue => new Issue
                    {
                        IssueKey = (string)issue["key"],
                        IssueType = (string)issue["fields"]["issuetype"]["name"],
                        Created = ((DateTime)issue["fields"]["created"]).ToUniversalTime(),
                        Columns = GetIssueColumns((JObject)issue, boardColumns),
                    }));
            }
            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }
    }

    public class DoneIssue
    {
        public string IssueKey { get; set; }
        public string IssueType { get; set; }
        public int LeadTime { get; set; }
        public int TouchTime { get; set; }
        public int QueueTime { get; set; }
        public DateTime ToDo { get; set; }
        public DateTime InProgress { get; set; }
        public DateTime Done { get; set; }
        public Column[] Columns { get; set; }
    }

    [Serializable]
    public class Issue
    {
        public string IssueKey { get; set; }
        public string IssueType { get; set; }
        public DateTime Created { get; set; }
        public Column[] Columns { get; set; }

        public Column Col(string columnName)
        {
            return Columns.FirstOrDefault(_ => _.Name == columnName) ?? new Column() { Name = columnName };
        }

        public DateTime? Entered(params string[] columnNames)
        {
            return columnNames.Select(col => Col(col).Entered).FirstOrDefault(ts => ts.HasValue);
        }

        public DateTime? FirstEntered(params string[] columnNames)
        {
            return columnNames.SelectMany(col => Col(col).Transitions).Select(_ => _.Entered).FirstOrDefault();
        }

        public DateTime? Exited(params string[] columnNames)
        {
            return columnNames.Select(col => Col(col).Exited).FirstOrDefault(ts => ts.HasValue);
        }

        private static readonly string[] DefaultStartColumns = { "To Do" };
        private static readonly string[] DefaultEndColumns = { "Review QA", "Done" };

        public int LeadTime(string[] startColumns = null, string[] endColumns = null)
        {
            var start = Entered(startColumns ?? DefaultStartColumns);
            var end = FirstEntered(endColumns ?? DefaultEndColumns);
            if (!start.HasValue || !end.HasValue) return 0;
            if (end < start) end = Entered(endColumns ?? DefaultEndColumns);
            return (int)Math.Round((end - start).Value.TotalDays);
        }

        public int TouchTime(string[] startColumns = null, string[] endColumns = null,
            Func<Column, bool> queueFilter = null)
        {
            var leadTime = LeadTime(startColumns, endColumns);
            if (leadTime == 0) return 0;
            var queueTime = Math.Min(leadTime, QueueTime(queueFilter));
            return (leadTime - queueTime);
        }

        public int QueueTime(Func<Column, bool> queueFilter = null)
        {
            return (int)Math.Round(Columns
                .Where(c => c.Entered.HasValue && c.Exited.HasValue)
                .Where(queueFilter ?? DefaultQueueFilter)
                .Sum(c => (c.Exited.Value - c.Entered.Value).TotalDays));
        }

        private static bool DefaultQueueFilter(Column col)
        {
            var n = col.Name.ToLower();
            return n.Contains("waiting") || n == "to do";
        }
    }


    public static class StringExtensions
    {
        public static bool IsDefect(this string issueType)
        {
            var s = issueType.ToLower();
            return s == "defect" || s == "issue";
        }
    }
}
