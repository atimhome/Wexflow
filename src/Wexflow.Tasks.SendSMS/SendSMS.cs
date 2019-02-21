using Newtonsoft.Json;
using System.Threading;
using System.Xml.Linq;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Wexflow.Core;

namespace Wexflow.Tasks.SendSMS
{
    public class SendSMS : Task
    {
        private string _AccountSid { get; set; }

        private string _AuthToken { get; set; }

        public string fromPhoneNumber { get; private set; }

        public string bodyContent { get; private set; }

        public SendSMS(XElement xe, Workflow wf) : base(xe, wf)
        {
            this._AccountSid = GetSetting("accountSid");
            this._AuthToken = GetSetting("authToken");
            this.fromPhoneNumber = GetSetting("from");
            this.bodyContent = GetSetting("body");
        }

        public override TaskStatus Run()
        {
            try
            {
                TwilioClient.Init(this._AccountSid, this._AuthToken);

                string toPhoneNumber = this.Hashtable["to"].ToString();

                var message = MessageResource.Create(
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(this.fromPhoneNumber),
                    body: this.bodyContent
                );

#if DEBUG
                Info("The MessageResource is :\r\n" + JsonConvert.SerializeObject(message));
#endif
                return new TaskStatus(Status.Success);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
        }
    }
}
