using System;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.SwitchCheck
{
    public class SwitchCheck : Task
    {
        public string ParentID { get; private set; }

        public string CheckID { get; private set; }

        public string DefaultTasks { get; private set; }

        public string CaseTasks { get; private set; }

        public SwitchCheck(XElement xe, Workflow wf) : base(xe, wf)
        {
            this.ParentID = GetSetting("parent");
            this.CheckID = GetSetting("checkID");
            this.DefaultTasks = GetSetting("defaultTasks");
            this.CaseTasks = GetSetting("caseTasks");
        }

        public override TaskStatus Run()
        {
            return new TaskStatus(Status.Success);
        }
    }
}
