﻿using System;
using System.Collections.Generic;
using System.Linq;
using VAdvantage.DataBase;
using VAdvantage.Model;
using VAdvantage.Utility;
using System.Data;
using VIS.Models;
using System.Text;
using VIS.DataContracts;

using System.Globalization;
using System.IO;
using System.Web.Hosting;
using VAdvantage.Classes;
namespace VIS.Helpers
{

    public class HomeHelper
    {
        DataSet dsData = new DataSet();
        string strQuery = "";

        //To get the All Alert Count
        public HomeModels getHomeAlrtCount(Ctx ctx)
        {
            HomeModels objHome = new HomeModels();
            try
            {
                #region Request Count
                //To Get Request count
                strQuery = " SELECT  count(VAR_Request.VAR_Request_ID) FROM VAR_Request  inner join  VAR_Req_Type rt on VAR_Request.VAR_Req_Type_id=rt.VAR_Req_Type_ID";
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "VAR_Request", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
                strQuery += " AND ( VAR_Request.SalesRep_ID =" + ctx.GetVAF_UserContact_ID() + " OR VAR_Request.VAF_Role_ID =" + ctx.GetVAF_Role_ID() + ")"
                 + " AND VAR_Request.Processed ='N'"
                + " AND (VAR_Request.VAR_Req_Status_ID IS NULL OR VAR_Request.VAR_Req_Status_ID IN (SELECT VAR_Req_Status_ID FROM VAR_Req_Status WHERE IsClosed='N'))";
                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nRequest = 0;
                if (dsData != null)
                {
                    nRequest = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }

                #endregion

                # region Notice Count
                //To get Notice Count
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL("SELECT count(VAF_Notice_ID) FROM VAF_Notice "
                    , "VAF_Notice", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
                strQuery += " AND VAF_UserContact_ID IN (" + ctx.GetVAF_UserContact_ID() + ")"
                  + " AND Processed='N'";
                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nNotice = 0;
                if (dsData != null)
                {
                    nNotice = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }
                #endregion

                #region WorkFlow Count
                //To Get Work flow Count
                //strQuery = "select count(a.VAF_WFlow_Task_ID) FROM VAF_WFlow_Task a Left Outer JOin VAF_WFlow_Node node ON a.VAF_WFlow_Node_id=node.VAF_WFlow_Node_id  "
                //     + "WHERE a.Processed='N' AND a.WFState='OS' AND a.VAF_Client_ID=" + ctx.GetVAF_Client_ID() + " AND ("
                //    // Owner of Activity
                //      + " a.VAF_UserContact_ID=" + ctx.GetVAF_UserContact_ID() // #1
                //    // Invoker (if no invoker = all)
                //      + " OR EXISTS (SELECT * FROM VAF_WFlow_Incharge r WHERE a.VAF_WFlow_Incharge_ID=r.VAF_WFlow_Incharge_ID"
                //      + " AND COALESCE(r.VAF_UserContact_ID,0)=0 AND (a.VAF_UserContact_ID=" + ctx.GetVAF_UserContact_ID() + " OR a.VAF_UserContact_ID IS NULL))" // #2
                //    // Responsible User
                //      + " OR EXISTS (SELECT * FROM VAF_WFlow_Incharge r WHERE a.VAF_WFlow_Incharge_ID=r.VAF_WFlow_Incharge_ID"
                //      + " AND r.VAF_UserContact_ID=" + ctx.GetVAF_UserContact_ID() + ")"  // #3
                //    // Responsible Role
                //      + " OR EXISTS (SELECT * FROM VAF_WFlow_Incharge r INNER JOIN VAF_UserContact_Roles ur ON (r.VAF_Role_ID=ur.VAF_Role_ID)"
                //      + " WHERE a.VAF_WFlow_Incharge_ID=r.VAF_WFlow_Incharge_ID AND ur.VAF_UserContact_ID=" + ctx.GetVAF_UserContact_ID() + " and a.VAF_Client_ID=" + ctx.GetVAF_Client_ID() + " and a.VAF_Org_ID=" + ctx.GetVAF_Org_ID() + ")" // #4
                //    //
                //      + ") ORDER BY a.Priority DESC, a.Created";

                strQuery = @"SELECT COUNT(*)
                            FROM VAF_WFlow_Task a
                            WHERE a.Processed  ='N'
                            AND a.WFState      ='OS'
                            AND a.VAF_Client_ID =" + ctx.GetVAF_Client_ID() + @"
                            AND ( (a.VAF_UserContact_ID=" + ctx.GetVAF_UserContact_ID() + @"
                            OR a.VAF_UserContact_ID   IN
                              (SELECT VAF_UserContact_ID
                              FROM VAF_UserContact_Standby
                              WHERE IsActive   ='Y'
                              AND Substitute_ID=" + ctx.GetVAF_UserContact_ID() + @"
                              AND (validfrom  <=sysdate)
                              AND (sysdate    <=validto )
                              ))
                            OR EXISTS
                              (SELECT *
                              FROM VAF_WFlow_Incharge r
                              WHERE a.VAF_WFlow_Incharge_ID=r.VAF_WFlow_Incharge_ID
                              AND COALESCE(r.VAF_UserContact_ID,0)=0
                              AND (a.VAF_UserContact_ID           =" + ctx.GetVAF_UserContact_ID() + @"
                              OR a.VAF_UserContact_ID            IS NULL
                              OR a.VAF_UserContact_ID            IN
                                (SELECT VAF_UserContact_ID
                                FROM VAF_UserContact_Standby
                                WHERE IsActive   ='Y'
                                AND Substitute_ID=" + ctx.GetVAF_UserContact_ID() + @"
                                AND (validfrom  <=sysdate)
                                AND (sysdate    <=validto )
                                ))
                              )
                            OR EXISTS
                              (SELECT *
                              FROM VAF_WFlow_Incharge r
                              WHERE a.VAF_WFlow_Incharge_ID=r.VAF_WFlow_Incharge_ID
                              AND (r.VAF_UserContact_ID           =" + ctx.GetVAF_UserContact_ID() + @"
                              OR a.VAF_UserContact_ID            IN
                                (SELECT VAF_UserContact_ID
                                FROM VAF_UserContact_Standby
                                WHERE IsActive   ='Y'
                                AND Substitute_ID=" + ctx.GetVAF_UserContact_ID() + @"
                                AND (validfrom  <=sysdate)
                                AND (sysdate    <=validto )
                                ))
                              )
                            OR EXISTS
                              (SELECT *
                              FROM VAF_WFlow_Incharge r
                              INNER JOIN VAF_UserContact_Roles ur
                              ON (r.VAF_Role_ID            =ur.VAF_Role_ID)
                              WHERE a.VAF_WFlow_Incharge_ID=r.VAF_WFlow_Incharge_ID
                              AND (ur.VAF_UserContact_ID          =" + ctx.GetVAF_UserContact_ID() + @"
                              OR a.VAF_UserContact_ID            IN
                                (SELECT VAF_UserContact_ID
                                FROM VAF_UserContact_Standby
                                WHERE IsActive   ='Y'
                                AND Substitute_ID=" + ctx.GetVAF_UserContact_ID() + @"
                                AND (validfrom  <=sysdate)
                                AND (sysdate    <=validto )
                                ))
                              AND r.responsibletype !='H'
                              ) )
                           ";
                // Applied Role access on workflow Activities
                strQuery = MVAFRole.GetDefault(ctx).AddAccessSQL(strQuery, "a", true, true);
                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nWorkFlow = 0;
                if (dsData != null)
                {
                    nWorkFlow = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }
                #endregion

                #region FollowUps
                //To get the Folloups count
                StringBuilder SqlQuery = new StringBuilder();
                SqlQuery.Append("select COUNT(inn.ChatID) As Count");

                SqlQuery.Append(" from (select * from (select CH.VACM_Chat_ID as ChatID,  max(CE.VACM_ChatLine_ID)as EntryID")
                         .Append("  from VACM_ChatLine CE join VACM_Chat CH on CE.VACM_Chat_ID= CH.VACM_Chat_ID ")
                         .Append("  JOIN VACM_Subscribe CS  ON (CH.vaf_tableview_id= CS.vaf_tableview_id) AND (CH.record_id = CS.record_id)")
                         .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID() + " group by CH.VACM_Chat_ID order by entryID )inn1) inn ")
                         .Append("  JOIN VACM_ChatLine CH on inn.ChatID= ch.VACM_Chat_ID ")
                         .Append("  JOIN VACM_Chat CMH on (cmh.VACM_Chat_ID= inn.chatid)")
                         .Append("  JOIN VACM_Subscribe CS  ON (CMH.vaf_tableview_id= CS.vaf_tableview_id) AND (CMH.record_id = CS.record_id)")
                         .Append(" Join VAF_UserContact Au on au.VAF_UserContact_id= CH.createdBy")
                         .Append(" left outer JOIN VAF_Image AI on(ai.VAF_Image_id=au.VAF_Image_id)")
                         .Append("  join VAF_Screen AW on(cs.VAF_Screen_id= aw.VAF_Screen_id) left outer  JOIN VAF_Image adi on(adi.VAF_Image_id= aw.VAF_Image_id)  where cs.createdby=" + ctx.GetVAF_UserContact_ID())
                         .Append(" and ch.VACM_ChatLine_ID =(Select max(VACM_ChatLine_ID) from VACM_ChatLine where VACM_Chat_ID= ch.VACM_Chat_ID)")
                         .Append("  order by inn.EntryID desc,ch.VACM_ChatLine_ID asc");
                dsData = new DataSet();
                dsData = DB.ExecuteDataset(SqlQuery.ToString());
                int nTotalFollow = 0;
                if (dsData != null)
                {
                    nTotalFollow = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }

                #endregion

                #region Notes
                //To get The Notes count
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL("SELECT COUNT(wsp_note_id) As NCount FROM WSP_Note", "WSP_Note", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO) + " AND VAF_USERCONTACT_ID=" + ctx.GetVAF_UserContact_ID() + " Order BY Created DESC";
                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nNotes = 0;
                if (dsData != null)
                {
                    nNotes = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }


                #endregion

                #region Appointments Count

                strQuery = "SELECT COUNT( AppointmentsInfo.ID)  FROM (SELECT AppointmentsInfo.Appointmentsinfo_id AS ID,AppointmentsInfo.VAF_Client_ID,AppointmentsInfo.VAF_Org_ID"
                    //+ "  FROM AppointmentsInfo JOIN VAF_UserContact ON VAF_UserContact.VAF_UserContact_ID =AppointmentsInfo.CreatedBy WHERE AppointmentsInfo.IsRead='N' AND AppointmentsInfo.istask ='N' AND  AppointmentsInfo.CreatedBy  !=" + ctx.GetVAF_UserContact_ID() + " AND AppointmentsInfo.VAF_UserContact_ID  = " + ctx.GetVAF_UserContact_ID() + ""
                          + "  FROM AppointmentsInfo JOIN VAF_UserContact ON VAF_UserContact.VAF_UserContact_ID =AppointmentsInfo.CreatedBy WHERE AppointmentsInfo.IsRead='N' AND AppointmentsInfo.istask ='N'  AND AppointmentsInfo.VAF_UserContact_ID  = " + ctx.GetVAF_UserContact_ID() + ""
                         + "  UNION SELECT AppointmentsInfo.Appointmentsinfo_id AS ID, AppointmentsInfo.VAF_Client_ID,AppointmentsInfo.VAF_Org_ID FROM AppointmentsInfo"
                         + "  JOIN VAF_UserContact ON VAF_UserContact.VAF_UserContact_ID = AppointmentsInfo.CreatedBy  WHERE (AppointmentsInfo.IsRead ='Y'   AND AppointmentsInfo.VAF_UserContact_ID = " + ctx.GetVAF_UserContact_ID() + " )  AND AppointmentsInfo.istask ='N'  AND AppointmentsInfo.startDate BETWEEN to_date('";
                //DateTime.Now.ToShortDateString()
                strQuery += DateTime.Now.AddDays(-1).ToString("M/dd/yy");
                // Use current time
                strQuery += @"','mm/dd/yy') and to_date('";
                strQuery += DateTime.Now.AddDays(7).ToString("M/dd/yy");
                //DateTime.UtcNow.AddDays(1).ToShortDateString() 
                strQuery += " 23.59','mm/dd/yy HH24:MI') "
                          + "  OR  to_date('" + DateTime.Now.ToString("M/dd/yy") + "','mm/dd/yy')  BETWEEN  AppointmentsInfo.startDate  AND AppointmentsInfo.endDate  AND  AppointmentsInfo.CreatedBy  !=" + ctx.GetVAF_UserContact_ID() + " AND AppointmentsInfo.VAF_UserContact_ID  = " + ctx.GetVAF_UserContact_ID() + ") AppointmentsInfo";
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "AppointmentsInfo", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);

                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nTotalAppnt = 0;
                if (dsData != null)
                {
                    nTotalAppnt = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }

                //dsData = new DataSet();
                //dsData = DB.ExecuteDataset(strQuery);
                //int nAppt = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());

                #endregion

                #region Task Assign By me count

                strQuery = " SELECT COUNT(AppointmentsInfo.Appointmentsinfo_id)   FROM AppointmentsInfo ";
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "AppointmentsInfo", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
                strQuery += "  AND  AppointmentsInfo.IsRead='N' AND AppointmentsInfo.istask ='Y'  AND AppointmentsInfo.isClosed ='N'  AND  AppointmentsInfo.CreatedBy =" + ctx.GetVAF_UserContact_ID() + "  AND  AppointmentsInfo.VAF_UserContact_ID !=" + ctx.GetVAF_UserContact_ID() + "";

                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nTaskAssignByMe = 0;
                if (dsData != null)
                {
                    nTaskAssignByMe = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }

                #endregion

                #region My task count


                strQuery = " SELECT COUNT(AppointmentsInfo.Appointmentsinfo_id)   FROM AppointmentsInfo ";
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "AppointmentsInfo", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
                strQuery += "  AND   AppointmentsInfo.IsRead='N' AND AppointmentsInfo.istask ='Y' AND AppointmentsInfo.isClosed ='N'  AND AppointmentsInfo.VAF_UserContact_ID =" + ctx.GetVAF_UserContact_ID() + " ";

                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                int nMyTask = 0;
                if (dsData != null)
                {
                    nMyTask = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
                }



                #endregion

                #region To do Count

                int nTodo = 0;
                #endregion

                #region Incommin req Count
                int nIncmngReq = 0;
                #endregion

                #region KPI COUNT
                int userid = ctx.GetVAF_UserContact_ID();
                int roleid = ctx.GetVAF_Role_ID();

                string sql = @"SELECT Distinct  kpi.VARC_KPI_ID
                                        FROM VARC_KPI kpi
                                        
                                        WHERE kpi.IsActive      = 'Y'
                                        AND kpi.KPITYPE         ='Te'

                                        AND kpi.VARC_KPI_ID  IN
                                          (SELECT record_ID
                                          FROM VAF_UserHomePageSetting
                                          WHERE ISActive ='Y'
                                          AND VAF_TableView_ID=
                                            (SELECT VAF_TableView_ID FROM VAF_TableView WHERE TableName='VARC_KPI'
                                            )
                                          AND VAF_UserContact_ID=" + userid + ") ";
                //sql += " AND ( (acc.VAF_UserContact_ID = " + userid + @")   OR ( acc.VAF_Role_ID = " + roleid + @") ) ";



                dsData = new DataSet();
                dsData = DB.ExecuteDataset(sql);
                int KPICount = 0;
                if (dsData != null)
                {
                    KPICount = dsData.Tables[0].Rows.Count;
                }

                #endregion

                objHome.RequestCnt = nRequest;
                objHome.NoticeCnt = nNotice;
                objHome.WorkFlowCnt = nWorkFlow;

                objHome.FollowUpCnt = nTotalFollow;
                objHome.AppointmentCnt = nTotalAppnt;
                objHome.ToDoCnt = nTodo;
                objHome.NotesCnt = nNotes;
                objHome.IncommingRequestCnt = nIncmngReq;
                objHome.TaskAssignByMeCnt = nTaskAssignByMe;
                objHome.MyTaskCnt = nMyTask;
                objHome.KPICnt = KPICount;
                #region Current User Info
                strQuery = "SELECT au.name,au.VAF_Image_ID,au.email,ai.binarydata,ai.imageurl,au.comments FROM VAF_UserContact au LEFT OUTER JOIN VAF_Image ai  ON (ai.VAF_Image_id= au.VAF_Image_id) WHERE VAF_UserContact_id=" + ctx.GetVAF_UserContact_ID();
                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                if (dsData != null)
                {
                    objHome.UsrName = dsData.Tables[0].Rows[0]["name"].ToString();
                    objHome.UsrEmail = dsData.Tables[0].Rows[0]["email"].ToString();
                    if (Util.GetValueOfString(dsData.Tables[0].Rows[0]["comments"]).Length > 0)
                        objHome.UsrStatus = dsData.Tables[0].Rows[0]["comments"].ToString();
                    if (dsData.Tables[0].Rows[0]["binarydata"].ToString() != "")
                    {
                        objHome.UsrImage = Convert.ToBase64String((byte[])dsData.Tables[0].Rows[0]["binarydata"]);
                    }
                    else
                    {
                        //***** Added By Sarab

                        //***** If User Image saved in FileSystem
                        try
                        {
                            MVAFImage objImage = new MVAFImage(ctx, Util.GetValueOfInt(dsData.Tables[0].Rows[0]["VAF_Image_ID"]), null);
                            objHome.UsrImage = Convert.ToBase64String(objImage.GetThumbnailByte(140, 120));
                        }
                        catch
                        {
                        }
                        //FileStream stream = null;
                        //try
                        //{
                        //    string filepath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, dsData.Tables[0].Rows[0]["imageurl"].ToString());
                        //    if(File.Exists(filepath))
                        //    {
                        //    stream = File.OpenRead(filepath);
                        //    byte[] fileBytes = new byte[stream.Length];
                        //    stream.Read(fileBytes, 0, fileBytes.Length);
                        //    objHome.UsrImage = Convert.ToBase64String(fileBytes);
                        //    stream.Close();
                        //    }
                        //}
                        //catch
                        //{
                        //}
                        //finally
                        //{
                        //    if(stream!=null)
                        //    stream.Close();
                        //}

                    }
                    //ShowGreeting(objHome, ctx);
                }

                #endregion

                return objHome;
            }
            catch (Exception)
            {

            }
            return objHome;
        }

        #region get Login User Info
        //Get login user info
        public HomeModels getLoginUserInfo(Ctx ctx, int height, int width)
        {
            HomeModels objHome = null;
            string strUserQuery = "SELECT au.name,au.VAF_Image_ID,au.email,ai.binarydata,ai.imageurl FROM VAF_UserContact au LEFT OUTER JOIN VAF_Image ai  ON (ai.VAF_Image_id= au.VAF_Image_id) WHERE VAF_UserContact_id=" + ctx.GetVAF_UserContact_ID();
            dsData = new DataSet();
            dsData = DB.ExecuteDataset(strUserQuery);
            objHome = new HomeModels();
            if (dsData != null)
            {
                objHome.UsrName = dsData.Tables[0].Rows[0]["name"].ToString();
                objHome.UsrEmail = dsData.Tables[0].Rows[0]["email"].ToString();
                var uimgId = Util.GetValueOfInt(dsData.Tables[0].Rows[0]["VAF_Image_ID"].ToString());

                MVAFImage mimg = new MVAFImage(ctx, uimgId, null);
                var imgfll = mimg.GetThumbnailURL(height, width);
                objHome.UsrImage = imgfll;

                //if (dsData.Tables[0].Rows[0]["binarydata"].ToString() != "")
                //{
                //    objHome.UsrImage = Convert.ToBase64String((byte[])dsData.Tables[0].Rows[0]["binarydata"]);
                //}
                //else
                //{
                //    try
                //    {
                //        MImage objImage = new MImage(ctx, Util.GetValueOfInt(dsData.Tables[0].Rows[0]["VAF_Image_ID"]), null);

                //        objHome.UsrImage = Convert.ToBase64String(objImage.GetThumbnailByte(height, width));
                //    }
                //    catch
                //    {
                //    }
                //}
            }
            return objHome;
        }

        #endregion

        #region Show Greeting
        /// <summary>
        /// Show Greeting according to time
        /// </summary>
        /// <param name="objHome"></param>
        /// <param name="ctx"></param>
        //private void ShowGreeting(HomeModels objHome, Ctx ctx)  //Added By Sarab
        //{
        //    if (DateTime.Now.Hour < 12)
        //    {
        //        objHome.Greeting = Msg.GetMsg(ctx, "GoodMorning");
        //    }
        //    else if (DateTime.Now.Hour < 17)
        //    {
        //        objHome.Greeting = Msg.GetMsg(ctx, "GoodAfternoon");
        //    }
        //    else
        //    {
        //        objHome.Greeting = Msg.GetMsg(ctx, "GoodEvening");

        //    }
        //}
        #endregion

        # region Follups start

        public int getFllCnt(Ctx ctx)
        {
            //To get the Folloups count
            StringBuilder SqlQuery = new StringBuilder();
            SqlQuery.Append("select COUNT(inn.ChatID) As Count");

            SqlQuery.Append(" from (select * from (select CH.VACM_Chat_ID as ChatID,  max(CE.VACM_ChatLine_ID)as EntryID")
                     .Append("  from VACM_ChatLine CE join VACM_Chat CH on CE.VACM_Chat_ID= CH.VACM_Chat_ID ")
                     .Append("  JOIN VACM_Subscribe CS  ON (CH.vaf_tableview_id= CS.vaf_tableview_id) AND (CH.record_id = CS.record_id)")
                     .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID() + " group by CH.VACM_Chat_ID order by entryID )inn1) inn ")
                     .Append("  JOIN VACM_ChatLine CH on inn.ChatID= ch.VACM_Chat_ID ")
                     .Append("  JOIN VACM_Chat CMH on (cmh.VACM_Chat_ID= inn.chatid)")
                     .Append("  JOIN VACM_Subscribe CS  ON (CMH.vaf_tableview_id= CS.vaf_tableview_id) AND (CMH.record_id = CS.record_id)")
                     .Append(" Join VAF_UserContact Au on au.VAF_UserContact_id= CH.createdBy")
                     .Append(" left outer JOIN VAF_Image AI on(ai.VAF_Image_id=au.VAF_Image_id)")
                     .Append("  join VAF_Screen AW on(cs.VAF_Screen_id= aw.VAF_Screen_id) left outer  JOIN VAF_Image adi on(adi.VAF_Image_id= aw.VAF_Image_id)  where cs.createdby=" + ctx.GetVAF_UserContact_ID())
                     .Append(" and ch.VACM_ChatLine_ID =(Select max(VACM_ChatLine_ID) from VACM_ChatLine where VACM_Chat_ID= ch.VACM_Chat_ID)")
                     .Append("  order by inn.EntryID desc,ch.VACM_ChatLine_ID asc");
            try
            {
                int nTotalFollow = Util.GetValueOfInt(DB.ExecuteScalar(SqlQuery.ToString()));
                return nTotalFollow;
            }
            catch
            {
                return 0;
            }


        }

        //Get Folloups
        public HomeFolloUpsInfo getFolloUps(Ctx ctx, int PageSize, int page)
        {
            List<HomeFolloUps> lstFollUps = new List<HomeFolloUps>();
            List<FllUsrImages> lstUImg = new List<FllUsrImages>();
            HomeFolloUpsInfo objFllupsInfo = new HomeFolloUpsInfo();
            //List<HomeFolloUpsInfo> objFllups = new List<HomeFolloUpsInfo>();
            try
            {
                // To get the Folloups details
                StringBuilder SqlQuery = new StringBuilder();
                SqlQuery.Append("select inn.ChatID, inn.EntryID,  CH.characterdata, ch.VACM_ChatLine_ID,");
                if (ctx.GetVAF_Language() != Env.GetBaseVAF_Language())
                {
                    SqlQuery.Append("(Select name from VAF_Screen_TL where VAF_Screen_ID= Aw.VAF_Screen_ID and VAF_Language='" + ctx.GetVAF_Language() + "') as WINNAME,");
                }
                else
                {
                    SqlQuery.Append("aw.DisplayName as WINNAME,");
                }
                SqlQuery.Append("  At.TableName, aw.VAF_Screen_ID,cs.VAF_TableView_ID,cs.RECOrd_ID, aw.help,au.Name AS NAME,cs.VACM_Subscribe_ID, ch.created,ai.VAF_Image_id ,ai.binarydata as UsrImg,  adi.binarydata as WinImg,CH.createdby  from (select * from (select CH.VACM_Chat_ID as ChatID,  max(CE.VACM_ChatLine_ID)as EntryID")
                        .Append("  from VACM_ChatLine CE join VACM_Chat CH on CE.VACM_Chat_ID= CH.VACM_Chat_ID ")
                        .Append("  JOIN VACM_Subscribe CS  ON (CH.vaf_tableview_id= CS.vaf_tableview_id) AND (CH.record_id = CS.record_id)")
                        .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID() + " group by CH.VACM_Chat_ID order by entryID )inn1) inn ")
                        .Append("  JOIN VACM_ChatLine CH on inn.ChatID= ch.VACM_Chat_ID ")
                        .Append("  JOIN VACM_Chat CMH on (cmh.VACM_Chat_ID= inn.chatid)")
                        .Append("  JOIN VACM_Subscribe CS  ON (CMH.vaf_tableview_id= CS.vaf_tableview_id) AND (CMH.record_id = CS.record_id)")
                        .Append("  Join VAF_UserContact Au on au.VAF_UserContact_id= CH.createdBy")
                        .Append("  Join vaf_tableview At on at.vaf_tableview_id= CS.vaf_tableview_id")
                        .Append("  left outer JOIN VAF_Image AI on(ai.VAF_Image_id=au.VAF_Image_id)")
                        .Append("  join VAF_Screen AW on(cs.VAF_Screen_id= aw.VAF_Screen_id) left outer  JOIN VAF_Image adi on(adi.VAF_Image_id= aw.VAF_Image_id)")
                        .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID())
                        .Append("  and ch.VACM_ChatLine_ID =(Select max(VACM_ChatLine_ID) from VACM_ChatLine where VACM_Chat_ID= ch.VACM_Chat_ID)")
                        .Append("  order by inn.EntryID desc,ch.VACM_ChatLine_ID asc");

                SqlParamsIn objSP = new SqlParamsIn();
                dsData = new DataSet();
                objSP.page = page;
                objSP.pageSize = PageSize;
                objSP.sql = SqlQuery.ToString();
                dsData = VIS.DBase.DB.ExecuteDatasetPaging(objSP.sql, objSP.page, objSP.pageSize);
                if (dsData != null)
                {
                    for (int i = 0; i < dsData.Tables[0].Rows.Count; i++)
                    {
                        var Fllps = new HomeFolloUps();
                        Fllps.ChatID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["ChatID"].ToString());
                        Fllps.ChatEntryID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VACM_ChatLine_ID"].ToString());
                        Fllps.EntryID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["EntryID"].ToString());
                        Fllps.WinID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Screen_ID"].ToString());
                        Fllps.TableID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_TableView_ID"].ToString());
                        Fllps.RecordID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["Record_ID"].ToString());
                        Fllps.SubscriberID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VACM_Subscribe_ID"].ToString());
                        Fllps.ChatData = dsData.Tables[0].Rows[i]["characterdata"].ToString();
                        Fllps.TableName = dsData.Tables[0].Rows[i]["TableName"].ToString();
                        Fllps.Name = dsData.Tables[0].Rows[i]["NAME"].ToString();
                        Fllps.WinName = dsData.Tables[0].Rows[i]["WINNAME"].ToString();
                        Fllps.VAF_UserContact_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["CREATEDBY"].ToString());
                        DateTime _createdDate = new DateTime();
                        if (dsData.Tables[0].Rows[i]["created"].ToString() != null && dsData.Tables[0].Rows[i]["created"].ToString() != "")
                        {
                            _createdDate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["created"].ToString());
                            DateTime _format = DateTime.SpecifyKind(new DateTime(_createdDate.Year, _createdDate.Month, _createdDate.Day, _createdDate.Hour, _createdDate.Minute, _createdDate.Second), DateTimeKind.Utc);
                            _createdDate = _format;

                            Fllps.Cdate = _format;
                        }
                        int uimgId = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Image_id"].ToString());
                        Fllps.VAF_Image_ID = uimgId;
                        if (lstUImg.Where(a => a.VAF_Image_ID == uimgId).Count() == 0)
                        {
                            var uimsg = new FllUsrImages();
                            uimsg.VAF_Image_ID = uimgId;
                            MVAFImage mimg = new MVAFImage(ctx, uimgId, null);
                            var imgfll = mimg.GetThumbnailURL(46, 46);
                            uimsg.UserImg = imgfll;
                            lstUImg.Add(uimsg);

                            //var imgfll = "data:image/jpg;base64," + Convert.ToBase64String(mimg.GetThumbnailByte(46, 46));
                            //if (imgfll.ToString() == "FileDoesn'tExist" || imgfll.ToString() == "NoRecordFound")
                            //{
                            //}
                            //else
                            //{
                            //    uimsg.UserImg = imgfll;
                            //}
                            //lstUImg.Add(uimsg);
                            //if (dsData.Tables[0].Rows[i]["UsrImg"].ToString() != "")
                            //{
                            //    uimsg.UserImg = Convert.ToBase64String((byte[])dsData.Tables[0].Rows[i]["UsrImg"]);
                            //}   
                        }


                        /**************** IDENTIFIER **************/

                        string sql = "select ColumnName from VAF_Column where VAF_TableView_ID=" + Fllps.TableID + " and isidentifier='Y'  order by seqno";
                        DataSet ds = DB.ExecuteDataset(sql);
                        if (ds != null && ds.Tables[0].Rows.Count > 0)
                        {
                            string columns = "";
                            for (int l = 0; l < ds.Tables[0].Rows.Count; l++)
                            {
                                if (columns.Length > 0)
                                {
                                    columns += "|| '_' || " + ds.Tables[0].Rows[l]["ColumnName"].ToString();
                                }
                                else
                                {
                                    columns += ds.Tables[0].Rows[l]["ColumnName"].ToString();
                                }
                            }

                            sql = "SELECT distinct " + columns + " FROM " + Fllps.TableName + " WHERE " + Fllps.TableName + "_ID = " + Fllps.RecordID;
                            object identifier = DB.ExecuteScalar(sql);
                            if (identifier != null && identifier != DBNull.Value)
                            {
                                Fllps.Identifier = identifier.ToString();
                            }
                        }

                        /***************  END IDENTIFIER  *************/



                        lstFollUps.Add(Fllps);
                    }

                }
                objFllupsInfo.lstUserImg = lstUImg;
                objFllupsInfo.lstFollowups = lstFollUps;
            }
            catch (Exception)
            {

            }
            return objFllupsInfo;
        }

        //Get  lastest  Folloups comment
        public HomeFolloUpsInfo getLatestFolloUps(Ctx ctx, int ChatID)
        {
            List<HomeFolloUps> lstFollUps = new List<HomeFolloUps>();
            List<FllUsrImages> lstUImg = new List<FllUsrImages>();
            HomeFolloUpsInfo objFllupsInfo = new HomeFolloUpsInfo();
            try
            {
                // To get the Folloups details
                StringBuilder SqlQuery = new StringBuilder();
                SqlQuery.Append("select inn.ChatID, inn.EntryID,  CH.characterdata, ch.VACM_ChatLine_ID,");
                if (ctx.GetVAF_Language() != Env.GetBaseVAF_Language())
                {
                    SqlQuery.Append("(Select name from VAF_Screen_TL where VAF_Screen_ID= Aw.VAF_Screen_ID and VAF_Language='" + ctx.GetVAF_Language() + "') as WINNAME,");
                }
                else
                {
                    SqlQuery.Append("aw.DisplayName as WINNAME,");
                }
                SqlQuery.Append("  At.TableName, aw.VAF_Screen_ID,cs.VAF_TableView_ID,cs.RECOrd_ID, aw.help,au.Name AS NAME,cs.VACM_Subscribe_ID, ch.created,ai.VAF_Image_id ,ai.binarydata as UsrImg,  adi.binarydata as WinImg  from (select * from (select CH.VACM_Chat_ID as ChatID,  max(CE.VACM_ChatLine_ID)as EntryID")
                        .Append("  from VACM_ChatLine CE join VACM_Chat CH on CE.VACM_Chat_ID= CH.VACM_Chat_ID ")
                        .Append("  JOIN VACM_Subscribe CS  ON (CH.vaf_tableview_id= CS.vaf_tableview_id) AND (CH.record_id = CS.record_id)")
                        .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID() + " group by CH.VACM_Chat_ID order by entryID )inn1) inn ")
                        .Append("  JOIN VACM_ChatLine CH on inn.ChatID= ch.VACM_Chat_ID ")
                        .Append("  JOIN VACM_Chat CMH on (cmh.VACM_Chat_ID= inn.chatid)")
                        .Append("  JOIN VACM_Subscribe CS  ON (CMH.vaf_tableview_id= CS.vaf_tableview_id) AND (CMH.record_id = CS.record_id)")
                        .Append("  Join VAF_UserContact Au on au.VAF_UserContact_id= CH.createdBy")
                        .Append("  Join vaf_tableview At on at.vaf_tableview_id= CS.vaf_tableview_id")
                        .Append("  left outer JOIN VAF_Image AI on(ai.VAF_Image_id=au.VAF_Image_id)")
                        .Append("  join VAF_Screen AW on(cs.VAF_Screen_id= aw.VAF_Screen_id) left outer  JOIN VAF_Image adi on(adi.VAF_Image_id= aw.VAF_Image_id)")
                        .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID())
                        .Append("  and ch.VACM_ChatLine_ID =(Select max(VACM_ChatLine_ID) from VACM_ChatLine where VACM_Chat_ID= ch.VACM_Chat_ID)")
                        .Append("  order by inn.EntryID desc,ch.VACM_ChatLine_ID asc");

                SqlParamsIn objSP = new SqlParamsIn();
                dsData = new DataSet();

                dsData = DB.ExecuteDataset(objSP.sql);
                if (dsData != null)
                {
                    for (int i = 0; i < dsData.Tables[0].Rows.Count; i++)
                    {
                        var Fllps = new HomeFolloUps();
                        Fllps.ChatID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["ChatID"].ToString());
                        Fllps.ChatEntryID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VACM_ChatLine_ID"].ToString());
                        Fllps.EntryID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["EntryID"].ToString());
                        Fllps.WinID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Screen_ID"].ToString());
                        Fllps.TableID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_TableView_ID"].ToString());
                        Fllps.RecordID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["Record_ID"].ToString());
                        Fllps.SubscriberID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VACM_Subscribe_ID"].ToString());
                        Fllps.ChatData = dsData.Tables[0].Rows[i]["characterdata"].ToString();
                        Fllps.TableName = dsData.Tables[0].Rows[i]["TableName"].ToString();
                        Fllps.Name = dsData.Tables[0].Rows[i]["NAME"].ToString();
                        Fllps.WinName = dsData.Tables[0].Rows[i]["WINNAME"].ToString();
                        if (dsData.Tables[0].Rows[i]["created"].ToString() != "")
                        {
                            Fllps.Cdate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["created"]);
                        }
                        int uimgId = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Image_id"].ToString());
                        Fllps.VAF_Image_ID = uimgId;
                        if (lstUImg.Where(a => a.VAF_Image_ID == uimgId).Count() == 0)
                        {
                            var uimsg = new FllUsrImages();
                            uimsg.VAF_Image_ID = uimgId;
                            MVAFImage mimg = new MVAFImage(ctx, uimgId, null);
                            var imgfll = mimg.GetThumbnailURL(46, 46);
                            uimsg.UserImg = imgfll;
                            lstUImg.Add(uimsg);

                            //var imgfll = "data:image/jpg;base64," + Convert.ToBase64String(mimg.GetThumbnailByte(46, 46));
                            //if (imgfll.ToString() == "FileDoesn'tExist" || imgfll.ToString() == "NoRecordFound")
                            //{
                            //}
                            //else
                            //{
                            //    uimsg.UserImg = imgfll;
                            //}
                            //lstUImg.Add(uimsg);
                            //if (dsData.Tables[0].Rows[i]["UsrImg"].ToString() != "")
                            //{
                            //    uimsg.UserImg = Convert.ToBase64String((byte[])dsData.Tables[0].Rows[i]["UsrImg"]);
                            //}   
                        }

                        lstFollUps.Add(Fllps);
                    }
                    objFllupsInfo.lstUserImg = lstUImg;
                    objFllupsInfo.lstFollowups = lstFollUps;
                }
            }
            catch (Exception)
            {

            }
            return objFllupsInfo;
        }


        //Get Followups cmnt
        public HomeFolloUpsInfo getFolloUpsCmnt(Ctx ctx, int ChatID, int RecordID, int SubscriberID, int TableID, int WinID, int RoleID, int PageSize, int page)
        {
            List<HomeFolloUps> lstFollUps = new List<HomeFolloUps>();
            List<FllUsrImages> lstUImg = new List<FllUsrImages>();
            HomeFolloUpsInfo objFllupsInfo = new HomeFolloUpsInfo();
            //List<HomeFolloUpsInfo> objFllups = new List<HomeFolloUpsInfo>();
            try
            {
                // To get the Folloups details
                StringBuilder SqlQuery = new StringBuilder();
                SqlQuery.Append("select inn.ChatID, inn.EntryID,  CH.characterdata, ch.VACM_ChatLine_ID,");
                if (ctx.GetVAF_Language() != Env.GetBaseVAF_Language())
                {
                    SqlQuery.Append("(Select name from VAF_Screen_TL where VAF_Screen_ID= Aw.VAF_Screen_ID and VAF_Language='" + ctx.GetVAF_Language() + "') as WINNAME,");
                }
                else
                {
                    SqlQuery.Append("aw.DisplayName as WINNAME,");
                }

                SqlQuery.Append("  aw.VAF_Screen_ID,cs.VAF_TableView_ID,cs.RECOrd_ID, aw.help,au.Name AS NAME,cs.VACM_Subscribe_ID, ch.created,ai.VAF_Image_id, ai.binarydata as UsrImg,  adi.binarydata as WinImg ,CH.createdby from (select * from (select CH.VACM_Chat_ID as ChatID,  max(CE.VACM_ChatLine_ID)as EntryID")
                        .Append("  from VACM_ChatLine CE join VACM_Chat CH on CE.VACM_Chat_ID= CH.VACM_Chat_ID ")
                        .Append("  JOIN VACM_Subscribe CS  ON (CH.vaf_tableview_id= CS.vaf_tableview_id) AND (CH.record_id = CS.record_id)")
                        .Append("  where cs.createdby=" + ctx.GetVAF_UserContact_ID() + " group by CH.VACM_Chat_ID order by entryID )inn1 ) inn ")
                        .Append("  JOIN VACM_ChatLine CH on inn.ChatID= ch.VACM_Chat_ID ")
                        .Append("  JOIN VACM_Chat CMH on (cmh.VACM_Chat_ID= inn.chatid)")
                        .Append("  JOIN VACM_Subscribe CS  ON (CMH.vaf_tableview_id= CS.vaf_tableview_id) AND (CMH.record_id = CS.record_id)")
                        .Append("  Join VAF_UserContact Au on au.VAF_UserContact_id= CH.createdBy")
                        .Append("  left outer JOIN VAF_Image AI on(ai.VAF_Image_id=au.VAF_Image_id)")
                        .Append("  join VAF_Screen AW on(cs.VAF_Screen_id= aw.VAF_Screen_id) left outer  JOIN VAF_Image adi on(adi.VAF_Image_id= aw.VAF_Image_id)  where cs.createdby=" + ctx.GetVAF_UserContact_ID())
                        .Append("  AND cmh.VACM_Chat_ID=" + ChatID)
                        .Append("  order by inn.EntryID desc,ch.VACM_ChatLine_ID asc");

                dsData = new DataSet();
                dsData = DB.ExecuteDataset(SqlQuery.ToString());
                if (dsData != null)
                {
                    for (int i = 0; i < dsData.Tables[0].Rows.Count; i++)
                    {
                        var Fllps = new HomeFolloUps();
                        Fllps.ChatID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["ChatID"].ToString());
                        Fllps.ChatEntryID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VACM_ChatLine_ID"].ToString());
                        Fllps.EntryID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["EntryID"].ToString());
                        Fllps.WinID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Screen_ID"].ToString());
                        Fllps.TableID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_TableView_ID"].ToString());
                        Fllps.RecordID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["Record_ID"].ToString());
                        Fllps.SubscriberID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VACM_Subscribe_ID"].ToString());
                        Fllps.ChatData = dsData.Tables[0].Rows[i]["characterdata"].ToString();
                        Fllps.Name = dsData.Tables[0].Rows[i]["NAME"].ToString();
                        Fllps.WinName = dsData.Tables[0].Rows[i]["WINNAME"].ToString();
                        Fllps.VAF_UserContact_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["CREATEDBY"].ToString());

                        DateTime _createdDate = new DateTime();
                        if (dsData.Tables[0].Rows[i]["created"].ToString() != null && dsData.Tables[0].Rows[i]["created"].ToString() != "")
                        {
                            _createdDate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["created"].ToString());
                            DateTime _format = DateTime.SpecifyKind(new DateTime(_createdDate.Year, _createdDate.Month, _createdDate.Day, _createdDate.Hour, _createdDate.Minute, _createdDate.Second), DateTimeKind.Utc);
                            _createdDate = _format;

                            Fllps.Cdate = _format;
                        }

                        //if (dsData.Tables[0].Rows[i]["created"].ToString() != "")
                        //{
                        //    Fllps.Cdate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["created"]);
                        //}

                        //if (lstUImg.Where(a => a.VAF_Image_ID == uimgId).Count() == 0)
                        //{
                        //    var uimsg = new FllUsrImages();
                        //    uimsg.VAF_Image_ID = uimgId;
                        //    MImage mimg = new MImage(ctx, uimgId, null);
                        //    var imgfll = mimg.GetThumbnailURL(46, 46);
                        //    if (imgfll.ToString() == "FileDoesn'tExist" || imgfll.ToString() == "NoRecordFound")
                        //    {

                        //    }
                        //    else
                        //    {
                        //        uimsg.UserImg = imgfll;
                        //    }
                        //    lstUImg.Add(uimsg);
                        //}

                        int uimgId = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Image_id"].ToString());
                        Fllps.VAF_Image_ID = uimgId;

                        if (lstUImg.Where(a => a.VAF_Image_ID == uimgId).Count() == 0)
                        {
                            var uimsg = new FllUsrImages();
                            uimsg.VAF_Image_ID = uimgId;
                            MVAFImage mimg = new MVAFImage(ctx, uimgId, null);
                            var imgfll = mimg.GetThumbnailURL(46, 46);
                            uimsg.UserImg = imgfll;
                            lstUImg.Add(uimsg);
                        }

                        lstFollUps.Add(Fllps);
                    }
                    objFllupsInfo.lstUserImg = lstUImg;
                    objFllupsInfo.lstFollowups = lstFollUps;
                }
            }

            catch (Exception)
            {

            }
            return objFllupsInfo;
        }

        //Save Folloups Comment
        public void SaveFllupsCmnt(Ctx ctx, int ChatID, int SubscriberID, string txt)
        {
            MVACMChat _chat = new MVACMChat(ctx, ChatID, null);
            MVACMChatLine entry = new MVACMChatLine(_chat, txt);
            if (entry.Save())
            {
                //strQuery = "  UPDATE VACM_Subscribe SET isRead='N' WHERE isRead='Y' AND VACM_Subscribe_id=" + SubscriberID;
                //DB.ExecuteQuery(strQuery);
            }

        }

        #endregion

        # region Start Notice
        //Count of notice
        public int getNoticeCnt(Ctx ctx)
        {
            int ncnt = 0;
            try
            {
                //To get Notice Count
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL("SELECT count(VAF_Notice_ID) FROM VAF_Notice "
                    , "VAF_Notice", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
                strQuery += " AND VAF_UserContact_ID IN (" + ctx.GetVAF_UserContact_ID() + ")"
                  + " AND Processed='N'";

                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                ncnt = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
            }
            catch (Exception)
            {

            }
            return ncnt;
        }
        //List of Notice
        public List<HomeNotice> getHomeNotice(Ctx ctx, int PageSize, int page)
        {
            List<HomeNotice> lstNts = new List<HomeNotice>();
            try
            {
                //Notice
                //strQuery = "SELECT  substr(VAF_Notice.textmsg,0,100) as Title, VAF_Notice.textmsg as Description , VAF_Notice.Created  as dbDate,VAF_Notice.vaf_tableview_id"
                //+ ",VAF_TableView.tablename,VAF_Notice_id as ID   FROM VAF_Notice JOIN VAF_TableView on vaf_tableview.vaf_tableview_ID=VAF_Notice.vaf_tableview_ID ";

                //                strQuery = @"SELECT SUBSTR(VAF_Notice.textmsg,0,100) AS Title,
                //                            VAF_Notice.textmsg    AS Description ,
                //                            VAF_Notice.Created    AS dbDate,
                //                            VAF_Msg_Lable.msgtext as MsgType,
                //                            VAF_Notice.VAF_TableView_ID , 
                //                            VAF_Notice.Record_ID,
                //                            (SELECT  VAF_TableView.TableName FROM  VAF_TableView WHERE  VAF_TableView.TableName='VAF_Notice') TableName,
                //                            (SELECT  VAF_TableView.VAF_Screen_ID FROM  VAF_TableView WHERE  VAF_TableView.TableName='VAF_Notice') VAF_Screen_ID,
                //                            VAF_Notice.VAF_Notice_ID
                //                            FROM VAF_Notice INNER JOIN VAF_Msg_Lable ON VAF_Msg_Lable.VAF_Msg_Lable_ID=VAF_Notice.VAF_Msg_Lable_ID";
                strQuery = @"SELECT SUBSTR(VAF_Notice.textmsg,0,100) AS Title,
                              VAF_Notice.textmsg                    AS Description ,
                              VAF_Notice.Created                    AS dbDate,
                              VAF_Msg_Lable.msgtext                 AS MsgType,
                              VAF_Notice.VAF_TableView_ID ,
                              VAF_Notice.Record_ID,
                              (SELECT VAF_TableView.TableName FROM VAF_TableView WHERE VAF_TableView.TableName='VAF_Notice'
                              ) TableName,
                              (SELECT VAF_TableView.VAF_Screen_ID
                              FROM VAF_TableView
                              WHERE VAF_TableView.TableName='VAF_Notice'
                              ) VAF_Screen_ID,
                              VAF_Notice.VAF_Notice_ID
                            FROM VAF_Notice
                            INNER JOIN VAF_Msg_Lable
                            ON VAF_Msg_Lable.VAF_Msg_Lable_ID         =VAF_Notice.VAF_Msg_Lable_ID";
                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "VAF_Notice", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);

                strQuery += "  AND VAF_Notice.VAF_UserContact_ID IN (0," + ctx.GetVAF_UserContact_ID() + ")"
                + " AND VAF_Notice.Processed='N' ORDER BY VAF_Notice.Created DESC";

                int PResultTableID = MVAFTableView.Get_Table_ID("VAF_JInstance_Result");

                dsData = VIS.DBase.DB.ExecuteDatasetPaging(strQuery, page, PageSize);
                dsData = VAdvantage.DataBase.DB.SetUtcDateTime(dsData);

                object windowID = DB.ExecuteScalar("SELECT VAF_Screen_ID FROM VAF_Screen WHERE Name='Process Result'");

                if (dsData != null)
                {
                    for (int i = 0; i < dsData.Tables[0].Rows.Count; i++)
                    {
                        var Alrt = new HomeNotice();
                        Alrt.VAF_Notice_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Notice_ID"].ToString());
                        Alrt.VAF_TableView_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_TableView_ID"].ToString());
                        Alrt.VAF_Screen_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Screen_ID"].ToString());
                        Alrt.Record_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["Record_ID"].ToString());
                        Alrt.MsgType = dsData.Tables[0].Rows[i]["MsgType"].ToString();
                        Alrt.Title = dsData.Tables[0].Rows[i]["Title"].ToString();
                        Alrt.TableName = dsData.Tables[0].Rows[i]["TableName"].ToString();
                        if (PResultTableID == Alrt.VAF_TableView_ID)
                        {
                            Alrt.ProcessWindowID = Util.GetValueOfInt(windowID);
                            Alrt.ProcessTableName = "VAF_JInstance_Result";
                            Alrt.SpecialTable = true;
                        }
                        else
                        {
                            Alrt.SpecialTable = false;
                        }
                        Alrt.Description = dsData.Tables[0].Rows[i]["Description"].ToString();
                        DateTime _createdDate = new DateTime();
                        if (dsData.Tables[0].Rows[i]["dbDate"].ToString() != null && dsData.Tables[0].Rows[i]["dbDate"].ToString() != "")
                        {
                            _createdDate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["dbDate"].ToString());
                            DateTime _format = DateTime.SpecifyKind(new DateTime(_createdDate.Year, _createdDate.Month, _createdDate.Day, _createdDate.Hour, _createdDate.Minute, _createdDate.Second), DateTimeKind.Utc);
                            _createdDate = _format;
                            Alrt.CDate = _format;
                        }
                        lstNts.Add(Alrt);
                    }
                }
            }
            catch (Exception)
            {
            }
            return lstNts;
        }
        //Approve Notice
        public bool ApproveNotice(Ctx ctx, int VAF_Notice_ID, bool isAcknowldge)
        {
            MVAFNotice objNote = new MVAFNotice(ctx, VAF_Notice_ID, null);
            objNote.SetProcessed(isAcknowldge);
            if (objNote.Save())
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        #endregion

        #region StartRequest
        //count of request
        public int getRequestCnt(Ctx ctx)
        {
            int ncnt = 0;
            try
            {
                //To Get Request count
                //strQuery = " SELECT  count(VAR_Request.VAR_Request_ID) FROM VAR_Request  inner join  VAR_Req_Type rt on VAR_Request.VAR_Req_Type_id=rt.VAR_Req_Type_ID";
                strQuery = @" SELECT  count(VAR_Request.VAR_Request_ID) FROM VAR_Request
                        LEFT OUTER JOIN VAB_BusinessPartner
                        ON VAR_Request.VAB_BusinessPartner_ID=VAB_BusinessPartner.VAB_BusinessPartner_ID
                        LEFT OUTER JOIN VAR_Req_Type rt
                        ON VAR_Request.VAR_Req_Type_id = rt.VAR_Req_Type_ID
                        LEFT OUTER JOIN VAR_Req_Status rs
                        ON rs.VAR_Req_Status_ID=VAR_Request.VAR_Req_Status_ID
                        LEFT OUTER JOIN VAF_CtrlRef_List adl
                        ON adl.Value=VAR_Request.Priority
                        JOIN VAF_Control_Ref adr
                        ON adr.VAF_Control_Ref_ID=adl.VAF_Control_Ref_ID ";

                strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "VAR_Request", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
                strQuery += "  AND adr.Name='_PriorityRule'  AND ( VAR_Request.SalesRep_ID =" + ctx.GetVAF_UserContact_ID() + " OR VAR_Request.VAF_Role_ID =" + ctx.GetVAF_Role_ID() + ")"
                 + " AND VAR_Request.Processed ='N'"
                + " AND (VAR_Request.VAR_Req_Status_ID IS NULL OR VAR_Request.VAR_Req_Status_ID IN (SELECT VAR_Req_Status_ID FROM VAR_Req_Status WHERE IsClosed='N'))";


                dsData = new DataSet();
                dsData = DB.ExecuteDataset(strQuery);
                ncnt = Util.GetValueOfInt(dsData.Tables[0].Rows[0][0].ToString());
            }
            catch (Exception)
            {

            }
            return ncnt;
        }
        //List of Request
        public List<HomeRequest> getHomeRequest(Ctx ctx, int PageSize, int page)
        {
            List<HomeRequest> lstAlerts = new List<HomeRequest>();


            //strQuery = "SELECT VAB_BusinessPartner.Name ,rt.Name As CaseType,VAR_Request.DocumentNo , VAR_Request.Summary ,VAR_Request.StartDate ,VAR_Request.DateNextAction,VAR_Request.Created,"
            //+ "VAR_Request.VAR_Request_ID,VAR_Request.Priority as PriorityID,adl.Name as Priority,rs.name As Status,"
            //+ "(SELECT  VAF_TableView.TableName FROM  VAF_TableView WHERE  VAF_TableView.TableName='VAR_Request') TableName,"
            //+ "(SELECT  VAF_TableView.VAF_Screen_ID FROM  VAF_TableView WHERE  VAF_TableView.TableName='VAR_Request') VAF_Screen_ID  FROM VAR_Request"
            //+ " INNER JOIN VAB_BusinessPartner on VAR_Request.VAB_BusinessPartner_ID=VAB_BusinessPartner.VAB_BusinessPartner_ID"
            //+ " INNER JOIN VAR_Req_Type rt ON VAR_Request.VAR_Req_Type_id = rt.VAR_Req_Type_ID"
            //+ " Left outer JOIN  VAR_Req_Status rs on rs.VAR_Req_Status_ID=VAR_Request.VAR_Req_Status_ID"
            //+ " Left Outer JOIN  VAF_CtrlRef_List adl on adl.Value=VAR_Request.Priority"
            //+ " JOIN  VAF_Control_Ref adr on adr.VAF_Control_Ref_ID=adl.VAF_Control_Ref_ID";

            //            strQuery = @" SELECT VAB_BusinessPartner.Name ,
            //                          rt.Name AS CaseType,
            //                          VAR_Request.DocumentNo ,
            //                          VAR_Request.Summary ,
            //                          VAR_Request.StartDate ,
            //                          VAR_Request.DateNextAction,
            //                          VAR_Request.Created,
            //                          VAR_Request.VAR_Request_ID,
            //                          VAR_Request.Priority AS PriorityID,
            //                          adl.Name           AS Priority,
            //                          rs.name            AS Status,
            //                          (SELECT VAF_TableView.TableName FROM VAF_TableView WHERE VAF_TableView.TableName='VAR_Request'
            //                          ) TableName,
            //                          (SELECT VAF_TableView.VAF_Screen_ID
            //                          FROM VAF_TableView
            //                          WHERE VAF_TableView.TableName='VAR_Request'
            //                          ) VAF_Screen_ID
            //                        FROM VAR_Request
            //                        INNER JOIN VAB_BusinessPartner
            //                        ON VAR_Request.VAB_BusinessPartner_ID=VAB_BusinessPartner.VAB_BusinessPartner_ID
            //                        INNER JOIN VAR_Req_Type rt
            //                        ON VAR_Request.VAR_Req_Type_id = rt.VAR_Req_Type_ID
            //                        LEFT OUTER JOIN VAR_Req_Status rs
            //                        ON rs.VAR_Req_Status_ID=VAR_Request.VAR_Req_Status_ID
            //                        LEFT OUTER JOIN VAF_CtrlRef_List adl
            //                        ON adl.Value=VAR_Request.Priority
            //                        JOIN VAF_Control_Ref adr
            //                        ON adr.VAF_Control_Ref_ID=adl.VAF_Control_Ref_ID ";


            strQuery = @" SELECT VAB_BusinessPartner.Name ,
                          rt.Name AS CaseType,
                          VAR_Request.DocumentNo ,
                          VAR_Request.Summary ,
                          VAR_Request.StartDate ,
                          VAR_Request.DateNextAction,
                          VAR_Request.Created,
                          VAR_Request.VAR_Request_ID,
                          VAR_Request.Priority AS PriorityID,
                          adl.Name           AS Priority,
                          rs.name            AS Status,
                          (SELECT VAF_TableView.TableName FROM VAF_TableView WHERE VAF_TableView.TableName='VAR_Request'
                          ) TableName,
                          (SELECT VAF_TableView.VAF_Screen_ID
                          FROM VAF_TableView
                          WHERE VAF_TableView.TableName='VAR_Request'
                          ) VAF_Screen_ID
                        FROM VAR_Request
                        LEFT OUTER JOIN VAB_BusinessPartner
                        ON VAR_Request.VAB_BusinessPartner_ID=VAB_BusinessPartner.VAB_BusinessPartner_ID
                        LEFT OUTER JOIN VAR_Req_Type rt
                        ON VAR_Request.VAR_Req_Type_id = rt.VAR_Req_Type_ID
                        LEFT OUTER JOIN VAR_Req_Status rs
                        ON rs.VAR_Req_Status_ID=VAR_Request.VAR_Req_Status_ID
                        LEFT OUTER JOIN VAF_CtrlRef_List adl
                        ON adl.Value=VAR_Request.Priority
                        JOIN VAF_Control_Ref adr
                        ON adr.VAF_Control_Ref_ID=adl.VAF_Control_Ref_ID ";



            strQuery = MVAFRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "VAR_Request", MVAFRole.SQL_FULLYQUALIFIED, MVAFRole.SQL_RO);
            strQuery += "  AND adr.Name='_PriorityRule' AND ( VAR_Request.SalesRep_ID =" + ctx.GetVAF_UserContact_ID() + " OR VAR_Request.VAF_Role_ID =" + ctx.GetVAF_Role_ID() + ")"
            + " AND VAR_Request.Processed ='N'  AND (VAR_Request.VAR_Req_Status_ID IS NULL OR VAR_Request.VAR_Req_Status_ID IN (SELECT VAR_Req_Status_ID FROM VAR_Req_Status WHERE IsClosed='N')) ORDER By VAR_Request.Updated, VAR_Request.Priority ";
            // change to sort Requests based on updated date and time


            //Request
            //strQuery = " SELECT rt.Name ,VAR_Request.Summary , VAR_Request.StartDate ,VAR_Request.DateNextAction,DateLastAction.Created"
            // + " VAR_Request.VAR_Request_ID  FROM VAR_Request  inner join  VAR_Req_Type rt on VAR_Request.VAR_Req_Type_id=rt.VAR_Req_Type_ID";
            //strQuery = MRole.Get(ctx, ctx.GetVAF_Role_ID()).AddAccessSQL(strQuery, "VAR_Request", MRole.SQL_FULLYQUALIFIED, MRole.SQL_RO);
            //strQuery += " AND ( VAR_Request.SalesRep_ID =" + ctx.GetVAF_UserContact_ID() + " OR VAR_Request.VAF_Role_ID =" + ctx.GetVAF_Role_ID() + ")"
            //+ " AND VAR_Request.Processed ='N' AND (VAR_Request.DateNextAction IS NULL OR TRUNC(VAR_Request.DateNextAction, 'DD') <= TRUNC(SysDate, 'DD'))"
            //+ " AND (VAR_Request.VAR_Req_Status_ID IS NULL OR VAR_Request.VAR_Req_Status_ID IN (SELECT VAR_Req_Status_ID FROM VAR_Req_Status WHERE IsClosed='N'))";

            SqlParamsIn objSP = new SqlParamsIn();
            dsData = new DataSet();
            objSP.page = page;
            objSP.pageSize = PageSize;
            objSP.sql = strQuery;
            dsData = VIS.DBase.DB.ExecuteDatasetPaging(objSP.sql, objSP.page, objSP.pageSize);
            if (dsData != null)
            {
                dsData = VAdvantage.DataBase.DB.SetUtcDateTime(dsData);
                for (int i = 0; i < dsData.Tables[0].Rows.Count; i++)
                {
                    var Alrt = new HomeRequest();
                    Alrt.VAR_Request_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAR_Request_ID"].ToString());
                    Alrt.VAF_Screen_ID = Util.GetValueOfInt(dsData.Tables[0].Rows[i]["VAF_Screen_ID"].ToString());
                    Alrt.TableName = dsData.Tables[0].Rows[i]["TableName"].ToString();
                    Alrt.Name = dsData.Tables[0].Rows[i]["Name"].ToString();
                    Alrt.CaseType = dsData.Tables[0].Rows[i]["CaseType"].ToString();
                    Alrt.DocumentNo = dsData.Tables[0].Rows[i]["DocumentNo"].ToString();
                    Alrt.Status = dsData.Tables[0].Rows[i]["Status"].ToString();
                    Alrt.Priority = dsData.Tables[0].Rows[i]["Priority"].ToString();
                    Alrt.Summary = dsData.Tables[0].Rows[i]["Summary"].ToString();


                    DateTime _DateNextAction = new DateTime();
                    if (dsData.Tables[0].Rows[i]["DateNextAction"].ToString() != null && dsData.Tables[0].Rows[i]["DateNextAction"].ToString() != "")
                    {
                        _DateNextAction = Convert.ToDateTime(dsData.Tables[0].Rows[i]["DateNextAction"].ToString());
                        DateTime _format = DateTime.SpecifyKind(new DateTime(_DateNextAction.Year, _DateNextAction.Month, _DateNextAction.Day, _DateNextAction.Hour, _DateNextAction.Minute, _DateNextAction.Second), DateTimeKind.Utc);
                        _DateNextAction = _format;
                        Alrt.NextActionDate = _format;
                    }

                    DateTime _createdDate = new DateTime();
                    if (dsData.Tables[0].Rows[i]["created"].ToString() != null && dsData.Tables[0].Rows[i]["created"].ToString() != "")
                    {
                        _createdDate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["created"].ToString());
                        DateTime _format = DateTime.SpecifyKind(new DateTime(_createdDate.Year, _createdDate.Month, _createdDate.Day, _createdDate.Hour, _createdDate.Minute, _createdDate.Second), DateTimeKind.Utc);
                        _createdDate = _format;
                        Alrt.CreatedDate = _format;
                    }



                    DateTime _StartDate = new DateTime();
                    if (dsData.Tables[0].Rows[i]["StartDate"].ToString() != null && dsData.Tables[0].Rows[i]["StartDate"].ToString() != "")
                    {
                        _StartDate = Convert.ToDateTime(dsData.Tables[0].Rows[i]["StartDate"].ToString());
                        DateTime _format = DateTime.SpecifyKind(new DateTime(_StartDate.Year, _StartDate.Month, _StartDate.Day, _StartDate.Hour, _StartDate.Minute, _StartDate.Second), DateTimeKind.Utc);
                        _StartDate = _format;
                        Alrt.StartDate = _format;
                    }
                    lstAlerts.Add(Alrt);
                }
            }
            return lstAlerts;
        }
        #endregion



        public List<FavNode> GetBarNodes(List<VTreeNode> nodes)
        {
            List<FavNode> items = new List<FavNode>();
            FavNode itm = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                itm = new FavNode();
                itm.Name = nodes[i].SetName;
                itm.Action = nodes[i].GetAction();
                itm.WindowID = nodes[i].VAF_Screen_ID;
                itm.FormID = nodes[i].VAF_Page_ID;
                itm.ProcessID = nodes[i].VAF_Job_ID;
                itm.NodeID = nodes[i].GetNode_ID();
                items.Add(itm);
            }
            return items;


        }


        public string SetNodeFavourite(int nodeID, Ctx ctx)
        {
            int VAF_TreeInfo_ID = DB.GetSQLValue(null,
                        "SELECT COALESCE(r.VAF_Tree_Menu_ID, ci.VAF_Tree_Menu_ID)"
                       + "FROM VAF_ClientDetail ci"
                       + " INNER JOIN VAF_Role r ON (ci.VAF_Client_ID=r.VAF_Client_ID) "
                       + "WHERE VAF_Role_ID=" + ctx.GetVAF_Role_ID());
            string sql = "INSERT INTO VAF_TreeInfoBar "
                                 + "(VAF_TreeInfo_ID,VAF_UserContact_ID,Node_ID, "
                                 + "VAF_Client_ID,VAF_Org_ID, "
                                 + "IsActive,Created,CreatedBy,Updated,UpdatedBy)VALUES (" + VAF_TreeInfo_ID + "," + ctx.GetVAF_UserContact_ID() + "," + nodeID + ","
                                 + ctx.GetVAF_Client_ID() + "," + ctx.GetVAF_Org_ID() + ","
                                 + "'Y',SysDate," + ctx.GetVAF_UserContact_ID() + ",SysDate," + ctx.GetVAF_UserContact_ID() + ")";
            //	if already exist, will result in ORA-00001: unique constraint 
            return DB.ExecuteQuery(sql, null).ToString();

        }

        public string RemoveNodeFavourite(int nodeID, Ctx ctx)
        {
            int VAF_TreeInfo_ID = DB.GetSQLValue(null,
                        "SELECT COALESCE(r.VAF_Tree_Menu_ID, ci.VAF_Tree_Menu_ID)"
                       + "FROM VAF_ClientDetail ci"
                       + " INNER JOIN VAF_Role r ON (ci.VAF_Client_ID=r.VAF_Client_ID) "
                       + "WHERE VAF_Role_ID=" + ctx.GetVAF_Role_ID());
            string sql = sql = "DELETE FROM VAF_TreeInfoBar WHERE VAF_TreeInfo_ID=" + VAF_TreeInfo_ID + " AND VAF_UserContact_ID=" + ctx.GetVAF_UserContact_ID()
                       + " AND Node_ID=" + nodeID;
            return DB.ExecuteQuery(sql, null).ToString();

        }

        public string GetSubscriptionDaysLeft(string url)
        {
            object key = null;
            try
            {
                key = System.Web.Configuration.WebConfigurationManager.AppSettings["TrialMessage"];

                if (key == null || key.ToString() == "" || key.ToString() == "N")
                {
                    return "True";
                }

            }
            catch
            {
                return "True";
            }

            string retUrl = "";
            BaseLibrary.CloudService.ServiceSoapClient cloud = null;

            try
            {
                cloud = VAdvantage.Classes.ServerEndPoint.GetCloudClient();

                if (cloud == null || cloud.ToString() == "")
                {
                    //Response.Redirect("http://demo.viennaadvantage.com",true);
                    retUrl = GenerateUrl(url);
                    return retUrl;
                }
            }
            catch
            {
            }
            try
            {
                //System.Net.ServicePointManager.Expect100Continue = false;
                try
                {
                    System.Net.ServicePointManager.Expect100Continue = false;
                    retUrl = cloud.GetSubscriptionDays(url, SecureEngine.Encrypt(System.Web.Configuration.WebConfigurationManager.AppSettings["accesskey"].ToString()));
                }
                catch
                {

                }
                cloud.Close();
            }
            catch
            {

                return retUrl;
            }

            return retUrl;
        }

        private static string GenerateUrl(string urlIn)
        {
            string urlOut = "";

            if (VAdvantage.Classes.ServerEndPoint.IsSSLEnabled())
            {

                if (urlIn.Contains("http://"))
                {
                    urlOut = urlIn.Replace("http://", "https://");
                    //  Response.Redirect(url, false);
                }
                else if (urlIn.Contains("https://"))
                {
                    urlOut = "";
                }
                else
                {
                    urlOut = "https://" + urlIn;
                }
            }
            return urlOut;
        }

        public void InitializeLog(Ctx ct)
        {
            DataSet ds = null;
            try
            {
                ds = DB.ExecuteDataset("SELECT MaintainAccording,TraceLevel FROM VAF_ClientDetail WHERE VAF_Client_ID=" + ct.GetVAF_Client_ID());
            }
            catch
            {
            }
            if (ds == null || ds.Tables[0].Rows.Count == 0)
            {
                return;
            }
            string maintainAccording = Util.GetValueOfString(ds.Tables[0].Rows[0][0]);
            if (string.IsNullOrEmpty(maintainAccording))
            {
                return;
            }
            else if (maintainAccording == "T")
            {
                string traceLevel = Util.GetValueOfString(ds.Tables[0].Rows[0][1]);
                if (!string.IsNullOrEmpty(traceLevel) && Util.GetValueOfInt(traceLevel) < 9999)
                {
                    long hid = DateTime.Now.Ticks;
                    ct.SetContext("#LogHandler", hid.ToString());
                // VAdvantage.Logging.VLogMgt.Initialize(true, Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, (ct.GetVAF_Org_Name() == "*" ? "Star" : ct.GetVAF_Org_Name()) + "_" + ct.GetVAF_UserContact_Name()), Util.GetValueOfInt(traceLevel), hid, ct);
                }
            }
            else if (maintainAccording == "U")
            {
                string traceLevel = ct.GetContext("TraceLevel");
                if (!string.IsNullOrEmpty(traceLevel) && Util.GetValueOfInt(traceLevel) < 9999)
                {
                    long hid = DateTime.Now.Ticks;
                    ct.SetContext("#LogHandler", hid.ToString());
              //   VAdvantage.Logging.VLogMgt.Initialize(true, Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, (ct.GetVAF_Org_Name() == "*" ? "Star" : ct.GetVAF_Org_Name()) + "_" + ct.GetVAF_UserContact_Name()), Util.GetValueOfInt(traceLevel), hid, ct);
                }
            }

        }
    }


    public class FavNode
    {
        public string Name
        {
            get;
            set;

        }
        public string Action
        {
            get;
            set;

        }
        public int WindowID
        {
            get;
            set;

        }
        public int FormID
        {
            get;
            set; 

        }
        public int ProcessID
        {
            get;
            set;

        }
        public int NodeID
        {
            get;
            set;
        }


    }
}