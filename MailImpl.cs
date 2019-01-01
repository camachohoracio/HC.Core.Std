#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using HC.Core.Comunication.Web;
using HC.Core.Io;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues;

#endregion

namespace HC.Core
{
    public static class MailImpl
    {
        private static readonly ProducerConsumerQueue<LogQueueItem> m_emailExceptionQueue;
        private static readonly HashSet<string> m_emailValidator = new HashSet<string>();
        private static readonly List<string> m_defaultTo = 
            Config.GetEmailToList();

        static MailImpl()
        {
            try
            {
                m_emailExceptionQueue = new ProducerConsumerQueue<LogQueueItem>(1, 100, false, true);
                m_emailExceptionQueue.SetAutoDisposeTasks(true);
                m_emailExceptionQueue.DoNotLogExceptions = true;
                m_emailExceptionQueue.OnWork += logQueueItem =>
                {
                    try
                    {
                        string str = logQueueItem.Title + logQueueItem.Messge;
                        string strHash =
                            str.GetHashCode().ToString() +
                            str.Length;
                        if (m_emailValidator.Contains(strHash))
                        {
                            return;
                        }
                        m_emailValidator.Add(strHash);
                        SendMessage0(
                            logQueueItem.Title,
                            logQueueItem.Messge,
                            logQueueItem.Sender,
                            logQueueItem.IsHtml,
                            logQueueItem.ImageFileNames,
                            logQueueItem.To,
                            0);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex, false);
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void SendEmail(
            string strSubject,
            string strMessage,
            bool blnIsHtml = false,
            List<string> imageFileNames = null,
            bool blnWait = false,
            List<string> strTo = null)
        {
            SendEmail(
                strSubject, 
                strMessage, 
                string.Empty, 
                blnIsHtml,
                imageFileNames,
                blnWait,
                strTo);
        }

        public static void SendEmail(
            string strSubject,
            string strMessage,
            string strSender,
            bool blnIsHtml = false,
            List<string> imageFileNames = null,
            bool blnWait = false,
            List<string> strTo = null)
        {
            if(strTo == null ||
                strTo.Count == 0)
            {
                strTo = m_defaultTo.ToList();
            }

            var task  = m_emailExceptionQueue.EnqueueTask(
                new LogQueueItem
                    {
                        Title = strSubject,
                        Messge = strMessage,
                        Sender = strSender,
                        IsHtml = blnIsHtml,
                        ImageFileNames = imageFileNames,
                        To = strTo
                    });
            if(blnWait)
            {
                task.Wait();
            }
        }

        private static void SendMessage0(
            string strSubject, 
            string strMessage, 
            string strSender, 
            bool blnIsHtml, 
            List<string> imageFileNames,
            List<string> strTo,
            int intTrials)
        {
            try
            {
                if(!WebHelper.IsConnectedToInternet())
                {
                    return;
                }

                try
                {
                    strSubject = strSubject.Replace('\r', ' ').Replace('\n', ' ');
                    string strEmail = 
                        Config.GetEmailAddr();
                    string strEmailName =
                        Config.GetEmailName();
                    string strPassword = 
                        Config.GetEmailPsw();
                    string strHost =
                        Config.GetEmailHost();
                    int intPort =
                        Config.GetEmailPort();

                    var fromAddress = new MailAddress(
                        strEmail, 
                        string.IsNullOrEmpty(strSender) ?
                            strEmailName : 
                            strSender);

                    var smtp = new SmtpClient
                    {
                        Host = strHost,
                        Port = intPort,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(
                                        fromAddress.Address, 
                                        strPassword)
                    };

                    using (var message = new MailMessage()
                    {
                        From = fromAddress,
                        Subject = strSubject,
                        IsBodyHtml = blnIsHtml
                    })
                    {
                        foreach (var strRecipient in strTo)
                        {
                            message.To.Add(strRecipient);
                        }

                        if (imageFileNames != null &&
                            imageFileNames.Count > 0)
                        {
                            strMessage = CheckImmageAttatchments(
                                strMessage, 
                                imageFileNames, 
                                message);
                        }
                        message.Body = strMessage;
                        smtp.Send(message);
                    }
                }
                catch (Exception ex)
                {
                    if (intTrials > 3)
                    {
                        return;
                    }
                    Console.WriteLine("Email failed. Trying again [" +
                        intTrials + @"]/[3][" +
                        strSubject + "]");
                    Thread.Sleep(5000);
                    SendMessage0(
                        strSubject,
                        strMessage,
                        strSender,
                        blnIsHtml,
                        imageFileNames,
                        strTo,
                        ++intTrials);
                    Logger.Log(ex, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

        }

        private static string CheckImmageAttatchments(
            string strMessage, 
            List<string> imageFileNames, 
            MailMessage message)
        {
            try
            {
                foreach (string strImageFileName in imageFileNames)
                {
                    if (!FileHelper.Exists(strImageFileName))
                    {
                        continue;
                    }
                    var attachment = new Attachment(
                        strImageFileName);
                    string stContentId = new FileInfo(strImageFileName).Name
                        .Replace("^", string.Empty);
                    attachment.ContentId = stContentId;
                    attachment.ContentDisposition.Inline = true;
                    attachment.ContentDisposition.DispositionType =
                        DispositionTypeNames.Inline;
                    message.Attachments.Add(attachment);
                    strMessage +=
                        HtmlParser.ConvertToHtmlString(
                            Environment.NewLine) +  
                        "<img src=\"cid:" + stContentId + "\">";
                }
                return strMessage;
            }
            catch(Exception ex)
            {
                Logger.Log(ex, false);
            }
            return null;
        }
    }
}



