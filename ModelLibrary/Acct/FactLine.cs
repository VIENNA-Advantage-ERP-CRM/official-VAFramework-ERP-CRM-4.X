﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : FactLine
 * Purpose        : Accounting Fact Entry.
 * Class Used     : X_Actual_Acct_Detail
 * Chronological    Development
 * Raghunandan      19-Jan-2010
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;
using System.Data.SqlClient;
using VAdvantage.Acct;

namespace VAdvantage.Acct
{
    public sealed class FactLine : X_Actual_Acct_Detail
    {
        //Account					
        private MVABAccount _acct = null;
        // Accounting Schema		
        private MVABAccountBook _acctSchema = null;
        // Document Header			
        private Doc _doc = null;
        // Document Line 			
        private DocLine _docLine = null;
        // conversion Rate
        private Decimal _ConversionRate = Env.ZERO;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="VAF_TableView_ID"></param>
        /// <param name="Record_ID"></param>
        /// <param name="Line_ID"></param>
        /// <param name="trxName"></param>
        public FactLine(Ctx ctx, int VAF_TableView_ID, int Record_ID, int Line_ID, Trx trxName)
            : base(ctx, 0, trxName)
        {
            SetVAF_Client_ID(0);							//	do not derive
            SetVAF_Org_ID(0);							//	do not derive
            //
            SetAmtAcctCr(Env.ZERO);
            SetAmtAcctDr(Env.ZERO);
            SetAmtSourceCr(Env.ZERO);
            SetAmtSourceDr(Env.ZERO);
            SetVAF_TableView_ID(VAF_TableView_ID);
            SetRecord_ID(Record_ID);
            SetLine_ID(Line_ID);
        }

        /// <summary>
        /// Create Reversal (negate DR/CR) of the line
        /// </summary>
        /// <param name="description"> new description</param>
        /// <returns>reversal line</returns>
        public FactLine Reverse(String description)
        {
            FactLine reversal = new FactLine(GetCtx(), GetVAF_TableView_ID(), GetRecord_ID(), GetLine_ID(), Get_TrxName());
            reversal.SetClientOrg(this);	//	needs to be set explicitly
            reversal.SetDocumentInfo(_doc, _docLine);
            reversal.SetAccount(_acctSchema, _acct);
            reversal.SetPostingType(GetPostingType());
            reversal.SetAmtSource(GetVAB_Currency_ID(), Decimal.Negate(GetAmtSourceDr()), Decimal.Negate(GetAmtSourceCr()));
            reversal.Convert();
            reversal.SetDescription(description);
            return reversal;
        }

        /// <summary>
        /// Create Accrual (flip CR/DR) of the line
        /// </summary>
        /// <param name="description">new description</param>
        /// <returns>accrual line</returns>
        public FactLine Accrue(String description)
        {
            FactLine accrual = new FactLine(GetCtx(), GetVAF_TableView_ID(), GetRecord_ID(), GetLine_ID(), Get_TrxName());
            accrual.SetClientOrg(this);	//	needs to be set explicitly
            accrual.SetDocumentInfo(_doc, _docLine);
            accrual.SetAccount(_acctSchema, _acct);
            accrual.SetPostingType(GetPostingType());
            accrual.SetAmtSource(GetVAB_Currency_ID(), GetAmtSourceCr(), GetAmtSourceDr());
            accrual.Convert();
            accrual.SetDescription(description);
            return accrual;
        }

        /// <summary>
        /// Set Account Info
        /// </summary>
        /// <param name="acctSchema">account schema</param>
        /// <param name="acct">account</param>
        public void SetAccount(MVABAccountBook acctSchema, MVABAccount acct)
        {
            _acctSchema = acctSchema;
            SetVAB_AccountBook_ID(acctSchema.GetVAB_AccountBook_ID());
            //
            _acct = acct;
            if (GetVAF_Client_ID() == 0)
            {
                SetVAF_Client_ID(_acct.GetVAF_Client_ID());
            }
            SetAccount_ID(_acct.GetAccount_ID());
            SetVAB_SubAcct_ID(_acct.GetVAB_SubAcct_ID());

            //	User Defined References
            MVABAccountBookElement ud1 = _acctSchema.GetAcctSchemaElement(
                    X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement1);
            if (ud1 != null && GetUserElement1_ID() <= 0)
            {
                String ColumnName1 = ud1.GetDisplayColumnName();
                if (ColumnName1 != null)
                {
                    int ID1 = 0;
                    if (_docLine != null)
                    {
                        ID1 = _docLine.GetValue(ColumnName1);
                    }
                    if (ID1 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID1 = _doc.GetValue(ColumnName1);
                    }
                    if (ID1 != 0)
                    {
                        SetUserElement1_ID(ID1);
                    }
                }
            }
            MVABAccountBookElement ud2 = _acctSchema.GetAcctSchemaElement(
                    X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement2);
            if (ud2 != null && GetUserElement2_ID() <= 0)
            {
                String ColumnName2 = ud2.GetDisplayColumnName();
                if (ColumnName2 != null)
                {
                    int ID2 = 0;
                    if (_docLine != null)
                    {
                        ID2 = _docLine.GetValue(ColumnName2);
                    }
                    if (ID2 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID2 = _doc.GetValue(ColumnName2);
                    }
                    if (ID2 != 0)
                    {
                        SetUserElement2_ID(ID2);
                    }
                }
            }

            #region change by mohit to consider userelements3 to userelements9 16/12/2016

            //user element 3
            MVABAccountBookElement ud3 = _acctSchema.GetAcctSchemaElement(
                   X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement3);
            if (ud3 != null && GetUserElement3_ID() <= 0)
            {
                String ColumnName3 = ud3.GetDisplayColumnName();
                if (ColumnName3 != null)
                {
                    int ID3 = 0;
                    if (_docLine != null)
                    {
                        ID3 = _docLine.GetValue(ColumnName3);
                    }
                    if (ID3 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID3 = _doc.GetValue(ColumnName3);
                    }
                    if (ID3 != 0)
                    {
                        SetUserElement3_ID(ID3);
                    }
                }
            }
            //user element 4
            MVABAccountBookElement ud4 = _acctSchema.GetAcctSchemaElement(
                   X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement4);
            if (ud4 != null && GetUserElement4_ID() <= 0)
            {
                String ColumnName4 = ud4.GetDisplayColumnName();
                if (ColumnName4 != null)
                {
                    int ID4 = 0;
                    if (_docLine != null)
                    {
                        ID4 = _docLine.GetValue(ColumnName4);
                    }
                    if (ID4 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID4 = _doc.GetValue(ColumnName4);
                    }
                    if (ID4 != 0)
                    {
                        SetUserElement4_ID(ID4);
                    }
                }
            }

            //user element 5
            MVABAccountBookElement ud5 = _acctSchema.GetAcctSchemaElement(
                   X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement5);
            if (ud5 != null && GetUserElement5_ID() <= 0)
            {
                String ColumnName5 = ud5.GetDisplayColumnName();
                if (ColumnName5 != null)
                {
                    int ID5 = 0;
                    if (_docLine != null)
                    {
                        ID5 = _docLine.GetValue(ColumnName5);
                    }
                    if (ID5 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID5 = _doc.GetValue(ColumnName5);
                    }
                    if (ID5 != 0)
                    {
                        SetUserElement5_ID(ID5);
                    }
                }
            }
            //user element 6
            MVABAccountBookElement ud6 = _acctSchema.GetAcctSchemaElement(
                 X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement6);
            if (ud6 != null && GetUserElement6_ID() <= 0)
            {
                String ColumnName6 = ud6.GetDisplayColumnName();
                if (ColumnName6 != null)
                {
                    int ID6 = 0;
                    if (_docLine != null)
                    {
                        ID6 = _docLine.GetValue(ColumnName6);
                    }
                    if (ID6 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID6 = _doc.GetValue(ColumnName6);
                    }
                    if (ID6 != 0)
                    {
                        SetUserElement6_ID(ID6);
                    }
                }
            }
            //user element 7
            MVABAccountBookElement ud7 = _acctSchema.GetAcctSchemaElement(
                 X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement7);
            if (ud7 != null && GetUserElement7_ID() <= 0)
            {
                String ColumnName7 = ud7.GetDisplayColumnName();
                if (ColumnName7 != null)
                {
                    int ID7 = 0;
                    if (_docLine != null)
                    {
                        ID7 = _docLine.GetValue(ColumnName7);
                    }
                    if (ID7 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID7 = _doc.GetValue(ColumnName7);
                    }
                    if (ID7 != 0)
                    {
                        SetUserElement7_ID(ID7);
                    }
                }
            }

            //user element 8
            MVABAccountBookElement ud8 = _acctSchema.GetAcctSchemaElement(
                 X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement8);
            if (ud8 != null && GetUserElement8_ID() <= 0)
            {
                String ColumnName8 = ud8.GetDisplayColumnName();
                if (ColumnName8 != null)
                {
                    int ID8 = 0;
                    if (_docLine != null)
                    {
                        ID8 = _docLine.GetValue(ColumnName8);
                    }
                    if (ID8 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID8 = _doc.GetValue(ColumnName8);
                    }
                    if (ID8 != 0)
                    {
                        SetUserElement8_ID(ID8);
                    }
                }
            }

            //user element 9
            MVABAccountBookElement ud9 = _acctSchema.GetAcctSchemaElement(
                 X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement9);
            if (ud9 != null && GetUserElement9_ID() <= 0)
            {
                String ColumnName9 = ud9.GetDisplayColumnName();
                if (ColumnName9 != null)
                {
                    int ID9 = 0;
                    if (_docLine != null)
                    {
                        ID9 = _docLine.GetValue(ColumnName9);
                    }
                    if (ID9 == 0)
                    {
                        if (_doc == null)
                        {
                            throw new ArgumentException("Document not set yet");
                        }
                        ID9 = _doc.GetValue(ColumnName9);
                    }
                    if (ID9 != 0)
                    {
                        SetUserElement9_ID(ID9);
                    }
                }
            }
            #endregion


        }

        /// <summary>
        /// Set Source Amounts
        /// </summary>
        /// <param name="VAB_Currency_ID">currency</param>
        /// <param name="AmtSourceDr">source amount dr</param>
        /// <param name="AmtSourceCr">source amount cr</param>
        /// <returns>true, if any if the amount is not zero</returns>
        public bool SetAmtSource(int VAB_Currency_ID, Decimal? AmtSourceDr, Decimal? AmtSourceCr)
        {
            SetVAB_Currency_ID(VAB_Currency_ID);
            if (AmtSourceDr != null)
            {
                SetAmtSourceDr(AmtSourceDr);
            }
            if (AmtSourceCr != null)
            {
                SetAmtSourceCr(AmtSourceCr);
            }
            //  one needs to be non zero
            if (GetAmtSourceDr().Equals(Env.ZERO) && GetAmtSourceCr().Equals(Env.ZERO))
            {
                return false;
            }
            //	Currency Precision
            int precision = MVABCurrency.GetStdPrecision(GetCtx(), VAB_Currency_ID);
            if (AmtSourceDr != null && Env.Scale(AmtSourceDr.Value) > precision)
            {
                Decimal AmtSourceDr1 = Decimal.Round(AmtSourceDr.Value, precision, MidpointRounding.AwayFromZero);
                log.Warning("Source DR Precision " + AmtSourceDr.Value + " -> " + AmtSourceDr1);
                SetAmtSourceDr(AmtSourceDr1);
            }
            if (AmtSourceCr != null && Env.Scale(AmtSourceCr.Value) > precision)
            {
                Decimal AmtSourceCr1 = Decimal.Round(AmtSourceCr.Value, precision, MidpointRounding.AwayFromZero);
                log.Warning("Source CR Precision " + AmtSourceCr + " -> " + AmtSourceCr1);
                SetAmtSourceCr(AmtSourceCr1);
            }
            return true;
        }

        /// <summary>
        /// Set Accounted Amounts (alternative: call convert)
        /// </summary>
        /// <param name="AmtAcctDr">acct amount dr</param>
        /// <param name="AmtAcctCr">acct amount cr</param>
        public void SetAmtAcct(Decimal? AmtAcctDr, Decimal? AmtAcctCr)
        {
            SetAmtAcctDr(AmtAcctDr.Value);
            SetAmtAcctCr(AmtAcctCr.Value);
        }

        /// <summary>
        /// Get Conversion Rate
        /// </summary>
        /// <returns>Conversion Rate Value</returns>
        public Decimal GetConversionRate()
        {
            return _ConversionRate;
        }

        /// <summary>
        /// Set Conversion Rate
        /// </summary>
        /// <param name="conversionRate">Conversion Rate Value</param>
        public void SetConversionRate(Decimal conversionRate)
        {
            _ConversionRate = conversionRate;
        }

        /// <summary>
        /// Set Document Info
        /// </summary>
        /// <param name="doc">document</param>
        /// <param name="docLine">doc line</param>
        public void SetDocumentInfo(Doc doc, DocLine docLine)
        {
            _doc = doc;
            _docLine = docLine;

            // Get Dimension which is to be posted respective to Accounting Schema
            Dictionary<string, string> acctSchemaElementRecord = new Dictionary<string, string>();
            acctSchemaElementRecord = GetDimenssion(GetVAB_AccountBook_ID());

            //	reset
            SetVAF_Org_ID(0);
            SetVAB_SalesRegionState_ID(0);
            //	Client
            if (GetVAF_Client_ID() == 0)
            {
                SetVAF_Client_ID(_doc.GetVAF_Client_ID());
            }
            //	Date Trx
            SetDateTrx(_doc.GetDateDoc());
            if (_docLine != null && _docLine.GetDateDoc() != null)
            {
                SetDateTrx(_docLine.GetDateDoc());
            }
            //	Date Acct
            SetDateAcct(_doc.GetDateAcct());
            if (_docLine != null && _docLine.GetDateAcct() != null)
            {
                SetDateAcct(_docLine.GetDateAcct());
            }
            //	Period, Tax
            if (_docLine != null && _docLine.GetVAB_YearPeriod_ID() != 0)
            {
                SetVAB_YearPeriod_ID(_docLine.GetVAB_YearPeriod_ID());
            }
            else
            {
                SetVAB_YearPeriod_ID(_doc.GetVAB_YearPeriod_ID());
            }
            // Set Line Table ID
            if (_docLine != null && _docLine.GetLineTable_ID() > 0 && Get_ColumnIndex("LineTable_ID") > -1)
            {
                Set_Value("LineTable_ID", _docLine.GetLineTable_ID());
            }
            else if (Get_ColumnIndex("LineTable_ID") > -1)
            {
                Set_Value("LineTable_ID", _doc.Get_Table_ID());
            }
            if (_docLine != null)
            {
                SetVAB_TaxRate_ID(_docLine.GetVAB_TaxRate_ID());
            }
            //	Description
            StringBuilder description = new StringBuilder();
            if (_docLine != null)
            {
                //description.Append(" #").Append(_docLine.GetLine());
                if (_docLine.GetDescription() != null)
                {
                    //description.Append(" (").Append(_docLine.GetDescription()).Append(")");
                    description.Append(_docLine.GetDescription());
                }
                else if (_doc.GetDescription() != null && _doc.GetDescription().Length > 0)
                {
                    description.Append(_doc.GetDocumentNo());
                    description.Append(" #").Append(_docLine.GetLine());
                    description.Append(" (").Append(_doc.GetDescription()).Append(")");
                }
                else
                {
                    // if on header - description not defined then post document No and line as description
                    description.Append(_doc.GetDocumentNo());
                    description.Append(" #").Append(_docLine.GetLine());
                }
            }
            else if (_doc.GetDescription() != null && _doc.GetDescription().Length > 0)
            {
                description.Append(" (").Append(_doc.GetDescription()).Append(")");
            }
            else
            {
                // if on header - description not defined then post document No as description
                description.Append(_doc.GetDocumentNo());
            }
            SetDescription(description.ToString());
            //	Journal Info
            SetVAGL_Budget_ID(_doc.GetVAGL_Budget_ID());
            SetVAGL_Group_ID(_doc.GetVAGL_Group_ID());

            //	Product
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_Product))
            {
                if (_docLine != null)
                {
                    SetVAM_Product_ID(_docLine.GetVAM_Product_ID());
                }
                if (GetVAM_Product_ID() == 0)
                {
                    SetVAM_Product_ID(_doc.GetVAM_Product_ID());
                }
            }
            //	UOM
            if (_docLine != null)
            {
                SetVAB_UOM_ID(_docLine.GetVAB_UOM_ID());
            }
            //	Qty
            if (Get_Value("Qty") == null)	// not previously set
            {
                SetQty(_doc.GetQty());	//	neg = outgoing
                if (_docLine != null)
                {
                    SetQty(_docLine.GetQty());
                }
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_LocationFrom))
            {
                //	Loc From (maybe set earlier)
                if (GetVAB_LocFrom_ID() == 0 && _docLine != null)
                {
                    SetVAB_LocFrom_ID(_docLine.GetVAB_LocFrom_ID());
                }
                if (GetVAB_LocFrom_ID() == 0)
                {
                    SetVAB_LocFrom_ID(_doc.GetVAB_LocFrom_ID());
                }
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_LocationTo))
            {
                //	Loc To (maybe set earlier)
                if (GetVAB_LocTo_ID() == 0 && _docLine != null)
                {
                    SetVAB_LocTo_ID(_docLine.GetVAB_LocTo_ID());
                }
                if (GetVAB_LocTo_ID() == 0)
                {
                    SetVAB_LocTo_ID(_doc.GetVAB_LocTo_ID());
                }
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_BPartner))
            {
                //	BPartner
                if (_docLine != null)
                {
                    SetVAB_BusinessPartner_ID(_docLine.GetVAB_BusinessPartner_ID());
                }
                if (GetVAB_BusinessPartner_ID() == 0)
                {
                    SetVAB_BusinessPartner_ID(_doc.GetVAB_BusinessPartner_ID());
                }
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_OrgTrx))
            {
                //	Sales Region from BPLocation/Sales Rep
                //	Trx Org
                if (_docLine != null)
                {
                    SetVAF_OrgTrx_ID(_docLine.GetVAF_OrgTrx_ID());
                }
                if (GetVAF_OrgTrx_ID() == 0)
                {
                    SetVAF_OrgTrx_ID(_doc.GetVAF_OrgTrx_ID());
                }
            }

            //	Set User Dimension
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement1) && _docLine != null)
            {
                SetUserElement1_ID(_docLine.GetUserElement1());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement2) && _docLine != null)
            {
                SetUserElement2_ID(_docLine.GetUserElement2());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement3) && _docLine != null)
            {
                SetUserElement3_ID(_docLine.GetUserElement3());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement4) && _docLine != null)
            {
                SetUserElement4_ID(_docLine.GetUserElement4());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement5) && _docLine != null)
            {
                SetUserElement5_ID(_docLine.GetUserElement5());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement6) && _docLine != null)
            {
                SetUserElement6_ID(_docLine.GetUserElement6());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement7) && _docLine != null)
            {
                SetUserElement7_ID(_docLine.GetUserElement7());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement8) && _docLine != null)
            {
                SetUserElement8_ID(_docLine.GetUserElement8());
            }
            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_UserElement9) && _docLine != null)
            {
                SetUserElement9_ID(_docLine.GetUserElement9());
            }
            if (_docLine != null && _docLine.GetConversionRate() > 0)
            {
                SetConversionRate(_docLine.GetConversionRate());
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_Project))
            {
                //	Project
                if (_docLine != null)
                {
                    SetVAB_Project_ID(_docLine.GetVAB_Project_ID());
                }
                if (GetVAB_Project_ID() == 0)
                {
                    SetVAB_Project_ID(_doc.GetVAB_Project_ID());
                }
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_Campaign))
            {
                //	Campaign
                if (_docLine != null)
                {
                    SetVAB_Promotion_ID(_docLine.GetVAB_Promotion_ID());
                }
                if (GetVAB_Promotion_ID() == 0)
                {
                    SetVAB_Promotion_ID(_doc.GetVAB_Promotion_ID());
                }
            }

            if (acctSchemaElementRecord.ContainsKey(MVABAccountBookElement.ELEMENTTYPE_Activity))
            {
                //	Activity
                if (_docLine != null)
                {
                    SetVAB_BillingCode_ID(_docLine.GetVAB_BillingCode_ID());
                }
                if (GetVAB_BillingCode_ID() == 0)
                {
                    SetVAB_BillingCode_ID(_doc.GetVAB_BillingCode_ID());
                }
            }

            //	User List 1
            if (_docLine != null)
            {
                SetUser1_ID(_docLine.GetUser1_ID());
            }
            if (GetUser1_ID() == 0)
            {
                SetUser1_ID(_doc.GetUser1_ID());
            }
            //	User List 2
            if (_docLine != null)
            {
                SetUser2_ID(_docLine.GetUser2_ID());
            }
            if (GetUser2_ID() == 0)
            {
                SetUser2_ID(_doc.GetUser2_ID());
            }
            //	References in setAccount
        }


        /// <summary>
        /// Get Dimension define on accounting schema
        /// </summary>
        /// <param name="as1">accounting schema</param>
        /// <returns>dimensions</returns>
        private Dictionary<string, string> GetDimenssion(int VAB_AccountBook_ID)
        {
            Dictionary<string, string> acctSchemaElementRecord = new Dictionary<string, string>();
            try
            {
                string sql = @"SELECT ase.vaf_client_id ,   ase.ElementType ,   ase.VAB_BillingCode_id ,   ase.VAB_BusinessPartner_id ,
                                     ase.VAB_Promotion_id ,   ase.VAB_Address_id ,   ase.VAB_Project_ID ,   ase.VAB_SalesRegionState_id ,
                                     ase.VAM_Product_id ,   ase.org_id ,   c.columnname
                             FROM VAB_AccountBook_Element ase LEFT JOIN vaf_column c ON ase.vaf_column_id   = c.vaf_column_id 
                             WHERE ase.VAB_AccountBook_ID = " + VAB_AccountBook_ID + " AND ase.IsActive = 'Y'";
                DataSet dsAcctSchemaElement = DB.ExecuteDataset(sql, null, null);
                if (dsAcctSchemaElement != null && dsAcctSchemaElement.Tables.Count > 0 && dsAcctSchemaElement.Tables[0].Rows.Count > 0)
                {
                    for (int ase = 0; ase < dsAcctSchemaElement.Tables[0].Rows.Count; ase++)
                    {
                        if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "AY")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAB_BillingCode_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "BP")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAB_BusinessPartner_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "LF" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "LT")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAB_Address_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "MC")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAB_Promotion_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "OT")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAF_OrgTrx_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "PJ")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAB_Project_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "PR")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAM_Product_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "SR")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = "VAB_SalesRegionState_ID";
                        }
                        else if (System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X1" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X2" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X3" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X4" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X5" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X6" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X7" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X8" ||
                                 System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"]) == "X9")
                        {
                            acctSchemaElementRecord[Util.GetValueOfString(dsAcctSchemaElement.Tables[0].Rows[ase]["ElementType"])] = System.Convert.ToString(dsAcctSchemaElement.Tables[0].Rows[ase]["columnname"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Log(Level.SEVERE, "GetDimenssion: error occure -> " + ex.ToString(), ex);
            }
            return acctSchemaElementRecord;
        }

        /// <summary>
        /// Get Document Line
        /// </summary>
        /// <returns>doc line</returns>
        public DocLine GetDocLine()
        {
            return _docLine;
        }

        /// <summary>
        /// Set Description
        /// </summary>
        /// <param name="description">description</param>
        public void AddDescription(String description)
        {
            String original = GetDescription();
            if (original == null || original.Trim().Length == 0)
            {
                base.SetDescription(description);
            }
            else
            {
                base.SetDescription(original + " - " + description);
            }
        }

        /// <summary>
        /// Set Warehouse Locator.
        /// - will overwrite Organization -
        /// </summary>
        /// <param name="VAM_Locator_ID">locator</param>
        public new void SetVAM_Locator_ID(int VAM_Locator_ID)
        {
            base.SetVAM_Locator_ID(VAM_Locator_ID);
            SetVAF_Org_ID(0);	//	reset
        }


        /// <summary>
        /// Set Location
        /// </summary>
        /// <param name="VAB_Address_ID"></param>
        /// <param name="isFrom"></param>
        public void SetLocation(int VAB_Address_ID, bool isFrom)
        {
            if (isFrom)
            {
                SetVAB_LocFrom_ID(VAB_Address_ID);
            }
            else
            {
                SetVAB_LocTo_ID(VAB_Address_ID);
            }
        }

        /// <summary>
        /// Set Location from Locator
        /// </summary>
        /// <param name="VAM_Locator_ID"></param>
        /// <param name="isFrom"></param>
        public void SetLocationFroMVAMLocator(int VAM_Locator_ID, bool isFrom)
        {
            if (VAM_Locator_ID == 0)
            {
                return;
            }
            int VAB_Address_ID = 0;
            String sql = "SELECT w.VAB_Address_ID FROM VAM_Warehouse w, VAM_Locator l "
                + "WHERE w.VAM_Warehouse_ID=l.VAM_Warehouse_ID AND l.VAM_Locator_ID=" + VAM_Locator_ID;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                if (idr.Read())
                {
                    VAB_Address_ID = Utility.Util.GetValueOfInt(idr[0]);//.getInt(1);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
                log.Log(Level.SEVERE, sql, e);
                return;
            }
            if (VAB_Address_ID != 0)
                SetLocation(VAB_Address_ID, isFrom);
        }

        /// <summary>
        /// Set Location from Busoness Partner Location
        /// </summary>
        /// <param name="VAB_BPart_Location_ID"></param>
        /// <param name="isFrom"></param>
        public void SetLocationFromBPartner(int VAB_BPart_Location_ID, bool isFrom)
        {
            if (VAB_BPart_Location_ID == 0)
            {
                return;
            }
            int VAB_Address_ID = 0;
            String sql = "SELECT VAB_Address_ID FROM VAB_BPart_Location WHERE VAB_BPart_Location_ID=" + VAB_BPart_Location_ID;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                if (idr.Read())
                {
                    VAB_Address_ID = Utility.Util.GetValueOfInt(idr[0]);//.getInt(1);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                    idr.Close();
                log.Log(Level.SEVERE, sql, e);
                return;
            }
            if (VAB_Address_ID != 0)
            {
                SetLocation(VAB_Address_ID, isFrom);
            }
        }

        /// <summary>
        /// Set Location from Organization
        /// </summary>
        /// <param name="VAF_Org_ID"></param>
        /// <param name="isFrom"></param>
        public void SetLocationFromOrg(int VAF_Org_ID, bool isFrom)
        {
            if (VAF_Org_ID == 0)
            {
                return;
            }
            int VAB_Address_ID = 0;
            String sql = "SELECT VAB_Address_ID FROM VAF_OrgDetail WHERE VAF_Org_ID=" + VAF_Org_ID;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                if (idr.Read())
                {
                    VAB_Address_ID = Utility.Util.GetValueOfInt(idr[0]);//.getInt(1);
                }
                idr.Close();

            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
                log.Log(Level.SEVERE, sql, e);
                return;
            }
            if (VAB_Address_ID != 0)
            {
                SetLocation(VAB_Address_ID, isFrom);
            }
        }


        /// <summary>
        /// Returns Source Balance of line
        /// </summary>
        /// <returns>source balance</returns>
        public Decimal GetSourceBalance()
        {
            //if (GetAmtSourceDr() == null)
            //{
            //    SetAmtSourceDr(Env.ZERO);
            //}
            //if (GetAmtSourceCr() == null)
            //{
            //    SetAmtSourceCr(Env.ZERO);
            //}
            //
            return Decimal.Subtract(GetAmtSourceDr(), GetAmtSourceCr());
        }

        /// <summary>
        /// Is Debit Source Balance
        /// </summary>
        /// <returns>true if DR source balance</returns>
        public bool IsDrSourceBalance()
        {
            return Env.Signum(GetSourceBalance()) != -1;
        }

        /// <summary>
        /// Get Accounted Balance
        /// </summary>
        /// <returns>accounting balance</returns>
        public Decimal GetAcctBalance()
        {
            //if (GetAmtAcctDr() == null)
            //{
            //    SetAmtAcctDr(Env.ZERO);
            //}
            //if (GetAmtAcctCr() == null)
            //{
            //    SetAmtAcctCr(Env.ZERO);
            //}
            return Decimal.Subtract(GetAmtAcctDr(), GetAmtAcctCr());
        }

        /// <summary>
        /// Is Account on Balance Sheet
        /// </summary>
        /// <returns>true if account is a balance sheet account</returns>
        public bool IsBalanceSheet()
        {
            return _acct.IsBalanceSheet();
        }

        /// <summary>
        /// Currect Accounting Amount.
        /// <pre>
        /// Example:    1       -1      1       -1
        /// Old         100/0   100/0   0/100   0/100
        /// New         99/0    101/0   0/99    0/101
        /// </pre>
        /// </summary>
        /// <param name="deltaAmount">delta amount</param>
        public void CurrencyCorrect(Decimal deltaAmount)
        {
            bool negative = deltaAmount.CompareTo(Env.ZERO) < 0;
            bool adjustDr = Math.Abs(GetAmtAcctDr()).CompareTo(Math.Abs(GetAmtAcctCr())) > 0;

            log.Fine(deltaAmount.ToString()
                + "; Old-AcctDr=" + GetAmtAcctDr() + ",AcctCr=" + GetAmtAcctCr()
                + "; Negative=" + negative + "; AdjustDr=" + adjustDr);

            if (adjustDr)
            {
                if (negative)
                {
                    SetAmtAcctDr(Decimal.Subtract(GetAmtAcctDr(), deltaAmount));
                }
                else
                {
                    SetAmtAcctDr(Decimal.Subtract(GetAmtAcctDr(), deltaAmount));
                }
            }
            else
            {
                if (negative)
                {
                    SetAmtAcctCr(Decimal.Add(GetAmtAcctCr(), deltaAmount));
                }
                else
                {
                    SetAmtAcctCr(Decimal.Add(GetAmtAcctCr(), deltaAmount));
                }
            }

            log.Fine("New-AcctDr=" + GetAmtAcctDr() + ",AcctCr=" + GetAmtAcctCr());
        }

        /// <summary>
        /// Convert to Accounted Currency
        /// </summary>
        /// <returns>true if converted</returns>
        public bool Convert()
        {
            //  Document has no currency
            if (GetVAB_Currency_ID() == Doc.NO_CURRENCY)
            {
                SetVAB_Currency_ID(_acctSchema.GetVAB_Currency_ID());
            }

            if (_acctSchema.GetVAB_Currency_ID() == GetVAB_Currency_ID())
            {
                SetAmtAcctDr(GetAmtSourceDr());
                SetAmtAcctCr(GetAmtSourceCr());
                return true;
            }
            //	Get Conversion Type from Line or Header
            int VAB_CurrencyType_ID = 0;
            int VAF_Org_ID = 0;
            if (_docLine != null)			//	get from line
            {
                VAB_CurrencyType_ID = _docLine.GetVAB_CurrencyType_ID();
                VAF_Org_ID = _docLine.GetVAF_Org_ID();
            }
            if (VAB_CurrencyType_ID == 0)	//	get from header
            {
                if (_doc == null)
                {
                    log.Severe("No Document VO");
                    return false;
                }
                VAB_CurrencyType_ID = _doc.GetVAB_CurrencyType_ID();
                if (VAF_Org_ID == 0)
                {
                    VAF_Org_ID = _doc.GetVAF_Org_ID();
                }
            }

            DateTime? convDate = GetDateAcct();

            // For sourceforge bug 1718381: Use transaction date instead of
            // accounting date for currency conversion when the document is Bank
            // Statement. Ideally this should apply to all "reconciliation"
            // accounting entries, but doing just Bank Statement for now to avoid
            // breaking other things.
            if (_doc is Doc_Bank)
            {
                convDate = GetDateTrx();
            }
            else if (_doc is Doc_GLJournal)
            {
                SetAmtAcctDr(Decimal.Round(Decimal.Multiply(GetAmtSourceDr(), _docLine != null ? Util.GetValueOfDecimal(_docLine.GetConversionRate()) : Util.GetValueOfDecimal(GetConversionRate())), _acctSchema.GetStdPrecision()));
                SetAmtAcctCr(Decimal.Round(Decimal.Multiply(GetAmtSourceCr(), _docLine != null ? Util.GetValueOfDecimal(_docLine.GetConversionRate()) : Util.GetValueOfDecimal(GetConversionRate())), _acctSchema.GetStdPrecision()));
                return true;
            }

            SetAmtAcctDr(MVABExchangeRate.Convert(GetCtx(),
                GetAmtSourceDr(), GetVAB_Currency_ID(), _acctSchema.GetVAB_Currency_ID(),
                convDate, VAB_CurrencyType_ID, _doc.GetVAF_Client_ID(), VAF_Org_ID));
            //if (GetAmtAcctDr() == null)
            //{
            //    return false;
            //}
            SetAmtAcctCr(MVABExchangeRate.Convert(GetCtx(),
                GetAmtSourceCr(), GetVAB_Currency_ID(), _acctSchema.GetVAB_Currency_ID(),
                convDate, VAB_CurrencyType_ID, _doc.GetVAF_Client_ID(), VAF_Org_ID));
            return true;
        }

        /// <summary>
        /// Get Account
        /// </summary>
        /// <returns>account</returns>
        public MVABAccount GetAccount()
        {
            return _acct;
        }

        /// <summary>
        /// To String
        /// </summary>
        /// <returns>String</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("FactLine=[");
            sb.Append(GetVAF_TableView_ID()).Append(":").Append(GetRecord_ID())
                .Append(",").Append(_acct)
                .Append(",Cur=").Append(GetVAB_Currency_ID())
                .Append(", DR=").Append(GetAmtSourceDr()).Append("|").Append(GetAmtAcctDr())
                .Append(", CR=").Append(GetAmtSourceCr()).Append("|").Append(GetAmtAcctCr())
                .Append("]");
            return sb.ToString();
        }


        /// <summary>
        /// Get VAF_Org_ID (balancing segment).
        /// (if not set directly - from document line, document, account, locator)
        /// <p>
        /// Note that Locator needs to be set before - otherwise
        /// segment balancing might produce the wrong results
        /// </summary>
        /// <returns>VAF_Org_ID</returns>
        public new int GetVAF_Org_ID()
        {
            if (base.GetVAF_Org_ID() != 0)      //  set earlier
            {
                return base.GetVAF_Org_ID();
            }
            //	Prio 1 - get from locator - if exist
            if (GetVAM_Locator_ID() != 0)
            {
                String sql = "SELECT VAF_Org_ID FROM VAM_Locator WHERE VAM_Locator_ID=" + GetVAM_Locator_ID() + " AND VAF_Client_ID=" + GetVAF_Client_ID();
                IDataReader idr = null;
                try
                {
                    idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                    if (idr.Read())
                    {
                        SetVAF_Org_ID(Utility.Util.GetValueOfInt(idr[0]));//.getInt(1));
                        log.Finer("VAF_Org_ID=" + base.GetVAF_Org_ID() + " (1 from VAM_Locator_ID=" + GetVAM_Locator_ID() + ")");
                    }
                    else
                    {
                        log.Log(Level.SEVERE, "VAF_Org_ID - Did not find VAM_Locator_ID=" + GetVAM_Locator_ID());
                    }
                    idr.Close();
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                        idr = null;
                    }
                    log.Log(Level.SEVERE, sql, e);
                }
            }   //  VAM_Locator_ID != 0

            //	Prio 2 - get from doc line - if exists (document context overwrites)
            if (_docLine != null && base.GetVAF_Org_ID() == 0)
            {
                SetVAF_Org_ID(_docLine.GetVAF_Org_ID());
                log.Finer("VAF_Org_ID=" + base.GetVAF_Org_ID() + " (2 from DocumentLine)");
            }
            //	Prio 3 - get from doc - if not GL
            if (_doc != null && base.GetVAF_Org_ID() == 0)
            {
                if (MVABMasterDocType.DOCBASETYPE_GLJOURNAL.Equals(_doc.GetDocumentType()))
                {
                    SetVAF_Org_ID(_acct.GetVAF_Org_ID()); //	inter-company GL
                    log.Finer("VAF_Org_ID=" + base.GetVAF_Org_ID() + " (3 from Acct)");
                }
                else
                {
                    SetVAF_Org_ID(_doc.GetVAF_Org_ID());
                    log.Finer("VAF_Org_ID=" + base.GetVAF_Org_ID() + " (3 from Document)");
                }
            }
            //	Prio 4 - get from account - if not GL
            if (_doc != null && base.GetVAF_Org_ID() == 0)
            {
                if (MVABMasterDocType.DOCBASETYPE_GLJOURNAL.Equals(_doc.GetDocumentType()))
                {
                    SetVAF_Org_ID(_doc.GetVAF_Org_ID());
                    log.Finer("VAF_Org_ID=" + base.GetVAF_Org_ID() + " (4 from Document)");
                }
                else
                {
                    SetVAF_Org_ID(_acct.GetVAF_Org_ID());
                    log.Finer("VAF_Org_ID=" + base.GetVAF_Org_ID() + " (4 from Acct)");
                }
            }
            return base.GetVAF_Org_ID();
        }

        /// <summary>
        /// Get/derive Sales Region
        /// </summary>
        /// <returns>Sales Region</returns>
        public new int GetVAB_SalesRegionState_ID()
        {
            if (base.GetVAB_SalesRegionState_ID() != 0)
            {
                return base.GetVAB_SalesRegionState_ID();
            }
            //
            if (_docLine != null)
            {
                SetVAB_SalesRegionState_ID(_docLine.GetVAB_SalesRegionState_ID());
            }
            if (_doc != null)
            {
                if (base.GetVAB_SalesRegionState_ID() == 0)
                {
                    SetVAB_SalesRegionState_ID(_doc.GetVAB_SalesRegionState_ID());
                }
                if (base.GetVAB_SalesRegionState_ID() == 0 && _doc.GetBP_VAB_SalesRegionState_ID() > 0)
                {
                    SetVAB_SalesRegionState_ID(_doc.GetBP_VAB_SalesRegionState_ID());
                }
                //	derive SalesRegion if AcctSegment
                if (base.GetVAB_SalesRegionState_ID() == 0
                    && _doc.GetVAB_BPart_Location_ID() != 0
                    && _doc.GetBP_VAB_SalesRegionState_ID() == -1)	//	never tried
                //	&& _acctSchema.isAcctSchemaElement(MAcctSchemaElement.ELEMENTTYPE_SalesRegion))
                {
                    String sql = "SELECT COALESCE(VAB_SalesRegionState_ID,0) FROM VAB_BPart_Location WHERE VAB_BPart_Location_ID=@param1";
                    SetVAB_SalesRegionState_ID(DataBase.DB.GetSQLValue(null, sql, _doc.GetVAB_BPart_Location_ID()));

                    if (base.GetVAB_SalesRegionState_ID() != 0)		//	save in VO
                    {
                        _doc.SetBP_VAB_SalesRegionState_ID(base.GetVAB_SalesRegionState_ID());
                        log.Fine("VAB_SalesRegionState_ID=" + base.GetVAB_SalesRegionState_ID() + " (from BPL)");
                    }
                    else	//	From Sales Rep of Document -> Sales Region
                    {
                        sql = "SELECT COALESCE(MAX(VAB_SalesRegionState_ID),0) FROM VAB_SalesRegionState WHERE SalesRep_ID=@param1";
                        SetVAB_SalesRegionState_ID(DataBase.DB.GetSQLValue(null, sql, _doc.GetSalesRep_ID()));
                        if (base.GetVAB_SalesRegionState_ID() != 0)		//	save in VO
                        {
                            _doc.SetBP_VAB_SalesRegionState_ID(base.GetVAB_SalesRegionState_ID());
                            log.Fine("VAB_SalesRegionState_ID=" + base.GetVAB_SalesRegionState_ID() + " (from SR)");
                        }
                        else
                        {
                            _doc.SetBP_VAB_SalesRegionState_ID(-2);	//	don't try again
                        }
                    }
                }
                if (_acct != null && base.GetVAB_SalesRegionState_ID() == 0)
                {
                    SetVAB_SalesRegionState_ID(_acct.GetVAB_SalesRegionState_ID());
                }
            }
            //
            //	log.Fine("VAB_SalesRegionState_ID=" + base.getVAB_SalesRegionState_ID() 
            //		+ ", VAB_BPart_Location_ID=" + m_docVO.VAB_BPart_Location_ID
            //		+ ", BP_VAB_SalesRegionState_ID=" + m_docVO.BP_VAB_SalesRegionState_ID 
            //		+ ", SR=" + _acctSchema.isAcctSchemaElement(MAcctSchemaElement.ELEMENTTYPE_SalesRegion));
            return base.GetVAB_SalesRegionState_ID();
        }


        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord"></param>
        /// <returns>true</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            if (newRecord)
            {
                log.Fine(ToString());
                //
                GetVAF_Org_ID();
                GetVAB_SalesRegionState_ID();
                //  Set Default Account Info
                if (GetVAM_Product_ID() == 0)
                {
                    SetVAM_Product_ID(_acct.GetVAM_Product_ID());
                }
                if (GetVAB_LocFrom_ID() == 0)
                {
                    SetVAB_LocFrom_ID(_acct.GetVAB_LocFrom_ID());
                }
                if (GetVAB_LocTo_ID() == 0)
                {
                    SetVAB_LocTo_ID(_acct.GetVAB_LocTo_ID());
                }
                if (GetVAB_BusinessPartner_ID() == 0)
                {
                    SetVAB_BusinessPartner_ID(_acct.GetVAB_BusinessPartner_ID());
                }
                if (GetVAF_OrgTrx_ID() == 0)
                {
                    SetVAF_OrgTrx_ID(_acct.GetVAF_OrgTrx_ID());
                }
                if (GetVAB_Project_ID() == 0)
                {
                    SetVAB_Project_ID(_acct.GetVAB_Project_ID());
                }
                if (GetVAB_Promotion_ID() == 0)
                {
                    SetVAB_Promotion_ID(_acct.GetVAB_Promotion_ID());
                }
                if (GetVAB_BillingCode_ID() == 0)
                {
                    SetVAB_BillingCode_ID(_acct.GetVAB_BillingCode_ID());
                }
                if (GetUser1_ID() == 0)
                {
                    SetUser1_ID(_acct.GetUser1_ID());
                }
                if (GetUser2_ID() == 0)
                {
                    SetUser2_ID(_acct.GetUser2_ID());
                }
                if (GetUserElement1_ID() == 0)
                {
                    SetUserElement1_ID(_acct.GetUserElement1_ID());
                }
                if (GetUserElement2_ID() == 0)
                {
                    SetUserElement2_ID(_acct.GetUserElement2_ID());
                }
                if (GetUserElement3_ID() == 0)
                {
                    SetUserElement3_ID(_acct.GetUserElement3_ID());
                }
                if (GetUserElement4_ID() == 0)
                {
                    SetUserElement4_ID(_acct.GetUserElement4_ID());
                }
                if (GetUserElement5_ID() == 0)
                {
                    SetUserElement5_ID(_acct.GetUserElement5_ID());
                }
                if (GetUserElement6_ID() == 0)
                {
                    SetUserElement6_ID(_acct.GetUserElement6_ID());
                }
                if (GetUserElement7_ID() == 0)
                {
                    SetUserElement7_ID(_acct.GetUserElement7_ID());
                }
                if (GetUserElement8_ID() == 0)
                {
                    SetUserElement8_ID(_acct.GetUserElement8_ID());
                }
                if (GetUserElement9_ID() == 0)
                {
                    SetUserElement9_ID(_acct.GetUserElement9_ID());
                }


                //  Revenue Recognition for AR Invoices
                if (_doc.GetDocumentType().Equals(MVABMasterDocType.DOCBASETYPE_ARINVOICE)
                    && _docLine != null
                    && _docLine.GetVAB_Rev_Recognition_ID() != 0)
                {
                    int VAF_UserContact_ID = 0;
                    SetAccount_ID(
                        CreateRevenueRecognition(
                            _docLine.GetVAB_Rev_Recognition_ID(), _docLine.Get_ID(),
                            GetVAF_Client_ID(), GetVAF_Org_ID(), VAF_UserContact_ID,
                            GetAccount_ID(), GetVAB_SubAcct_ID(),
                            GetVAM_Product_ID(), GetVAB_BusinessPartner_ID(), GetVAF_OrgTrx_ID(),
                            GetVAB_LocFrom_ID(), GetVAB_LocTo_ID(),
                            GetVAB_SalesRegionState_ID(), GetVAB_Project_ID(),
                            GetVAB_Promotion_ID(), GetVAB_BillingCode_ID(),
                            GetUser1_ID(), GetUser2_ID(),
                            GetUserElement1_ID(), GetUserElement2_ID())
                        );
                }
            }
            return true;
        }

        /// <summary>
        /// Revenue Recognition.
        /// Called from FactLine.save
        /// <p>
        /// Create Revenue recognition plan and return Unearned Revenue account
        /// to be used instead of Revenue Account. If not found, it returns
        /// the revenue account.
        /// </summary>
        /// <param name="VAB_Rev_Recognition_ID">revenue recognition</param>
        /// <param name="VAB_InvoiceLine_ID">invoice line</param>
        /// <param name="VAF_Client_ID">client</param>
        /// <param name="VAF_Org_ID">Org</param>
        /// <param name="VAF_UserContact_ID">user</param>
        /// <param name="Account_ID">of Revenue Account</param>
        /// <param name="VAB_SubAcct_ID"> sub account</param>
        /// <param name="VAM_Product_ID">product</param>
        /// <param name="VAB_BusinessPartner_ID">bpartner</param>
        /// <param name="VAF_OrgTrx_ID"> trx org</param>
        /// <param name="VAB_LocFrom_ID">loc from</param>
        /// <param name="VAB_LocTo_ID">loc to</param>
        /// <param name="C_SRegion_ID">sales region</param>
        /// <param name="VAB_Project_ID">project</param>
        /// <param name="VAB_Promotion_ID">campaign</param>
        /// <param name="VAB_BillingCode_ID">activity</param>
        /// <param name="User1_ID"></param>
        /// <param name="User2_ID"></param>
        /// <param name="UserElement1_ID">user element 1</param>
        /// <param name="UserElement2_ID">user element 2</param>
        /// <returns></returns>
        private int CreateRevenueRecognition(
            int VAB_Rev_Recognition_ID, int VAB_InvoiceLine_ID,
            int VAF_Client_ID, int VAF_Org_ID, int VAF_UserContact_ID,
            int Account_ID, int VAB_SubAcct_ID,
            int VAM_Product_ID, int VAB_BusinessPartner_ID, int VAF_OrgTrx_ID,
            int VAB_LocFrom_ID, int VAB_LocTo_ID, int C_SRegion_ID, int VAB_Project_ID,
            int VAB_Promotion_ID, int VAB_BillingCode_ID,
            int User1_ID, int User2_ID, int UserElement1_ID, int UserElement2_ID)
        {
            log.Fine("From Accout_ID=" + Account_ID);
            //  get VC for P_Revenue (from Product)
            MVABAccount revenue = MVABAccount.Get(GetCtx(),
                VAF_Client_ID, VAF_Org_ID, GetVAB_AccountBook_ID(), Account_ID, VAB_SubAcct_ID,
                VAM_Product_ID, VAB_BusinessPartner_ID, VAF_OrgTrx_ID, VAB_LocFrom_ID, VAB_LocTo_ID, C_SRegion_ID,
                VAB_Project_ID, VAB_Promotion_ID, VAB_BillingCode_ID,
                User1_ID, User2_ID, UserElement1_ID, UserElement2_ID);
            if (revenue != null && revenue.Get_ID() == 0)
            {
                revenue.Save();
            }
            if (revenue == null || revenue.Get_ID() == 0)
            {
                log.Severe("Revenue_Acct not found");
                return Account_ID;
            }
            int P_Revenue_Acct = revenue.Get_ID();

            //  get Unearned Revenue Acct from BPartner Group
            int unearnedRevenue_Acct = 0;
            int new_Account_ID = 0;

            String sql = "SELECT ga.UnearnedRevenue_Acct, vc.Account_ID "
                + "FROM VAB_BPart_Category_Acct ga, VAB_BusinessPartner p, VAB_Acct_ValidParameter vc "
                + "WHERE ga.VAB_BPart_Category_ID=p.VAB_BPart_Category_ID"
                + " AND ga.UnearnedRevenue_Acct=vc.VAB_Acct_ValidParameter_ID"
                + " AND ga.VAB_AccountBook_ID=" + GetVAB_AccountBook_ID() + " AND p.VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                if (idr.Read())
                {
                    unearnedRevenue_Acct = Utility.Util.GetValueOfInt(idr[0]);///.getInt(1);
                    new_Account_ID = Utility.Util.GetValueOfInt(idr[1]);//.getInt(2);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                    idr.Close();
                log.Log(Level.SEVERE, sql, e);
            }
            if (new_Account_ID == 0)
            {
                log.Severe("UnearnedRevenue_Acct not found");
                return Account_ID;
            }

            MVABRevRecognitionStrtgy plan = new MVABRevRecognitionStrtgy(GetCtx(), 0, null);
            plan.SetVAB_Rev_Recognition_ID(VAB_Rev_Recognition_ID);
            plan.SetVAB_AccountBook_ID(GetVAB_AccountBook_ID());
            plan.SetVAB_InvoiceLine_ID(VAB_InvoiceLine_ID);
            plan.SetUnEarnedRevenue_Acct(unearnedRevenue_Acct);
            plan.SetP_Revenue_Acct(P_Revenue_Acct);
            plan.SetVAB_Currency_ID(GetVAB_Currency_ID());
            plan.SetTotalAmt(GetAcctBalance());
            if (!plan.Save(Get_TrxName()))
            {
                log.Severe("Plan NOT created");
                return Account_ID;
            }
            log.Fine("From Acctount_ID=" + Account_ID + " to " + new_Account_ID
                + " - Plan from UnearnedRevenue_Acct=" + unearnedRevenue_Acct + " to Revenue_Acct=" + P_Revenue_Acct);
            return new_Account_ID;
        }


        /// <summary>
        /// Update Line with reversed Original Amount in Accounting Currency.
        /// Also copies original dimensions like Project, etc.
        /// Called from Doc_MatchInv
        /// </summary>
        /// <param name="VAF_TableView_ID"></param>
        /// <param name="Record_ID"></param>
        /// <param name="Line_ID"></param>
        /// <param name="multiplier">targetQty/documentQty</param>
        /// <returns>true if success</returns>
        public bool UpdateReverseLine(int VAF_TableView_ID, int Record_ID, int Line_ID, Decimal multiplier)
        {
            bool success = false;

            String sql = "SELECT * "
                + "FROM Actual_Acct_Detail "
                + "WHERE VAB_AccountBook_ID=" + GetVAB_AccountBook_ID() + " AND VAF_TableView_ID=" + VAF_TableView_ID + " AND Record_ID=" + Record_ID
                + " AND Line_ID=" + Line_ID + " AND Account_ID=" + _acct.GetAccount_ID();
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                //pstmt.setInt(1, GetVAB_AccountBook_ID());
                //pstmt.setInt(2, VAF_TableView_ID);
                //pstmt.setInt(3, Record_ID);
                //pstmt.setInt(4, Line_ID);
                //pstmt.setInt(5, _acct.GetAccount_ID());

                if (idr.Read())
                {
                    MActualAcctDetail fact = new MActualAcctDetail(GetCtx(), idr, Get_TrxName());
                    //  Accounted Amounts - reverse
                    Decimal dr = fact.GetAmtAcctDr();
                    Decimal cr = fact.GetAmtAcctCr();
                    SetAmtAcctDr(Decimal.Multiply(cr, multiplier));
                    SetAmtAcctCr(Decimal.Multiply(dr, multiplier));
                    //  Source Amounts
                    SetAmtSourceDr(GetAmtAcctDr());
                    SetAmtSourceCr(GetAmtAcctCr());
                    //
                    success = true;
                    log.Fine(new StringBuilder("(Table=").Append(VAF_TableView_ID)
                        .Append(",Record_ID=").Append(Record_ID)
                        .Append(",Line=").Append(Record_ID)
                        .Append(", Account=").Append(_acct)
                        .Append(",dr=").Append(dr).Append(",cr=").Append(cr)
                        .Append(") - DR=").Append(GetAmtSourceDr()).Append("|").Append(GetAmtAcctDr())
                        .Append(", CR=").Append(GetAmtSourceCr()).Append("|").Append(GetAmtAcctCr())
                        .ToString());
                    //	Dimensions
                    SetVAF_OrgTrx_ID(fact.GetVAF_OrgTrx_ID());
                    SetVAB_Project_ID(fact.GetVAB_Project_ID());
                    SetVAB_BillingCode_ID(fact.GetVAB_BillingCode_ID());
                    SetVAB_Promotion_ID(fact.GetVAB_Promotion_ID());
                    SetVAB_SalesRegionState_ID(fact.GetVAB_SalesRegionState_ID());
                    SetVAB_LocFrom_ID(fact.GetVAB_LocFrom_ID());
                    SetVAB_LocTo_ID(fact.GetVAB_LocTo_ID());
                    SetVAM_Product_ID(fact.GetVAM_Product_ID());
                    SetVAM_Locator_ID(fact.GetVAM_Locator_ID());
                    SetUser1_ID(fact.GetUser1_ID());
                    SetUser2_ID(fact.GetUser2_ID());
                    SetVAB_UOM_ID(fact.GetVAB_UOM_ID());
                    SetVAB_TaxRate_ID(fact.GetVAB_TaxRate_ID());
                    //	Org for cross charge
                    SetVAF_Org_ID(fact.GetVAF_Org_ID());
                }
                else
                {
                    log.Warning(new StringBuilder("Not Found (try later) ")
                        .Append(",VAB_AccountBook_ID=").Append(GetVAB_AccountBook_ID())
                        .Append(", VAF_TableView_ID=").Append(VAF_TableView_ID)
                        .Append(",Record_ID=").Append(Record_ID)
                        .Append(",Line_ID=").Append(Line_ID)
                        .Append(", Account_ID=").Append(_acct.GetAccount_ID()).ToString());
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
                log.Log(Level.SEVERE, sql, e);
            }
            return success;
        }

    }
}
