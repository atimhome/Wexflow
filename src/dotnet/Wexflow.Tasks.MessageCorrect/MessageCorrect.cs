using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.MessageCorrect
{
    public class MessageCorrect : Task
    {        
        public string CheckString { get; private set; }

        public MessageCorrect(XElement xe, Workflow wf) : base(xe, wf)
        {            
            this.CheckString = GetSetting("checkString");
        }

        public override TaskStatus Run()
        {
            try
            {
                string message = this.Hashtable["message"].ToString();
                bool result = message.IndexOf(this.CheckString) >= 0;

#if DEBUG
                Info("The result is " + result);
#endif

                return new TaskStatus(result ? Status.Success : Status.Error, result, message);
            }
            catch (ThreadAbortException)
            {
                throw;
            }            
        }
    }
}
