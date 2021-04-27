﻿/********************************************************
 * Module Name    : 
 * Purpose        : Model for Commission.
 * Class Used     : X_VAB_WorkCommission
 * Chronological    Development
 * Veena        07-Nov-2009
**********************************************************/

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Classes;
using VAdvantage.Utility;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    /// <summary>
    /// Model for Commission.
    /// </summary>
    public class MVABWorkCommission : X_VAB_WorkCommission
    {
        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_WorkCommission_Amt_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVABWorkCommission(Ctx ctx, int VAB_WorkCommission_ID, Trx trxName)
            : base(ctx, VAB_WorkCommission_ID, trxName)
        {
            if (VAB_WorkCommission_ID == 0)
            {
                //	SetName (null);
                //	SetVAB_BusinessPartner_ID (0);
                //	SetVAB_Charge_ID (0);
                //	SetVAB_WorkCommission_ID (0);
                //	SetVAB_Currency_ID (0);
                //
                SetDocBasisType(DOCBASISTYPE_Invoice);  // I
                SetFrequencyType(FREQUENCYTYPE_Monthly);    // M
                SetListDetails(false);
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">data row</param>
        /// <param name="trxName">transaction</param>
        public MVABWorkCommission(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        /// <summary>
        /// Get Lines
        /// </summary>
        /// <returns>array of lines</returns>
        public MVABWorkCommissionLine[] GetLines()
        {
            String sql = "SELECT * FROM VAB_WorkCommissionLine WHERE VAB_WorkCommission_ID=@comid AND IsActive='Y' ORDER BY VAM_Product_ID, VAM_ProductCategory_ID,VAB_BusinessPartner_ID,VAB_BPart_Category_ID,VAF_Org_ID,VAB_SalesRegionState_ID";
            List<MVABWorkCommissionLine> list = new List<MVABWorkCommissionLine>();
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@comid", GetVAB_WorkCommission_ID());

                DataSet ds = DataBase.DB.ExecuteDataset(sql, param, Get_TrxName());
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        list.Add(new MVABWorkCommissionLine(GetCtx(), dr, Get_TrxName()));
                    }
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }

            //	Convert
            MVABWorkCommissionLine[] retValue = new MVABWorkCommissionLine[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Set Date Last Run
        /// </summary>
        /// <param name="dateLastRun">date</param>
        public new void SetDateLastRun(DateTime? dateLastRun)
        {
            if (dateLastRun != null)
                base.SetDateLastRun(dateLastRun.Value);
        }

        /// <summary>
        /// Copy Lines From other Commission
        /// </summary>
        /// <param name="otherCom">commission</param>
        /// <returns>number of lines copied</returns>
        public int CopyLinesFrom(MVABWorkCommission otherCom)
        {
            if (otherCom == null)
                return 0;
            MVABWorkCommissionLine[] fromLines = otherCom.GetLines();
            int count = 0;
            for (int i = 0; i < fromLines.Length; i++)
            {
                MVABWorkCommissionLine line = new MVABWorkCommissionLine(GetCtx(), 0, Get_TrxName());
                PO.CopyValues(fromLines[i], line, GetVAF_Client_ID(), GetVAF_Org_ID());
                line.Set_ValueNoCheck("VAB_WorkCommissionLine_ID", null);	//	new
                line.SetVAB_WorkCommission_ID(GetVAB_WorkCommission_ID());
                if (line.Save())
                    count++;
            }
            if (fromLines.Length != count)
                log.Log(Level.SEVERE, "Line difference - From=" + fromLines.Length + " <> Saved=" + count);
            return count;
        }


    }
}