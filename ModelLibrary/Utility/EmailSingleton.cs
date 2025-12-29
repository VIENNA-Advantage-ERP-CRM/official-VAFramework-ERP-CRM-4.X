using System;
using System.Data;
using System.IO;
using System.Threading;
using VAdvantage.Common;
using VAdvantage.DataBase;
using VAdvantage.Logging;
using VAdvantage.Model;
using VAdvantage.Utility;

namespace ModelLibrary.Utility
{
    // Sealed restricts the inheritence
    public sealed class EmailSingleton
    {
        // Private variable which is readonly to implement thread safety and avoid race condition
        private static readonly Lazy<EmailSingleton> lazy = new Lazy<EmailSingleton>(() => new EmailSingleton());

        // Thread to run function 
        private static Thread thread = null;

        // Public property is used to return only one instance of the class leveraging on the private property
        public static EmailSingleton Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        // Private constructor ensures that object is not instantiated other than with in the class itself
        private EmailSingleton()
        { }

        // Public method which can be invoked through the singleton class
        public void StartEmailing()
        {
            if (thread == null || !thread.IsAlive)
            {
                thread = new Thread(Email);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        // Private method called from with the class
        private static void Email()
        {
            // Run this itration infinetly
            try
            {
                while (true)
                {
                    // Get records from mail queue table to send them one by one as email
                    DataSet mailds = DB.ExecuteDataset("SELECT AD_Org_ID, AD_Client_ID, CreatedBy, AD_Role_ID, ToEMail, ToName, MailSubject, MailMessage, IsHtmlEmail, AD_Table_ID, Record_ID, AD_WF_Activity_ID, AD_WF_EventAudit_ID, AD_MailQueue_ID, AD_WF_Process_ID FROM AD_MailQueue WHERE MailStatus = 'Q' AND ROWNUM <= 5 ORDER BY AD_MailQueue_ID");

                    if (mailds != null && mailds.Tables.Count > 0 && mailds.Tables[0].Rows.Count > 0)
                    {
                        for (int m = 0; m < mailds.Tables[0].Rows.Count; m++)
                        {
                            int AD_Org_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_Org_ID"]);
                            int AD_Client_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_Client_ID"]);
                            int AD_User_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["CreatedBy"]);
                            int AD_Role_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_Role_ID"]);

                            String toEMail = Util.GetValueOfString(mailds.Tables[0].Rows[m]["ToEMail"]);
                            String toName = Util.GetValueOfString(mailds.Tables[0].Rows[m]["ToName"]);
                            String subject = Util.GetValueOfString(mailds.Tables[0].Rows[m]["MailSubject"]);
                            String message = Util.GetValueOfString(mailds.Tables[0].Rows[m]["MailMessage"]);
                            FileInfo attachment = null;
                            bool isHtml = Util.GetValueOfString(mailds.Tables[0].Rows[m]["IsHtmlEmail"]) == "Y" ? true : false;
                            int AD_Table_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_Table_ID"]);
                            int Record_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["Record_ID"]);
                            byte[] array = null;
                            String fileName = null;
                            int AD_WF_Activity_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_WF_Activity_ID"]);
                            int AD_WF_EventAudit_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_WF_EventAudit_ID"]);
                            int AD_WF_Process_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_WF_Process_ID"]);
                            int AD_MailQueue_ID = Util.GetValueOfInt(mailds.Tables[0].Rows[m]["AD_MailQueue_ID"]);


                            // Create context
                            Ctx _ctx = new Ctx();
                            _ctx.SetAD_Org_ID(AD_Org_ID);
                            _ctx.SetAD_Client_ID(AD_Client_ID);
                            _ctx.SetAD_User_ID(AD_User_ID);
                            _ctx.SetAD_Role_ID(AD_Role_ID);


                            string sql = "SELECT AD_WF_NODE_ID FROM AD_WF_Activity WHERE AD_WF_Activity_ID=" + AD_WF_Activity_ID;
                            int nodeID = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                            X_AD_WF_Node node = null;

                            // Check  node found from wf activity table for current email thread, if yes then get node data
                            if (nodeID > 0)
                            {
                                node = new X_AD_WF_Node(_ctx, Convert.ToInt32(nodeID), null);
                            }

                            ViennaAdvantage.Model.X_AD_MailQueue mailQueue = new ViennaAdvantage.Model.X_AD_MailQueue(_ctx, AD_MailQueue_ID, null);

                            // Check if activity node is not null for current email thread
                            if (node != null)
                            {
                                FileInfo pdf = null;
                                MTable table = MTable.Get(_ctx, AD_Table_ID);
                                PO po = table.GetPO(_ctx, Record_ID, null);
                                if (node.IsAttachReport())
                                {
                                    VAdvantage.Common.Common com = new Common();
                                    pdf = com.GetPdfReportForMail(_ctx, null, AD_Table_ID, Record_ID, AD_User_ID, AD_Client_ID,
                                       "", 0, AD_WF_Activity_ID);
                                }
                                else if (po is VAdvantage.Process.DocAction)
                                {
                                    VAdvantage.Process.DocAction doc = (VAdvantage.Process.DocAction)po;
                                    attachment = doc.CreatePDF();
                                }
                                // Check if there is m class for the record and table and fetch attachment from that class 
                                //MTable table = MTable.Get(_ctx, AD_Table_ID);
                                //PO po = table.GetPO(_ctx, Record_ID, null);

                                //if (po is VAdvantage.Process.DocAction) // MClass Implement DocAction
                                //{
                                //VAdvantage.Process.DocAction doc = (VAdvantage.Process.DocAction)po;
                                //attachment = doc.CreatePDF();
                                //VAdvantage.Common.Common com = new Common();
                                //FileInfo pdf = com.GetPdfReportForMail(_ctx, null, AD_Table_ID, Record_ID, AD_User_ID, AD_Client_ID,
                                //   "", 0, AD_WF_Activity_ID);
                                if (pdf != null)
                                {
                                    array = File.ReadAllBytes(pdf.FullName);
                                    fileName = pdf.Name;
                                }
                                //}

                                EMail objMail = new EMail(_ctx, "", "", "", "", "", "", true, false);

                                if (EMail.SENT_OK.Equals(objMail.IsConfigurationExist(_ctx)))
                                {
                                    // Create Mclient object and send Email 
                                    MClient client = MClient.Get(_ctx, AD_Client_ID);
                                    bool mailsent = client.SendEMail(toEMail, toName, subject, message, attachment, isHtml, AD_Table_ID, Record_ID, array, fileName);

                                    if (mailsent)
                                    {
                                        mailQueue.SetMailStatus("S");
                                        mailQueue.Set_ValueNoCheck("AD_MailStatusDesc", "Email sent. ToEmail=" + toEMail);
                                        int act1 = DB.ExecuteQuery("UPDATE AD_WF_Activity SET WFSTATE = 'CC' WHERE AD_WF_Activity_ID = " + AD_WF_Activity_ID);
                                        int aud1 = DB.ExecuteQuery("UPDATE AD_WF_EventAudit SET WFSTATE = 'CC' WHERE AD_WF_EventAudit_ID = " + AD_WF_EventAudit_ID);
                                        int wpro = DB.ExecuteQuery("UPDATE AD_WF_Process SET WFSTATE = 'CC' WHERE AD_WF_Process_ID = " + AD_WF_Process_ID + " AND WFState = 'BK' ");
                                    }
                                    else
                                    {
                                        mailQueue.SetMailStatus("F");
                                        mailQueue.Set_ValueNoCheck("AD_MailStatusDesc", "Email pushed to mail server email but not delivered to recipient by mail server, please check your email configuration or log of mail server. ToEmail=" + toEMail + ", AD_MailQueue_ID=" + AD_MailQueue_ID + ", AD_WF_Activity_ID=" + AD_WF_Activity_ID);
                                        VLogger.Get().Warning("Email pushed to mail server email but not delivered to recipient by mail server, please check your email configuration or log of mail server.");
                                    }
                                }
                                else
                                {
                                    mailQueue.SetMailStatus("F");
                                    mailQueue.Set_ValueNoCheck("AD_MailStatusDesc", "Email configuration not found. ToEmail=" + toEMail + ", AD_MailQueue_ID=" + AD_MailQueue_ID + ", AD_WF_Activity_ID=" + AD_WF_Activity_ID);
                                    VLogger.Get().Warning("Email configuration not found.");
                                }

                                bool mq = mailQueue.Save();
                            }
                            else
                            {
                                // Mark mail record as failed if node/activity not found 
                                mailQueue.SetMailStatus("F");
                                mailQueue.Set_ValueNoCheck("AD_MailStatusDesc", "Activity node not found for email, marking as failed. ToEmail=" + toEMail + ", AD_MailQueue_ID=" + AD_MailQueue_ID + ", AD_WF_Activity_ID=" + AD_WF_Activity_ID);
                                mailQueue.Save();
                                VLogger.Get().Warning("Activity node not found for email, marking as failed");
                            }

                            // Sleep thread by 1 second to let memory work properly
                            Thread.Sleep(1000);

                            // For testing thread abort problem 
                            //thread.Abort();
                        }

                        int deleteoldmails = DB.ExecuteQuery("DELETE FROM AD_MailQueue WHERE (MailStatus = 'S') AND Created <=" + GlobalVariable.TO_DATE(DateTime.Now, false) + " - 7");
                    }
                    else
                    {
                        // Sleep thread by 5 minutes if there is no record in mail queue table
                        Thread.Sleep(60000 * 5);
                    }
                }
            }
            catch (Exception e)
            {
                VLogger.Get().Severe("Error in Email singleton class " + e.Message);
                //mailQueue.Set_ValueNoCheck("AD_MailStatusDesc", "Error in Email singleton class " + e.Message);
                try
                {
                    // Check if state is not running 
                    if (thread.ThreadState != ThreadState.Running)
                        Thread.ResetAbort();
                    else
                    {
                        thread.Abort();
                        Thread.ResetAbort();
                    }
                }
                catch (Exception ex)
                { }
                finally
                {
                    thread = null;
                }
            }
        }
    }
}