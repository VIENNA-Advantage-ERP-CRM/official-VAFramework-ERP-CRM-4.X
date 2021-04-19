﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAPAReportingOrder
 * Purpose        : Reporting Hierarchy Model
 * Class Used     : X_VAPA_ReportingOrder
 * Chronological    Development
 * Deepak           11-Jan-2010
  ******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Model;
using System.Data.SqlClient;
using System.Data;
using VAdvantage.Utility;
using VAdvantage.Login;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVAPAReportingOrder:X_VAPA_ReportingOrder
    {
        /// <summary>
        /// Get MVAPAReportingOrder from Cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAPA_FinancialReportingOrder_ID">id</param>
        /// <returns>MVAPAReportingOrder</returns>
        public static MVAPAReportingOrder Get(Ctx ctx, int VAPA_FinancialReportingOrder_ID)
        {
            int key =VAPA_FinancialReportingOrder_ID;
            MVAPAReportingOrder retValue = (MVAPAReportingOrder)s_cache[key];//.get(key);
            if (retValue != null)
            {
                return retValue;
            }
            retValue = new MVAPAReportingOrder(ctx, VAPA_FinancialReportingOrder_ID, null);
            if (retValue.Get_ID() != 0)
            {
                s_cache.Add(key, retValue);// .put(key, retValue);
            }
            return retValue;
        } //	get

        /**	Cache						*/
        private static CCache<int, MVAPAReportingOrder> s_cache
            = new CCache<int, MVAPAReportingOrder>("VAPA_FinancialReportingOrder_ID", 20);

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAPA_FinancialReportingOrder_ID">id</param>
        /// <param name="trxName">trx</param>
        public MVAPAReportingOrder(Ctx ctx, int VAPA_FinancialReportingOrder_ID, Trx trxName):base(ctx, VAPA_FinancialReportingOrder_ID, trxName)
        {
            //super(ctx, VAPA_FinancialReportingOrder_ID, trxName);
        }	//	MVAPAReportingOrder

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">datarow</param>
        /// <param name="trxName">trx</param>
        public MVAPAReportingOrder(Ctx ctx,DataRow dr, Trx trxName):base(ctx,dr, trxName)
        {
            //super(ctx, rs, trxName);
        }	//	MVAPAReportingOrder

        /// <summary>
        /// Get VAF_TreeInfo_ID based on tree type
        /// </summary>
        /// <param name="TreeType">Tree Type</param>
        /// <returns>id or 0</returns>
        public int GetVAF_TreeInfo_ID(String TreeType)
        {
            if (MVAFTreeInfo.TREETYPE_Activity.Equals(TreeType))
            {
                return GetVAF_Tree_Activity_ID();
            }
            if (MVAFTreeInfo.TREETYPE_BPartner.Equals(TreeType))
            {
                return GetVAF_Tree_BPartner_ID();
            }
            if (MVAFTreeInfo.TREETYPE_Campaign.Equals(TreeType))
            {
                return GetVAF_Tree_Campaign_ID();
            }
            if (MVAFTreeInfo.TREETYPE_ElementValue.Equals(TreeType))
            {
                return GetVAF_Tree_Account_ID();
            }
            if (MVAFTreeInfo.TREETYPE_Organization.Equals(TreeType))
            {
                return GetVAF_Tree_Org_ID();
            }
            if (MVAFTreeInfo.TREETYPE_Product.Equals(TreeType))
            {
                return GetVAF_Tree_Product_ID();
            }
            if (MVAFTreeInfo.TREETYPE_Project.Equals(TreeType))
            {
                return GetVAF_Tree_Project_ID();
            }
            if (MVAFTreeInfo.TREETYPE_SalesRegion.Equals(TreeType))
            {
                return GetVAF_Tree_SalesRegion_ID();
            }
            //
            log.Warning("Not supported: " + TreeType);
            return 0;
        }	//	getVAF_TreeInfo_ID

    }	//	MVAPAReportingOrder

}
