﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using VAdvantage.DataBase;
using VAdvantage.Model;
using VAdvantage.Utility;
namespace VIS.Models
{
    public class SubscribeModel
    {
        Ctx _ctx = null;
        public SubscribeModel()
        {

        }


        public SubscribeModel(Ctx ctx)
        {
            _ctx = ctx;
        }

        public int InsertSubscription(int win_ID, int rec_ID, int table_ID)
        {
            X_VACM_Subscribe subs = new X_VACM_Subscribe(_ctx, 0, null);
            subs.SetVAF_Client_ID(_ctx.GetVAF_Client_ID());
            subs.SetVAF_Org_ID(_ctx.GetVAF_Org_ID());
            subs.SetVAF_Screen_ID(win_ID);
            subs.SetVAF_TableView_ID(table_ID);
            subs.SetRecord_ID(rec_ID);
            subs.SetVAF_UserContact_ID(_ctx.GetVAF_UserContact_ID());
            if (!subs.Save())
            {
                return 0;
            }
            return 1;
        }


        public int DeleteSubscription(int VAF_Screen_ID, int Record_ID, int VAF_TableView_ID)
        {

            if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT Count(*) FROM VACM_Subscribe WHERE  VAF_Screen_ID=" + VAF_Screen_ID + " AND VAF_TableView_ID=" + VAF_TableView_ID + " AND Record_ID=" + Record_ID)) > 0)
            {
                if (DB.ExecuteQuery("delete from VACM_Subscribe where  VAF_Screen_ID=" + VAF_Screen_ID + " AND VAF_TableView_ID=" + VAF_TableView_ID + " AND Record_ID=" + Record_ID) != 1)
                {
                    return 0;
                }
            }
            else
            {
                return 2;
            }
            return 1;
        }



    }




}