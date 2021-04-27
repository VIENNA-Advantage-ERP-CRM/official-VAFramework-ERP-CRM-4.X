﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAMManufacturer
 * Purpose        : Product Manufacturer setting using x-classes
 * Class Used     : X_VAM_Manufacturer
 * Chronological    Development
 * Raghunandan     27-March-2015
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
using VAdvantage.ProcessEngine;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
//////using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVAMManufacturer : X_VAM_Manufacturer
    {
        #region variable

        private string sql = "";
        private int manu_ID = 0;
        private static VLogger _log = VLogger.GetVLogger(typeof(MVABOrderLine).FullName);
        #endregion

        /// <summary>
        /// Load Cosntructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="rs">result set</param>
        /// <param name="trxName">transaction</param>
        public MVAMManufacturer(Ctx ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /// <summary>
        /// Load Cosntructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="rs">result set</param>
        /// <param name="trxName">transaction</param>
        public MVAMManufacturer(Ctx ctx, int VAM_Manufacturer_ID, Trx trxName)
            : base(ctx, VAM_Manufacturer_ID, trxName)
        {
        }

        protected override bool BeforeSave(bool newRecord)
        {
            if (!String.IsNullOrEmpty(GetUPC()) &&
                          Util.GetValueOfString(Get_ValueOld("UPC")) != GetUPC())
            {
                //sql = "SELECT UPCUNIQUE('m','" + GetUPC() + "') as productID FROM Dual";
                //manu_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));

                manu_ID = MVAMProduct.UpcUniqueClientWise(GetVAF_Client_ID(), GetUPC());
                if (manu_ID > 0)
                //if (manu_ID != 0 && manu_ID != GetVAM_Product_ID())
                {
                    _log.SaveError("UPCUnique", "");
                    return false;
                }
            }
            return true;
        }

        //protected override bool AfterDelete(bool success)
        //{
        //    sql = "DELETE FROM VAM_ProductFeatures WHERE IsActive = 'Y' AND VAM_PFeature_SetInstance_ID = " + GetVAM_PFeature_SetInstance_ID() + " AND UPC = '" + GetUPC() + "'";
        //    manu_ID = DB.ExecuteQuery(sql, null, null);
        //    if (manu_ID <= 0)
        //    {
        //        return false;
        //    }
        //    return true;
        //}
    }
}