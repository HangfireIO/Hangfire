using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Notification;

namespace Hangfire.EmailNotification
{
    public class SmtpClientNotifier : INotifier
    {
        private readonly SmtpClient _smtpClient;
        private readonly Dictionary<EventTypes, List<string>> _eventReceivers;
        private readonly string _fromEmail;
        private readonly string _fromName;
        
        public SmtpClientNotifier(string fromEmail, string fromName, SmtpConfiguration sendgridConfig)
        {
            _fromEmail = fromEmail;
            _fromName = fromName;
            _smtpClient = new SmtpClient(sendgridConfig.Host, sendgridConfig.Port);
            _eventReceivers = new Dictionary<EventTypes, List<string>>();
            _smtpClient.Credentials = new System.Net.NetworkCredential(sendgridConfig.Username, sendgridConfig.Password);
        }

        public void Notify(EventTypes eventType, string subject, string message)
        {
            var mailMsg = new MailMessage();

            List<string> emails;
            var success = _eventReceivers.TryGetValue(eventType, out emails);

            if (success && emails.Count > 0)
            {
                foreach (var email in emails)
                {
                    mailMsg.To.Add(email);
                }

                mailMsg.From = new MailAddress(_fromEmail, _fromName);
                mailMsg.Subject = subject;
                mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message, null, MediaTypeNames.Text.Plain));

                _smtpClient.Send(mailMsg);
            }
        }

        public void Subscribe(EventTypes eventType, List<string> toEmails)
        {
            if (toEmails == null) throw new NullReferenceException();

            _eventReceivers.Add(eventType, toEmails);
        }
    }
}
