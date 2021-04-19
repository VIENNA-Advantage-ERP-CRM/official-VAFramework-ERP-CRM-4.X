﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using VAdvantage.DataBase;
using VAdvantage.Logging;
using VAdvantage.Model;
using VAdvantage.Utility;

namespace VIS.Models
{
    public class MPaymentModel
    {
        private static VLogger log = VLogger.GetVLogger(typeof(MPaymentModel));
        /// <summary>
        /// GetPayment
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetPayment(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int VAB_Payment_ID;
            //Assign parameter value
            VAB_Payment_ID = Util.GetValueOfInt(paramValue[0].ToString());
            MVABPayment payment = new MVABPayment(ctx, VAB_Payment_ID, null);
            Dictionary<string, string> result = new Dictionary<string, string>();
            result["VAB_Charge_ID"] = payment.GetVAB_Charge_ID().ToString();
            result["VAB_Invoice_ID"] = payment.GetVAB_Invoice_ID().ToString();
            result["VAB_Order_ID"] = payment.GetVAB_Order_ID().ToString();
            return result;
        }

        //Added by Bharat on 12/May/2017
        /// <summary>
        /// GetPayment Amount
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public decimal GetPayAmt(Ctx ctx, string fields)
        {
            int VAB_Payment_ID;
            //Assign parameter value
            VAB_Payment_ID = Util.GetValueOfInt(fields);
            MVABPayment payment = new MVABPayment(ctx, VAB_Payment_ID, null);
            return payment.GetPayAmt();
        }

        //Added by Bharat on 17/May/2017
        /// <summary>
        /// Get Invoice Data
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <param name="fields">Paramaters</param>
        /// <returns>Dictionary, Invoice Data</returns>
        public Dictionary<String, Object> GetInvoiceData(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int VAB_Invoice_ID = Util.GetValueOfInt(paramValue[0]);
            int VAB_PaymentSchedule_ID = Util.GetValueOfInt(paramValue[1]);
            DateTime? trxDate = Util.GetValueOfDateTime(paramValue[2]);
            Dictionary<String, Object> retDic = null;
            string sql = "";
            if (Env.IsModuleInstalled("VA009_"))
            {
                sql = "SELECT i.VAB_BusinessPartner_ID, i.VAB_Currency_ID, i.VAB_CurrencyType_ID, i.VAB_BPart_Location_Id,"
                    //+ " invoiceOpen(VAB_Invoice_ID, @param1) as invoiceOpen,"
                     + " NVL(p.DueAmt , 0) - NVL(p.VA009_PaidAmntInvce , 0) as invoiceOpen,"
                     + " invoiceDiscount(" + VAB_Invoice_ID + ",@param1," + VAB_PaymentSchedule_ID + ") as invoiceDiscount,"
                     + " i.IsSOTrx, i.IsInDispute, i.IsReturnTrx"
                     + " FROM VAB_Invoice i INNER JOIN VAB_sched_InvoicePayment p ON p.VAB_Invoice_ID = i.VAB_Invoice_ID"
                     + " WHERE i.VAB_Invoice_ID=" + VAB_Invoice_ID + " AND p.VAB_sched_InvoicePayment_ID=" + VAB_PaymentSchedule_ID;
            }
            else
            {
                sql = "SELECT i.VAB_BusinessPartner_ID, i.VAB_Currency_ID, i.VAB_CurrencyType_ID, i.VAB_BPart_Location_Id,"
                    //+ " invoiceOpen(VAB_Invoice_ID, @param1) as invoiceOpen,"
                    + " p.DueAmt as invoiceOpen,"
                    + " invoiceDiscount(" + VAB_Invoice_ID + ",@param1," + VAB_PaymentSchedule_ID + ") as invoiceDiscount,"
                    + " i.IsSOTrx, i.IsInDispute, i.IsReturnTrx"
                    + " FROM VAB_Invoice i INNER JOIN VAB_sched_InvoicePayment p ON p.VAB_Invoice_ID = i.VAB_Invoice_ID"
                    + " WHERE i.VAB_Invoice_ID=" + VAB_Invoice_ID + " AND p.VAB_sched_InvoicePayment_ID=" + VAB_PaymentSchedule_ID;
            }
            SqlParameter[] param = new SqlParameter[1];
            //param[0] = new SqlParameter("@param1", VAB_PaymentSchedule_ID);
            param[0] = new SqlParameter("@param1", trxDate);
            //param[2] = new SqlParameter("@param3", VAB_PaymentSchedule_ID);
            //param[3] = new SqlParameter("@param4", VAB_Invoice_ID);
            DataSet ds = DB.ExecuteDataset(sql, param, null);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                retDic = new Dictionary<string, object>();
                retDic["VAB_BusinessPartner_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_BusinessPartner_ID"]);
                retDic["VAB_BPart_Location_Id"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_BPart_Location_Id"]);
                retDic["VAB_Currency_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Currency_ID"]);

                // JID_1208: System should set Currency Type that is defined on Invoice.
                retDic["VAB_CurrencyType_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_CurrencyType_ID"]);
                retDic["invoiceOpen"] = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["invoiceOpen"]);
                retDic["invoiceDiscount"] = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["invoiceDiscount"]);
                retDic["IsSOTrx"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsSOTrx"]);
                retDic["IsInDispute"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsInDispute"]);
                retDic["IsReturnTrx"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsReturnTrx"]);
            }
            return retDic;
        }

        //Added by Bharat on 17/May/2017
        /// <summary>
        /// Get Business Partner OutStanding Amount
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public string GetOutStandingAmt(Ctx ctx, string fields)
        {
            string Amounts = "";
            StringBuilder sql = new StringBuilder();
            try
            {
                string[] paramValue = fields.Split(',');                
                bool countVA027 = Util.GetValueOfBool(paramValue[0]);
                int bp_BusinessPartner = Util.GetValueOfInt(paramValue[1]);
                DateTime? asOnDate = Util.GetValueOfDateTime(paramValue[2]);
                int Client_ID = ctx.GetVAF_Client_ID();
                sql.Clear();
                sql.Append(@"SELECT LTRIM(MAX(SYS_CONNECT_BY_PATH( ConvertPrice, ',')),',') amounts FROM " +
                   "(SELECT ConvertPrice, ROW_NUMBER () OVER (ORDER BY ConvertPrice ) RN, COUNT (*) OVER () CNT FROM " +
                   "(SELECT iso_code || ':' || SUM(OpenAmt) AS ConvertPrice " +
                   "FROM ((SELECT c.iso_code,  invoiceOpen (i.VAB_Invoice_ID,i.VAB_sched_InvoicePayment_ID)*MultiplierAP AS OpenAmt FROM VAB_Invoice_v i " +
                   "LEFT JOIN VAB_Currency C ON C.VAB_Currency_ID=i.VAB_Currency_ID " +
                   "LEFT JOIN VAB_sched_InvoicePayment IPS ON IPS.VAB_Invoice_ID = i.VAB_Invoice_ID " +
                   "WHERE i.docstatus IN ('CO','CL') AND i.IsActive ='Y' AND i.ispaid ='N' " +
                   "AND ips.duedate IS NOT NULL AND NVL(ips.dueamt,0)!=0 AND i.VAB_BusinessPartner_id = " + bp_BusinessPartner + " AND i.VAF_Client_ID=" + Client_ID +
                    //" AND TRUNC(ips.duedate) <= (CASE WHEN  TRUNC(@param1) > TRUNC(sysdate) THEN TRUNC(sysdate) ELSE TRUNC(@param2) END ) " +
                   " AND TRUNC(ips.duedate) <= TRUNC(@param1) ) " +
                   "UNION SELECT c.iso_code, paymentAvailable(p.VAB_Payment_ID)*p.MultiplierAP*-1 AS OpenAmt " +
                   "FROM VAB_Payment_V p LEFT JOIN VAB_Currency C ON C.VAB_Currency_ID=p.VAB_Currency_ID " +
                   "LEFT JOIN VAB_Payment pay ON (p.VAB_Payment_id   =pay.VAB_Payment_ID) WHERE p.IsAllocated  ='N' " +
                   " AND p.VAB_BUSINESSPARTNER_ID = " + bp_BusinessPartner + " AND p.DocStatus     IN ('CO','CL') " + " AND p.VAF_Client_ID=" + Client_ID +
                    //" AND TRUNC(pay.DateTrx) <= ( CASE WHEN TRUNC(@param3) > TRUNC(sysdate) THEN TRUNC(sysdate) ELSE TRUNC(@param4) END) " +
                   " AND TRUNC(pay.DateTrx) <= TRUNC(@param2) " +
                   ") GROUP BY iso_code ) ) WHERE RN = CNT START WITH RN = 1 CONNECT BY RN = PRIOR RN + 1 ");
                SqlParameter[] param = new SqlParameter[2];
                param[0] = new SqlParameter("@param1", asOnDate);
                param[1] = new SqlParameter("@param2", asOnDate);
                //param[2] = new SqlParameter("@param3", asOnDate);
                //param[3] = new SqlParameter("@param4", asOnDate);
                Amounts = Util.GetValueOfString(DB.ExecuteScalar(sql.ToString(), param, null));
                if (countVA027)
                {
                    sql.Clear();
                    sql.Append(@"SELECT LTRIM(MAX(SYS_CONNECT_BY_PATH( ConvertPrice, ',')),',') amounts
                    FROM (SELECT ConvertPrice, ROW_NUMBER () OVER (ORDER BY ConvertPrice ) RN,
                    COUNT (*) OVER () CNT FROM  
                    (SELECT iso_code|| ':'|| ROUND(SUM(PdcDue),2) AS ConvertPrice 
                    FROM (SELECT c.iso_code, CASE WHEN (pdc.VA027_MultiCheque = 'Y') THEN chk.VA027_ChequeAmount 
                    ELSE pdc.VA027_PayAmt END AS PdcDue 
                    FROM VA027_PostDatedCheck pdc LEFT JOIN VA027_ChequeDetails chk 
                    ON chk.VA027_PostDatedCheck_ID = pdc.VA027_PostDatedCheck_ID 
                    INNER JOIN VAB_Currency C ON C.VAB_Currency_ID=PDC.VAB_Currency_ID 
                    INNER JOIN VAB_DocTypes doc ON doc.VAB_DocTypes_ID = pdc.VAB_DocTypes_ID
                    WHERE pdc.IsActive ='Y' AND doc.DocBaseType = 'PDR'
                    AND pdc.DocStatus = 'CO' AND pdc.VA027_PAYMENTGENERATED ='N' AND pdc.VA027_PaymentStatus !=3
                    AND (CASE WHEN pdc.VA027_MultiCheque = 'Y' THEN TRUNC(chk.VA027_CheckDate)
                    ELSE TRUNC(pdc.VA027_CheckDate) END) <= TRUNC(@param1) AND PDC.VAB_BusinessPartner_id = " + bp_BusinessPartner + " AND PDC.VAF_Client_ID=" + Client_ID +
                        @" UNION SELECT c.iso_code, CASE WHEN (pdc.VA027_MultiCheque = 'Y') THEN chk.VA027_ChequeAmount*-1 
                    ELSE pdc.VA027_PayAmt*-1 END AS PdcDue 
                    FROM VA027_PostDatedCheck pdc LEFT JOIN VA027_ChequeDetails chk 
                    ON chk.VA027_PostDatedCheck_ID = pdc.VA027_PostDatedCheck_ID 
                    INNER JOIN VAB_Currency C ON C.VAB_Currency_ID=pdc.VAB_Currency_ID
                    INNER JOIN VAB_DocTypes doc ON doc.VAB_DocTypes_ID = pdc.VAB_DocTypes_ID 
                    WHERE pdc.IsActive ='Y' AND doc.DocBaseType = 'PDP' 
                    AND pdc.VA027_PAYMENTGENERATED ='N' AND pdc.VA027_PaymentStatus !=3 
                    AND (CASE WHEN pdc.VA027_MultiCheque = 'Y' THEN TRUNC(chk.VA027_CheckDate)
                    ELSE TRUNC(pdc.VA027_CheckDate) END) <= TRUNC(@param2) AND pdc.VAB_BusinessPartner_id = " + bp_BusinessPartner + " AND PDC.VAF_Client_ID=" + Client_ID +
                        @") GROUP BY iso_code )) WHERE RN = CNT START WITH RN = 1 CONNECT BY RN = PRIOR RN + 1");
                    SqlParameter[] param1 = new SqlParameter[2];
                    param1[0] = new SqlParameter("@param1", asOnDate);
                    param1[1] = new SqlParameter("@param2", asOnDate);
                    Amounts += ' ' + "PDC-:" + Util.GetValueOfString(DB.ExecuteScalar(sql.ToString(), param1, null));
                }
            }
            catch (Exception ex)
            {
                log.Log(Level.SEVERE, sql.ToString(), ex);
            }
            return Amounts;
        }

        //Added by Bharat on 18/May/2017
        /// <summary>
        /// Get Order Data
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<String, Object> GetOrderData(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            bool countVA009 = Util.GetValueOfBool(paramValue[0]);
            int VAB_Order_ID = Util.GetValueOfInt(paramValue[1]);
            Dictionary<String, Object> retDic = null;
            string sql = "SELECT";
            if (countVA009)
            {
                sql += " VA009_PaymentMethod_ID,";
            }
            sql += " VAB_BusinessPartner_ID, VAB_Currency_ID, GrandTotal, VAB_BPart_Location_ID , VAB_CurrencyType_ID"
                + " FROM VAB_Order WHERE VAB_Order_ID = " + VAB_Order_ID;

            DataSet ds = DB.ExecuteDataset(sql, null, null);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                retDic = new Dictionary<string, object>();
                if (countVA009)
                {
                    retDic["VA009_PaymentMethod_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VA009_PaymentMethod_ID"]);
                }
                retDic["VAB_BusinessPartner_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_BusinessPartner_ID"]);
                retDic["VAB_Currency_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Currency_ID"]);
                retDic["GrandTotal"] = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["GrandTotal"]);
                retDic["VAB_BPart_Location_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_BPart_Location_ID"]);
                retDic["VAB_CurrencyType_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_CurrencyType_ID"]);
                //check weather it is PrePayment or not
                if (Util.GetValueOfString(DB.ExecuteScalar("SELECT DocSubTypeSO FROM VAB_Order o INNER JOIN VAB_DocTypes DT ON o.VAB_DocTypesTarget_ID = DT.VAB_DocTypes_ID WHERE o.IsActive='Y' AND  VAB_Order_ID = " + VAB_Order_ID, null, null)).Equals(X_VAB_DocTypes.DOCSUBTYPESO_PrepayOrder))
                {
                    retDic["IsPrePayOrder"] = true;
                }
                else
                {
                    retDic["IsPrePayOrder"] = false;
                }
            }
            return retDic;
        }

        /// <summary>
        /// Get Bank Account Currency 
        /// </summary>
        /// <param name="fields">Parameters</param>
        /// <returns>Currency</returns>
        public int GetBankAcctCurrency(string fields)
        {
            int Currency_ID = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAB_Currency_ID FROM VAB_Bank_Acct WHERE VAB_Bank_Acct_ID = " + Util.GetValueOfInt(fields)));
            return Currency_ID;
        }
    }
}