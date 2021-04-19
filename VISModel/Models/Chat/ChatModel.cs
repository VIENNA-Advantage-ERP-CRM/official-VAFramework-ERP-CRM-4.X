﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

using System.Linq;
using System.Resources;
using System.Text;
using System.Web;
using VAdvantage.DataBase;
using VAdvantage.Model;
using VAdvantage.Utility;
using VIS.DataContracts;

namespace VIS.Models
{
    public class ChatModel
    {

        #region Private Variables
        //Window No
        private int _windowNo;
        //Attachment
        private MVACMChat _chat;
        // private MChatEntry _entry; 
        //get login info from context class
       // Ctx ctx = Env.GetContext();
        private MVACMChatLine[] _chatEntries = null;//chat entries
        //private SimpleDateFormat _createdDate = null;
        private DateTime _createdDate;//get date from database according to chat entry
        private DataSet _ds = null;//data set for VACM_ChatLine table
        private DateTime _format;
        private ChatInfo subsribedChat = null;
        //change text
        //private Boolean _change = false;

        const double SUBTRACTED_WIDTH = 100;
        #endregion

        //System.Runtime.CompilerServices.ExtensionAttribute x = null;


        #region Parametrised Constructor

        public ChatModel(Ctx ct, int Chat_ID, int VAF_TableView_ID, int Record_ID, string description)
        {
            // _chat = new MChat(ct, Chat_ID, null);
            //ctx = ct;

            if (Chat_ID == 0)
            {
                //set chat from MChat class first time
                _chat = new MVACMChat(ct, VAF_TableView_ID, Record_ID, description, null);
            }
            else
            {
                _chat = new MVACMChat(ct, Chat_ID, null);
            }
            //ctx = ct;

        }






        /// <summary>
        /// Parametrised Constructor.
        /// loads Chat, if ID <> 0
        /// </summary>
        /// <param name="windowNo">window no</param>
        /// <param name="VACM_Chat_ID">chat</param>
        /// <param name="VAF_TableView_ID">table</param>
        /// <param name="Record_ID">record key</param>
        /// <param name="description">description</param>
        /// <param name="trxName">transaction</param>
        public ChatModel(Ctx ct, int windowNo, int VACM_Chat_ID, int VAF_TableView_ID, int Record_ID, String description, Trx trxName, int page, int pageSize)
        // : base(false, false, false, false, "Chat")
        {
            //set current window
            _windowNo = windowNo;
            //set current window
            //from design
            //  SetBusy(true);
            // double width = this.Width;
            // LayoutRootChat.Background = new SolidColorBrush(DataBase.GlobalVariable.WINDOW_BACK_COLOR);
            // EventHander();
            //	when chatId is zero
            if (VACM_Chat_ID == 0)
            {
                //set chat from MChat class first time
                _chat = new MVACMChat(ct, VAF_TableView_ID, Record_ID, description, trxName);
            }
            else
            {
                _chat = new MVACMChat(ct, VACM_Chat_ID, trxName);
            }
            subsribedChat = new ChatInfo();
            subsribedChat = GetHistory(MVACMChat.CONFIDENTIALTYPE_Internal, _chat, page, pageSize,ct);
            //ctx = ct;
            //Call Load Chat function 
            // Deployment.Current.Dispatcher.BeginInvoke(() => ShowText(subsribedChat));
        }
        #endregion


        #region GetEntries
        /// <summary>
        ///Get Entries
        /// </summary>
        /// <param name="reload">Bool Type(reload data)</param>
        /// <returns>array of lines</returns>
        public MVACMChatLine[] GetEntries(Boolean reload, int chatID, int page, int pageSize,Ctx ctx)
        {
            //chat entries
            if (_chatEntries != null && !reload)
                return _chatEntries;//return chat
            //list for chatEntry records
            List<MVACMChatLine> list = new List<MVACMChatLine>();
            String sql = "SELECT * FROM VACM_ChatLine WHERE VACM_Chat_ID=" + chatID + " ORDER BY Created";


            //SqlParamsIn objSP = new SqlParamsIn();
            //_ds = new DataSet();
            //objSP.page = page;
            //objSP.pageSize = pageSize;
            //objSP.sql = sql.ToString();

            //_ds = ExecuteDataSetPaging(objSP);

            _ds = DB.ExecuteDataset(sql);


            try
            {
                //execute the Query get number of chat for a perticular CHAT_ID
                //_ds = DB.ExecuteDataset(sql, null, null);

                DataRow rs;
                for (int i = 0; i < _ds.Tables[0].Rows.Count; i++)
                {
                    rs = _ds.Tables[0].Rows[i];
                    //list.Add(new MChatEntry(GetCtx, rs, Get_TrxName()));
                    //add chatentries into list from VACM_ChatLine table
                    list.Add(new MVACMChatLine(ctx, rs, null));
                }
                //_ds = null;

            }
            catch (Exception)
            {
                //log.Log(Level.SEVERE, sql, e);
            }
            //count number of records
            _chatEntries = new MVACMChatLine[list.Count];
            //add list into array
            _chatEntries = list.ToArray();
            //list.ToArray(_chatEntries);1..........................
            return _chatEntries;
        }
        #endregion



        #region Chat History


        /// <summary>
        /// Get History as text from data base using Html display formate
        /// </summary>
        /// <param name="ConfidentialType">confidential type</param>
        /// <returns>text from control</returns>
        public ChatInfo GetHistory(String confidentialType, MVACMChat _chat, int page, int pageSize,Ctx ctx)
        {
            ChatInfo cinfo = new ChatInfo();
            GetEntries(true, _chat.GetCM_Chat_ID(), page, pageSize,ctx);//array list status

            StringBuilder strName = new StringBuilder();
            List<LatestSubscribedRecordChat> subscribedChat = new List<LatestSubscribedRecordChat>();
            //List<int> imgIds = new List<int>();
            List<UserImages> imgIds = new List<UserImages>();
            DataSet ds = null;
            //ring img = null;
            int imgID = 0;
            for (int i = 0; i < _chatEntries.Length; i++)
            {
                //olean first = true;
                MVACMChatLine entry = _chatEntries[i];
                //get the created date of a perticular chat from PO
                _createdDate = entry.GetCreated();
                _format = DateTime.SpecifyKind(new DateTime(_createdDate.Year, _createdDate.Month, _createdDate.Day, _createdDate.Hour, _createdDate.Minute, _createdDate.Second), DateTimeKind.Utc);
                _createdDate = _format;
                if (!entry.IsActive() || !entry.IsConfidentialTypeValid(confidentialType))
                    continue;
                //status for first chat
                string sql = "SELECT au.name, aimg.VAF_Image_id FROM VAF_UserContact au LEFT OUTER JOIN VAF_Image aimg";
                sql += " ON(au.VAF_Image_id= aimg.VAF_Image_id) where au.VAF_UserContact_id =" + entry.GetCreatedBy();
                ds = DB.ExecuteDataset(sql, null);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    strName.Clear();
                    strName.Append(ds.Tables[0].Rows[0]["NAME"].ToString());
                    //if (ds.Tables[0].Rows[0]["BINARYDATA"] != null && ds.Tables[0].Rows[0]["BINARYDATA"] != DBNull.Value)
                    //{
                    //    img = Convert.ToBase64String((Byte[])ds.Tables[0].Rows[0]["BINARYDATA"]);
                    //}
                    imgID = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAF_Image_id"]);


                    if (imgIds.Where(a => a.VAF_Image_ID == imgID).Count() == 0)
                    {
                        UserImages uimsg = new UserImages();
                        uimsg.VAF_Image_ID = imgID;
                        MVAFImage mimg = new MVAFImage(ctx, imgID, null);
                        uimsg.UserImg = mimg.GetThumbnailURL(46, 46);
                        imgIds.Add(uimsg);
                    }
                }
                subscribedChat.Add(new LatestSubscribedRecordChat()
                {
                    //UserImg = img,
                    UserName = strName.ToString(),
                    ChatData = entry.GetCharacterData(),
                    ChatDate = _createdDate,
                    VAF_Image_ID = imgID,
                    VAF_UserContact_ID = entry.GetCreatedBy()
                }
                );
            }




            cinfo.subChat = subscribedChat;
            cinfo.userimages = imgIds;
            return cinfo;

        }


        #endregion

        public bool Ok(string data)
        {
            if (data.Trim() != "" && data.Trim() != null && data.Length > 0)
            {
                if (_chat.Get_ID() == 0)
                {
                    _chat.Save();
                }
                MVACMChatLine entry = new MVACMChatLine(_chat, data);

                bool saved = entry.Save();

                return saved;
            }
            return false;
            //else
            //{
            //    ShowMessage.Info("PleaseEnterText", null, "", "");
            //    btnOK.IsEnabled = true;
            //    SetBusy(false);
            //    rtxtEnter.Focus();
            //}
        }


        public ChatInfo GetChatData()
        {
            return subsribedChat;
        }


        //public DataSet ExecuteDataSetPaging(SqlParamsIn sqlIn)
        //{
        //    string sql = sqlIn.sql;
        //    bool doPaging = sqlIn.pageSize > 0;
        //    SqlParams[] paramIn = sqlIn.param == null ? null : sqlIn.param.ToArray();
        //    DataSet ds = null;
        //    string strConn = System.Configuration.ConfigurationSettings.AppSettings["oracleConnectionString"];

        //    //Paging
        //    OracleConnection Connection = new OracleConnection(strConn);
        //    try
        //    {
        //        Connection.Open();
        //        OracleDataAdapter adapter = new OracleDataAdapter();
        //        adapter.SelectCommand = new OracleCommand(sql);
        //        adapter.SelectCommand.Connection = Connection;
        //        ds = new DataSet();

        //        if (sqlIn.page < 1)// Set rowcount =PageNumber * PageSize for best performance
        //        {
        //            sqlIn.page = 1;
        //        }
        //        adapter.Fill(ds, (sqlIn.page - 1) * sqlIn.pageSize, sqlIn.pageSize, "Data");
        //    }
        //    catch
        //    {

        //    }
        //    finally
        //    {
        //        Connection.Close();
        //    }
        //    return ds;
        //}




    }

    public class ChatProperties
    {
        public int VAF_TableView_ID { get; set; }
        public int Record_ID { get; set; }
        public int ChatID { get; set; }
        public string Description { get; set; }
        public int WindowNo { get; set; }
        public string TrxName { get; set; }
        public string ChatText { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
    }



    public class LatestSubscribedRecordChat
    {
        public string ChatData { get; set; }
        public Object ChatDate { get; set; }
        public string UserName { get; set; }
        public int VAF_Image_ID { get; set; }
        public int VAF_UserContact_ID { get; set; }
    }

    public class UserImages
    {
        public string UserImg { get; set; }
        public int VAF_Image_ID { get; set; }
    }


    public class ChatInfo
    {
        public List<UserImages> userimages { get; set; }
        public List<LatestSubscribedRecordChat> subChat { get; set; }
    }



}