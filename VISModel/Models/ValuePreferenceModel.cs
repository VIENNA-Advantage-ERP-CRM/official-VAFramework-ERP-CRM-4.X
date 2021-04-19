﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VAdvantage.Classes;
using VAdvantage.DataBase;
using VAdvantage.Model;
using VAdvantage.Utility;

namespace VIS.Controllers
{
    public class ValuePreferenceModel
    {
        public String Attribute { get; set; }
        public String DisplayAttribute { get; set; }
        public String Value { get; set; }
        public String DisplayValue { get; set; }
        public int DisplayType { get; set; }

        public int VAF_Client_ID { get; set; }
        public int VAF_Org_ID { get; set; }
        public int VAF_UserContact_ID { get; set; }
        public int VAF_Screen_ID { get; set; }
        public int VAF_Control_Ref_ID { get; set; }

        //Repository

        /// <summary>
        /// delete prefrence accouring to there value
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="preferenceId"></param>
        /// <returns></returns>
        public bool DeletePrefrence(Ctx ctx, string preferenceId)
        {
            bool success = false;
            int VAF_ValuePreference_ID = Convert.ToInt32(preferenceId);

            MVAFValuePreference pref = new MVAFValuePreference(ctx, VAF_ValuePreference_ID, null);
            // delete the preference
            success = pref.Delete(true);

            return success;
        }

        /// <summary>
        /// Save logic for value prefrences
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="preferenceId"></param>
        /// <param name="clientId"></param>
        /// <param name="orgId"></param>
        /// <param name="chkWindow"></param>
        /// <param name=
        /// ></param>
        /// <param name="chkUser"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public bool SavePrefrence(Ctx ctx, string preferenceId, string clientId, string orgId, string chkWindow, string VAF_Screen_ID, string chkUser, string attribute, string userId, string value)
        {
            bool success = false;

            int VAF_ValuePreference_ID = Convert.ToInt32(preferenceId);
            int _VAF_Screen_ID = Convert.ToInt32(VAF_Screen_ID);
            int _VAF_UserContact_ID = Convert.ToInt32(userId);
            bool _chkUser, _chkWindow;
            _chkUser = Convert.ToBoolean(chkUser);
            _chkWindow = Convert.ToBoolean(chkWindow);

            MVAFValuePreference pref = new MVAFValuePreference(ctx, VAF_ValuePreference_ID, null);
            // if preference id=0
            if (VAF_ValuePreference_ID == 0)
            {
                // if inserting a new record, then set initial values
                // set client id
                int Client_ID = Convert.ToInt32(clientId);
                // set organization id
                int Org_ID = Convert.ToInt32(orgId);

                pref.SetClientOrg(Client_ID, Org_ID);
                // set window id
                if (_chkWindow)
                {
                    pref.SetVAF_Screen_ID(_VAF_Screen_ID);
                }
                // set user id
                if (_chkUser)
                {
                    pref.SetVAF_UserContact_ID(_VAF_UserContact_ID);
                }

                // set attribute(columnname)
                pref.SetAttribute(attribute);
            }
            // set value of attribute
            pref.SetValue(value);
            // save the record
            success = pref.Save();
            return success;
        }

        // Added by Bharat on 12 June 2017
        public int GetPrefrenceID(string sql)
        {            
            int prefID = Util.GetValueOfInt(DB.ExecuteScalar(sql));
            return prefID;
        }
    }
}