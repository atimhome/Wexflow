using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.WhileCheck
{
    public class WhileCheck : Task
    {
        public string ParentID { get; private set; }

        public string CheckID { get; private set; }

        public string WhileTasks { get; private set; }        

        public WhileCheck(XElement xe, Workflow wf) : base(xe, wf)
        {
            this.ParentID = GetSetting("parent");
            this.CheckID = GetSetting("checkID");
            this.WhileTasks = GetSetting("whileTasks");            
        }

        public override TaskStatus Run()
        {
            return new TaskStatus(Status.Success);
        }
    }
}
