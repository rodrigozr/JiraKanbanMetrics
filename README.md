# JiraKanbanMetrics
Extract Kanban metrics from Jira Agile boards using the REST API

[Download](https://github.com/rodrigozr/JiraKanbanMetrics/releases)

# Table of contents

   * [Quick start step-by-step](#quick-start-step-by-step)
   * [Sample generated charts](#sample-generated-charts)
   * [Configuration File](#configuration-file)

# Quick start step-by-step
## Step 1: Generate a configuration file

`JiraKanbanMetrics.exe configure --ConfigFile config.xml --JiraUsername MYUSER --JiraInstanceBaseAddress https://jira.YOURCOMPANY.com/jira/ --BoardId 123456`

This will prompt for your Jira password:

```
Enter the Jira password for user 'MYUSER':
*********
Configuration file generated at: config.xml
```

**IMPORTANT:** Your password will be stored in an encrypted form using a single DES encryption and a hard-coded encryption key. For optimal security, you can opt to avoid storing your password by using the `--NoStorePassword` option.

## Step 2: Edit the configuration file
Edit the generated `config.xml` file, which should look like this:
```xml
<?xml version="1.0" encoding="utf-8"?>
<JiraKanbanConfig>
  <JiraInstanceBaseAddress>https://jira.YOURCOMPANY.com/jira/</JiraInstanceBaseAddress>
  <JiraUsername>MYUSER</JiraUsername>
  <JiraPassword>4an3OGlEUd2Xr31U/w5Dhy0U0QV36LKs</JiraPassword>
  <BoardId>123456</BoardId>
  <QuickFilters></QuickFilters>
  <DefectIssueTypes>Defect</DefectIssueTypes>
  <IgnoredIssueTypes></IgnoredIssueTypes>
  <IgnoredIssueKeys></IgnoredIssueKeys>
  <QueueColumns>To Do</QueueColumns>
  <CommitmentStartColumns>To Do</CommitmentStartColumns>
  <InProgressStartColumns>In progress</InProgressStartColumns>
  <DoneColumns>Done</DoneColumns>
  <BacklogColumnName>Backlog</BacklogColumnName>
  <MonthsToAnalyse>5</MonthsToAnalyse>
</JiraKanbanConfig>
```
Please refer to the [Configuration File](#configuration-file) section for the details about each configuration option.

## Step 3: Run it

`JiraKanbanMetrics.exe generate --ConfigFile config.xml`

This will connect to your Jira instance, extract metrics and generate a set of **".png"** files on your current working directory:
  * FlowEfficiencyChart.png
  * WeeklyThroughputChart.png
  * WeeklyThroughputHistogramChart.png
  * LeadTimeControlChart.png
  * LeadTimeHistogramChart.png
  * CumulativeFlowDiagramChart.png

# Sample generated charts

## FlowEfficiencyChart.png
![FlowEfficiencyChart](images/FlowEfficiencyChart.png)
## WeeklyThroughputChart.png
![WeeklyThroughputChart](images/WeeklyThroughputChart.png)
## WeeklyThroughputHistogramChart.png
![WeeklyThroughputHistogramChart](images/WeeklyThroughputHistogramChart.png)
## LeadTimeControlChart.png
![LeadTimeControlChart](images/LeadTimeControlChart.png)
## LeadTimeHistogramChart.png
![LeadTimeHistogramChart](images/LeadTimeHistogramChart.png)
## CumulativeFlowDiagramChart.png
![CumulativeFlowDiagramChart](images/CumulativeFlowDiagramChart.png)

# Configuration File
The configuration file used by JiraKanbanMetrics.exe contains the following options:
  * **JiraInstanceBaseAddress**
    
    The base URL of the Jira instance. This is a mandatory configuration.
  * **JiraUsername**
    
    Your Jira username. This is a mandatory configuration.
  * **JiraPassword**
    
    Your Jira encrypted password. This configuration can only be set be using the `configure` command-line argument. The tool will automatically encrypt your password and store it on this configuration.
    
    If this configuration is not set, your password will be requested when running the tool.
  * BoardId
  
    The default Jira Kanban Board ID to use when generating metrics. Please notice that you can override this by using the `--BoardId` command-line option.
  * **QuickFilters**
  
    List of Jira "quick filter IDs" to use during the metrics extraction.
  * **DefectIssueTypes**
  
    Comma-separated list of issue types to consider as **Defect**.
    **Default: Defect**
  * **IgnoredIssueTypes**
  
    Comma-separated list of issue types to ignore and exclude from the metrics.
  * **IgnoredIssueKeys**
  
    Comma-separated list of issue keys to ignore and exclude from the metrics.
  * **QueueColumns**
  
    Comma-separated list of column names to consider as "queues" in the Kanban Board.
    **Default: To Do**
  * **CommitmentStartColumns**
  
    Comma-separated list of column names to consider as the "commitment start point" in the Kanban Board.
    
    If more than one column is configured, then the first one that the issue has passed thru will be used.
    **Default: To Do**
  * **InProgressStartColumns**
  
    Comma-separated list of column names to consider as the "working start point" in the Kanban Board.
    
    If more than one column is configured, then the first one that the issue has passed thru will be used.
    **Default: In progress**
  * **DoneColumns**
  
    Comma-separated list of column names to consider as the "Done" in the Kanban Board.
    
    If more than one column is configured, then the first one that the issue has passed thru will be used.
    **Default: Done**
  * **BacklogColumnName**
  
    Name of your "Backlog" column in the Kanban board. This is only used for the "CDF" chart.
    **Default: Backlog**
  * **MonthsToAnalyse**
  
    Number of months back in time to consider when analysing data.
    **Default: 5**
