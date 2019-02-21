﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Wexflow.Core.Service.Contracts;
using System.ServiceModel.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;
using LaunchType = Wexflow.Core.Service.Contracts.LaunchType;

namespace Wexflow.Clients.WindowsService
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WexflowService : IWexflowService
    {
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "workflows")]
        public WorkflowInfo[] GetWorkflows()
        {
            return WexflowWindowsService.WexflowEngine.Workflows.Select(wf => new WorkflowInfo(wf.Id, wf.Name,
                    (LaunchType) wf.LaunchType, wf.IsEnabled, wf.Description, wf.IsRunning, wf.IsPaused,
                    wf.Period.ToString(@"dd\.hh\:mm\:ss"), wf.WorkflowFilePath, wf.IsExecutionGraphEmpty))
                .ToArray();
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "start/{id}")]
        public void StartWorkflow(string id)
        {
            WexflowWindowsService.WexflowEngine.StartWorkflow(int.Parse(id));
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "startWithData/?id={id}&hashValue={hashValue}")]
        public void StartWorkflowWithData(string id,string hashValue)
        {
            WexflowWindowsService.WexflowEngine.StartWorkflow(int.Parse(id),hashValue);
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "stop/{id}")]
        public void StopWorkflow(string id)
        {
            WexflowWindowsService.WexflowEngine.StopWorkflow(int.Parse(id));
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "suspend/{id}")]
        public void SuspendWorkflow(string id)
        {
            WexflowWindowsService.WexflowEngine.PauseWorkflow(int.Parse(id));
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "resume/{id}")]
        public void ResumeWorkflow(string id)
        {
            WexflowWindowsService.WexflowEngine.ResumeWorkflow(int.Parse(id));
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "workflow/{id}")]
        public WorkflowInfo GetWorkflow(string id)
        {
            var wf = WexflowWindowsService.WexflowEngine.GetWorkflow(int.Parse(id));
            if (wf != null)
            {
                return new WorkflowInfo(wf.Id, wf.Name, (LaunchType) wf.LaunchType, wf.IsEnabled, wf.Description,
                    wf.IsRunning, wf.IsPaused, wf.Period.ToString(@"dd\.hh\:mm\:ss") , wf.WorkflowFilePath, wf.IsExecutionGraphEmpty);
            }

            return null;
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "tasks/{id}")]
        public TaskInfo[] GetTasks(string id)
        {
            var wf = WexflowWindowsService.WexflowEngine.GetWorkflow(int.Parse(id));
            if (wf != null)
            {
                IList<TaskInfo> taskInfos = new List<TaskInfo>();

                foreach (var task in wf.Taks)
                {
                    IList<SettingInfo> settingInfos = new List<SettingInfo>();

                    foreach (var setting in task.Settings)
                    {
                        IList<AttributeInfo> attributeInfos = new List<AttributeInfo>();

                        foreach (var attribute in setting.Attributes)
                        {
                            AttributeInfo attributeInfo = new AttributeInfo(attribute.Name, attribute.Value);
                            attributeInfos.Add(attributeInfo);
                        }

                        SettingInfo settingInfo =
                            new SettingInfo(setting.Name, setting.Value, attributeInfos.ToArray());
                        settingInfos.Add(settingInfo);
                    }

                    TaskInfo taskInfo = new TaskInfo(task.Id, task.Name, task.Description, task.IsEnabled,
                        settingInfos.ToArray());

                    taskInfos.Add(taskInfo);
                }
                return taskInfos.ToArray();
            }
            return null;
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "xml/{id}")]
        public string GetWorkflowXml(string id)
        {
            var wf = WexflowWindowsService.WexflowEngine.GetWorkflow(int.Parse(id));
            if (wf != null)
            {
                return wf.XDoc.ToString();
            }
            return string.Empty;
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "taskToXml")]
        public string GetTaskXml(Stream streamdata)
        {
            try
            {
                StreamReader reader = new StreamReader(streamdata);
                string json = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();

                JObject task = JObject.Parse(json);

                int taskId = (int)task.SelectToken("Id");
                string taskName = (string)task.SelectToken("Name");
                string taskDesc = (string)task.SelectToken("Description");
                bool isTaskEnabled = (bool)task.SelectToken("IsEnabled");

                var xtask = new XElement("Task"
                    , new XAttribute("id", taskId)
                    , new XAttribute("name", taskName)
                    , new XAttribute("description", taskDesc)
                    , new XAttribute("enabled", isTaskEnabled.ToString().ToLower())
                );

                var settings = task.SelectToken("Settings");
                foreach (var setting in settings)
                {
                    string settingName = (string)setting.SelectToken("Name");
                    string settingValue = (string)setting.SelectToken("Value");

                    var xsetting = new XElement("Setting"
                        , new XAttribute("name", settingName)
                    );

                    if (!string.IsNullOrEmpty(settingValue))
                    {
                        xsetting.SetAttributeValue("value", settingValue);
                    }

                    if (settingName == "selectFiles" || settingName == "selectAttachments")
                    {
                        if (!string.IsNullOrEmpty(settingValue))
                        {
                            xsetting.SetAttributeValue("value", settingValue);
                        }
                    }
                    else
                    {
                        xsetting.SetAttributeValue("value", settingValue);
                    }

                    var attributes = setting.SelectToken("Attributes");
                    foreach (var attribute in attributes)
                    {
                        string attributeName = (string)attribute.SelectToken("Name");
                        string attributeValue = (string)attribute.SelectToken("Value");
                        xsetting.SetAttributeValue(attributeName, attributeValue);
                    }

                    xtask.Add(xsetting);
                }

                return xtask.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return string.Empty;
            }
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "save")]
        public bool SaveWorkflow(Stream streamdata)
        {
            try
            {
                StreamReader reader = new StreamReader(streamdata);
                string json = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();

                JObject o = JObject.Parse(json);
                var wi = o.SelectToken("WorkflowInfo");
                bool createExecutionGraphXML = false; // Tim

                var isNew = (bool) wi.SelectToken("IsNew");
                if (isNew)
                {
                    XNamespace xn = "urn:wexflow-schema";
                    var xdoc = new XDocument();
                 
                    int workflowId = (int)wi.SelectToken("Id");
                    string workflowName = (string)wi.SelectToken("Name");
                    LaunchType workflowLaunchType = (LaunchType)((int)wi.SelectToken("LaunchType"));
                    string p = (string)wi.SelectToken("Period");
                    TimeSpan workflowPeriod = TimeSpan.Parse(string.IsNullOrEmpty(p) ? "00.00:00:00" : p);
                    bool isWorkflowEnabled = (bool)wi.SelectToken("IsEnabled");
                    string workflowDesc = (string)wi.SelectToken("Description");

                    // tasks
                    var xtasks = new XElement(xn + "Tasks");
                    var tasks = o.SelectToken("Tasks");
                    foreach (var task in tasks)
                    {
                        int taskId = (int)task.SelectToken("Id");
                        string taskName = (string)task.SelectToken("Name");
                        string taskDesc = (string)task.SelectToken("Description");
                        bool isTaskEnabled = (bool)task.SelectToken("IsEnabled");

                        var xtask = new XElement(xn + "Task"
                            , new XAttribute("id", taskId)
                            , new XAttribute("name", taskName)
                            , new XAttribute("description", taskDesc)
                            , new XAttribute("enabled", isTaskEnabled.ToString().ToLower())
                        );

                        var settings = task.SelectToken("Settings");
                        foreach (var setting in settings)
                        {
                            string settingName = (string)setting.SelectToken("Name");
                            string settingValue = (string)setting.SelectToken("Value");

                            var xsetting = new XElement(xn + "Setting"
                                , new XAttribute("name", settingName)
                            );

                            if (!string.IsNullOrEmpty(settingValue))
                            {
                                xsetting.SetAttributeValue("value", settingValue);
                            }

                            if (settingName == "selectFiles" || settingName == "selectAttachments")
                            {
                                if (!string.IsNullOrEmpty(settingValue))
                                {
                                    xsetting.SetAttributeValue("value", settingValue);
                                }
                            }
                            else
                            {
                                xsetting.SetAttributeValue("value", settingValue);
                            }

                            var attributes = setting.SelectToken("Attributes");
                            foreach (var attribute in attributes)
                            {
                                string attributeName = (string)attribute.SelectToken("Name");
                                string attributeValue = (string)attribute.SelectToken("Value");
                                xsetting.SetAttributeValue(attributeName, attributeValue);
                            }

                            xtask.Add(xsetting);
                        }

                        if (createExecutionGraphXML == false)
                        {
                            createExecutionGraphXML = (taskName == "IfCheck" || taskName == "SwitchCheck" || taskName == "WhileCheck") ? true : false;
                        } // Tim

                        xtasks.Add(xtask);
                    }

                    // Create the ExecutionGraphXML
                    var xExecutionGraph = new XElement(xn + "ExecutionGraph");                    
                    if (createExecutionGraphXML == true)
                    {
                        var taskList = xtasks.Elements().ToList();
                        var rootElements = taskList.Where(
                            e => string.IsNullOrEmpty(
                                new Core.WexflowGraphXML(taskList).GetSettingValue(e.Elements().ToList(), "parent")
                            ) == false
                        ).ToList();

                        xExecutionGraph = new Core.WexflowGraphXML(taskList).GetGraphXElements(rootElements, xExecutionGraph, true);
                    } // Tim

                    // root
                    var xwf = new XElement(xn + "Workflow"
                        , new XAttribute("id", workflowId)
                        , new XAttribute("name", workflowName)
                        , new XAttribute("description", workflowDesc)
                            , new XElement(xn + "Settings"
                                , new XElement(xn + "Setting"
                                    , new XAttribute("name", "launchType")
                                    , new XAttribute("value", workflowLaunchType.ToString().ToLower()))
                                , new XElement(xn + "Setting"
                                    , new XAttribute("name", "enabled")
                                    , new XAttribute("value", isWorkflowEnabled.ToString().ToLower()))
                                , new XElement(xn + "Setting"
                                    , new XAttribute("name", "period")
                                    , new XAttribute("value", workflowPeriod.ToString(@"dd\.hh\:mm\:ss")))
                                        )
                            , xtasks
                            , xExecutionGraph // Tim
                        );

                    xdoc.Add(xwf);

                    var path = (string) wi.SelectToken("Path");
                    xdoc.Save(path);
                }
                else
                {
                    int id = int.Parse((string)o.SelectToken("Id"));
                    var wf = WexflowWindowsService.WexflowEngine.GetWorkflow(id);
                    if (wf != null)
                    {
                        var xdoc = wf.XDoc;

                        int workflowId = (int) wi.SelectToken("Id");
                        string workflowName = (string) wi.SelectToken("Name");
                        LaunchType workflowLaunchType = (LaunchType) ((int) wi.SelectToken("LaunchType"));
                        string p = (string) wi.SelectToken("Period");
                        TimeSpan workflowPeriod = TimeSpan.Parse(string.IsNullOrEmpty(p) ? "00.00:00:00" : p);
                        bool isWorkflowEnabled = (bool) wi.SelectToken("IsEnabled");
                        string workflowDesc = (string) wi.SelectToken("Description");

                        //if(xdoc.Root == null) throw new Exception("Root is null");
                        xdoc.Root.Attribute("id").Value = workflowId.ToString();
                        xdoc.Root.Attribute("name").Value = workflowName;
                        xdoc.Root.Attribute("description").Value = workflowDesc;

                        var xwfEnabled = xdoc.Root.XPathSelectElement("wf:Settings/wf:Setting[@name='enabled']",
                            wf.XmlNamespaceManager);
                        xwfEnabled.Attribute("value").Value = isWorkflowEnabled.ToString().ToLower();
                        var xwfLaunchType = xdoc.Root.XPathSelectElement("wf:Settings/wf:Setting[@name='launchType']",
                            wf.XmlNamespaceManager);
                        xwfLaunchType.Attribute("value").Value = workflowLaunchType.ToString().ToLower();

                        var xwfPeriod = xdoc.Root.XPathSelectElement("wf:Settings/wf:Setting[@name='period']",
                            wf.XmlNamespaceManager);
                        //if (workflowLaunchType == LaunchType.Periodic)
                        //{
                        if (xwfPeriod != null)
                        {
                            xwfPeriod.Attribute("value").Value = workflowPeriod.ToString(@"dd\.hh\:mm\:ss");
                        }
                        else
                        {
                            xdoc.Root.XPathSelectElement("wf:Settings", wf.XmlNamespaceManager)
                                .Add(new XElement(wf.XNamespaceWf + "Setting", new XAttribute("name", "period"),
                                    new XAttribute("value", workflowPeriod.ToString())));
                        }
                        //}

                        var xtasks = xdoc.Root.Element(wf.XNamespaceWf + "Tasks");
                        var alltasks = xtasks.Elements(wf.XNamespaceWf + "Task");
                        alltasks.Remove();

                        var tasks = o.SelectToken("Tasks");
                        foreach (var task in tasks)
                        {
                            int taskId = (int) task.SelectToken("Id");
                            string taskName = (string) task.SelectToken("Name");
                            string taskDesc = (string) task.SelectToken("Description");
                            bool isTaskEnabled = (bool) task.SelectToken("IsEnabled");

                            var xtask = new XElement(wf.XNamespaceWf + "Task"
                                , new XAttribute("id", taskId)
                                , new XAttribute("name", taskName)
                                , new XAttribute("description", taskDesc)
                                , new XAttribute("enabled", isTaskEnabled.ToString().ToLower())
                            );

                            var settings = task.SelectToken("Settings");
                            foreach (var setting in settings)
                            {
                                string settingName = (string) setting.SelectToken("Name");
                                string settingValue = (string) setting.SelectToken("Value");

                                var xsetting = new XElement(wf.XNamespaceWf + "Setting"
                                    , new XAttribute("name", settingName)
                                );

                                if (settingName == "selectFiles" || settingName == "selectAttachments")
                                {
                                    if (!string.IsNullOrEmpty(settingValue))
                                    {
                                        xsetting.SetAttributeValue("value", settingValue);
                                    }
                                }
                                else
                                {
                                    xsetting.SetAttributeValue("value", settingValue);
                                }

                                var attributes = setting.SelectToken("Attributes");
                                foreach (var attribute in attributes)
                                {
                                    string attributeName = (string) attribute.SelectToken("Name");
                                    string attributeValue = (string) attribute.SelectToken("Value");
                                    xsetting.SetAttributeValue(attributeName, attributeValue);
                                }

                                xtask.Add(xsetting);
                            }

                            if (createExecutionGraphXML == false)
                            {
                                createExecutionGraphXML = (taskName == "IfCheck" || taskName == "SwitchCheck" || taskName == "WhileCheck") ? true : false;
                            } // Tim

                            xtasks.Add(xtask);
                        }

                        // Create the ExecutionGraphXML                        
                        if (xdoc.Root.Element(wf.XNamespaceWf + "ExecutionGraph") != null)
                            xdoc.Root.Element(wf.XNamespaceWf + "ExecutionGraph").Remove();

                        var xExecutionGraph = new XElement(wf.XNamespaceWf + "ExecutionGraph");
                        if (createExecutionGraphXML == true)
                        {
                            var taskList = xtasks.Elements().ToList();
                            var rootElements = taskList.Where(
                                e => string.IsNullOrEmpty(
                                    new Core.WexflowGraphXML(taskList).GetSettingValue(e.Elements().ToList(), "parent")
                                ) == false
                            ).ToList();

                            xExecutionGraph = new Core.WexflowGraphXML(taskList).GetGraphXElements(rootElements, xExecutionGraph, true);
                            xdoc.Root.Add(xExecutionGraph);
                        } // Tim

                        xdoc.Save(wf.WorkflowFilePath);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "taskNames")]
        public string[] GetTaskNames()
        {
            JArray array = JArray.Parse(File.ReadAllText(WexflowWindowsService.WexflowEngine.TasksNamesFile));
            return array.ToObject<string[]>().OrderBy(x => x).ToArray();
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "workflowsFolder")]
        public string GetWorkflowsFolder()
        {
            return WexflowWindowsService.WexflowEngine.WorkflowsFolder;
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "isWorkflowIdValid/{id}")]
        public bool IsWorkflowIdValid(string id)
        {
            var workflowId = int.Parse(id);
            foreach (var workflow in WexflowWindowsService.WexflowEngine.Workflows)
            {
                if (workflow.Id == workflowId) return false;
            }
            return true;
        }

        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "delete/{id}")]
        public bool DeleteWorkflow(string id)
        {
            try
            {
                var wf = WexflowWindowsService.WexflowEngine.GetWorkflow(int.Parse(id));
                if (wf != null)
                {
                    string destPath = Path.Combine(WexflowWindowsService.WexflowEngine.TrashFolder, Path.GetFileName(wf.WorkflowFilePath));
                    if (File.Exists(destPath))
                    {
                        destPath = Path.Combine(WexflowWindowsService.WexflowEngine.TrashFolder
                            , Path.GetFileNameWithoutExtension(destPath) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(destPath));
                    }
                    File.Move(wf.WorkflowFilePath, destPath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "settings/{taskName}")]
        public string[] GetSettings(string taskName)
        {
            JObject o = JObject.Parse(File.ReadAllText(WexflowWindowsService.WexflowEngine.TasksSettingsFile));
            var token = o.SelectToken(taskName);
            return token != null ? token.ToObject<string[]>().OrderBy(x => x).ToArray() : new string[]{};
        }

        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "graph/{id}")]
        public Core.ExecutionGraph.GraphNode[] GetExecutionGraph(string id)
        {
            var wf = WexflowWindowsService.WexflowEngine.GetWorkflow(int.Parse(id));
            if (wf != null)
            {
                return new Core.WexflowGraph(wf).GetGraphNodes();
            }

            return null;
        }        
    }
}
