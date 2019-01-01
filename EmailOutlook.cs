using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Net.Mime;
using System.Threading;
using NUnit.Framework;
using Attachment = System.Net.Mail.Attachment;
using Exception = System.Exception;
using HC.Core.Logging;

namespace HC.Core
{
    public static class EmailOutlook
    {
        private static readonly object m_emailLock = new object();

        public static void SendEMail(
            string strTitle,
            string strMessage,
            List<string> recipients,
            bool blnIsHtml)
        {
            SendEMail(
                        strTitle,
                        strMessage,
                        recipients,
                        blnIsHtml,
                        string.Empty);
        }

        public static void SendEMail(
            string strTitle,
            string strMessage,
            List<string> recipients,
            bool blnIsHtml,
            string strAttatchments)
        {
            SendEMail(
                strTitle,
                strMessage,
                recipients,
                blnIsHtml,
                new List<string>
                    {
                        strAttatchments
                    });
        }

        public static void SendEMail(
            string strTitle,
            string strMessage,
            List<string> recipients,
            bool blnIsHtml,
            List<string> attatchments)
        {
            try
            {
                lock (m_emailLock)
                {
                    using (var smtp = new SmtpClient
                    {
                        Host = Config.GetEmailHost(),
                        Port = Config.GetEmailPort(),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(
                            Config.GetEmailAddr(),
                            Config.GetEmailPsw())
                    })
                    {
                        using (var oMsg = new MailMessage
                        {
                            IsBodyHtml = blnIsHtml,
                            Body = strMessage,
                            Subject = strTitle,
                            From = new MailAddress(
                                                      Config.GetEmailAddr(),
                                                      Config.GetEmailName())
                        })
                        {
                            foreach (var strRecipient in recipients)
                            {
                                oMsg.To.Add(strRecipient);
                            }
                            CheckImmageAttatchments(
                                strMessage,
                                attatchments,
                                oMsg);
                            smtp.Send(oMsg);
                            Thread.Sleep(1000);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log(ex, false);
            }
        }

        public static void SendMessage(
            string strTitle,
            string strMessage)
        {
            SendEMail(
                strTitle,
                strMessage,
                Config.GetEmailToList(),
                    false);
        }

        private static void CheckImmageAttatchments(
            string strMessage,
            List<string> imageFileNames,
            MailMessage message)
        {
            try
            {
                if (imageFileNames == null ||
                    imageFileNames.Count == 0)
                {
                    return;
                }

                foreach (string strImageFileName in imageFileNames)
                {
                    if (!File.Exists(strImageFileName))
                    {
                        continue;
                    }
                    var attachment = new Attachment(
                        strImageFileName);
                    message.Attachments.Add(attachment);

                    string strContentId = new FileInfo(strImageFileName).Name
                        .Replace("^", string.Empty);

                    if (strMessage.Contains( /// check if this is an image inline with message
                        strContentId))
                    {
                        attachment.ContentId = strContentId;
                        attachment.ContentDisposition.Inline = true;
                        attachment.ContentDisposition.DispositionType =
                            DispositionTypeNames.Inline;
                    }
                }
                message.Body = strMessage;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, false);
            }
        }

    }
}
