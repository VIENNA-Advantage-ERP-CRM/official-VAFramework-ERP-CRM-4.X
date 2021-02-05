﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using VAdvantage.Model;
using VAdvantage.Utility;
using VIS.DBase;

namespace VIS.Models
{
    public class MProjectModel
    {
        /// <summary>
        /// GetProjectDetail
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetProjectDetail(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int TaskID = 0, phaseID = 0, projID = 0, ProductID = 0;
            TaskID = Util.GetValueOfInt(paramValue[0].ToString());
            phaseID = Util.GetValueOfInt(paramValue[1].ToString());
            projID = Util.GetValueOfInt(paramValue[2].ToString());
            ProductID = Util.GetValueOfInt(paramValue[3].ToString());
            Dictionary<string, object> result = null;
            string Sql = "SELECT VAB_Project_ID FROM VAB_ProjectStage WHERE VAB_ProjectStage_ID IN (SELECT VAB_ProjectStage_ID FROM" +
                    " VAB_ProjectJob WHERE VAB_ProjectJob_ID = " + TaskID + ")";
            int id = Util.GetValueOfInt(DB.ExecuteScalar(Sql, null, null));
            if (id > 0)
            {
                projID = id;
            }
            else
            {
                Sql = "SELECT VAB_Project_ID FROM VAB_ProjectStage WHERE VAB_ProjectStage_ID = " + phaseID;
                id = Util.GetValueOfInt(DB.ExecuteScalar(Sql, null, null));
                if (id > 0)
                {
                    projID = id;
                }
            }
            //Issue ID= SI_0468 Reported by Ankita Work Done by Manjot 
            //To get the actual value from the right field
            Sql = "SELECT PriceList, PriceStd, PriceLimit FROM VAM_ProductPrice WHERE VAM_PriceListVersion_ID = (SELECT c.VAM_PriceListVersion_ID FROM VAB_Project c WHERE c.VAB_Project_ID = "
                + projID + ")  AND VAM_Product_ID=" + ProductID;
            DataSet ds = DB.ExecuteDataset(Sql, null, null);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                result = new Dictionary<string, object>();
                result["PriceList"] = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceList"]);
                result["PriceStd"] = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceStd"]);
                result["PriceLimit"] = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceLimit"]);
            }
            return result;
        }

        // Added by Bharat on 23 May 2017
        public Decimal GetProjectPriceLimit(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int TaskID = 0, projID = 0, ProductID = 0;
            TaskID = Util.GetValueOfInt(paramValue[0].ToString());
            projID = Util.GetValueOfInt(paramValue[1].ToString());
            ProductID = Util.GetValueOfInt(paramValue[2].ToString());
            string Sql = "SELECT VAB_Project_ID FROM VAB_ProjectStage WHERE VAB_ProjectStage_ID IN (SELECT VAB_ProjectStage_ID FROM" +
                    " VAB_ProjectJob WHERE VAB_ProjectJob_ID = " + TaskID + ")";
            int id = Util.GetValueOfInt(DB.ExecuteScalar(Sql, null, null));
            if (id > 0)
            {
                projID = id;
            }

            Sql = "SELECT PriceLimit FROM VAM_ProductPrice WHERE VAM_PriceListVersion_ID = (SELECT c.VAM_PriceListVersion_ID FROM  VAB_Project c WHERE c.VAB_Project_ID = "
                + projID + ")  AND VAM_Product_ID=" + ProductID;
            Decimal PriceLimit = Util.GetValueOfDecimal(DB.ExecuteScalar(Sql, null, null));
            return PriceLimit;
        }
    }
}