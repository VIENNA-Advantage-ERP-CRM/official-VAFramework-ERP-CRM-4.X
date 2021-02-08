﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
////using VAdvantage.Common;
using ViennaAdvantage.Process;
//////using System.Windows.Forms;
//using ViennaAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;
using VAdvantage.ProcessEngine;


using VAdvantage.Process;

namespace ViennaAdvantage.Process
{
    public class CreateAPInvoice : SvrProcess
    {
        #region Private Variable
        private int _VAB_BusinessPartner_ID = 0;
        private DateTime? _DateFrom = null;
        private DateTime? _DateTo = null;
        //private int _noInvoices = 0;
        string sql = "";
        //string msg = "";
        string fromDate = "";
        string toDate = "";
        List<int> VAB_BusinessPartner_ID = new List<int>();
        List<int> VAM_Product_ID = new List<int>();
        Dictionary<int, int> BPInvoice = new Dictionary<int, int>();
        private List<int> invoices = new List<int>();
        #endregion

        /// <summary>
        /// Prepare - e.g., get Parameters.
        /// </summary>
        protected override void Prepare()
        {
            ProcessInfoParameter[] para = GetParameter();
            for (int i = 0; i < para.Length; i++)
            {
                String name = para[i].GetParameterName();
                if (para[i].GetParameter() == null)
                {
                    ;
                }
                else if (name.Equals("VAB_BusinessPartner_ID"))
                {
                    _VAB_BusinessPartner_ID = para[i].GetParameterAsInt();
                }
                else if (name.Equals("FromDate"))
                {
                    _DateFrom = (DateTime?)para[i].GetParameter();
                    // _DateTo = (DateTime?)para[i].GetParameter_To();
                }
                else if (name.Equals("ToDate"))
                {
                    // _DateFrom = (DateTime?)para[i].GetParameter();
                    _DateTo = (DateTime?)para[i].GetParameter();
                }
                else
                {
                    log.Log(Level.SEVERE, "Unknown Parameter: " + name);
                }
            }
        }

        /// <summary>
        /// Generate Shipments
        /// </summary>
        /// <returns>info</returns>
        protected override String DoIt()
        {
            //string sqlSelect = "select * from VAS_ExpenseReportline where processed = 'Y' and (APInvoice = 'Y' or expenseinvoice = 'Y') and VAB_Invoice_ID is null";
            //string sqlSelect = "select * from VAS_ExpenseReportline where APInvoice = 'Y' and VAB_Invoice_ID is null";
            string sqlSelect = "SELECT res.VAB_BusinessPartner_ID, tl.VAS_ExpenseReportline_ID FROM VAS_ExpenseReportline tl inner join VAS_Resource res on (res.VAS_Resource_ID = tl.VAS_Resource_ID) "
                           + " WHERE tl.processed = 'Y' AND (tl.APInvoice = 'Y' OR tl.expenseinvoice = 'Y') AND tl.VAB_Invoice_ID IS NULL";
            StringBuilder sqlWhere = new StringBuilder();
            if (_VAB_BusinessPartner_ID != 0)
            {
                sqlWhere.Append(" AND res.VAB_BusinessPartner_ID = " + _VAB_BusinessPartner_ID);
            }

            if (_DateFrom != null && _DateTo != null)
            {
                fromDate = _DateFrom.Value.ToString("dd-MMM-yyyy");
                toDate = _DateTo.Value.ToString("dd-MMM-yyyy");
                sqlWhere.Append(" AND tl.dateexpense between '" + fromDate + "' and '" + toDate + "'");
            }
            else if (_DateFrom != null && _DateTo == null)
            {
                fromDate = _DateFrom.Value.ToString("dd-MMM-yyyy");
                sqlWhere.Append(" AND tl.dateexpense > '" + fromDate + "'");
            }
            else if (_DateFrom == null && _DateTo != null)
            {
                toDate = _DateTo.Value.ToString("dd-MMM-yyyy");
                sqlWhere.Append(" AND tl.dateexpense < '" + toDate + "'");
            }

            if (sqlWhere.Length > 0)
            {
                sql = sqlSelect + sqlWhere.ToString();
            }
            else
            {
                sql = sqlSelect;
            }

            sql = sql + " order by res.VAB_BusinessPartner_id";

            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql, null, null);
                while (idr.Read())
                {
                    if (VAB_BusinessPartner_ID.Contains(Util.GetValueOfInt(idr["VAB_BusinessPartner_ID"])))
                    {

                    }
                    else
                    {
                        VAB_BusinessPartner_ID.Add(Util.GetValueOfInt(idr["VAB_BusinessPartner_ID"]));
                        VAdvantage.Model.X_VAS_ExpenseReportLine tLine = new VAdvantage.Model.X_VAS_ExpenseReportLine(GetCtx(), Util.GetValueOfInt(idr["VAS_ExpenseReportline_id"]), null);
                        VAdvantage.Model.X_VAS_ExpenseReport tExp = new VAdvantage.Model.X_VAS_ExpenseReport(GetCtx(), Util.GetValueOfInt(tLine.GetVAS_ExpenseReport_ID()), null);
                        int VAB_Invoice_ID = GenerateInvoice(tLine, tExp);

                        BPInvoice.Add(Util.GetValueOfInt(idr["VAB_BusinessPartner_ID"]), VAB_Invoice_ID);
                        invoices.Add(VAB_Invoice_ID);
                    }
                }
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }

                if (BPInvoice.Count > 0)
                {
                    foreach (KeyValuePair<int, int> pair in BPInvoice)
                    {
                        sqlWhere = new StringBuilder();
                        sqlWhere.Append(" AND res.VAB_BusinessPartner_ID = " + Util.GetValueOfInt(pair.Key));
                        if (_DateFrom != null && _DateTo != null)
                        {
                            fromDate = _DateFrom.Value.ToString("dd-MMM-yyyy");
                            toDate = _DateTo.Value.ToString("dd-MMM-yyyy");
                            sqlWhere.Append(" AND tl.dateexpense between '" + fromDate + "' and '" + toDate + "'");
                        }
                        else if (_DateFrom != null && _DateTo == null)
                        {
                            fromDate = _DateFrom.Value.ToString("dd-MMM-yyyy");
                            sqlWhere.Append(" AND tl.dateexpense > '" + fromDate + "'");
                        }
                        else if (_DateFrom == null && _DateTo != null)
                        {
                            toDate = _DateTo.Value.ToString("dd-MMM-yyyy");
                            sqlWhere.Append(" AND tl.dateexpense < '" + toDate + "'");
                        }
                        sql = "";
                        if (sqlWhere.Length > 0)
                        {
                            sql = sqlSelect + sqlWhere.ToString();
                        }

                        DataSet ds = DB.ExecuteDataset(sql, null, null);
                        if (ds != null)
                        {
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                for (int j = 0; j < ds.Tables[0].Rows.Count; j++)
                                {
                                    CreateLine(Util.GetValueOfInt(ds.Tables[0].Rows[j]["VAS_ExpenseReportLine_ID"]), Util.GetValueOfInt(pair.Value));
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }

            string docNo = "";
            for (int i = 0; i < invoices.Count; i++)
            {
                VAdvantage.Model.MInvoice inv = new VAdvantage.Model.MInvoice(GetCtx(), Util.GetValueOfInt(invoices[i]), null);
                docNo = docNo + ", " + inv.GetDocumentNo();
            }
            if (docNo != "")
            {
                docNo = docNo.Remove(0, 2);
            }
            return Msg.GetMsg(GetCtx(), "InvoicesCreated" + " : " + docNo);

            //return Msg.GetMsg(GetCtx(),"ProcessCompleted");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        private void CreateLine(int VAS_ExpenseReportLine_ID, int VAB_Invoice_ID)
        {
            VAdvantage.Model.X_VAS_ExpenseReportLine tLine = new VAdvantage.Model.X_VAS_ExpenseReportLine(GetCtx(), VAS_ExpenseReportLine_ID, null);
            VAdvantage.Model.X_VAS_ExpenseReport tExp = new VAdvantage.Model.X_VAS_ExpenseReport(GetCtx(), Util.GetValueOfInt(tLine.GetVAS_ExpenseReport_ID()), null);

            if (tLine.IsAPInvoice())
            {
                if (Util.GetValueOfInt(tLine.GetVAM_Product_ID()) != 0)
                {
                    sql = "select max(line) from VAB_InvoiceLine where VAB_Invoice_id = " + VAB_Invoice_ID;
                    int lineNo = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));


                    sql = "select VAB_InvoiceLine_ID from VAB_InvoiceLine where VAB_Invoice_ID = " + VAB_Invoice_ID + " and VAM_Product_id = " + tLine.GetVAM_Product_ID();
                    int VAB_InvoiceLine_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    if (VAB_InvoiceLine_ID != 0)
                    {

                        sql = "select VAB_UOM_id from VAB_Orderline where VAB_Orderline_id = " + tLine.GetVAB_OrderLine_ID();
                        int VAB_UOM_IDTo = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        sql = "select VAB_UOM_id from VAM_Product where VAM_Product_id = " + tLine.GetVAM_Product_ID();
                        int VAB_UOM_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        Decimal? qty = 0;
                        if (VAB_UOM_IDTo != 0 && VAB_UOM_ID != 0)
                        {
                            //qty = MUOMConversion.Convert(VAB_UOM_ID, VAB_UOM_IDTo, tLine.GetAPApprovedHrs(), true);
                            qty = VAdvantage.Model.MUOMConversion.ConvertProductTo(GetCtx(), tLine.GetVAM_Product_ID(), VAB_UOM_IDTo, tLine.GetAPApprovedHrs());
                        }

                        VAdvantage.Model.MInvoiceLine iLine = new VAdvantage.Model.MInvoiceLine(GetCtx(), VAB_InvoiceLine_ID, null);
                        iLine.SetQtyEntered(Decimal.Add(iLine.GetQtyEntered(), qty.Value));
                        iLine.SetQtyInvoiced(Decimal.Add(iLine.GetQtyInvoiced(), qty.Value));
                        // iLine.SetTaxAmt(Decimal.Add(iLine.GetTaxAmt(), tLine.GetTaxAmt()));
                        iLine.SetLineNetAmt(Decimal.Multiply(iLine.GetQtyEntered(), iLine.GetPriceEntered()));
                        iLine.SetLineTotalAmt(Decimal.Add(iLine.GetLineNetAmt(), iLine.GetTaxAmt()));
                        if (!iLine.Save())
                        {

                        }
                    }
                    else
                    {
                        lineNo = lineNo + 10;
                        Decimal? price = 0;
                        //sql = "select VAS_Resource_ID from VAS_Resource where VAB_BusinessPartner_id = " + tLine.GetVAB_BusinessPartner_ID()
                        //    + " and VAF_UserContact_id = " + tExp.GetVAF_UserContact_ID() + " and VAM_Product_id = " + tLine.GetVAM_Product_ID() + " and isactive = 'Y'";
                        //sql = "select VAS_Resource_ID from  where VAB_BusinessPartner_id = " + tLine.GetVAB_BusinessPartner_ID();



                        sql = "select VAB_UOM_id from VAB_Orderline where VAB_Orderline_id = " + tLine.GetVAB_OrderLine_ID();
                        int VAB_UOM_IDTo = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        sql = "select VAB_UOM_id from VAM_Product where VAM_Product_id = " + tLine.GetVAM_Product_ID();
                        int VAB_UOM_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        Decimal? qty = 0;

                        //qty = MUOMConversion.Convert(VAB_UOM_ID, VAB_UOM_IDTo, tLine.GetAPApprovedHrs(), true);

                        qty = VAdvantage.Model.MUOMConversion.ConvertProductTo(GetCtx(), tLine.GetVAM_Product_ID(), VAB_UOM_IDTo, tLine.GetAPApprovedHrs());
                        //Decimal? qty2 = MUOMConversion.ConvertProductFrom(GetCtx(), tLine.GetVAM_Product_ID(), VAB_UOM_IDTo, tLine.GetAPApprovedHrs());
                        //Decimal? qty3 = MUOMConversion.GetProductRateTo(GetCtx(), tLine.GetVAM_Product_ID(), VAB_UOM_IDTo);
                        //Decimal? qty4 = MUOMConversion.GetProductRateFrom(GetCtx(), tLine.GetVAM_Product_ID(), VAB_UOM_ID);


                        int VAS_Resource_ID = Util.GetValueOfInt(tLine.GetS_Resource_ID());

                        if (VAS_Resource_ID != 0)
                        {
                            sql = "select HourlyRate from VAS_Resource where VAS_Resource_ID = " + VAS_Resource_ID;
                            price = Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, null));
                            price = VAdvantage.Model.MUOMConversion.ConvertProductFrom(GetCtx(), tLine.GetVAM_Product_ID(), VAB_UOM_IDTo, price.Value);
                        }
                        else
                        {
                            sql = "select VAM_PriceListVersion_id from VAM_PriceListVersion where isactive = 'Y' and VAM_PriceList_id = " + tExp.GetVAM_PriceList_ID();
                            int plv_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (plv_ID != 0)
                            {
                                sql = "select pricestd from VAM_ProductPrice where VAM_PriceListVersion_id = " + plv_ID + " and VAM_Product_ID = " + tLine.GetVAM_Product_ID();
                                price = Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, null));
                            }
                        }



                        VAdvantage.Model.MInvoiceLine iLine = new VAdvantage.Model.MInvoiceLine(GetCtx(), 0, null);
                        iLine.SetVAF_Client_ID(GetCtx().GetVAF_Client_ID());
                        iLine.SetVAF_Org_ID(GetCtx().GetVAF_Org_ID());
                        iLine.SetVAB_Invoice_ID(VAB_Invoice_ID);
                        iLine.SetVAB_TaxRate_ID(tLine.GetVAB_TaxRate_ID());
                        iLine.SetVAB_UOM_ID(tLine.GetVAB_UOM_ID());
                        iLine.SetDescription(tLine.GetDescription());
                        iLine.SetVAM_Product_ID(tLine.GetVAM_Product_ID());
                        iLine.SetQtyEntered(qty);
                        iLine.SetQtyInvoiced(qty);
                        iLine.SetPriceEntered(price);
                        iLine.SetPriceList(price);
                        iLine.SetPriceLimit(price);
                        iLine.SetPriceActual(price);

                        //  iLine.SetTaxAmt(tLine.GetTaxAmt());
                        iLine.SetLineNetAmt(Decimal.Multiply(iLine.GetQtyEntered(), iLine.GetPriceEntered()));
                        iLine.SetLineTotalAmt(Decimal.Add(iLine.GetLineNetAmt(), iLine.GetTaxAmt()));
                        iLine.SetLine(lineNo);
                        if (!iLine.Save())
                        {

                        }
                    }
                    sql = "update VAS_ExpenseReportLine set VAB_Invoice_ID = " + VAB_Invoice_ID + " where VAS_ExpenseReportLine_ID = " + VAS_ExpenseReportLine_ID;
                    int res = Util.GetValueOfInt(DB.ExecuteQuery(sql, null, null));
                }
            }

            if (tLine.IsExpenseInvoice())
            {
                if (Util.GetValueOfInt(tLine.GetVAB_Charge_ID()) != 0)
                {
                    sql = "select max(line) from VAB_InvoiceLine where VAB_Invoice_id = " + VAB_Invoice_ID;
                    int lineNo = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                    sql = "select VAB_InvoiceLine_ID from VAB_InvoiceLine where VAB_Invoice_ID = " + VAB_Invoice_ID + " and VAB_Charge_ID = " + tLine.GetVAB_Charge_ID() + " and VAB_TaxRate_id = " + tLine.GetVAB_TaxRate_ID();
                    int VAB_InvoiceLine_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    if (VAB_InvoiceLine_ID != 0)
                    {
                        VAdvantage.Model.MInvoiceLine iLine = new VAdvantage.Model.MInvoiceLine(GetCtx(), VAB_InvoiceLine_ID, null);
                        iLine.SetPriceEntered(Decimal.Add(tLine.GetApprovedExpenseAmt(), iLine.GetPriceEntered()));
                        iLine.SetPriceActual(Decimal.Add(tLine.GetApprovedExpenseAmt(), iLine.GetPriceActual()));
                        iLine.SetPriceLimit(Decimal.Add(tLine.GetApprovedExpenseAmt(), iLine.GetPriceLimit()));
                        iLine.SetPriceList(Decimal.Add(tLine.GetApprovedExpenseAmt(), iLine.GetPriceList()));
                        // iLine.SetTaxAmt(Decimal.Add(tLine.GetTaxAmt(), iLine.GetTaxAmt()));
                        //  iLine.SetLineNetAmt(Decimal.Multiply(iLine.GetQtyEntered(), iLine.GetPriceEntered()));
                        //  iLine.SetLineTotalAmt(Decimal.Add(iLine.GetLineNetAmt(), iLine.GetTaxAmt()));
                        if (!iLine.Save())
                        {

                        }
                    }
                    else
                    {
                        lineNo = lineNo + 10;
                        VAdvantage.Model.MInvoiceLine iLine = new VAdvantage.Model.MInvoiceLine(GetCtx(), 0, null);
                        iLine.SetVAF_Client_ID(GetCtx().GetVAF_Client_ID());
                        iLine.SetVAF_Org_ID(GetCtx().GetVAF_Org_ID());
                        iLine.SetVAB_Invoice_ID(VAB_Invoice_ID);
                        iLine.SetVAB_TaxRate_ID(tLine.GetVAB_TaxRate_ID());
                        iLine.SetVAB_UOM_ID(tLine.GetVAB_UOM_ID());
                        iLine.SetDescription(tLine.GetDescription());
                        iLine.SetVAB_Charge_ID(tLine.GetVAB_Charge_ID());
                        iLine.SetQtyEntered(Decimal.One);
                        iLine.SetQtyInvoiced(Decimal.One);
                        iLine.SetPriceEntered(tLine.GetApprovedExpenseAmt());
                        iLine.SetPriceActual(tLine.GetApprovedExpenseAmt());
                        iLine.SetPriceLimit(tLine.GetApprovedExpenseAmt());
                        iLine.SetPriceList(tLine.GetApprovedExpenseAmt());
                        //   iLine.SetTaxAmt(tLine.GetTaxAmt());
                        //   iLine.SetLineNetAmt(Decimal.Multiply(iLine.GetQtyEntered(), iLine.GetPriceEntered()));
                        //  iLine.SetLineTotalAmt(Decimal.Add(iLine.GetLineNetAmt(), iLine.GetTaxAmt()));
                        iLine.SetLine(lineNo);
                        if (!iLine.Save())
                        {

                        }

                    }

                    sql = "update VAS_ExpenseReportLine set VAB_Invoice_ID = " + VAB_Invoice_ID + " where VAS_ExpenseReportLine_ID = " + VAS_ExpenseReportLine_ID;
                    int res = Util.GetValueOfInt(DB.ExecuteQuery(sql, null, null));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tLine"></param>
        private int GenerateInvoice(VAdvantage.Model.X_VAS_ExpenseReportLine tLine, VAdvantage.Model.X_VAS_ExpenseReport tExp)
        {
            int VAB_PaymentTerm_ID = 0;
            VAdvantage.Model.X_VAB_Order ord = null;
            if (tLine.GetVAB_Order_ID() != 0)
            {
                ord = new VAdvantage.Model.X_VAB_Order(GetCtx(), tLine.GetVAB_Order_ID(), null);
            }

            //sql = "select VAS_Resource_ID from VAS_Resource where VAB_BusinessPartner_id = " + tLine.GetVAB_BusinessPartner_ID()
            //               + " and VAF_UserContact_id = " + tExp.GetVAF_UserContact_ID() + " and VAM_Product_id = " + tLine.GetVAM_Product_ID() + " and isactive = 'Y'";
            //int VAS_Resource_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            int VAS_Resource_ID = Util.GetValueOfInt(tLine.GetS_Resource_ID());
            int VAB_Currency_ID = 0;
            int VAB_BusinessPartner_ID = 0;
            if (VAS_Resource_ID != 0)
            {
                sql = "select VAB_Currency_ID from VAS_Resource where VAS_Resource_ID = " + VAS_Resource_ID;
                VAB_Currency_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                sql = "select VAB_BusinessPartner_ID from VAS_Resource where VAS_Resource_ID = " + VAS_Resource_ID;
                VAB_BusinessPartner_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            }
            else
            {
                sql = "select VAB_Currency_ID from VAM_PriceList where isactive = 'Y' and VAM_PriceList_id = " + tExp.GetVAM_PriceList_ID();
                VAB_Currency_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            }

            sql = "select VAB_DocTypes_ID from VAB_DocTypes where docbasetype = 'API' and vaf_client_id = " + GetCtx().GetVAF_Client_ID();
            int VAB_DocTypes_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

            sql = "select VAB_BPart_Location_ID from VAB_BPart_Location where VAB_BusinessPartner_ID = " + VAB_BusinessPartner_ID;
            int VAB_BPart_Location_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

            VAdvantage.Model.X_VAB_Invoice inv = new VAdvantage.Model.X_VAB_Invoice(GetCtx(), 0, null);
            inv.SetVAF_Client_ID(GetCtx().GetVAF_Client_ID());
            inv.SetVAF_Org_ID(GetCtx().GetVAF_Org_ID());
            inv.SetVAF_UserContact_ID(tExp.GetVAF_UserContact_ID());
            inv.SetVAB_BusinessPartner_ID(VAB_BusinessPartner_ID);
            inv.SetVAB_BPart_Location_ID(VAB_BPart_Location_ID);
            inv.SetVAB_Currency_ID(VAB_Currency_ID);
            inv.SetVAB_DocTypes_ID(VAB_DocTypes_ID);
            inv.SetVAB_DocTypesTarget_ID(VAB_DocTypes_ID);
            inv.SetDateInvoiced(System.DateTime.Now);
            inv.SetVAM_PriceList_ID(tExp.GetVAM_PriceList_ID());
            inv.SetDateAcct(System.DateTime.Now);
            inv.SetIsApproved(true);
            //inv.SetPaymentRule();

            inv.SetIsSOTrx(false);
            //inv.SetSalesRep_ID();
            inv.SetDocAction("CO");
            inv.SetDocStatus("DR");
            if (ord != null)
            {
                inv.SetVAB_PaymentTerm_ID(ord.GetVAB_PaymentTerm_ID());
                inv.SetSalesRep_ID(ord.GetSalesRep_ID());
                // inv.SetVAB_Order_ID(ord.GetVAB_Order_ID());
                // inv.SetVAM_PriceList_ID(ord.GetVAM_PriceList_ID());
            }
            else
            {
                sql = " select VAB_Paymentterm_id from VAB_BusinessPartner where VAB_BusinessPartner_id = " + tLine.GetVAB_BusinessPartner_ID();
                VAB_PaymentTerm_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                if (VAB_PaymentTerm_ID == 0)
                {
                    sql = "select VAB_Paymentterm_id from VAB_Paymentterm where isdefault = 'Y' and vaf_client_id= " + GetCtx().GetVAF_Client_ID();
                    VAB_PaymentTerm_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                }
                inv.SetVAB_PaymentTerm_ID(VAB_PaymentTerm_ID);
                inv.SetSalesRep_ID(tExp.GetSalesRep_ID());
            }

            if (!inv.Save())
            {
                log.SaveError("InvoiceNotSaved", "InvoiceNotSaved");
                return 0;
            }
            return inv.GetVAB_Invoice_ID();
        }



    }
}
