﻿/********************************************************
 * Class Name     : MVABInvoice
 * Purpose        : Calculate the invoice using VAB_Invoice table
 * Class Used     : X_VAB_Invoice, DocAction
 * Chronological    Development
 * Raghunandan     05-Jun-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using System.Data.SqlClient;
using System.IO;
//using java.io;
//using System.IO;
using VAdvantage.Logging;
using VAdvantage.Print;


namespace VAdvantage.Model
{
    public class MVABInvoice : X_VAB_Invoice, DocAction
    {
        #region Variables
        //	Open Amount		
        private Decimal? _openAmt = null;

        //	Invoice Lines	
        private MVABInvoiceLine[] _lines;
        //	Invoice Taxes	
        private MVABTaxInvoice[] _taxes;
        /**	Process Message 			*/
        private String _processMsg = null;
        /**	Just Prepared Flag			*/
        private bool _justPrepared = false;

        /** For Counter Document **/
        private MVABBusinessPartner counterBPartner = null; // counter Business partner
        private int counterOrgId = 0; // counter Organization

        /** Reversal Flag		*/
        private bool _reversal = false;
        //	Cache					
        private static CCache<int, MVABInvoice> _cache = new CCache<int, MVABInvoice>("VAB_Invoice", 20, 2);	//	2 minutes
        //	Logger			
        private static VLogger _log = VLogger.GetVLogger(typeof(MVABInvoice).FullName);

        private MVABAccountBook acctSchema = null;

        decimal currentCostPrice = 0;
        string conversionNotFoundInvoice = "";
        string conversionNotFoundInvoice1 = "";
        string conversionNotFoundInOut = "";
        MVAMInvInOutLine sLine = null;

        #endregion

        /// <summary>
        /// Get Payments Of BPartner
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_BusinessPartner_ID">id</param>
        /// <param name="trxName">transaction</param>
        /// <returns>Array</returns>
        public static MVABInvoice[] GetOfBPartner(Ctx ctx, int VAB_BusinessPartner_ID, Trx trxName)
        {
            List<MVABInvoice> list = new List<MVABInvoice>();
            String sql = "SELECT * FROM VAB_Invoice WHERE VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID;
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, trxName);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVABInvoice(ctx, dr, trxName));
                }

            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                _log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;
            }

            MVABInvoice[] retValue = new MVABInvoice[list.Count];

            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        ///	Create new Invoice by copying
        /// </summary>
        /// <param name="from">invoice</param>
        /// <param name="dateDoc">date of the document date</param>
        /// <param name="VAB_DocTypesTarget_ID">target doc type</param>
        /// <param name="counter">sales order</param>
        /// <param name="trxName">trx</param>
        /// <param name="setOrder">set Order links</param>
        /// <returns></returns>
        public static MVABInvoice CopyFrom(MVABInvoice from, DateTime? dateDoc, int VAB_DocTypesTarget_ID,
            Boolean counter, Trx trxName, Boolean setOrder)
        {
            MVABInvoice to = new MVABInvoice(from.GetCtx(), 0, null);
            to.Set_TrxName(trxName);
            PO.CopyValues(from, to, from.GetVAF_Client_ID(), from.GetVAF_Org_ID());
            to.Set_ValueNoCheck("VAB_Invoice_ID", I_ZERO);
            to.Set_ValueNoCheck("DocumentNo", null);
            if (!counter)
            {
                to.AddDescription("{->" + from.GetDocumentNo() + ")");
            }
            else
            {
                //set the description and invoice reference from original to counter
                to.AddDescription(Msg.GetMsg(from.GetCtx(), "CounterDocument") + from.GetDocumentNo());
                to.Set_Value("InvoiceReference", from.GetDocumentNo());
            }
            //
            to.SetDocStatus(DOCSTATUS_Drafted);		//	Draft
            to.SetDocAction(DOCACTION_Complete);
            //
            to.SetVAB_DocTypes_ID(0);
            to.SetVAB_DocTypesTarget_ID(VAB_DocTypesTarget_ID, true);
            //
            to.SetDateInvoiced(dateDoc);
            to.SetDateAcct(dateDoc);
            to.SetDatePrinted(null);
            to.SetIsPrinted(false);
            //    
            to.SetIsApproved(false);
            to.SetVAB_Payment_ID(0);
            to.SetVAB_CashJRNLLine_ID(0);
            to.SetIsPaid(false);
            to.SetIsInDispute(false);
            //
            //	Amounts are updated by trigger when adding lines
            to.SetGrandTotal(Env.ZERO);
            to.SetTotalLines(Env.ZERO);
            //
            to.SetIsTransferred(false);
            to.SetPosted(false);
            to.SetProcessed(false);
            //	delete references
            to.SetIsSelfService(false);
            if (!setOrder)
                to.SetVAB_Order_ID(0);
            if (counter)
            {
                to.SetVAB_Order_ID(0);

                //SI_0625 : Link Organization Functionality
                // set counter BP Org
                if (from.GetCounterOrgID() > 0)
                    to.SetVAF_Org_ID(from.GetCounterOrgID());

                // set Counter BP Details
                if (from.GetCounterBPartner() != null)
                    to.SetBPartner(from.GetCounterBPartner());

                MVAMPriceList pl = MVAMPriceList.Get(from.GetCtx(), to.GetVAM_PriceList_ID(), trxName);
                //when record is of SO then price list must be Sale price list and vice versa
                if (from.GetCounterBPartner() != null && ((to.IsSOTrx() && !pl.IsSOPriceList()) || (!to.IsSOTrx() && pl.IsSOPriceList())))
                {
                    /* 1. check first with same currency , same Org , same client and IsDefault as True
                     * 2. check 2nd with same currency , same Org , same client and IsDefault as False
                     * 3. check 3rd with same currency , (*) Org , same client and IsDefault as True
                     * 4. check 3rd with same currency , (*) Org , same client and IsDefault as False */
                    string sql = @"SELECT VAM_PriceList_ID FROM VAM_PriceList 
                               WHERE IsActive = 'Y' AND VAF_Client_ID IN ( " + to.GetVAF_Client_ID() + @" , 0 ) " +
                                    @" AND VAB_Currency_ID = " + to.GetVAB_Currency_ID() +
                                    @" AND VAF_Org_ID IN ( " + to.GetVAF_Org_ID() + @" , 0 ) " +
                                    @" AND IsSOPriceList = '" + (to.IsSOTrx() ? "Y" : "N") + "' " +
                                    @" ORDER BY VAF_Org_ID DESC , IsDefault DESC,  VAM_PriceList_ID DESC , VAF_Client_ID DESC";
                    int priceListId = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, trxName));
                    if (priceListId > 0)
                    {
                        to.SetVAM_PriceList_ID(priceListId);
                    }
                    else
                    {
                        //Could not create Invoice. Price List not avialable
                        from.SetProcessMsg(Msg.GetMsg(from.GetCtx(), "VIS_PriceListNotFound"));
                        throw new Exception("Could not create Invoice. Price List not avialable");
                    }
                }

                to.SetRef_Invoice_ID(from.GetVAB_Invoice_ID());
                //	Try to find Order link
                if (from.GetVAB_Order_ID() != 0)
                {
                    MVABOrder peer = new MVABOrder(from.GetCtx(), from.GetVAB_Order_ID(), from.Get_TrxName());
                    if (peer.GetRef_Order_ID() != 0)
                        to.SetVAB_Order_ID(peer.GetRef_Order_ID());
                }
            }
            else
                to.SetRef_Invoice_ID(0);

            // on copy record set Temp Document No to empty
            if (to.Get_ColumnIndex("TempDocumentNo") > 0)
            {
                to.SetTempDocumentNo("");
            }

            if (!to.Save(trxName))
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null)
                {
                    from.SetProcessMsg("Could not create Invoice. " + pp.GetName());
                }
                else
                {
                    from.SetProcessMsg("Could not create Invoice.");
                }
                throw new Exception("Could not create Invoice. " + (pp != null && pp.GetName() != null ? pp.GetName() : ""));
            }
            if (counter)
                from.SetRef_Invoice_ID(to.GetVAB_Invoice_ID());
            //	Lines
            if (to.CopyLinesFrom(from, counter, setOrder) == 0)
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null)
                {
                    from.SetProcessMsg("Could not create Invoice Lines. " + pp.GetName());
                }
                else
                {
                    from.SetProcessMsg("Could not create Invoice Lines.");
                }
                throw new Exception("Could not create Invoice Lines. " + (pp != null && pp.GetName() != null ? pp.GetName() : ""));
            }
            return to;
        }

        /// <summary>
        /// Get PDF File Name
        /// </summary>
        /// <param name="documentDir">directory</param>
        /// <param name="VAB_Invoice_ID">invoice</param>
        /// <returns>file name</returns>
        public static String GetPDFFileName(String documentDir, int VAB_Invoice_ID)
        {
            StringBuilder sb = new StringBuilder(documentDir);
            if (sb.Length == 0)
                sb.Append(".");
            //if (!sb.ToString().EndsWith(File.separator))
            //    sb.Append(File.separator);
            if (!sb.ToString().EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                sb.Append(Path.AltDirectorySeparatorChar.ToString());
            }
            sb.Append("VAB_Invoice_ID_")
                .Append(VAB_Invoice_ID)
                .Append(".pdf");
            return sb.ToString();
        }

        /// <summary>
        /// Get MVABInvoice from Cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_Invoice_ID">id</param>
        /// <returns>MVABInvoice</returns>
        public static MVABInvoice Get(Ctx ctx, int VAB_Invoice_ID)
        {
            int key = VAB_Invoice_ID;
            MVABInvoice retValue = (MVABInvoice)_cache[key];
            if (retValue != null)
                return retValue;
            retValue = new MVABInvoice(ctx, VAB_Invoice_ID, null);
            if (retValue.Get_ID() != 0)
                _cache.Add(key, retValue);
            return retValue;
        }

        /// <summary>
        /// Invoice Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_Invoice_ID">invoice or 0 for new</param>
        /// <param name="trxName">trx name</param>
        public MVABInvoice(Ctx ctx, int VAB_Invoice_ID, Trx trxName) :
            base(ctx, VAB_Invoice_ID, trxName)
        {
            if (VAB_Invoice_ID == 0)
            {
                SetDocStatus(DOCSTATUS_Drafted);		//	Draft
                SetDocAction(DOCACTION_Complete);
                //
                //  SetPaymentRule(PAYMENTRULE_OnCredit);	//	Payment Terms

                SetDateInvoiced(DateTime.Now);
                SetDateAcct(DateTime.Now);
                //
                SetChargeAmt(Env.ZERO);
                SetTotalLines(Env.ZERO);
                SetGrandTotal(Env.ZERO);
                //
                SetIsSOTrx(true);
                SetIsTaxIncluded(false);
                SetIsApproved(false);
                SetIsDiscountPrinted(false);
                base.SetIsPaid(false);
                SetSendEMail(false);
                SetIsPrinted(false);
                SetIsTransferred(false);
                SetIsSelfService(false);
                SetIsPayScheduleValid(false);
                SetIsInDispute(false);
                SetPosted(false);
                SetIsReturnTrx(false);
                base.SetProcessed(false);
                SetProcessing(false);
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">datarow</param>
        /// <param name="trxName">transaction</param>
        public MVABInvoice(Ctx ctx, DataRow dr, Trx trxName) :
            base(ctx, dr, trxName)
        {

        }

        /// <summary>
        /// Create Invoice from Order
        /// </summary>
        /// <param name="order">order</param>
        /// <param name="VAB_DocTypesTarget_ID">target document type</param>
        /// <param name="invoiceDate">date or null</param>
        public MVABInvoice(MVABOrder order, int VAB_DocTypesTarget_ID, DateTime? invoiceDate)
            : this(order.GetCtx(), 0, order.Get_TrxName())
        {
            try
            {

                SetClientOrg(order);
                SetOrder(order);	//	set base settings
                //
                if (VAB_DocTypesTarget_ID == 0)
                {
                    VAB_DocTypesTarget_ID = DataBase.DB.GetSQLValue(null,
                "SELECT VAB_DocTypesInvoice_ID FROM VAB_DocTypes WHERE VAB_DocTypes_ID=@param1",
                order.GetVAB_DocTypes_ID());
                    //VAB_DocTypesTarget_ID = DataBase.DB.ExecuteQuery("SELECT VAB_DocTypesInvoice_ID FROM VAB_DocTypes WHERE VAB_DocTypes_ID=" + order.GetVAB_DocTypes_ID(), null, null);
                }
                SetVAB_DocTypesTarget_ID(VAB_DocTypesTarget_ID, true);
                if (invoiceDate != null)
                {
                    SetDateInvoiced(invoiceDate);
                }
                SetDateAcct(GetDateInvoiced());
                //
                SetSalesRep_ID(order.GetSalesRep_ID());
                //
                SetVAB_BusinessPartner_ID(order.GetBill_BPartner_ID());
                SetVAB_BPart_Location_ID(order.GetBill_Location_ID());
                SetVAF_UserContact_ID(order.GetBill_User_ID());
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "MVABInvoice", e);
                throw new ArgumentException(e.Message);
            }
        }

        /// <summary>
        /// Create Invoice from Shipment
        /// </summary>
        /// <param name="ship">shipment</param>
        /// <param name="invoiceDate">date or null</param>
        public MVABInvoice(MVAMInvInOut ship, DateTime? invoiceDate)
            : this(ship.GetCtx(), 0, ship.Get_TrxName())
        {

            SetClientOrg(ship);
            SetShipment(ship);	//	set base settings
            //
            SetVAB_DocTypesTarget_ID();
            if (invoiceDate != null)
                SetDateInvoiced(invoiceDate);
            SetDateAcct(GetDateInvoiced());
            //
            SetSalesRep_ID(ship.GetSalesRep_ID());
            SetVAF_UserContact_ID(ship.GetVAF_UserContact_ID());
        }

        /// <summary>
        /// Create Invoice from Shipment
        /// </summary>
        /// <param name="ship">shipment</param>
        /// <param name="invoiceDate">date or null</param>
        /// <param name="VAB_Order_ID">Order reference for invoice creation</param>
        //public MVABInvoice(MVAMInvInOut ship, DateTime? invoiceDate, int VAB_Order_ID)
        //    : this(ship.GetCtx(), 0, ship.Get_TrxName())
        //{

        //    SetClientOrg(ship);
        //    SetShipment(ship, VAB_Order_ID);	//	set base settings
        //    //
        //    SetVAB_DocTypesTarget_ID();
        //    if (invoiceDate != null)
        //        SetDateInvoiced(invoiceDate);
        //    SetDateAcct(GetDateInvoiced());
        //    //
        //    SetSalesRep_ID(ship.GetSalesRep_ID());
        //    SetVAF_UserContact_ID(ship.GetVAF_UserContact_ID());
        //}

        /// <summary>
        /// Create Invoice from Batch Line
        /// </summary>
        /// <param name="batch">batch</param>
        /// <param name="line">batch line</param>
        public MVABInvoice(MVABInvoiceBatch batch, MVABBatchInvoiceLine line)
            : this(line.GetCtx(), 0, line.Get_TrxName())
        {

            SetClientOrg(line);
            SetDocumentNo(line.GetDocumentNo());
            //
            SetIsSOTrx(batch.IsSOTrx());
            MVABBusinessPartner bp = new MVABBusinessPartner(line.GetCtx(), line.GetVAB_BusinessPartner_ID(), line.Get_TrxName());
            SetBPartner(bp);	//	defaults
            //
            SetIsTaxIncluded(line.IsTaxIncluded());
            //	May conflict with default price list
            SetVAB_Currency_ID(batch.GetVAB_Currency_ID());
            SetVAB_CurrencyType_ID(batch.GetVAB_CurrencyType_ID());
            //
            //	setPaymentRule(order.getPaymentRule());
            //	setVAB_PaymentTerm_ID(order.getVAB_PaymentTerm_ID());
            //	setPOReference("");
            SetDescription(batch.GetDescription());
            //	setDateOrdered(order.getDateOrdered());
            //
            SetVAF_OrgTrx_ID(line.GetVAF_OrgTrx_ID());
            SetVAB_Project_ID(line.GetVAB_Project_ID());
            //	setVAB_Promotion_ID(line.getVAB_Promotion_ID());
            SetVAB_BillingCode_ID(line.GetVAB_BillingCode_ID());
            SetUser1_ID(line.GetUser1_ID());
            SetUser2_ID(line.GetUser2_ID());
            //
            SetVAB_DocTypesTarget_ID(line.GetVAB_DocTypes_ID(), true);
            SetDateInvoiced(line.GetDateInvoiced());
            SetDateAcct(line.GetDateAcct());
            //
            SetSalesRep_ID(batch.GetSalesRep_ID());
            //
            SetVAB_BusinessPartner_ID(line.GetVAB_BusinessPartner_ID());
            SetVAB_BPart_Location_ID(line.GetVAB_BPart_Location_ID());
            SetVAF_UserContact_ID(line.GetVAF_UserContact_ID());
        }

        /// <summary>
        /// Overwrite Client/Org if required
        /// </summary>
        /// <param name="VAF_Client_ID">client</param>
        /// <param name="VAF_Org_ID">org</param>
        //public void SetClientOrg(int VAF_Client_ID, int VAF_Org_ID)
        //{
        //    base.SetClientOrg(VAF_Client_ID, VAF_Org_ID);
        //}

        /// <summary>
        /// Set Business Partner Defaults & Details
        /// </summary>
        /// <param name="bp">business partner</param>
        public void SetBPartner(MVABBusinessPartner bp)
        {
            if (bp == null)
                return;

            SetVAB_BusinessPartner_ID(bp.GetVAB_BusinessPartner_ID());
            //	Set Defaults
            int ii = 0;
            if (IsSOTrx())
                ii = bp.GetVAB_PaymentTerm_ID();
            else
                ii = bp.GetPO_PaymentTerm_ID();
            if (ii != 0)
                SetVAB_PaymentTerm_ID(ii);
            //
            if (IsSOTrx())
                ii = bp.GetVAM_PriceList_ID();
            else
                ii = bp.GetPO_PriceList_ID();
            if (ii != 0)
                SetVAM_PriceList_ID(ii);
            //
            String ss = null;
            if (IsSOTrx())
                ss = bp.GetPaymentRule();
            else
                ss = bp.GetPaymentRulePO();
            if (ss != null)
                SetPaymentRule(ss);


            //	Set Locations
            MVABBPartLocation[] locs = bp.GetLocations(false);
            if (locs != null)
            {
                for (int i = 0; i < locs.Length; i++)
                {
                    if ((locs[i].IsBillTo() && IsSOTrx())
                    || (locs[i].IsPayFrom() && !IsSOTrx()))
                        SetVAB_BPart_Location_ID(locs[i].GetVAB_BPart_Location_ID());
                }
                //	set to first
                if (GetVAB_BPart_Location_ID() == 0 && locs.Length > 0)
                    SetVAB_BPart_Location_ID(locs[0].GetVAB_BPart_Location_ID());
            }
            if (GetVAB_BPart_Location_ID() == 0)
            {
                log.Log(Level.SEVERE, "Has no To Address: " + bp);
            }

            //	Set Contact
            MVAFUserContact[] contacts = bp.GetContacts(false);
            if (contacts != null && contacts.Length > 0)	//	get first User
                SetVAF_UserContact_ID(contacts[0].GetVAF_UserContact_ID());
        }

        /// <summary>
        /// 	Set Order References
        /// </summary>
        /// <param name="order"> order</param>
        public void SetOrder(MVABOrder order)
        {
            if (order == null)
                return;

            SetVAB_Order_ID(order.GetVAB_Order_ID());
            SetIsSOTrx(order.IsSOTrx());
            SetIsDiscountPrinted(order.IsDiscountPrinted());
            SetIsSelfService(order.IsSelfService());
            SetSendEMail(order.IsSendEMail());
            //
            SetVAM_PriceList_ID(order.GetVAM_PriceList_ID());
            if (Util.GetValueOfInt(order.GetVAPOS_POSTerminal_ID()) > 0)
            {
                SetIsTaxIncluded(Util.GetValueOfString(DB.ExecuteScalar("SELECT IsTaxIncluded FROM VAM_PriceList WHERE VAM_PriceList_ID = (SELECT VAM_PriceList_ID FROM VAB_Order WHERE VAB_Order_ID = " + order.GetVAB_Order_ID() + ")")) == "Y");
            }
            else
            {
                SetIsTaxIncluded(order.IsTaxIncluded());
            }
            SetVAB_Currency_ID(order.GetVAB_Currency_ID());
            SetVAB_CurrencyType_ID(order.GetVAB_CurrencyType_ID());
            //

            SetPaymentRule(order.GetPaymentRule());
            SetVAB_PaymentTerm_ID(order.GetVAB_PaymentTerm_ID());
            SetPOReference(order.GetPOReference());
            SetDescription(order.GetDescription());
            SetDateOrdered(order.GetDateOrdered());
            SetPaymentMethod(order.GetPaymentMethod());
            //
            SetVAF_OrgTrx_ID(order.GetVAF_OrgTrx_ID());
            SetVAB_Project_ID(order.GetVAB_Project_ID());
            SetVAB_Promotion_ID(order.GetVAB_Promotion_ID());
            SetVAB_BillingCode_ID(order.GetVAB_BillingCode_ID());
            SetUser1_ID(order.GetUser1_ID());
            SetUser2_ID(order.GetUser2_ID());
        }

        /// <summary>
        /// Set Shipment References
        /// </summary>
        /// <param name="ship">shipment</param>
        public void SetShipment(MVAMInvInOut ship)
        {
            // SetShipment(ship, 0);
            if (ship == null)
                return;

            SetIsSOTrx(ship.IsSOTrx());
            //vikas 9/16/14 Set cb partner 
            MVABOrder ord = new MVABOrder(GetCtx(), ship.GetVAB_Order_ID(), Get_Trx());
            MVABBusinessPartner bp = null;
            if (Util.GetValueOfInt(ship.GetVAB_Order_ID()) > 0)
            {
                bp = new MVABBusinessPartner(GetCtx(), ord.GetBill_BPartner_ID(), Get_Trx());
            }
            else
            {
                //vikas
                bp = new MVABBusinessPartner(GetCtx(), ship.GetVAB_BusinessPartner_ID(), Get_Trx());

            }
            //vikas
            //MBPartner bp = new MBPartner(GetCtx(), ship.GetVAB_BusinessPartner_ID(), null);
            SetBPartner(bp);
            SetVAF_UserContact_ID(ord.GetBill_User_ID());
            //
            SetSendEMail(ship.IsSendEMail());
            //
            SetPOReference(ship.GetPOReference());
            SetDescription(ship.GetDescription());
            SetDateOrdered(ship.GetDateOrdered());
            //
            SetVAF_OrgTrx_ID(ship.GetVAF_OrgTrx_ID());

            // set target document type for purchase cycle
            if (!ship.IsSOTrx())
            {
                MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), ship.GetVAB_DocTypes_ID());
                if (dt.GetVAB_DocTypesInvoice_ID() != 0)
                    SetVAB_DocTypesTarget_ID(dt.GetVAB_DocTypesInvoice_ID(), ship.IsSOTrx());
            }

            // Added by Vivek Kumar on 10/10/2017 advised by Pradeep 
            // Set Dropship true at invoice header
            SetIsDropShip(ship.IsDropShip());
            SetVAB_Project_ID(ship.GetVAB_Project_ID());
            SetVAB_Promotion_ID(ship.GetVAB_Promotion_ID());
            SetVAB_BillingCode_ID(ship.GetVAB_BillingCode_ID());
            SetUser1_ID(ship.GetUser1_ID());
            SetUser2_ID(ship.GetUser2_ID());
            //
            if (ship.GetVAB_Order_ID() != 0)
            {
                SetVAB_Order_ID(ship.GetVAB_Order_ID());
                MVABOrder order = new MVABOrder(GetCtx(), ship.GetVAB_Order_ID(), Get_TrxName());
                SetIsDiscountPrinted(order.IsDiscountPrinted());
                SetDateOrdered(order.GetDateOrdered());
                SetVAM_PriceList_ID(order.GetVAM_PriceList_ID());
                // Change for POS
                if (Util.GetValueOfInt(order.GetVAPOS_POSTerminal_ID()) > 0)
                    SetIsTaxIncluded(Util.GetValueOfString(DB.ExecuteScalar("SELECT IsTaxIncluded FROM VAM_PriceList WHERE VAM_PriceList_ID = (SELECT VAM_PriceList_ID FROM VAB_Order WHERE VAB_Order_ID = " + order.GetVAB_Order_ID() + ")")) == "Y");
                else
                    SetIsTaxIncluded(order.IsTaxIncluded());
                SetVAB_Currency_ID(order.GetVAB_Currency_ID());
                SetVAB_CurrencyType_ID(order.GetVAB_CurrencyType_ID());
                SetPaymentRule(order.GetPaymentRule());
                SetVAB_PaymentTerm_ID(order.GetVAB_PaymentTerm_ID());
                //
                MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), order.GetVAB_DocTypes_ID());
                if (dt.GetVAB_DocTypesInvoice_ID() != 0)
                    SetVAB_DocTypesTarget_ID(dt.GetVAB_DocTypesInvoice_ID(), true);
                //	Overwrite Invoice Address
                SetVAB_BPart_Location_ID(order.GetBill_Location_ID());
            }
        }

        /// <summary>
        /// Set Shipment References
        /// </summary>
        /// <param name="ship">shipment</param>
        /// <param name="VAB_Order_ID">Order reference for invoice creation</param>
        //public void SetShipment(MVAMInvInOut ship, int VAB_Order_ID)
        //{
        //    if (ship == null)
        //        return;

        //    SetIsSOTrx(ship.IsSOTrx());
        //    MOrder ord = new MOrder(GetCtx(), VAB_Order_ID , Get_Trx());
        //    MBPartner bp = null;
        //    if (VAB_Order_ID > 0)
        //    {
        //        bp = new MBPartner(GetCtx(), ord.GetBill_BPartner_ID(), null);
        //    }
        //    else
        //    {
        //        bp = new MBPartner(GetCtx(), ship.GetVAB_BusinessPartner_ID(), null);

        //    }

        //    SetBPartner(bp);
        //    SetVAF_UserContact_ID(ship.GetVAF_UserContact_ID());
        //    //
        //    SetSendEMail(ship.IsSendEMail());
        //    //
        //    SetPOReference(ship.GetPOReference());
        //    SetDescription(ship.GetDescription());
        //    SetDateOrdered(ship.GetDateOrdered());
        //    //
        //    SetVAF_OrgTrx_ID(ship.GetVAF_OrgTrx_ID());

        //    // set target document type for purchase cycle
        //    if (!ship.IsSOTrx())
        //    {
        //        MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), ship.GetVAB_DocTypes_ID());
        //        if (dt.GetVAB_DocTypesInvoice_ID() != 0)
        //            SetVAB_DocTypesTarget_ID(dt.GetVAB_DocTypesInvoice_ID(), ship.IsSOTrx());
        //    }

        //    // Added by Vivek Kumar on 10/10/2017 advised by Pradeep 
        //    // Set Dropship true at invoice header
        //    SetIsDropShip(ship.IsDropShip());
        //    SetVAB_Project_ID(ship.GetVAB_Project_ID());
        //    SetVAB_Promotion_ID(ship.GetVAB_Promotion_ID());
        //    SetVAB_BillingCode_ID(ship.GetVAB_BillingCode_ID());
        //    SetUser1_ID(ship.GetUser1_ID());
        //    SetUser2_ID(ship.GetUser2_ID());
        //    //
        //    if (VAB_Order_ID != 0)
        //    {
        //        SetVAB_Order_ID(ord.GetVAB_Order_ID());
        //        // MOrder order = new MOrder(GetCtx(), ship.GetVAB_Order_ID(), Get_TrxName());
        //        SetIsDiscountPrinted(ord.IsDiscountPrinted());
        //        SetDateOrdered(ord.GetDateOrdered());
        //        SetVAM_PriceList_ID(ord.GetVAM_PriceList_ID());
        //        // Change for POS
        //        if (Util.GetValueOfInt(ord.GetVAPOS_POSTerminal_ID()) > 0)
        //            SetIsTaxIncluded(Util.GetValueOfString(DB.ExecuteScalar("SELECT IsTaxIncluded FROM VAM_PriceList WHERE VAM_PriceList_ID = (SELECT VAM_PriceList_ID FROM VAB_Order WHERE VAB_Order_ID = " + ord.GetVAB_Order_ID() + ")")) == "Y");
        //        else
        //            SetIsTaxIncluded(ord.IsTaxIncluded());
        //        SetVAB_Currency_ID(ord.GetVAB_Currency_ID());
        //        SetVAB_CurrencyType_ID(ord.GetVAB_CurrencyType_ID());
        //        SetPaymentRule(ord.GetPaymentRule());
        //        SetVAB_PaymentTerm_ID(ord.GetVAB_PaymentTerm_ID());
        //        //
        //        MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), ord.GetVAB_DocTypes_ID());
        //        if (dt.GetVAB_DocTypesInvoice_ID() != 0)
        //            SetVAB_DocTypesTarget_ID(dt.GetVAB_DocTypesInvoice_ID(), true);
        //        //	Overwrite Invoice Address
        //        SetVAB_BPart_Location_ID(ord.GetBill_Location_ID());
        //        SetVAF_UserContact_ID(ord.GetBill_User_ID());
        //    }
        //}

        /// <summary>
        /// Set Target Document Type
        /// </summary>
        /// <param name="DocBaseType">doc type MVABMasterDocType.DOCBASETYPE_</param>
        public void SetVAB_DocTypesTarget_ID(String DocBaseType)
        {
            //String sql = "SELECT VAB_DocTypes_ID FROM VAB_DocTypes "
            //    + "WHERE VAF_Client_ID=@param1 AND DocBaseType=@param2"
            //    + " AND IsActive='Y' "
            //    + "ORDER BY IsDefault DESC";
            String sql = "SELECT VAB_DocTypes_ID FROM VAB_DocTypes "
               + "WHERE VAF_Client_ID=@param1 AND DocBaseType=@param2"
               + " AND IsActive='Y' AND IsExpenseInvoice = 'N' "
               + "ORDER BY VAB_DocTypes_ID DESC ,   IsDefault DESC";
            int VAB_DocTypes_ID = DataBase.DB.GetSQLValue(null, sql, GetVAF_Client_ID(), DocBaseType);
            if (VAB_DocTypes_ID <= 0)
            {
                log.Log(Level.SEVERE, "Not found for AC_Client_ID="
                    + GetVAF_Client_ID() + " - " + DocBaseType);
            }
            else
            {
                log.Fine(DocBaseType);
                SetVAB_DocTypesTarget_ID(VAB_DocTypes_ID);
                bool isSOTrx = MVABMasterDocType.DOCBASETYPE_ARINVOICE.Equals(DocBaseType)
                    || MVABMasterDocType.DOCBASETYPE_ARCREDITMEMO.Equals(DocBaseType);
                SetIsSOTrx(isSOTrx);
                bool isReturnTrx = MVABMasterDocType.DOCBASETYPE_ARCREDITMEMO.Equals(DocBaseType)
                    || MVABMasterDocType.DOCBASETYPE_APCREDITMEMO.Equals(DocBaseType);
                SetIsReturnTrx(isReturnTrx);
            }
        }

        /// <summary>
        /// Set Target Document Type.
        //Based on SO flag AP/AP Invoice
        /// </summary>
        public void SetVAB_DocTypesTarget_ID()
        {
            if (GetVAB_DocTypesTarget_ID() > 0)
                return;
            if (IsSOTrx())
                SetVAB_DocTypesTarget_ID(MVABMasterDocType.DOCBASETYPE_ARINVOICE);
            else
                SetVAB_DocTypesTarget_ID(MVABMasterDocType.DOCBASETYPE_APINVOICE);
        }

        /// <summary>
        /// Set Target Document Type
        /// </summary>
        /// <param name="VAB_DocTypesTarget_ID"></param>
        /// <param name="setReturnTrx">if true set ReturnTrx and SOTrx</param>
        public void SetVAB_DocTypesTarget_ID(int VAB_DocTypesTarget_ID, bool setReturnTrx)
        {
            base.SetVAB_DocTypesTarget_ID(VAB_DocTypesTarget_ID);
            if (setReturnTrx)
            {
                MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), VAB_DocTypesTarget_ID);
                SetIsSOTrx(dt.IsSOTrx());
                SetIsReturnTrx(dt.IsReturnTrx());
            }
        }

        /// <summary>
        /// Get Grand Total
        /// </summary>
        /// <param name="creditMemoAdjusted">adjusted for CM (negative)</param>
        /// <returns>grand total</returns>
        public Decimal GetGrandTotal(bool creditMemoAdjusted)
        {
            if (!creditMemoAdjusted)
                return base.GetGrandTotal();
            //
            Decimal amt = GetGrandTotal();
            if (IsCreditMemo())
            {
                //return amt * -1;// amt.negate();
                return Decimal.Negate(amt);
            }
            return amt;
        }

        /// <summary>
        /// Get Invoice Lines of Invoice
        /// </summary>
        /// <param name="whereClause">starting with AND</param>
        /// <returns>lines</returns>
        private MVABInvoiceLine[] GetLines(String whereClause)
        {
            List<MVABInvoiceLine> list = new List<MVABInvoiceLine>();
            String sql = "SELECT * FROM VAB_InvoiceLine WHERE VAB_Invoice_ID= " + GetVAB_Invoice_ID();
            if (whereClause != null)
                sql += whereClause;
            sql += " ORDER BY Line";
            // commented - bcz freight is distributed based on avoalbel qty
            //sql += " ORDER BY VAM_Product_ID DESC "; // for picking all charge line first, than product
            try
            {
                DataSet ds = DataBase.DB.ExecuteDataset(sql, null, Get_TrxName());
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    MVABInvoiceLine il = new MVABInvoiceLine(GetCtx(), dr, Get_TrxName());
                    il.SetInvoice(this);
                    list.Add(il);
                }
                ds = null;
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "getLines", e);
            }

            MVABInvoiceLine[] lines = new MVABInvoiceLine[list.Count];
            lines = list.ToArray();
            return lines;
        }

        /// <summary>
        /// Get Invoice Lines
        /// </summary>
        /// <param name="requery">requery</param>
        /// <returns>lines</returns>
        public MVABInvoiceLine[] GetLines(bool requery)
        {
            if (_lines == null || _lines.Length == 0 || requery)
                _lines = GetLines(null);
            return _lines;
        }

        /// <summary>
        /// Get Lines of Invoice
        /// </summary>
        /// <returns>lines</returns>
        public MVABInvoiceLine[] GetLines()
        {
            return GetLines(false);
        }

        /// <summary>
        /// Renumber Lines
        /// </summary>
        /// <param name="step">start and step</param>
        public void RenumberLines(int step)
        {
            int number = step;
            MVABInvoiceLine[] lines = GetLines(false);
            for (int i = 0; i < lines.Length; i++)
            {
                MVABInvoiceLine line = lines[i];
                line.SetLine(number);
                line.Save();
                number += step;
            }
            _lines = null;
        }

        /// <summary>
        /// Copy Lines From other Invoice.
        /// </summary>
        /// <param name="otherInvoice">invoice</param>
        /// <param name="counter">create counter links</param>
        /// <param name="setOrder">set order links</param>
        /// <returns>number of lines copied</returns>
        public int CopyLinesFrom(MVABInvoice otherInvoice, bool counter, bool setOrder)
        {
            if (IsProcessed() || IsPosted() || otherInvoice == null)
            {
                return 0;
            }
            MVABInvoiceLine[] fromLines = otherInvoice.GetLines(false);
            int count = 0;
            for (int i = 0; i < fromLines.Length; i++)
            {
                MVABInvoiceLine line = new MVABInvoiceLine(GetCtx(), 0, Get_TrxName());
                MVABInvoiceLine fromLine = fromLines[i];
                if (counter)	//	header
                    PO.CopyValues(fromLine, line, GetVAF_Client_ID(), GetVAF_Org_ID());
                else
                    PO.CopyValues(fromLine, line, fromLine.GetVAF_Client_ID(), fromLine.GetVAF_Org_ID());
                line.SetVAB_Invoice_ID(GetVAB_Invoice_ID());
                line.SetInvoice(this);
                line.Set_ValueNoCheck("VAB_InvoiceLine_ID", I_ZERO);	// new
                //	Reset

                line.SetRef_InvoiceLine_ID(0);

                // enhanced by Amit 4-1-2016
                //line.SetVAM_PFeature_SetInstance_ID(0);
                //line.SetVAM_Inv_InOutLine_ID(0);
                //if (!setOrder)
                //    line.SetVAB_OrderLine_ID(0);
                //end
                line.SetA_Asset_ID(0);

                line.SetVAS_Res_Assignment_ID(0);
                //	New Tax
                if (GetVAB_BusinessPartner_ID() != otherInvoice.GetVAB_BusinessPartner_ID())
                    line.SetTax();	//	recalculate
                //

                // JID_1319: System should not copy Tax Amount, Line Total Amount and Taxable Amount field. System Should Auto Calculate thease field On save of lines.
                if (GetVAM_PriceList_ID() != otherInvoice.GetVAM_PriceList_ID())
                    line.SetTaxAmt();       //	recalculate Tax Amount

                // ReCalculate Surcharge Amount
                if (line.Get_ColumnIndex("SurchargeAmt") > 0)
                {
                    line.SetSurchargeAmt(Env.ZERO);
                }

                if (counter)
                {
                    line.SetRef_InvoiceLine_ID(fromLine.GetVAB_InvoiceLine_ID());
                    line.SetVAB_OrderLine_ID(0);
                    if (fromLine.GetVAB_OrderLine_ID() != 0)
                    {
                        MVABOrderLine peer = new MVABOrderLine(GetCtx(), fromLine.GetVAB_OrderLine_ID(), Get_TrxName());
                        if (peer.GetRef_OrderLine_ID() != 0)
                            line.SetVAB_OrderLine_ID(peer.GetRef_OrderLine_ID());
                    }
                    line.SetVAM_Inv_InOutLine_ID(0);
                    if (fromLine.GetVAM_Inv_InOutLine_ID() != 0)
                    {
                        MVAMInvInOutLine peer = new MVAMInvInOutLine(GetCtx(), fromLine.GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                        if (peer.GetRef_InOutLine_ID() != 0)
                            line.SetVAM_Inv_InOutLine_ID(peer.GetRef_InOutLine_ID());
                    }
                }
                else
                {
                    line.SetVAB_OrderLine_ID(0);
                    line.SetVAM_Inv_InOutLine_ID(0);
                }

                // to set OrderLine and InoutLine in case of reversal if it is available 
                if (IsReversal())
                {
                    line.SetVAB_OrderLine_ID(fromLine.GetVAB_OrderLine_ID());
                    line.SetVAM_Inv_InOutLine_ID(fromLine.GetVAM_Inv_InOutLine_ID());
                }
                //end 

                line.SetProcessed(false);
                if (line.Save(Get_TrxName()))
                {
                    count++;
                    CopyLandedCostAllocation(fromLine.GetVAB_InvoiceLine_ID(), line.GetVAB_InvoiceLine_ID());

                    try
                    {
                        // update VAM_MatchInvoiceoiceCostTrack with reverse invoice line ref for costing calculation
                        string sql = "Update VAM_MatchInvoiceoiceCostTrack SET Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID() +
                                        " WHERE VAB_InvoiceLine_ID = " + fromLine.GetVAB_InvoiceLine_ID();
                        int no = DB.ExecuteQuery(sql, null, Get_TrxName());
                    }
                    catch { }
                }
                //	Cross Link
                if (counter)
                {
                    fromLine.SetRef_InvoiceLine_ID(line.GetVAB_InvoiceLine_ID());
                    fromLine.Save(Get_TrxName());
                }
            }
            if (fromLines.Length != count)
            {
                log.Log(Level.SEVERE, "Line difference - From=" + fromLines.Length + " <> Saved=" + count);
            }
            return count;
        }

        /// <summary>
        /// is used to copy record of landed cost
        /// </summary>
        /// <param name="VAB_InvoiceLine_Id"></param>
        /// <param name="to_VAB_InvoiceLine_ID"></param>
        private void CopyLandedCostAllocation(int VAB_InvoiceLine_Id, int to_VAB_InvoiceLine_ID)
        {
            DataSet ds = new DataSet();
            try
            {
                // Create Landed Cost
                string sql = "SELECT * FROM VAB_LCost WHERE  VAB_InvoiceLine_Id = " + VAB_InvoiceLine_Id;
                ds = (DB.ExecuteDataset(sql, null, Get_Trx()));
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        MVABLCost lc = new MVABLCost(GetCtx(), 0, Get_Trx());
                        lc.SetVAF_Client_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAF_Client_ID"]));
                        lc.SetVAF_Org_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAF_Org_ID"]));
                        lc.SetVAB_InvoiceLine_ID(to_VAB_InvoiceLine_ID);
                        lc.SetDescription(Util.GetValueOfString(ds.Tables[0].Rows[i]["Description"]));
                        lc.SetLandedCostDistribution(Util.GetValueOfString(ds.Tables[0].Rows[i]["LandedCostDistribution"]));
                        lc.SetVAM_ProductCostElement_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_ProductCostElement_ID"]));
                        lc.SetVAM_Inv_InOut_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_Inv_InOut_ID"]));
                        lc.SetVAM_Inv_InOutLine_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_Inv_InOutLine_ID"]));
                        lc.SetVAM_Product_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_Product_ID"]));
                        lc.SetProcessing(false);
                        if (lc.Get_ColumnIndex("ReversalDoc_ID") > 0)
                        {
                            lc.SetReversalDoc_ID(VAB_InvoiceLine_Id);
                        }
                        if (!lc.Save())
                        {
                            ValueNamePair pp = VLogger.RetrieveError();
                            if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                            {
                                _processMsg = pp.GetName();
                            }
                            log.SaveError(_processMsg + " , " + "VIS_LandedCostnotSaved", "");
                            log.Info(_processMsg + " , " + "VIS_LandedCostnotSaved");
                            return;
                        }
                    }
                }
                ds.Dispose();

                ds = null;
                // Create Landed Cost Allocation
                sql = "SELECT * FROM VAB_LCostDistribution WHERE  VAB_InvoiceLine_Id = " + VAB_InvoiceLine_Id;
                ds = (DB.ExecuteDataset(sql, null, Get_Trx()));
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        MVABLCostDistribution lca = new MVABLCostDistribution(GetCtx(), 0, Get_Trx());
                        lca.SetVAF_Client_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAF_Client_ID"]));
                        lca.SetVAF_Org_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAF_Org_ID"]));
                        lca.SetVAB_InvoiceLine_ID(to_VAB_InvoiceLine_ID);
                        lca.SetVAM_ProductCostElement_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_ProductCostElement_ID"]));
                        lca.SetVAM_Product_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_Product_ID"]));
                        lca.SetVAM_PFeature_SetInstance_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_PFeature_SetInstance_ID"]));
                        lca.SetQty(Decimal.Negate(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["Qty"])));
                        lca.SetAmt(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["Amt"]));
                        lca.SetBase(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["Base"]));
                        if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                        {
                            lca.SetVAM_Warehouse_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_Warehouse_ID"]));
                        }
                        if (!lca.Save(Get_Trx()))
                        {
                            ValueNamePair pp = VLogger.RetrieveError();
                            if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                            {
                                _processMsg = pp.GetName();
                            }
                            log.SaveError(_processMsg + " , " + "VIS_LandedCostAllocationnotSaved", "");
                            log.Log(Level.SEVERE, _processMsg + " , " + "VIS_LandedCostAllocationnotSaved");
                        }
                        return;
                    }
                }
                ds.Dispose();
            }
            catch (Exception ex)
            {
                if (ds != null)
                {
                    ds.Dispose();
                }
                log.Log(Level.SEVERE, "LandedCost Allocation Not Created For Invoice " + VAB_InvoiceLine_Id + " , " + ex.Message);
            }
        }

        /**
         * 	Set Reversal
         *	@param reversal reversal
         */
        public void SetReversal(bool reversal)
        {
            _reversal = reversal;
        }

        /**
         * 	Is Reversal
         *	@return reversal
         */
        private bool IsReversal()
        {
            return _reversal;
        }

        /**
         * 	Get Taxes
         *	@param requery requery
         *	@return array of taxes
         */
        public MVABTaxInvoice[] GetTaxes(bool requery)
        {
            if (_taxes != null && !requery)
                return _taxes;
            String sql = "SELECT * FROM VAB_Tax_Invoice WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
            List<MVABTaxInvoice> list = new List<MVABTaxInvoice>();
            DataSet ds = null;
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, Get_TrxName());
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    list.Add(new MVABTaxInvoice(GetCtx(), dr, Get_TrxName()));
                }
                ds = null;
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "getTaxes", e);
            }
            finally
            {
                ds = null;
            }

            _taxes = new MVABTaxInvoice[list.Count];
            _taxes = list.ToArray();
            return _taxes;
        }

        /**
         * 	Add to Description
         *	@param description text
         */
        public void AddDescription(String description)
        {
            String desc = GetDescription();
            if (desc == null)
                SetDescription(description);
            else
                SetDescription(desc + " | " + description);
        }

        /**
         * 	Is it a Credit Memo?
         *	@return true if CM
         */
        public bool IsCreditMemo()
        {
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(),
                GetVAB_DocTypes_ID() == 0 ? GetVAB_DocTypesTarget_ID() : GetVAB_DocTypes_ID());
            return MVABMasterDocType.DOCBASETYPE_APCREDITMEMO.Equals(dt.GetDocBaseType())
                || MVABMasterDocType.DOCBASETYPE_ARCREDITMEMO.Equals(dt.GetDocBaseType());
        }

        /**
         * 	Set Processed.
         * 	Propergate to Lines/Taxes
         *	@param processed processed
         */
        public new void SetProcessed(bool processed)
        {
            base.SetProcessed(processed);
            if (Get_ID() == 0)
                return;
            String set = "SET Processed='"
                + (processed ? "Y" : "N")
                + "' WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
            int noLine = DataBase.DB.ExecuteQuery("UPDATE VAB_InvoiceLine " + set, null, Get_Trx());
            int noTax = DataBase.DB.ExecuteQuery("UPDATE VAB_Tax_Invoice " + set, null, Get_Trx());
            _lines = null;
            _taxes = null;
            log.Fine(processed + " - Lines=" + noLine + ", Tax=" + noTax);
        }

        /**
         * 	Validate Invoice Pay Schedule
         *	@return pay schedule is valid
         */
        public bool ValidatePaySchedule()
        {
            MVABSchedInvoicePayment[] schedule = MVABSchedInvoicePayment.GetInvoicePaySchedule
                (GetCtx(), GetVAB_Invoice_ID(), 0, Get_Trx());
            log.Fine("#" + schedule.Length);
            if (schedule.Length == 0)
            {
                SetIsPayScheduleValid(false);
                return false;
            }
            //	Add up due amounts
            Decimal total = Env.ZERO;
            for (int i = 0; i < schedule.Length; i++)
            {
                Decimal due = 0;
                schedule[i].SetParent(this);
                if (schedule[i].GetVA009_Variance() < 0)
                {
                    due = decimal.Add(schedule[i].GetDueAmt(), decimal.Negate(schedule[i].GetVA009_Variance()));
                }
                else
                {
                    due = schedule[i].GetDueAmt();
                }
                //if (due != null)
                total = Decimal.Add(total, due);
            }
            bool valid = (Get_ColumnIndex("GrandTotalAfterWithholding") > 0
                && GetGrandTotalAfterWithholding() != 0 ? GetGrandTotalAfterWithholding() : GetGrandTotal()).CompareTo(total) == 0;
            SetIsPayScheduleValid(valid);

            //	Update Schedule Lines
            for (int i = 0; i < schedule.Length; i++)
            {
                if (schedule[i].IsValid() != valid)
                {
                    schedule[i].SetIsValid(valid);
                    schedule[i].Save(Get_Trx());
                }
            }
            return valid;
        }

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">newRecord new</param>
        /// <returns>true</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            //	No Partner Info - set Template
            if (GetVAB_BusinessPartner_ID() == 0)
                SetBPartner(MVABBusinessPartner.GetTemplate(GetCtx(), GetVAF_Client_ID()));
            if (GetVAB_BPart_Location_ID() == 0)
                SetBPartner(new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), null));

            // Check Unique column marked on Database table
            StringBuilder colHeaders = new StringBuilder("");
            int displayType;
            string colName;
            StringBuilder sb = new StringBuilder("SELECT ColumnName, Name FROM VAF_Column WHERE IsActive = 'Y' AND IsUnique = 'Y' AND  VAF_TableView_ID = " + Get_Table_ID());
            DataSet UnqFields = DB.ExecuteDataset(sb.ToString(), null, Get_Trx());

            if (UnqFields != null && UnqFields.Tables[0].Rows.Count > 0)
            {
                sb.Clear();
                for (int l = 0; l < UnqFields.Tables[0].Rows.Count; l++)
                {
                    if (sb.Length == 0)
                    {
                        sb.Append(" SELECT COUNT(1) FROM ");
                        sb.Append(Get_TableName()).Append(" WHERE ");
                    }
                    else
                    {
                        sb.Append(" AND ");
                        colHeaders.Append(", ");
                    }

                    colName = Util.GetValueOfString(UnqFields.Tables[0].Rows[l]["ColumnName"]);
                    colHeaders.Append(UnqFields.Tables[0].Rows[l]["Name"]);
                    object colval = Get_Value(colName);
                    displayType = Get_ColumnDisplayType(Get_ColumnIndex(colName));

                    if (colval == null || colval == DBNull.Value)
                    {
                        sb.Append(UnqFields.Tables[0].Rows[l]["ColumnName"]).Append(" IS NULL ");
                    }
                    else
                    {
                        sb.Append(UnqFields.Tables[0].Rows[l]["ColumnName"]).Append(" = ");
                        if (DisplayType.IsID(displayType))
                        {
                            sb.Append(colval);
                        }
                        else if (DisplayType.IsDate(displayType))
                        {

                            sb.Append(DB.TO_DATE(Convert.ToDateTime(colval), DisplayType.Date == displayType));
                        }
                        else if (DisplayType.YesNo == displayType)
                        {
                            string boolval = "N";
                            if (VAdvantage.Utility.Util.GetValueOfBool(colval))
                                boolval = "Y";
                            sb.Append("'").Append(boolval).Append("'");
                        }
                        else
                        {
                            sb.Append("'").Append(colval).Append("'");
                        }
                    }
                }

                sb.Append(" AND " + Get_TableName() + "_ID != " + Get_ID());

                //Check unique record in DB 
                int count = Util.GetValueOfInt(DB.ExecuteScalar(sb.ToString(), null, Get_Trx()));
                sb = null;
                if (count > 0)
                {
                    log.SaveError("SaveErrorNotUnique", colHeaders.ToString());
                    return false;

                }
            }

            //APInvoice Case: invoice Reference can't be same for same financial year and Business Partner and DoCTypeTarget and DateAcct      
            if ((Is_ValueChanged("DateAcct") || Is_ValueChanged("VAB_BusinessPartner_ID") || Is_ValueChanged("VAB_DocTypesTarget_ID") || Is_ValueChanged("InvoiceReference")) && !IsSOTrx() && checkFinancialYear() > 0)
            {
                log.SaveError("", Msg.GetMsg(GetCtx(), "InvoiceReferenceExist"));
                return false;
            }

            //	Price List
            if (GetVAM_PriceList_ID() == 0)
            {
                int ii = GetCtx().GetContextAsInt("#VAM_PriceList_ID");
                if (ii != 0)
                    SetVAM_PriceList_ID(ii);
                else
                {
                    String sql = "SELECT VAM_PriceList_ID FROM VAM_PriceList WHERE VAF_Client_ID=@param1 AND IsDefault='Y'";
                    ii = DataBase.DB.GetSQLValue(null, sql, GetVAF_Client_ID());
                    if (ii != 0)
                        SetVAM_PriceList_ID(ii);
                }
            }

            //	Currency
            if (GetVAB_Currency_ID() == 0)
            {
                String sql = "SELECT VAB_Currency_ID FROM VAM_PriceList WHERE VAM_PriceList_ID=@param1";
                int ii = DataBase.DB.GetSQLValue(null, sql, GetVAM_PriceList_ID());
                if (ii != 0)
                    SetVAB_Currency_ID(ii);
                else
                    SetVAB_Currency_ID(GetCtx().GetContextAsInt("#VAB_Currency_ID"));
            }

            //	Sales Rep
            if (GetSalesRep_ID() == 0)
            {
                int ii = GetCtx().GetContextAsInt("#SalesRep_ID");
                if (ii != 0)
                    SetSalesRep_ID(ii);
            }

            //	Document Type
            if (GetVAB_DocTypes_ID() == 0)
                SetVAB_DocTypes_ID(0);	//	make sure it's set to 0
            if (GetVAB_DocTypesTarget_ID() == 0)
                SetVAB_DocTypesTarget_ID(IsSOTrx() ? MVABMasterDocType.DOCBASETYPE_ARINVOICE : MVABMasterDocType.DOCBASETYPE_APINVOICE);

            //JID_0244 -- On Invoice, set value of checkbox"Treat As Discount" based on document type. 
            if (Get_ColumnIndex("TreatAsDiscount") >= 0 && !IsSOTrx())
            {
                SetTreatAsDiscount(MVABDocTypes.Get(GetCtx(), GetVAB_DocTypesTarget_ID()).IsTreatAsDiscount());
            }

            //	Payment Term
            if (GetVAB_PaymentTerm_ID() == 0)
            {
                int ii = GetCtx().GetContextAsInt("#VAB_PaymentTerm_ID");
                if (ii != 0)
                    SetVAB_PaymentTerm_ID(ii);
                else
                {
                    String sql = "SELECT VAB_PaymentTerm_ID FROM VAB_PaymentTerm WHERE VAF_Client_ID=@param1 AND IsDefault='Y'";
                    ii = DataBase.DB.GetSQLValue(null, sql, GetVAF_Client_ID());
                    if (ii != 0)
                        SetVAB_PaymentTerm_ID(ii);
                }
            }
            //	BPartner Active
            if (newRecord || Is_ValueChanged("VAB_BusinessPartner_ID"))
            {
                MVABBusinessPartner bp = MVABBusinessPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID());
                if (!bp.IsActive())
                {
                    log.SaveWarning("NotActive", Msg.GetMsg(GetCtx(), "VAB_BusinessPartner_ID"));
                    return false;
                }
            }


            // If lines are available and user is changing the pricelist/conversiontype on header than we have to restrict it because
            // those lines are saved as privious pricelist prices or Payment term.. standard sheet issue no : SI_0344 / JID_0564 / JID_1536_1 by Manjot
            if (!newRecord && (Is_ValueChanged("VAM_PriceList_ID") || Is_ValueChanged("VAB_CurrencyType_ID")))
            {
                MVABInvoiceLine[] lines = GetLines(true);

                if (lines.Length > 0)
                {
                    // Please Delete Lines First
                    log.SaveWarning("", Msg.GetMsg(GetCtx(), "VIS_CantChange"));
                    return false;
                }
            }

            if (!newRecord && Is_ValueChanged("VAB_PaymentTerm_ID") && Env.IsModuleInstalled("VA009_"))
            {
                MVABInvoiceLine[] lines = GetLines(true);

                // check payment term is advance, if advance - and user try to changes payment term then not allowed
                bool isAdvance = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT SUM( CASE
                            WHEN VAB_Paymentterm.VA009_Advance!= COALESCE(VAB_PaymentSchedule.VA009_Advance,'N') THEN 1 ELSE 0 END) AS isAdvance
                        FROM VAB_Paymentterm LEFT JOIN VAB_PaymentSchedule ON VAB_Paymentterm.VAB_Paymentterm_ID = VAB_PaymentSchedule.VAB_Paymentterm_ID
                        WHERE VAB_Paymentterm.VAB_Paymentterm_ID = " + GetVAB_PaymentTerm_ID(), null, Get_Trx())) > 0 ? true : false;

                // check old payment term is advance, if advance - and user try to changes payment term then not allowed
                bool isOldAdvance = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT SUM( CASE
                            WHEN VAB_Paymentterm.VA009_Advance!= COALESCE(VAB_PaymentSchedule.VA009_Advance,'N') THEN 1 ELSE 0 END) AS isAdvance
                        FROM VAB_Paymentterm LEFT JOIN VAB_PaymentSchedule ON VAB_Paymentterm.VAB_Paymentterm_ID = VAB_PaymentSchedule.VAB_Paymentterm_ID
                        WHERE VAB_Paymentterm.VAB_Paymentterm_ID = " + Convert.ToInt32(Get_ValueOld("VAB_PaymentTerm_ID")), null, Get_Trx())) > 0 ? true : false;

                // when old payment term id (Advanced) not matched with new payment term id (not advanced)
                if (lines.Length > 0 && isAdvance != isOldAdvance)
                {
                    log.SaveWarning("", Msg.GetMsg(GetCtx(), "VIS_CantChange"));
                    return false;
                }

                if (lines.Length > 0 && isAdvance)
                {
                    // Please Delete Lines First
                    log.SaveWarning("", Msg.GetMsg(GetCtx(), "VIS_CantChange"));
                    return false;
                }
            }

            // set backup withholding tax amount
            if (!IsProcessing() && Get_ColumnIndex("VAB_Withholding_ID") > 0 && GetVAB_Withholding_ID() > 0)
            {
                if (!SetWithholdingAmount(this))
                {
                    log.SaveError("Error", Msg.GetMsg(GetCtx(), "WrongWithholdingTax"));
                    return false;
                }
            }

            //Added by Bharat for Credit Limit on 24/08/2016
            //not to check credit limit after invoice completion
            if (IsSOTrx() && !IsReversal() && !(GetDocStatus() == DOCSTATUS_Completed || GetDocStatus() == DOCSTATUS_Closed))
            {
                MVABBusinessPartner bp = MVABBusinessPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID());
                if (bp.GetCreditStatusSettingOn() == "CH")
                {
                    decimal creditLimit = bp.GetSO_CreditLimit();
                    string creditVal = bp.GetCreditValidation();
                    if (creditLimit != 0)
                    {
                        decimal creditAvlb = creditLimit - bp.GetTotalOpenBalance();
                        if (creditAvlb <= 0)
                        {
                            //if (creditVal == "C" || creditVal == "D" || creditVal == "F")
                            //{
                            //    log.SaveError("Error", Msg.GetMsg(GetCtx(), "CreditUsedInvoice"));
                            //    return false;
                            //}
                            //else if (creditVal == "I" || creditVal == "J" || creditVal == "L")
                            //{
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "CreditOver"));
                            //return false;
                            //}
                        }
                    }
                }
                // JID_0161 // change here now will check credit settings on field only on Business Partner Header // Lokesh Chauhan 15 July 2019
                else if (bp.GetCreditStatusSettingOn() == X_VAB_BusinessPartner.CREDITSTATUSSETTINGON_CustomerLocation)
                {
                    MVABBPartLocation bpl = new MVABBPartLocation(GetCtx(), GetVAB_BPart_Location_ID(), null);
                    //if (bpl.GetCreditStatusSettingOn() == "CL")
                    //{
                    decimal creditLimit = bpl.GetSO_CreditLimit();
                    string creditVal = bpl.GetCreditValidation();
                    if (creditLimit != 0)
                    {
                        decimal creditAvlb = creditLimit - bpl.GetSO_CreditUsed();
                        if (creditAvlb <= 0)
                        {
                            //if (creditVal == "C" || creditVal == "D" || creditVal == "F")
                            //{
                            //    log.SaveError("Error", Msg.GetMsg(GetCtx(), "CreditUsedInvoice"));
                            //    return false;
                            //}
                            //else if (creditVal == "I" || creditVal == "J" || creditVal == "L")
                            //{
                            log.SaveError("Warning", Msg.GetMsg(GetCtx(), "CreditOver"));
                            //}
                        }
                    }
                    //}   
                }
            }

            return true;
        }

        /**
         * 	Before Delete
         *	@return true if it can be deleted
         */
        protected override bool BeforeDelete()
        {
            if (GetVAB_Order_ID() != 0)
            {
                log.SaveError("Error", Msg.GetMsg(GetCtx(), "CannotDelete"));
                return false;
            }
            return true;
        }

        /**
         * 	String Representation
         *	@return Info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MVABInvoice[")
                .Append(Get_ID()).Append("-").Append(GetDocumentNo())
                .Append(",GrandTotal=").Append(GetGrandTotal());
            if (_lines != null)
                sb.Append(" (#").Append(_lines.Length).Append(")");
            sb.Append("]");
            return sb.ToString();
        }

        /**
         * 	Get Document Info
         *	@return document Info (untranslated)
         */
        public String GetDocumentInfo()
        {
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
            return dt.GetName() + " " + GetDocumentNo();
        }

        /**
         * 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return success
         */
        protected override bool AfterSave(bool newRecord, bool success)
        {
            //if (!success || newRecord)
            if (!success)
                return success;

            if (!newRecord)
            {
                if (Is_ValueChanged("VAF_Org_ID"))
                {
                    String sql = "UPDATE VAB_InvoiceLine ol"
                        + " SET VAF_Org_ID ="
                            + "(SELECT VAF_Org_ID"
                            + " FROM VAB_Invoice o WHERE ol.VAB_Invoice_ID=o.VAB_Invoice_ID) "
                        + "WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                    int no = DataBase.DB.ExecuteQuery(sql, null, Get_Trx());
                    log.Fine("Lines -> #" + no);
                }

                // If User unselect Hold Payment on Invoice header, system should also mark all the payment schedule as false and vice versa
                if (Get_ColumnIndex("IsHoldPayment") >= 0 && Is_ValueChanged("IsHoldPayment"))
                {
                    int no = DB.ExecuteQuery(@"UPDATE VAB_sched_InvoicePayment SET IsHoldPayment = '" + (IsHoldPayment() ? "Y" : "N") + @"' WHERE NVL(VAB_Payment_ID , 0) = 0
                                AND NVL(VAB_CashJRNLLine_ID , 0) = 0 AND VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                    log.Fine("Hold Payment Updated on Invoice Pay Schedule Lines -> #" + no);
                }
            }

            // To display warning on save if credit limit exceeds
            if (!(GetDocStatus() == DOCSTATUS_Completed || GetDocStatus() == DOCSTATUS_Closed))
            {
                string retMsg = "";
                Decimal invAmt = GetGrandTotal(true);
                // If Amount is ZERO then no need to check currency conversion
                if (!invAmt.Equals(Env.ZERO))
                {
                    // JID_1828 -- need to pick selected record conversion type
                    invAmt = MVABExchangeRate.ConvertBase(GetCtx(), invAmt,  //	CM adjusted 
                        GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());

                    // JID_0822: if conversion not found system will give message Message: Could not convert currency to base currency - Conversion type: XXXX
                    if (invAmt == 0)
                    {
                        MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                        retMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                            + MVABCurrency.GetISO_Code(GetCtx(), MVAFClient.Get(GetCtx()).GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();

                        log.SaveWarning("Warning", retMsg);
                    }
                }

                // To display warning on save if credit limit exceeds
                if (IsSOTrx() && !IsReversal())
                {
                    invAmt = Decimal.Add(0, invAmt);
                    //else
                    //    invAmt = Decimal.Subtract(0, invAmt);

                    MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());

                    bool crdAll = bp.IsCreditAllowed(GetVAB_BPart_Location_ID(), invAmt, out retMsg);
                    if (!crdAll)
                        log.SaveWarning("Warning", retMsg);
                    else if (bp.IsCreditWatch(GetVAB_BPart_Location_ID()))
                    {
                        log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "VIS_BPCreditWatch"));
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// check duplicate record against invoice reference and 
        /// should not check the uniqueness of invoice reference in case of reverse document
        /// </summary>
        /// <returns>count of Invoice and 0</returns>
        public int checkFinancialYear()
        {
            if (!(!String.IsNullOrEmpty(GetDescription()) && GetDescription().Contains("{->")))
            {
                DateTime? startDate = null;
                DateTime? endDate = null, dt;
                dt = (DateTime)GetDateAcct();
                int calendar_ID = 0;
                DataSet ds = new DataSet();

                calendar_ID = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAB_Calender_ID FROM VAF_ClientDetail WHERE vaf_client_id = " + GetVAF_Client_ID(), null, null));

                ds = DB.ExecuteDataset($"SELECT startdate , enddate FROM VAB_YearPeriod WHERE VAB_Year_id = (SELECT VAB_Year.VAB_Year_id FROM VAB_Year INNER JOIN VAB_YearPeriod ON " +
                    $"VAB_Year.VAB_Year_id = VAB_YearPeriod.VAB_Year_id WHERE  VAB_Year.VAB_Calender_id ={calendar_ID} and sysdate BETWEEN VAB_YearPeriod.startdate AND VAB_YearPeriod.enddate) " +
                    $"AND periodno IN (1, 12)", null, null);

                if (ds != null && ds.Tables[0].Rows.Count > 0)
                {
                    startDate = Convert.ToDateTime(ds.Tables[0].Rows[0]["startdate"]);
                    endDate = Convert.ToDateTime(ds.Tables[0].Rows[1]["enddate"]);
                }
                string sql = "SELECT COUNT(VAB_Invoice_ID) FROM VAB_Invoice WHERE DocStatus NOT IN('RE','VO') AND IsExpenseInvoice='N' AND IsSoTrx='N'" +
                  " AND VAB_BusinessPartner_ID = " + GetVAB_BusinessPartner_ID() + " AND InvoiceReference = '" + Get_Value("InvoiceReference") + "'" +
                  " AND VAF_Org_ID= " + GetVAF_Org_ID() + " AND VAF_Client_ID= " + GetVAF_Client_ID() + " AND VAB_DocTypesTarget_ID= " + GetVAB_DocTypesTarget_ID() +
                  " AND DateAcct BETWEEN " + GlobalVariable.TO_DATE(startDate, true) + " AND " + GlobalVariable.TO_DATE(endDate, true);

                return Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));
            }
            else
                return 0;
        }
        /**
         * 	Set Price List (and Currency) when valid
         * 	@param VAM_PriceList_ID price list
         */
        public new void SetVAM_PriceList_ID(int VAM_PriceList_ID)
        {
            String sql = "SELECT VAM_PriceList_ID, VAB_Currency_ID, IsTaxIncluded " // Set IsTaxIncluded from Price List
                + "FROM VAM_PriceList WHERE VAM_PriceList_ID=" + VAM_PriceList_ID;
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    base.SetVAM_PriceList_ID(Convert.ToInt32(dr[0]));
                    SetVAB_Currency_ID(Convert.ToInt32(dr[1]));
                    SetIsTaxIncluded(Util.GetValueOfString(dr[2]).Equals("Y"));
                }
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, "setVAM_PriceList_ID", e);
            }
            finally
            {

                dt = null;
            }
        }

        /**
         * 	Get Allocated Amt in Invoice Currency
         *	@return pos/neg amount or null
         */
        public Decimal? GetAllocatedAmt()
        {
            Decimal? retValue = null;
            String sql = "SELECT SUM(currencyConvert(al.Amount+al.DiscountAmt+al.WriteOffAmt,"
                    + "ah.VAB_Currency_ID, i.VAB_Currency_ID,ah.DateTrx,COALESCE(i.VAB_CurrencyType_ID,0), al.VAF_Client_ID,al.VAF_Org_ID)) " //jz 
                + "FROM VAB_DocAllocationLine al"
                + " INNER JOIN VAB_DocAllocation ah ON (al.VAB_DocAllocation_ID=ah.VAB_DocAllocation_ID)"
                + " INNER JOIN VAB_Invoice i ON (al.VAB_Invoice_ID=i.VAB_Invoice_ID) "
                + "WHERE al.VAB_Invoice_ID=" + GetVAB_Invoice_ID()
                + " AND ah.IsActive='Y' AND al.IsActive='Y'";
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    // handle null reference
                    object value = dr[0];
                    if (value != DBNull.Value)
                        retValue = Convert.ToDecimal(dr[0]);
                }
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }
            finally { dt = null; }
            //	log.Fine("getAllocatedAmt - " + retValue);
            //	? ROUND(NVL(v_AllocatedAmt,0), 2);
            return retValue;
        }

        /**
         * 	Test Allocation (and set paid flag)
         *	@return true if updated
         */
        public bool TestAllocation()
        {
            Decimal? alloc = GetAllocatedAmt();	//	absolute
            if (alloc == null)
                alloc = Env.ZERO;
            Decimal total = GetGrandTotal();
            if (!IsSOTrx())
                total = Decimal.Negate(total);
            if (IsCreditMemo())
                total = Decimal.Negate(total);
            bool test = total.CompareTo(alloc) == 0;
            bool change = test != IsPaid();

            //if document is alreday Reveresd then do not change IsPaid on those documents
            if (GetDocStatus() == DOCSTATUS_Reversed || GetDocStatus() == DOCSTATUS_Voided)
            {
                test = true;
            }
            //End 

            if (change)
                SetIsPaid(test);
            log.Fine("Paid=" + test + " (" + alloc + "=" + total + ")");
            return change;
        }

        /**
         * 	Set Paid Flag for invoices
         * 	@param ctx context
         *	@param VAB_BusinessPartner_ID if 0 all
         *	@param trxName transaction
         */
        public static void SetIsPaid(Ctx ctx, int VAB_BusinessPartner_ID, Trx trxName)
        {
            int counter = 0;
            String sql = "SELECT * FROM VAB_Invoice "
                + "WHERE IsPaid='N' AND DocStatus IN ('CO','CL')";
            if (VAB_BusinessPartner_ID > 1)
                sql += " AND VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID;
            else
                sql += " AND VAF_Client_ID=" + ctx.GetVAF_Client_ID();
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, trxName);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    MVABInvoice invoice = new MVABInvoice(ctx, dr, trxName);
                    if (invoice.TestAllocation())
                        if (invoice.Save())
                            counter++;
                }
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                _log.Log(Level.SEVERE, sql, e);
            }
            finally { dt = null; }
            _log.Config("#" + counter);
        }

        /**
         * 	Get Open Amount.
         * 	Used by web interface
         * 	@return Open Amt
         */
        public Decimal? GetOpenAmt()
        {
            return GetOpenAmt(true, null);
        }

        /**
         * 	Get Open Amount
         * 	@param creditMemoAdjusted adjusted for CM (negative)
         * 	@param paymentDate ignored Payment Date
         * 	@return Open Amt
         */
        public Decimal? GetOpenAmt(bool creditMemoAdjusted, DateTime? paymentDate)
        {
            if (IsPaid())
                return Env.ZERO;
            //
            if (_openAmt == null)
            {
                _openAmt = GetGrandTotal();
                if (paymentDate != null)
                {
                    //	Payment Discount
                    //	Payment Schedule
                }
                Decimal? allocated = GetAllocatedAmt();
                if (allocated != null)
                {
                    allocated = Math.Abs((Decimal)allocated);//.abs();	//	is absolute
                    _openAmt = Decimal.Subtract((Decimal)_openAmt, (Decimal)allocated);
                }
            }

            if (!creditMemoAdjusted)
                return _openAmt;
            if (IsCreditMemo())
                return Decimal.Negate((Decimal)_openAmt);
            return _openAmt;
        }

        /**
         * 	Get Document Status
         *	@return Document Status Clear Text
         */
        public String GetDocStatusName()
        {
            return MVAFCtrlRefList.GetListName(GetCtx(), 131, GetDocStatus());
        }

        /**
         * 	Create PDF
         *	@return File or null
         */
        //public File CreatePDF ()
        //public FileInfo CreatePDF()
        //{
        //    try
        //    {
        //        File temp = File.createTempFile(get_TableName() + get_ID() + "_", ".pdf");
        //        return createPDF(temp);
        //    }
        //    catch (Exception e)
        //    {
        //        log.severe("Could not create PDF - " + e.getMessage());
        //    }
        //    return null;
        //}	

        /**
         * 	Create PDF file
         *	@param file output file
         *	@return file if success
         */
        //public FileInfo CreatePDF (File file)
        //{
        //    ReportEngine re = ReportEngine.get (GetCtx(), ReportEngine.INVOICE, getVAB_Invoice_ID());
        //    if (re == null)
        //        return null;
        //    return re.getPDF(file);
        //}	

        /// <summary>
        /// Create PDF
        /// </summary>
        /// <returns>File or null</returns>
        public FileInfo CreatePDF()
        {
            try
            {
                //string fileName = Get_TableName() + Get_ID() + "_" + CommonFunctions.GenerateRandomNo()
                String fileName = Get_TableName() + Get_ID() + "_" + CommonFunctions.GenerateRandomNo() + ".pdf";
                string filePath = Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "TempDownload", fileName);


                ReportEngine_N re = ReportEngine_N.Get(GetCtx(), ReportEngine_N.INVOICE, GetVAB_Invoice_ID());
                if (re == null)
                    return null;

                re.GetView();
                bool b = re.CreatePDF(filePath);

                //File temp = File.createTempFile(Get_TableName() + Get_ID() + "_", ".pdf");
                //FileStream fOutStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

                FileInfo temp = new FileInfo(filePath);
                if (!temp.Exists)
                {
                    re.CreatePDF(filePath);
                    return new FileInfo(filePath);
                }
                else
                    return temp;
            }
            catch (Exception e)
            {
                log.Severe("Could not create PDF - " + e.Message);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public FileInfo CreatePDF(FileInfo file)
        {
            //ReportEngine re = ReportEngine.get(GetCtx(), ReportEngine.ORDER, GetVAB_Order_ID());
            //if (re == null)
            //    return null;
            //return re.getPDF(file);

            //Create a file to write to.
            using (StreamWriter sw = file.CreateText())
            {
                sw.WriteLine("Hello");
                sw.WriteLine("And");
                sw.WriteLine("Welcome");
            }

            return file;

        }

        /**
         * 	Get PDF File Name
         *	@param documentDir directory
         *	@return file name
         */
        public String GetPDFFileName(String documentDir)
        {
            return GetPDFFileName(documentDir, GetVAB_Invoice_ID());
        }

        /**
         *	Get ISO Code of Currency
         *	@return Currency ISO
         */
        public String GetCurrencyISO()
        {
            return MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID());
        }

        /**
         * 	Get Currency Precision
         *	@return precision
         */
        public int GetPrecision()
        {
            return MVABCurrency.GetStdPrecision(GetCtx(), GetVAB_Currency_ID());
        }

        /***
         * 	Process document
         *	@param processAction document action
         *	@return true if performed
         */
        public bool ProcessIt(String processAction)
        {
            _processMsg = null;
            DocumentEngine engine = new DocumentEngine(this, GetDocStatus());
            return engine.ProcessIt(processAction, GetDocAction());
        }

        /**
         * 	Unlock Document.
         * 	@return true if success 
         */
        public bool UnlockIt()
        {
            log.Info("unlockIt - " + ToString());
            SetProcessing(false);
            return true;
        }

        /**
         * 	Invalidate Document
         * 	@return true if success 
         */
        public bool InvalidateIt()
        {
            log.Info("invalidateIt - " + ToString());
            SetDocAction(DOCACTION_Prepare);
            return true;
        }

        /**
         *	Prepare Document
         * 	@return new status (In Progress or Invalid) 
         */
        public String PrepareIt()
        {

            log.Info(ToString());
            _processMsg = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_BEFORE_PREPARE);
            if (_processMsg != null)
                return DocActionVariables.STATUS_INVALID;
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypesTarget_ID());
            SetIsReturnTrx(dt.IsReturnTrx());
            SetIsSOTrx(dt.IsSOTrx());

            //	Std Period open?
            if (!MVABYearPeriod.IsOpen(GetCtx(), GetDateAcct(), dt.GetDocBaseType(), GetVAF_Org_ID()))
            {
                _processMsg = "@PeriodClosed@";
                return DocActionVariables.STATUS_INVALID;
            }

            // is Non Business Day?
            // JID_1205: At the trx, need to check any non business day in that org. if not fund then check * org.
            if (MVABNonBusinessDay.IsNonBusinessDay(GetCtx(), GetDateAcct(), GetVAF_Org_ID()))
            {
                _processMsg = Common.Common.NONBUSINESSDAY;
                return DocActionVariables.STATUS_INVALID;
            }

            //	Lines
            MVABInvoiceLine[] lines = GetLines(true);
            if (lines.Length == 0)
            {
                _processMsg = "@NoLines@";
                return DocActionVariables.STATUS_INVALID;
            }

            //	No Cash Book
            if (PAYMENTRULE_Cash.Equals(GetPaymentRule())
                && MVABCashBook.Get(GetCtx(), GetVAF_Org_ID(), GetVAB_Currency_ID()) == null)
            {
                _processMsg = "@NoCashBook@";
                return DocActionVariables.STATUS_INVALID;
            }

            //	Convert/Check DocType
            if (GetVAB_DocTypes_ID() != GetVAB_DocTypesTarget_ID())
                SetVAB_DocTypes_ID(GetVAB_DocTypesTarget_ID());
            if (GetVAB_DocTypes_ID() == 0)
            {
                _processMsg = "No Document Type";
                return DocActionVariables.STATUS_INVALID;
            }

            //check Payment term is valid or Not (SI_0018)
            if (Util.GetValueOfString(DB.ExecuteScalar("SELECT IsValid FROM VAB_PaymentTerm WHERE VAB_PaymentTerm_ID = " + GetVAB_PaymentTerm_ID())) == "N")
            {
                _processMsg = Msg.GetMsg(GetCtx(), "PaymentTermIsInValid");
                return DocActionVariables.STATUS_INVALID;
            }

            //JID_0770: added by Bharat on 29 Jan 2019 to avoid completion if Payment Method is not selected.
            if (!IsSOTrx() && Env.IsModuleInstalled("VA009_") && GetVA009_PaymentMethod_ID() == 0)
            {
                _processMsg = "@MandatoryPaymentMethod@";
                return DocActionVariables.STATUS_INVALID;
            }

            ExplodeBOM();
            if (!CalculateTaxTotal())	//	setTotals
            {
                _processMsg = "Error calculating Tax";
                return DocActionVariables.STATUS_INVALID;
            }

            // Check for Advance Payment Against Order added by vivek on 16/06/2016 by Siddharth
            if (GetDescription() != null && GetDescription().Contains("{->"))
            { }
            else
            {
                if (Env.IsModuleInstalled("VA009_"))
                {
                    MVABPaymentTerm payterm = new MVABPaymentTerm(GetCtx(), GetVAB_PaymentTerm_ID(), Get_TrxName());
                    if (GetVAB_Order_ID() != 0)
                    {
                        int _countschedule = Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA009_OrderPaySchedule Where VAB_Order_ID=" + GetVAB_Order_ID()));
                        if (_countschedule > 0)
                        {
                            if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA009_OrderPaySchedule Where VAB_Order_ID=" + GetVAB_Order_ID() + " AND VA009_Ispaid='Y'")) != _countschedule)
                            {
                                _processMsg = "Please Do Advance Payment for Order";
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                    else
                    {
                        if (payterm.IsVA009_Advance())
                        {
                            // JID_0383: if payment term is selected as advnace. System should give error "Please do the advance payment".
                            _processMsg = Msg.GetMsg(GetCtx(), "VIS_SelectAdvancePayment");
                            return DocActionVariables.STATUS_INVALID;
                        }
                        else if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_PaymentSchedule WHERE IsActive = 'Y' AND VAB_PaymentTerm_ID=" + GetVAB_PaymentTerm_ID())) > 0)
                        {
                            if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_PaymentSchedule WHERE IsActive = 'Y' AND IsValid = 'Y' AND VAB_PaymentTerm_ID="
                                                                    + GetVAB_PaymentTerm_ID() + " AND VA009_Advance='Y'")) == 1)
                            {
                                _processMsg = Msg.GetMsg(GetCtx(), "PaymentTermIsInValid");
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                }
            }

            //// set backup withholding tax amount
            if (!IsReversal() && Get_ColumnIndex("VAB_Withholding_ID") > 0 && GetVAB_Withholding_ID() > 0)
            {
                SetWithholdingAmount(this);
            }

            // not crating schedule in prepare stage, to be created in completed stage
            // Create Invoice schedule
            if (!CreatePaySchedule())
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                {
                    _processMsg = pp.GetName();
                }
                return DocActionVariables.STATUS_INVALID;
            }

            // JID_0822: if conversion not found system will give message Message: Could not convert currency to base currency - Conversion type: XXXX
            Decimal invAmt = GetGrandTotal(true);
            // If Amount is ZERO then no need to check currency conversion
            if (!invAmt.Equals(Env.ZERO))
            {
                invAmt = MVABExchangeRate.ConvertBase(GetCtx(), GetGrandTotal(true), //	CM adjusted 
             GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());

                if (invAmt == 0)
                {
                    MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                    _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                        + MVABCurrency.GetISO_Code(GetCtx(), MVAFClient.Get(GetCtx()).GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();

                    return DocActionVariables.STATUS_INVALID;
                }
            }

            //	Credit Status check only in case of AR Invoice 
            if (IsSOTrx() && !IsReversal() && !IsReturnTrx())
            {
                bool checkCreditStatus = true;
                if (Env.IsModuleInstalled("VAPOS_"))
                {
                    // JID_1224: Check Creadit Status of Business Partner for Standard Order and POS Credit Order
                    string result = Util.GetValueOfString(DB.ExecuteScalar("SELECT VAPOS_CreditAmt FROM VAB_Order WHERE VAPOS_POSTerminal_ID > 0 AND VAB_Order_ID = " + GetVAB_Order_ID(), null, Get_TrxName()));
                    if (!String.IsNullOrEmpty(result))
                    {
                        if (Util.GetValueOfDecimal(result) <= 0)
                        {
                            checkCreditStatus = false;
                        }
                    }
                }

                if (checkCreditStatus)
                {
                    if (IsSOTrx())
                        invAmt = Decimal.Add(0, invAmt);
                    //else
                    //    invAmt = Decimal.Subtract(0, invAmt);

                    MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), null);

                    // check for Credit limit and Credit validation on Customer Master or Location
                    string retMsg = "";
                    bool crdAll = bp.IsCreditAllowed(GetVAB_BPart_Location_ID(), invAmt, out retMsg);
                    if (!crdAll)
                    {
                        if (bp.ValidateCreditValidation("C,D,F", GetVAB_BPart_Location_ID()))
                        {
                            _processMsg = retMsg;
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }
                }
            }

            //	Landed Costs
            if (!IsSOTrx())
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABInvoiceLine line = lines[i];
                    String error = line.AllocateLandedCosts();
                    if (error != null && error.Length > 0)
                    {
                        _processMsg = error;
                        return DocActionVariables.STATUS_INVALID;
                    }
                }
            }

            //	Add up Amounts
            _justPrepared = true;
            if (!DOCACTION_Complete.Equals(GetDocAction()))
                SetDocAction(DOCACTION_Complete);
            return DocActionVariables.STATUS_INPROGRESS;
        }

        /**
         * 	Explode non stocked BOM.
         */
        private void ExplodeBOM()
        {
            String where = "AND IsActive='Y' AND EXISTS "
                + "(SELECT * FROM VAM_Product p WHERE VAB_InvoiceLine.VAM_Product_ID=p.VAM_Product_ID"
                + " AND	p.IsBOM='Y' AND p.IsVerified='Y' AND p.IsStocked='N')";
            //
            String sql = "SELECT COUNT(*) FROM VAB_InvoiceLine "
                + "WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID() + " " + where;
            int count = DataBase.DB.GetSQLValue(Get_TrxName(), sql);
            while (count != 0)
            {
                RenumberLines(100);

                //	Order Lines with non-stocked BOMs
                MVABInvoiceLine[] lines = GetLines(where);
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABInvoiceLine line = lines[i];
                    MVAMProduct product = MVAMProduct.Get(GetCtx(), line.GetVAM_Product_ID());
                    log.Fine(product.GetName());
                    //	New Lines
                    int lineNo = line.GetLine();
                    MVAMProductBOM[] boms = MVAMProductBOM.GetBOMLines(product);
                    for (int j = 0; j < boms.Length; j++)
                    {
                        MVAMProductBOM bom = boms[j];
                        MVABInvoiceLine newLine = new MVABInvoiceLine(this);
                        newLine.SetLine(++lineNo);
                        newLine.SetVAM_Product_ID(bom.GetProduct().GetVAM_Product_ID(),
                            bom.GetProduct().GetVAB_UOM_ID());
                        newLine.SetQty(Decimal.Multiply(line.GetQtyInvoiced(), bom.GetBOMQty()));		//	Invoiced/Entered
                        if (bom.GetDescription() != null)
                            newLine.SetDescription(bom.GetDescription());
                        //
                        newLine.SetPrice();
                        newLine.Save(Get_TrxName());
                    }
                    //	Convert into Comment Line
                    line.SetVAM_Product_ID(0);
                    line.SetVAM_PFeature_SetInstance_ID(0);
                    line.SetPriceEntered(Env.ZERO);
                    line.SetPriceActual(Env.ZERO);
                    line.SetPriceLimit(Env.ZERO);
                    line.SetPriceList(Env.ZERO);
                    line.SetLineNetAmt(Env.ZERO);
                    //
                    String description = product.GetName();
                    if (product.GetDescription() != null)
                        description += " " + product.GetDescription();
                    if (line.GetDescription() != null)
                        description += " " + line.GetDescription();
                    line.SetDescription(description);
                    line.Save(Get_TrxName());
                } //	for all lines with BOM

                _lines = null;
                count = DataBase.DB.GetSQLValue(Get_TrxName(), sql, GetVAB_Invoice_ID());
                RenumberLines(10);
            }	//	while count != 0
        }

        /**
         * 	Calculate Tax and Total
         * 	@return true if calculated
         */
        //private bool CalculateTaxTotal()
        public bool CalculateTaxTotal()
        {
            log.Fine("");
            try
            {
                //	Delete Taxes
                DataBase.DB.ExecuteQuery("DELETE FROM VAB_Tax_Invoice WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID(), null, Get_TrxName());
                _taxes = null;

                //	Lines
                Decimal totalLines = Env.ZERO;
                Decimal totalWithholdingAmt = Env.ZERO;
                List<int> taxList = new List<int>();
                MVABInvoiceLine[] lines = GetLines(false);
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABInvoiceLine line = lines[i];
                    /**	Sync ownership for SO
                    if (isSOTrx() && line.getVAF_Org_ID() != getVAF_Org_ID())
                    {
                        line.setVAF_Org_ID(getVAF_Org_ID());
                        line.Save();
                    }	**/
                    int taxID = (int)line.GetVAB_TaxRate_ID();
                    if (!taxList.Contains(taxID))
                    {
                        MVABTaxInvoice iTax = MVABTaxInvoice.Get(line, GetPrecision(),
                            false, Get_TrxName());	//	current Tax
                        if (iTax != null)
                        {
                            //iTax.SetIsTaxIncluded(IsTaxIncluded());
                            if (!iTax.CalculateTaxFromLines())
                                return false;
                            if (!iTax.Save())
                                return false;
                            taxList.Add(taxID);

                            // if Surcharge Tax is selected then calculate Tax for this Surcharge Tax.
                            if (line.Get_ColumnIndex("SurchargeAmt") > 0)
                            {
                                iTax = MVABTaxInvoice.GetSurcharge(line, GetPrecision(), false, Get_TrxName());  //	current Tax
                                if (iTax != null)
                                {
                                    if (!iTax.CalculateSurchargeFromLines())
                                        return false;
                                    if (!iTax.Save(Get_TrxName()))
                                        return false;
                                }
                            }
                        }
                    }
                    totalLines = Decimal.Add(totalLines, line.GetLineNetAmt());
                    if (Get_ColumnIndex("WithholdingAmt") > 0)
                    {
                        totalWithholdingAmt = Decimal.Add(totalWithholdingAmt, line.GetWithholdingAmt());
                    }
                }

                //	Taxes
                Decimal grandTotal = totalLines;
                MVABTaxInvoice[] taxes = GetTaxes(true);
                for (int i = 0; i < taxes.Length; i++)
                {
                    MVABTaxInvoice iTax = taxes[i];
                    MVABTaxRate tax = iTax.GetTax();
                    if (tax.IsSummary())
                    {
                        MVABTaxRate[] cTaxes = tax.GetChildTaxes(false);	//	Multiple taxes
                        for (int j = 0; j < cTaxes.Length; j++)
                        {
                            MVABTaxRate cTax = cTaxes[j];
                            Decimal taxAmt = cTax.CalculateTax(iTax.GetTaxBaseAmt(), false, GetPrecision());
                            //
                            // JID_0430: if we add 2 lines with different Taxes. one is Parent and other is child. System showing error on completion that "Error Calculating Tax"
                            if (taxList.Contains(cTax.GetVAB_TaxRate_ID()))
                            {
                                String sql = "SELECT * FROM VAB_Tax_Invoice WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID() + " AND VAB_TaxRate_ID=" + cTax.GetVAB_TaxRate_ID();
                                DataSet ds = DB.ExecuteDataset(sql, null, Get_TrxName());
                                if (ds != null && ds.Tables[0].Rows.Count > 0)
                                {
                                    DataRow dr = ds.Tables[0].Rows[0];
                                    MVABTaxInvoice newITax = new MVABTaxInvoice(GetCtx(), dr, Get_TrxName());
                                    newITax.SetTaxAmt(Decimal.Add(newITax.GetTaxAmt(), taxAmt));
                                    newITax.SetTaxBaseAmt(Decimal.Add(newITax.GetTaxBaseAmt(), iTax.GetTaxBaseAmt()));
                                    if (newITax.Get_ColumnIndex("TaxBaseCurrencyAmt") > 0)
                                    {
                                        Decimal baseTaxAmt = taxAmt;
                                        int primaryAcctSchemaCurrency_ = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_Currency_ID FROM VAB_AccountBook WHERE VAB_AccountBook_ID = 
                                            (SELECT VAB_AccountBook1_ID FROM VAF_ClientDetail WHERE VAF_Client_ID = " + GetVAF_Client_ID() + ")", null, Get_Trx()));
                                        if (GetVAB_Currency_ID() != primaryAcctSchemaCurrency_)
                                        {
                                            baseTaxAmt = MVABExchangeRate.Convert(GetCtx(), taxAmt, primaryAcctSchemaCurrency_, GetVAB_Currency_ID(),
                                                                                                       GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                                        }
                                        newITax.SetTaxBaseCurrencyAmt(Decimal.Add(newITax.GetTaxBaseCurrencyAmt(), baseTaxAmt));
                                    }
                                    if (!newITax.Save(Get_TrxName()))
                                    {
                                        return false;
                                    }
                                }
                                ds = null;
                            }
                            else
                            {
                                MVABTaxInvoice newITax = new MVABTaxInvoice(GetCtx(), 0, Get_TrxName());
                                newITax.SetClientOrg(this);
                                newITax.SetVAB_Invoice_ID(GetVAB_Invoice_ID());
                                newITax.SetVAB_TaxRate_ID(cTax.GetVAB_TaxRate_ID());
                                newITax.SetPrecision(GetPrecision());
                                newITax.SetIsTaxIncluded(IsTaxIncluded());
                                newITax.SetTaxBaseAmt(iTax.GetTaxBaseAmt());
                                newITax.SetTaxAmt(taxAmt);
                                //Set Tax Amount (Base Currency) on Invoice Tax Window //Arpit--8 Jan,2018 Puneet 
                                if (newITax.Get_ColumnIndex("TaxBaseCurrencyAmt") > 0)
                                {
                                    decimal? baseTaxAmt = taxAmt;
                                    int primaryAcctSchemaCurrency_ = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_Currency_ID FROM VAB_AccountBook WHERE VAB_AccountBook_ID = 
                                            (SELECT VAB_AccountBook1_id FROM VAF_ClientDetail WHERE vaf_client_id = " + GetVAF_Client_ID() + ")", null, Get_Trx()));
                                    if (GetVAB_Currency_ID() != primaryAcctSchemaCurrency_)
                                    {
                                        baseTaxAmt = MVABExchangeRate.Convert(GetCtx(), taxAmt, primaryAcctSchemaCurrency_, GetVAB_Currency_ID(),
                                                                                                   GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                                    }
                                    newITax.Set_Value("TaxBaseCurrencyAmt", baseTaxAmt);
                                }
                                if (!newITax.Save(Get_TrxName()))
                                    return false;
                            }
                            //
                            if (!IsTaxIncluded())
                                grandTotal = Decimal.Add(grandTotal, taxAmt);
                        }
                        if (!iTax.Delete(true, Get_TrxName()))
                            return false;
                    }
                    else
                    {
                        if (!IsTaxIncluded())
                            grandTotal = Decimal.Add(grandTotal, iTax.GetTaxAmt());
                    }
                }
                //
                SetTotalLines(totalLines);
                SetGrandTotal(grandTotal);
                if (Get_ColumnIndex("WithholdingAmt") > 0)
                {
                    base.SetWithholdingAmt(totalWithholdingAmt);
                    SetGrandTotalAfterWithholding(grandTotal - totalWithholdingAmt);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /**
         * 	(Re) Create Pay Schedule
         *	@return true if valid schedule
         */
        private bool CreatePaySchedule()
        {
            if (GetVAB_PaymentTerm_ID() == 0)
                return false;
            MVABPaymentTerm pt = new MVABPaymentTerm(GetCtx(), GetVAB_PaymentTerm_ID(), Get_TrxName());
            log.Fine(pt.ToString());
            return pt.Apply(this);		//	calls validate pay schedule
        }


        /**
         * 	Approve Document
         * 	@return true if success 
         */
        public bool ApproveIt()
        {
            log.Info(ToString());
            SetIsApproved(true);
            return true;
        }

        /**
         * 	Reject Approval
         * 	@return true if success 
         */
        public bool RejectIt()
        {
            log.Info(ToString());
            SetIsApproved(false);
            return true;
        }

        /// <summary>
        ///  Complete Document 
        /// </summary>
        /// <returns>return new status (Complete, In Progress, Invalid, Waiting ..)</returns>
        public String CompleteIt()
        {

            string timeEstimation = " start at  " + DateTime.Now.ToUniversalTime() + " - ";
            log.Warning("TStart time of invoice completion : " + DateTime.Now.ToUniversalTime());
            try
            {
                decimal creditLimit = 0;
                string creditVal = null;
                MVABBusinessPartner bp = null;
                //	Re-Check
                if (!_justPrepared)
                {
                    String status = PrepareIt();
                    if (!DocActionVariables.STATUS_INPROGRESS.Equals(status))
                        return status;
                }

                // JID_1290: Set the document number from completed document sequence after completed (if needed)
                SetCompletedDocumentNo();

                //	Implicit Approval
                if (!IsApproved())
                    ApproveIt();

                log.Info(ToString());
                StringBuilder Info = new StringBuilder();
                MVABDocTypes dt = new MVABDocTypes(GetCtx(), GetVAB_DocTypesTarget_ID(), Get_Trx());
                //	Create Cash when the invoice againt order and payment method cash and  order is of POS type
                if ((PAYMENTRULE_Cash.Equals(GetPaymentRule()) || PAYMENTRULE_CashAndCredit.Equals(GetPaymentRule())) && GetVAB_Order_ID() > 0)
                {
                    int posDocType = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_DocTypes_id FROM VAB_DocTypes WHERE docbasetype = 'SOO' AND docsubtypeso  = 'WR' 
                                      AND VAB_DocTypes_id  =   (SELECT VAB_DocTypestarget_id FROM VAB_Order WHERE VAB_Order_id = " + GetVAB_Order_ID() + ") ", null, Get_Trx()));
                    if (posDocType > 0)
                    {
                        MVABCashJRNL cash = null;

                        bool isStdPosOrder = true; //Order created from Backend 

                        if (Env.IsModuleInstalled("VAPOS_"))
                        {
                            MVABOrder order = new MVABOrder(GetCtx(), GetVAB_Order_ID(), Get_Trx());
                            int VAPOS_Terminal_ID = order.GetVAPOS_POSTerminal_ID();
                            if (VAPOS_Terminal_ID > 0)
                            {
                                isStdPosOrder = false;
                                //check Voucher Return 
                                bool IsGenerateVchRtn = IsGenerateVoucherReturn(order);

                                if (!IsGenerateVchRtn)
                                {

                                    if (!UpdateCashBaseAndMulticurrency(Info, order))
                                        return DocActionVariables.STATUS_INVALID;
                                } // end isvoucher return
                            } // end vapos order
                            else // std pos order
                            {
                                cash = MVABCashJRNL.Get(GetCtx(), GetVAF_Org_ID(),
                                   GetDateInvoiced(), GetVAB_Currency_ID(), Get_TrxName());
                            }
                        }
                        else
                        {
                            cash = MVABCashJRNL.Get(GetCtx(), GetVAF_Org_ID(),
                               GetDateInvoiced(), GetVAB_Currency_ID(), Get_TrxName());
                        }
                        if (isStdPosOrder)
                        {
                            if (cash == null || cash.Get_ID() == 0)
                            {
                                //SI_0648 : If POS Order Type Is created with cash and Previous Cash journal was open. System give erorr of "No Cashbook"
                                ValueNamePair pp = VLogger.RetrieveError();
                                if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                                {
                                    _processMsg = pp.GetName();
                                }
                                else
                                    _processMsg = "@NoCashBook@";
                                return DocActionVariables.STATUS_INVALID;
                            }
                            //Added By Manjot -Changes done for Target Doctype Cash Journal
                            Int32 DocType_ID = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAB_DocTypes_ID FROM VAB_DocTypes WHERE docbasetype='CMC' AND vaf_client_id='" + GetCtx().GetVAF_Client_ID() + "' AND vaf_org_id in('0','" + GetCtx().GetVAF_Org_ID() + "') ORDER BY  vaf_org_id desc"));
                            cash.SetVAB_DocTypes_ID(DocType_ID);
                            cash.Save();
                            // Manjot

                            MVABCashJRNLLine cl = null;
                            if (Env.IsModuleInstalled("VA009_"))
                            {
                                DataSet ds = new DataSet();
                                ds = DB.ExecuteDataset("SELECT * FROM VAB_sched_InvoicePayment WHERE IsActive = 'Y' AND VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    for (int k = 0; k < ds.Tables[0].Rows.Count; k++)
                                    {
                                        cl = new MVABCashJRNLLine(cash);
                                        cl.CreateCashLine(this, Util.GetValueOfInt(ds.Tables[0].Rows[k]["VAB_sched_InvoicePayment_ID"]), Util.GetValueOfDecimal(ds.Tables[0].Rows[k]["DueAmt"]));
                                        if (!cl.Save(Get_TrxName()))
                                        {
                                            _processMsg = "Could not Save Cash Journal Line";
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                        Info.Append("@VAB_CashJRNL_ID@: " + cash.GetName() + " #" + cl.GetLine());
                                    }
                                }
                                ds.Dispose();
                            }
                            else
                            {
                                cl = new MVABCashJRNLLine(cash);
                                cl.SetInvoice(this);
                                if (!cl.Save(Get_TrxName()))
                                {
                                    _processMsg = "Could not Save Cash Journal Line";
                                    return DocActionVariables.STATUS_INVALID;
                                }
                                Info.Append("@VAB_CashJRNL_ID@: " + cash.GetName() + " #" + cl.GetLine());
                                SetVAB_CashJRNLLine_ID(cl.GetVAB_CashJRNLLine_ID());
                            }
                        } //End Std Pos Order
                    }//Pos order type
                }	//	CashBook


                //	Update Order & Match
                int matchInv = 0;
                int matchPO = 0;
                //decimal AmortizedValue = 0;
                //DateTime? AmortStartDate = null;
                //DateTime? AmortEndDate = null;
                MVABInvoiceLine[] lines = GetLines(false);

                // for checking - costing calculate on completion or not
                // IsCostImmediate = true - calculate cost on completion
                MVAFClient client = MVAFClient.Get(GetCtx(), GetVAF_Client_ID());

                for (int i = 0; i < lines.Length; i++)
                {
                    MVABInvoiceLine line = lines[i];
                    MVAMMatchInvoice inv = null;
                    //	Update Order Line
                    MVABOrderLine ol = null;
                    if (line.GetVAB_OrderLine_ID() != 0)
                    {
                        if (IsSOTrx()
                            || line.GetVAM_Product_ID() == 0)
                        {
                            ol = new MVABOrderLine(GetCtx(), line.GetVAB_OrderLine_ID(), Get_TrxName());
                            // if (line.GetQtyInvoiced() != null)
                            {
                                // special check to Update orderline's Qty Invoiced
                                // if Qtyentered == QtyInvoiced Then do not update 

                                // Commented this check as it was not updating QtyInvoiced at Orderline //Lokesh Chauhan
                                // if (ol.GetQtyInvoiced() < ol.GetQtyEntered())
                                //{
                                //    ol.SetQtyInvoiced(Decimal.Add(ol.GetQtyInvoiced(), line.GetQtyInvoiced()));
                                //}

                                ol.SetQtyInvoiced(Decimal.Add(ol.GetQtyInvoiced(), line.GetQtyInvoiced()));

                            }
                            if (!ol.Save(Get_TrxName()))
                            {
                                _processMsg = "Could not update Order Line";
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                        //	Order Invoiced Qty updated via Matching Inv-PO
                        else if (!IsSOTrx()
                            && line.GetVAM_Product_ID() != 0
                            && !IsReversal())
                        {
                            //	MatchPO is created also from MVAMInvInOut when Invoice exists before Shipment
                            Decimal matchQty = line.GetQtyInvoiced();
                            MVAMMatchPO po = MVAMMatchPO.Create(line, null, GetDateInvoiced(), matchQty);
                            try
                            {
                                po.Set_ValueNoCheck("VAB_BusinessPartner_ID", GetVAB_BusinessPartner_ID());
                            }
                            catch { }
                            if (!po.Save(Get_TrxName()))
                            {
                                _processMsg = "Could not create PO Matching";
                                return DocActionVariables.STATUS_INVALID;
                            }
                            else
                                matchPO++;
                        }

                        if (line.GetVAB_OrderLine_ID() > 0)
                        {
                            if (ol == null || ol.Get_ID() == 0)
                            {
                                ol = new MVABOrderLine(GetCtx(), line.GetVAB_OrderLine_ID(), Get_TrxName());
                            }
                            if (ol.GetVAB_OrderLine_Blanket_ID() > 0)
                            {
                                MVABOrderLine lineBlanket1 = new MVABOrderLine(GetCtx(), ol.GetVAB_OrderLine_Blanket_ID(), null);
                                if (lineBlanket1.Get_ID() > 0)
                                {
                                    if (IsSOTrx())
                                    {
                                        lineBlanket1.SetQtyInvoiced(Decimal.Subtract(lineBlanket1.GetQtyInvoiced(), line.GetQtyInvoiced()));
                                    }
                                    else
                                    {
                                        lineBlanket1.SetQtyInvoiced(Decimal.Add(lineBlanket1.GetQtyInvoiced(), line.GetQtyInvoiced()));
                                    }
                                    lineBlanket1.Save();
                                }
                            }
                        }
                    }

                    #region Validate LOC which is created or not against Order 
                    // Validate LOC which is created or not against Order
                    if (line.GetVAB_OrderLine_ID() != 0)
                    {
                        int VA026_LCDetail_ID = 0;
                        if (Env.IsModuleInstalled("VA026_") && Env.IsModuleInstalled("VA009_"))
                        {
                            DataSet ds = DB.ExecuteDataset(@"SELECT o.VAB_Order_ID, o.IsSOTrx, pm.VA009_PaymentBaseType FROM VAB_OrderLine ol INNER JOIN VAB_Order o 
                                        ON ol.VAB_Order_ID=o.VAB_Order_ID INNER JOIN VA009_PaymentMethod pm ON pm.VA009_PaymentMethod_ID=o.VA009_PaymentMethod_ID
                                    WHERE o.IsActive = 'Y' AND DocStatus IN ('CL' , 'CO')  AND ol.VAB_OrderLine_ID =" + line.GetVAB_OrderLine_ID(), null, Get_Trx());
                            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                if (Util.GetValueOfString(ds.Tables[0].Rows[0]["VA009_PaymentBaseType"]).Equals(X_VAB_Invoice.PAYMENTMETHOD_LetterOfCredit))
                                {
                                    //Check VA026_LCDetail_ID if the LC OrderType is Single
                                    VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT MIN(VA026_LCDetail_ID)  FROM VA026_LCDetail 
                                                            WHERE IsActive = 'Y' AND DocStatus NOT IN ('RE', 'VO')  AND " +
                                                           (Util.GetValueOfString(ds.Tables[0].Rows[0]["IsSOTrx"]) == "Y" ? " VA026_Order_ID" : "VAB_Order_id") + " = " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Order_ID"]), null, Get_Trx()));
                                    // Check SO / PO Detail tab of Letter of Credit
                                    if (VA026_LCDetail_ID == 0)
                                    {
                                        VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT MIN(lc.VA026_LCDetail_ID)  FROM VA026_LCDetail lc
                                                        INNER JOIN " + (Util.GetValueOfString(ds.Tables[0].Rows[0]["IsSOTrx"]) == "Y" ? " VA026_SODetail" : "VA026_PODetail") + @" sod ON sod.VA026_LCDetail_ID = lc.VA026_LCDetail_ID 
                                                            WHERE sod.IsActive = 'Y' AND lc.IsActive = 'Y' AND lc.DocStatus NOT IN('RE', 'VO')  AND
                                                            sod.VAB_Order_ID = " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Order_ID"]), null, Get_Trx()));

                                        if (VA026_LCDetail_ID == 0)
                                        {
                                            _processMsg = Msg.GetMsg(GetCtx(), "VA026_LCNotDefine");
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                    }
                                    //VA026_LCDetail_ID Record is Completed or not
                                    int docStatus = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT  COUNT(VA026_LCDetail_ID)  FROM VA026_LCDetail 
                                                            WHERE IsActive = 'Y' AND DocStatus NOT IN ('CL' , 'CO')  AND VA026_LCDetail_ID =" + Util.GetValueOfInt(VA026_LCDetail_ID), null, Get_Trx()));
                                    if (docStatus > 0)
                                    {
                                        _processMsg = Msg.GetMsg(GetCtx(), "VA026_CompleteLC");
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    int countVA038 = Env.IsModuleInstalled("VA038_") ? 1 : 0;
                    //	Matching - Inv-Shipment
                    if (!IsSOTrx()
                        && line.GetVAM_Inv_InOutLine_ID() != 0
                        && line.GetVAM_Product_ID() != 0
                        && !IsReversal())
                    {
                        MVAMInvInOutLine receiptLine = new MVAMInvInOutLine(GetCtx(), line.GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                        Decimal matchQty = line.GetQtyInvoiced();

                        /////////////////////////
                        #region[By Sukhwinder on 21-Nov-2017 for Standard Issue #SI_0196 given by Puneet]
                        try
                        {
                            MVAMMatchInvoice[] MatchInvoices = MVAMMatchInvoice.Get(GetCtx(), receiptLine.GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                            decimal alreadyMatchedQty = 0;

                            if (MatchInvoices.Length > 0)
                            {
                                for (int len = 0; len < MatchInvoices.Length; len++)
                                {
                                    alreadyMatchedQty += MatchInvoices[len].GetQty();
                                }

                                if ((alreadyMatchedQty + matchQty) > receiptLine.GetMovementQty())
                                {
                                    matchQty = receiptLine.GetMovementQty() - alreadyMatchedQty;
                                }
                            }
                        }
                        catch { }

                        #endregion
                        /////////////////////////

                        if (receiptLine.GetMovementQty().CompareTo(matchQty) < 0)
                            matchQty = receiptLine.GetMovementQty();

                        if (matchQty != 0)
                        {
                            inv = new MVAMMatchInvoice(line, GetDateInvoiced(), matchQty);

                            try
                            {
                                inv.Set_ValueNoCheck("VAB_BusinessPartner_ID", GetVAB_BusinessPartner_ID());
                            }
                            catch { }
                            if (!inv.Save(Get_TrxName()))
                            {
                                _processMsg = "Could not create Invoice Matching";
                                return DocActionVariables.STATUS_INVALID;
                            }
                            else
                                matchInv++;
                        }
                        else
                        {
                            DB.ExecuteQuery("UPDATE VAB_InvoiceLine SET VAM_Inv_InOutLine_ID = NULL WHERE VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx());
                            line.SetVAM_Inv_InOutLine_ID(0);
                        }
                    }

                    // JID_1251 : If product type is Service or Expense and Asset group is linked with Product Category it will generate the Asset 
                    // Case When Material Receipt Not Linked to invoice and not created for return side (like AP credit memo )
                    // Generate Asset and then create amortization schedule for that asset 
                    if (!IsSOTrx() && !IsReturnTrx()
                       && line.GetVAM_Inv_InOutLine_ID() == 0
                       && line.GetVAM_Product_ID() != 0
                       && !IsReversal())
                    {
                        int noAssets = (int)line.GetQtyEntered();
                        MVAMProduct product = new MVAMProduct(GetCtx(), line.GetVAM_Product_ID(), Get_TrxName());
                        if (product != null &&
                            (product.GetProductType() == X_VAM_Product.PRODUCTTYPE_Service || product.GetProductType() == X_VAM_Product.PRODUCTTYPE_ExpenseType) &&
                            product.IsCreateAsset())
                        {
                            log.Fine("Asset");
                            Info.Append("@VAA_Asset_ID@: ");
                            //MVABInvoiceLine invoiceLine = new MVABInvoiceLine(GetCtx(), line.Get_ID(), Get_TrxName());
                            if (product.IsOneAssetPerUOM())
                            {
                                for (int j = 0; j < noAssets; j++)
                                {
                                    if (j > 0)
                                        Info.Append(" - ");
                                    int deliveryCount = j + 1;
                                    if (product.IsOneAssetPerUOM())
                                        deliveryCount = 0;
                                    MVAAsset asset = new MVAAsset(this, line, deliveryCount);
                                    // Change By Mohit Amortization process .3/11/2016 
                                    if (countVA038 > 0)
                                    {
                                        if (Util.GetValueOfInt(product.Get_Value("VA038_AmortizationTemplate_ID")) > 0)
                                        {
                                            asset.Set_Value("VA038_AmortizationTemplate_ID", Util.GetValueOfInt(product.Get_Value("VA038_AmortizationTemplate_ID")));
                                        }
                                    }
                                    if (!asset.Save(Get_TrxName()))
                                    {
                                        _processMsg = "Could not create Asset";
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                    else
                                    {
                                        asset.SetName(asset.GetName() + "_" + asset.GetValue());
                                        asset.Save(Get_TrxName());
                                    }
                                    Info.Append(asset.GetValue());
                                }
                            }
                            else
                            {
                                #region[Added by Sukhwinder (mantis ID: 1762, point 1)]
                                if (noAssets > 0 && Util.GetValueOfInt(product.GetVAA_AssetGroup_ID()) > 0)
                                {
                                    MVAAsset asset = new MVAAsset(this, line, noAssets);
                                    if (!asset.Save(Get_TrxName()))
                                    {
                                        _processMsg = "Could not create Asset";
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                    else
                                    {
                                        asset.SetName(asset.GetName() + "_" + asset.GetValue());
                                        asset.Save(Get_TrxName());
                                    }
                                    Info.Append(asset.GetValue());
                                }
                                #endregion
                            }
                        }
                    }

                    //	Lead/Request
                    line.CreateLeadRequest(this);

                    //Enhaced by amit 09-12-2016 for calculating Foriegn Cost
                    #region calculating Foriegn Cost
                    try
                    {
                        if (!IsSOTrx() && !IsReturnTrx() && line.GetVAM_Inv_InOutLine_ID() > 0) // for Invoice(vendor)
                        {
                            MVAMProduct product1 = new MVAMProduct(GetCtx(), line.GetVAM_Product_ID(), Get_Trx());
                            MVABInvoice invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_Trx());
                            if (product1 != null && product1.GetProductType() == "I" && product1.GetVAM_Product_ID() > 0) // for Item Type product
                            {
                                if (!MVAMCostForeignCurrency.InsertForeignCostAverageInvoice(GetCtx(), invoice, line, Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    log.Severe("Error occured during updating/inserting VAM_ProductCost_ForeignCurrency  against Average Invoice.");
                                    _processMsg = "Could not update Foreign Currency Cost";
                                    return DocActionVariables.STATUS_INVALID;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                    #endregion

                    //Added by Bharat on 11-April-2017 for Asset Expenses
                    #region Calculating Cost on Expenses
                    Tuple<String, String, String> mInfo = null;
                    if (Env.HasModulePrefix("VAFAM_", out mInfo) && line.Get_ColumnIndex("VAFAM_IsAssetRelated") > 0)
                    {
                        if (!IsSOTrx() && !IsReturnTrx() && Util.GetValueOfBool(line.Get_Value("VAFAM_IsAssetRelated")))
                        {
                            //                            DataSet _dsAsset = null;
                            //                            StringBuilder _sql = new StringBuilder();
                            //                            _sql.Append(@" SELECT VAM_ProductCost.VAF_CLIENT_ID, VAM_ProductCost.VAF_ORG_ID, VAM_ProductCost.VAM_ProductCostElement_ID,
                            //                                       VAM_ProductCost.VAB_ACCOUNTBOOK_ID, VAM_ProductCost.VAM_CostType_ID,
                            //                                       VAM_ProductCost.BASISTYPE, VAM_Product.VAB_UOM_ID, VAM_ProductCostElement.COSTINGMETHOD,
                            //                                       VAM_ProductCost.VAM_PFeature_SetInstance_ID, VAM_ProductCost.CURRENTCOSTPRICE, VAM_ProductCost.FUTURECOSTPRICE
                            //                                       FROM VAM_ProductCost VAM_ProductCostElement ON(VAM_ProductCostElement.VAM_ProductCostElement_ID  =VAM_ProductCost.VAM_ProductCostElement_ID)
                            //                                       WHERE VAM_ProductCostElement.COSTELEMENTTYPE='M' AND VAM_ProductCost.VAF_CLIENT_ID =" + GetCtx().GetVAF_Client_ID() +
                            //                                       @" AND VAM_ProductCost.ISACTIVE ='Y' AND VAM_ProductCost.IsAssetCost='Y' AND VAM_ProductCost.VAA_ASSET_ID =" + line.GetA_Asset_ID());
                            //                            _dsAsset = DB.ExecuteDataset(_sql.ToString());
                            //                            _sql.Clear();
                            //                            if (_dsAsset != null && _dsAsset.Tables[0].Rows.Count > 0)
                            //                            {                               
                            //                                MAsset _asset = new MAsset(GetCtx(), line.GetA_Asset_ID(), Get_Trx());
                            //                                for (int k = 0; k < _dsAsset.Tables[0].Rows.Count; k++)
                            //                                {
                            //                                    MVAMVAMProductCostElement _costElement = new MVAMVAMProductCostElement(GetCtx(), Util.GetValueOfInt(_dsAsset.Tables[0].Rows[k]["VAM_ProductCostElement_ID"]), Get_TrxName());
                            //                                    MAcctSchema _acctsch = null;
                            //                                    MVAMVAMProductCost _cost = null;
                            //                                    Decimal lineAmt = MConversionRate.ConvertBase(GetCtx(), line.GetLineTotalAmt(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                            //                                    _acctsch = new MAcctSchema(GetCtx(), Util.GetValueOfInt(_dsAsset.Tables[0].Rows[k]["VAB_ACCOUNTBOOK_ID"]), Get_TrxName());
                            //                                    _cost = new MVAMVAMProductCost(line.GetProduct(), Util.GetValueOfInt(_dsAsset.Tables[0].Rows[k]["VAM_PFeature_SetInstance_ID"]),
                            //                                        _acctsch, Util.GetValueOfInt(_dsAsset.Tables[0].Rows[k]["VAF_ORG_ID"]),
                            //                                        Util.GetValueOfInt(_dsAsset.Tables[0].Rows[k]["VAM_ProductCostElement_ID"]),
                            //                                        line.GetA_Asset_ID());
                            //                                    _cost.SetCurrentCostPrice(Util.GetValueOfDecimal(_dsAsset.Tables[0].Rows[k]["CURRENTCOSTPRICE"]) + lineAmt);
                            //                                    _cost.SetFutureCostPrice(Util.GetValueOfDecimal(_dsAsset.Tables[0].Rows[k]["FUTURECOSTPRICE"]) + lineAmt);
                            //                                    if (!_cost.Save(Get_TrxName()))
                            //                                    {
                            //                                        _log.Info("Asset Cost Line Not Saved For Costing Element " + _costElement.GetName());
                            //                                    }
                            //                                }
                            //                            }
                            //                            else
                            //                            {
                            //                                _processMsg = "Asset Cost Not Found";
                            //                                return DocActionVariables.STATUS_INVALID;
                            //                            }
                            Decimal lineAmt = MVABExchangeRate.ConvertBase(GetCtx(), line.GetLineTotalAmt(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                            //Pratap: Added check to update asset cost only in case of Capital/Expense=Capital
                            if (Util.GetValueOfString(line.Get_Value("VAFAM_CapitalExpense")) == "C")
                            {
                                string sql = " Update VAM_ProductCost set CurrentCostPrice = CurrentCostPrice + " + lineAmt + ",futurecostprice = futurecostprice + "
                                    + lineAmt + " Where  VAA_ASSET_ID = " + line.GetA_Asset_ID();

                                int update = DB.ExecuteQuery(sql, null, Get_TrxName());
                            }
                            //Added by Bharat on 12-April-2017 for Asset Expenses
                            #region To Mark Entry on Asset Expenses
                            PO po = MVAFTableView.GetPO(GetCtx(), "VAFAM_Expense", 0, Get_Trx());
                            if (po != null)
                            {
                                po.SetVAF_Client_ID(GetVAF_Client_ID());
                                po.SetVAF_Org_ID(GetVAF_Org_ID());
                                po.Set_Value("VAB_Invoice_ID", GetVAB_Invoice_ID());
                                po.Set_Value("DateAcct", GetDateAcct());
                                po.Set_ValueNoCheck("VAA_Asset_ID", line.GetA_Asset_ID());
                                //po.Set_Value("Amount", line.GetLineTotalAmt());
                                po.Set_Value("VAB_Charge_ID", line.GetVAB_Charge_ID());
                                po.Set_Value("VAM_PFeature_SetInstance_ID", line.GetVAM_PFeature_SetInstance_ID());
                                po.Set_Value("VAM_Product_ID", line.GetVAM_Product_ID());
                                po.Set_Value("VAB_UOM_ID", line.GetVAB_UOM_ID());
                                po.Set_Value("VAB_TaxRate_ID", line.GetVAB_TaxRate_ID());
                                //po.Set_Value("Price", line.GetPriceActual());
                                po.Set_Value("Qty", line.GetQtyEntered());
                                po.Set_Value("VAFAM_CapitalExpense", line.Get_Value("VAFAM_CapitalExpense"));

                                Decimal LineTotalAmt_ = MVABExchangeRate.ConvertBase(GetCtx(), line.GetLineTotalAmt(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                                Decimal PriceActual_ = MVABExchangeRate.ConvertBase(GetCtx(), line.GetPriceActual(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                                po.Set_Value("Amount", LineTotalAmt_);
                                po.Set_Value("Price", PriceActual_);

                                if (!po.Save())
                                {
                                    _log.Info("Asset Expense Not Saved For Asset ");
                                }
                                //In Case of Capital Expense ..Asset Gross Value will be updated  //Arpit //Ashish 12 Feb,2017
                                else
                                {
                                    String CapitalExpense_ = "";
                                    CapitalExpense_ = Util.GetValueOfString(line.Get_Value("VAFAM_CapitalExpense"));
                                    if (CapitalExpense_ == "C")
                                    {
                                        MVAAsset asst = new MVAAsset(GetCtx(), line.GetA_Asset_ID(), Get_TrxName());
                                        //Update Asset Gross Value in Case of Capital Expense
                                        if (asst.Get_ColumnIndex("VAFAM_AssetGrossValue") > 0)
                                        {
                                            Decimal LineNetAmt_ = MVABExchangeRate.ConvertBase(GetCtx(), line.GetLineNetAmt(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                                            asst.Set_Value("VAFAM_AssetGrossValue", Decimal.Add(Util.GetValueOfDecimal(asst.Get_Value("VAFAM_AssetGrossValue")), LineNetAmt_));
                                            if (!asst.Save(Get_TrxName()))
                                            {
                                                _log.Info("Asset Expense Not Updated For Asset ");
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }

                    #endregion

                    #region Create Asset against Ammortization Charge
                    if (Env.HasModulePrefix("VAFAM_", out mInfo) && line.GetVAB_Charge_ID() > 0 && !IsSOTrx() && !IsReturnTrx() && !IsReversal() && countVA038 > 0)
                    {
                        if (!GenerateAssetForAmortizationCharge(Info, line))
                        {
                            _processMsg = "Could not create Asset";
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }
                    #endregion

                    //Enhaced by amit 16-12-2015 for Cost Queue
                    if (client.IsCostImmediate())
                    {
                        bool isCostAdjustableOnLost = false;
                        int count = 0;
                        #region costing calculation
                        if (line != null && line.GetVAB_Invoice_ID() > 0 && line.GetQtyInvoiced() == 0)
                            continue;

                        Decimal ProductLineCost = line.GetProductLineCost(line);

                        // check IsCostAdjustmentOnLost exist on product 
                        string sql = @"SELECT COUNT(*) FROM VAF_Column WHERE IsActive = 'Y' AND 
                                       VAF_TableView_ID =  ( SELECT VAF_TableView_ID FROM VAF_TableView WHERE IsActive = 'Y' AND TableName LIKE 'VAM_Product' ) 
                                       AND ColumnName LIKE 'IsCostAdjustmentOnLost' ";
                        count = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        if (line.GetVAB_OrderLine_ID() > 0)
                        {
                            if (line.GetVAB_Charge_ID() > 0)
                            {
                                #region landed cost Allocation
                                if (!IsSOTrx() && !IsReturnTrx())
                                {
                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), null, 0, "Invoice(Vendor)", null, null, null, line,
                                         null, ProductLineCost, 0, Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                    {
                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                        {
                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                        }
                                        _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                        if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                        {
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                    }
                                    else
                                    {
                                        line.SetIsCostImmediate(true);
                                        line.Save();
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                MVAMProduct product1 = new MVAMProduct(GetCtx(), line.GetVAM_Product_ID(), Get_Trx());
                                if (product1.GetProductType() == "E" && product1.GetVAM_Product_ID() > 0) // for Expense type product
                                {
                                    #region for Expense type product
                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, 0, "Invoice(Vendor)", null, null, null, line,
                                        null, ProductLineCost, 0, Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                    {
                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                        {
                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                        }
                                        _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                        if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                        {
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                    }
                                    else
                                    {
                                        line.SetIsCostImmediate(true);
                                        line.Save();
                                    }
                                    #endregion
                                }
                                else if (product1.GetProductType() == "I" && product1.GetVAM_Product_ID() > 0) // for Item Type product
                                {
                                    #region for Item Type product
                                    // check isCostAdjutableonLost is true on product and mr qty is less than invoice qty then consider mr qty else inv qty
                                    if (count > 0)
                                    {
                                        isCostAdjustableOnLost = product1.IsCostAdjustmentOnLost();
                                    }

                                    MVABOrder order1 = new MVABOrder(GetCtx(), GetVAB_Order_ID(), null);
                                    MVABOrderLine ol1 = new MVABOrderLine(GetCtx(), line.GetVAB_OrderLine_ID(), Get_Trx());
                                    if (order1.GetVAB_Order_ID() == 0)
                                    {
                                        //MVABOrderLine ol1 = new MVABOrderLine(GetCtx(), line.GetVAB_OrderLine_ID(), Get_Trx());
                                        order1 = new MVABOrder(GetCtx(), ol1.GetVAB_Order_ID(), Get_Trx());
                                    }
                                    if (order1.IsSOTrx() && !order1.IsReturnTrx()) // SO
                                    {
                                        #region against SO
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Customer)", null, null, null, line, null,
                                              Decimal.Negate(ProductLineCost), Decimal.Negate(line.GetQtyInvoiced()),
                                              Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                            if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                            {
                                                return DocActionVariables.STATUS_INVALID;
                                            }
                                        }
                                        else
                                        {
                                            line.SetIsCostImmediate(true);
                                            line.Save();
                                        }
                                        #endregion
                                    }
                                    else if (!order1.IsSOTrx() && !order1.IsReturnTrx()) // PO
                                    {
                                        #region against Purchse Order
                                        // calculate cost of MR first if not calculate which is linked with that invoice line
                                        bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), GetVAF_Client_ID(), product1.GetVAM_Product_ID(), Get_Trx());
                                        if (line.GetVAM_Inv_InOutLine_ID() > 0)
                                        {
                                            sLine = new MVAMInvInOutLine(GetCtx(), line.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                            // get warehouse refernce from header -- InOut
                                            int VAM_Warehouse_Id = sLine.GetVAM_Warehouse_ID();
                                            if (!sLine.IsCostImmediate())
                                            {
                                                // get cost from Product Cost before cost calculation
                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                         product1.GetVAM_Product_ID(), sLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                 @" WHERE VAM_Inv_InOutLine_ID = " + sLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());

                                                Decimal ProductOrderLineCost = ol1.GetProductLineCost(ol1);
                                                Decimal ProductOrderPriceActual = ProductOrderLineCost / ol1.GetQtyEntered();

                                                // calculate cost
                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, sLine.GetVAM_PFeature_SetInstance_ID(),
                                            "Material Receipt", null, sLine, null, line, null,
                                            order1 != null && order1.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, ol1.GetQtyOrdered()), sLine.GetMovementQty())
                                                : Decimal.Multiply(ProductOrderPriceActual, sLine.GetQtyEntered()),
                                             sLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut, optionalstr: "window"))
                                                {
                                                    _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                                    if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                                    {
                                                        return DocActionVariables.STATUS_INVALID;
                                                    }
                                                }
                                                else
                                                {
                                                    // get cost from Product Cost after cost calculation
                                                    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                             product1.GetVAM_Product_ID(), sLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                                    DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = CASE WHEN CurrentCostPrice <> 0 THEN CurrentCostPrice ELSE " + currentCostPrice +
                                                                     @" END , IsCostImmediate = 'Y' , 
                                                                      PostCurrentCostPrice = CASE WHEN 1 = " + (isUpdatePostCurrentcostPriceFromMR ? 1 : 0) +
                                                                     @" THEN " + currentCostPrice + @" ELSE PostCurrentCostPrice END 
                                                                     WHERE VAM_Inv_InOutLine_ID = " + sLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                }
                                            }

                                            // for reverse record -- pick qty from VAM_MatchInvoiceoiceCostTrack 
                                            decimal matchInvQty = 0;
                                            if (GetDescription() != null && GetDescription().Contains("{->"))
                                            {
                                                matchInvQty = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT SUM(QTY) FROM VAM_MatchInvoiceoiceCostTrack WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx()));
                                                matchInvQty = decimal.Negate(matchInvQty);
                                            }

                                            // calculate Pre Cost - means cost before updation price impact of current record
                                            if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0 && inv.Get_ColumnIndex("CurrentCostPrice") >= 0)
                                            {
                                                // get cost from Product Cost before cost calculation
                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                         product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                                DB.ExecuteQuery("UPDATE VAM_MatchInvoice SET CurrentCostPrice = " + currentCostPrice +
                                                                 @" WHERE VAM_MatchInvoice_ID = " + inv.GetVAM_MatchInvoice_ID(), null, Get_Trx());

                                            }

                                            // calculate invoice line costing after calculating costing of linked MR line 
                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                                  "Invoice(Vendor)", null, sLine, null, line, null,
                                                  count > 0 && isCostAdjustableOnLost && ((inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : Decimal.Negate(matchInvQty)) < (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(line.GetQtyInvoiced()) : line.GetQtyInvoiced()) ? ProductLineCost : Decimal.Multiply(Decimal.Divide(ProductLineCost, line.GetQtyInvoiced()), (inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : matchInvQty),
                                                //count > 0 && isCostAdjustableOnLost && line.GetVAM_Inv_InOutLine_ID() > 0 && sLine.GetMovementQty() < (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(line.GetQtyInvoiced()) : line.GetQtyInvoiced()) ? (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(sLine.GetMovementQty()) : sLine.GetMovementQty()) : line.GetQtyInvoiced(),
                                                GetDescription() != null && GetDescription().Contains("{->") ? matchInvQty : inv.GetQty(),
                                                Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                            {
                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                {
                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                }
                                                _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                                if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                                {
                                                    return DocActionVariables.STATUS_INVALID;
                                                }
                                            }
                                            else
                                            {
                                                line.SetIsCostImmediate(true);
                                                line.Save(Get_Trx());

                                                if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0)
                                                {
                                                    if (inv.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                    {
                                                        // get cost from Product Cost after cost calculation
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                                 product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                                        inv.SetPostCurrentCostPrice(currentCostPrice);
                                                    }
                                                    inv.SetIsCostImmediate(true);
                                                    inv.Save(Get_Trx());

                                                    // update the Post current price after Invoice receving on inoutline
                                                    if (!isUpdatePostCurrentcostPriceFromMR)
                                                    {
                                                        DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =  " + currentCostPrice +
                                                                         @"  WHERE VAM_Inv_InOutLine_ID = " + inv.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                    }
                                                }
                                                else if (GetDescription() != null && GetDescription().Contains("{->"))
                                                {
                                                    int no = DB.ExecuteQuery("UPDATE VAM_MatchInvoiceoiceCostTrack SET IsReversedCostCalculated = 'Y' WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx());
                                                    // update Post cost as 0 on inoutline
                                                    if (!isUpdatePostCurrentcostPriceFromMR)
                                                    {
                                                        no = DB.ExecuteQuery(@"UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice = 0 WHERE VAM_Inv_InOutLine_id IN 
                                                                               (SELECT VAM_Inv_InOutLine_id FROM VAM_MatchInvoiceoiceCostTrack WHERE 
                                                                                Rev_VAB_InvoiceLine_ID =  " + line.GetVAB_InvoiceLine_ID() + " ) ", null, Get_Trx());
                                                    }
                                                }

                                            }
                                        }
                                        #endregion
                                    }
                                    else if (order1.IsSOTrx() && order1.IsReturnTrx()) // CRMA
                                    {
                                        #region CRMA
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                          "Invoice(Customer)", null, null, null, line, null, ProductLineCost,
                                           line.GetQtyInvoiced(), Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                            if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                            {
                                                return DocActionVariables.STATUS_INVALID;
                                            }
                                        }
                                        else
                                        {
                                            line.SetIsCostImmediate(true);
                                            line.Save();
                                        }
                                        #endregion
                                    }
                                    else if (!order1.IsSOTrx() && order1.IsReturnTrx()) // VRMA
                                    {
                                        #region when Ap Credit memo is independent.
                                        // when Ap Credit memo is alone and document type having setting as "Treat As Discount" then we will do a impact on costing.
                                        // this is bcz of giving discount for particular product
                                        MVABDocTypes docType = new MVABDocTypes(GetCtx(), GetVAB_DocTypesTarget_ID(), Get_Trx());
                                        if (docType.GetDocBaseType() == "APC" && line.GetVAB_OrderLine_ID() == 0 &&
                                            line.GetVAM_Inv_InOutLine_ID() == 0 && line.GetVAM_Product_ID() > 0 && docType.IsTreatAsDiscount())
                                        {
                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Vendor)", null, null, null, line, null, Decimal.Negate(ProductLineCost), Decimal.Negate(line.GetQtyInvoiced())
                                              , Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                            {
                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                {
                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                }
                                                _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                                if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                                {
                                                    return DocActionVariables.STATUS_INVALID;
                                                }
                                            }
                                            else
                                            {
                                                if (line.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                {
                                                    // get post cost after invoice cost calculation and update on invoice
                                                    currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), line.GetVAF_Org_ID(),
                                                                                                    product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                    line.SetPostCurrentCostPrice(currentCostPrice);
                                                }
                                                line.SetIsCostImmediate(true);
                                                line.Save();
                                            }
                                        }
                                        #endregion

                                        #region handle Costing for return to vendor either linked with (MR + PO) or with (MR)
                                        if (docType.GetDocBaseType() == "APC" &&
                                           line.GetVAM_Inv_InOutLine_ID() > 0 && line.GetVAM_Product_ID() > 0)
                                        {
                                            bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), GetVAF_Client_ID(), product1.GetVAM_Product_ID(), Get_Trx());
                                            // if qty is reduced through Return to vendor then we reduce amount else not
                                            sLine = new MVAMInvInOutLine(GetCtx(), line.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                            int VAM_Warehouse_Id = sLine.GetVAM_Warehouse_ID();
                                            if (sLine.IsCostImmediate())
                                            {
                                                // for reverse record -- pick qty from VAM_MatchInvoiceoiceCostTrack 
                                                decimal matchInvQty = 0;
                                                if (GetDescription() != null && GetDescription().Contains("{->"))
                                                {
                                                    matchInvQty = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT SUM(QTY) FROM VAM_MatchInvoiceoiceCostTrack WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx()));
                                                }

                                                // calculate Pre Cost - means cost before updation price impact of current record
                                                if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0 && inv.Get_ColumnIndex("CurrentCostPrice") >= 0)
                                                {
                                                    // get cost from Product Cost before cost calculation
                                                    currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                             product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id);
                                                    DB.ExecuteQuery("UPDATE VAM_MatchInvoice SET CurrentCostPrice = " + currentCostPrice +
                                                                     @" WHERE VAM_MatchInvoice_ID = " + inv.GetVAM_MatchInvoice_ID(), null, Get_Trx());

                                                }

                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                                  "Invoice(Vendor)-Return", null, sLine, null, line, null,
                                                  count > 0 && isCostAdjustableOnLost &&
                                                  ((inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : matchInvQty) <
                                                  (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(line.GetQtyInvoiced()) : line.GetQtyInvoiced())
                                                  ? Decimal.Negate(ProductLineCost)
                                                  : Decimal.Negate(Decimal.Multiply(Decimal.Divide(ProductLineCost, line.GetQtyInvoiced()), (inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : Decimal.Negate(matchInvQty))),
                                                  GetDescription() != null && GetDescription().Contains("{->") ? (matchInvQty) : Decimal.Negate(inv.GetQty()),
                                                   Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                                {
                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                    {
                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                    }
                                                    //_processMsg = "Could not create Product Costs";

                                                    line.SetIsCostImmediate(false);
                                                    line.Save();

                                                    if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0)
                                                    {
                                                        if (inv.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                        {
                                                            inv.SetPostCurrentCostPrice(0);
                                                        }
                                                        inv.SetIsCostImmediate(false);
                                                        inv.Save(Get_Trx());

                                                        // update the Post current price after Invoice receving on inoutline
                                                        if (!isUpdatePostCurrentcostPriceFromMR)
                                                        {
                                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =  " + 0 +
                                                                             @" , IsCostImmediate = 'Y' WHERE VAM_Inv_InOutLine_ID = " + inv.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                    }

                                                    _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                                    if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                                    {
                                                        return DocActionVariables.STATUS_INVALID;
                                                    }
                                                }
                                                else
                                                {
                                                    line.SetIsCostImmediate(true);
                                                    line.Save();

                                                    if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0)
                                                    {
                                                        if (inv.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                        {
                                                            // get cost from Product Cost after cost calculation
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                                     product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id);
                                                            inv.SetPostCurrentCostPrice(currentCostPrice);
                                                        }
                                                        inv.SetIsCostImmediate(true);
                                                        inv.Save(Get_Trx());

                                                        // update the Post current price after Invoice receving on inoutline
                                                        if (!isUpdatePostCurrentcostPriceFromMR)
                                                        {
                                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =  " + currentCostPrice +
                                                                             @" WHERE VAM_Inv_InOutLine_ID = " + inv.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                    }
                                                    else if (GetDescription() != null && GetDescription().Contains("{->"))
                                                    {
                                                        int no = DB.ExecuteQuery("UPDATE VAM_MatchInvoiceoiceCostTrack SET IsReversedCostCalculated = 'Y' WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx());
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                    #endregion
                                }
                            }
                        }
                        else
                        {
                            if (line.GetVAB_Charge_ID() > 0)  // for Expense type
                            {
                                if (!IsSOTrx() && !IsReturnTrx())
                                {
                                    #region landed cost allocation
                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), null, 0, "Invoice(Vendor)", null, null, null, line,
                                        null, ProductLineCost, 0, Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                    {
                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                        {
                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                        }
                                        _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                        if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                        {
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                    }
                                    else
                                    {
                                        line.SetIsCostImmediate(true);
                                        line.Save();
                                    }
                                    #endregion
                                }
                            }
                            MVAMProduct product1 = new MVAMProduct(GetCtx(), line.GetVAM_Product_ID(), Get_Trx());
                            if (product1.GetProductType() == "E" && product1.GetVAM_Product_ID() > 0) // for Expense type product
                            {
                                #region for Expense type product
                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, 0, "Invoice(Vendor)", null, null, null, line,
                                    null, ProductLineCost, 0, Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                {
                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                    {
                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                    }
                                    _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                    if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                    {
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                }
                                else
                                {
                                    line.SetIsCostImmediate(true);
                                    line.Save();
                                }
                                #endregion
                            }
                            else if (product1.GetProductType() == "I" && product1.GetVAM_Product_ID() > 0) // for Item Type product
                            {
                                if (count > 0)
                                {
                                    isCostAdjustableOnLost = product1.IsCostAdjustmentOnLost();
                                }

                                #region for Item Type product
                                if (IsSOTrx() && !IsReturnTrx()) // SO
                                {
                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                          "Invoice(Customer)", null, null, null, line, null, Decimal.Negate(ProductLineCost),
                                          Decimal.Negate(line.GetQtyInvoiced()), Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                    {
                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                        {
                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                        }
                                        _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                        if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                        {
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                    }
                                    else
                                    {
                                        line.SetIsCostImmediate(true);
                                        line.Save();
                                    }
                                }
                                else if (!IsSOTrx() && !IsReturnTrx()) // PO
                                {
                                    bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), GetVAF_Client_ID(), product1.GetVAM_Product_ID(), Get_Trx());
                                    // calculate cost of MR first if not calculate which is linked with that invoice line
                                    int VAM_Warehouse_Id = 0;
                                    if (line.GetVAM_Inv_InOutLine_ID() > 0)
                                    {
                                        sLine = new MVAMInvInOutLine(GetCtx(), line.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                        VAM_Warehouse_Id = sLine.GetVAM_Warehouse_ID();
                                        if (!sLine.IsCostImmediate())
                                        {
                                            // get cost from Product Cost before cost calculation
                                            currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                     product1.GetVAM_Product_ID(), sLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                             @" WHERE VAM_Inv_InOutLine_ID = " + sLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());

                                            // calculate cost
                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, sLine.GetVAM_PFeature_SetInstance_ID(),
                                        "Material Receipt", null, sLine, null, line, null, 0, sLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut, optionalstr: "window"))
                                            {
                                                _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                                if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                                {
                                                    return DocActionVariables.STATUS_INVALID;
                                                }
                                            }
                                            else
                                            {
                                                // get cost from Product Cost after cost calculation
                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                         product1.GetVAM_Product_ID(), sLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = CASE WHEN CurrentCostPrice <> 0 THEN CurrentCostPrice ELSE " + currentCostPrice +
                                                                     @" END , IsCostImmediate = 'Y' , 
                                                     PostCurrentCostPrice = CASE WHEN 1 = " + (isUpdatePostCurrentcostPriceFromMR ? 1 : 0) +
                                                     @" THEN " + currentCostPrice + @" ELSE PostCurrentCostPrice END 
                                                 WHERE VAM_Inv_InOutLine_ID = " + sLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                sLine.SetIsCostImmediate(true);
                                            }
                                        }
                                    }

                                    // for reverse record -- pick qty from VAM_MatchInvoiceoiceCostTrack 
                                    decimal matchInvQty = 0;
                                    if (GetDescription() != null && GetDescription().Contains("{->"))
                                    {
                                        matchInvQty = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT SUM(QTY) FROM VAM_MatchInvoiceoiceCostTrack WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx()));
                                        matchInvQty = decimal.Negate(matchInvQty);
                                    }

                                    if (line.GetVAM_Inv_InOutLine_ID() > 0 && sLine.IsCostImmediate())
                                    {
                                        // calculate Pre Cost - means cost before updation price impact of current record
                                        if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0 && inv.Get_ColumnIndex("CurrentCostPrice") >= 0)
                                        {
                                            // get cost from Product Cost before cost calculation
                                            currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                     product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                            DB.ExecuteQuery("UPDATE VAM_MatchInvoice SET CurrentCostPrice = " + currentCostPrice +
                                                             @" WHERE VAM_MatchInvoice_ID = " + inv.GetVAM_MatchInvoice_ID(), null, Get_Trx());

                                        }

                                        // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Vendor)", null, null, null, line, null,
                                              count > 0 && isCostAdjustableOnLost && ((inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : Decimal.Negate(matchInvQty)) < (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(line.GetQtyInvoiced()) : line.GetQtyInvoiced()) ? ProductLineCost : Decimal.Multiply(Decimal.Divide(ProductLineCost, line.GetQtyInvoiced()), (inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : matchInvQty),
                                            //count > 0 && isCostAdjustableOnLost && line.GetVAM_Inv_InOutLine_ID() > 0 && sLine.GetMovementQty() < (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(line.GetQtyInvoiced()) : line.GetQtyInvoiced()) ? (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(sLine.GetMovementQty()) : sLine.GetMovementQty()) : line.GetQtyInvoiced(),
                                            GetDescription() != null && GetDescription().Contains("{->") ? matchInvQty : inv.GetQty(), Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                            if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                            {
                                                return DocActionVariables.STATUS_INVALID;
                                            }
                                        }
                                        else
                                        {
                                            line.SetIsCostImmediate(true);
                                            line.Save(Get_Trx());

                                            if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0)
                                            {
                                                if (inv.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                {
                                                    // get cost from Product Cost after cost calculation
                                                    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                             product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id, false);
                                                    inv.SetPostCurrentCostPrice(currentCostPrice);
                                                }
                                                inv.SetIsCostImmediate(true);
                                                inv.Save(Get_Trx());

                                                // update the Post current price after Invoice receving on inoutline
                                                if (!isUpdatePostCurrentcostPriceFromMR)
                                                {
                                                    DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =  " + currentCostPrice +
                                                                     @" WHERE VAM_Inv_InOutLine_ID = " + inv.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                }
                                            }
                                            else if (GetDescription() != null && GetDescription().Contains("{->"))
                                            {
                                                int no = DB.ExecuteQuery("UPDATE VAM_MatchInvoiceoiceCostTrack SET IsReversedCostCalculated = 'Y' WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx());
                                                // update Post cost as 0 on inoutline
                                                if (!isUpdatePostCurrentcostPriceFromMR)
                                                {
                                                    no = DB.ExecuteQuery(@"UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice = 0 WHERE VAM_Inv_InOutLine_id IN 
                                                                               (SELECT VAM_Inv_InOutLine_id FROM VAM_MatchInvoiceoiceCostTrack WHERE 
                                                                                Rev_VAB_InvoiceLine_ID =  " + line.GetVAB_InvoiceLine_ID() + " ) ", null, Get_Trx());
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (IsSOTrx() && IsReturnTrx()) // CRMA
                                {
                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                      "Invoice(Customer)", null, null, null, line, null, ProductLineCost, line.GetQtyInvoiced(),
                                      Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                    {
                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                        {
                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                        }
                                        _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                        if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                        {
                                            return DocActionVariables.STATUS_INVALID;
                                        }
                                    }
                                    else
                                    {
                                        line.SetIsCostImmediate(true);
                                        line.Save();
                                    }
                                }
                                else if (!IsSOTrx() && IsReturnTrx()) // VRMA
                                {
                                    #region when Ap Credit memo is alone then we will do a impact on costing.
                                    // when Ap Credit memo is alone then we will do a impact on costing.
                                    // this is bcz of giving discount for particular product
                                    MVABDocTypes docType = new MVABDocTypes(GetCtx(), GetVAB_DocTypesTarget_ID(), Get_Trx());
                                    if (docType.GetDocBaseType() == "APC" && docType.IsTreatAsDiscount() && line.GetVAB_OrderLine_ID() == 0 && line.GetVAM_Inv_InOutLine_ID() == 0 && line.GetVAM_Product_ID() > 0)
                                    {
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                          "Invoice(Vendor)", null, null, null, line, null, Decimal.Negate(ProductLineCost), Decimal.Negate(line.GetQtyInvoiced()),
                                          Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _processMsg = Msg.GetMsg(GetCtx(), "VIS_CostNotCalculated");// "Could not create Product Costs";
                                            if (client.Get_ColumnIndex("IsCostMandatory") > 0 && client.IsCostMandatory())
                                            {
                                                return DocActionVariables.STATUS_INVALID;
                                            }
                                        }
                                        else
                                        {
                                            if (line.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                            {
                                                // get post cost after invoice cost calculation and update on invoice
                                                currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), line.GetVAF_Org_ID(),
                                                                                                product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                line.SetPostCurrentCostPrice(currentCostPrice);
                                            }
                                            line.SetIsCostImmediate(true);
                                            line.Save();
                                        }
                                    }
                                    #endregion

                                    #region handle Costing for return to vendor either linked with (MR + PO) or with (MR)
                                    if (docType.GetDocBaseType() == "APC" &&
                                       line.GetVAM_Inv_InOutLine_ID() > 0 && line.GetVAM_Product_ID() > 0)
                                    {
                                        bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), GetVAF_Client_ID(), product1.GetVAM_Product_ID(), Get_Trx());
                                        // if qty is reduced through Return to vendor then we reduce amount else not
                                        sLine = new MVAMInvInOutLine(GetCtx(), line.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                        if (sLine.IsCostImmediate())
                                        {
                                            int VAM_Warehouse_Id = sLine.GetVAM_Warehouse_ID();
                                            // for reverse record -- pick qty from VAM_MatchInvoiceoiceCostTrack 
                                            decimal matchInvQty = 0;
                                            if (GetDescription() != null && GetDescription().Contains("{->"))
                                            {
                                                matchInvQty = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT SUM(QTY) FROM VAM_MatchInvoiceoiceCostTrack WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx()));
                                            }

                                            // calculate Pre Cost - means cost before updation price impact of current record
                                            if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0 && inv.Get_ColumnIndex("CurrentCostPrice") >= 0)
                                            {
                                                // get cost from Product Cost before cost calculation
                                                currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                         product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id);
                                                DB.ExecuteQuery("UPDATE VAM_MatchInvoice SET CurrentCostPrice = " + currentCostPrice +
                                                                 @" WHERE VAM_MatchInvoice_ID = " + inv.GetVAM_MatchInvoice_ID(), null, Get_Trx());

                                            }

                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), GetVAF_Org_ID(), product1, line.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Vendor)-Return", null, sLine, null, line, null,
                                              count > 0 && isCostAdjustableOnLost &&
                                              ((inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : matchInvQty) <
                                              (GetDescription() != null && GetDescription().Contains("{->") ? Decimal.Negate(line.GetQtyInvoiced()) : line.GetQtyInvoiced())
                                              ? Decimal.Negate(ProductLineCost)
                                              : Decimal.Negate(Decimal.Multiply(Decimal.Divide(ProductLineCost, line.GetQtyInvoiced()), (inv != null && inv.GetVAM_Inv_InOutLine_ID() > 0) ? inv.GetQty() : Decimal.Negate(matchInvQty))),
                                              GetDescription() != null && GetDescription().Contains("{->") ? (matchInvQty) : Decimal.Negate(inv.GetQty()),
                                               Get_Trx(), out conversionNotFoundInvoice, optionalstr: "window"))
                                            {
                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                {
                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                }
                                                _processMsg = "Could not create Product Costs";

                                                line.SetIsCostImmediate(false);
                                                line.Save();

                                                if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0)
                                                {
                                                    if (inv.Get_ColumnIndex("CurrentCostPrice") >= 0)
                                                    {
                                                        inv.SetCurrentCostPrice(0);
                                                    }
                                                    inv.SetIsCostImmediate(false);
                                                    inv.Save(Get_Trx());

                                                    // update the Post current price after Invoice receving on inoutline
                                                    if (!isUpdatePostCurrentcostPriceFromMR)
                                                    {
                                                        DB.ExecuteQuery(@"UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =  0 
                                                                      WHERE VAM_Inv_InOutLine_ID = " + inv.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                line.SetIsCostImmediate(true);
                                                line.Save();

                                                if (inv != null && inv.GetVAM_MatchInvoice_ID() > 0)
                                                {
                                                    if (inv.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                    {
                                                        // get cost from Product Cost after cost calculation
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                                 product1.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_Id);
                                                        inv.SetPostCurrentCostPrice(currentCostPrice);
                                                    }
                                                    inv.SetIsCostImmediate(true);
                                                    inv.Save(Get_Trx());

                                                    // update the Post current price after Invoice receving on inoutline
                                                    if (!isUpdatePostCurrentcostPriceFromMR)
                                                    {
                                                        DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =  " + currentCostPrice +
                                                                         @" WHERE VAM_Inv_InOutLine_ID = " + inv.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                    }
                                                }
                                                else if (GetDescription() != null && GetDescription().Contains("{->"))
                                                {
                                                    int no = DB.ExecuteQuery("UPDATE VAM_MatchInvoiceoiceCostTrack SET IsReversedCostCalculated = 'Y' WHERE Rev_VAB_InvoiceLine_ID = " + line.GetVAB_InvoiceLine_ID(), null, Get_Trx());
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                        #endregion
                    }
                    //end

                    //JID_1126: System will check the selected Vendor and product to update the price on Purchasing tab.
                    if (!IsSOTrx() && !IsReturnTrx() && dt.GetDocBaseType() == "API")
                    {
                        MVAMProductPO po = null;
                        if (line.GetVAM_Product_ID() > 0)
                        {

                            po = MVAMProductPO.GetOfVendorProduct(GetCtx(), GetVAB_BusinessPartner_ID(), line.GetVAM_Product_ID(), Get_Trx());
                            if (po != null)
                            {
                                // Check if multiple vendors exist as current vwndor.Mohit
                                if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAM_Product_PO WHERE IsActive='Y' AND IsCurrentVendor='Y' AND  VAM_Product_ID = " + line.GetVAM_Product_ID(), null, Get_Trx())) > 1)
                                {
                                    // Update all vendors on purchasing tab as current vendor = False.
                                    DB.ExecuteQuery("UPDATE VAM_Product_PO SET IsCurrentVendor='N' WHERE VAB_BusinessPartner_ID !=" + GetVAB_BusinessPartner_ID() + " AND VAM_Product_ID= " + line.GetVAM_Product_ID(), null, Get_Trx());
                                    // Set current vendor.
                                    po.SetIsCurrentVendor(true);
                                }
                                po.SetPriceLastInv(line.GetPriceEntered());
                                if (!po.Save())
                                {
                                    _processMsg = Msg.GetMsg(GetCtx(), "NotUpdateInvPrice");
                                    return DocActionVariables.STATUS_INVALID;
                                }
                            }
                        }
                    }

                    //Create RecognoitionPlan and RecognitiontionRun
                    if (Env.IsModuleInstalled("FRPT_") && line.Get_ColumnIndex("VAB_Rev_Recognition_ID") >= 0 && line.Get_Value("VAB_Rev_Recognition_ID") != null && !IsReversal())
                    {
                        if (!MVABRevRecognition.CreateRevenueRecognitionPlan(line.GetVAB_InvoiceLine_ID(), Util.GetValueOfInt(line.Get_Value("VAB_Rev_Recognition_ID")), this))
                        {
                            _processMsg = Msg.GetMsg(GetCtx(), "PlaRunNotCreated");
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }

                }	//	for all lines


                // By Amit For Foreign cost
                if (!IsSOTrx() && !IsReturnTrx()) // for Invoice with MR
                {
                    if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_InvoiceLine WHERE IsFutureCostCalculated = 'N' AND VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx())) <= 0)
                    {
                        int no = Util.GetValueOfInt(DB.ExecuteQuery("UPDATE VAB_Invoice Set IsFutureCostCalculated = 'Y' WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx()));
                    }
                }
                //end


                if (matchInv > 0)
                    Info.Append(" @VAM_MatchInvoice_ID@#").Append(matchInv).Append(" ");
                if (matchPO > 0)
                    Info.Append(" @VAM_MatchPO_ID@#").Append(matchPO).Append(" ");



                //	Update BP Statistics
                bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_TrxName());
                //	Update total revenue and balance / credit limit (reversed on AllocationLine.processIt)
                Decimal invAmt = MVABExchangeRate.ConvertBase(GetCtx(), GetGrandTotal(true),	//	CM adjusted 
                    GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());

                //Added by Vivek for Credit Limit on 24/08/2016

                // JID_0556 :: // Change by Lokesh Chauhan to validate watch % from BP Group, 
                // if it is 0 on BP Group then default to 90 // 12 July 2019
                MVABBPartCategory bpg = new MVABBPartCategory(GetCtx(), bp.GetVAB_BPart_Category_ID(), Get_TrxName());
                Decimal? watchPer = bpg.GetCreditWatchPercent();
                if (watchPer == 0)
                    watchPer = 90;

                Decimal newBalance = 0;
                Decimal newCreditAmt = 0;
                if (bp.GetCreditStatusSettingOn() == "CH")
                {
                    creditLimit = bp.GetSO_CreditLimit();
                    creditVal = bp.GetCreditValidation();
                    //if (invAmt == null)
                    //{
                    //    _processMsg = "Could not convert VAB_Currency_ID=" + GetVAB_Currency_ID()
                    //        + " to base VAB_Currency_ID=" + MClient.Get(GetCtx()).GetVAB_Currency_ID();
                    //    return DocActionVariables.STATUS_INVALID;
                    //}
                    //	Total Balance
                    newBalance = bp.GetTotalOpenBalance(false);
                    //if (newBalance == null)
                    //    newBalance = Env.ZERO;
                    if (IsSOTrx())
                    {
                        newBalance = Decimal.Add(newBalance, invAmt);
                        //
                        if (bp.GetFirstSale() == null)
                        {
                            bp.SetFirstSale(GetDateInvoiced());
                        }
                        Decimal newLifeAmt = bp.GetActualLifeTimeValue();
                        //if (newLifeAmt == null)
                        //{
                        //    newLifeAmt = invAmt;
                        //}
                        //else
                        //{
                        newLifeAmt = Decimal.Add(newLifeAmt, invAmt);
                        // }
                        newCreditAmt = bp.GetSO_CreditUsed();
                        //if (newCreditAmt == null)
                        //{
                        //    newCreditAmt = invAmt;
                        //}
                        //else
                        //{
                        newCreditAmt = Decimal.Add(newCreditAmt, invAmt);
                        //}
                        log.Fine("GrandTotal=" + GetGrandTotal(true) + "(" + invAmt
                            + ") BP Life=" + bp.GetActualLifeTimeValue() + "->" + newLifeAmt
                            + ", Credit=" + bp.GetSO_CreditUsed() + "->" + newCreditAmt
                            + ", Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
                        bp.SetActualLifeTimeValue(newLifeAmt);
                        bp.SetSO_CreditUsed(newCreditAmt);
                        //if (creditLimit > 0 && (X_VAB_BusinessPartner.SOCREDITSTATUS_CreditStop != bp.GetSOCreditStatus()) && (X_VAB_BusinessPartner.SOCREDITSTATUS_NoCreditCheck != bp.GetSOCreditStatus()))
                        //{
                        //    if (((newCreditAmt / creditLimit) * 100) >= 100)
                        //    {
                        //        // JID_0560 // Change here to set credit status to hold instead of stop // Lokesh Chauhan 15 July 2019
                        //        bp.SetSOCreditStatus("H");
                        //    }

                        //    else if (((newCreditAmt / creditLimit) * 100) >= watchPer)
                        //    {
                        //        bp.SetSOCreditStatus("W");
                        //    }
                        //    else
                        //    {
                        //        bp.SetSOCreditStatus("O");
                        //    }
                        //}
                    }	//	SO
                    else
                    {
                        newBalance = Decimal.Subtract(newBalance, invAmt);
                        log.Fine("GrandTotal=" + GetGrandTotal(true) + "(" + invAmt
                            + ") Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
                    }
                    bp.SetTotalOpenBalance(newBalance);
                    bp.SetSOCreditStatus();
                    if (!bp.Save(Get_TrxName()))
                    {
                        _processMsg = "Could not update Business Partner";
                        return DocActionVariables.STATUS_INVALID;
                    }
                }
                // JID_0161 // change here now will check credit settings on field only on Business Partner Header // Lokesh Chauhan 15 July 2019
                else if (bp.GetCreditStatusSettingOn() == X_VAB_BusinessPartner.CREDITSTATUSSETTINGON_CustomerLocation)
                {
                    MVABBPartLocation bpl = new MVABBPartLocation(GetCtx(), GetVAB_BPart_Location_ID(), null);
                    //if (bpl.GetCreditStatusSettingOn() == "CL")
                    //{
                    creditLimit = bpl.GetSO_CreditLimit();
                    creditVal = bpl.GetCreditValidation();

                    newBalance = bpl.GetTotalOpenBalance();

                    if (IsSOTrx())
                    {
                        newBalance = Decimal.Add(newBalance, invAmt);

                        newCreditAmt = bpl.GetSO_CreditUsed();

                        newCreditAmt = Decimal.Add(newCreditAmt, invAmt);

                        log.Fine("GrandTotal=" + GetGrandTotal(true) + "(" + invAmt
                            + ") Credit=" + bp.GetSO_CreditUsed() + "->" + newCreditAmt
                            + ", Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);

                        bpl.SetSO_CreditUsed(newCreditAmt);

                        // set new Life Amount
                        Decimal newLifeAmt = bp.GetActualLifeTimeValue();
                        newLifeAmt = Decimal.Add(newLifeAmt, invAmt);
                        bp.SetActualLifeTimeValue(newLifeAmt);

                        //if (creditLimit > 0)
                        //{
                        //    if (((newCreditAmt / creditLimit) * 100) >= 100)
                        //    {
                        //        // JID_0560 // Change here to set credit status to hold instead of stop // Lokesh Chauhan 15 July 2019
                        //        bpl.SetSOCreditStatus("H");
                        //    }
                        //    else if (((newCreditAmt / creditLimit) * 100) >= watchPer)
                        //    {
                        //        bpl.SetSOCreditStatus("W");
                        //    }
                        //    else
                        //    {
                        //        bpl.SetSOCreditStatus("O");
                        //    }
                        //}
                    }
                    else
                    {
                        newBalance = Decimal.Subtract(newBalance, invAmt);
                        log.Fine("GrandTotal=" + GetGrandTotal(true) + "(" + invAmt
                            + ") Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
                    }
                    bpl.SetTotalOpenBalance(newBalance);
                    bpl.SetSOCreditStatus();

                    Decimal bptotalopenbal = bp.GetTotalOpenBalance();
                    Decimal bpSOcreditUsed = bp.GetSO_CreditUsed();
                    if (IsSOTrx())
                    {
                        bptotalopenbal = bptotalopenbal + invAmt;
                        bpSOcreditUsed = bpSOcreditUsed + invAmt;
                        bp.SetSO_CreditUsed(bpSOcreditUsed);
                    }
                    else
                    {
                        bptotalopenbal = bptotalopenbal - invAmt;
                    }

                    bp.SetTotalOpenBalance(bptotalopenbal);
                    //if (bp.GetSO_CreditLimit() > 0)
                    //{
                    //    if (((bpSOcreditUsed / bp.GetSO_CreditLimit()) * 100) >= 100)
                    //    {
                    //        // JID_0560 // Change here to set credit status to hold instead of stop // Lokesh Chauhan 15 July 2019
                    //        bp.SetSOCreditStatus("H");
                    //    }
                    //    else if (((bpSOcreditUsed / bp.GetSO_CreditLimit()) * 100) >= watchPer)
                    //    {
                    //        bp.SetSOCreditStatus("W");
                    //    }
                    //    else
                    //    {
                    //        bp.SetSOCreditStatus("O");
                    //    }
                    //}
                    if (bp.Save(Get_TrxName()))
                    {
                        if (!bpl.Save(Get_TrxName()))
                        {
                            _processMsg = "Could not update Business Partner and Location";
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }

                    //}
                }
                //Credit Limit
                //	User - Last Result/Contact
                if (GetVAF_UserContact_ID() != 0)
                {
                    MVAFUserContact user = new MVAFUserContact(GetCtx(), GetVAF_UserContact_ID(), Get_TrxName());
                    //user.SetLastContact(new DateTime(System.currentTimeMillis()));
                    user.SetLastContact(DateTime.Now);
                    user.SetLastResult(Msg.Translate(GetCtx(), "VAB_Invoice_ID") + ": " + GetDocumentNo());
                    if (!user.Save(Get_TrxName()))
                    {
                        _processMsg = "Could not update Business Partner User";
                        return DocActionVariables.STATUS_INVALID;
                    }
                }	//	user

                //	Update Project
                if (IsSOTrx() && GetVAB_Project_ID() != 0)
                {
                    MVABProject project = new MVABProject(GetCtx(), GetVAB_Project_ID(), Get_TrxName());
                    Decimal amt = GetGrandTotal(true);
                    int VAB_CurrencyTo_ID = project.GetVAB_Currency_ID();
                    if (VAB_CurrencyTo_ID != GetVAB_Currency_ID())
                        amt = MVABExchangeRate.Convert(GetCtx(), amt, GetVAB_Currency_ID(), VAB_CurrencyTo_ID,
                            GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                    //if (amt == null)
                    //{
                    //    _processMsg = "Could not convert VAB_Currency_ID=" + GetVAB_Currency_ID()
                    //        + " to Project VAB_Currency_ID=" + VAB_CurrencyTo_ID;
                    //    return DocActionVariables.STATUS_INVALID;
                    //}
                    Decimal newAmt = project.GetInvoicedAmt();
                    //if (newAmt == null)
                    //    newAmt = amt;
                    //else
                    newAmt = Decimal.Add(newAmt, amt);
                    log.Fine("GrandTotal=" + GetGrandTotal(true) + "(" + amt + ") Project " + project.GetName()
                        + " - Invoiced=" + project.GetInvoicedAmt() + "->" + newAmt);
                    project.SetInvoicedAmt(newAmt);
                    if (!project.Save(Get_TrxName()))
                    {
                        _processMsg = "Could not update Project";
                        return DocActionVariables.STATUS_INVALID;
                    }
                }	//	project

                //	User Validation
                String valid = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_AFTER_COMPLETE);
                if (valid != null)
                {
                    _processMsg = valid;
                    return DocActionVariables.STATUS_INVALID;
                }

                try
                {
                    //	Counter Documents
                    MVABInvoice counter = CreateCounterDoc();
                    if (counter != null)
                        Info.Append(" - @CounterDoc@: @VAB_Invoice_ID@=").Append(counter.GetDocumentNo());
                }
                catch (Exception e)
                {
                    Info.Append(" - @CounterDoc@: ").Append(e.Message.ToString());
                }

                timeEstimation += " End at " + DateTime.Now.ToUniversalTime();

                log.Warning(timeEstimation + " - " + GetDocumentNo());

                _processMsg = Info.ToString().Trim();
                SetProcessed(true);
                SetDocAction(DOCACTION_Close);
            }
            catch
            {
                ValueNamePair pp = VLogger.RetrieveError();
                _log.Info("Error found at Invoice Completion. Invoice Document no = " + GetDocumentNo() + " Error Name is " + pp.GetName());
                return DocActionVariables.STATUS_INVALID;
            }

            /* Creation of allocation against invoice whose payment is done against order */
            if (Env.IsModuleInstalled("VA009_") && DocActionVariables.STATUS_COMPLETED == "CO" && GetVAB_Order_ID() > 0)
            {
                if (!AllocationAgainstOrderPayment(this))
                {
                    return DocActionVariables.STATUS_INVALID;
                }
            }

            return DocActionVariables.STATUS_COMPLETED;
        }

        /// <summary>
        ///  Creation of allocation against invoice whose payment is done against order
        /// </summary>
        /// <returns>True, if view Allocation created and completed sucessfully</returns>
        private bool AllocationAgainstOrderPayment(MVABInvoice invoice)
        {
            MVABDocAllocation AllHdr = null;
            MVABDocAllocationLine Allline = null;
            int PaymentId = 0;
            StringBuilder _msg = new StringBuilder(); // is used to maintain document no

            // get record detail of Invoice pay schedule and payment
            DataSet dsPayment = DB.ExecuteDataset(@"SELECT VAB_sched_InvoicePayment.VAB_Payment_ID , VAB_sched_InvoicePayment.VAB_sched_InvoicePayment_ID ,
                                                           CASE WHEN VAB_sched_InvoicePayment.VAB_Currency_ID = VAB_Payment.VAB_Currency_ID THEN VAB_sched_InvoicePayment.DueAmt
                                                           ELSE CurrencyConvert(VAB_sched_InvoicePayment.DueAmt,VAB_sched_InvoicePayment.VAB_Currency_ID, VAB_Payment.VAB_Currency_ID,
                                                                VAB_Payment.DateAcct, VAB_Payment.VAB_CurrencyType_ID , VAB_Payment.VAF_Client_ID,VAB_Payment.VAF_Org_ID) END AS DueAmt,
                                                           VAB_Payment.VAB_Currency_ID,   VAB_Payment.VAB_CurrencyType_ID, VAB_Payment.VAF_Org_ID,
                                                           VAB_Payment.DateTrx, VAB_Payment.DateAcct, VAB_Payment.DocumentNo, VAB_Payment.VAB_BusinessPartner_ID, VAB_Payment.VAB_Order_ID
                                                    FROM VAB_sched_InvoicePayment INNER JOIN VAB_Payment ON VAB_sched_InvoicePayment.VAB_Payment_ID = VAB_Payment.VAB_Payment_ID
                                                    WHERE VAB_sched_InvoicePayment.VA009_orderpayschedule_id != 0
                                                          AND VAB_sched_InvoicePayment.VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID() + @"
                                                    ORDER BY VAB_sched_InvoicePayment.VAB_Payment_ID ASC ", null, invoice.Get_Trx());
            if (dsPayment != null && dsPayment.Tables.Count > 0 && dsPayment.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < dsPayment.Tables[0].Rows.Count; i++)
                {
                    // create new allocation for first record or when we found different Payment Reference (but complete the previous document first)
                    //if (i == 0 || PaymentId != Convert.ToInt32(dsPayment.Tables[0].Rows[i]["VAB_Payment_ID"]))
                    //{
                    // complete previous allocation before creation of new allocation
                    //if (i != 0 && AllHdr.Get_ID() > 0 && AllHdr.CompleteIt() == "CO")
                    //{
                    //    AllHdr.SetProcessed(true);
                    //    AllHdr.SetDocStatus(DOCACTION_Complete);
                    //    AllHdr.SetDocAction(DOCACTION_Close);
                    //    if (GetDescription() != null && GetDescription().Contains("{->"))
                    //    {
                    //        AllHdr.SetDocumentNo(GetDocumentNo() + "^");
                    //    }
                    //    AllHdr.Save(Get_TrxName());
                    //    if (_msg.Length > 0)
                    //    {
                    //        _msg.Append("," + AllHdr.GetDocumentNo());
                    //    }
                    //    else
                    //    {
                    //        _msg.Append(AllHdr.GetDocumentNo());
                    //    }
                    //}

                    #region created new allocation for diferent payment
                    AllHdr = new MVABDocAllocation(GetCtx(), 0, Get_TrxName());
                    AllHdr.SetVAF_Client_ID(GetVAF_Client_ID());
                    AllHdr.SetVAF_Org_ID(Convert.ToInt32(dsPayment.Tables[0].Rows[i]["VAF_Org_ID"]));
                    AllHdr.SetDateTrx(Util.GetValueOfDateTime(dsPayment.Tables[0].Rows[i]["DateTrx"]));
                    AllHdr.SetDateAcct(Util.GetValueOfDateTime(dsPayment.Tables[0].Rows[i]["DateAcct"]));
                    AllHdr.SetVAB_Currency_ID(Convert.ToInt32(dsPayment.Tables[0].Rows[i]["VAB_Currency_ID"]));
                    // Update conversion type from payment to view allocation (required for posting)
                    if (AllHdr.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
                    {
                        AllHdr.SetVAB_CurrencyType_ID(Util.GetValueOfInt(dsPayment.Tables[0].Rows[i]["VAB_CurrencyType_ID"]));
                    }
                    // for reverse record, set isactive as false
                    if (invoice.GetRef_VAB_Invoice_ID() > 0)
                    {
                        AllHdr.SetIsActive(false);
                    }
                    AllHdr.SetDescription("Payment: " + Util.GetValueOfString(dsPayment.Tables[0].Rows[i]["DocumentNo"]) + "[1]");
                    if (!AllHdr.Save(Get_TrxName()))
                    {
                        ValueNamePair pp = VLogger.RetrieveError();
                        if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                        {
                            SetProcessMsg(pp.GetName());
                        }
                        else
                        {
                            SetProcessMsg(Msg.GetMsg(GetCtx(), "VIS_AllocNotCreated"));
                        }
                        return false;
                    }
                    #endregion

                    //}

                    PaymentId = Convert.ToInt32(dsPayment.Tables[0].Rows[i]["VAB_Payment_ID"]);
                    if (AllHdr.Get_ID() > 0)
                    {
                        #region created Allocation Line
                        Allline = new MVABDocAllocationLine(AllHdr);
                        Allline.SetVAB_BusinessPartner_ID(Util.GetValueOfInt(dsPayment.Tables[0].Rows[i]["VAB_BusinessPartner_ID"]));
                        Allline.SetVAB_Invoice_ID(GetVAB_Invoice_ID());
                        Allline.SetVAB_sched_InvoicePayment_ID(Util.GetValueOfInt(dsPayment.Tables[0].Rows[i]["VAB_sched_InvoicePayment_ID"]));
                        Allline.SetVAB_Order_ID(Util.GetValueOfInt(dsPayment.Tables[0].Rows[i]["VAB_Order_ID"]));
                        Allline.SetVAB_Payment_ID(PaymentId);
                        Allline.SetDateTrx(DateTime.Now);

                        MVABDocTypes doctype = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypesTarget_ID());
                        // for reverse record
                        if (invoice.GetRef_VAB_Invoice_ID() > 0)
                        {
                            if (doctype.GetDocBaseType() == MVABMasterDocType.DOCBASETYPE_ARCREDITMEMO || doctype.GetDocBaseType() == MVABMasterDocType.DOCBASETYPE_APINVOICE)
                            {
                                Allline.SetAmount(Util.GetValueOfDecimal(dsPayment.Tables[0].Rows[i]["DueAmt"]));
                            }
                            else
                            {
                                Allline.SetAmount(Decimal.Negate(Util.GetValueOfDecimal(dsPayment.Tables[0].Rows[i]["DueAmt"])));
                            }
                        }
                        else // for orignal record
                        {
                            if (doctype.GetDocBaseType() == MVABMasterDocType.DOCBASETYPE_ARCREDITMEMO || doctype.GetDocBaseType() == MVABMasterDocType.DOCBASETYPE_APINVOICE)
                            {
                                Allline.SetAmount(Decimal.Negate(Util.GetValueOfDecimal(dsPayment.Tables[0].Rows[i]["DueAmt"])));
                            }
                            else
                            {
                                Allline.SetAmount(Util.GetValueOfDecimal(dsPayment.Tables[0].Rows[i]["DueAmt"]));
                            }
                        }
                        if (!Allline.Save(Get_TrxName()))
                        {
                            ValueNamePair pp = VLogger.RetrieveError();
                            if (pp != null)
                            {
                                SetProcessMsg(Msg.GetMsg(GetCtx(), "VIS_AllocLineNotCreated") + ", " + pp.GetName());
                            }
                            return false;
                        }
                        else
                        {
                            //if (i == dsPayment.Tables[0].Rows.Count - 1)
                            //{
                            // complete view allocation
                            if (AllHdr.Get_ID() > 0 && AllHdr.CompleteIt() == "CO")
                            {
                                AllHdr.SetProcessed(true);
                                AllHdr.SetDocStatus(DOCACTION_Complete);
                                AllHdr.SetDocAction(DOCACTION_Close);
                                if (GetDescription() != null && GetDescription().Contains("{->"))
                                {
                                    AllHdr.SetDocumentNo(GetDocumentNo() + "^");
                                }
                                if (!AllHdr.Save(Get_TrxName()))
                                {
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    if (pp != null)
                                    {
                                        SetProcessMsg(Msg.GetMsg(GetCtx(), "VIS_AllocNotCreated") + ", " + pp.GetName());
                                    }
                                    return false;
                                }
                                if (_msg.Length > 0)
                                {
                                    _msg.Append("," + AllHdr.GetDocumentNo());
                                }
                                else
                                {
                                    _msg.Append(AllHdr.GetDocumentNo());
                                }
                            }
                            else
                            {
                                ValueNamePair pp = VLogger.RetrieveError();
                                if (pp != null)
                                {
                                    SetProcessMsg(Msg.GetMsg(GetCtx(), "VIS_AllocNotCompleted") + ", " + pp.GetName());
                                }
                                return false;
                            }
                            //}
                        }
                        #endregion
                    }
                }
            }
            if (!String.IsNullOrEmpty(_msg.ToString()))
            {
                //"Allocation Created for Advanced Payment: "
                _processMsg = Msg.GetMsg(GetCtx(), "VIS_AllocCreated") + _msg;
            }
            return true;
        }

        /// <summary>
        /// Set the document number from Completed Document Sequence after completed
        /// </summary>
        private void SetCompletedDocumentNo()
        {
            // if Reversal document then no need to get Document no from Completed sequence
            if (IsReversal())
            {
                return;
            }

            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            // if Overwrite Date on Complete checkbox is true.
            if (dt.IsOverwriteDateOnComplete())
            {
                SetDateInvoiced(DateTime.Now.Date);
                if (GetDateAcct().Value.Date < GetDateInvoiced().Value.Date)
                {
                    SetDateAcct(GetDateInvoiced());

                    //	Std Period open?
                    if (!MVABYearPeriod.IsOpen(GetCtx(), GetDateAcct(), dt.GetDocBaseType(), GetVAF_Org_ID()))
                    {
                        throw new Exception("@PeriodClosed@");
                    }
                }
            }

            // if Overwrite Sequence on Complete checkbox is true.
            if (dt.IsOverwriteSeqOnComplete())
            {
                // Set Drafted Document No into Temp Document No.
                if (Get_ColumnIndex("TempDocumentNo") > 0)
                {
                    SetTempDocumentNo(GetDocumentNo());
                }

                // Get current next from Completed document sequence defined on Document type
                String value = MVAFRecordSeq.GetDocumentNo(GetVAB_DocTypes_ID(), Get_TrxName(), GetCtx(), true, this);
                if (value != null)
                {
                    SetDocumentNo(value);
                }
            }
        }

        /// <summary>
        /// Set Backup Withholding Tax Amount
        /// </summary>
        /// <param name="invoice">invoice refrence</param>
        /// <returns>true, if success</returns>
        public bool SetWithholdingAmount(MVABInvoice invoice)
        {
            Decimal withholdingAmt = 0;
            String sql = "";

            sql = @"SELECT VAB_BusinessPartner.IsApplicableonAPInvoice, VAB_BusinessPartner.IsApplicableonAPPayment, VAB_BusinessPartner.IsApplicableonARInvoice,
                            VAB_BusinessPartner.IsApplicableonARReceipt,  
                            VAB_Address.VAB_Country_ID , VAB_Address.VAB_RegionState_ID";
            sql += @" FROM VAB_BusinessPartner INNER JOIN VAB_BPart_Location ON 
                     VAB_BusinessPartner.VAB_BusinessPartner_ID = VAB_BPart_Location.VAB_BusinessPartner_ID 
                     INNER JOIN VAB_Address ON VAB_BPart_Location.VAB_Address_ID = VAB_Address.VAB_Address_ID  WHERE 
                     VAB_BusinessPartner.VAB_BusinessPartner_ID = " + invoice.GetVAB_BusinessPartner_ID() + @" AND VAB_BPart_Location.VAB_BPart_Location_ID = " + invoice.GetVAB_BPart_Location_ID();
            DataSet ds = DB.ExecuteDataset(sql, null, Get_Trx());
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                // check Withholding applicable on vendor/customer
                if ((!IsSOTrx() && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonAPInvoice"]).Equals("Y")) ||
                    (IsSOTrx() && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonARInvoice"]).Equals("Y")))
                {
                    sql = @"SELECT IsApplicableonInv, InvCalculation , InvPercentage 
                                        FROM VAB_Withholding WHERE IsApplicableonInv = 'Y' AND VAB_Withholding_ID = " + GetVAB_Withholding_ID();
                    if (Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_RegionState_ID"]) > 0)
                    {
                        sql += " AND NVL(VAB_RegionState_ID, 0) IN (0 ,  " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_RegionState_ID"]) + ")";
                    }
                    else
                    {
                        sql += " AND NVL(VAB_RegionState_ID, 0) IN (0) ";
                    }
                    if (Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Country_ID"]) > 0)
                    {
                        sql += " AND NVL(VAB_Country_ID , 0) IN (0 ,  " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Country_ID"]) + ")";
                    }
                    DataSet dsWithholding = DB.ExecuteDataset(sql, null, Get_Trx());
                    if (dsWithholding != null && dsWithholding.Tables.Count > 0 && dsWithholding.Tables[0].Rows.Count > 0)
                    {
                        // check on withholding - "Applicable on Invoice" or not
                        if (Util.GetValueOfString(dsWithholding.Tables[0].Rows[0]["IsApplicableonInv"]).Equals("Y"))
                        {
                            // get amount on which we have to derive withholding tax amount
                            if (Util.GetValueOfString(dsWithholding.Tables[0].Rows[0]["InvCalculation"]).Equals(X_VAB_Withholding.INVCALCULATION_GrandTotal))
                            {
                                if (IsTaxIncluded())
                                    sql = @"SELECT (SELECT COALESCE(SUM(LineNetAmt),0) FROM VAB_InvoiceLine WHERE VAB_Invoice_ID = inv.VAB_Invoice_ID) FROM VAB_Invoice inv "
                                        + " WHERE inv.VAB_Invoice_ID=" + invoice.GetVAB_Invoice_ID();
                                else
                                    sql = @"SELECT (SELECT COALESCE(SUM(LineNetAmt),0) FROM VAB_InvoiceLine WHERE VAB_Invoice_ID = inv.VAB_Invoice_ID) + 
                                    (SELECT COALESCE(SUM(TaxAmt),0) FROM VAB_Tax_Invoice it WHERE i.VAB_Invoice_ID=inv.VAB_Invoice_ID) FROM VAB_Invoice inv "
                                         + " WHERE inv.VAB_Invoice_ID=" + invoice.GetVAB_Invoice_ID();

                                withholdingAmt = DB.ExecuteQuery(sql, null, invoice.Get_Trx());//GetGrandTotal();
                            }
                            else if (Util.GetValueOfString(dsWithholding.Tables[0].Rows[0]["InvCalculation"]).Equals(X_VAB_Withholding.INVCALCULATION_SubTotal))
                            {
                                withholdingAmt = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COALESCE(SUM(LineNetAmt),0) FROM VAB_InvoiceLine 
                                             WHERE VAB_Invoice_ID  = " + GetVAB_Invoice_ID(), null, invoice.Get_Trx())); //GetTotalLines();
                            }
                            else if (Util.GetValueOfString(dsWithholding.Tables[0].Rows[0]["InvCalculation"]).Equals(X_VAB_Withholding.INVCALCULATION_TaxAmount))
                            {
                                // get tax amount from Invoice tax
                                withholdingAmt = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT SUM(TaxAmt) FROM VAB_Tax_Invoice 
                                             WHERE VAB_Invoice_ID  = " + GetVAB_Invoice_ID(), null, invoice.Get_Trx()));
                            }

                            _log.Info("Invoice withholding detail, Invoice Document No = " + GetDocumentNo() + " , Amount on distribute = " + withholdingAmt +
                             " , Invoice Withhold Percentage " + Util.GetValueOfDecimal(dsWithholding.Tables[0].Rows[0]["InvPercentage"]));

                            // derive formula
                            withholdingAmt = Decimal.Divide(
                                             Decimal.Multiply(withholdingAmt, Util.GetValueOfDecimal(dsWithholding.Tables[0].Rows[0]["InvPercentage"]))
                                             , 100);

                            invoice.SetBackupWithholdingAmount(Decimal.Round(withholdingAmt, GetPrecision()));
                            invoice.SetGrandTotalAfterWithholding(Decimal.Subtract(GetGrandTotal(), Decimal.Add(GetWithholdingAmt(), GetBackupWithholdingAmount())));
                        }
                    }
                    else
                    {
                        // when backup withholding ref as ZERO, when it is not for Invoice
                        //invoice.SetVAB_Withholding_ID(0);
                        _log.Info("Invoice backup withholding not found, Invoice Document No = " + GetDocumentNo());
                        return false;
                    }
                }
                else
                {
                    // when withholding not applicable on Business Partner
                    SetBackupWithholdingAmount(0);
                    // when withholdinf define by user manual or already set, but not applicable on invoice
                    if (GetVAB_Withholding_ID() > 0)
                    {
                        //SetVAB_Withholding_ID(0);
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// update cash book and cash jounal according to order's  cash book id 
        /// </summary>
        /// <param name="Info">info message </param>
        /// <param name="order">order model</param>
        /// <returns></returns>
        private bool UpdateCashBaseAndMulticurrency(StringBuilder Info, MVABOrder order)
        {
            // get def Cash book id
            int VAB_CashBook_ID = GetVAB_CashBook_ID(order, order.GetVAB_Currency_ID());// Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAB_CashBook_ID FROM VAPOS_POSTerminal WHERE VAPOS_POSTerminal_ID = " + order.GetVAPOS_POSTerminal_ID(), null, null));

            if (Env.IsModuleInstalled("VA205_") && !String.IsNullOrEmpty(order.GetVA205_Currencies()))
            {
                Dictionary<int, Decimal?> CurrencyAmounts = new Dictionary<int, Decimal?>();
                Dictionary<int, int> CashBooks = new Dictionary<int, int>();
                if (!GetCashbookAndAmountList(order, Info, out CurrencyAmounts, out CashBooks))
                    return false;
                // Change Multicurrency DayEnd
                foreach (int currency in CurrencyAmounts.Keys)
                {

                    if (Util.GetValueOfDecimal(CurrencyAmounts[currency]) != 0)
                        CreateUpdateCash(order, CashBooks[currency], Util.GetValueOfDecimal(CurrencyAmounts[currency]), currency);
                }
            } // end multicurrencies

            else //base currencies
            {
                decimal amt = order.GetVAPOS_CashPaid();

                if (amt == 0)
                {
                    log.Info(" cash amt is zero");
                    return true;
                }

                string ret = CreateUpdateCash(order, VAB_CashBook_ID, Util.GetValueOfDecimal(order.GetVAPOS_CashPaid()), GetVAB_Currency_ID());
                if (ret.Contains("VAB_CashJRNL_ID"))
                {
                    Info.Append(ret);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Create Currency's Cashbool and its Amount  as list
        /// </summary>
        /// <param name="order"></param>
        /// <param name="Info"></param>
        /// <param name="CurrencyAmounts"></param>
        /// <param name="CashBooks"></param>
        /// <returns>retrun list of currency cash book id and amount</returns>
        public bool GetCashbookAndAmountList(MVABOrder order, StringBuilder Info, out Dictionary<int, Decimal?> CurrencyAmounts, out Dictionary<int, int> CashBooks)
        {
            int VAB_CashBook_ID = GetVAB_CashBook_ID(order, order.GetVAB_Currency_ID());// Util.GetValueO
            string _currencies = order.GetVA205_Currencies();

            log.Info("CASH ==>> Multicurrency Currencies :: " + _currencies);

            CurrencyAmounts = new Dictionary<int, Decimal?>();
            CashBooks = new Dictionary<int, int>();
            // Change Multicurrency DayEnd


            string _amounts = order.GetVA205_Amounts();

            log.Info("CASH ==>> Multicurrency Amounts :: " + _amounts);

            bool hasCurrAmt = false;

            string[] _curVals = _currencies.Split(',');

            for (int i = 0; i < _curVals.Length; i++)
            {
                _curVals[i] = _curVals[i].Trim();
            }
            string[] _amtVals = _amounts.Split(',');
            for (int i = 0; i < _amtVals.Length; i++)
            {
                _amtVals[i] = _amtVals[i].Trim();
                if (_amtVals[i] != "")
                {
                    hasCurrAmt = true;
                }
            }
            // Note ::: Case for Multicurrency, Sometimes not creating Cash Journal, In That case created Jounal in base currency from Cash Paid Field of Sales Order
            if (!hasCurrAmt)
            {
                if (Info != null)
                {
                    string ret = CreateUpdateCash(order, VAB_CashBook_ID, Util.GetValueOfDecimal(order.GetVAPOS_CashPaid()), GetVAB_Currency_ID());
                    if (ret.Contains("VAB_CashJRNL_ID"))
                    {
                        Info.Append(ret);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // for inserting returns if multicurrency used;;;
                int countDefaultCur = 0;
                Decimal rtnAmt = 0;
                for (int i = 0; i < _curVals.Length; i++)
                {
                    VAB_CashBook_ID = GetVAB_CashBook_ID(order, Util.GetValueOfInt(_curVals[i]));
                    // Change Multicurrency DayEnd
                    if (!CashBooks.ContainsKey(Util.GetValueOfInt(_curVals[i])))
                    {
                        CashBooks.Add(Util.GetValueOfInt(_curVals[i]), VAB_CashBook_ID);
                    }

                    if (order.GetVAB_Currency_ID() == Util.GetValueOfInt(_curVals[i]))
                    {
                        rtnAmt = order.GetVAPOS_ReturnAmt();
                        ++countDefaultCur;
                    }
                    log.Info("CASH ==>> Return Amount :: " + rtnAmt);
                    //string ret = "";
                    if (countDefaultCur > 0 && rtnAmt > 0 && order.GetVAB_Currency_ID() == Util.GetValueOfInt(_curVals[i]))
                    {

                        // Change Multicurrency DayEnd
                        if (Util.GetValueOfDecimal(_amtVals[i]) != 0)
                        {
                            if (CurrencyAmounts.ContainsKey(order.GetVAB_Currency_ID()))
                            {
                                CurrencyAmounts[order.GetVAB_Currency_ID()] = Util.GetValueOfDecimal(CurrencyAmounts[order.GetVAB_Currency_ID()]) + Util.GetValueOfDecimal(_amtVals[i]);
                            }
                            else
                            {
                                CurrencyAmounts.Add(order.GetVAB_Currency_ID(), Util.GetValueOfDecimal(_amtVals[i]));
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (Util.GetValueOfDecimal(_amtVals[i]) == 0)
                        {
                            log.Info("CASH ==>> Amount 0 :: Continued ==>> " + Util.GetValueOfDecimal(_amtVals[i]));
                            continue;
                        }
                        else
                        {
                            if (GetGrandTotal() < 0 && GetGrandTotal() >= Util.GetValueOfDecimal(_amtVals[i]))
                            {
                                rtnAmt = 0;
                                _amtVals[i] = GetGrandTotal().ToString();

                            }
                            // Change Multicurrency DayEnd
                            int cashCurrencyID = Util.GetValueOfInt(_curVals[i]);
                            if (CurrencyAmounts.ContainsKey(cashCurrencyID))
                            {
                                CurrencyAmounts[cashCurrencyID] = Util.GetValueOfDecimal(CurrencyAmounts[cashCurrencyID]) + Util.GetValueOfDecimal(_amtVals[i]);
                            }
                            else
                            {
                                CurrencyAmounts.Add(cashCurrencyID, Util.GetValueOfDecimal(_amtVals[i]));
                            }
                        }
                    }
                }
                // Change Multicurrency DayEnd
                if (Util.GetValueOfString(order.GetVA205_RetCurrencies()) != "")
                {

                    if (!InsertReturnAmounts(order, order.GetVAB_Currency_ID(), CurrencyAmounts, CashBooks))
                    {
                        return false;
                    }
                }
                else if (Math.Abs(rtnAmt) != 0)
                {
                    if (CurrencyAmounts.ContainsKey(order.GetVAB_Currency_ID()))
                    {
                        CurrencyAmounts[order.GetVAB_Currency_ID()] = Util.GetValueOfDecimal(CurrencyAmounts[order.GetVAB_Currency_ID()]) + (-rtnAmt);
                    }
                    else
                    {
                        CurrencyAmounts.Add(order.GetVAB_Currency_ID(), -rtnAmt);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Check return voucher instaed of cash 
        /// </summary>
        /// <param name="order"></param>
        /// <param name="IsGenerateVchRtn"></param>
        /// <returns></returns>
        private bool IsGenerateVoucherReturn(MVABOrder order)
        {
            if (Env.IsModuleInstalled("VA018_"))
            {
                string ifGenVch = Util.GetValueOfString(DB.ExecuteScalar("SELECT VA018_GenerateVchRtn FROM VAPOS_POSTerminal WHERE VAPOS_POSTerminal_ID=" + order.GetVAPOS_POSTerminal_ID()));
                if (ifGenVch == "Y" && IsReturnTrx())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generate Asset against charge if charge type is AMortization
        /// </summary>
        /// <param name="Info"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool GenerateAssetForAmortizationCharge(StringBuilder Info, MVABInvoiceLine line)
        {
            try
            {

                MVABCharge ch = new MVABCharge(GetCtx(), line.GetVAB_Charge_ID(), Get_TrxName());
                if (ch.GetDTD001_ChargeType() == "AMR" && Util.GetValueOfInt(ch.Get_Value("VAA_AssetGroup_ID")) > 0
                    && ch.Get_ColumnIndex("VA038_AmortizationTemplate_ID") > 0
                    && Util.GetValueOfInt(ch.Get_Value("VA038_AmortizationTemplate_ID")) > 0)
                {
                    log.Fine("Asset");
                    Info.Append("@VAA_Asset_ID@: ");
                    MVAAAssetGroup astgrp = new MVAAAssetGroup(GetCtx(), Util.GetValueOfInt(ch.Get_Value("VAA_AssetGroup_ID")), Get_TrxName());
                    int Qty = 1;
                    decimal invQty = line.GetQtyInvoiced();
                    if (astgrp.IsOneAssetPerUOM())
                    {
                        Qty = Convert.ToInt32(line.GetQtyEntered());
                        invQty = 1;
                    }
                    for (Int32 i = 0; i < Qty; i++)
                    {
                        MVAAsset ast = new MVAAsset(GetCtx(), 0, Get_TrxName());
                        ast.SetVAF_Client_ID(GetVAF_Client_ID());
                        ast.SetVAF_Org_ID(GetVAF_Org_ID());
                        ast.Set_Value("VAB_Charge_ID", line.GetVAB_Charge_ID());
                        ast.SetVAA_AssetGroup_ID(Util.GetValueOfInt(ch.Get_Value("VAA_AssetGroup_ID")));

                        ast.SetVA038_AmortizationTemplate_ID(Util.GetValueOfInt(astgrp.Get_Value("VA038_AmortizationTemplate_ID")));
                        ast.SetVAFAM_AssetType(Util.GetValueOfString(astgrp.Get_Value("VAFAM_AssetType")));
                        ast.SetIsOwned(Util.GetValueOfBool(astgrp.Get_Value("IsOwned")));
                        ast.Set_Value("VAB_InvoiceLine_ID", Util.GetValueOfInt(line.GetVAB_InvoiceLine_ID()));
                        ast.Set_Value("VAFAM_DepreciationType_ID", Util.GetValueOfInt(astgrp.Get_Value("VAFAM_DepreciationType_ID")));
                        ast.Set_Value("IsInPosession", true);
                        ast.Set_Value("AssetServiceDate", Util.GetValueOfDateTime(GetDateAcct()));
                        ast.Set_Value("GuaranteeDate", Util.GetValueOfDateTime(GetDateAcct()));
                        ast.SetQty(invQty);
                        ast.SetName(ch.GetName());

                        if (!ast.Save(Get_TrxName()))
                        {
                            return false;
                            // _processMsg = "Could not create Asset";
                            // return DocActionVariables.STATUS_INVALID;
                        }
                        else
                        {
                            ast.SetName(ast.GetName() + "_" + ast.GetValue());
                            if (!ast.Save(Get_TrxName()))
                            {
                                return false;
                            }
                        }
                        if (i == 0)
                            Info.Append(ast.GetValue());
                        else
                            Info.Append("," + ast.GetValue());
                    }
                }
            }
            catch (Exception e)
            {
                log.Info("Could not create Asset");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create Counter Document
        /// </summary>
        /// <returns>counter invoice</returns>
        private MVABInvoice CreateCounterDoc()
        {
            //	Is this a counter doc ?
            if (GetRef_Invoice_ID() != 0)
                return null;

            //	Org Must be linked to BPartner
            MVAFOrg org = MVAFOrg.Get(GetCtx(), GetVAF_Org_ID());
            int counterVAB_BusinessPartner_ID = org.GetLinkedVAB_BusinessPartner_ID(Get_TrxName()); //jz
            if (counterVAB_BusinessPartner_ID == 0)
                return null;
            //	Business Partner needs to be linked to Org
            MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), null);
            int counterVAF_Org_ID = bp.GetVAF_OrgBP_ID_Int();
            if (counterVAF_Org_ID == 0)
                return null;

            MVABBusinessPartner counterBP = new MVABBusinessPartner(GetCtx(), counterVAB_BusinessPartner_ID, null);
            MVAFOrgDetail counterOrgInfo = MVAFOrgDetail.Get(GetCtx(), counterVAF_Org_ID, null);
            log.Info("Counter BP=" + counterBP.GetName());

            //	Document Type
            int VAB_DocTypesTarget_ID = 0;
            MVABInterCompanyDoc counterDT = MVABInterCompanyDoc.GetCounterDocType(GetCtx(), GetVAB_DocTypes_ID());
            if (counterDT != null)
            {
                log.Fine(counterDT.ToString());
                if (!counterDT.IsCreateCounter() || !counterDT.IsValid())
                    return null;
                VAB_DocTypesTarget_ID = counterDT.GetCounter_VAB_DocTypes_ID();
            }
            if (VAB_DocTypesTarget_ID <= 0)
                return null;

            // Ref_VAB_Invoice_ID --> contain reference of Orignal Document which is to be reversed
            // Ref_Invoice_ID --> contain reference of counter document which is to be created against this document
            // when we reverse document, and if counter document is created agaisnt its orignal document then need to reverse that document also
            if (Get_ColumnIndex("Ref_VAB_Invoice_ID") > 0 && GetRef_VAB_Invoice_ID() > 0)
            {
                int counterInvoiceId = Util.GetValueOfInt(DB.ExecuteScalar("SELECT Ref_Invoice_ID FROM VAB_Invoice WHERE VAB_Invoice_ID = " + GetRef_VAB_Invoice_ID(), null, Get_Trx()));
                MVABInvoice counterReversed = new MVABInvoice(GetCtx(), counterInvoiceId, Get_Trx());
                if (counterReversed != null && counterReversed.GetVAB_Invoice_ID() > 0)
                {
                    counterReversed.SetDocAction(DOCACTION_Void);
                    counterReversed.ProcessIt(DOCACTION_Void);
                    counterReversed.Save(Get_Trx());
                    return counterReversed;
                }
                return null;
            }

            // set counter Businees partner, and its Organization
            SetCounterBPartner(counterBP, counterVAF_Org_ID);

            //	Deep Copy
            MVABInvoice counter = CopyFrom(this, GetDateInvoiced(),
                VAB_DocTypesTarget_ID, true, Get_TrxName(), true);
            //	Refernces (Should not be required)
            counter.SetSalesRep_ID(GetSalesRep_ID());
            //
            counter.SetProcessing(false);
            counter.Save(Get_TrxName());

            //	Update copied lines
            MVABInvoiceLine[] counterLines = counter.GetLines(true);
            for (int i = 0; i < counterLines.Length; i++)
            {
                MVABInvoiceLine counterLine = counterLines[i];
                counterLine.SetClientOrg(counter);
                counterLine.SetInvoice(counter);	//	copies header values (BP, etc.)
                counterLine.SetPrice();
                counterLine.SetTax();
                //
                counterLine.Save(Get_TrxName());
            }

            log.Fine(counter.ToString());

            //	Document Action
            if (counterDT != null)
            {
                if (counterDT.GetDocAction() != null)
                {
                    counter.SetDocAction(counterDT.GetDocAction());
                    counter.ProcessIt(counterDT.GetDocAction());
                    counter.Save(Get_TrxName());
                }
            }
            return counter;
        }

        /// <summary>
        /// Void Document.
        /// </summary>
        /// <returns>true if success </returns>
        public bool VoidIt()
        {
            log.Info(ToString());
            if (DOCSTATUS_Closed.Equals(GetDocStatus())
                || DOCSTATUS_Reversed.Equals(GetDocStatus())
                || DOCSTATUS_Voided.Equals(GetDocStatus()))
            {
                _processMsg = "Document Closed: " + GetDocStatus();
                SetDocAction(DOCACTION_None);
                return false;
            }

            //	Not Processed
            if (DOCSTATUS_Drafted.Equals(GetDocStatus())
                || DOCSTATUS_Invalid.Equals(GetDocStatus())
                || DOCSTATUS_InProgress.Equals(GetDocStatus())
                || DOCSTATUS_Approved.Equals(GetDocStatus())
                || DOCSTATUS_NotApproved.Equals(GetDocStatus()))
            {
                //	Set lines to 0
                MVABInvoiceLine[] lines = GetLines(false);
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABInvoiceLine line = lines[i];
                    Decimal old = line.GetQtyInvoiced();
                    if (old.CompareTo(Env.ZERO) != 0)
                    {
                        line.SetQty(Env.ZERO);
                        line.SetTaxAmt(Env.ZERO);
                        line.SetLineNetAmt(Env.ZERO);
                        line.SetLineTotalAmt(Env.ZERO);
                        line.AddDescription(Msg.GetMsg(GetCtx(), "Voided") + " (" + old + ")");
                        //	Unlink Shipment
                        if (line.GetVAM_Inv_InOutLine_ID() != 0)
                        {
                            MVAMInvInOutLine ioLine = new MVAMInvInOutLine(GetCtx(), line.GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                            ioLine.SetIsInvoiced(false);
                            ioLine.Save(Get_TrxName());
                            line.SetVAM_Inv_InOutLine_ID(0);
                        }
                        line.Save(Get_TrxName());
                    }
                }
                AddDescription(Msg.GetMsg(GetCtx(), "Voided"));
                SetIsPaid(true);
                SetVAB_Payment_ID(0);
            }
            else
            {
                return ReverseCorrectIt();
            }

            SetProcessed(true);
            SetDocAction(DOCACTION_None);
            return true;
        }

        /**
         * 	Close Document.
         * 	@return true if success 
         */
        public bool CloseIt()
        {
            log.Info(ToString());
            SetProcessed(true);
            SetDocAction(DOCACTION_None);
            return true;
        }

        /// <summary>
        /// Reverse Correction - same date
        /// </summary>
        /// <returns>return true if success</returns>
        public bool ReverseCorrectIt()
        {
            //added by shubham JID_1501 to check payment shedule or not during void_
            if (Env.IsModuleInstalled("VA009_"))
            {
                int checkpayshedule = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAB_sched_InvoicePayment_ID) FROM VAB_sched_InvoicePayment WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID() + " AND VA009_ISpaid='Y'", null, Get_Trx()));
                if (checkpayshedule != 0)
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "DeleteAllowcationFirst");
                    return false;
                }
            }
            //if PDC available against Invoice donot void/reverse the Invoice
            if (Env.IsModuleInstalled("VA027_"))
            {
                int count = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VA027_postdatedcheck_id) FROM va027_Postdatedcheck WHERE DocStatus NOT IN('RE', 'VO') AND VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx()));
                if (count > 0)
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "LinkedDocStatus");
                    return false;
                }
                else
                {
                    string sql = "SELECT COUNT(Va027_CheckAllocate_ID) FROM Va027_CheckAllocate i INNER JOIN va027_chequeDetails ii ON" +
                        " i.va027_chequedetails_ID = ii.va027_chequedetails_ID" +
                        " INNER JOIN va027_postdatedcheck iii on ii.va027_postdatedcheck_id = iii.va027_postdatedcheck_id" +
                        " WHERE iii. DocStatus NOT IN ('RE', 'VO') And i.VAB_Invoice_id = " + GetVAB_Invoice_ID();
                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx())) > 0)
                    {
                        _processMsg = Msg.GetMsg(GetCtx(), "LinkedDocStatus");
                        return false;
                    }

                }

            }
            log.Info(ToString());
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
            if (!MVABYearPeriod.IsOpen(GetCtx(), GetDateAcct(), dt.GetDocBaseType(), GetVAF_Org_ID()))
            {
                _processMsg = "@PeriodClosed@";
                return false;
            }

            // is Non Business Day?
            // JID_1205: At the trx, need to check any non business day in that org. if not fund then check * org.
            if (MVABNonBusinessDay.IsNonBusinessDay(GetCtx(), GetDateAcct(), GetVAF_Org_ID()))
            {
                _processMsg = Common.Common.NONBUSINESSDAY;
                return false;
            }

            // if  Amount is Recoganized then invoice cant be reverse
            string sqlrun = "SELECT COUNT(run.VAB_Rev_RecognitionRun_ID) FROM VAB_Rev_RecognitionRun run " +
                "INNER JOIN VAB_Rev_RecognitionStrtgy plan on run.VAB_Rev_RecognitionStrtgy_id = plan.VAB_Rev_RecognitionStrtgy_id WHERE plan.VAB_InvoiceLine_ID IN " +
                "(SELECT VAB_InvoiceLine_ID FROM VAB_InvoiceLine WHERE VAB_Rev_Recognition_ID IS NOT NULL AND VAB_Invoice_ID= " + GetVAB_Invoice_ID() + ") AND run.VAGL_JRNL_ID IS not null";
            if (Util.GetValueOfInt(DB.ExecuteScalar(sqlrun)) > 0)
            {
                _processMsg = Msg.GetMsg(GetCtx(), "Recoganized");
                return false;
            }


            //	Don't touch allocation for cash as that is handled in CashJournal
            bool isCash = PAYMENTRULE_Cash.Equals(GetPaymentRule());

            if (!isCash)
            {
                MVABDocAllocation[] allocations = MVABDocAllocation.GetOfInvoice(GetCtx(),
                    GetVAB_Invoice_ID(), Get_TrxName());
                for (int i = 0; i < allocations.Length; i++)
                {
                    allocations[i].SetDocAction(DocActionVariables.ACTION_REVERSE_CORRECT);
                    allocations[i].ReverseCorrectIt();
                    allocations[i].Save(Get_TrxName());
                }
            }
            //	Reverse/Delete Matching
            if (!IsSOTrx())
            {
                MVAMMatchInvoice[] mInv = MVAMMatchInvoice.GetInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
                for (int i = 0; i < mInv.Length; i++)
                    mInv[i].Delete(true);
                MVAMMatchPO[] mPO = MVAMMatchPO.GetInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
                for (int i = 0; i < mPO.Length; i++)
                {
                    if (mPO[i].GetVAM_Inv_InOutLine_ID() == 0)
                        mPO[i].Delete(true);
                    else
                    {
                        mPO[i].SetVAB_InvoiceLine_ID(null);
                        mPO[i].Save(Get_TrxName());
                    }
                }
            }

            // JID_0872: Remove invoice reference from Service Contract Schedule.
            if (IsSOTrx() && GetVAB_Contract_ID() > 0)
            {
                string qry = "UPDATE VAB_ContractSchedule SET VAB_Invoice_ID = NULL WHERE VAB_Contract_ID = " + GetVAB_Contract_ID() + " AND VAB_Invoice_ID = " + GetVAB_Invoice_ID();
                int res = DB.ExecuteQuery(qry, null, Get_TrxName());
            }

            //
            Load(Get_TrxName());	//	reload allocation reversal Info

            //	Deep Copy
            MVABInvoice reversal = CopyFrom(this, GetDateInvoiced(),
                GetVAB_DocTypes_ID(), false, Get_TrxName(), true);
            // set original document reference
            reversal.SetRef_VAB_Invoice_ID(GetVAB_Invoice_ID());
            if (Get_ColumnIndex("BackupWithholdingAmount") > 0)
            {
                reversal.SetVAB_Withholding_ID(GetVAB_Withholding_ID()); // backup withholding refernce
                reversal.SetBackupWithholdingAmount(Decimal.Negate(GetBackupWithholdingAmount()));
                reversal.SetGrandTotalAfterWithholding(Decimal.Negate(GetGrandTotalAfterWithholding()));
            }
            //reversal.AddDescription("{->" + GetDocumentNo() + ")");
            try
            {
                reversal.SetIsFutureCostCalculated(false);

                // JID_1737: On Invoice at the time of reversal, Account date was setting incorrect.
                reversal.SetDateAcct(GetDateAcct());
            }
            catch (Exception) { }

            if (!reversal.Save(Get_TrxName()))
            {
                _processMsg = "Could not create Invoice Reversal";
                return false;
            }

            if (reversal == null)
            {
                _processMsg = "Could not create Invoice Reversal";
                return false;
            }
            reversal.SetReversal(true);

            //	Reverse Line Qty
            MVABInvoiceLine[] rLines = reversal.GetLines(false);
            MVABInvoiceLine[] OldLines = this.GetLines(false);
            for (int i = 0; i < rLines.Length; i++)
            {
                MVABInvoiceLine rLine = rLines[i];
                MVABInvoiceLine oldline = OldLines[i];
                rLine.SetQtyEntered(Decimal.Negate(rLine.GetQtyEntered()));
                rLine.SetQtyInvoiced(Decimal.Negate(rLine.GetQtyInvoiced()));
                rLine.SetLineNetAmt(Decimal.Negate(rLine.GetLineNetAmt()));
                if (((Decimal)rLine.GetTaxAmt()).CompareTo(Env.ZERO) != 0)
                    rLine.SetTaxAmt(Decimal.Negate((Decimal)rLine.GetTaxAmt()));

                // In Case of Reversal set Surcharge Amount as Negative if available.
                if (rLine.Get_ColumnIndex("SurchargeAmt") > 0 && (((Decimal)rLine.GetSurchargeAmt()).CompareTo(Env.ZERO) != 0))
                {
                    rLine.SetSurchargeAmt(Decimal.Negate((Decimal)rLine.GetSurchargeAmt()));
                }
                if (((Decimal)rLine.GetLineTotalAmt()).CompareTo(Env.ZERO) != 0)
                    rLine.SetLineTotalAmt(Decimal.Negate((Decimal)rLine.GetLineTotalAmt()));
                // bcz we set this field value as ZERO in Copy From Process
                rLine.SetVAB_OrderLine_ID(oldline.GetVAB_OrderLine_ID());
                rLine.SetVAM_Inv_InOutLine_ID(oldline.GetVAM_Inv_InOutLine_ID());
                try
                {
                    rLine.SetIsFutureCostCalculated(false);
                }
                catch (Exception) { }
                if (rLine.Get_ColumnIndex("IsCostImmediate") >= 0)
                {
                    rLine.SetIsCostImmediate(false);
                }
                if (Get_ColumnIndex("BackupWithholdingAmount") > 0)
                {
                    rLine.SetVAB_Withholding_ID(oldline.GetVAB_Withholding_ID()); //  withholding refernce
                    rLine.SetWithholdingAmt(Decimal.Negate(oldline.GetWithholdingAmt())); // withholding amount
                }
                if (!rLine.Save(Get_TrxName()))
                {
                    _processMsg = "Could not correct Invoice Reversal Line";
                    return false;
                }
                #region Calculating Cost on Expenses Arpit
                else
                {
                    Tuple<String, String, String> mInfo = null;
                    if (Env.HasModulePrefix("VAFAM_", out mInfo) && rLine.Get_ColumnIndex("VAFAM_IsAssetRelated") > 0)
                    {
                        if (!IsSOTrx() && !IsReturnTrx() && Util.GetValueOfBool(rLine.Get_Value("VAFAM_IsAssetRelated")))
                        {
                            if (Util.GetValueOfBool(rLine.Get_Value("VAFAM_IsAssetRelated")))
                            {
                                PO po = MVAFTableView.GetPO(GetCtx(), "VAFAM_Expense", 0, Get_Trx());
                                if (po != null)
                                {
                                    CreateExpenseAgainstReverseInvoice(rLine, oldline, po);

                                    if (!po.Save())
                                    {
                                        _log.Info("Asset Expense Not Saved For Asset ");
                                    }
                                    //In Case of Capital Expense ..Asset Gross Value will be updated  //Arpit //Ashish 12 Feb,2017
                                    else
                                    {
                                        UpdateDescriptionInOldExpnse(oldline, rLine);
                                        UpdateAssetGrossValue(rLine, po);
                                    }
                                }
                                //
                            }
                        }
                    }
                }
                #endregion

                //else
                //{
                //    CopyLandedCostAllocation(rLine.GetVAB_InvoiceLine_ID());
                //}
                //Amortization Schedule change
                //int countVA038 = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VA038_' "));
                //if (countVA038 > 0)
                //{
                //    if (rLine.GetVAM_Inv_InOutLine_ID() > 0)
                //    {
                //        int no = Util.GetValueOfInt(DB.ExecuteQuery("UPDATE VAA_Asset SET IsActive='N' WHERE VAM_Inv_InOutLine_ID=" + rLine.GetVAM_Inv_InOutLine_ID()));
                //        int no1 = Util.GetValueOfInt(DB.ExecuteQuery("UPDATE VA038_AmortizationSchedule SET IsActive='N' WHERE VAB_InvoiceLine_ID= " + oldline.GetVAB_InvoiceLine_ID()));
                //    }
                //    if (rLine.GetVAM_Inv_InOutLine_ID() == 0)
                //    {
                //        int Asset_ID = Util.GetValueOfInt(DB.ExecuteScalar(" SELECT VAA_Asset_ID FROM VA038_AmortizationSchedule WHERE VAB_InvoiceLine_ID= " + rLine.GetVAB_InvoiceLine_ID()));
                //        if (Asset_ID > 0)
                //        {
                //            int no = Util.GetValueOfInt(DB.ExecuteQuery("UPDATE VAA_Asset SET IsActive='N' WHERE VAA_Asset_ID=" + Asset_ID));
                //            int no1 = Util.GetValueOfInt(DB.ExecuteQuery("UPDATE VA038_AmortizationSchedule SET IsActive='N' WHERE VAB_InvoiceLine_ID= " + oldline.GetVAB_InvoiceLine_ID()));
                //        }
                //    }
                //}

                // End
            }
            reversal.SetVAB_Order_ID(GetVAB_Order_ID());
            reversal.Save();
            //
            if (!reversal.ProcessIt(DocActionVariables.ACTION_COMPLETE))
            {
                _processMsg = "Reversal ERROR: " + reversal.GetProcessMsg();
                return false;
            }
            reversal.SetVAB_Payment_ID(0);
            reversal.SetIsPaid(true);
            reversal.CloseIt();
            reversal.SetProcessing(false);
            reversal.SetDocStatus(DOCSTATUS_Reversed);
            reversal.SetDocAction(DOCACTION_None);
            reversal.Save(Get_TrxName());

            //JID_0889: show on void full message Reversal Document created
            _processMsg = Msg.GetMsg(GetCtx(), "VIS_DocumentReversed") + reversal.GetDocumentNo();
            //
            AddDescription("(" + reversal.GetDocumentNo() + "<-)");
            // Set reversal document reference
            SetRef_VAB_Invoice_ID(reversal.GetVAB_Invoice_ID());

            //	Clean up Reversed (this)
            MVABInvoiceLine[] iLines = GetLines(false);
            for (int i = 0; i < iLines.Length; i++)
            {
                MVABInvoiceLine iLine = iLines[i];
                if (iLine.GetVAM_Inv_InOutLine_ID() != 0)
                {
                    MVAMInvInOutLine ioLine = new MVAMInvInOutLine(GetCtx(), iLine.GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                    ioLine.SetIsInvoiced(false);
                    ioLine.Save(Get_TrxName());
                    //	Reconsiliation
                    iLine.SetVAM_Inv_InOutLine_ID(0);
                    iLine.Save(Get_TrxName());
                }
            }
            SetProcessed(true);
            SetDocStatus(DOCSTATUS_Reversed);	//	may come from void
            SetDocAction(DOCACTION_None);
            SetVAB_Payment_ID(0);
            SetIsPaid(true);

            //	Create Allocation
            if (!isCash)
            {
                MVABDocAllocation alloc = new MVABDocAllocation(GetCtx(), false, GetDateAcct(),
                    GetVAB_Currency_ID(),
                    Msg.Translate(GetCtx(), "VAB_Invoice_ID") + ": " + GetDocumentNo() + "/" + reversal.GetDocumentNo(),
                    Get_TrxName());
                alloc.SetVAF_Org_ID(GetVAF_Org_ID());
                // Update conversion type from Invoice to view allocation (required for posting)
                if (alloc.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
                {
                    alloc.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
                }
                if (alloc.Save())
                {
                    //	Amount
                    Decimal gt = GetGrandTotal(true);
                    if (!IsSOTrx())
                        gt = Decimal.Negate(gt);
                    //	Orig Line
                    MVABDocAllocationLine aLine = new MVABDocAllocationLine(alloc, gt, Env.ZERO, Env.ZERO, Env.ZERO);
                    aLine.SetVAB_Invoice_ID(GetVAB_Invoice_ID());
                    aLine.Save();
                    //	Reversal Line
                    MVABDocAllocationLine rLine = new MVABDocAllocationLine(alloc, Decimal.Negate(gt), Env.ZERO, Env.ZERO, Env.ZERO);
                    rLine.SetVAB_Invoice_ID(reversal.GetVAB_Invoice_ID());
                    rLine.Save();
                    //	Process It
                    if (alloc.ProcessIt(DocActionVariables.ACTION_COMPLETE))
                        alloc.Save();
                }
            }	//	notCash

            //	Explicitly Save for balance calc.
            Save();

            #region Commented by Manjot Suggested by Amit/Mukesh/Surya Sir on 30/04/2018, As discussed that we don't need to delete the schedules in case of reversal and Mark all the schedules as Paid

            //Modeule Prefix VA009--- For Deletion Of Payment Schedules
            //int _CountVA009 = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VA009_'  AND IsActive = 'Y'"));
            if (Env.IsModuleInstalled("VA009_"))
            {
                //TODO Verify why schedules are deleted and , why transaction is not used here
                //int count = Convert.ToInt32(DB.ExecuteScalar("DELETE FROM VAB_sched_InvoicePayment WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID(), null, Get_Trx()));
                int count = Convert.ToInt32(DB.ExecuteScalar("UPDATE VAB_sched_InvoicePayment SET VA009_Ispaid='Y' WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID(), null, Get_Trx()));
            }
            //End OF VA009..........................................................................................................
            #endregion




            //delete revenuerecognition run and plan
            DB.ExecuteQuery("DELETE FROM VAB_Rev_RecognitionRun WHERE VAB_Rev_RecognitionRun_ID IN (SELECT run.VAB_Rev_RecognitionRun_ID FROM VAB_Rev_RecognitionRun run " +
                           "INNER JOIN VAB_Rev_RecognitionStrtgy plan on run.VAB_Rev_RecognitionStrtgy_id = plan.VAB_Rev_RecognitionStrtgy_ID " +
                           "WHERE plan.VAB_InvoiceLine_ID IN(SELECT VAB_InvoiceLine_ID FROM VAB_InvoiceLine WHERE VAB_Rev_Recognition_ID IS NOT NULL AND VAB_Invoice_ID =" + GetVAB_Invoice_ID() + "))");

            DB.ExecuteQuery("DELETE FROM VAB_Rev_RecognitionStrtgy WHERE VAB_Rev_RecognitionStrtgy_ID IN (SELECT VAB_Rev_RecognitionStrtgy_ID FROM " +
                "VAB_Rev_RecognitionStrtgy WHERE VAB_InvoiceLine_ID IN(SELECT VAB_InvoiceLine_ID FROM VAB_InvoiceLine WHERE VAB_Rev_Recognition_ID IS NOT NULL AND " +
                "VAB_Invoice_ID= " + GetVAB_Invoice_ID() + "))");



            // code commented for updating open amount against customer while reversing invoice, code already exist
            // Done by Vivek on 24/11/2017
            //	Update BP Balance
            //MBPartner bp = new MBPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_TrxName());
            //if (bp.GetCreditStatusSettingOn() == "CH")
            //{
            //    bp.SetTotalOpenBalance();
            //    bp.Save();
            //}

            return true;
        }
        //update Description
        private void UpdateDescriptionInOldExpnse(MVABInvoiceLine oldline, MVABInvoiceLine rLine)
        {
            IDataReader idr = null;
            String Sql_ = "";
            try
            {
                Sql_ = "SELECT VAFAM_Expense_ID FROM VAFAM_Expense WHERE IsActive='Y' AND VAB_Invoice_ID=" + oldline.GetVAB_Invoice_ID();
                idr = DB.ExecuteReader(Sql_, null, Get_TrxName());
                if (idr != null)
                {
                    while (idr.Read())
                    {
                        PO oldPO = MVAFTableView.GetPO(GetCtx(), "VAFAM_Expense", Util.GetValueOfInt(idr["VAFAM_Expense_ID"]), Get_Trx());
                        MVABInvoice iv = new MVABInvoice(GetCtx(), rLine.GetVAB_Invoice_ID(), Get_TrxName());
                        oldPO.Set_Value("Description", "(" + iv.GetDocumentNo() + "<-)");
                        oldPO.Save(Get_TrxName());
                    }
                }
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
                log.SaveError(Sql_, e);
            }
        }

        //Update Gross Value in Case Asset against Capital Expsnse -Capital Type
        private void UpdateAssetGrossValue(MVABInvoiceLine rLine, PO po)
        {
            String CapitalExpense_ = "";
            CapitalExpense_ = Util.GetValueOfString(rLine.Get_Value("VAFAM_CapitalExpense"));
            if (CapitalExpense_ == "C")
            {
                MVAAsset asst = new MVAAsset(GetCtx(), Util.GetValueOfInt(po.Get_Value("VAA_Asset_ID")), Get_TrxName());
                //Update Asset Gross Value in Case of Capital Expense
                if (asst.Get_ColumnIndex("VAFAM_AssetGrossValue") > 0)
                {
                    Decimal LineNetAmt_ = MVABExchangeRate.ConvertBase(GetCtx(), rLine.GetLineNetAmt(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
                    asst.Set_Value("VAFAM_AssetGrossValue", Decimal.Add(Util.GetValueOfDecimal(asst.Get_Value("VAFAM_AssetGrossValue")), LineNetAmt_));
                    if (!asst.Save(Get_TrxName()))
                    {
                        _log.Info("Asset Expense Not Updated For Asset ");
                    }
                }
            }
        }

        //Create a new expense against selected asset in Line of Invoice
        private void CreateExpenseAgainstReverseInvoice(MVABInvoiceLine rLine, MVABInvoiceLine oldline, PO po)
        {
            po.SetVAF_Client_ID(GetVAF_Client_ID());
            po.SetVAF_Org_ID(GetVAF_Org_ID());
            po.Set_Value("VAB_Invoice_ID", rLine.GetVAB_Invoice_ID());
            po.Set_Value("DateAcct", GetDateAcct());
            po.Set_ValueNoCheck("VAA_Asset_ID", oldline.GetA_Asset_ID());
            //po.Set_Value("Amount", line.GetLineTotalAmt());
            po.Set_Value("VAB_Charge_ID", rLine.GetVAB_Charge_ID());
            po.Set_Value("VAM_PFeature_SetInstance_ID", rLine.GetVAM_PFeature_SetInstance_ID());
            po.Set_Value("VAM_Product_ID", rLine.GetVAM_Product_ID());
            po.Set_Value("VAB_UOM_ID", rLine.GetVAB_UOM_ID());
            po.Set_Value("VAB_TaxRate_ID", rLine.GetVAB_TaxRate_ID());
            //po.Set_Value("Price", line.GetPriceActual());
            po.Set_Value("Qty", rLine.GetQtyEntered());
            po.Set_Value("VAFAM_CapitalExpense", rLine.Get_Value("VAFAM_CapitalExpense"));

            Decimal LineTotalAmt_ = MVABExchangeRate.ConvertBase(GetCtx(), rLine.GetLineTotalAmt(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
            Decimal PriceActual_ = MVABExchangeRate.ConvertBase(GetCtx(), rLine.GetPriceActual(), GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
            po.Set_Value("Amount", LineTotalAmt_);
            po.Set_Value("Price", PriceActual_);

            //Descrption
            //po.Set_Value("Description", "{->" + Rinv.GetDocumentNo() + ")");
            po.Set_Value("Description", "{->" + GetDocumentNo() + ")");
        }

        /**
         * 	Reverse Accrual - none
         * 	@return false 
         */
        public bool ReverseAccrualIt()
        {
            log.Info(ToString());
            return false;
        }

        /** 
         * 	Re-activate
         * 	@return false 
         */
        public bool ReActivateIt()
        {
            log.Info(ToString());
            return false;
        }

        /***
         * 	Get Summary
         *	@return Summary of Document
         */
        public String GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetDocumentNo());
            //	: Grand Total = 123.00 (#1)
            sb.Append(": ").
                Append(Msg.Translate(GetCtx(), "GrandTotal")).Append("=").Append(GetGrandTotal())
                .Append(" (#").Append(GetLines(false).Length).Append(")");
            //	 - Description
            if (GetDescription() != null && GetDescription().Length > 0)
                sb.Append(" - ").Append(GetDescription());
            return sb.ToString();
        }

        /**
         * 	Get Process Message
         *	@return clear text error message
         */
        public String GetProcessMsg()
        {
            return _processMsg;
        }

        /**
         * 	Get Document Owner (Responsible)
         *	@return VAF_UserContact_ID
         */
        public int GetDoc_User_ID()
        {
            return GetSalesRep_ID();
        }

        /**
         * 	Get Document Approval Amount
         *	@return amount
         */
        public Decimal GetApprovalAmt()
        {
            return GetGrandTotal();
        }

        /**
         * 	Set Price List - Callout
         *	@param oldVAM_PriceList_ID old value
         *	@param newVAM_PriceList_ID new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetVAM_PriceList_ID(String oldVAM_PriceList_ID, String newVAM_PriceList_ID, int windowNo)
        {
            if (newVAM_PriceList_ID == null || newVAM_PriceList_ID.Length == 0)
                return;
            int VAM_PriceList_ID = int.Parse(newVAM_PriceList_ID);
            if (VAM_PriceList_ID == 0)
                return;

            String sql = "SELECT pl.IsTaxIncluded,pl.EnforcePriceLimit,pl.VAB_Currency_ID,c.StdPrecision,"
                + "plv.VAM_PriceListVersion_ID,plv.ValidFrom "
                + "FROM VAM_PriceList pl,VAB_Currency c,VAM_PriceListVersion plv "
                + "WHERE pl.VAB_Currency_ID=c.VAB_Currency_ID"
                + " AND pl.VAM_PriceList_ID=plv.VAM_PriceList_ID"
                + " AND pl.VAM_PriceList_ID=" + VAM_PriceList_ID						//	1
                + "ORDER BY plv.ValidFrom DESC";
            //	Use newest price list - may not be future
            IDataReader dr = null;
            try
            {
                dr = DataBase.DB.ExecuteReader(sql, null, null);
                if (dr.Read())
                {
                    base.SetVAM_PriceList_ID(VAM_PriceList_ID);
                    //	Tax Included
                    SetIsTaxIncluded("Y".Equals(dr[0].ToString()));
                    //	Price Limit Enforce
                    //if (p_changeVO != null)
                    //{
                    //	p_changeVO.setContext(GetCtx(), windowNo, "EnforcePriceLimit", dr.getString(2));
                    //}
                    //	Currency
                    int ii = Utility.Util.GetValueOfInt(dr[2]);
                    SetVAB_Currency_ID(ii);
                    //	PriceList Version
                    //if (p_changeVO != null)
                    //{
                    //	p_changeVO.setContext(GetCtx(), windowNo, "VAM_PriceListVersion_ID", dr.getInt(5));
                    //}
                }
                dr.Close();
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }

                log.Log(Level.SEVERE, sql, e);
            }

        }

        /**
         * 
         * @param oldVAB_PaymentTerm_ID
         * @param newVAB_PaymentTerm_ID
         * @param windowNo
         * @throws Exception
         */
        ///@UICallout
        public void SetVAB_PaymentTerm_ID(String oldVAB_PaymentTerm_ID, String newVAB_PaymentTerm_ID, int windowNo)
        {
            if (newVAB_PaymentTerm_ID == null || newVAB_PaymentTerm_ID.Length == 0)
                return;
            int VAB_PaymentTerm_ID = int.Parse(newVAB_PaymentTerm_ID);
            int VAB_Invoice_ID = GetVAB_Invoice_ID();
            if (VAB_PaymentTerm_ID == 0 || VAB_Invoice_ID == 0)	//	not saved yet
                return;

            MVABPaymentTerm pt = new MVABPaymentTerm(GetCtx(), VAB_PaymentTerm_ID, null);
            if (pt.Get_ID() == 0)
            {
                //addError(Msg.getMsg(GetCtx(), "PaymentTerm not found"));
            }

            bool valid = pt.Apply(VAB_Invoice_ID);
            SetIsPayScheduleValid(valid);
            return;
        }

        /**
         *	Invoice Header - DocType.
         *		- PaymentRule
         *		- temporary Document
         *  Ctx:
         *  	- DocSubTypeSO
         *		- HasCharges
         *	- (re-sets Business Partner Info of required)
         *	@param ctx context
         *	@param WindowNo window no
         *	@param mTab tab
         *	@param mField field
         *	@param value value
         *	@return null or error message
         */
        ///@UICallout
        public void SetVAB_DocTypesTarget_ID(String oldVAB_DocTypesTarget_ID, String newVAB_DocTypesTarget_ID, int WindowNo)
        {
            if (newVAB_DocTypesTarget_ID == null || newVAB_DocTypesTarget_ID.Length == 0)
            {
                return;
            }
            int VAB_DocTypes_ID = Utility.Util.GetValueOfInt(newVAB_DocTypesTarget_ID);
            if (VAB_DocTypes_ID.ToString() == null || VAB_DocTypes_ID == 0)
            {
                return;
            }

            String sql = "SELECT d.HasCharges,'N',d.IsDocNoControlled,"
                + "s.CurrentNext, d.DocBaseType "
                /*//jz outer join
                + "FROM VAB_DocTypes d, VAF_Record_Seq s "
                + "WHERE VAB_DocTypes_ID=?"		//	1
                + " AND d.DocNoSequence_ID=s.VAF_Record_Seq_ID(+)";
                */
                + "FROM VAB_DocTypes d "
                + "LEFT OUTER JOIN VAF_Record_Seq s ON (d.DocNoSequence_ID=s.VAF_Record_Seq_ID) "
                + "WHERE VAB_DocTypes_ID=" + VAB_DocTypes_ID;		//	1

            IDataReader dr = null;
            try
            {
                dr = DataBase.DB.ExecuteReader(sql, null, null);
                if (dr.Read())
                {
                    //	Charges - Set Ctx
                    SetContext(WindowNo, "HasCharges", dr[0].ToString());
                    //	DocumentNo
                    if (dr[2].ToString().Equals("Y"))
                        SetDocumentNo("<" + dr[3].ToString() + ">");
                    //  DocBaseType - Set Ctx
                    String s = dr[4].ToString();
                    SetContext(WindowNo, "DocBaseType", s);
                    //  AP Check & AR Credit Memo
                    if (s.StartsWith("AP"))
                        SetPaymentRule("S");    //  Check
                    else if (s.EndsWith("C"))
                        SetPaymentRule("P");    //  OnCredit
                }
                dr.Close();

            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }

            return;
        }

        /**
         *	Invoice Header- BPartner.
         *		- VAM_PriceList_ID (+ Ctx)
         *		- VAB_BPart_Location_ID
         *		- VAF_UserContact_ID
         *		- POReference
         *		- SO_Description
         *		- IsDiscountPrinted
         *		- PaymentRule
         *		- VAB_PaymentTerm_ID
         *	@param ctx context
         *	@param WindowNo window no
         *	@param mTab tab
         *	@param mField field
         *	@param value value
         *	@return null or error message
         */
        //@UICallout
        public void SetVAB_BusinessPartner_ID(String oldVAB_BusinessPartner_ID, String newVAB_BusinessPartner_ID, int WindowNo)
        {
            if (newVAB_BusinessPartner_ID == null || newVAB_BusinessPartner_ID.Length == 0)
                return;
            int VAB_BusinessPartner_ID = int.Parse(newVAB_BusinessPartner_ID);
            if (VAB_BusinessPartner_ID == 0)
                return;

            String sql = "SELECT p.VAF_Language,p.VAB_PaymentTerm_ID,"
                + " COALESCE(p.VAM_PriceList_ID,g.VAM_PriceList_ID) AS VAM_PriceList_ID, p.PaymentRule,p.POReference,"
                + " p.SO_Description,p.IsDiscountPrinted,"
                + " p.SO_CreditLimit, p.SO_CreditLimit-p.SO_CreditUsed AS CreditAvailable,"
                + " l.VAB_BPart_Location_ID,c.VAF_UserContact_ID,"
                + " COALESCE(p.PO_PriceList_ID,g.PO_PriceList_ID) AS PO_PriceList_ID, p.PaymentRulePO,p.PO_PaymentTerm_ID "
                + "FROM VAB_BusinessPartner p"
                + " INNER JOIN VAB_BPart_Category g ON (p.VAB_BPart_Category_ID=g.VAB_BPart_Category_ID)"
                + " LEFT OUTER JOIN VAB_BPart_Location l ON (p.VAB_BusinessPartner_ID=l.VAB_BusinessPartner_ID AND l.IsBillTo='Y' AND l.IsActive='Y')"
                + " LEFT OUTER JOIN VAF_UserContact c ON (p.VAB_BusinessPartner_ID=c.VAB_BusinessPartner_ID) "
                + "WHERE p.VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID + " AND p.IsActive='Y'";		//	#1

            bool isSOTrx = IsSOTrx();
            try
            {
                DataSet ds = DataBase.DB.ExecuteDataset(sql, null, null);
                //
                for (int ik = 0; ik < ds.Tables[0].Rows.Count; ik++)
                {
                    DataRow dr = ds.Tables[0].Rows[ik];
                    //	PriceList & IsTaxIncluded & Currency
                    int ii = Utility.Util.GetValueOfInt(dr[isSOTrx ? "VAM_PriceList_ID" : "PO_PriceList_ID"].ToString());
                    if (dr != null)
                        SetVAM_PriceList_ID(ii);
                    else
                    {	//	get default PriceList
                        int i = GetCtx().GetContextAsInt("#VAM_PriceList_ID");
                        if (i != 0)
                            SetVAM_PriceList_ID(i);
                    }

                    //	PaymentRule
                    String s = dr[isSOTrx ? "PaymentRule" : "PaymentRulePO"].ToString();
                    if (s != null && s.Length != 0)
                    {
                        if (GetCtx().GetContext(WindowNo, "DocBaseType").EndsWith("C"))	//	Credits are Payment Term
                            s = "P";
                        else if (isSOTrx && (s.Equals("S") || s.Equals("U")))	//	No Check/Transfer for SO_Trx
                            s = "P";											//  Payment Term
                        SetPaymentRule(s);
                    }
                    //  Payment Term
                    ii = (int)dr[isSOTrx ? "VAB_PaymentTerm_ID" : "PO_PaymentTerm_ID"];
                    if (dr != null)
                        SetVAB_PaymentTerm_ID(ii);

                    //	Location
                    int locID = (int)dr["VAB_BPart_Location_ID"];
                    //	overwritten by InfoBP selection - works only if InfoWindow
                    //	was used otherwise creates error (uses last value, may belong to differnt BP)
                    if (VAB_BusinessPartner_ID.ToString().Equals(GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BusinessPartner_ID")))
                    {
                        String loc = GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BPart_Location_ID");
                        if (loc.Length > 0)
                            locID = int.Parse(loc);
                    }
                    if (locID == 0)
                    {
                        //p_changeVO.addChangedValue("VAB_BPart_Location_ID", (String)null);
                    }
                    else
                    {
                        SetVAB_BPart_Location_ID(locID);
                    }
                    //	Contact - overwritten by InfoBP selection
                    int contID = (int)dr["VAF_UserContact_ID"];
                    if (VAB_BusinessPartner_ID.ToString().Equals(GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BusinessPartner_ID")))
                    {
                        String cont = GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAF_UserContact_ID");
                        if (cont.Length > 0)
                            contID = int.Parse(cont);
                    }
                    SetVAF_UserContact_ID(contID);

                    //	CreditAvailable
                    if (isSOTrx)
                    {
                        double CreditLimit = (double)dr["SO_CreditLimit"];
                        if (CreditLimit != 0)
                        {
                            double CreditAvailable = (double)dr["CreditAvailable"];
                            //if (!dr.IsDBNull() && CreditAvailable < 0)
                            if (dr != null && CreditAvailable < 0)
                            {
                                String msg = Msg.GetMsg(GetCtx(), "CreditLimitOver");//, DisplayType.getNumberFormat(DisplayType.Amount).format(CreditAvailable));
                                //addError(msg);
                            }
                        }
                    }

                    //	PO Reference
                    s = dr["POReference"].ToString();
                    if (s != null && s.Length != 0)
                        SetPOReference(s);
                    else
                        SetPOReference(null);
                    //	SO Description
                    s = dr["SO_Description"].ToString();
                    if (s != null && s.Trim().Length != 0)
                        SetDescription(s);
                    //	IsDiscountPrinted
                    s = dr["IsDiscountPrinted"].ToString();
                    SetIsDiscountPrinted("Y".Equals(s));
                }
                ds = null;
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "bPartner", e);
            }

            return;
        }

        /**
         * 	Set DateInvoiced - Callout
         *	@param oldDateInvoiced old
         *	@param newDateInvoiced new
         *	@param windowNo window no
         */
        //@UICallout 
        public void SetDateInvoiced(String oldDateInvoiced, String newDateInvoiced, int windowNo)
        {
            if (newDateInvoiced == null || newDateInvoiced.Length == 0)
                return;
            DateTime dateInvoiced = Convert.ToDateTime(PO.ConvertToTimestamp(newDateInvoiced));
            if (dateInvoiced == null)
                return;
            SetDateInvoiced(dateInvoiced);
        }

        public bool InsertReturnAmounts(MVABOrder order, int BaseCurrency, Dictionary<int, Decimal?> CurrAmounts, Dictionary<int, int> Cashbooks)
        {
            StringBuilder _sb = new StringBuilder("");
            int VAB_CashBook_ID = 0;
            string ret = "";
            string _amounts = order.GetVA205_RetAmounts();
            string _currencies = order.GetVA205_RetCurrencies();

            log.Info("CASH ==>> Multicurrency Amounts :: " + _amounts);

            string[] _curVals = _currencies.Split(',');

            bool hasCurrAmt = false;

            for (int i = 0; i < _curVals.Length; i++)
            {
                _curVals[i] = _curVals[i].Trim();
            }
            string[] _amtVals = _amounts.Split(',');
            for (int i = 0; i < _amtVals.Length; i++)
            {
                _amtVals[i] = _amtVals[i].Trim();
                if (_amtVals[i] != "")
                {
                    hasCurrAmt = true;
                }
            }

            for (int i = 0; i < _curVals.Length; i++)
            {
                var cashAmount = Util.GetValueOfDecimal(_amtVals[i]);
                int cashCurrencyID = Util.GetValueOfInt(_curVals[i]);

                if (cashAmount == 0)
                {
                    continue;
                }
                else
                {
                    cashAmount = Decimal.Negate(cashAmount);
                }

                VAB_CashBook_ID = GetVAB_CashBook_ID(order, cashCurrencyID);

                if (!Cashbooks.ContainsKey(cashCurrencyID))
                {
                    Cashbooks.Add(cashCurrencyID, VAB_CashBook_ID);
                }

                if (CurrAmounts.ContainsKey(cashCurrencyID))
                {
                    CurrAmounts[cashCurrencyID] = Util.GetValueOfDecimal(CurrAmounts[cashCurrencyID]) + cashAmount;
                }
                else
                {
                    CurrAmounts.Add(cashCurrencyID, cashAmount);
                }

            }

            return true;
        }

        private int GetVAB_CashBook_ID(MVABOrder order, int VAB_Currency_ID)
        {
            int VAB_CashBook_ID = 0;
            StringBuilder _sb = new StringBuilder();
            if (order.GetVAB_Currency_ID() == VAB_Currency_ID)
            {
                _sb.Clear();
                _sb.Append("SELECT VAB_CashBook_ID FROM VAPOS_POSTerminal WHERE VAPOS_POSTerminal_ID = " + order.GetVAPOS_POSTerminal_ID());


                VAB_CashBook_ID = Util.GetValueOfInt(DB.ExecuteScalar(_sb.ToString(), null, null));

                int empCBId = 0;

                if (Util.GetValueOfString(DB.ExecuteScalar(@"SELECT VAPOS_MaintainCashbook FROM VAPOS_TerminalSetting WHERE 
                                                                      VAPOS_PosTerminal_ID = " + order.GetVAPOS_POSTerminal_ID())) == "E")
                {
                    empCBId = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_CashBook_ID FROM VAPOS_TerminalUsers WHERE VAF_UserContact_ID = " + order.GetCreatedBy()));
                }

                if (empCBId > 0)
                {
                    VAB_CashBook_ID = empCBId;
                }

                if (VAB_CashBook_ID <= 0)
                {
                    MVABCashBook cb = MVABCashBook.Get(GetCtx(), GetVAF_Org_ID(), VAB_Currency_ID);
                    VAB_CashBook_ID = Util.GetValueOfInt(cb.GetVAB_CashBook_ID());
                }
            }
            else
            {
                _sb.Clear();
                _sb.Append("SELECT VAB_CashBook_ID FROM VA205_CurrencyOptions WHERE VAPOS_POSTerminal_ID = " + order.GetVAPOS_POSTerminal_ID() + " AND VAB_Currency_ID = " + VAB_Currency_ID);

                VAB_CashBook_ID = Util.GetValueOfInt(DB.ExecuteScalar(_sb.ToString(), null, null));

                if (VAB_CashBook_ID <= 0)
                {
                    MVABCashBook cb = MVABCashBook.Get(GetCtx(), GetVAF_Org_ID(), VAB_Currency_ID);
                    VAB_CashBook_ID = Util.GetValueOfInt(cb.GetVAB_CashBook_ID());
                }
            }

            if (VAB_CashBook_ID <= 0)
            {
                _sb.Clear();
                _sb.Append("SELECT VAB_CashBook_ID FROM VAPOS_POSTerminal WHERE VAPOS_POSTerminal_ID = " + order.GetVAPOS_POSTerminal_ID());
                VAB_CashBook_ID = Util.GetValueOfInt(DB.ExecuteScalar(_sb.ToString(), null, null));
            }
            return VAB_CashBook_ID;
        }

        /// <summary>
        /// Create cash jour and update cash line
        /// </summary>
        /// <param name="order"></param>
        /// <param name="VAB_CashBook_ID"></param>
        /// <param name="amtVal"></param>
        /// <param name="VAB_Currency_ID"></param>
        /// <returns></returns>
        public string CreateUpdateCash(MVABOrder order, int VAB_CashBook_ID, Decimal amtVal, int VAB_Currency_ID)
        {
            //voucher return
            //int _CountVA018 = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VA018_'  AND ISActive='Y'  "));
            // int _CountVA205 = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VA205_'  AND ISActive='Y'  "));

            MVABCashJRNL cash;
            if (GetGrandTotal() < 0 && amtVal > 0)
            {
                amtVal = 0 - amtVal;
            }
            if (Env.IsModuleInstalled("VA018_"))
            {
                if (order.IsVA018_ReturnVoucher() && 0 - GetGrandTotal() == order.GetVA018_VoucherAmount())
                {
                    log.Info("CASH ==>> Voucher Module :: Return Voucher :: Returned Cash ID 0");
                    return "VAB_CashJRNL_ID 0";
                }
            }

            if (order.GetVAPOS_CashPaid() == 0)
            {
                log.Info("Amount Not Found for Cash Journal :: Cash Paid");
                return "VAB_CashJRNL_ID 0";
            }
            else
            {
                if (amtVal == 0 && VAB_Currency_ID == order.GetVAB_Currency_ID())
                {
                    amtVal = order.GetVAPOS_CashPaid();
                }
                else if (amtVal == 0)
                {
                    log.Info("Amount Not Found for Cash Journal :: AMTVAL 0");
                    return "VAB_CashJRNL_ID 0";
                }
            }
            VAdvantage.Model.MVABDocTypes dt = VAdvantage.Model.MVABDocTypes.Get(GetCtx(), order.GetVAB_DocTypes_ID());
            String DocSubTypeSO = dt.GetDocSubTypeSO();

            if (VAdvantage.Model.MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))
            {
                //if (order.GetVAPOS_ShiftDetails_ID() == 0)
                //{
                //    //cash = MCash.GetCash(GetCtx(), VAB_CashBook_ID, GetDateInvoiced(), Get_TrxName(), order.GetVAPOS_ShiftDetails_ID(), order.GetVAPOS_ShiftDate());
                //    cash = MCash.GetCash(GetCtx(), VAB_CashBook_ID, order.GetOrderCompletionDatetime(), Get_TrxName(), order.GetVAPOS_ShiftDetails_ID(), order.GetVAPOS_ShiftDate());

                //    if (cash == null || cash.Get_ID() == 0)
                //    {
                //        log.Info("CASH ==>> No CashBook Found ");
                //        _processMsg = "@NoCashBook@";
                //        return _processMsg;
                //    }
                //}
                //else
                //{
                //cash = MCash.GetCash(GetCtx(), VAB_CashBook_ID, GetDateInvoiced(), Get_TrxName(), order.GetVAPOS_ShiftDetails_ID(), order.GetVAPOS_ShiftDate());
                cash = MVABCashJRNL.GetCash(GetCtx(), VAB_CashBook_ID, order.GetOrderCompletionDatetime(), Get_TrxName(), order.GetVAPOS_ShiftDetails_ID(), order.GetVAPOS_ShiftDate());
                if (cash == null || cash.Get_ID() == 0)
                {
                    log.Info("CASH ==>> No CashBook Found 2");
                    _processMsg = "@NoCashBook@";
                    return _processMsg;
                }
                //}
            }
            else
            {
                //by sanjiv for cash payment for cash journal entry according to cash book selected during the POS terminal.	
                if (order.GetVAPOS_POSTerminal_ID() != 0)
                {
                    //cash = MCash.Get(GetCtx(), VAB_CashBook_ID, GetDateInvoiced(), Get_TrxName());
                    cash = MVABCashJRNL.Get(GetCtx(), VAB_CashBook_ID, Convert.ToDateTime(order.GetOrderCompletionDatetime()).ToLocalTime(), Get_TrxName());

                }
                else
                {
                    cash = MVABCashJRNL.Get(GetCtx(), GetVAF_Org_ID(), GetDateInvoiced(),
                        GetVAB_Currency_ID(), Get_TrxName());
                }

                if (cash == null || cash.Get_ID() == 0)
                {
                    log.Info("CASH ==>> No CashBook Found 3");
                    _processMsg = "@NoCashBook@";
                    return _processMsg;
                    // return DocActionVariables.STATUS_INVALID;//modified by vijay for 3.2
                }
            }
            //------------- Edited on 1/12/2014 : Abhishek
            int DocType_ID = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAB_DocTypes_ID FROM VAB_DocTypes WHERE Docbasetype='CMC' AND VAF_Client_ID='" + GetCtx().GetVAF_Client_ID() + "' AND VAF_Org_ID IN('0','" + GetCtx().GetVAF_Org_ID() + "') ORDER BY  VAF_Org_ID DESC"));
            cash.SetVAB_DocTypes_ID(DocType_ID);
            if (!cash.Save())
            {
                log.Info("CASH ==>> Error in saving Cash Journal ");
            }
            //------------- End Edited on 1/12/2014 : Abhishek

            MVABCashJRNLLine cl = new MVABCashJRNLLine(cash);
            cl.SetVAB_BusinessPartner_ID(this.GetVAB_BusinessPartner_ID());
            cl.SetVAB_BPart_Location_ID(GetVAB_BPart_Location_ID());
            //cl.SetVSS_PAYMENTTYPE("R");
            if (VAB_Currency_ID == 0)
            {
                cl.SetInvoice(this);
            }
            else
            {
                cl.SetInvoiceMultiCurrency(this, Util.GetValueOfDecimal(amtVal), Util.GetValueOfInt(VAB_Currency_ID));
            }
            // Change 2 July
            if (cl.GetAmount() >= 0)
            {
                cl.SetVSS_PAYMENTTYPE("R");
            }
            else
            {
                cl.SetVSS_PAYMENTTYPE("P");
            }
            cl.SetVAB_CurrencyType_ID(order.GetVAB_CurrencyType_ID());

            // check invoice pay schedule id 
            // set schedule id on cash line 
            if (Env.IsModuleInstalled("VA009_"))
            {
                int invPaySchId = DB.GetSQLValue
                    (null, "SELECT VAB_sched_InvoicePayment_ID FROM VAB_sched_InvoicePayment WHERE VA009_TransCurrency= " + VAB_Currency_ID
                      + " AND  VAB_Invoice_ID = " + GetVAB_Invoice_ID() + " AND  VA009_PaymentMethod_ID IN "
                      + " (SELECT p.VA009_PaymentMethod_ID FROM VA009_PaymentMethod p WHERE p.VA009_PaymentBaseType = "
                                     + " '" + X_VAB_Order.PAYMENTRULE_Cash + "' AND p.VAB_Currency_ID IS NULL AND p.IsActive = 'Y' AND p.VAF_Client_ID = " + GetVAF_Client_ID() + ") ");
                //Currency Id Null
                cl.SetVAB_sched_InvoicePayment_ID(invPaySchId);
                //string sql= "SELECT VAB_sched_InvoicePayment_ID FROM VAB_sched_InvoicePayment WHERE VAB_Invoice_ID = "++" AND VA009_PaymentMethod_ID=" X_VAB_Invoice. +;
            }

            if (!cl.Save(Get_TrxName()))
            {
                log.Info("CASH ==>> Cash Line Not Saved :: Error :: Journal ==>> " + cash.GetName() + " :: Line ==>> " + GetDocumentNo() + ", Amount ==>> " + Util.GetValueOfDecimal(amtVal));
                _processMsg = "Could not Save Cash Journal Line";
                return _processMsg;
                //return DocActionVariables.STATUS_INVALID;
            }

            // Do Not change return msg here as it is being used in the calling function
            return "@VAB_CashJRNL_ID@: " + cash.GetName() + " #" + cl.GetLine();
            //Info.Append("@VAB_CashJRNL_ID@: " + cash.GetName() + " #" + cl.GetLine());
            //SetVAB_CashJRNLLine_ID(cl.GetVAB_CashJRNLLine_ID());
        }

        #region DocAction Members


        public Env.QueryParams GetLineOrgsQueryInfo()
        {
            return null;
        }

        public DateTime? GetDocumentDate()
        {
            return null;
        }

        public string GetDocBaseType()
        {
            return null;
        }

        public void SetProcessMsg(string processMsg)
        {
            _processMsg = processMsg;
        }

        /// <summary>
        /// update counter Business partner and Organization
        /// </summary>
        /// <param name="BPartner">counter Business Partner</param>
        /// <param name="counterAdOrgId">counter Organization</param>
        private void SetCounterBPartner(MVABBusinessPartner BPartner, int counterAdOrgId)
        {
            counterBPartner = BPartner;
            counterOrgId = counterAdOrgId;
        }

        private MVABBusinessPartner GetCounterBPartner()
        {
            return counterBPartner;
        }

        private int GetCounterOrgID()
        {
            return counterOrgId;
        }

        #endregion

    }
}