using System;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.IfCheck
{
    public class IfCheck : Task
    {
        public string ParentID { get; private set; }

        public string CheckID { get; private set; }

        public int[] TrueTasks { get; private set; }

        public int[] FalseTasks { get; private set; }

        public IfCheck(XElement xe, Workflow wf) : base(xe, wf)
        {            
            this.ParentID = GetSetting("parent");
            this.CheckID = GetSetting("checkID");
            this.TrueTasks = this.GetSettintTasksID("trueTasks");
            this.FalseTasks = this.GetSettintTasksID("falseTasks");
        }

        public override TaskStatus Run()
        {
            return new TaskStatus(Status.Success);
        }

        private int[] GetSettintTasksID(string name)
        {
            string setting = GetSetting(name);
            if (string.IsNullOrWhiteSpace(setting))
            {
                throw new Exception("Setting " + name + " value is empty.");
            }

            string[] stringArray = setting.Split(',');// check only a task id ex:1
            if (stringArray.Length == 0)
            {
                throw new Exception("Setting " + name + " value is empty.");
            }

            int[] intArray = Array.ConvertAll(stringArray, delegate (string input)
                {
                    int output;
                    return int.TryParse(input, out output) ? output : default(int);
                }
            );

            return intArray;
        }
    }
}
