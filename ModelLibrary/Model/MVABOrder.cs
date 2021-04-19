﻿/********************************************************
 * Module Name    : 
 * Purpose        : 
 * Class Used     : X_VAB_Order, DocAction(Interface)
 * Chronological Development
 * Veena Pandey     18-May-2009
 * Raghunandan      17-june-2009 
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
using System.IO;
using System.Data.SqlClient;
using VAdvantage.Logging;
using VAdvantage.Print;
using System.Reflection;
using ModelLibrary.Classes;

namespace VAdvantage.Model
{
    /// <summary>
    /// Order model.
    /// Please do not set DocStatus and VAB_DocTypes_ID directly. 
    /// They are set in the Process() method. 
    /// Use DocAction and VAB_DocTypesTarget_ID instead.
    /// </summary>
    public class MVABOrder : X_VAB_Order, DocAction
    {
        #region Variables
        /**	Process Message 			*/
        private String _processMsg = null;

        /**	Order Lines					*/
        private MVABOrderLine[] _lines = null;
        /**	Tax Lines					*/
        private MVABOrderTax[] _taxes = null;
        /** Force Creation of order		*/
        private bool _forceCreation = false;
        /**	Just Prepared Flag			*/
        private bool _justPrepared = false;

        /**Create Counter Document **/
        //private int havingPriceList;
        private MVABBusinessPartner counterBPartner = null;
        private int counterOrgId = 0;
        private int counterWarehouseId = 0;
        /** Sales Order Sub Type - SO	*/
        public static String DocSubTypeSO_Standard = "SO";
        /** Sales Order Sub Type - OB	*/
        public static String DocSubTypeSO_Quotation = "OB";
        /** Sales Order Sub Type - ON	*/
        public static String DocSubTypeSO_Proposal = "ON";
        /** Sales Order Sub Type - PR	*/
        public static String DocSubTypeSO_Prepay = "PR";
        /** Sales Order Sub Type - WR	*/
        public static String DocSubTypeSO_POS = "WR";
        /** Sales Order Sub Type - WP	*/
        public static String DocSubTypeSO_Warehouse = "WP";
        /** Sales Order Sub Type - WI	*/
        public static String DocSubTypeSO_OnCredit = "WI";
        /** Sales Order Sub Type - RM	*/
        public static String DocSubTypeSO_RMA = "RM";
        String DocSubTypeSO = "";
        public Decimal? OnHandQty = 0;
        /**is container applicable */
        private bool isContainerApplicable = false;

        private String _budgetMessage = String.Empty;

        #endregion

        /* 	Create new Order by copying
         * 	@param from order
         * 	@param dateDoc date of the document date
         * 	@param VAB_DocTypesTarget_ID target document type
         * 	@param isSOTrx sales order 
         * 	@param counter create counter links
         *	@param copyASI copy line attributes Attribute Set Instance, Resource Assignment
         * 	@param trxName trx
         *	@return Order
         */
        public static MVABOrder CopyFrom(MVABOrder from, DateTime? dateDoc, int VAB_DocTypesTarget_ID, bool counter, bool copyASI, Trx trxName, bool fromCreateSO = false)//added optional parameter which indicates from where this function is being called(If this fuction is called from Create SO Process on Sales Quotation window then fromCreateSO is true otherwise it will be false)----Neha
        {
            MVABOrder to = new MVABOrder(from.GetCtx(), 0, trxName);


            to.Set_TrxName(trxName);
            PO.CopyValues(from, to, from.GetVAF_Client_ID(), from.GetVAF_Org_ID());
            to.Set_ValueNoCheck("VAB_Order_ID", I_ZERO);
            to.Set_ValueNoCheck("DocumentNo", null);
            //
            to.SetDocStatus(DOCSTATUS_Drafted);		//	Draft
            to.SetDocAction(DOCACTION_Complete);
            //
            to.SetVAB_DocTypes_ID(0);
            to.SetVAB_DocTypesTarget_ID(VAB_DocTypesTarget_ID, true);
            //
            to.SetIsSelected(false);
            to.SetDateOrdered(dateDoc);
            to.SetDateAcct(dateDoc);
            to.SetDatePromised(dateDoc);	//	assumption
            to.SetDatePrinted(null);
            to.SetIsPrinted(false);
            //
            to.SetIsApproved(false);
            to.SetIsCreditApproved(false);
            to.SetVAB_Payment_ID(0);
            to.SetVAB_CashJRNLLine_ID(0);
            //	Amounts are updated  when adding lines
            to.SetGrandTotal(Env.ZERO);
            to.SetTotalLines(Env.ZERO);
            //
            to.SetIsDelivered(false);
            to.SetIsInvoiced(false);
            to.SetIsSelfService(false);
            to.SetIsTransferred(false);
            to.SetPosted(false);
            to.SetProcessed(false);
            if (counter)
            {
                to.SetRef_Order_ID(from.GetVAB_Order_ID());

                //SI_0625 : Link Organization Functionality
                // set counter BP Org
                if (from.GetCounterOrgID() > 0)
                    to.SetVAF_Org_ID(from.GetCounterOrgID());

                //set warehouse
                if (from.GetCounterWarehouseID() > 0)
                    to.SetVAM_Warehouse_ID(from.GetCounterWarehouseID());

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
                        //Could not create Order. Price List not avialable
                        from.SetProcessMsg(Msg.GetMsg(from.GetCtx(), "VIS_PriceListNotFound"));
                        throw new Exception("Could not create Order. Price List not avialable");
                    }
                }
            }
            else
            {
                to.SetRef_Order_ID(0);
            }
            //
            if (!to.Save(trxName))
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null)
                {
                    from.SetProcessMsg("Could not create Order. " + pp.GetName());
                }
                else
                {
                    from.SetProcessMsg("Could not create Order.");
                }
                throw new Exception("Could not create Order. " + (pp != null && pp.GetName() != null ? pp.GetName() : ""));
            }
            if (counter)
            {
                from.SetRef_Order_ID(to.GetVAB_Order_ID());
            }

            if (to.CopyLinesFrom(from, counter, copyASI, fromCreateSO) == 0)//added optional parameter which indicates from where this function is being called(If this fuction is called from Create SO Process on Sales Quotation window then fromCreateSO is true otherwise it will be false)----Neha
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null)
                {
                    from.SetProcessMsg("Could not create Order Lines. " + pp.GetName());
                }
                else
                {
                    from.SetProcessMsg("Could not create Order Lines.");
                }
                throw new Exception("Could not create Order Lines. " + (pp != null && pp.GetName() != null ? pp.GetName() : ""));
            }

            return to;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="VAB_Order_ID"></param>
        /// <param name="trxName"></param>
        public MVABOrder(Ctx ctx, int VAB_Order_ID, Trx trxName)
            : base(ctx, VAB_Order_ID, trxName)
        {

            //  New
            if (VAB_Order_ID == 0)
            {
                SetDocStatus(DOCSTATUS_Drafted);
                SetDocAction(DOCACTION_Prepare);
                //
                SetDeliveryRule(DELIVERYRULE_Force);
                SetFreightCostRule(FREIGHTCOSTRULE_FreightIncluded);
                SetInvoiceRule(INVOICERULE_Immediate);
                SetPaymentRule(PAYMENTRULE_OnCredit);
                SetPriorityRule(PRIORITYRULE_Medium);
                SetDeliveryViaRule(DELIVERYVIARULE_Pickup);
                //
                SetIsDiscountPrinted(false);
                SetIsSelected(false);
                SetIsTaxIncluded(false);
                SetIsSOTrx(true);
                SetIsDropShip(false);
                SetSendEMail(false);
                //
                SetIsApproved(false);
                SetIsPrinted(false);
                SetIsCreditApproved(false);
                SetIsDelivered(false);
                SetIsInvoiced(false);
                SetIsTransferred(false);
                SetIsSelfService(false);
                SetIsReturnTrx(false);
                //
                base.SetProcessed(false);
                SetProcessing(false);
                SetPosted(false);

                SetDateAcct(Convert.ToDateTime(DateTime.Now));// CommonFunctions.CurrentTimeMillis()));
                SetDatePromised(Convert.ToDateTime(DateTime.Now));// CommonFunctions.CurrentTimeMillis()));
                SetDateOrdered(Convert.ToDateTime(DateTime.Now));// CommonFunctions.CurrentTimeMillis()));

                SetFreightAmt(Env.ZERO);
                SetChargeAmt(Env.ZERO);
                SetTotalLines(Env.ZERO);
                SetGrandTotal(Env.ZERO);
            }


        }

        /*  Project Constructor
        *  @param  project Project to create Order from
        *  @param IsSOTrx sales order
        * 	@param	DocSubTypeSO if SO DocType Target (default DocSubTypeSO_OnCredit)
        */
        public MVABOrder(MVABProject project, bool IsSOTrx, String DocSubTypeSO)
            : this(project.GetCtx(), 0, project.Get_TrxName())
        {


            SetVAF_Client_ID(project.GetVAF_Client_ID());
            SetVAF_Org_ID(project.GetVAF_Org_ID());
            SetVAB_Promotion_ID(project.GetVAB_Promotion_ID());
            SetSalesRep_ID(project.GetSalesRep_ID());
            //
            SetVAB_Project_ID(project.GetVAB_Project_ID());
            SetDescription(project.GetName());
            DateTime? ts = project.GetDateContract();
            if (ts != null)
                SetDateOrdered(ts);
            ts = project.GetDateFinish();
            if (ts != null)
                SetDatePromised(ts);
            //
            SetVAB_BusinessPartner_ID(project.GetVAB_BusinessPartner_ID());
            SetVAB_BPart_Location_ID(project.GetVAB_BPart_Location_ID());
            SetVAF_UserContact_ID(project.GetVAF_UserContact_ID());
            //
            SetVAM_Warehouse_ID(project.GetVAM_Warehouse_ID());
            SetVAM_PriceList_ID(project.GetVAM_PriceList_ID());
            SetVAB_PaymentTerm_ID(project.GetVAB_PaymentTerm_ID());
            //
            SetIsSOTrx(IsSOTrx);
            if (IsSOTrx)
            {
                if (DocSubTypeSO == null || DocSubTypeSO.Length == 0)
                    SetVAB_DocTypesTarget_ID(DocSubTypeSO_OnCredit);
                else
                    SetVAB_DocTypesTarget_ID(DocSubTypeSO);
            }
            else
            {
                SetVAB_DocTypesTarget_ID();
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dr"></param>
        /// <param name="trxName"></param>
        public MVABOrder(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        /*	Overwrite Client/Org if required
        * 	@param VAF_Client_ID client
        * 	@param VAF_Org_ID org
        */
        public new void SetClientOrg(int VAF_Client_ID, int VAF_Org_ID)
        {
            base.SetClientOrg(VAF_Client_ID, VAF_Org_ID);
        }

        /// <summary>
        /// Add to Description
        /// </summary>
        /// <param name="description">text</param>
        public void AddDescription(String description)
        {
            String desc = GetDescription();
            if (desc == null)
            {
                SetDescription(description);
            }
            else
            {
                SetDescription(desc + " | " + description);
            }
        }

        /**
         * 	Set Business Partner (Ship+Bill)
         *	@param VAB_BusinessPartner_ID bpartner
         */
        public new void SetVAB_BusinessPartner_ID(int VAB_BusinessPartner_ID)
        {
            base.SetVAB_BusinessPartner_ID(VAB_BusinessPartner_ID);
            base.SetBill_BPartner_ID(VAB_BusinessPartner_ID);
        }

        /**
         * 	Set Business Partner Defaults & Details.
         * 	SOTrx should be set.
         * 	@param bp business partner
         */
        public void SetBPartner(MVABBusinessPartner bp)
        {
            try
            {
                if (bp == null || !bp.IsActive())
                    return;

                SetVAB_BusinessPartner_ID(bp.GetVAB_BusinessPartner_ID());
                //	Defaults Payment Term
                int ii = 0;
                if (IsSOTrx())
                    ii = bp.GetVAB_PaymentTerm_ID();
                else
                    ii = bp.GetPO_PaymentTerm_ID();
                if (ii != 0)
                    SetVAB_PaymentTerm_ID(ii);
                //	Default Price List
                if (IsSOTrx())
                    ii = bp.GetVAM_PriceList_ID();
                else
                    ii = bp.GetPO_PriceList_ID();
                if (ii != 0)
                    SetVAM_PriceList_ID(ii);
                //	Default Delivery/Via Rule
                String ss = bp.GetDeliveryRule();
                if (ss != null)
                    SetDeliveryRule(ss);
                ss = bp.GetDeliveryViaRule();
                if (ss != null)
                    SetDeliveryViaRule(ss);
                //	Default Invoice/Payment Rule
                ss = bp.GetInvoiceRule();
                if (ss != null)
                    SetInvoiceRule(ss);
                if (IsSOTrx())
                    ss = bp.GetPaymentRule();
                else
                    ss = bp.GetPaymentRulePO();
                if (ss != null)
                    SetPaymentRule(ss);
                //	Sales Rep
                ii = bp.GetSalesRep_ID();
                if (ii != 0)
                    SetSalesRep_ID(ii);


                //	Set Locations
                MVABBPartLocation[] locs = bp.GetLocations(false);
                if (locs != null)
                {
                    for (int i = 0; i < locs.Length; i++)
                    {
                        if (locs[i].IsShipTo())
                            base.SetVAB_BPart_Location_ID(locs[i].GetVAB_BPart_Location_ID());
                        if (locs[i].IsBillTo())
                            SetBill_Location_ID(locs[i].GetVAB_BPart_Location_ID());
                    }
                    //	set to first
                    if (GetVAB_BPart_Location_ID() == 0 && locs.Length > 0)
                        base.SetVAB_BPart_Location_ID(locs[0].GetVAB_BPart_Location_ID());
                    if (GetBill_Location_ID() == 0 && locs.Length > 0)
                        SetBill_Location_ID(locs[0].GetVAB_BPart_Location_ID());
                }
                if (GetVAB_BPart_Location_ID() == 0)
                {
                    log.Log(Level.SEVERE, "MOrder.setBPartner - Has no Ship To Address: " + bp);
                }
                if (GetBill_Location_ID() == 0)
                {
                    log.Log(Level.SEVERE, "MOrder.setBPartner - Has no Bill To Address: " + bp);
                }

                //	Set Contact
                MVAFUserContact[] contacts = bp.GetContacts(false);
                if (contacts != null && contacts.Length == 1)
                {
                    SetVAF_UserContact_ID(contacts[0].GetVAF_UserContact_ID());
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetBPartner");
            }
        }

        /**
         * 	Set Business Partner - Callout
         *	@param oldVAB_BusinessPartner_ID old BP
         *	@param newVAB_BusinessPartner_ID new BP
         *	@param windowNo window no
         */
        //@UICallout 
        public void SetVAB_BusinessPartner_ID(String oldVAB_BusinessPartner_ID, String newVAB_BusinessPartner_ID, int windowNo)
        {
            if (newVAB_BusinessPartner_ID == null || newVAB_BusinessPartner_ID.Length == 0)
                return;
            int VAB_BusinessPartner_ID = Convert.ToInt32(newVAB_BusinessPartner_ID);
            if (VAB_BusinessPartner_ID == 0)
                return;

            // Skip these steps for RMA. These fields are copied over from the orignal order instead.
            if (IsReturnTrx())
                return;

            String sql = "SELECT p.VAF_Language,p.VAB_PaymentTerm_ID,"
                + " COALESCE(p.VAM_PriceList_ID,g.VAM_PriceList_ID) AS VAM_PriceList_ID, p.PaymentRule,p.POReference,"
                + " p.SO_Description,p.IsDiscountPrinted,"
                + " p.InvoiceRule,p.DeliveryRule,p.FreightCostRule,DeliveryViaRule,"
                + " p.SO_CreditLimit, p.SO_CreditLimit-p.SO_CreditUsed AS CreditAvailable,"
                + " lship.VAB_BPart_Location_ID,c.VAF_UserContact_ID,"
                + " COALESCE(p.PO_PriceList_ID,g.PO_PriceList_ID) AS PO_PriceList_ID, p.PaymentRulePO,p.PO_PaymentTerm_ID,"
                + " lbill.VAB_BPart_Location_ID AS Bill_Location_ID, p.SOCreditStatus, lbill.IsShipTo "
                + "FROM VAB_BusinessPartner p"
                + " INNER JOIN VAB_BPart_Category g ON (p.VAB_BPart_Category_ID=g.VAB_BPart_Category_ID)"
                + " LEFT OUTER JOIN VAB_BPart_Location lbill ON (p.VAB_BusinessPartner_ID=lbill.VAB_BusinessPartner_ID AND lbill.IsBillTo='Y' AND lbill.IsActive='Y')"
                + " LEFT OUTER JOIN VAB_BPart_Location lship ON (p.VAB_BusinessPartner_ID=lship.VAB_BusinessPartner_ID AND lship.IsShipTo='Y' AND lship.IsActive='Y')"
                + " LEFT OUTER JOIN VAF_UserContact c ON (p.VAB_BusinessPartner_ID=c.VAB_BusinessPartner_ID) "
                + "WHERE p.VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID + " AND p.IsActive='Y'";		//	#1

            bool isSOTrx = IsSOTrx();

            DataTable dt = null;

            try
            {

                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {

                    base.SetVAB_BusinessPartner_ID(VAB_BusinessPartner_ID);

                    //	PriceList (indirect: IsTaxIncluded & Currency)
                    int ii = Utility.Util.GetValueOfInt(dr[isSOTrx ? "VAM_PriceList_ID" : "PO_PriceList_ID"].ToString());
                    if (ii != 0)
                        SetVAM_PriceList_ID(null, ii.ToString(), windowNo);
                    else
                    {	//	get default PriceList
                        ii = GetCtx().GetContextAsInt("#VAM_PriceList_ID");
                        if (ii != 0)
                            SetVAM_PriceList_ID(null, ii.ToString(), windowNo);
                    }

                    //	Bill-To BPartner
                    SetBill_BPartner_ID(VAB_BusinessPartner_ID);
                    int bill_Location_ID = Utility.Util.GetValueOfInt(dr["Bill_Location_ID"].ToString());
                    if (bill_Location_ID == 0)
                    {
                        //   p_changeVO.addChangedValue("Bill_Location_ID", (String)null);
                    }
                    else
                    {
                        SetBill_Location_ID(bill_Location_ID);
                    }

                    // Ship-To Location
                    int shipTo_ID = Utility.Util.GetValueOfInt(dr["VAB_BPart_Location_ID"].ToString());
                    //	overwritten by InfoBP selection - works only if InfoWindow
                    //	was used otherwise creates error (uses last value, may belong to differnt BP)
                    if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BusinessPartner_ID") == VAB_BusinessPartner_ID)
                    {
                        String loc = GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BPart_Location_ID");
                        if (loc.Length > 0)
                            shipTo_ID = int.Parse(loc);
                    }
                    if (shipTo_ID == 0)
                    {
                        // p_changeVO.addChangedValue("VAB_BPart_Location_ID", (String)null);
                    }
                    else
                    {
                        SetVAB_BPart_Location_ID(shipTo_ID);
                    }
                    if ("Y".Equals(dr["IsShipTo"].ToString()))	//	set the same
                        SetBill_Location_ID(shipTo_ID);

                    //	Contact - overwritten by InfoBP selection
                    int contID = Utility.Util.GetValueOfInt(dr["VAF_UserContact_ID"].ToString());
                    if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BusinessPartner_ID") == VAB_BusinessPartner_ID)
                    {
                        String cont = GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAF_UserContact_ID");
                        if (cont.Length > 0)
                            contID = int.Parse(cont);
                    }
                    SetVAF_UserContact_ID(contID);
                    SetBill_User_ID(contID);

                    //	CreditAvailable 
                    if (isSOTrx)
                    {
                        Decimal CreditLimit = Utility.Util.GetValueOfDecimal(dr["SO_CreditLimit"].ToString());
                        //	String SOCreditStatus = dr.getString("SOCreditStatus");
                        if (CreditLimit != null && Env.Signum(CreditLimit) != 0)
                        {
                            Decimal CreditAvailable = Utility.Util.GetValueOfDecimal(dr["CreditAvailable"].ToString());
                            //if (p_changeVO != null && CreditAvailable != null && CreditAvailable.signum() < 0)
                            //{
                            //    String msg = Msg.getMsg(GetCtx(), "CreditLimitOver",DisplayType.getNumberFormat(DisplayType.Amount).format(CreditAvailable));
                            //    p_changeVO.addError(msg);
                            //}
                        }
                    }

                    //	PO Reference
                    String s = dr["POReference"].ToString();
                    if (s != null && s.Length != 0)
                        SetPOReference(s);

                    //	SO Description
                    s = dr["SO_Description"].ToString();
                    if (s != null && s.Trim().Length != 0)
                        SetDescription(s);
                    //	IsDiscountPrinted
                    s = dr["IsDiscountPrinted"].ToString();
                    SetIsDiscountPrinted("Y".Equals(s));

                    //	Defaults, if not Walk-in Receipt or Walk-in Invoice
                    String OrderType = GetCtx().GetContext(windowNo, "OrderType");
                    SetInvoiceRule(INVOICERULE_AfterDelivery);
                    SetDeliveryRule(DELIVERYRULE_Availability);
                    SetPaymentRule(PAYMENTRULE_OnCredit);
                    if (OrderType.Equals(DocSubTypeSO_Prepay))
                    {
                        SetInvoiceRule(INVOICERULE_Immediate);
                        SetDeliveryRule(DELIVERYRULE_AfterReceipt);
                    }
                    else if (OrderType.Equals(MVABOrder.DocSubTypeSO_POS))	//  for POS
                        SetPaymentRule(PAYMENTRULE_Cash);
                    else
                    {
                        //	PaymentRule
                        s = dr[isSOTrx ? "PaymentRule" : "PaymentRulePO"].ToString();
                        if (s != null && s.Length != 0)
                        {
                            if (s.Equals("B"))				//	No Cache in Non POS
                                s = PAYMENTRULE_OnCredit;	//  Payment Term
                            if (isSOTrx && (s.Equals("S") || s.Equals("U")))	//	No Check/Transfer for SO_Trx
                                s = PAYMENTRULE_OnCredit;	//  Payment Term
                            SetPaymentRule(s);
                        }
                        //	Payment Term
                        ii = Utility.Util.GetValueOfInt(dr[isSOTrx ? "VAB_PaymentTerm_ID" : "PO_PaymentTerm_ID"].ToString());
                        if (ii != 0)
                            SetVAB_PaymentTerm_ID(ii);
                        //	InvoiceRule
                        s = dr["InvoiceRule"].ToString();
                        if (s != null && s.Length != 0)
                            SetInvoiceRule(s);
                        //	DeliveryRule
                        s = dr["DeliveryRule"].ToString();
                        if (s != null && s.Length != 0)
                            SetDeliveryRule(s);
                        //	FreightCostRule
                        s = dr["FreightCostRule"].ToString();
                        if (s != null && s.Length != 0)
                            SetFreightCostRule(s);
                        //	DeliveryViaRule
                        s = dr["DeliveryViaRule"].ToString();
                        if (s != null && s.Length != 0)
                            SetDeliveryViaRule(s);
                    }
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAB_BusinessPartner_ID");
            }
            finally { dt = null; }
        }


        /**
         * 	Set Bill Business Partner - Callout
         *	@param oldBill_BPartner_ID old BP
         *	@param newBill_BPartner_ID new BP
         *	@param windowNo window no
         */
        //@UICallout
        public void SetBill_BPartner_ID(String oldBill_BPartner_ID, String newBill_BPartner_ID, int windowNo)
        {
            if (newBill_BPartner_ID == null || newBill_BPartner_ID.Length == 0)
                return;
            int bill_BPartner_ID = int.Parse(newBill_BPartner_ID);
            if (bill_BPartner_ID == 0)
                return;

            // Skip these steps for RMA. These fields are copied over from the orignal order instead.
            if (IsReturnTrx())
                return;

            String sql = "SELECT p.VAF_Language,p.VAB_PaymentTerm_ID,"
                + "p.VAM_PriceList_ID,p.PaymentRule,p.POReference,"
                + "p.SO_Description,p.IsDiscountPrinted,"
                + "p.InvoiceRule,p.DeliveryRule,p.FreightCostRule,DeliveryViaRule,"
                + "p.SO_CreditLimit, p.SO_CreditLimit-p.SO_CreditUsed AS CreditAvailable,"
                + "c.VAF_UserContact_ID,"
                + "p.PO_PriceList_ID, p.PaymentRulePO, p.PO_PaymentTerm_ID,"
                + "lbill.VAB_BPart_Location_ID AS Bill_Location_ID "
                + "FROM VAB_BusinessPartner p"
                + " LEFT OUTER JOIN VAB_BPart_Location lbill ON (p.VAB_BusinessPartner_ID=lbill.VAB_BusinessPartner_ID AND lbill.IsBillTo='Y' AND lbill.IsActive='Y')"
                + " LEFT OUTER JOIN VAF_UserContact c ON (p.VAB_BusinessPartner_ID=c.VAB_BusinessPartner_ID) "
                + "WHERE p.VAB_BusinessPartner_ID=" + bill_BPartner_ID + " AND p.IsActive='Y'";		//	#1

            bool isSOTrx = IsSOTrx();
            DataTable dt = null;
            try
            {

                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    base.SetBill_BPartner_ID(bill_BPartner_ID);
                    //	PriceList (indirect: IsTaxIncluded & Currency)
                    int ii = Utility.Util.GetValueOfInt(dr[isSOTrx ? "VAM_PriceList_ID" : "PO_PriceList_ID"].ToString());
                    if (ii != 0)
                        SetVAM_PriceList_ID(null, ii.ToString(), windowNo);
                    else
                    {	//	get default PriceList
                        ii = GetCtx().GetContextAsInt("#VAM_PriceList_ID");
                        if (ii != 0)
                            SetVAM_PriceList_ID(null, ii.ToString(), windowNo);
                    }

                    int bill_Location_ID = Utility.Util.GetValueOfInt(dr["Bill_Location_ID"].ToString());
                    //	overwritten by InfoBP selection - works only if InfoWindow
                    //	was used otherwise creates error (uses last value, may belong to differnt BP)
                    if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BusinessPartner_ID") == bill_BPartner_ID)
                    {
                        String loc = GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BPart_Location_ID");
                        if (loc.Length > 0)
                            bill_Location_ID = int.Parse(loc);
                    }
                    if (bill_Location_ID != 0)
                        SetBill_Location_ID(bill_Location_ID);

                    //	Contact - overwritten by InfoBP selection
                    int contID = Utility.Util.GetValueOfInt(dr["VAF_UserContact_ID"].ToString());
                    if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_BusinessPartner_ID") == bill_BPartner_ID)
                    {
                        String cont = GetCtx().GetContext(Env.WINDOW_INFO, Env.TAB_INFO, "VAF_UserContact_ID");
                        if (cont.Length > 0)
                            contID = int.Parse(cont);
                    }
                    SetBill_User_ID(contID);

                    //	CreditAvailable 
                    if (isSOTrx)
                    {
                        Decimal CreditLimit = Utility.Util.GetValueOfDecimal(dr["SO_CreditLimit"].ToString());
                        //	String SOCreditStatus = dr.getString("SOCreditStatus");
                        if (CreditLimit != null && Env.Signum(CreditLimit) != 0)
                        {
                            Decimal CreditAvailable = Utility.Util.GetValueOfDecimal(dr["CreditAvailable"].ToString());
                            //if (p_changeVO != null && CreditAvailable != null && Env.Signum(CreditAvailable) < 0)
                            //{
                            //    String msg = Msg.getMsg(GetCtx(), "CreditLimitOver",DisplayType.getNumberFormat(DisplayType.Amount).format(CreditAvailable));
                            //    p_changeVO.addError(msg);
                            //}
                        }
                    }

                    //	PO Reference
                    String s = dr["POReference"].ToString();

                    // Order Reference should not be reset by Bill To BPartner; only by BPartner 
                    /*if (s != null && s.Length != 0)
                        setPOReference(s); */
                    //	SO Description
                    s = dr["SO_Description"].ToString();
                    if (s != null && s.Trim().Length != 0)
                        SetDescription(s);
                    //	IsDiscountPrinted
                    s = dr["IsDiscountPrinted"].ToString();
                    SetIsDiscountPrinted("Y".Equals(s));

                    //	Defaults, if not Walk-in Receipt or Walk-in Invoice
                    //	Defaults, if not Walk-in Receipt or Walk-in Invoice
                    String OrderType = GetCtx().GetContext(windowNo, "OrderType");
                    SetInvoiceRule(INVOICERULE_AfterDelivery);
                    SetPaymentRule(PAYMENTRULE_OnCredit);
                    if (OrderType.Equals(DocSubTypeSO_Prepay))
                        SetInvoiceRule(INVOICERULE_Immediate);
                    else if (OrderType.Equals(MVABOrder.DocSubTypeSO_POS))	//  for POS
                        SetPaymentRule(PAYMENTRULE_Cash);
                    else
                    {
                        //	PaymentRule
                        s = dr[isSOTrx ? "PaymentRule" : "PaymentRulePO"].ToString();
                        if (s != null && s.Length != 0)
                        {
                            if (s.Equals("B"))				//	No Cache in Non POS
                                s = PAYMENTRULE_OnCredit;	//  Payment Term
                            if (isSOTrx && (s.Equals("S") || s.Equals("U")))	//	No Check/Transfer for SO_Trx
                                s = PAYMENTRULE_OnCredit;	//  Payment Term
                            SetPaymentRule(s);
                        }
                        //	Payment Term
                        ii = Utility.Util.GetValueOfInt(dr[isSOTrx ? "VAB_PaymentTerm_ID" : "PO_PaymentTerm_ID"].ToString());
                        if (ii != 0)
                            SetVAB_PaymentTerm_ID(ii);
                        //	InvoiceRule
                        s = dr["InvoiceRule"].ToString();
                        if (s != null && s.Length != 0)
                            SetInvoiceRule(s);
                    }
                }

                //dt.Dispose();
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAB_BusinessPartner_ID-CallOut");
            }
            finally
            {
                dt = null;
            }
        }


        /**
         * 	Set Business Partner Location (Ship+Bill)
         *	@param VAB_BPart_Location_ID bp location
         */
        public new void SetVAB_BPart_Location_ID(int VAB_BPart_Location_ID)
        {
            base.SetVAB_BPart_Location_ID(VAB_BPart_Location_ID);
            base.SetBill_Location_ID(VAB_BPart_Location_ID);
        }


        /// <summary>
        /// Set Business Partner Contact (Ship+Bill)
        /// </summary>
        /// <param name="VAF_UserContact_ID">user</param>
        public new void SetVAF_UserContact_ID(int VAF_UserContact_ID)
        {
            base.SetVAF_UserContact_ID(VAF_UserContact_ID);
            base.SetBill_User_ID(VAF_UserContact_ID);
        }

        /*	Set Ship Business Partner
        *	@param VAB_BusinessPartner_ID bpartner
        */
        public void SetShip_BPartner_ID(int VAB_BusinessPartner_ID)
        {
            base.SetVAB_BusinessPartner_ID(VAB_BusinessPartner_ID);
        }

        /**
         * 	Set Ship Business Partner Location
         *	@param VAB_BPart_Location_ID bp location
         */
        public void SetShip_Location_ID(int VAB_BPart_Location_ID)
        {
            base.SetVAB_BPart_Location_ID(VAB_BPart_Location_ID);
        }

        /**
         * 	Set Ship Business Partner Contact
         *	@param VAF_UserContact_ID user
         */
        public void SetShip_User_ID(int VAF_UserContact_ID)
        {
            base.SetVAF_UserContact_ID(VAF_UserContact_ID);
        }


        /**
         * 	Set Warehouse
         *	@param VAM_Warehouse_ID warehouse
         */
        public new void SetVAM_Warehouse_ID(int VAM_Warehouse_ID)
        {
            base.SetVAM_Warehouse_ID(VAM_Warehouse_ID);
        }

        /**
         * 	Set Drop Ship
         *	@param IsDropShip drop ship
         */
        public new void SetIsDropShip(bool IsDropShip)
        {
            base.SetIsDropShip(IsDropShip);
        }

        /**
         * 	Set DateOrdered - Callout
         *	@param oldDateOrdered old
         *	@param newDateOrdered new
         *	@param windowNo window no
         */
        //@UICallout 
        public void SetDateOrdered(String oldDateOrdered, String newDateOrdered, int windowNo)
        {
            try
            {
                if (newDateOrdered == null || newDateOrdered.Length == 0)
                {
                    return;
                }
                DateTime? dateOrdered = (DateTime?)PO.ConvertToTimestamp(newDateOrdered);
                if (dateOrdered == null)
                {
                    return;
                }
                SetDateOrdered(dateOrdered);
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetDateOrdered");
            }
        }

        /**
         *	Set Date Ordered and Acct Date
         */
        public new void SetDateOrdered(DateTime? dateOrdered)
        {
            base.SetDateOrdered(dateOrdered);
            base.SetDateAcct(dateOrdered);
        }


        /*	Set Target Sales Document Type - Callout.
        * 	Sets OrderType (=DocSubTypeSO), HasCharges [ctx only]
        * 	IsDropShip, DeliveryRule, InvoiceRule, PaymentRule, IsSOTrx, DocumentNo
        * 	If BP is changed: PaymentRule, VAB_PaymentTerm_ID, InvoiceRule, DeliveryRule,
        * 	FreightCostRule, DeliveryViaRule
        * 	@param oldVAB_DocTypesTarget_ID old ID
        * 	@param newVAB_DocTypesTarget_ID new ID
        * 	@param windowNo window
        */
        //@UICallout
        public void SetVAB_DocTypesTarget_ID(String oldVAB_DocTypesTarget_ID, String newVAB_DocTypesTarget_ID, int windowNo)
        {
            if (newVAB_DocTypesTarget_ID == null || newVAB_DocTypesTarget_ID.Length == 0)
                return;
            int VAB_DocTypesTarget_ID = int.Parse(newVAB_DocTypesTarget_ID);
            if (VAB_DocTypesTarget_ID == 0)
                return;

            //	Re-Create new DocNo, if there is a doc number already
            //	and the existing source used a different Sequence number
            String oldDocNo = GetDocumentNo();
            bool newDocNo = (oldDocNo == null);
            if (!newDocNo && oldDocNo.StartsWith("<") && oldDocNo.EndsWith(">"))
                newDocNo = true;
            int oldVAB_DocTypes_ID = GetVAB_DocTypes_ID();

            String sql = "SELECT d.DocSubTypeSO,d.HasCharges,'N',"			//	1..3
                + "d.IsDocNoControlled,s.CurrentNext,s.CurrentNextSys,"     //  4..6
                + "s.VAF_Record_Seq_ID,d.IsSOTrx,d.IsReturnTrx "               //	7..9
                + "FROM VAB_DocTypes d "
                + "LEFT OUTER JOIN VAF_Record_Seq s ON (d.DocNoSequence_ID=s.VAF_Record_Seq_ID) "
                + "WHERE VAB_DocTypes_ID=";	//	#1
            DataTable dt = null;
            try
            {
                int VAF_Record_Seq_ID = 0;

                IDataReader idr = null;
                //	Get old AD_SeqNo for comparison
                if (!newDocNo && oldVAB_DocTypes_ID != 0)
                {
                    sql = sql + oldVAB_DocTypes_ID;
                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    dt = new DataTable();
                    dt.Load(idr);
                    idr.Close();
                    foreach (DataRow dr in dt.Rows)
                    {
                        VAF_Record_Seq_ID = Utility.Util.GetValueOfInt(dr[5].ToString());
                    }
                    dt = null;
                }
                sql = sql + VAB_DocTypesTarget_ID;
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                String DocSubTypeSO = "";
                bool isSOTrx = true;
                bool isReturnTrx = false;
                foreach (DataRow dr in dt.Rows)		//	we found document type
                {
                    base.SetVAB_DocTypesTarget_ID(VAB_DocTypesTarget_ID);
                    //	Set Ctx:	Document Sub Type for Sales Orders
                    DocSubTypeSO = dr[0].ToString();
                    if (DocSubTypeSO == null)
                        DocSubTypeSO = "--";
                    //if (p_changeVO != null)
                    //    p_changeVO.setContext(GetCtx(), windowNo, "OrderType", DocSubTypeSO);
                    //	No Drop Ship other than Standard
                    if (!DocSubTypeSO.Equals(DocSubTypeSO_Standard))
                        SetIsDropShip(false);

                    //	IsSOTrx
                    if ("N".Equals(dr[7].ToString()))
                        isSOTrx = false;
                    SetIsSOTrx(isSOTrx);

                    // IsReturnTrx
                    isReturnTrx = "Y".Equals(dr[8].ToString());
                    SetIsReturnTrx(isReturnTrx);

                    if (!isReturnTrx)
                    {
                        //	Delivery Rule
                        if (DocSubTypeSO.Equals(MVABOrder.DocSubTypeSO_POS))
                            SetDeliveryRule(DELIVERYRULE_Force);
                        else if (DocSubTypeSO.Equals(MVABOrder.DocSubTypeSO_Prepay))
                            SetDeliveryRule(DELIVERYRULE_AfterReceipt);
                        else
                            SetDeliveryRule(DELIVERYRULE_Availability);

                        //	Invoice Rule
                        if (DocSubTypeSO.Equals(DocSubTypeSO_POS)
                            || DocSubTypeSO.Equals(DocSubTypeSO_Prepay)
                            || DocSubTypeSO.Equals(DocSubTypeSO_OnCredit))
                            SetInvoiceRule(INVOICERULE_Immediate);
                        else
                            SetInvoiceRule(INVOICERULE_AfterDelivery);


                        //	Payment Rule - POS Order
                        if (DocSubTypeSO.Equals(DocSubTypeSO_POS))
                            SetPaymentRule(PAYMENTRULE_Cash);
                        else
                            SetPaymentRule(PAYMENTRULE_OnCredit);

                        //	Set Ctx: Charges
                        //if (p_changeVO != null)
                        //    p_changeVO.setContext(GetCtx(), windowNo, "HasCharges", dr.getString(2));
                    }
                    else
                    {
                        if (DocSubTypeSO.Equals(MVABOrder.DocSubTypeSO_POS))
                            SetDeliveryRule(DELIVERYRULE_Force);
                        else
                            SetDeliveryRule(DELIVERYRULE_Manual);
                    }

                    //	DocumentNo
                    if (dr[3].ToString().Equals("Y"))			//	IsDocNoControlled
                    {
                        if (!newDocNo && VAF_Record_Seq_ID != Utility.Util.GetValueOfInt(dr[6].ToString()))
                            newDocNo = true;
                        if (newDocNo)
                            if (Ini.IsPropertyBool(Ini._VIENNASYS) && Env.GetContext().GetVAF_Client_ID() < 1000000)
                            {
                                SetDocumentNo("<" + dr[5].ToString() + ">");
                            }
                            else
                            {
                                SetDocumentNo("<" + dr[4].ToString() + ">");
                            }
                    }
                }

                // Skip remaining steps for RMA. These are copied over from original order.
                if (isReturnTrx)
                    return;

                //  When BPartner is changed, the Rules are not set if
                //  it is a POS or Credit Order (i.e. defaults from Standard BPartner)
                //  This re-reads the Rules and applies them.
                if (DocSubTypeSO.Equals(DocSubTypeSO_POS)
                    || DocSubTypeSO.Equals(DocSubTypeSO_Prepay))    //  not for POS/PrePay
                {
                    ;
                }
                else
                {
                    int VAB_BusinessPartner_ID = GetVAB_BusinessPartner_ID();
                    sql = "SELECT PaymentRule,VAB_PaymentTerm_ID,"            //  1..2
                        + "InvoiceRule,DeliveryRule,"                       //  3..4
                        + "FreightCostRule,DeliveryViaRule, "               //  5..6
                        + "PaymentRulePO,PO_PaymentTerm_ID "
                        + "FROM VAB_BusinessPartner "
                        + "WHERE VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID;		//	#1
                    dt = null;
                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    dt = new DataTable();
                    dt.Load(idr);
                    idr.Close();
                    foreach (DataRow dr in dt.Rows)
                    {
                        //	PaymentRule
                        String paymentRule = dr[isSOTrx ? "PaymentRule" : "PaymentRulePO"].ToString();
                        if (paymentRule != null && paymentRule.Length != 0)
                        {
                            if (isSOTrx 	//	No Cash/Check/Transfer for SO_Trx
                                && (paymentRule.Equals(PAYMENTRULE_Cash)
                                    || paymentRule.Equals(PAYMENTRULE_Check)
                                    || paymentRule.Equals(PAYMENTRULE_DirectDeposit)))
                                paymentRule = PAYMENTRULE_OnCredit;				//  Payment Term
                            if (!isSOTrx 	//	No Cash for PO_Trx
                                    && (paymentRule.Equals(PAYMENTRULE_Cash)))
                                paymentRule = PAYMENTRULE_OnCredit;				//  Payment Term
                            SetPaymentRule(paymentRule);
                        }
                        //	Payment Term
                        int VAB_PaymentTerm_ID = Utility.Util.GetValueOfInt(dr[isSOTrx ? "VAB_PaymentTerm_ID" : "PO_PaymentTerm_ID"].ToString());
                        if (VAB_PaymentTerm_ID != 0)
                            SetVAB_PaymentTerm_ID(VAB_PaymentTerm_ID);
                        //	InvoiceRule
                        String invoiceRule = dr[2].ToString();
                        if (invoiceRule != null && invoiceRule.Length != 0)
                            SetInvoiceRule(invoiceRule);
                        //	DeliveryRule
                        String deliveryRule = dr[3].ToString();
                        if (deliveryRule != null && deliveryRule.Length != 0)
                            SetDeliveryRule(deliveryRule);
                        //	FreightCostRule
                        String freightCostRule = dr[4].ToString();
                        if (freightCostRule != null && freightCostRule.Length != 0)
                            SetFreightCostRule(freightCostRule);
                        //	DeliveryViaRule
                        String deliveryViaRule = dr[5].ToString();
                        if (deliveryViaRule != null && deliveryViaRule.Length != 0)
                            SetDeliveryViaRule(deliveryViaRule);
                    }
                }   //  re-read customer rules

            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;

            }
        }

        /**
         * 	Set Target Sales Document Type
         * 	@param DocSubTypeSO_x SO sub type - see DocSubTypeSO_*
         */
        public void SetVAB_DocTypesTarget_ID(String DocSubTypeSO_x)
        {
            try
            {
                String sql = "SELECT VAB_DocTypes_ID FROM VAB_DocTypes "
                    + "WHERE VAF_Client_ID=" + GetVAF_Client_ID() + " AND VAF_Org_ID IN (0," + GetVAF_Org_ID()
                    + ") AND DocSubTypeSO='" + DocSubTypeSO_x + "' AND IsReturnTrx='N' "
                    + "ORDER BY VAF_Org_ID DESC, IsDefault DESC";
                int VAB_DocTypes_ID = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, null));
                if (VAB_DocTypes_ID <= 0)
                {
                    log.Severe("Not found for VAF_Client_ID=" + GetVAF_Client_ID() + ", SubType=" + DocSubTypeSO_x);
                }
                else
                {
                    log.Fine("(SO) - " + DocSubTypeSO_x);
                    SetVAB_DocTypesTarget_ID(VAB_DocTypes_ID);
                    SetIsSOTrx(true);
                    SetIsReturnTrx(false);
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAB_DocTypesTarget_ID");
            }
        }

        /**
         * 	Set Target Document Type
         *	@param VAB_DocTypesTarget_ID id
         *	@param setReturnTrx if true set ReturnTrx and SOTrx
         */
        public void SetVAB_DocTypesTarget_ID(int VAB_DocTypesTarget_ID, bool setReturnTrx)
        {
            try
            {
                base.SetVAB_DocTypesTarget_ID(VAB_DocTypesTarget_ID);
                if (setReturnTrx)
                {
                    MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), VAB_DocTypesTarget_ID);
                    SetIsSOTrx(dt.IsSOTrx());
                    SetIsReturnTrx(dt.IsReturnTrx());
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAB_DocTypesTarget_ID(int VAB_DocTypesTarget_ID, bool setReturnTrx)");
            }
        }

        /**
         * 	Set Target Document Type.
         * 	Standard Order or PO
         */
        public void SetVAB_DocTypesTarget_ID()
        {
            try
            {
                if (IsSOTrx())		//	SO = Std Order
                {
                    SetVAB_DocTypesTarget_ID(DocSubTypeSO_Standard);
                    return;
                }
                //	PO
                String sql = "SELECT VAB_DocTypes_ID FROM VAB_DocTypes "
                    + "WHERE VAF_Client_ID=" + GetVAF_Client_ID() + " AND VAF_Org_ID IN (0," + GetVAF_Org_ID()
                    + ") AND DocBaseType='POO' AND IsReturnTrx='N' "
                    + "ORDER BY VAF_Org_ID DESC, IsDefault DESC";
                int VAB_DocTypes_ID = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, null));
                if (VAB_DocTypes_ID <= 0)
                {
                    log.Severe("No POO found for VAF_Client_ID=" + GetVAF_Client_ID());
                }
                else
                {
                    log.Fine("(PO) - " + VAB_DocTypes_ID);
                    SetVAB_DocTypesTarget_ID(VAB_DocTypes_ID);
                    SetIsReturnTrx(false);
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAB_DocTypesTarget_ID()");
            }
        }

        // New function added to set document target type according relseased order
        /**
         * 	Set Target Document Type.
         * 	@params bool released order @params
         * 	Standard Order or PO which doesn't have blanket order
         */
        public void SetVAB_DocTypesTarget_ID(bool ReleaseOrder)
        {
            string Released = "Y";
            try
            {
                if (IsSOTrx())		//	SO = Std Order
                {
                    SetVAB_DocTypesTarget_ID(DocSubTypeSO_Standard);
                    return;
                }
                if (!ReleaseOrder)
                {
                    Released = "N";
                }
                //	PO
                String sql = "SELECT VAB_DocTypes_ID FROM VAB_DocTypes "
                    + "WHERE VAF_Client_ID=" + GetVAF_Client_ID() + " AND VAF_Org_ID IN (0," + GetVAF_Org_ID()
                    + ") AND DocBaseType='POO' AND IsReturnTrx='N' AND IsReleaseDocument='" + Released + "'"
                    + " ORDER BY VAF_Org_ID DESC, IsDefault DESC";
                int VAB_DocTypes_ID = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, null));
                if (VAB_DocTypes_ID <= 0)
                {
                    log.Severe("No POO found for VAF_Client_ID=" + GetVAF_Client_ID());
                }
                else
                {
                    log.Fine("(PO) - " + VAB_DocTypes_ID);
                    SetVAB_DocTypesTarget_ID(VAB_DocTypes_ID);
                    SetIsReturnTrx(false);
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAB_DocTypesTarget_ID()");
            }
        }

        /* 	Copy Lines From other Order
        *	@param otherOrder order
        *	@param counter set counter Info
        *	@param copyASI copy line attributes Attribute Set Instance, Resaouce Assignment
        *	@return number of lines copied
        */
        public int CopyLinesFrom(MVABOrder otherOrder, bool counter, bool copyASI, bool fromCreateSO = false)//added optional parameter which indicates from where this function is being called(If this fuction is called from Create SO Process on Sales Quotation window then fromCreateSO is true otherwise it will be false)----Neha
        {
            int count = 0;
            try
            {
                if (IsProcessed() || IsPosted() || otherOrder == null)
                    return 0;
                MVABOrderLine[] fromLines = otherOrder.GetLines(false, null);

                // Added by Bharat on 05 Jan 2018 to set Values for Blanket Sales Order from Sales Quotation.
                MVABDocTypes docType = new MVABDocTypes(GetCtx(), GetVAB_DocTypesTarget_ID(), Get_TrxName());
                string docBaseType = docType.GetDocBaseType();
                for (int i = 0; i < fromLines.Length; i++)
                {
                    //issue JID_1474 If full quantity of any line is released from blanket order then system will not create that line in Release order
                    if (docType.IsReleaseDocument())
                    {
                        if (docBaseType == MVABMasterDocType.DOCBASETYPE_BLANKETSALESORDER || docBaseType == MVABMasterDocType.DOCBASETYPE_SALESORDER)
                        {
                            if (fromLines[i].GetQtyEntered() == 0)
                            {
                                continue;
                            }
                        }
                    }

                    MVABOrderLine line = new MVABOrderLine(this);
                    PO.CopyValues(fromLines[i], line, GetVAF_Client_ID(), GetVAF_Org_ID());

                    line.SetVAB_Order_ID(GetVAB_Order_ID());
                    line.SetOrder(this);
                    line.Set_ValueNoCheck("VAB_OrderLine_ID", I_ZERO);	//	new
                    line.Set_ValueNoCheck("VAB_Contract_ID", I_ZERO);
                    line.SetCreateServiceContract("N");
                    //	References
                    if (!copyASI)
                    {
                        line.SetVAM_PFeature_SetInstance_ID(0);
                        line.SetVAS_Res_Assignment_ID(0);
                    }
                    if (counter)
                        line.SetRef_OrderLine_ID(fromLines[i].GetVAB_OrderLine_ID());
                    else
                        line.SetRef_OrderLine_ID(0);
                    //
                    if (docBaseType == "BOO")
                    {
                        //Changes done by Neha---10 August 2018---Set QtyEstimation =QtyEntered when we create Blanket Sales Order from Create Sales Order Process
                        if (fromCreateSO)
                            line.Set_ValueNoCheck("QtyEstimation", fromLines[i].GetQtyEntered());
                        else
                            //Changes done by Neha Thakur--2 July,2018--Wrong Qty Estimation was updated.Set QtyEstimation at the place of QtyEntered when Copy Order Line from Copy From Process on header tab--Asked by Vineet/Pradeep
                            line.Set_ValueNoCheck("QtyEstimation", fromLines[i].GetQtyEstimation());
                    }

                    // Set Reference of Blanket Order Line on Release Order Line.
                    if (docType.IsReleaseDocument())
                    {
                        line.SetVAB_OrderLine_Blanket_ID(fromLines[i].GetVAB_OrderLine_ID());
                        // Blanket order qty not updated correctly by Release order process
                        line.SetQtyBlanket(fromLines[i].GetQtyOrdered());
                    }

                    // Added by Bharat on 06 Jan 2018 to set Values on Sales Order from Sales Quotation.
                    if (line.Get_ColumnIndex("VAB_Quotation_Line_ID") > 0)
                        line.Set_Value("VAB_Quotation_Line_ID", fromLines[i].GetVAB_OrderLine_ID());
                    // Added by Bharat on 06 Jan 2018 to set Values on Sales Order from Sales Quotation.
                    if (line.Get_ColumnIndex("VAB_Order_Quotation") > 0)
                        line.Set_Value("VAB_Order_Quotation", fromLines[i].GetVAB_Order_ID());

                    line.SetQtyDelivered(Env.ZERO);
                    line.SetQtyInvoiced(Env.ZERO);
                    line.SetQtyReserved(Env.ZERO);
                    line.SetQtyReleased(Env.ZERO);      // set Qty Released to Zero.
                    line.SetDateDelivered(null);
                    line.SetDateInvoiced(null);
                    //	Tax
                    if (GetVAB_BusinessPartner_ID() != otherOrder.GetVAB_BusinessPartner_ID())
                        line.SetTax();		//	recalculate
                    //

                    //	Tax Amount
                    // JID_1319: System should not copy Tax Amount, Line Total Amount and Taxable Amount field. System Should Auto Calculate thease field On save of lines.
                    if (GetVAM_PriceList_ID() != otherOrder.GetVAM_PriceList_ID())
                        line.SetTaxAmt();		//	recalculate Tax Amount

                    // ReCalculate Surcharge Amount
                    if (line.Get_ColumnIndex("SurchargeAmt") > 0)
                    {
                        line.SetSurchargeAmt(Env.ZERO);
                    }

                    //
                    line.SetProcessed(false);
                    if (line.Save(Get_TrxName()))
                        count++;
                    //	Cross Link
                    if (counter)
                    {
                        fromLines[i].SetRef_OrderLine_ID(line.GetVAB_OrderLine_ID());
                        fromLines[i].Save(Get_TrxName());
                    }
                }
                if (fromLines.Length != count)
                {
                    log.Log(Level.SEVERE, "Line difference - From=" + fromLines.Length + " <> Saved=" + count);
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "CopyLinesFrom");
            }
            return count;
        }

        /*	String Representation
        *	@return Info
        */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MOrder[")
                .Append(Get_ID()).Append("-").Append(GetDocumentNo())
                .Append(",IsSOTrx=").Append(IsSOTrx())
                .Append(",VAB_DocTypes_ID=").Append(GetVAB_DocTypes_ID())
                .Append(", GrandTotal=").Append(GetGrandTotal())
                .Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Get Document Info
        /// </summary>
        /// <returns>document Info (untranslated)</returns>
        public String GetDocumentInfo()
        {
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
            return dt.GetName() + " " + GetDocumentNo();
        }

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


                ReportEngine_N re = ReportEngine_N.Get(GetCtx(), ReportEngine_N.ORDER, GetVAB_Order_ID());
                if (re == null)
                    return null;

                re.GetView();
                bool b = re.CreatePDF(filePath);

                //File temp = File.createTempFile(Get_TableName() + Get_ID() + "_", ".pdf");
                //FileStream fOutStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

                FileInfo temp = new FileInfo(filePath);
                if (!temp.Exists)
                {
                    b = re.CreatePDF(filePath);
                    if (b)
                    {
                        return new FileInfo(filePath);
                    }
                    return null;
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

            return null;

        }

        /*	Set Price List (and Currency, TaxIncluded) when valid
        * 	@param VAM_PriceList_ID price list
        */
        public new void SetVAM_PriceList_ID(int VAM_PriceList_ID)
        {
            MVAMPriceList pl = MVAMPriceList.Get(GetCtx(), VAM_PriceList_ID, null);
            if (pl.Get_ID() == VAM_PriceList_ID)
            {
                base.SetVAM_PriceList_ID(VAM_PriceList_ID);
                SetVAB_Currency_ID(pl.GetVAB_Currency_ID());
                SetIsTaxIncluded(pl.IsTaxIncluded());
            }
        }

        /*	Set Price List - Callout
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
            DataTable dt = null;
            try
            {

                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    base.SetVAM_PriceList_ID(VAM_PriceList_ID);
                    //	Tax Included
                    SetIsTaxIncluded("Y".Equals(dr[0].ToString()));
                    //	Price Limit Enforce
                    //if (p_changeVO != null)
                    //    p_changeVO.setContext(GetCtx(), windowNo, "EnforcePriceLimit", dr.getString(2));
                    //	Currency
                    int ii = Utility.Util.GetValueOfInt(dr[2].ToString());
                    SetVAB_Currency_ID(ii);
                    //	PriceList Version
                    //if (p_changeVO != null)
                    //    p_changeVO.setContext(GetCtx(), windowNo, "VAM_PriceListVersion_ID", dr.getInt(5));
                }

            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAM_PriceList_ID-CallOut");
            }
            finally
            {
                dt = null;
            }
        }

        /// <summary>
        /// Set Return Policy
        /// </summary>
        public void SetVAM_ReturnRule_ID()
        {
            try
            {
                MVABBusinessPartner bpartner = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), null);
                if (bpartner.Get_ID() != 0)
                {
                    if (IsSOTrx())
                    {
                        base.SetVAM_ReturnRule_ID(bpartner.GetVAM_ReturnRule_ID());
                    }
                    else
                    {
                        base.SetVAM_ReturnRule_ID(bpartner.GetPO_ReturnPolicy_ID());
                    }
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetVAM_ReturnRule_ID");
            }

        }

        /* 	Set Original Order for RMA
         * 	SOTrx should be set.
         * 	@param origOrder MOrder
         */
        public void SetOrigOrder(MVABOrder origOrder)
        {
            try
            {
                if (origOrder == null || origOrder.Get_ID() == 0)
                    return;

                SetOrig_Order_ID(origOrder.GetVAB_Order_ID());
                //	Get Details from Original Order
                MVABBusinessPartner bpartner = new MVABBusinessPartner(GetCtx(), origOrder.GetVAB_BusinessPartner_ID(), null);

                // Reset Original Shipment
                SetOrig_InOut_ID(-1);
                SetVAB_BusinessPartner_ID(origOrder.GetVAB_BusinessPartner_ID());
                SetVAB_BPart_Location_ID(origOrder.GetVAB_BPart_Location_ID());
                SetVAF_UserContact_ID(origOrder.GetVAF_UserContact_ID());
                SetBill_BPartner_ID(origOrder.GetBill_BPartner_ID());
                SetBill_Location_ID(origOrder.GetBill_Location_ID());
                SetBill_User_ID(origOrder.GetBill_User_ID());

                SetVAM_ReturnRule_ID();

                SetVAM_PriceList_ID(origOrder.GetVAM_PriceList_ID());
                SetPaymentRule(origOrder.GetPaymentRule());
                SetVAB_PaymentTerm_ID(origOrder.GetVAB_PaymentTerm_ID());
                //setDeliveryRule(X_VAB_Order.DELIVERYRULE_Manual);

                SetBill_Location_ID(origOrder.GetBill_Location_ID());
                SetInvoiceRule(origOrder.GetInvoiceRule());
                SetPaymentRule(origOrder.GetPaymentRule());
                SetDeliveryViaRule(origOrder.GetDeliveryViaRule());
                SetFreightCostRule(origOrder.GetFreightCostRule());
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetOrigOrder");
            }
            return;

        }

        /*	Set Original Order - Callout
        *	@param oldOrig_Order_ID old Orig Order
        *	@param newOrig_Order_ID new Orig Order
        *	@param windowNo window no
        */
        //@UICallout
        public void SetOrig_Order_ID(String oldOrig_Order_ID, String newOrig_Order_ID, int windowNo)
        {
            try
            {
                if (newOrig_Order_ID == null || newOrig_Order_ID.Length == 0)
                    return;
                int Orig_Order_ID = int.Parse(newOrig_Order_ID);
                if (Orig_Order_ID == 0)
                {
                    return;
                }

                //		Get Details
                MVABOrder origOrder = new MVABOrder(GetCtx(), Orig_Order_ID, null);
                if (origOrder.Get_ID() != 0)
                {
                    SetOrigOrder(origOrder);
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetOrig_Order_ID-callout");
            }

        }

        /*	Set Original Shipment for RMA
        * 	SOTrx should be set.
        * 	@param origInOut MVAMInvInOut
        */
        public void SetOrigInOut(MVAMInvInOut origInOut)
        {
            try
            {
                if (origInOut == null || origInOut.Get_ID() == 0)
                {
                    return;
                }
                SetOrig_InOut_ID(origInOut.GetVAM_Inv_InOut_ID());
                SetVAB_Project_ID(origInOut.GetVAB_Project_ID());
                SetVAB_Promotion_ID(origInOut.GetVAB_Promotion_ID());
                SetVAB_BillingCode_ID(origInOut.GetVAB_BillingCode_ID());
                SetVAF_OrgTrx_ID(origInOut.GetVAF_OrgTrx_ID());
                SetUser1_ID(origInOut.GetUser1_ID());
                SetUser2_ID(origInOut.GetUser2_ID());
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetOrigInOut");
            }

            return;

        }

        /*	Set Original Shipment - Callout
        *	@param oldOrig_InOut_ID old Orig Order
        *	@param newOrig_InOut_ID new Orig Order
        *	@param windowNo window no
        */
        //@UICallout
        public void SetOrig_InOut_ID(String oldOrig_InOut_ID, String newOrig_InOut_ID, int windowNo)
        {
            try
            {
                if (newOrig_InOut_ID == null || newOrig_InOut_ID.Length == 0)
                    return;
                int Orig_InOut_ID = int.Parse(newOrig_InOut_ID);
                if (Orig_InOut_ID == 0)
                    return;
                //		Get Details
                MVAMInvInOut origInOut = new MVAMInvInOut(GetCtx(), Orig_InOut_ID, null);
                if (origInOut.Get_ID() != 0)
                    SetOrigInOut(origInOut);
            }
            catch
            {

                //ShowMessage.Error("MOrder", null, "SetOrig_InOut_ID");
            }

        }

        /// <summary>
        /// Get Lines of Order
        /// </summary>
        /// <param name="whereClause">where clause or null (starting with AND)</param>
        /// <param name="orderClause">order clause</param>
        /// <returns>lines</returns>
        public MVABOrderLine[] GetLines(String whereClause, String orderClause)
        {
            List<MVABOrderLine> list = new List<MVABOrderLine>();
            StringBuilder sql = new StringBuilder("SELECT * FROM VAB_OrderLine WHERE VAB_Order_ID=" + GetVAB_Order_ID() + "");
            if (whereClause != null)
                sql.Append(whereClause);
            if (orderClause != null)
                sql.Append(" ").Append(orderClause);
            try
            {
                DataSet ds = DataBase.DB.ExecuteDataset(sql.ToString(), null, Get_TrxName());
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        MVABOrderLine ol = new MVABOrderLine(GetCtx(), dr, Get_TrxName());
                        ol.SetHeaderInfo(this);
                        //JID_1673 Quantity entered should not be zero
                        if ((Utility.Util.GetValueOfDecimal(dr["QtyEntered"])) > 0)
                            list.Add(ol);
                    }
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql.ToString(), e);
            }
            //
            MVABOrderLine[] lines = new MVABOrderLine[list.Count];
            lines = list.ToArray();
            return lines;
        }

        /// <summary>
        /// Get Lines of Order
        /// </summary>
        /// <param name="requery">requery</param>
        /// <param name="orderBy">optional order by column</param>
        /// <returns>lines</returns>
        public MVABOrderLine[] GetLines(bool requery, String orderBy)
        {
            try
            {
                if (_lines != null && !requery)
                {
                    return _lines;
                }
                //
                String orderClause = "ORDER BY ";
                if (orderBy != null && orderBy.Length > 0)
                {
                    orderClause += orderBy;
                }
                else
                {
                    orderClause += "Line";
                }
                _lines = GetLines(null, orderClause);

            }
            catch
            {

                //ShowMessage.Error("MOrder", null, "GetLines");
            }
            return _lines;
        }

        /// <summary>
        /// Get Lines of Order.
        /// </summary>
        /// <returns>lines</returns>
        public MVABOrderLine[] GetLines()
        {
            return GetLines(false, null);
        }

        /// <summary>
        /// Get Lines of Order for a given product
        /// </summary>
        /// <param name="VAM_Product_ID"></param>
        /// <param name="whereClause"></param>
        /// <param name="orderClause">order clause</param>
        /// <returns>lines</returns>
        /// <date>10-March-2011</date>
        /// <writer>raghu</writer>
        public MVABOrderLine[] GetLines(int VAM_Product_ID, String whereClause, String orderClause)
        {
            List<MVABOrderLine> list = new List<MVABOrderLine>();
            StringBuilder sql = new StringBuilder("SELECT * FROM VAB_OrderLine WHERE VAB_Order_ID=" + GetVAB_Order_ID() + " AND VAM_Product_ID=" + VAM_Product_ID);

            if (whereClause != null)
                sql.Append(" AND ").Append(whereClause);

            if (orderClause != null)
                sql.Append(" ORDER BY ").Append(orderClause);

            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql.ToString(), null, Get_TrxName());
                DataTable dt = new DataTable();
                dt.Load(idr);
                idr.Close();

                foreach (DataRow dr in dt.Rows)
                {
                    MVABOrderLine ol = new MVABOrderLine(GetCtx(), dr, Get_TrxName());
                    ol.SetHeaderInfo(this);
                    list.Add(ol);
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql.ToString(), e);
            }
            finally
            {
                if (idr != null)
                {

                    idr.Close();
                    idr = null;
                }
            }
            //
            MVABOrderLine[] lines = new MVABOrderLine[list.Count]; ;
            lines = list.ToArray();
            return lines;
        }

        /// <summary>
        /// Get Lines of Order
        /// </summary>
        /// <param name="orderBy">optional order by column</param>
        /// <returns>lines</returns>
        public MVABOrderLine[] GetLines(String orderBy)
        {
            String orderClause = "ORDER BY ";
            if ((orderBy != null) && (orderBy.Length > 0))
            {
                orderClause += orderBy;
            }
            else
            {
                orderClause += "Line";
            }
            return GetLines(null, orderClause);
        }

        /// <summary>
        /// Is Used to get all orderline except those where (Product is of ITEM type)
        /// </summary>
        /// <returns>lines</returns>
        /// <writer>Amit</writer>
        public MVABOrderLine[] GetLinesOtherthanProduct()
        {
            List<MVABOrderLine> list = new List<MVABOrderLine>();
            StringBuilder sql = new StringBuilder(@"SELECT * FROM VAB_OrderLine ol
                                                        LEFT JOIN VAM_Product p ON p.VAM_Product_id = ol.VAM_Product_id
                                                        WHERE ol.VAB_Order_ID =" + GetVAB_Order_ID() + @" AND ol.isactive = 'Y' 
                                                        AND (ol.VAM_Product_ID IS NULL OR p.ProductType     != 'I')");
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql.ToString(), null, Get_TrxName());
                DataTable dt = new DataTable();
                dt.Load(idr);
                idr.Close();

                foreach (DataRow dr in dt.Rows)
                {
                    MVABOrderLine ol = new MVABOrderLine(GetCtx(), dr, Get_TrxName());
                    ol.SetHeaderInfo(this);
                    list.Add(ol);
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql.ToString(), e);
            }
            finally
            {
                if (idr != null)
                {

                    idr.Close();
                    idr = null;
                }
            }
            //
            MVABOrderLine[] lines = new MVABOrderLine[list.Count]; ;
            lines = list.ToArray();
            return lines;
        }

        /*	Renumber Lines
        *	@param step start and step
        */
        public void RenumberLines(int step)
        {
            int number = step;
            MVABOrderLine[] lines = GetLines(true, null);	//	Line is default
            for (int i = 0; i < lines.Length; i++)
            {
                MVABOrderLine line = lines[i];
                line.SetLine(number);
                line.Save(Get_TrxName());
                number += step;
            }
            _lines = null;
        }

        /* 	Does the Order Line belong to this Order
         *	@param VAB_OrderLine_ID line
         *	@return true if part of the order
         */
        public bool IsOrderLine(int VAB_OrderLine_ID)
        {
            if (_lines == null)
                GetLines();
            for (int i = 0; i < _lines.Length; i++)
                if (_lines[i].GetVAB_OrderLine_ID() == VAB_OrderLine_ID)
                    return true;
            return false;
        }

        /* 	Get Taxes of Order
         *	@param requery requery
         *	@return array of taxes
         */
        public MVABOrderTax[] GetTaxes(bool requery)
        {
            if (_taxes != null && !requery)
                return _taxes;
            //
            List<MVABOrderTax> list = new List<MVABOrderTax>();
            String sql = "SELECT * FROM VAB_OrderTax WHERE VAB_Order_ID=" + GetVAB_Order_ID();
            DataTable dt = null;
            try
            {
                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);

                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVABOrderTax(GetCtx(), dr, Get_TrxName()));
                }
                dt = null;
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;
            }
            _taxes = new MVABOrderTax[list.Count];
            _taxes = list.ToArray();
            return _taxes;
        }

        /*	Get Invoices of Order
        * 	@param hearderLinkOnly shipments based on header only
        * 	@return invoices
        */
        public MVABInvoice[] GetInvoices(bool hearderLinkOnly)
        {
            //	TODO get invoiced which are linked on line level
            List<MVABInvoice> list = new List<MVABInvoice>();
            String sql = "SELECT * FROM VAB_Invoice WHERE VAB_Order_ID=" + GetVAB_Order_ID() + " ORDER BY Created DESC";
            DataTable dt = null;
            try
            {
                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVABInvoice(GetCtx(), dr, Get_TrxName()));
                }
                dt = null;
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            finally { dt = null; }

            MVABInvoice[] retValue = new MVABInvoice[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /*	Get latest Invoice of Order
        * 	@return invoice id or 0
        */
        public int GetVAB_Invoice_ID()
        {
            int VAB_Invoice_ID = 0;
            String sql = "SELECT VAB_Invoice_ID FROM VAB_Invoice "
                + "WHERE VAB_Order_ID=" + GetVAB_Order_ID() + " AND DocStatus IN ('CO','CL') "
                + "ORDER BY Created DESC";
            DataTable dt = null;
            try
            {
                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    //VAB_Invoice_ID =Convert.ToInt32(dr[0]);
                    VAB_Invoice_ID = Utility.Util.GetValueOfInt(dr[0].ToString());
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "getVAB_Invoice_ID", e);
            }
            finally { dt = null; }
            return VAB_Invoice_ID;
        }

        /* 	Get Shipments of Order
         * 	@param hearderLinkOnly shipments based on header only
         * 	@return shipments
         */
        public MVAMInvInOut[] GetShipments(bool hearderLinkOnly)
        {
            //	TODO: getShipment if linked on line
            List<MVAMInvInOut> list = new List<MVAMInvInOut>();
            String sql = "SELECT * FROM VAM_Inv_InOut WHERE VAB_Order_ID=" + GetVAB_Order_ID() + " ORDER BY Created DESC";
            DataTable dt = null;
            try
            {
                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVAMInvInOut(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            finally { dt = null; }

            MVAMInvInOut[] retValue = new MVAMInvInOut[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /*	Get RMAs of Order
        * 	@return RMAs
        */
        public MVABOrder[] GetRMAs()
        {
            List<MVABOrder> list = new List<MVABOrder>();
            String sql = "SELECT * FROM VAB_Order WHERE Orig_Order_ID=" + GetVAB_Order_ID() + " ORDER BY Created DESC";
            DataTable dt = null;
            try
            {
                IDataReader idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVABOrder(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            finally { dt = null; }


            MVABOrder[] retValue = new MVABOrder[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /*	Get Shipment Lines of Order
        * 	@return shipments newest first
        */
        public MVAMInvInOutLine[] GetShipmentLines()
        {
            List<MVAMInvInOutLine> list = new List<MVAMInvInOutLine>();
            String sql = "SELECT * FROM VAM_Inv_InOutLine iol "
                + "WHERE iol.VAB_OrderLine_ID IN "
                    + "(SELECT VAB_OrderLine_ID FROM VAB_OrderLine WHERE VAB_Order_ID=@VAB_Order_ID) "
                + "ORDER BY VAM_Inv_InOutLine_ID";
            DataTable dt = null;
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@VAB_Order_ID", GetVAB_Order_ID());
                IDataReader idr = DataBase.DB.ExecuteReader(sql, param, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVAMInvInOutLine(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch
            {

                //ShowMessage.Error("MOrder", null, "GetShipmentLines");
            }
            finally { dt = null; }

            MVAMInvInOutLine[] retValue = new MVAMInvInOutLine[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /*	Get ISO Code of Currency
        *	@return Currency ISO
        */
        public String GetCurrencyISO()
        {
            return MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID());
        }

        /// <summary>
        /// Get Currency Precision
        /// </summary>
        /// <returns>precision</returns>
        public int GetPrecision()
        {
            return MVABCurrency.GetStdPrecision(GetCtx(), GetVAB_Currency_ID());
        }

        /*	Get Document Status
        *	@return Document Status Clear Text
        */
        public String GetDocStatusName()
        {
            return MVAFCtrlRefList.GetListName(GetCtx(), 131, GetDocStatus());
        }

        /// <summary>
        /// Set DocAction
        /// </summary>
        /// <param name="docAction">doc action</param>
        public new void SetDocAction(String docAction)
        {
            SetDocAction(docAction, false);
        }

        /// <summary>
        /// Set DocAction
        /// </summary>
        /// <param name="docAction">doc action</param>
        /// <param name="forceCreation">force creation</param>
        public void SetDocAction(String docAction, bool forceCreation)
        {
            base.SetDocAction(docAction);
            _forceCreation = forceCreation;
        }

        /*	Set Processed.
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
                + "' WHERE VAB_Order_ID=" + GetVAB_Order_ID();
            int noLine = DataBase.DB.ExecuteQuery("UPDATE VAB_OrderLine " + set, null, Get_TrxName());
            int noTax = DataBase.DB.ExecuteQuery("UPDATE VAB_OrderTax " + set, null, Get_TrxName());
            _lines = null;
            _taxes = null;
            log.Fine(processed + " - Lines=" + noLine + ", Tax=" + noTax);
        }

        /* 	Before Save
        *	@param newRecord new
        *	@return save
        */
        protected override bool BeforeSave(bool newRecord)
        {
            try
            {
                //	Client/Org Check
                if (GetVAF_Org_ID() == 0)
                {
                    int context_VAF_Org_ID = GetCtx().GetVAF_Org_ID();
                    if (context_VAF_Org_ID != 0)
                    {
                        SetVAF_Org_ID(context_VAF_Org_ID);
                        log.Warning("Changed Org to Ctx=" + context_VAF_Org_ID);
                    }
                }
                if (GetVAF_Client_ID() == 0)
                {
                    _processMsg = "VAF_Client_ID = 0";
                    return false;
                }

                //	New Record Doc Type - make sure DocType set to 0
                if (newRecord && GetVAB_DocTypes_ID() == 0)
                    SetVAB_DocTypes_ID(0);

                //	Default Warehouse
                if (GetVAM_Warehouse_ID() == 0)
                {
                    MVAFOrg org = MVAFOrg.Get(GetCtx(), GetVAF_Org_ID());
                    SetVAM_Warehouse_ID(org.GetVAM_Warehouse_ID());
                }

                //	Warehouse Org
                MVAMWarehouse wh = null;
                if (newRecord
                    || Is_ValueChanged("VAF_Org_ID") || Is_ValueChanged("VAM_Warehouse_ID"))
                {
                    wh = MVAMWarehouse.Get(GetCtx(), GetVAM_Warehouse_ID());
                    if (wh.GetVAF_Org_ID() != GetVAF_Org_ID())
                    {
                        //Arpit 20th Nov,2017 issue No.115 -Not to save record if WareHouse is conflictiong with Organization
                        log.SaveWarning("WarehouseOrgConflict", "");
                        return false;
                    }
                }

                // JID_1366 : in case of disallow : True on warhouse, and when user try to save SO with shipping Rule as Force,
                // then system will save record with Availability as shipping Rule and give message to user.
                if (GetDeliveryRule() == X_VAB_Order.DELIVERYRULE_Force)
                {
                    if (wh == null)
                    {
                        wh = MVAMWarehouse.Get(GetCtx(), GetVAM_Warehouse_ID());
                    }
                    if (wh.IsDisallowNegativeInv())
                    {
                        SetDeliveryRule(X_VAB_Order.DELIVERYRULE_Availability);
                        log.Info("JID_1366 : in case of disallow : True on warhouse, and user try to save Order with shipping Rule as Force, then system will save record with Availability.");
                    }
                }

                //	Reservations in Warehouse
                if (!newRecord && Is_ValueChanged("VAM_Warehouse_ID"))
                {
                    MVABOrderLine[] lines = GetLines(false, null);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (!lines[i].CanChangeWarehouse())		// saves Error	
                            return false;
                    }
                }

                // If lines are available and user is changing the pricelist, Order or Ship/Receipt on header than we have to restrict it because
                // JID_0399_1: After change the receipt or order system will give the error message
                if (!newRecord && (Is_ValueChanged("VAM_PriceList_ID") || Is_ValueChanged("Orig_Order_ID") || Is_ValueChanged("Orig_InOut_ID")))
                {
                    MVABOrderLine[] lines = GetLines(false, null);
                    if (lines.Length > 0)
                    {
                        log.SaveWarning("pleaseDeleteLinesFirst", "");
                        return false;
                    }
                }
                //End

                //	No Partner Info - set Template
                if (GetVAB_BusinessPartner_ID() == 0)
                    SetBPartner(MVABBusinessPartner.GetTemplate(GetCtx(), GetVAF_Client_ID()));
                if (GetVAB_BPart_Location_ID() == 0)
                    SetBPartner(MVABBusinessPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID()));
                //	No Bill - get from Ship
                if (GetBill_BPartner_ID() == 0)
                {
                    SetBill_BPartner_ID(GetVAB_BusinessPartner_ID());
                    SetBill_Location_ID(GetVAB_BPart_Location_ID());
                }
                if (GetBill_Location_ID() == 0)
                    SetBill_Location_ID(GetVAB_BPart_Location_ID());

                //	BP Active check
                if (newRecord || Is_ValueChanged("VAB_BusinessPartner_ID"))
                {
                    MVABBusinessPartner bp = MVABBusinessPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID());
                    if (!bp.IsActive())
                    {
                        log.SaveError("NotActive", Msg.GetMsg(GetCtx(), "VAB_BusinessPartner_ID"));
                        return false;
                    }
                }
                if ((newRecord || Is_ValueChanged("Bill_BPartner_ID"))
                        && GetBill_BPartner_ID() != GetVAB_BusinessPartner_ID())
                {
                    MVABBusinessPartner bp = MVABBusinessPartner.Get(GetCtx(), GetBill_BPartner_ID());
                    if (!bp.IsActive())
                    {
                        log.SaveError("NotActive", Msg.GetMsg(GetCtx(), "Bill_BPartner_ID"));
                        return false;
                    }
                }

                //	Default Price List
                if (GetVAM_PriceList_ID() == 0)
                {
                    string test = IsSOTrx() ? "Y" : "N";
                    int ii = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar("SELECT VAM_PriceList_ID FROM VAM_PriceList "
                        + "WHERE VAF_Client_ID=" + GetVAF_Client_ID() + " AND IsSOPriceList='" + test
                        + "' ORDER BY IsDefault DESC", null, null));
                    if (ii != 0)
                        SetVAM_PriceList_ID(ii);
                }
                //	Default Currency
                if (GetVAB_Currency_ID() == 0)
                {
                    String sql = "SELECT VAB_Currency_ID FROM VAM_PriceList WHERE VAM_PriceList_ID=" + GetVAM_PriceList_ID();
                    int ii = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, null));
                    if (ii != 0)
                        SetVAB_Currency_ID(ii);
                    else
                        SetVAB_Currency_ID(GetCtx().GetContextAsInt("#VAB_Currency_ID"));
                }

                //	Default Sales Rep
                if (GetSalesRep_ID() == 0)
                {
                    int ii = GetCtx().GetContextAsInt("#SalesRep_ID");
                    if (ii != 0)
                        SetSalesRep_ID(ii);
                }

                //	Default Document Type
                if (GetVAB_DocTypesTarget_ID() == 0)
                    SetVAB_DocTypesTarget_ID(DocSubTypeSO_Standard);

                //	Default Payment Term
                if (GetVAB_PaymentTerm_ID() == 0)
                {
                    int ii = GetCtx().GetContextAsInt("#VAB_PaymentTerm_ID");
                    if (ii != 0)
                        SetVAB_PaymentTerm_ID(ii);
                    else
                    {
                        String sql = "SELECT VAB_PaymentTerm_ID FROM VAB_PaymentTerm WHERE VAF_Client_ID=" + GetVAF_Client_ID() + " AND IsDefault='Y'";
                        ii = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, null));
                        if (ii != 0)
                            SetVAB_PaymentTerm_ID(ii);
                    }
                }

                //Do not save if "valid to" date is less than "valid from" date in case of blanket order.  By SUkhwinder on 31/07/2017
                MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypesTarget_ID());
                if (dt.GetDocBaseType() == "BOO") ///dt.GetValue() == "BSO" || dt.GetValue() == "BPO")
                {
                    if (GetOrderValidFrom() != null && GetOrderValidTo() != null)
                    {
                        if (GetOrderValidFrom().Value.Date > GetOrderValidTo().Value.Date)
                        {
                            log.SaveError(Msg.Translate(GetCtx(), "VIS_ValidFromDateGrtrThanValidToDate"), "");
                            return false;
                        }
                    }
                }

                //JID_0211: System is allowing to save promised date smaller than order date on header as wll as on order lines.. There should be a validation.
                if (!IsReturnTrx() && GetDateOrdered() != null && GetDatePromised() != null)
                {
                    if (GetDateOrdered().Value.Date > GetDatePromised().Value.Date)
                    {
                        log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_OrderDateGrtrThanPromisedDate"));
                        return false;
                    }
                }

                //SI_0648_2 : payment rule and payment method should be same as on payment base type of payment method window
                if (Env.IsModuleInstalled("VA009_") && GetVA009_PaymentMethod_ID() > 0)
                {
                    string paymentRule = Util.GetValueOfString(DB.ExecuteScalar(@"SELECT VA009_PAYMENTBASETYPE FROM VA009_PAYMENTMETHOD 
                                                                                   WHERE VA009_PAYMENTMETHOD_ID=" + GetVA009_PaymentMethod_ID(), null, Get_Trx()));
                    if (!String.IsNullOrEmpty(paymentRule))
                    {
                        SetPaymentMethod(paymentRule);
                        SetPaymentRule(paymentRule);
                    }
                }

                //Added by Bharat for Credit Limit on 24/08/2016
                //if (IsSOTrx())
                //{
                //    MBPartner bp = MBPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID());
                //    if (bp.GetCreditStatusSettingOn() == "CH")
                //    {
                //        decimal creditLimit = bp.GetSO_CreditLimit();
                //        string creditVal = bp.GetCreditValidation();
                //        if (creditLimit != 0)
                //        {
                //            decimal creditAvlb = creditLimit - bp.GetSO_CreditUsed();
                //            if (creditAvlb <= 0)
                //            {
                //                //if (creditVal == "A" || creditVal == "D" || creditVal == "E")
                //                //{
                //                //    log.SaveError("Error", Msg.GetMsg(GetCtx(), "CreditUsedSalesOrder"));
                //                //    return false;
                //                //}
                //                //else if (creditVal == "G" || creditVal == "J" || creditVal == "K")
                //                //{
                //                    log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "CreditOver"));
                //                //}
                //            }
                //        }
                //    }
                //    // JID_0161 // change here now will check credit settings on field only on Business Partner Header // Lokesh Chauhan 15 July 2019
                //    else if (bp.GetCreditStatusSettingOn() == X_VAB_BusinessPartner.CREDITSTATUSSETTINGON_CustomerLocation)
                //    {
                //        MBPartnerLocation bpl = new MBPartnerLocation(GetCtx(), GetVAB_BPart_Location_ID(), null);
                //        //if (bpl.GetCreditStatusSettingOn() == "CL")
                //        //{
                //            decimal creditLimit = bpl.GetSO_CreditLimit();
                //            string creditVal = bpl.GetCreditValidation();
                //            if (creditLimit != 0)
                //            {
                //                decimal creditAvlb = creditLimit - bpl.GetSO_CreditUsed();
                //                if (creditAvlb <= 0)
                //                {
                //                    //if (creditVal == "A" || creditVal == "D" || creditVal == "E")
                //                    //{
                //                    //    log.SaveError("Error", Msg.GetMsg(GetCtx(), "CreditUsedSalesOrder"));
                //                    //    return false;
                //                    //}
                //                    //else if (creditVal == "G" || creditVal == "J" || creditVal == "K")
                //                    //{
                //                        //log.Warning(Msg.GetMsg(GetCtx(), "CreditOver"));
                //                        log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "CreditOver"));
                //                    //}
                //                }
                //            }
                //        //}
                //    }
                //}

                if (IsReturnTrx())
                {
                    bool withinPolicy = true;

                    if (GetVAM_ReturnRule_ID() == 0)
                        SetVAM_ReturnRule_ID();

                    if (GetVAM_ReturnRule_ID() != 0)
                    {
                        MVAMInvInOut origInOut = new MVAMInvInOut(GetCtx(), GetOrig_InOut_ID(), null);
                        MVAMReturnRule rpolicy = new MVAMReturnRule(GetCtx(), GetVAM_ReturnRule_ID(), null);
                        log.Fine("RMA Date : " + GetDateOrdered() + " Shipment Date : " + origInOut.GetMovementDate());
                        withinPolicy = rpolicy.CheckReturnPolicy(origInOut.GetMovementDate(), GetDateOrdered());
                    }
                    else
                        withinPolicy = false;

                    if (!withinPolicy)
                    {
                        if (!MVAFRole.GetDefault(GetCtx()).IsOverrideReturnPolicy())
                        {
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "ReturnPolicyExceeded"));
                            return false;
                        }
                        else
                        {
                            log.SaveWarning("Warning", "ReturnPolicyExceeded");
                        }
                    }
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "Before Save");
                return false;
            }
            return true;
        }

        /* 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return true if can be saved
         */
        protected override bool AfterSave(bool newRecord, bool success)
        {
            try
            {
                //if (!success || newRecord)
                if (!success)
                    return success;

                if (!newRecord)
                {
                    //	Propagate Description changes
                    if (Is_ValueChanged("Description") || Is_ValueChanged("POReference"))
                    {
                        String sql = "UPDATE VAB_Invoice i"
                            + " SET (Description,POReference)="
                                + "(SELECT Description, POReference "
                                + "FROM VAB_Order o WHERE i.VAB_Order_ID=o.VAB_Order_ID) "
                            + "WHERE DocStatus NOT IN ('RE','CL') AND VAB_Order_ID=" + GetVAB_Order_ID();

                        int no = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, Get_TrxName()));
                        log.Fine("Description -> #" + no);
                    }

                    //	Propagate Changes of Payment Info to existing (not reversed/closed) invoices
                    if (Is_ValueChanged("PaymentRule") || Is_ValueChanged("VAB_PaymentTerm_ID")
                        || Is_ValueChanged("DateAcct") || Is_ValueChanged("VAB_Payment_ID")
                        || Is_ValueChanged("VAB_CashJRNLLine_ID"))
                    {
                        String sql = "UPDATE VAB_Invoice i "
                            + "SET (PaymentRule,VAB_PaymentTerm_ID,DateAcct,VAB_Payment_ID,VAB_CashJRNLLine_ID)="
                                + "(SELECT PaymentRule,VAB_PaymentTerm_ID,DateAcct,VAB_Payment_ID,VAB_CashJRNLLine_ID "
                                + "FROM VAB_Order o WHERE i.VAB_Order_ID=o.VAB_Order_ID)"
                            + "WHERE DocStatus NOT IN ('RE','CL') AND VAB_Order_ID=" + GetVAB_Order_ID();
                        //	Don't touch Closed/Reversed entries
                        int no = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, Get_TrxName()));
                        log.Fine("Payment -> #" + no);
                    }

                    //	Sync Lines
                    AfterSaveSync("VAF_Org_ID");
                    AfterSaveSync("VAB_BusinessPartner_ID");
                    AfterSaveSync("VAB_BPart_Location_ID");
                    AfterSaveSync("DateOrdered");
                    AfterSaveSync("DatePromised");
                    AfterSaveSync("VAM_Warehouse_ID");
                    AfterSaveSync("VAM_ShippingMethod_ID");
                    AfterSaveSync("VAB_Currency_ID");
                }

                // Applied check for warning message on credit limit for Business Partner
                if ((IsSOTrx() && !IsReturnTrx()) || (!IsSOTrx() && IsReturnTrx()))
                {
                    string docSubType = Util.GetValueOfString(DB.ExecuteScalar(@"SELECT DocSubtypeSO FROM VAB_DocTypes WHERE 
                                                VAB_DocTypes_ID = " + GetVAB_DocTypesTarget_ID() + " AND DocBaseType = 'SOO'", null, Get_TrxName()));
                    if (!(docSubType == "ON" || docSubType == "OB"))
                    {
                        Decimal grandTotal = MVABExchangeRate.ConvertBase(GetCtx(),
                            GetGrandTotal(), GetVAB_Currency_ID(), GetDateOrdered(),
                            GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());

                        MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());
                        string retMsg = "";
                        bool crdAll = bp.IsCreditAllowed(GetVAB_BPart_Location_ID(), grandTotal, out retMsg);
                        if (!crdAll)
                            log.SaveWarning("Warning", retMsg);
                        else if (bp.IsCreditWatch(GetVAB_BPart_Location_ID()))
                        {
                            log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "VIS_BPCreditWatch"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Severe(ex.ToString());
                //MessageBox.Show("Error in MOrder--AfterSave");
                return false;
            }
            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        private void AfterSaveSync(String columnName)
        {
            if (Is_ValueChanged(columnName))
            {
                String sql = "UPDATE VAB_OrderLine ol"
                    + " SET " + columnName + " ="
                        + "(SELECT " + columnName
                        + " FROM VAB_Order o WHERE ol.VAB_Order_ID=o.VAB_Order_ID) "
                    + "WHERE VAB_Order_ID=" + GetVAB_Order_ID();
                int no = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, Get_TrxName()));
                log.Fine(columnName + " Lines -> #" + no);
            }
        }

        /// <summary>
        /// when doc type = Warehouse Order / Credit Order / POS Order / Prepay order --- and payment term is advance -- then system return false
        /// </summary>
        /// <param name="documnetType_Id"></param>
        /// <param name="PaymentTerm_Id"></param>
        /// <returns></returns>
        public bool checkAdvancePaymentTerm(int documnetType_Id, int PaymentTerm_Id)
        {
            bool isAdvancePayTerm = true;

            // when document type is not --  Warehouse Order / Credit Order / POS Order / Prepay order , then true
            // Payment term can't be advance for Customer RMA / Vendor RMA
            MVABDocTypes doctype = MVABDocTypes.Get(GetCtx(), documnetType_Id);
            if (!(doctype.GetDocSubTypeSO() == X_VAB_DocTypes.DOCSUBTYPESO_PrepayOrder ||
                doctype.GetDocSubTypeSO() == X_VAB_DocTypes.DOCSUBTYPESO_OnCreditOrder ||
                doctype.GetDocSubTypeSO() == X_VAB_DocTypes.DOCSUBTYPESO_WarehouseOrder ||
                doctype.GetDocSubTypeSO() == X_VAB_DocTypes.DOCSUBTYPESO_POSOrder ||
                (doctype.GetDocSubTypeSO() == X_VAB_DocTypes.DOCSUBTYPESO_StandardOrder && doctype.IsReturnTrx()) ||
                (doctype.GetDocBaseType() == "POO" && doctype.IsReturnTrx())))
            {
                isAdvancePayTerm = true;
            }
            // check payment term is Advance, then return False
            else if (Util.GetValueOfString(DB.ExecuteScalar(@"SELECT VA009_Advance FROM VAB_PaymentTerm
                                            WHERE VAB_PaymentTerm_ID = " + PaymentTerm_Id, null, Get_TrxName())).Equals("Y"))
            {
                isAdvancePayTerm = false;
            }
            // check any payment term schedule is Advance, then return False
            // JID_1193: If Payment term header is valid but having lines with advance and Inactive. System should consider that as 100% immedate. However, system is creating schedule of advance on order.
            else if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM VAB_PaymentSchedule WHERE IsActive = 'Y' AND IsValid = 'Y' 
                                                            AND VA009_Advance = 'Y' AND VAB_PaymentTerm_ID = " + PaymentTerm_Id, null, Get_TrxName())) > 0)
            {
                isAdvancePayTerm = false;
            }

            return isAdvancePayTerm;
        }


        /* 	Before Delete
         *	@return true of it can be deleted
         */
        protected override bool BeforeDelete()
        {
            try
            {
                if (IsProcessed())
                    return false;

                GetLines();
                for (int i = 0; i < _lines.Length; i++)
                {
                    if (!_lines[i].DeleteCheck())
                    {
                        return false;
                    }
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "BeforeDelete");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Process document
        /// </summary>
        /// <param name="processAction">document action</param>
        /// <returns>true if performed</returns>
        public bool ProcessIt(String processAction)
        {
            _processMsg = null;
            DocumentEngine engine = new DocumentEngine(this, GetDocStatus());
            return engine.ProcessIt(processAction, GetDocAction());
        }

        /// <summary>
        /// Unlock Document.
        /// </summary>
        /// <returns>true if success</returns>
        public bool UnlockIt()
        {
            log.Info("unlockIt - " + ToString());
            SetProcessing(false);
            return true;
        }

        /// <summary>
        /// Invalidate Document
        /// </summary>
        /// <returns>true if success</returns>
        public bool InvalidateIt()
        {
            log.Info(ToString());
            SetDocAction(DOCACTION_Prepare);
            return true;
        }

        /// <summary>
        /// Prepare Document
        /// </summary>
        /// <returns>new status (In Progress or Invalid)</returns>
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
            MVABOrderLine[] lines = GetLines(true, "VAM_Product_ID");
            if (lines.Length == 0)
            {
                _processMsg = "@NoLines@";
                return DocActionVariables.STATUS_INVALID;
            }

            //	Convert DocType to Target
            if (GetVAB_DocTypes_ID() != GetVAB_DocTypesTarget_ID())
            {
                //	Cannot change Std to anything else if different warehouses
                if (GetVAB_DocTypes_ID() != 0)
                {
                    MVABDocTypes dtOld = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
                    if (MVABDocTypes.DOCSUBTYPESO_StandardOrder.Equals(dtOld.GetDocSubTypeSO())		//	From SO
                        && !MVABDocTypes.DOCSUBTYPESO_StandardOrder.Equals(dt.GetDocSubTypeSO()))	//	To !SO
                    {
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].GetVAM_Warehouse_ID() != GetVAM_Warehouse_ID())
                            {
                                log.Warning("different Warehouse " + lines[i]);
                                _processMsg = "@CannotChangeDocType@";
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                }

                //	New or in Progress/Invalid
                if (DOCSTATUS_Drafted.Equals(GetDocStatus())
                    || DOCSTATUS_InProgress.Equals(GetDocStatus())
                    || DOCSTATUS_Invalid.Equals(GetDocStatus())
                    || GetVAB_DocTypes_ID() == 0)
                {
                    SetVAB_DocTypes_ID(GetVAB_DocTypesTarget_ID());
                }
                else	//	convert only if offer
                {
                    if (dt.IsOffer())
                        SetVAB_DocTypes_ID(GetVAB_DocTypesTarget_ID());
                    else
                    {
                        _processMsg = "@CannotChangeDocType@";
                        return DocActionVariables.STATUS_INVALID;
                    }
                }
            }	//	convert DocType

            //	Mandatory Product Attribute Set Instance
            String mandatoryType = "='Y'";	//	IN ('Y','S')
            String sql = "SELECT COUNT(*) "
                + "FROM VAB_OrderLine ol"
                + " INNER JOIN VAM_Product p ON (ol.VAM_Product_ID=p.VAM_Product_ID)"
                + " INNER JOIN VAM_PFeature_Set pas ON (p.VAM_PFeature_Set_ID=pas.VAM_PFeature_Set_ID) "
                + "WHERE pas.MandatoryType" + mandatoryType
                + " AND ol.VAM_PFeature_SetInstance_ID IS NULL"
                + " AND ol.VAB_Order_ID=" + GetVAB_Order_ID();
            int no = DataBase.DB.GetSQLValue(Get_TrxName(), sql);
            if (no != 0)
            {
                _processMsg = "@LinesWithoutProductAttribute@ (" + no + ")";
                return DocActionVariables.STATUS_INVALID;
            }

            // stop completing of RMA when original order and shipment/receipt are not completed or closed            
            if (IsReturnTrx())   // Added by Vivek on 20/01/2018 assigned by Mukesh sir
            {
                MVABOrder OrigOrder = new MVABOrder(GetCtx(), GetOrig_Order_ID(), Get_Trx());
                MVAMInvInOut OrigInout = new MVAMInvInOut(GetCtx(), GetOrig_InOut_ID(), Get_Trx());
                if (OrigInout.GetDocStatus() == "RE" || OrigInout.GetDocStatus() == "VO")
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "Order/ShipmentNotCompleted");
                    return DocActionVariables.STATUS_INVALID;
                }
                if (OrigOrder.GetDocStatus() == "RE" || OrigOrder.GetDocStatus() == "VO")
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "Order/ShipmentNotCompleted");
                    return DocActionVariables.STATUS_INVALID;
                }

            }




            //	Lines
            if (ExplodeBOM())
                lines = GetLines(true, "VAM_Product_ID");


            MVABDocTypes docType = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            //check Payment term is valid or Not (SI_0018)
            if (Util.GetValueOfString(DB.ExecuteScalar("SELECT IsValid FROM VAB_PaymentTerm WHERE VAB_PaymentTerm_ID = " + GetVAB_PaymentTerm_ID())) == "N")
            {
                _processMsg = Msg.GetMsg(GetCtx(), "VIS_PaymentTermIsInValid");
                return DocActionVariables.STATUS_INVALID;
            }

            // SI_0646_1 : when doc type = Warehouse Order / Credit Order / POS Order / Prepay order --- and payment term is advance -- then system return false
            if (Env.IsModuleInstalled("VA009_") && !checkAdvancePaymentTerm(GetVAB_DocTypesTarget_ID(), GetVAB_PaymentTerm_ID()))
            {
                _processMsg = Msg.GetMsg(GetCtx(), "VIS_NotToBeAdvance");
                return DocActionVariables.STATUS_INVALID;
            }

            if (!ReserveStock(dt, lines))
            {
                _processMsg = "Cannot reserve Stock";
                return DocActionVariables.STATUS_INVALID;
            }

            if (!CalculateTaxTotal())
            {
                _processMsg = "Error calculating tax";
                return DocActionVariables.STATUS_INVALID;
            }

            // Changed check to handle Vendor RMA Cases also
            if ((IsSOTrx() && !IsReturnTrx()) || (!IsSOTrx() && IsReturnTrx()))
            {
                // added by Bharat to avoid completion if Payment Method is not selected
                // Tuple<String, String, String> aInfo = null;
                if (Env.IsModuleInstalled("VA009_") && GetVA009_PaymentMethod_ID() == 0 && !Util.GetValueOfBool(Get_Value("IsSalesQuotation")))
                {
                    _processMsg = "@MandatoryPaymentMethod@";
                    return DocActionVariables.STATUS_INVALID;
                }

                if (Env.IsModuleInstalled("VAPOS_") && GetVAPOS_POSTerminal_ID() > 0)
                {
                    if (Util.GetValueOfDecimal(GetVAPOS_CreditAmt()) > 0)
                    {
                        MVABBusinessPartner bp = MVABBusinessPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID());
                        if (MVABBusinessPartner.SOCREDITSTATUS_CreditStop.Equals(bp.GetSOCreditStatus()))
                        {
                            _processMsg = "@BPartnerCreditStop@ - @TotalOpenBalance@="
                                + bp.GetTotalOpenBalance()
                                + ", @SO_CreditLimit@=" + bp.GetSO_CreditLimit();
                            return DocActionVariables.STATUS_INVALID;
                        }
                        if (MVABBusinessPartner.SOCREDITSTATUS_CreditHold.Equals(bp.GetSOCreditStatus()))
                        {
                            _processMsg = "@BPartnerCreditHold@ - @TotalOpenBalance@="
                                + bp.GetTotalOpenBalance()
                                + ", @SO_CreditLimit@=" + bp.GetSO_CreditLimit();
                            return DocActionVariables.STATUS_INVALID;
                        }
                        Decimal grandTotal = MVABExchangeRate.ConvertBase(GetCtx(),
                            GetVAPOS_CreditAmt(), GetVAB_Currency_ID(), GetDateOrdered(),
                            GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());

                        if (MVABBusinessPartner.SOCREDITSTATUS_CreditHold.Equals(bp.GetSOCreditStatus(grandTotal)))
                        {
                            _processMsg = "@BPartnerOverOCreditHold@ - @TotalOpenBalance@="
                                + bp.GetTotalOpenBalance() + ", @GrandTotal@=" + grandTotal
                                + ", @SO_CreditLimit@=" + bp.GetSO_CreditLimit();
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }
                }
                else
                {
                    string docSubType = Util.GetValueOfString(DB.ExecuteScalar(@"SELECT DocSubtypeSO FROM VAB_DocTypes WHERE 
                                                VAB_DocTypes_ID = " + GetVAB_DocTypesTarget_ID() + " AND DocBaseType = 'SOO'", null, Get_TrxName()));
                    if (!(docSubType == "ON" || docSubType == "OB"))
                    {
                        MVABBusinessPartner bp = MVABBusinessPartner.Get(GetCtx(), GetVAB_BusinessPartner_ID());
                        //if (MBPartner.SOCREDITSTATUS_CreditStop.Equals(bp.GetSOCreditStatus()))
                        //{
                        //    _processMsg = "@BPartnerCreditStop@ - @TotalOpenBalance@="
                        //        + bp.GetTotalOpenBalance()
                        //        + ", @SO_CreditLimit@=" + bp.GetSO_CreditLimit();
                        //    return DocActionVariables.STATUS_INVALID;
                        //}
                        //if (MBPartner.SOCREDITSTATUS_CreditHold.Equals(bp.GetSOCreditStatus()))
                        //{
                        //    _processMsg = "@BPartnerCreditHold@ - @TotalOpenBalance@="
                        //        + bp.GetTotalOpenBalance()
                        //        + ", @SO_CreditLimit@=" + bp.GetSO_CreditLimit();
                        //    return DocActionVariables.STATUS_INVALID;
                        //}
                        Decimal grandTotal = MVABExchangeRate.ConvertBase(GetCtx(),
                           GetGrandTotal(), GetVAB_Currency_ID(), GetDateOrdered(),
                            GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                        //if (MBPartner.SOCREDITSTATUS_CreditHold.Equals(bp.GetSOCreditStatus(grandTotal)))
                        //{
                        //    _processMsg = "@BPartnerOverOCreditHold@ - @TotalOpenBalance@="
                        //        + bp.GetTotalOpenBalance() + ", @GrandTotal@=" + grandTotal
                        //        + ", @SO_CreditLimit@=" + bp.GetSO_CreditLimit();
                        //    return DocActionVariables.STATUS_INVALID;
                        //}

                        string retMsg = "";
                        bool crdAll = bp.IsCreditAllowed(GetVAB_BPart_Location_ID(), grandTotal, out retMsg);
                        if (!crdAll)
                        {
                            if (bp.ValidateCreditValidation("A,D,E", GetVAB_BPart_Location_ID()))
                            {
                                _processMsg = retMsg;
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                }
            }

            _justPrepared = true;
            // dont uncomment
            //if (!DOCACTION_Complete.Equals(getDocAction()))		don't set for just prepare 
            //		setDocAction(DOCACTION_Complete);
            return DocActionVariables.STATUS_INPROGRESS;
        }

        /* 	Explode non stocked BOM.
         * 	@return true if bom exploded
         */
        private bool ExplodeBOM()
        {
            bool retValue = false;
            String where = "AND IsActive='Y' AND EXISTS "
                + "(SELECT * FROM VAM_Product p WHERE VAB_OrderLine.VAM_Product_ID=p.VAM_Product_ID"
                + " AND	p.IsBOM='Y' AND p.IsVerified='Y' AND p.IsStocked='N')";
            //
            String sql = "SELECT COUNT(*) FROM VAB_OrderLine "
                + "WHERE VAB_Order_ID=" + GetVAB_Order_ID() + where;
            int count = DataBase.DB.GetSQLValue(Get_TrxName(), sql); //Convert.ToInt32(DataBase.DB.ExecuteScalar(sql, null, Get_TrxName()));

            StringBuilder sbSQL = new StringBuilder("");

            while (count != 0)
            {
                retValue = true;
                RenumberLines(1000);		//	max 999 bom items	

                //	Order Lines with non-stocked BOMs
                MVABOrderLine[] lines = GetLines(where, "ORDER BY Line");
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABOrderLine line = lines[i];
                    MVAMProduct product = MVAMProduct.Get(GetCtx(), line.GetVAM_Product_ID());
                    log.Fine(product.GetName());
                    //	New Lines
                    int lineNo = line.GetLine();
                    MVAMProductBOM[] boms = MVAMProductBOM.GetBOMLines(product);
                    for (int j = 0; j < boms.Length; j++)
                    {
                        MVAMProductBOM bom = boms[j];
                        MVABOrderLine newLine = new MVABOrderLine(this);
                        newLine.SetLine(++lineNo);
                        newLine.SetVAM_Product_ID(bom.GetProduct()
                            .GetVAM_Product_ID());
                        newLine.SetVAB_UOM_ID(bom.GetProduct().GetVAB_UOM_ID());
                        newLine.SetQty(Decimal.Multiply(line.GetQtyOrdered(), bom.GetBOMQty()));
                        if (bom.GetDescription() != null)
                            newLine.SetDescription(bom.GetDescription());
                        //
                        newLine.SetPrice();
                        newLine.Save(Get_TrxName());
                    }
                    //	Convert into Comment Line
                    //line.SetVAM_Product_ID(0);
                    //line.SetVAM_PFeature_SetInstance_ID(0);
                    //line.SetPrice(Env.ZERO);
                    //line.SetPriceLimit(Env.ZERO);
                    //line.SetPriceList(Env.ZERO);
                    //line.SetLineNetAmt(Env.ZERO);
                    //line.SetFreightAmt(Env.ZERO);
                    //
                    String description = product.GetName();
                    if (product.GetDescription() != null)
                        description += " " + product.GetDescription();
                    if (line.GetDescription() != null)
                        description += " " + line.GetDescription();
                    //line.SetDescription(description);
                    //line.Save(Get_TrxName());

                    // change here to set product and other related information through query as in orderline before save you can not set both product or charge to ZERO
                    // Lokesh 10 july 2019
                    sbSQL.Clear();
                    sbSQL.Append(@"UPDATE VAB_OrderLine SET VAM_Product_ID = null, VAM_PFeature_SetInstance_ID = null, PriceEntered = 0, PriceLimit = 0, PriceList = 0, LineNetAmt = 0, 
                                    FreightAmt = 0, Description = '" + description + "' WHERE VAB_OrderLine_ID = " + line.GetVAB_OrderLine_ID());
                    int res = Util.GetValueOfInt(DB.ExecuteQuery(sbSQL.ToString(), null, Get_TrxName()));
                    if (res <= 0)
                    {
                        log.Info("line not updated for BOM type product");
                        break;
                    }

                }	//	for all lines with BOM

                _lines = null;		//	force requery
                count = DataBase.DB.GetSQLValue(Get_TrxName(), sql, GetVAB_Invoice_ID());
                RenumberLines(10);
            }	//	while count != 0
            return retValue;
        }

        /* Reserve Inventory.
        * 	Counterpart: MVAMInvInOut.completeIt()
        * 	@param dt document type or null
        * 	@param lines order lines (ordered by VAM_Product_ID for deadlock prevention)
        * 	@return true if (un) reserved
        */
        private bool ReserveStock(MVABDocTypes dt, MVABOrderLine[] lines)
        {
            try
            {
                if (dt == null)
                    dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

                // Reserved quantity and ordered quantity should not be updated for returns
                if (dt.IsReturnTrx())
                    return true;

                //	Binding
                bool binding = !dt.IsProposal();
                //	Not binding - i.e. Target=0
                if (DOCACTION_Void.Equals(GetDocAction())
                //Closing Binding Quotation
                || (MVABDocTypes.DOCSUBTYPESO_Quotation.Equals(dt.GetDocSubTypeSO())
                    && DOCACTION_Close.Equals(GetDocAction())))
                    //Commented this check for get binding by Vivek on 27/09/2017
                    //|| IsDropShip())
                    //if (DOCACTION_Void.Equals(GetDocAction())
                    //    //	Closing Binding Quotation
                    //|| (MVABDocTypes.DOCSUBTYPESO_Quotation.Equals(dt.GetDocSubTypeSO())
                    //    && DOCACTION_Close.Equals(GetDocAction())) || IsDropShip())
                    binding = false;
                bool isSOTrx = IsSOTrx();
                log.Fine("Binding=" + binding + " - IsSOTrx=" + isSOTrx);
                //	Force same WH for all but SO/PO
                int header_VAM_Warehouse_ID = GetVAM_Warehouse_ID();
                if (MVABDocTypes.DOCSUBTYPESO_StandardOrder.Equals(dt.GetDocSubTypeSO())
                    || MVABMasterDocType.DOCBASETYPE_PURCHASEORDER.Equals(dt.GetDocBaseType()))
                    header_VAM_Warehouse_ID = 0;		//	don't enforce

                Decimal Volume = Env.ZERO;
                Decimal Weight = Env.ZERO;

                //	Always check and (un) Reserve Inventory		
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABOrderLine line = lines[i];
                    int VAM_Locator_ID = 0;
                    MVAMWarehouse wh = MVAMWarehouse.Get(GetCtx(), line.GetVAM_Warehouse_ID());
                    //	Check/set WH/Org
                    if (header_VAM_Warehouse_ID != 0)	//	enforce WH
                    {
                        if (header_VAM_Warehouse_ID != line.GetVAM_Warehouse_ID())
                            line.SetVAM_Warehouse_ID(header_VAM_Warehouse_ID);
                        if (GetVAF_Org_ID() != line.GetVAF_Org_ID())
                            line.SetVAF_Org_ID(GetVAF_Org_ID());
                    }
                    //	Binding
                    Decimal target = binding ? line.GetQtyOrdered() : Env.ZERO;

                    Decimal difference = 0;

                    if (dt.GetDocBaseType() == "BOO")      //if (dt.GetValue() == "BSO")  IF is is BSO or BPO
                    {
                        difference = Decimal.Subtract(Decimal.Add(target, line.GetQtyReleased()), line.GetQtyReserved());
                    }
                    else
                    {
                        difference = Decimal.Subtract(Decimal.Subtract(target, line.GetQtyReserved()), line.GetQtyDelivered());
                    }



                    if (Env.Signum(difference) == 0)
                    {
                        MVAMProduct product = line.GetProduct();
                        if (product != null)
                        {
                            Volume = Decimal.Add(Volume, (Decimal.Multiply((Decimal)product.GetVolume(), line.GetQtyOrdered())));
                            Weight = Decimal.Add(Weight, (Decimal.Multiply(product.GetWeight(), line.GetQtyOrdered())));
                        }

                        //JID_1686,JID_1687 only Items are updated in storage tab
                        if (product.IsStocked())
                        {
                            // Work done by Vivek on 13/11/2017 assigned by Mukesh sir
                            // Work done to update qtyordered at storage and qtyreserved at order line
                            // when document is processed in closing state
                            if (DOCACTION_Close.Equals(GetDocAction()))
                            {
                                Decimal ordered = isSOTrx ? Env.ZERO : line.GetQtyReserved();
                                Decimal reserved = isSOTrx ? line.GetQtyReserved() : Env.ZERO;
                                VAM_Locator_ID = wh.GetDefaultVAM_Locator_ID();
                                if (!MVAMStorage.Add(GetCtx(), line.GetVAM_Warehouse_ID(), VAM_Locator_ID,
                                            line.GetVAM_Product_ID(),
                                            line.GetVAM_PFeature_SetInstance_ID(), reserved,
                                            ordered, Get_TrxName()))
                                    return false;
                                line.SetQtyReserved(Env.ZERO);
                                if (!line.Save(Get_TrxName()))
                                    return false;
                            }
                        }
                        continue;
                    }

                    log.Fine("Line=" + line.GetLine()
                        + " - Target=" + target + ",Difference=" + difference
                        + " - Ordered=" + line.GetQtyOrdered()
                        + ",Reserved=" + line.GetQtyReserved() + ",Delivered=" + line.GetQtyDelivered());

                    //	Check Product - Stocked and Item
                    MVAMProduct product1 = line.GetProduct();
                    if (product1 != null)
                    {
                        if (product1.IsStocked())
                        {
                            Decimal ordered = isSOTrx ? Env.ZERO : difference;

                            //if (dt.GetValue() == "BSO" && ordered == 0 && difference > 0)  // In case of Blanket Sales order set quantity order at Storage.
                            //{
                            //    ordered = difference;
                            //}

                            Decimal reserved = isSOTrx ? difference : Env.ZERO;
                            //	Get default Location'
                            //MWarehouse wh = MWarehouse.Get(GetCtx(), line.GetVAM_Warehouse_ID());

                            if (Env.IsModuleInstalled("VAPOS_") && Util.GetValueOfInt(GetVAPOS_POSTerminal_ID()) > 0)
                            {
                                //	Get Locator to reserve
                                if (line.GetVAM_PFeature_SetInstance_ID() != 0)	//	Get existing Location
                                    VAM_Locator_ID = MVAMStorage.GetVAM_Locator_ID(line.GetVAM_Warehouse_ID(),
                                        line.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(),
                                        ordered, Get_TrxName());
                                //	Get default Location
                                if (VAM_Locator_ID == 0)
                                {
                                    VAM_Locator_ID = wh.GetDefaultVAM_Locator_ID();
                                }
                            }
                            else
                            {
                                VAM_Locator_ID = wh.GetDefaultVAM_Locator_ID();

                                //	Get Locator to reserve
                                if (VAM_Locator_ID == 0)
                                {
                                    if (line.GetVAM_PFeature_SetInstance_ID() != 0)	//	Get existing Location
                                        VAM_Locator_ID = MVAMStorage.GetVAM_Locator_ID(line.GetVAM_Warehouse_ID(),
                                            line.GetVAM_Product_ID(), line.GetVAM_PFeature_SetInstance_ID(),
                                            ordered, Get_TrxName());
                                }
                                if (VAM_Locator_ID == 0)
                                {
                                    String sql = "SELECT VAM_Locator_ID FROM VAM_Locator WHERE VAM_Warehouse_ID=" + line.GetVAM_Warehouse_ID();
                                    VAM_Locator_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                                }
                            }


                            if (dt.IsReleaseDocument() && (dt.GetDocBaseType() == "SOO" || dt.GetDocBaseType() == "POO"))  //if (dt.GetValue() == "RSO" || dt.GetValue() == "RPO") // if (dt.IsSOTrx() && dt.GetDocBaseType() == "SOO" && dt.GetDocSubTypeSO() == "BO")   
                            {
                                // if it is Release Sales Order (RSO) Or Release Purchase Order (RPO) then donot reserve stock as it is already reserved during Blanket Sales Order Completion.
                            }
                            else
                            {
                                //	Update Storage
                                if (!MVAMStorage.Add(GetCtx(), line.GetVAM_Warehouse_ID(), VAM_Locator_ID,
                                    line.GetVAM_Product_ID(),
                                    line.GetVAM_PFeature_SetInstance_ID(), line.GetVAM_PFeature_SetInstance_ID(),
                                    Env.ZERO, reserved, ordered, Get_TrxName()))
                                    return false;
                            }
                        }	//	stockec
                        //	update line                             

                        Decimal? qtyRel = MVABUOMConversion.ConvertProductTo(GetCtx(), line.GetVAM_Product_ID(), line.GetVAB_UOM_ID(), difference);

                        // Added by Bharat on 03 April 2018 to handle issue to set Blanket Order Quantity only in case of Blanket Order
                        if (dt.GetDocBaseType() == "BOO")
                        {
                            if (qtyRel != null)
                            {
                                line.SetQtyBlanket(Decimal.Add(line.GetQtyBlanket(), Convert.ToDecimal(qtyRel)));
                            }
                            else
                            {
                                line.SetQtyBlanket(Decimal.Add(line.GetQtyBlanket(), difference));
                            }
                        }
                        line.SetQtyReserved(Decimal.Add(line.GetQtyReserved(), difference));

                        if (!line.Save(Get_TrxName()))
                            return false;



                        if (dt.IsReleaseDocument() && (dt.GetDocBaseType() == "SOO" || dt.GetDocBaseType() == "POO"))  //if (dt.GetValue() == "RSO" || dt.GetValue() == "RPO") // if (dt.IsSOTrx() && dt.GetDocBaseType() == "SOO" && dt.GetDocSubTypeSO() == "BO")
                        {
                            MVABOrderLine lineBlanket = new MVABOrderLine(GetCtx(), line.GetVAB_OrderLine_Blanket_ID(), Get_TrxName());

                            if (qtyRel != null)
                            {
                                lineBlanket.SetQty(Decimal.Subtract(lineBlanket.GetQtyEntered(), Convert.ToDecimal(qtyRel)));
                            }
                            else
                            {
                                lineBlanket.SetQty(Decimal.Subtract(lineBlanket.GetQtyEntered(), difference));
                            }

                            lineBlanket.SetQtyReleased(Decimal.Add(lineBlanket.GetQtyReleased(), difference));

                            line.SetQtyBlanket(lineBlanket.GetQtyEntered());

                            lineBlanket.Save();
                            line.Save();
                        }


                        Volume = Decimal.Add(Volume, (Decimal.Multiply((Decimal)product1.GetVolume(), line.GetQtyOrdered())));
                        Weight = Decimal.Add(Weight, (Decimal.Multiply(product1.GetWeight(), line.GetQtyOrdered())));
                    }	//	product
                }	//	reverse inventory

                SetVolume(Volume);
                SetWeight(Weight);
            }
            catch (Exception ex)
            {
                //ShowMessage.Error("MOrder", null, "ReserveStock");
            }
            return true;
        }

        /// <summary>
        /// Calculate Tax and Total
        /// </summary>
        /// <returns>true if tax total calculated</returns>
        private bool CalculateTaxTotal()
        {
            try
            {
                log.Fine("");
                //	Delete Taxes
                DataBase.DB.ExecuteQuery("DELETE FROM VAB_OrderTax WHERE VAB_Order_ID=" + GetVAB_Order_ID(), null, Get_TrxName());
                _taxes = null;

                //	Lines
                Decimal totalLines = Env.ZERO;
                List<int> taxList = new List<int>();
                MVABOrderLine[] lines = GetLines();
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABOrderLine line = lines[i];
                    int taxID = line.GetVAB_TaxRate_ID();
                    if (!taxList.Contains(taxID))
                    {
                        MVABOrderTax oTax = MVABOrderTax.Get(line, GetPrecision(),
                            false, Get_TrxName());	//	current Tax
                        //oTax.SetIsTaxIncluded(IsTaxIncluded());
                        if (!oTax.CalculateTaxFromLines())
                            return false;
                        if (!oTax.Save(Get_TrxName()))
                            return false;
                        taxList.Add(taxID);

                        // if Surcharge Tax is selected then calculate Tax for this Surcharge Tax.
                        if (line.Get_ColumnIndex("SurchargeAmt") > 0)
                        {
                            oTax = MVABOrderTax.GetSurcharge(line, GetPrecision(), false, Get_TrxName());  //	current Tax
                            if (oTax != null)
                            {
                                if (!oTax.CalculateSurchargeFromLines())
                                    return false;
                                if (!oTax.Save(Get_TrxName()))
                                    return false;
                            }
                        }
                    }
                    totalLines = Decimal.Add(totalLines, line.GetLineNetAmt());
                }

                //	Taxes
                Decimal grandTotal = totalLines;
                MVABOrderTax[] taxes = GetTaxes(true);
                for (int i = 0; i < taxes.Length; i++)
                {
                    MVABOrderTax oTax = taxes[i];
                    MVABTaxRate tax = oTax.GetTax();
                    if (tax.IsSummary())
                    {
                        MVABTaxRate[] cTaxes = tax.GetChildTaxes(false);
                        for (int j = 0; j < cTaxes.Length; j++)
                        {
                            MVABTaxRate cTax = cTaxes[j];
                            Decimal taxAmt = cTax.CalculateTax(oTax.GetTaxBaseAmt(), false, GetPrecision());

                            // JID_0430: if we add 2 lines with different Taxes. one is Parent and other is child. System showing error on completion that "Error Calculating Tax"
                            if (taxList.Contains(cTax.GetVAB_TaxRate_ID()))
                            {
                                String sql = "SELECT * FROM VAB_OrderTax WHERE VAB_Order_ID=" + GetVAB_Order_ID() + " AND VAB_TaxRate_ID=" + cTax.GetVAB_TaxRate_ID();
                                DataSet ds = DB.ExecuteDataset(sql, null, Get_TrxName());
                                if (ds != null && ds.Tables[0].Rows.Count > 0)
                                {
                                    DataRow dr = ds.Tables[0].Rows[0];
                                    MVABOrderTax newOTax = new MVABOrderTax(GetCtx(), dr, Get_TrxName());
                                    newOTax.SetTaxAmt(Decimal.Add(newOTax.GetTaxAmt(), taxAmt));
                                    newOTax.SetTaxBaseAmt(Decimal.Add(newOTax.GetTaxBaseAmt(), oTax.GetTaxBaseAmt()));
                                    if (!newOTax.Save(Get_TrxName()))
                                    {
                                        return false;
                                    }
                                }
                                ds = null;
                            }
                            else
                            {
                                //
                                MVABOrderTax newOTax = new MVABOrderTax(GetCtx(), 0, Get_TrxName());
                                newOTax.SetClientOrg(this);
                                newOTax.SetVAB_Order_ID(GetVAB_Order_ID());
                                newOTax.SetVAB_TaxRate_ID(cTax.GetVAB_TaxRate_ID());
                                newOTax.SetPrecision(GetPrecision());
                                newOTax.SetIsTaxIncluded(IsTaxIncluded());
                                newOTax.SetTaxBaseAmt(oTax.GetTaxBaseAmt());
                                newOTax.SetTaxAmt(taxAmt);
                                if (!newOTax.Save(Get_TrxName()))
                                    return false;
                            }
                            //
                            if (!IsTaxIncluded())
                                grandTotal = Decimal.Add(grandTotal, taxAmt);
                        }
                        if (!oTax.Delete(true, Get_TrxName()))
                            return false;
                        _taxes = null;
                    }
                    else
                    {
                        if (!IsTaxIncluded())
                            grandTotal = Decimal.Add(grandTotal, oTax.GetTaxAmt());
                    }
                }
                //
                SetTotalLines(totalLines);
                SetGrandTotal(grandTotal);
            }
            catch
            {
                //ShowMessage.Error("MOrder",null,"CalculateTaxTotal");
            }
            return true;
        }

        /// <summary>
        /// Approve Document
        /// </summary>
        /// <returns>true if success</returns>
        public bool ApproveIt()
        {
            log.Info("approveIt - " + ToString());
            SetIsApproved(true);
            return true;
        }

        /// <summary>
        /// Reject Approval
        /// </summary>
        /// <returns>true if success</returns>
        public bool RejectIt()
        {
            log.Info("rejectIt - " + ToString());
            SetIsApproved(false);
            return true;
        }

        /// <summary>
        /// Complete Document
        /// </summary>
        /// <returns>new status (Complete, In Progress, Invalid, Waiting ..)</returns>
        /****************************************************************************************************/
        public String CompleteIt()
        {
            // chck pallet Functionality applicable or not
            isContainerApplicable = MVAMInvTrx.ProductContainerApplicable(GetCtx());

            try
            {
                MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
                DocSubTypeSO = dt.GetDocSubTypeSO();

                //	Just prepare
                if (DOCACTION_Prepare.Equals(GetDocAction()))
                {
                    SetProcessed(false);
                    return DocActionVariables.STATUS_INPROGRESS;
                }

                // JID_1290: Set the document number from completed document sequence after completed (if needed)
                SetCompletedDocumentNo();

                if (!IsReturnTrx())
                {
                    //	Offers
                    if (MVABDocTypes.DOCSUBTYPESO_Proposal.Equals(DocSubTypeSO)
                        || MVABDocTypes.DOCSUBTYPESO_Quotation.Equals(DocSubTypeSO))
                    {
                        //	Binding
                        if (MVABDocTypes.DOCSUBTYPESO_Quotation.Equals(DocSubTypeSO))
                            ReserveStock(dt, GetLines(true, "VAM_Product_ID"));

                        // Added by Bharat on 22 August 2017 to copy lines to new table Order Line History in case of Quotation
                        if (PO.Get_Table_ID("VAB_OrderlineHistory") > 0)
                        {
                            #region VAB_OrderlineHistory
                            MVABOrderlineHistory lHist = null;
                            GetLines(true, null);
                            if (_lines.Length > 0)
                            {
                                for (int i = 0; i < _lines.Length; i++)
                                {
                                    lHist = new MVABOrderlineHistory(GetCtx(), 0, Get_TrxName());
                                    lHist.SetClientOrg(_lines[i]);
                                    lHist.SetVAB_OrderLine_ID(_lines[i].Get_ID());
                                    lHist.SetVAB_Charge_ID(_lines[i].GetVAB_Charge_ID());
                                    lHist.SetVAB_Frequency_ID(_lines[i].GetVAB_Frequency_ID());
                                    lHist.SetVAB_TaxRate_ID(_lines[i].GetVAB_TaxRate_ID());
                                    lHist.SetDateOrdered(_lines[i].GetDateOrdered());
                                    lHist.SetDatePromised(_lines[i].GetDatePromised());
                                    lHist.SetDescription(_lines[i].GetDescription());
                                    lHist.SetDiscount(_lines[i].GetDiscount());
                                    lHist.SetEndDate(_lines[i].GetEndDate());
                                    lHist.SetLineNetAmt(_lines[i].GetLineNetAmt());
                                    lHist.SetVAM_Product_ID(_lines[i].GetVAM_Product_ID());
                                    lHist.SetVAB_UOM_ID(_lines[i].GetVAB_UOM_ID());
                                    lHist.SetVAM_ShippingMethod_ID(_lines[i].GetVAM_ShippingMethod_ID());
                                    lHist.SetNoofCycle(_lines[i].GetNoofCycle());
                                    lHist.SetPriceActual(_lines[i].GetPriceActual());
                                    lHist.SetPriceCost(_lines[i].GetPriceCost());
                                    lHist.SetPriceEntered(_lines[i].GetPriceEntered());
                                    lHist.SetPriceList(_lines[i].GetPriceList());
                                    lHist.SetProcessed(true);
                                    lHist.SetQtyEntered(_lines[i].GetQtyEntered());
                                    lHist.SetQtyOrdered(_lines[i].GetQtyOrdered());
                                    lHist.SetQtyPerCycle(_lines[i].GetQtyPerCycle());
                                    lHist.SetStartDate(_lines[i].GetStartDate());
                                    if (!lHist.Save(Get_TrxName()))
                                    {
                                        _processMsg = "Could not Create Order Line History";
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                }
                            }
                            #endregion
                        }
                        SetProcessed(true);
                        SetDocAction(DOCACTION_Close);
                        return DocActionVariables.STATUS_COMPLETED;
                    }
                    //	Waiting Payment - until we have a payment
                    if (!_forceCreation
                        && MVABDocTypes.DOCSUBTYPESO_PrepayOrder.Equals(DocSubTypeSO)
                        && GetVAB_Payment_ID() == 0 && GetVAB_CashJRNLLine_ID() == 0)
                    {
                        SetProcessed(true);
                        return DocActionVariables.STATUS_WAITINGPAYMENT;
                    }


                    //by Sukhwinder on 28 July for Release Sales/Purchase order completion
                    if (dt.IsReleaseDocument() && (dt.GetDocBaseType() == "SOO" || dt.GetDocBaseType() == "POO"))// if (dt.GetValue() == "RSO" || dt.GetValue() == "RPO")  ///if (dt.IsSOTrx() && dt.GetDocBaseType() == "SOO" && dt.GetDocSubTypeSO() == "BO")     //if (dt.GetValue() == "RSO")
                    {
                        MVABOrderLine[] lines = GetLines(true, "VAM_Product_ID");
                        MVABOrder mo = new MVABOrder(GetCtx(), GetVAB_Order_ID(), null);
                        MVABOrder moBlanket = new MVABOrder(GetCtx(), mo.GetVAB_Order_Blanket(), null);

                        if (lines.Length == 0)
                        {
                            _processMsg = "@NoLines@";
                            return DocActionVariables.STATUS_INVALID;
                        }

                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].GetVAM_Warehouse_ID() != GetVAM_Warehouse_ID())
                            {
                                log.Warning("different Warehouse " + lines[i]);
                                _processMsg = "@CannotChangeDocType@";
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }

                        #region commented
                        //for (int i = 0; i < lines.Length; i++)
                        //{
                        //    bool binding = !dt.IsProposal();

                        //    Decimal target = binding ? lines[i].GetQtyOrdered() : Env.ZERO;

                        //    Decimal difference = 0;

                        //    difference = Decimal.Subtract(lines[i].GetQtyReserved(), lines[i].GetQtyDelivered());

                        //    MVABOrderLine lineBlanket = new MVABOrderLine(GetCtx(), lines[i].GetVAB_OrderLine_Blanket_ID(), null);

                        //    lineBlanket.SetQty(Decimal.Subtract(lineBlanket.GetQtyEntered(), difference));
                        //    lineBlanket.SetQtyReleased(Decimal.Add(lineBlanket.GetQtyReleased(), difference));

                        //    lines[i].SetQtyBlanket(lineBlanket.GetQtyEntered());

                        //    lineBlanket.Save();
                        //    lines[i].Save();
                        //}                      


                        //for (int i = 0; i < lines.Length; i++)
                        //{
                        //    MVABOrderLine lineBlanket = new MVABOrderLine(GetCtx(), lines[i].GetVAB_OrderLine_Blanket_ID(), null);

                        //    if (lineBlanket.GetQtyEntered() > 0
                        //        && Decimal.Subtract(lines[i].GetQtyBlanket(), lines[i].GetQtyEntered()) > 0
                        //        )
                        //    {
                        //        if (lines[i].GetQtyEntered() > lines[i].GetQtyReleased())
                        //        {
                        //            lineBlanket.SetQty(Decimal.Subtract(lineBlanket.GetQtyEntered(), Decimal.Subtract(lines[i].GetQtyEntered(), lines[i].GetQtyReleased())));
                        //            lineBlanket.SetQtyReleased(Decimal.Add(Decimal.Subtract(lines[i].GetQtyEntered(), lines[i].GetQtyReleased()), lineBlanket.GetQtyReleased()));

                        //        }
                        //        else if (lines[i].GetQtyEntered() == lines[i].GetQtyReleased())
                        //        {
                        //            if (Decimal.Subtract(lineBlanket.GetQtyEntered(), lineBlanket.GetQtyReleased()) == lineBlanket.GetQtyEntered())
                        //            {
                        //                lineBlanket.SetQty(Decimal.Subtract(lineBlanket.GetQtyEntered(), lines[i].GetQtyEntered()));
                        //                lineBlanket.SetQtyReleased(Decimal.Add(lineBlanket.GetQtyReleased(), lines[i].GetQtyEntered()));
                        //            }
                        //        }
                        //        else
                        //        {
                        //            lineBlanket.SetQty(Decimal.Add(lineBlanket.GetQtyEntered(), Decimal.Subtract(lines[i].GetQtyReleased(), lines[i].GetQtyEntered())));
                        //            lineBlanket.SetQtyReleased(Decimal.Subtract(lineBlanket.GetQtyReleased(), Decimal.Subtract(lines[i].GetQtyReleased(), lines[i].GetQtyEntered())));
                        //        }

                        //        // lineBlanket.SetQty(lineBlanket.GetQtyEntered() - lines[i].GetQtyEntered());
                        //        //lineBlanket.SetQtyReleased(Decimal.Subtract(Decimal.Add(lineBlanket.GetQtyReleased(), lines[i].GetQtyEntered()), lines[i].GetQtyReleased()));

                        //        // lines[i].SetQtyReserved(Decimal.Add(lines[i].GetQtyReserved(), lines[i].GetQtyEntered()));
                        //        lines[i].SetQtyBlanket(lineBlanket.GetQtyEntered());
                        //    }

                        //    lineBlanket.Save();
                        //    lines[i].Save();
                        //}
                        #endregion

                    }

                    // Enabled Order History Tab
                    //if (dt.GetDocBaseType() == "BOO") ///dt.GetValue() == "BSO" || dt.GetValue() == "BPO")
                    //{
                    if (PO.Get_Table_ID("VAB_OrderlineHistory") > 0)
                    {
                        #region VAB_OrderlineHistory
                        MVABOrderlineHistory lHist = null;
                        GetLines(true, null);
                        if (_lines.Length > 0)
                        {
                            for (int i = 0; i < _lines.Length; i++)
                            {
                                lHist = new MVABOrderlineHistory(GetCtx(), 0, Get_TrxName());
                                lHist.SetClientOrg(_lines[i]);
                                lHist.SetVAB_OrderLine_ID(_lines[i].Get_ID());
                                lHist.SetVAB_Charge_ID(_lines[i].GetVAB_Charge_ID());
                                lHist.SetVAB_Frequency_ID(_lines[i].GetVAB_Frequency_ID());
                                lHist.SetVAB_TaxRate_ID(_lines[i].GetVAB_TaxRate_ID());
                                lHist.SetDateOrdered(_lines[i].GetDateOrdered());
                                lHist.SetDatePromised(_lines[i].GetDatePromised());
                                lHist.SetDescription(_lines[i].GetDescription());
                                lHist.SetDiscount(_lines[i].GetDiscount());
                                lHist.SetEndDate(_lines[i].GetEndDate());
                                lHist.SetLineNetAmt(_lines[i].GetLineNetAmt());
                                lHist.SetVAM_Product_ID(_lines[i].GetVAM_Product_ID());
                                lHist.SetVAB_UOM_ID(_lines[i].GetVAB_UOM_ID());
                                lHist.SetVAM_ShippingMethod_ID(_lines[i].GetVAM_ShippingMethod_ID());
                                lHist.SetNoofCycle(_lines[i].GetNoofCycle());
                                lHist.SetPriceActual(_lines[i].GetPriceActual());
                                lHist.SetPriceCost(_lines[i].GetPriceCost());
                                lHist.SetPriceEntered(_lines[i].GetPriceEntered());
                                lHist.SetPriceList(_lines[i].GetPriceList());
                                lHist.SetProcessed(true);
                                lHist.SetQtyEntered(_lines[i].GetQtyEntered());
                                lHist.SetQtyOrdered(_lines[i].GetQtyOrdered());
                                lHist.SetQtyPerCycle(_lines[i].GetQtyPerCycle());
                                lHist.SetStartDate(_lines[i].GetStartDate());
                                if (!lHist.Save(Get_TrxName()))
                                {
                                    _processMsg = "Could not Create Order Line History";
                                    return DocActionVariables.STATUS_INVALID;
                                }
                            }
                        }
                        #endregion
                        //}
                    }

                    //	Re-Check
                    if (!_justPrepared)
                    {
                        String status = PrepareIt();
                        if (!DocActionVariables.STATUS_INPROGRESS.Equals(status))
                            return status;
                    }
                }

                // Handle Budget Control
                if (Env.IsModuleInstalled("FRPT_") && !IsSOTrx() && !IsReturnTrx())
                {
                    // budget control functionality work when Financial Managemt Module Available
                    try
                    {
                        log.Info("Budget Control Start for PO Document No  " + GetDocumentNo());
                        EvaluateBudgetControlData();
                        if (_budgetMessage.Length > 0)
                        {
                            _processMsg = Msg.GetMsg(GetCtx(), "BudgetExceedFor") + _budgetMessage;
                            SetProcessed(false);
                            return DocActionVariables.STATUS_INPROGRESS;
                        }
                        log.Info("Budget Control Completed for PO Document No  " + GetDocumentNo());
                    }
                    catch (Exception ex)
                    {
                        log.Severe("Budget Control Issue " + ex.Message);
                        SetProcessed(false);
                        return DocActionVariables.STATUS_INPROGRESS;
                    }
                }

                //	Implicit Approval
                if (!IsApproved())
                    ApproveIt();
                GetLines(true, null);

                //JID_1126: System will check the selected Vendor and product to update the price on Purchasing tab.
                if (!IsSOTrx() && !IsReturnTrx() && dt.GetDocBaseType() == "POO")
                {
                    MVAMProductPO po = null;

                    for (int i = 0; i < _lines.Length; i++)
                    {
                        if (_lines[i].GetVAB_Charge_ID() > 0)
                        {
                            continue;
                        }
                        po = MVAMProductPO.GetOfVendorProduct(GetCtx(), GetVAB_BusinessPartner_ID(), _lines[i].GetVAM_Product_ID(), Get_Trx());
                        if (po != null)
                        {
                            po.SetPriceLastPO(_lines[i].GetPriceEntered());
                            if (!po.Save())
                            {
                                _processMsg = Msg.GetMsg(GetCtx(), "NotUpdatePOPrice");
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                }

                log.Info(ToString());
                StringBuilder Info = new StringBuilder();

                /* nnayak - Bug 1720003 - We need to set the processed flag so the Tax Summary Line
                does not get recreated in the afterSave procedure of the MVABOrderLine class */
                SetProcessed(true);

                bool realTimePOS = false;

                try
                {
                    //	Counter Documents
                    MVABOrder counter = CreateCounterDoc();
                    if (counter != null)
                        Info.Append(" - @CounterDoc@: @Order@=").Append(counter.GetDocumentNo());
                }
                catch (Exception e)
                {
                    Info.Append(" - @CounterDoc@: ").Append(e.Message.ToString());
                }

                ////	Create SO Shipment - Force Shipment
                MVAMInvInOut shipment = null;
                // Shipment not created in case of Resturant               

                if (Util.GetValueOfString(dt.GetVAPOS_POSMode()) != "RS")
                {
                    if (MVABDocTypes.DOCSUBTYPESO_OnCreditOrder.Equals(DocSubTypeSO)		//	(W)illCall(I)nvoice
                        || MVABDocTypes.DOCSUBTYPESO_WarehouseOrder.Equals(DocSubTypeSO)	//	(W)illCall(P)ickup	
                        || MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO)			//	(W)alkIn(R)eceipt
                        || MVABDocTypes.DOCSUBTYPESO_PrepayOrder.Equals(DocSubTypeSO))
                    {
                        if (!DELIVERYRULE_Force.Equals(GetDeliveryRule()))
                            SetDeliveryRule(DELIVERYRULE_Force);
                        //
                        shipment = CreateShipment(dt, realTimePOS ? null : GetDateOrdered());
                        if (shipment == null)
                            return DocActionVariables.STATUS_INVALID;
                        Info.Append("Successfully created:@VAM_Inv_InOut_ID@ & doc no.: ").Append(shipment.GetDocumentNo());
                        _processMsg = Info.ToString();
                        if (shipment.GetDocStatus() == "DR")
                        {
                            if (String.IsNullOrEmpty(_processMsg))
                            {
                                _processMsg = " Could Not Complete because Reserved qty is greater than Onhand qty, Available Qty Is : " + OnHandQty;
                            }
                            shipment.SetProcessMsg(_processMsg);
                            // Info.Append(" " + _processMsg);
                        }


                        String msg = shipment.GetProcessMsg();
                        if (msg != null && msg.Length > 0)
                            Info.Append(" (").Append(msg).Append(")");
                    }
                }


                //	Create SO Invoice - Always invoice complete Order
                if (MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO)
                    || MVABDocTypes.DOCSUBTYPESO_OnCreditOrder.Equals(DocSubTypeSO)
                    || MVABDocTypes.DOCSUBTYPESO_PrepayOrder.Equals(DocSubTypeSO))
                {
                    try
                    {
                        DateTime? tSet = realTimePOS ? null : GetDateOrdered();
                        MVABInvoice invoice = CreateInvoice(dt, shipment, tSet);
                        if (invoice == null)
                        {
                            Get_Trx().Rollback();
                            return DocActionVariables.STATUS_INVALID;
                        }
                        //Info.Append(" - @VAB_Invoice_ID@: ").Append(invoice.GetDocumentNo());
                        //Info.Append(" & @VAB_Invoice_ID@ No: ").Append(invoice.GetDocumentNo()).Append(" generated successfully");
                        Info.Append(" & @VAB_Invoice_ID@ No: ").Append(invoice.GetDocumentNo());
                        _processMsg += Info.ToString();

                        String msg = invoice.GetProcessMsg();
                        if (msg != null && msg.Length > 0)
                            Info.Append(" (").Append(msg).Append(")");
                    }
                    catch (NullReferenceException ex)
                    {
                        //ShowMessage.Error("Moder",null,"Completeit");
                    }
                }

                ////	Counter Documents
                //MOrder counter = CreateCounterDoc();
                //if (counter != null)
                //    Info.Append(" - @CounterDoc@: @Order@=").Append(counter.GetDocumentNo());
                //if (havingPriceList == 0)
                //{
                //    _processMsg = Msg.GetMsg(GetCtx(), "VIS_PriceListNotAvilable");
                //    return DocActionVariables.STATUS_INVALID;
                //}

                //User Validation
                String valid = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_AFTER_COMPLETE);
                if (valid != null)
                {
                    if (Info.Length > 0)
                        Info.Append(" - ");
                    Info.Append(valid);
                    _processMsg = Info.ToString();
                    return DocActionVariables.STATUS_INVALID;
                }
                /******************/
                String Qry = "Select * from VAB_OrderLine where VAB_Order_ID=" + GetVAB_Order_ID();
                DataSet orderlines = new DataSet();
                orderlines = DB.ExecuteDataset(Qry);
                if (orderlines.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < orderlines.Tables[0].Rows.Count; i++)
                    {
                        Char IsCont = Convert.ToChar(orderlines.Tables[0].Rows[i]["IsContract"]);
                        if (IsCont == 'Y')
                        {
                            MVABOrder mo = new MVABOrder(GetCtx(), GetVAB_Order_ID(), null);
                            mo.SetIsContract(true);
                            mo.Save();
                        }

                        ////Set Values on unit window if Unit is seleced on Line in case of POC Construction Module installed only for demo perpose
                        if (Env.IsModuleInstalled("VA052_"))
                        {
                            int UnitID = Util.GetValueOfInt(orderlines.Tables[0].Rows[i]["VAA_Asset_ID"]); //AssetID is UnitID
                            if (UnitID > 0)
                            {
                                MVAAsset asset = new MVAAsset(GetCtx(), UnitID, Get_Trx());
                                asset.Set_Value("VA052_Status", "SO");
                                asset.Set_Value("VA052_LoanAmount", Util.GetValueOfDecimal(orderlines.Tables[0].Rows[i]["VA052_LoanAmount"]));
                                asset.Set_Value("VAB_BusinessPartner_ID", GetVAB_BusinessPartner_ID());
                                asset.Set_Value("VA052_BuyersCont", Util.GetValueOfDecimal(orderlines.Tables[0].Rows[i]["LineNetAmt"]) - Util.GetValueOfDecimal(orderlines.Tables[0].Rows[i]["VA052_LoanAmount"]));
                                if (!asset.Save())
                                    log.SaveError("Error", "WhileSavingDataOnAssetWindow_VA052");

                            }
                        }
                    }
                }

                /******************/

                //landed cost distribution
                if (!IsSOTrx())
                {
                    String error = ExpectedlandedCostDistribution();
                    if (!Util.IsEmpty(error))
                    {
                        _processMsg = error;
                        return DocActionVariables.STATUS_INVALID;
                    }
                }

                SetProcessed(true);
                _processMsg = Info.ToString();
                //
                SetDocAction(DOCACTION_Close);
                //Changes by abhishek suggested by lokesh on 7/1/2016
                //try
                //{
                //    int countVAPOS = Util.GetValueOfInt(DB.ExecuteScalar("Select count(*) from VAF_ModuleInfo Where Prefix='VAPOS_'"));
                //    if (countVAPOS > 0)
                //    {
                //        MVAMPriceList priceLst = new MVAMPriceList(GetCtx(), GetVAM_PriceList_ID(), null);
                //        bool taxInclusive = priceLst.IsTaxIncluded();
                //        int VAPOS_POSTertminal_ID = Util.GetValueOfInt(DB.ExecuteScalar("Select VAPOS_POSTerminal_ID from VAB_Order Where VAB_Order_ID=" + GetVAB_Order_ID()));
                //        if (VAPOS_POSTertminal_ID > 0)
                //        {
                //            string cAmount = Util.GetValueOfString(DB.ExecuteScalar("Select VAPOS_CashPaid from VAB_Order Where VAB_Order_ID=" + GetVAB_Order_ID()));
                //            string pAmount = Util.GetValueOfString(DB.ExecuteScalar("Select VAPOS_PayAmt from VAB_Order Where VAB_Order_ID=" + GetVAB_Order_ID()));
                //            List<string> tax_IDLst = new List<string>();
                //            List<string> OLTaxAmtLst = new List<string>();
                //            List<string> DscLineLst = new List<string>();

                //            DataSet dsDE = DB.ExecuteDataset("select ol.VAB_TaxRate_ID, ol.VAPOS_DiscountAmount, ol.LINENETAMT, tx.rate from VAB_OrderLine ol inner join VAB_TaxRate tx on(ol.VAB_TaxRate_ID=tx.VAB_TaxRate_ID)  where VAB_Order_ID=" + GetVAB_Order_ID());
                //            try
                //            {
                //                if (dsDE != null)
                //                {
                //                    if (dsDE.Tables[0].Rows.Count > 0)
                //                    {
                //                        for (int i = 0; i < dsDE.Tables[0].Rows.Count; i++)
                //                        {
                //                            tax_IDLst.Add(Util.GetValueOfString(dsDE.Tables[0].Rows[i]["VAB_TaxRate_ID"]));
                //                            DscLineLst.Add(Util.GetValueOfString(dsDE.Tables[0].Rows[i]["VAPOS_DiscountAmount"]));
                //                            decimal taxRate = Util.GetValueOfDecimal(dsDE.Tables[0].Rows[i]["rate"]);
                //                            decimal LINENETAMT = Util.GetValueOfDecimal(dsDE.Tables[0].Rows[i]["LINENETAMT"]);
                //                            if (taxInclusive)
                //                            {
                //                                OLTaxAmtLst.Add(Convert.ToString(((LINENETAMT / (100 + taxRate)) * (taxRate / 100))));
                //                            }
                //                            else
                //                            {
                //                                OLTaxAmtLst.Add(Convert.ToString(taxRate * LINENETAMT / 100));

                //                            }

                //                        }
                //                    }
                //                    dsDE.Dispose();
                //                }
                //            }
                //            catch
                //            {
                //                if (dsDE != null) { dsDE.Dispose(); }
                //            }
                //            string[] tax_ID = tax_IDLst.ToArray();
                //            string[] OLTaxAmt = OLTaxAmtLst.ToArray();
                //            string[] DscLine = DscLineLst.ToArray();
                //            SaveDayEndRecord(GetCtx(), VAPOS_POSTertminal_ID, cAmount, pAmount, GetVAB_DocTypes_ID(), tax_ID, OLTaxAmt, GetGrandTotal().ToString(), DscLine);
                //        }
                //    }
                //}

                //catch
                //{
                //    //ShowMessage.Error("MOrder",null,"CompleteIt");
                //}
            }

            catch
            {
                //ShowMessage.Error("MOrder",null,"CompleteIt");
                _processMsg = GetProcessMsg();
                return DocActionVariables.STATUS_INVALID;
            }


            return DocActionVariables.STATUS_COMPLETED;
        }

        /// <summary>
        /// This function is used to check, document is budget control or not.
        /// </summary>
        /// <returns>True, when budget controlled or not applicable</returns>
        private bool EvaluateBudgetControlData()
        {
            DataSet dsRecordData;
            DataRow[] drRecordData = null;
            DataRow[] drBudgetControl = null;
            DataSet dsBudgetControlDimension;
            DataRow[] drBudgetControlDimension = null;
            List<BudgetControl> _budgetControl = new List<BudgetControl>();
            StringBuilder sql = new StringBuilder();
            BudgetCheck budget = new BudgetCheck();

            sql.Clear();
            sql.Append(@"SELECT VAGL_Budget.VAGL_Budget_ID , VAGL_Budget.BudgetControlBasis, VAGL_Budget.VAB_Year_ID , VAGL_Budget.VAB_YearPeriod_ID,VAGL_Budget.Name As BudgetName, 
                  VAGL_BudgetActivation.VAB_AccountBook_ID, VAGL_BudgetActivation.CommitmentType, VAGL_BudgetActivation.BudgetControlScope,  VAGL_BudgetActivation.VAGL_BudgetActivation_ID, VAGL_BudgetActivation.Name AS ControlName 
                FROM VAGL_Budget INNER JOIN VAGL_BudgetActivation ON VAGL_Budget.VAGL_Budget_ID = VAGL_BudgetActivation.VAGL_Budget_ID
                INNER JOIN VAF_ClientDetail ON VAF_ClientDetail.VAF_Client_ID = VAGL_Budget.VAF_Client_ID
                WHERE VAGL_BudgetActivation.IsActive = 'Y' AND VAGL_Budget.IsActive = 'Y' AND VAGL_BudgetActivation.VAF_Org_ID IN (0 , " + GetVAF_Org_ID() + @")
                   AND VAGL_BudgetActivation.CommitmentType IN('B', 'C') AND
                  ((VAGL_Budget.BudgetControlBasis = 'P' AND VAGL_Budget.VAB_YearPeriod_ID =
                  (SELECT VAB_YearPeriod.VAB_YearPeriod_ID FROM VAB_YearPeriod INNER JOIN VAB_Year ON VAB_Year.VAB_Year_ID = VAB_YearPeriod.VAB_Year_ID
                  WHERE VAB_YearPeriod.IsActive = 'Y'  AND VAB_Year.VAB_Calender_ID = VAF_ClientDetail.VAB_Calender_ID
                  AND " + GlobalVariable.TO_DATE(GetDateAcct(), true) + @" BETWEEN VAB_YearPeriod.StartDate AND VAB_YearPeriod.EndDate))
                OR(VAGL_Budget.BudgetControlBasis = 'A' AND VAGL_Budget.VAB_Year_ID =
                  (SELECT VAB_YearPeriod.VAB_Year_ID FROM VAB_YearPeriod INNER JOIN VAB_Year ON VAB_Year.VAB_Year_ID = VAB_YearPeriod.VAB_Year_ID
                  WHERE VAB_YearPeriod.IsActive = 'Y'   AND VAB_Year.VAB_Calender_ID = VAF_ClientDetail.VAB_Calender_ID
                AND " + GlobalVariable.TO_DATE(GetDateAcct(), true) + @" BETWEEN VAB_YearPeriod.StartDate AND VAB_YearPeriod.EndDate) ) ) 
                AND(SELECT COUNT(Actual_Acct_Detail_ID) FROM Actual_Acct_Detail
                WHERE VAGL_Budget_ID = VAGL_Budget.VAGL_Budget_ID
                AND(VAB_YearPeriod_ID  IN (NVL(VAGL_Budget.VAB_YearPeriod_ID, 0))
                OR VAB_YearPeriod_ID    IN (SELECT VAB_YearPeriod_ID FROM VAB_YearPeriod   WHERE VAB_Year_ID = NVL(VAGL_Budget.VAB_Year_ID, 0)))) > 0");
            DataSet dsBudgetControl = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
            if (dsBudgetControl != null && dsBudgetControl.Tables.Count > 0 && dsBudgetControl.Tables[0].Rows.Count > 0)
            {
                // get budget control ids (TODO for postgre)
                object[] budgetControlIds = dsBudgetControl.Tables[0].AsEnumerable().Select(r => r.Field<object>("VAGL_BUDGETACTIVATION_ID")).ToArray();
                string result = string.Join(",", budgetControlIds);
                dsBudgetControlDimension = budget.GetBudgetDimension(result);

                // get record posting data 
                dsRecordData = BudgetControlling();
                if (dsRecordData != null && dsRecordData.Tables.Count > 0 && dsRecordData.Tables[0].Rows.Count > 0)
                {
                    // datarows of Debit values which to be controlled
                    drRecordData = dsRecordData.Tables[0].Select("Debit > 0 ", " Account_ID ASC");
                    if (drRecordData != null)
                    {
                        // loop on PO record data which is to be debited only 
                        for (int i = 0; i < drRecordData.Length; i++)
                        {
                            // datarows of Budget, of selected accouting schema
                            drBudgetControl = dsBudgetControl.Tables[0].Select("VAB_AccountBook_ID  = " + Util.GetValueOfInt(drRecordData[i]["VAB_AccountBook_ID"]));

                            // loop on Budget which to be controlled 
                            if (drBudgetControl != null)
                            {
                                for (int j = 0; j < drBudgetControl.Length; j++)
                                {
                                    // get budget Dimension datarow 
                                    drBudgetControlDimension = dsBudgetControlDimension.Tables[0].Select("VAGL_BudgetActivation_ID  = "
                                                                + Util.GetValueOfInt(drBudgetControl[j]["VAGL_BudgetActivation_ID"]));

                                    // get BUdgeted Controlled Value based on dimension
                                    _budgetControl = budget.GetBudgetControlValue(drRecordData[i], drBudgetControl[j], drBudgetControlDimension, GetDateAcct(),
                                        _budgetControl, Get_Trx(), 'O', GetVAB_Order_ID());

                                    // Reduce amount from Budget controlled value
                                    _budgetControl = ReduceAmountFromBudget(drRecordData[i], drBudgetControl[j], drBudgetControlDimension, _budgetControl);

                                }
                            }
                        }
                    }
                }
            }
            else
            {
                //  no recod found for budget control 
                log.Info("Budget control not found" + sql.ToString());
                return true;
            }
            return true;
        }

        /// <summary>
        /// This Function is used to get data based on Posting Logic, which is to be posted after completion.
        /// </summary>
        /// <returns>DataSet of Posting Records</returns>
        private DataSet BudgetControlling()
        {
            int VAF_Screen_id = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAF_Screen_ID FROM VAF_Screen WHERE  Export_ID = 'VIS_181'")); // Purchase Order
            DataSet result = new DataSet();
            Type type = null;
            MethodInfo methodInfo = null;
            string className = "FRPTSvc.Controllers.PostAccLocalizationVO";
            type = ClassTypeContainer.GetClassType(className, "FRPTSvc");
            if (type != null)
            {
                methodInfo = type.GetMethod("BudgetControlled");
                if (methodInfo != null)
                {
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if (parameters.Length == 8)
                    {
                        object[] parametersArray = new object[] { GetCtx(),
                                                                Util.GetValueOfInt(GetVAF_Client_ID()),
                                                                Util.GetValueOfInt(X_VAB_Order.Table_ID),//MVAFTableView.Get(GetCtx() , "VAB_Order").GetVAF_TableView_ID()
                                                                Util.GetValueOfInt(GetVAB_Order_ID()),
                                                                true,
                                                                Util.GetValueOfInt(GetVAF_Org_ID()),
                                                                VAF_Screen_id,
                                                                Util.GetValueOfInt(GetVAB_DocTypesTarget_ID()) };
                        result = (DataSet)methodInfo.Invoke(null, parametersArray);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// This Function is used to Reduce From Budget controlled amount
        /// </summary>
        /// <param name="drDataRecord">document Posting Record</param>
        /// <param name="drBUdgetControl">BUdget Control information</param>
        /// <param name="drBudgetComtrolDimension">Budget Control dimension which is applicable</param>
        /// <param name="_listBudgetControl">list of budget controls</param>
        /// <returns>modified list Budget Control</returns>
        public List<BudgetControl> ReduceAmountFromBudget(DataRow drDataRecord, DataRow drBUdgetControl, DataRow[] drBudgetComtrolDimension, List<BudgetControl> _listBudgetControl)
        {
            BudgetControl _budgetControl = null;
            List<String> selectedDimension = new List<string>();
            if (drBudgetComtrolDimension != null)
            {
                for (int i = 0; i < drBudgetComtrolDimension.Length; i++)
                {
                    selectedDimension.Add(Util.GetValueOfString(drBudgetComtrolDimension[i]["ElementType"]));
                }
            }

            if (_listBudgetControl.Exists(x => (x.VAGL_Budget_ID == Util.GetValueOfInt(drBUdgetControl["VAGL_Budget_ID"])) &&
                                              (x.VAGL_BudgetActivation_ID == Util.GetValueOfInt(drBUdgetControl["VAGL_BudgetActivation_ID"])) &&
                                              (x.Account_ID == Util.GetValueOfInt(drDataRecord["Account_ID"])) &&
                                              (x.VAF_Org_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Organization) ? Util.GetValueOfInt(drDataRecord["VAF_Org_ID"]) : 0)) &&
                                              (x.VAB_BusinessPartner_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_BPartner) ? Util.GetValueOfInt(drDataRecord["VAB_BusinessPartner_ID"]) : 0)) &&
                                              (x.VAM_Product_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Product) ? Util.GetValueOfInt(drDataRecord["VAM_Product_ID"]) : 0)) &&
                                              (x.VAB_BillingCode_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Activity) ? Util.GetValueOfInt(drDataRecord["VAB_BillingCode_ID"]) : 0)) &&
                                              (x.VAB_AddressFrom_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationFrom) ? Util.GetValueOfInt(drDataRecord["VAB_AddressFrom_ID"]) : 0)) &&
                                              (x.VAB_AddressTo_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationTo) ? Util.GetValueOfInt(drDataRecord["VAB_AddressTo_ID"]) : 0)) &&
                                              (x.VAB_Promotion_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Campaign) ? Util.GetValueOfInt(drDataRecord["VAB_Promotion_ID"]) : 0)) &&
                                              (x.VAF_OrgTrx_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_OrgTrx) ? Util.GetValueOfInt(drDataRecord["VAF_OrgTrx_ID"]) : 0)) &&
                                              (x.VAB_Project_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Project) ? Util.GetValueOfInt(drDataRecord["VAB_Project_ID"]) : 0)) &&
                                              (x.VAB_SalesRegionState_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_SalesRegion) ? Util.GetValueOfInt(drDataRecord["VAB_SalesRegionState_ID"]) : 0)) &&
                                              (x.UserList1_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList1) ? Util.GetValueOfInt(drDataRecord["UserList1_ID"]) : 0)) &&
                                              (x.UserList2_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList2) ? Util.GetValueOfInt(drDataRecord["UserList2_ID"]) : 0)) &&
                                              (x.UserElement1_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement1) ? Util.GetValueOfInt(drDataRecord["UserElement1_ID"]) : 0)) &&
                                              (x.UserElement2_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement2) ? Util.GetValueOfInt(drDataRecord["UserElement2_ID"]) : 0)) &&
                                              (x.UserElement3_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement3) ? Util.GetValueOfInt(drDataRecord["UserElement3_ID"]) : 0)) &&
                                              (x.UserElement4_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement4) ? Util.GetValueOfInt(drDataRecord["UserElement4_ID"]) : 0)) &&
                                              (x.UserElement5_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement5) ? Util.GetValueOfInt(drDataRecord["UserElement5_ID"]) : 0)) &&
                                              (x.UserElement6_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement6) ? Util.GetValueOfInt(drDataRecord["UserElement6_ID"]) : 0)) &&
                                              (x.UserElement7_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement7) ? Util.GetValueOfInt(drDataRecord["UserElement7_ID"]) : 0)) &&
                                              (x.UserElement8_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement8) ? Util.GetValueOfInt(drDataRecord["UserElement8_ID"]) : 0)) &&
                                              (x.UserElement9_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement9) ? Util.GetValueOfInt(drDataRecord["UserElement9_ID"]) : 0))
                                             ))
            {
                _budgetControl = _listBudgetControl.Find(x => (x.VAGL_Budget_ID == Util.GetValueOfInt(drBUdgetControl["VAGL_Budget_ID"])) &&
                                              (x.VAGL_BudgetActivation_ID == Util.GetValueOfInt(drBUdgetControl["VAGL_BudgetActivation_ID"])) &&
                                              (x.Account_ID == Util.GetValueOfInt(drDataRecord["Account_ID"])) &&
                                              (x.VAF_Org_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Organization) ? Util.GetValueOfInt(drDataRecord["VAF_Org_ID"]) : 0)) &&
                                              (x.VAB_BusinessPartner_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_BPartner) ? Util.GetValueOfInt(drDataRecord["VAB_BusinessPartner_ID"]) : 0)) &&
                                              (x.VAM_Product_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Product) ? Util.GetValueOfInt(drDataRecord["VAM_Product_ID"]) : 0)) &&
                                              (x.VAB_BillingCode_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Activity) ? Util.GetValueOfInt(drDataRecord["VAB_BillingCode_ID"]) : 0)) &&
                                              (x.VAB_AddressFrom_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationFrom) ? Util.GetValueOfInt(drDataRecord["VAB_AddressFrom_ID"]) : 0)) &&
                                              (x.VAB_AddressTo_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationTo) ? Util.GetValueOfInt(drDataRecord["VAB_AddressTo_ID"]) : 0)) &&
                                              (x.VAB_Promotion_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Campaign) ? Util.GetValueOfInt(drDataRecord["VAB_Promotion_ID"]) : 0)) &&
                                              (x.VAF_OrgTrx_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_OrgTrx) ? Util.GetValueOfInt(drDataRecord["VAF_OrgTrx_ID"]) : 0)) &&
                                              (x.VAB_Project_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Project) ? Util.GetValueOfInt(drDataRecord["VAB_Project_ID"]) : 0)) &&
                                              (x.VAB_SalesRegionState_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_SalesRegion) ? Util.GetValueOfInt(drDataRecord["VAB_SalesRegionState_ID"]) : 0)) &&
                                              (x.UserList1_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList1) ? Util.GetValueOfInt(drDataRecord["UserList1_ID"]) : 0)) &&
                                              (x.UserList2_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList2) ? Util.GetValueOfInt(drDataRecord["UserList2_ID"]) : 0)) &&
                                              (x.UserElement1_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement1) ? Util.GetValueOfInt(drDataRecord["UserElement1_ID"]) : 0)) &&
                                              (x.UserElement2_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement2) ? Util.GetValueOfInt(drDataRecord["UserElement2_ID"]) : 0)) &&
                                              (x.UserElement3_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement3) ? Util.GetValueOfInt(drDataRecord["UserElement3_ID"]) : 0)) &&
                                              (x.UserElement4_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement4) ? Util.GetValueOfInt(drDataRecord["UserElement4_ID"]) : 0)) &&
                                              (x.UserElement5_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement5) ? Util.GetValueOfInt(drDataRecord["UserElement5_ID"]) : 0)) &&
                                              (x.UserElement6_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement6) ? Util.GetValueOfInt(drDataRecord["UserElement6_ID"]) : 0)) &&
                                              (x.UserElement7_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement7) ? Util.GetValueOfInt(drDataRecord["UserElement7_ID"]) : 0)) &&
                                              (x.UserElement8_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement8) ? Util.GetValueOfInt(drDataRecord["UserElement8_ID"]) : 0)) &&
                                              (x.UserElement9_ID == (selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement9) ? Util.GetValueOfInt(drDataRecord["UserElement9_ID"]) : 0))
                                             );
                _budgetControl.ControlledAmount = Decimal.Subtract(_budgetControl.ControlledAmount, Util.GetValueOfDecimal(drDataRecord["Debit"]));
                if (_budgetControl.ControlledAmount < 0)
                {
                    if (!_budgetMessage.Contains(Util.GetValueOfString(drBUdgetControl["BudgetName"])))
                    {
                        _budgetMessage += Util.GetValueOfString(drBUdgetControl["BudgetName"]) + " - "
                                            + Util.GetValueOfString(drBUdgetControl["ControlName"]) + ", ";
                    }
                    log.Info("Budget control Exceed - " + Util.GetValueOfString(drBUdgetControl["BudgetName"]) + " - "
                                        + Util.GetValueOfString(drBUdgetControl["ControlName"]) + " - (" + _budgetControl.ControlledAmount + ") - Table ID : " +
                                        Util.GetValueOfInt(drDataRecord["LineTable_ID"]) + " - Record ID : " + Util.GetValueOfInt(drDataRecord["Line_ID"]));
                }
            }
            else
            {
                if (_listBudgetControl.Exists(x => (x.VAGL_Budget_ID == Util.GetValueOfInt(drBUdgetControl["VAGL_Budget_ID"])) &&
                                             (x.VAGL_BudgetActivation_ID == Util.GetValueOfInt(drBUdgetControl["VAGL_BudgetActivation_ID"])) &&
                                             (x.Account_ID == Util.GetValueOfInt(drDataRecord["Account_ID"]))
                                            ))
                {
                    if (!_budgetMessage.Contains(Util.GetValueOfString(drBUdgetControl["BudgetName"])))
                    {
                        _budgetMessage += Util.GetValueOfString(drBUdgetControl["BudgetName"]) + " - "
                                            + Util.GetValueOfString(drBUdgetControl["ControlName"]) + ", ";
                    }
                    log.Info("Budget control not defined for - " + Util.GetValueOfString(drBUdgetControl["BudgetName"]) + " - "
                                        + Util.GetValueOfString(drBUdgetControl["ControlName"]) + " - Table ID : " +
                                        Util.GetValueOfInt(drDataRecord["LineTable_ID"]) + " - Record ID : " + Util.GetValueOfInt(drDataRecord["Line_ID"]) +
                                        " - Account ID : " + Util.GetValueOfInt(drDataRecord["Account_ID"]));
                }
            }

            return _listBudgetControl;
        }

        /// <summary>
        /// This Function is used to get those dimension which is not to controlled
        /// </summary>
        /// <param name="selectedDimension">controlled dimension</param>
        /// <returns>where Condition</returns>
        public String GetNonSelectedDimension(List<String> selectedDimension)
        {
            String where = "";
            String sql = @" SELECT VAF_CtrlRef_List.Value FROM VAF_Control_Ref INNER JOIN VAF_CtrlRef_List ON VAF_CtrlRef_List.VAF_Control_Ref_ID = VAF_Control_Ref.VAF_Control_Ref_ID 
                            WHERE  VAF_Control_Ref.VAF_Control_Ref_ID=181 AND VAF_CtrlRef_List.Value NOT IN ('AC' , 'SA')";
            DataSet dsElementType = DB.ExecuteDataset(sql, null, null);
            if (dsElementType != null && dsElementType.Tables.Count > 0)
            {
                for (int i = 0; i < dsElementType.Tables[0].Rows.Count; i++)
                {
                    if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_BPartner)
                        && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_BPartner))
                    {
                        where += " AND NVL(VAB_BusinessPartner_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_Product)
                        && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Product))
                    {
                        where += " AND NVL(VAM_Product_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_Activity)
                        && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Activity))
                    {
                        where += " AND NVL(VAB_BillingCode_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationFrom)
                        && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationFrom))
                    {
                        where += " AND NVL(VAB_LocFrom_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationTo)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_LocationTo))
                    {
                        where += " AND NVL(VAB_LocTo_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_Campaign)
                      && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Campaign))
                    {
                        where += " AND NVL(VAB_Promotion_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_OrgTrx)
                      && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_OrgTrx))
                    {
                        where += " AND NVL(VAF_OrgTrx_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_Project)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_Project))
                    {
                        where += " AND NVL(VAB_Project_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_SalesRegion)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_SalesRegion))
                    {
                        where += " AND NVL(VAB_SalesRegionState_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement1)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement1))
                    {
                        where += " AND NVL(UserElement1_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement2)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement2))
                    {
                        where += " AND NVL(UserElement2_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement3)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement3))
                    {
                        where += " AND NVL(UserElement3_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement4)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement4))
                    {
                        where += " AND NVL(UserElement4_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement5)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement5))
                    {
                        where += " AND NVL(UserElement5_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement6)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement6))
                    {
                        where += " AND NVL(UserElement6_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement7)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement7))
                    {
                        where += " AND NVL(UserElement7_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement8)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement8))
                    {
                        where += " AND NVL(UserElement8_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement9)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserElement9))
                    {
                        where += " AND NVL(UserElement9_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList1)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList1))
                    {
                        where += " AND NVL(User1_ID, 0) = 0 ";
                    }
                    else if (Util.GetValueOfString(dsElementType.Tables[0].Rows[i]["Value"]).Equals(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList2)
                       && !selectedDimension.Contains(X_VAB_AccountBook_Element.ELEMENTTYPE_UserList2))
                    {
                        where += " AND NVL(User2_ID, 0) = 0 ";
                    }
                }
            }
            return where;
        }

        /// <summary>
        /// This function is used to create default record
        /// </summary>
        /// <param name="budget_id"></param>
        /// <param name="budgetControl_Id"></param>
        /// <param name="acctSchema_ID"></param>
        /// <param name="account_id"></param>
        public void CheckOrCreateDefault(int budget_id, int budgetControl_Id, int acctSchema_ID, int account_id, List<BudgetControl> _listBudgetControl)
        {
            BudgetControl budgetControl = null;
            if (!_listBudgetControl.Exists(x => (x.VAGL_Budget_ID == budget_id) &&
                                             (x.VAGL_BudgetActivation_ID == budgetControl_Id) &&
                                             (x.Account_ID == account_id) &&
                                             (x.VAF_Org_ID == 0) &&
                                             (x.VAB_BusinessPartner_ID == 0) &&
                                             (x.VAM_Product_ID == 0) &&
                                             (x.VAB_BillingCode_ID == 0) &&
                                             (x.VAB_AddressFrom_ID == 0) &&
                                             (x.VAB_AddressTo_ID == 0) &&
                                             (x.VAB_Promotion_ID == 0) &&
                                             (x.VAF_OrgTrx_ID == 0) &&
                                             (x.VAB_Project_ID == 0) &&
                                             (x.VAB_SalesRegionState_ID == 0) &&
                                             (x.UserList1_ID == 0) &&
                                             (x.UserList2_ID == 0) &&
                                             (x.UserElement1_ID == 0) &&
                                             (x.UserElement2_ID == 0) &&
                                             (x.UserElement3_ID == 0) &&
                                             (x.UserElement4_ID == 0) &&
                                             (x.UserElement5_ID == 0) &&
                                             (x.UserElement6_ID == 0) &&
                                             (x.UserElement7_ID == 0) &&
                                             (x.UserElement8_ID == 0) &&
                                             (x.UserElement9_ID == 0)
                                            ))
            {
                budgetControl = new BudgetControl();
                budgetControl.VAGL_Budget_ID = budget_id;
                budgetControl.VAGL_BudgetActivation_ID = budgetControl_Id;
                budgetControl.VAB_AccountBook_ID = acctSchema_ID;
                budgetControl.Account_ID = account_id;
                budgetControl.VAF_Org_ID = 0;
                budgetControl.VAM_Product_ID = 0;
                budgetControl.VAB_BusinessPartner_ID = 0;
                budgetControl.VAB_BillingCode_ID = 0;
                budgetControl.VAB_AddressFrom_ID = 0;
                budgetControl.VAB_AddressTo_ID = 0;
                budgetControl.VAB_Promotion_ID = 0;
                budgetControl.VAF_OrgTrx_ID = 0;
                budgetControl.VAB_Project_ID = 0;
                budgetControl.VAB_SalesRegionState_ID = 0;
                budgetControl.UserList1_ID = 0;
                budgetControl.UserList2_ID = 0;
                budgetControl.UserElement1_ID = 0;
                budgetControl.UserElement2_ID = 0;
                budgetControl.UserElement3_ID = 0;
                budgetControl.UserElement4_ID = 0;
                budgetControl.UserElement5_ID = 0;
                budgetControl.UserElement6_ID = 0;
                budgetControl.UserElement7_ID = 0;
                budgetControl.UserElement8_ID = 0;
                budgetControl.UserElement9_ID = 0;
                budgetControl.ControlledAmount = 0;
                _listBudgetControl.Add(budgetControl);
            }
        }

        /// <summary>
        /// Set the document number from Completed Document Sequence after completed
        /// </summary>
        protected void SetCompletedDocumentNo()
        {
            // if Re-Activated document then no need to get Document no from Completed sequence
            if (Get_ColumnIndex("IsReActivated") > 0 && IsReActivated())
            {
                return;
            }

            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            // if Overwrite Date on Complete checkbox is true.
            if (dt.IsOverwriteDateOnComplete())
            {
                SetDateOrdered(DateTime.Now.Date);
                if (GetDateAcct().Value.Date < GetDateOrdered().Value.Date)
                {
                    SetDateAcct(GetDateOrdered());

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
        /// Create Exepected Landed cost distribution lines
        /// </summary>
        /// <returns>if success then empty string else message</returns>
        protected String ExpectedlandedCostDistribution()
        {
            MVABExpectedCost[] expectedlandedCosts = MVABExpectedCost.GetLines(GetCtx(), GetVAB_Order_ID(), Get_Trx());
            if (expectedlandedCosts != null && expectedlandedCosts.Length > 0)
            {
                for (int i = 0; i < expectedlandedCosts.Length; i++)
                {
                    String error = expectedlandedCosts[i].DistributeLandedCost();
                    if (!Util.IsEmpty(error))
                        return error;
                }
            }
            return "";
        }

        //Changes by abhishek suggested by lokesh on 7/1/2016
        //public void SaveDayEndRecord(Ctx ctx, int Terminal_ID, string CashAmt, string CreditAmt, int DocType_ID, string[] Tax_ID, string[] TaxAmts, string OrderTotal, string[] DiscountLine)
        //{
        //    int DayEnd_ID = Util.GetValueOfInt(DB.ExecuteScalar("select vapos_dayendreport_id from vapos_dayendreport where to_date(vapos_trxdate, 'DD-MM-YYYY') =to_date(sysdate, 'DD-MM-YYYY') and vapos_posterminal_id=" + Terminal_ID));
        //    if (DayEnd_ID > 0)
        //    {
        //        // Addition in existing record
        //        ViennaAdvantage.Model.MVAPOSDayEndReport DayEndRep = new ViennaAdvantage.Model.MVAPOSDayEndReport(ctx, DayEnd_ID, null);
        //        if (Util.GetValueOfDecimal(CashAmt) > 0)
        //        {
        //            DayEndRep.SetVAPOS_CashPaymrnt(Decimal.Add(DayEndRep.GetVAPOS_CashPaymrnt(), Util.GetValueOfDecimal(CashAmt)));
        //            DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CashAmt)));
        //        }
        //        if (Util.GetValueOfDecimal(CreditAmt) > 0)
        //        {
        //            DayEndRep.SetVAPOS_CreditCardPay(Decimal.Add(DayEndRep.GetVAPOS_CreditCardPay(), Util.GetValueOfDecimal(CreditAmt)));
        //            DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CreditAmt)));
        //        }
        //        string OrderType = Util.GetValueOfString(DB.ExecuteScalar("select vapos_ordertype from VAB_DocTypes where VAB_DocTypes_id=" + DocType_ID));
        //        if (OrderType == "H")
        //        {
        //            DayEndRep.SetVAPOS_HmeDelivery(Decimal.Add(DayEndRep.GetVAPOS_HmeDelivery(), Util.GetValueOfDecimal(OrderTotal)));

        //        }
        //        else if (OrderType == "P")
        //        {
        //            DayEndRep.SetVAPOS_PickOrder(Decimal.Add(DayEndRep.GetVAPOS_PickOrder(), Util.GetValueOfDecimal(OrderTotal)));
        //        }
        //        else if (OrderType == "R")
        //        {
        //            DayEndRep.SetVAPOS_Return(Decimal.Add(DayEndRep.GetVAPOS_Return(), Util.GetValueOfDecimal(OrderTotal)));
        //        }
        //        else if (OrderType == "W")
        //        {
        //            DayEndRep.SetVAPOS_WHOrder(Decimal.Add(DayEndRep.GetVAPOS_WHOrder(), Util.GetValueOfDecimal(OrderTotal)));
        //        }
        //        DayEndRep.SetVAPOS_OrderGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_OrderGrandTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //        DayEndRep.SetVAPOS_SalesOSubTotal(Decimal.Add(DayEndRep.GetVAPOS_SalesOSubTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //        DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(OrderTotal)));

        //        for (int i = 0; i < DiscountLine.Length; i++)
        //        {
        //            DayEndRep.SetVAPOS_Discount(Decimal.Add(DayEndRep.GetVAPOS_Discount(), Util.GetValueOfDecimal(DiscountLine[i])));
        //            DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(DiscountLine[i])));
        //        }
        //        if (DayEndRep.Save())
        //        {
        //            for (int j = 0; j < Tax_ID.Length; j++)
        //            {
        //                int DayEndTax_ID = Util.GetValueOfInt(DB.ExecuteScalar("select vapos_dayendreporttax_ID from vapos_dayendreporttax where vapos_dayendreport_id=" + DayEndRep.GetVAPOS_DayEndReport_ID() + " and VAB_TaxRate_id=" + Util.GetValueOfInt(Tax_ID[j])));
        //                if (DayEndTax_ID > 0)
        //                {
        //                    ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, DayEndTax_ID, null);
        //                    EndDayTax.SetVAPOS_TaxAmount(Decimal.Add(EndDayTax.GetVAPOS_TaxAmount(), Util.GetValueOfDecimal(TaxAmts[j])));
        //                    if (EndDayTax.Save())
        //                    {

        //                    }
        //                }
        //                else
        //                {
        //                    ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, 0, null);
        //                    EndDayTax.SetVAPOS_DayEndReport_ID(DayEndRep.GetVAPOS_DayEndReport_ID());
        //                    EndDayTax.SetVAB_TaxRate_ID(Util.GetValueOfInt(Tax_ID[j]));
        //                    EndDayTax.SetVAPOS_TaxAmount(Util.GetValueOfDecimal(TaxAmts[j]));
        //                    if (EndDayTax.Save())
        //                    {

        //                    }
        //                }
        //            }

        //        }
        //    }
        //    else
        //    {
        //        //New Records On First Tab Of Day End
        //        ViennaAdvantage.Model.MVAPOSDayEndReport DayEndRep = new ViennaAdvantage.Model.MVAPOSDayEndReport(ctx, 0, null);
        //        DayEndRep.SetVAPOS_POSTerminal_ID(Util.GetValueOfInt(Terminal_ID));
        //        DayEndRep.SetVAPOS_TrxDate((DateTime.Now.ToLocalTime()));
        //        if (Util.GetValueOfDecimal(CashAmt) > 0)
        //        {
        //            DayEndRep.SetVAPOS_CashPaymrnt(Util.GetValueOfDecimal(CashAmt));
        //            DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CashAmt)));
        //        }
        //        if (Util.GetValueOfDecimal(CreditAmt) > 0)
        //        {
        //            DayEndRep.SetVAPOS_CreditCardPay(Util.GetValueOfDecimal(CreditAmt));
        //            DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CreditAmt)));
        //        }
        //        string OrderType = Util.GetValueOfString(DB.ExecuteScalar("select vapos_ordertype from VAB_DocTypes where VAB_DocTypes_id=" + DocType_ID));
        //        if (OrderType == "H")
        //        {
        //            DayEndRep.SetVAPOS_HmeDelivery(Util.GetValueOfDecimal(OrderTotal));
        //        }
        //        else if (OrderType == "P")
        //        {
        //            DayEndRep.SetVAPOS_PickOrder(Util.GetValueOfDecimal(OrderTotal));
        //        }
        //        else if (OrderType == "R")
        //        {
        //            DayEndRep.SetVAPOS_Return(Util.GetValueOfDecimal(OrderTotal));
        //        }
        //        else if (OrderType == "W")
        //        {
        //            DayEndRep.SetVAPOS_WHOrder(Util.GetValueOfDecimal(OrderTotal));
        //        }
        //        DayEndRep.SetVAPOS_OrderGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_OrderGrandTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //        DayEndRep.SetVAPOS_SalesOSubTotal(Util.GetValueOfDecimal(OrderTotal));
        //        DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(OrderTotal)));
        //        for (int i = 0; i < DiscountLine.Length; i++)
        //        {
        //            DayEndRep.SetVAPOS_Discount(Decimal.Add(DayEndRep.GetVAPOS_Discount(), Util.GetValueOfDecimal(DiscountLine[i])));
        //            DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(DiscountLine[i])));
        //        }
        //        if (DayEndRep.Save())
        //        {
        //            for (int j = 0; j < Tax_ID.Length; j++)
        //            {
        //                int DayEndTax_ID = Util.GetValueOfInt(DB.ExecuteScalar("select vapos_dayendreporttax_ID from vapos_dayendreporttax where vapos_dayendreport_id=" + DayEndRep.GetVAPOS_DayEndReport_ID() + " and VAB_TaxRate_id=" + Tax_ID[j]));
        //                if (DayEndTax_ID > 0)
        //                {
        //                    ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, DayEndTax_ID, null);
        //                    EndDayTax.SetVAPOS_TaxAmount(Decimal.Add(EndDayTax.GetVAPOS_TaxAmount(), Util.GetValueOfDecimal(TaxAmts[j])));
        //                    if (EndDayTax.Save())
        //                    {
        //                    }
        //                }
        //                else
        //                {
        //                    ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, 0, null);
        //                    EndDayTax.SetVAPOS_DayEndReport_ID(DayEndRep.GetVAPOS_DayEndReport_ID());
        //                    EndDayTax.SetVAB_TaxRate_ID(Util.GetValueOfInt(Tax_ID[j]));
        //                    EndDayTax.SetVAPOS_TaxAmount(Util.GetValueOfDecimal(TaxAmts[j]));
        //                    if (EndDayTax.Save())
        //                    {
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //Changes by abhishek suggested by lokesh on 7/1/2016
        //public void SaveDayEndRecord(Ctx ctx, int Terminal_ID, string CashAmt, string CreditAmt, int DocType_ID, string[] Tax_ID, string[] TaxAmts, string OrderTotal, string[] DiscountLine)
        //{
        //    MVAFTableView tblDayEnd = new MVAFTableView(ctx, Util.GetValueOfInt(DB.ExecuteScalar("select VAF_TableView_ID from vaf_tableview where  tablename = 'VAPOS_DayEndReport'")), null);

        //    int DayEnd_ID = Util.GetValueOfInt(DB.ExecuteScalar("select vapos_dayendreport_id from vapos_dayendreport where to_date(vapos_trxdate, 'DD-MM-YYYY') =to_date(sysdate, 'DD-MM-YYYY') and vapos_posterminal_id=" + Terminal_ID));
        //    if (DayEnd_ID > 0)
        //    {
        //        PO poDayEnd = tblDayEnd.GetPO(ctx, DayEnd_ID, null);
        //        if (Math.Abs(Util.GetValueOfDecimal(CashAmt)) > 0)
        //        {
        //            //DayEndRep.SetVAPOS_CashPaymrnt(Decimal.Add(DayEndRep.GetVAPOS_CashPaymrnt(), Util.GetValueOfDecimal(CashAmt)));
        //            //DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CashAmt)));
        //            poDayEnd.Set_Value("VAPOS_CashPaymrnt", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_CashPaymrnt")), Util.GetValueOfDecimal(CashAmt)));
        //            poDayEnd.Set_Value("VAPOS_PayGrandTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_PayGrandTotal")), Util.GetValueOfDecimal(CashAmt)));
        //        }
        //        if (Util.GetValueOfDecimal(CreditAmt) > 0)
        //        {
        //            //DayEndRep.SetVAPOS_CreditCardPay(Decimal.Add(DayEndRep.GetVAPOS_CreditCardPay(), Util.GetValueOfDecimal(CreditAmt)));
        //            //DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CreditAmt)));
        //            poDayEnd.Set_Value("VAPOS_CreditCardPay", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_CreditCardPay")), Util.GetValueOfDecimal(CreditAmt)));
        //            poDayEnd.Set_Value("VAPOS_PayGrandTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_PayGrandTotal")), Util.GetValueOfDecimal(CreditAmt)));
        //        }
        //        string OrderType = Util.GetValueOfString(DB.ExecuteScalar("select vapos_ordertype from VAB_DocTypes where VAB_DocTypes_id=" + DocType_ID));
        //        if (OrderType == "H")
        //        {
        //            //DayEndRep.SetVAPOS_HmeDelivery(Decimal.Add(DayEndRep.GetVAPOS_HmeDelivery(), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_HmeDelivery", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_HmeDelivery")), Util.GetValueOfDecimal(OrderTotal)));

        //        }
        //        else if (OrderType == "P")
        //        {
        //            //DayEndRep.SetVAPOS_PickOrder(Decimal.Add(DayEndRep.GetVAPOS_PickOrder(), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_PickOrder", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_PickOrder")), Util.GetValueOfDecimal(OrderTotal)));
        //        }
        //        else if (OrderType == "R")
        //        {
        //            //DayEndRep.SetVAPOS_Return(Decimal.Add(DayEndRep.GetVAPOS_Return(), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_Return", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_Return")), Util.GetValueOfDecimal(OrderTotal)));
        //        }
        //        else if (OrderType == "W")
        //        {
        //            //DayEndRep.SetVAPOS_WHOrder(Decimal.Add(DayEndRep.GetVAPOS_WHOrder(), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_WHOrder", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_WHOrder")), Util.GetValueOfDecimal(OrderTotal)));
        //        }
        //        //DayEndRep.SetVAPOS_OrderGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_OrderGrandTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //        //DayEndRep.SetVAPOS_SalesOSubTotal(Decimal.Add(DayEndRep.GetVAPOS_SalesOSubTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //        //DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(OrderTotal)));
        //        poDayEnd.Set_Value("VAPOS_OrderGrandTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_OrderGrandTotal")), Util.GetValueOfDecimal(OrderTotal)));
        //        poDayEnd.Set_Value("VAPOS_SalesOSubTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_SalesOSubTotal")), Util.GetValueOfDecimal(OrderTotal)));
        //        poDayEnd.Set_Value("VAPOS_OSummaryGrandTot", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_OSummaryGrandTot")), Util.GetValueOfDecimal(OrderTotal)));

        //        for (int i = 0; i < DiscountLine.Length; i++)
        //        {
        //            //DayEndRep.SetVAPOS_Discount(Decimal.Add(DayEndRep.GetVAPOS_Discount(), Util.GetValueOfDecimal(DiscountLine[i])));
        //            //DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(DiscountLine[i])));
        //            poDayEnd.Set_Value("VAPOS_Discount", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_Discount")), Util.GetValueOfDecimal(DiscountLine[i])));
        //            poDayEnd.Set_Value("VAPOS_OSummaryGrandTot", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_OSummaryGrandTot")), Util.GetValueOfDecimal(DiscountLine[i])));
        //        }
        //        if (poDayEnd.Save())
        //        {
        //            for (int j = 0; j < Tax_ID.Length; j++)
        //            {
        //                MVAFTableView tblTax = new MVAFTableView(ctx, Util.GetValueOfInt(DB.ExecuteScalar("select VAF_TableView_ID from vaf_tableview where  tablename = 'VAPOS_DayEndReportTax'")), null);
        //                int DayEndTax_ID = Util.GetValueOfInt(DB.ExecuteScalar("select vapos_dayendreporttax_ID from vapos_dayendreporttax where vapos_dayendreport_id=" + Util.GetValueOfInt(poDayEnd.Get_Value("VAPOS_DayEndReport_ID")) + " and VAB_TaxRate_id=" + Util.GetValueOfInt(Tax_ID[j])));
        //                if (DayEndTax_ID > 0)
        //                {
        //                    //ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, DayEndTax_ID, null);
        //                    //EndDayTax.SetVAPOS_TaxAmount(Decimal.Add(EndDayTax.GetVAPOS_TaxAmount(), Util.GetValueOfDecimal(TaxAmts[j])));
        //                    PO poDayEndTax = tblDayEnd.GetPO(ctx, DayEndTax_ID, null);
        //                    poDayEndTax.Set_Value("VAPOS_TaxAmount", Decimal.Add(Util.GetValueOfDecimal(poDayEndTax.Get_Value("VAPOS_TaxAmount")), Util.GetValueOfDecimal(TaxAmts[j])));
        //                    if (poDayEndTax.Save())
        //                    {

        //                    }
        //                }
        //                else
        //                {
        //                    //ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, 0, null);
        //                    //EndDayTax.SetVAPOS_DayEndReport_ID(DayEndRep.GetVAPOS_DayEndReport_ID());
        //                    //EndDayTax.SetVAB_TaxRate_ID(Util.GetValueOfInt(Tax_ID[j]));
        //                    //EndDayTax.SetVAPOS_TaxAmount(Util.GetValueOfDecimal(TaxAmts[j]));
        //                    PO poDayEndTax = tblDayEnd.GetPO(ctx, 0, null);
        //                    poDayEndTax.Set_Value("VAPOS_TaxAmount", Util.GetValueOfDecimal(TaxAmts[j]));
        //                    poDayEndTax.Set_Value("VAPOS_DayEndReport_ID", poDayEnd.Get_Value("VAPOS_DayEndReport_ID"));
        //                    poDayEndTax.Set_Value("VAB_TaxRate_ID", Util.GetValueOfInt(Tax_ID[j]));

        //                    if (poDayEndTax.Save())
        //                    {

        //                    }
        //                }
        //            }

        //        }
        //        else
        //        {
        //            poDayEnd = tblDayEnd.GetPO(ctx, 0, null);
        //            poDayEnd.Set_Value("VAPOS_POSTerminal_ID", Util.GetValueOfInt(Terminal_ID));
        //            poDayEnd.Set_Value("VAPOS_TrxDate", (DateTime.Now.ToLocalTime()));
        //            if (Math.Abs(Util.GetValueOfDecimal(CashAmt)) > 0)
        //            {
        //                //DayEndRep.SetVAPOS_CashPaymrnt(Decimal.Add(DayEndRep.GetVAPOS_CashPaymrnt(), Util.GetValueOfDecimal(CashAmt)));
        //                //DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CashAmt)));
        //                poDayEnd.Set_Value("VAPOS_CashPaymrnt", Util.GetValueOfDecimal(CashAmt));
        //                poDayEnd.Set_Value("VAPOS_PayGrandTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_PayGrandTotal")), Util.GetValueOfDecimal(CashAmt)));
        //            }
        //            if (Util.GetValueOfDecimal(CreditAmt) > 0)
        //            {
        //                //DayEndRep.SetVAPOS_CreditCardPay(Decimal.Add(DayEndRep.GetVAPOS_CreditCardPay(), Util.GetValueOfDecimal(CreditAmt)));
        //                //DayEndRep.SetVAPOS_PayGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_PayGrandTotal(), Util.GetValueOfDecimal(CreditAmt)));
        //                poDayEnd.Set_Value("VAPOS_CreditCardPay", Util.GetValueOfDecimal(CreditAmt));
        //                poDayEnd.Set_Value("VAPOS_PayGrandTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_PayGrandTotal")), Util.GetValueOfDecimal(CreditAmt)));
        //            }
        //            OrderType = Util.GetValueOfString(DB.ExecuteScalar("select vapos_ordertype from VAB_DocTypes where VAB_DocTypes_id=" + DocType_ID));
        //            if (OrderType == "H")
        //            {
        //                //DayEndRep.SetVAPOS_HmeDelivery(Decimal.Add(DayEndRep.GetVAPOS_HmeDelivery(), Util.GetValueOfDecimal(OrderTotal)));
        //                poDayEnd.Set_Value("VAPOS_HmeDelivery", Util.GetValueOfDecimal(OrderTotal));

        //            }
        //            else if (OrderType == "P")
        //            {
        //                //DayEndRep.SetVAPOS_PickOrder(Decimal.Add(DayEndRep.GetVAPOS_PickOrder(), Util.GetValueOfDecimal(OrderTotal)));
        //                poDayEnd.Set_Value("VAPOS_PickOrder", Util.GetValueOfDecimal(OrderTotal));
        //            }
        //            else if (OrderType == "R")
        //            {
        //                //DayEndRep.SetVAPOS_Return(Decimal.Add(DayEndRep.GetVAPOS_Return(), Util.GetValueOfDecimal(OrderTotal)));
        //                poDayEnd.Set_Value("VAPOS_Return", Util.GetValueOfDecimal(OrderTotal));
        //            }
        //            else if (OrderType == "W")
        //            {
        //                //DayEndRep.SetVAPOS_WHOrder(Decimal.Add(DayEndRep.GetVAPOS_WHOrder(), Util.GetValueOfDecimal(OrderTotal)));
        //                poDayEnd.Set_Value("VAPOS_WHOrder", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_WHOrder")), Util.GetValueOfDecimal(OrderTotal)));
        //            }
        //            //DayEndRep.SetVAPOS_OrderGrandTotal(Decimal.Add(DayEndRep.GetVAPOS_OrderGrandTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //            //DayEndRep.SetVAPOS_SalesOSubTotal(Decimal.Add(DayEndRep.GetVAPOS_SalesOSubTotal(), Util.GetValueOfDecimal(OrderTotal)));
        //            //DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_OrderGrandTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_OrderGrandTotal")), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_SalesOSubTotal", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_SalesOSubTotal")), Util.GetValueOfDecimal(OrderTotal)));
        //            poDayEnd.Set_Value("VAPOS_OSummaryGrandTot", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_OSummaryGrandTot")), Util.GetValueOfDecimal(OrderTotal)));

        //            for (int i = 0; i < DiscountLine.Length; i++)
        //            {
        //                //DayEndRep.SetVAPOS_Discount(Decimal.Add(DayEndRep.GetVAPOS_Discount(), Util.GetValueOfDecimal(DiscountLine[i])));
        //                //DayEndRep.SetVAPOS_OSummaryGrandTot(Decimal.Add(DayEndRep.GetVAPOS_OSummaryGrandTot(), Util.GetValueOfDecimal(DiscountLine[i])));
        //                poDayEnd.Set_Value("VAPOS_Discount", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_Discount")), Util.GetValueOfDecimal(DiscountLine[i])));
        //                poDayEnd.Set_Value("VAPOS_OSummaryGrandTot", Decimal.Add(Util.GetValueOfDecimal(poDayEnd.Get_Value("VAPOS_OSummaryGrandTot")), Util.GetValueOfDecimal(DiscountLine[i])));
        //            }
        //            if (poDayEnd.Save())
        //            {
        //                for (int j = 0; j < Tax_ID.Length; j++)
        //                {
        //                    MVAFTableView tblTax = new MVAFTableView(ctx, Util.GetValueOfInt(DB.ExecuteScalar("select VAF_TableView_ID from vaf_tableview where  tablename = 'VAPOS_DayEndReportTax'")), null);
        //                    int DayEndTax_ID = Util.GetValueOfInt(DB.ExecuteScalar("select vapos_dayendreporttax_ID from vapos_dayendreporttax where vapos_dayendreport_id=" + Util.GetValueOfInt(poDayEnd.Get_Value("VAPOS_DayEndReport_ID")) + " and VAB_TaxRate_id=" + Util.GetValueOfInt(Tax_ID[j])));
        //                    if (DayEndTax_ID > 0)
        //                    {
        //                        //ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, DayEndTax_ID, null);
        //                        //EndDayTax.SetVAPOS_TaxAmount(Decimal.Add(EndDayTax.GetVAPOS_TaxAmount(), Util.GetValueOfDecimal(TaxAmts[j])));
        //                        PO poDayEndTax = tblDayEnd.GetPO(ctx, DayEndTax_ID, null);
        //                        poDayEndTax.Set_Value("VAPOS_TaxAmount", Decimal.Add(Util.GetValueOfDecimal(poDayEndTax.Get_Value("VAPOS_TaxAmount")), Util.GetValueOfDecimal(TaxAmts[j])));
        //                        if (poDayEndTax.Save())
        //                        {

        //                        }
        //                    }
        //                    else
        //                    {
        //                        //ViennaAdvantage.Model.X_VAPOS_DayEndReportTax EndDayTax = new ViennaAdvantage.Model.X_VAPOS_DayEndReportTax(ctx, 0, null);
        //                        //EndDayTax.SetVAPOS_DayEndReport_ID(DayEndRep.GetVAPOS_DayEndReport_ID());
        //                        //EndDayTax.SetVAB_TaxRate_ID(Util.GetValueOfInt(Tax_ID[j]));
        //                        //EndDayTax.SetVAPOS_TaxAmount(Util.GetValueOfDecimal(TaxAmts[j]));
        //                        PO poDayEndTax = tblDayEnd.GetPO(ctx, 0, null);
        //                        poDayEndTax.Set_Value("VAPOS_TaxAmount", Util.GetValueOfDecimal(TaxAmts[j]));
        //                        poDayEndTax.Set_Value("VAPOS_DayEndReport_ID", poDayEnd.Get_Value("VAPOS_DayEndReport_ID"));
        //                        poDayEndTax.Set_Value("VAB_TaxRate_ID", Util.GetValueOfInt(Tax_ID[j]));

        //                        if (poDayEndTax.Save())
        //                        {

        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        /* 	Create Shipment
        *	@param dt order document type
        *	@param movementDate optional movement date (default today)
        *	@return shipment or null
        */
        private MVAMInvInOut CreateShipment(MVABDocTypes dt, DateTime? movementDate)
        {
            MVAMInvInOut shipment = new MVAMInvInOut(this, (int)dt.GetVAB_DocTypesShipment_ID(), (DateTime?)movementDate);
            String DocSubTypeSO = dt.GetDocSubTypeSO();
            if (MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))
            {
                if (Util.GetValueOfInt(GetVAPOS_POSTerminal_ID()) != 0)
                {
                    shipment.SetDocumentNo(GetDocumentNo());
                }
            }
            log.Info("For " + dt);
            bool IsDrafted = false;
            try
            {
                //	shipment.setDateAcct(getDateAcct());
                if (!shipment.Save(Get_TrxName()))
                {
                    _processMsg = "Could not create Shipment";
                    return null;
                }
                //
                String posStatus = "";
                MVAMWarehouse wh = null;
                MVABOrderLine[] oLines = GetLines(true, null);
                for (int i = 0; i < oLines.Length; i++)
                {

                    MVABOrderLine oLine = oLines[i];
                    if (Util.GetValueOfInt(GetVAPOS_POSTerminal_ID()) > 0)
                    {
                        #region POS Terminal > 0
                        MVAMInvInOutLine ioLine = new MVAMInvInOutLine(shipment);
                        //	Qty = Ordered - Delivered
                        Decimal MovementQty = Decimal.Subtract(oLine.GetQtyOrdered(), oLine.GetQtyDelivered());
                        //	Location
                        int VAM_Locator_ID = MVAMStorage.GetVAM_Locator_ID(oLine.GetVAM_Warehouse_ID(),
                                oLine.GetVAM_Product_ID(), oLine.GetVAM_PFeature_SetInstance_ID(),
                                MovementQty, Get_TrxName());
                        if (VAM_Locator_ID == 0)      //	Get default Location
                        {
                            MVAMProduct product = ioLine.GetProduct();
                            int VAM_Warehouse_ID = oLine.GetVAM_Warehouse_ID();
                            VAM_Locator_ID = MVAMProductLocator.GetFirstVAM_Locator_ID(product, VAM_Warehouse_ID);
                            if (VAM_Locator_ID == 0)
                            {
                                wh = MVAMWarehouse.Get(GetCtx(), VAM_Warehouse_ID);
                                VAM_Locator_ID = wh.GetDefaultVAM_Locator_ID();
                            }
                        }
                        //
                        ioLine.SetOrderLine(oLine, VAM_Locator_ID, MovementQty);
                        ioLine.SetQty(MovementQty);
                        if (oLine.GetQtyEntered().CompareTo(oLine.GetQtyOrdered()) != 0)
                        {
                            ioLine.SetQtyEntered(Decimal.Multiply(MovementQty, (Decimal.Divide(oLine.GetQtyEntered(), (oLine.GetQtyOrdered())))));
                        }
                        if (!ioLine.Save(Get_TrxName()))
                        {
                            ValueNamePair pp = VLogger.RetrieveError();
                            if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                                _processMsg = "Could not create Shipment Line. " + pp.GetName();
                            else
                                _processMsg = "Could not create Shipment Line";
                            return null;
                        }
                        #endregion
                    }
                    else
                    {
                        // when order line created with charge OR with Product which is not of "item type" then not to create shipment line against this.
                        MVAMProduct oproduct = oLine.GetProduct();

                        //Create Lines for Charge / (Resource - Service - Expense) type product based on setting on Tenant to "Allow Non Item type".
                        if ((oproduct == null || !(oproduct != null && oproduct.GetProductType() == MVAMProduct.PRODUCTTYPE_Item))
                            && (Util.GetValueOfString(GetCtx().GetContext("$AllowNonItem")).Equals("N")))
                            continue;

                        //
                        int VAM_Warehouse_ID = oLine.GetVAM_Warehouse_ID();
                        wh = MVAMWarehouse.Get(GetCtx(), VAM_Warehouse_ID);

                        MVAMInvInOutLine ioLine = new MVAMInvInOutLine(shipment);

                        //	Qty = Ordered - Delivered
                        Decimal MovementQty = Decimal.Subtract(oLine.GetQtyOrdered(), oLine.GetQtyDelivered());

                        //	Location
                        int VAM_Locator_ID = MVAMStorage.GetVAM_Locator_ID(oLine.GetVAM_Warehouse_ID(),
                                oLine.GetVAM_Product_ID(), oLine.GetVAM_PFeature_SetInstance_ID(),
                                MovementQty, Get_TrxName());
                        if (VAM_Locator_ID == 0)      //	Get default Location
                        {
                            MVAMProduct product = ioLine.GetProduct();
                            VAM_Locator_ID = MVAMProductLocator.GetFirstVAM_Locator_ID(product, VAM_Warehouse_ID);
                            if (VAM_Locator_ID == 0)
                            {
                                wh = MVAMWarehouse.Get(GetCtx(), VAM_Warehouse_ID);
                                VAM_Locator_ID = wh.GetDefaultVAM_Locator_ID();
                            }
                        }

                        Decimal? QtyAvail = MVAMStorage.GetQtyAvailable(VAM_Warehouse_ID, oLine.GetVAM_Product_ID(), oLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                        if (MovementQty > 0)
                            QtyAvail += MovementQty;

                        String sql = "SELECT SUM(QtyOnHand) FROM VAM_Storage s INNER JOIN VAM_Locator l ON (s.VAM_Locator_ID=l.VAM_Locator_ID) WHERE s.VAM_Product_ID=" + oLine.GetVAM_Product_ID() + " AND l.VAM_Warehouse_ID=" + VAM_Warehouse_ID;
                        if (oLine.GetVAM_PFeature_SetInstance_ID() != 0)
                        {
                            sql += " AND VAM_PFeature_SetInstance_ID=" + oLine.GetVAM_PFeature_SetInstance_ID();
                        }
                        // check onhand qty on specified locator
                        if (VAM_Locator_ID > 0)
                        {
                            sql += " AND l.VAM_Locator_ID = " + VAM_Locator_ID;
                        }
                        OnHandQty = Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, Get_Trx()));
                        if (wh.IsDisallowNegativeInv() == true)
                        {
                            if (oLine.GetQtyOrdered() > QtyAvail && (DocSubTypeSO == "WR" || DocSubTypeSO == "WP"))
                            {
                                #region In Case of -- WR (WareHouse Order) / WP (POS Order)
                                // when qty avialble on warehouse is less than qty ordered, at that time we can create shipment in Drafetd stage, to be completed mnually
                                _processMsg = CreateShipmentLineContainer(shipment, ioLine, oLine, VAM_Locator_ID, MovementQty, wh.IsDisallowNegativeInv(), oproduct);

                                // when OnHand qty is less than qtyOrdered then not to create shipment (need to be rollback)
                                if (!String.IsNullOrEmpty(_processMsg) && OnHandQty < oLine.GetQtyOrdered())
                                {
                                    return null;
                                }

                                //ioLine.SetOrderLine(oLine, VAM_Locator_ID, MovementQty);
                                //ioLine.SetQty(MovementQty);
                                //if (oLine.GetQtyEntered().CompareTo(oLine.GetQtyOrdered()) != 0)
                                //{
                                //    //ioLine.SetQtyEntered(Decimal.Multiply(MovementQty,(oLine.getQtyEntered()).divide(oLine.getQtyOrdered(), 6, Decimal.ROUND_HALF_UP));
                                //    ioLine.SetQtyEntered(Decimal.Multiply(MovementQty, (Decimal.Divide(oLine.GetQtyEntered(), (oLine.GetQtyOrdered())))));
                                //}
                                //if (!ioLine.Save(Get_TrxName()))
                                //{
                                //    //_processMsg = "Could not create Shipment Line";
                                //    //return null;
                                //}
                                //	Manually Process Shipment
                                IsDrafted = true;
                                #endregion
                            }
                            else
                            {
                                #region In Case of -- Credit Order / PePay Order / WareHouse Order / POS Order

                                _processMsg = CreateShipmentLineContainer(shipment, ioLine, oLine, VAM_Locator_ID, MovementQty, wh.IsDisallowNegativeInv(), oproduct);
                                if (!String.IsNullOrEmpty(_processMsg))
                                {
                                    return null;
                                }

                                //ioLine.SetOrderLine(oLine, VAM_Locator_ID, MovementQty);
                                //ioLine.SetQty(MovementQty);
                                //if (oLine.GetQtyEntered().CompareTo(oLine.GetQtyOrdered()) != 0)
                                //{
                                //    //ioLine.SetQtyEntered(Decimal.Multiply(MovementQty,(oLine.getQtyEntered()).divide(oLine.getQtyOrdered(), 6, Decimal.ROUND_HALF_UP));
                                //    ioLine.SetQtyEntered(Decimal.Multiply(MovementQty, (Decimal.Divide(oLine.GetQtyEntered(), (oLine.GetQtyOrdered())))));
                                //}
                                //if (!ioLine.Save(Get_TrxName()))
                                //{
                                //    ValueNamePair pp = VLogger.RetrieveError();
                                //    if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                                //        _processMsg = "Could not create Shipment Line. " + pp.GetName();
                                //    else
                                //        _processMsg = "Could not create Shipment Line";
                                //    return null;
                                //}
                                #endregion
                            }
                        }
                        else
                        {
                            #region when disallow is FALSE
                            _processMsg = CreateShipmentLineContainer(shipment, ioLine, oLine, VAM_Locator_ID, MovementQty, wh.IsDisallowNegativeInv(), oproduct);
                            if (!String.IsNullOrEmpty(_processMsg))
                            {
                                return null;
                            }

                            //ioLine.SetOrderLine(oLine, VAM_Locator_ID, MovementQty);
                            //ioLine.SetQty(MovementQty);
                            //if (oLine.GetQtyEntered().CompareTo(oLine.GetQtyOrdered()) != 0)
                            //{
                            //    //ioLine.SetQtyEntered(Decimal.Multiply(MovementQty,(oLine.getQtyEntered()).divide(oLine.getQtyOrdered(), 6, Decimal.ROUND_HALF_UP));
                            //    ioLine.SetQtyEntered(Decimal.Multiply(MovementQty, (Decimal.Divide(oLine.GetQtyEntered(), (oLine.GetQtyOrdered())))));
                            //}
                            //if (!ioLine.Save(Get_TrxName()))
                            //{
                            //    ValueNamePair pp = VLogger.RetrieveError();
                            //    if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                            //        _processMsg = "Could not create Shipment Line. " + pp.GetName();
                            //    else
                            //        _processMsg = "Could not create Shipment Line";
                            //    return null;
                            //}
                            #endregion
                        }
                    }
                }

                /// Change Here for Warehouse and Home Delivery Orders In case of POS Orders
                if (Util.GetValueOfInt(GetVAPOS_POSTerminal_ID()) > 0)
                {
                    if (MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))
                    {
                        string sql = "SELECT COUNT(*) FROM VAF_Column WHERE VAF_TableView_ID = (SELECT VAF_TableView_ID FROM VAF_TableView WHERE TableName = 'VAB_DocTypes') AND ColumnName = 'VAPOS_OrderType'";
                        int val = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                        if (val > 0)
                        {
                            sql = "SELECT VAPOS_OrderType FROM VAB_DocTypes WHERE VAB_DocTypes_ID = " + dt.GetVAB_DocTypes_ID();
                            string oType = Util.GetValueOfString(DB.ExecuteScalar(sql, null, Get_TrxName()));
                            if (oType == "H" || oType == "W")
                            {
                                posStatus = shipment.PrepareIt();
                                shipment.SetDocStatus(posStatus);
                                shipment.Save(Get_TrxName());
                                return shipment;
                            }
                        }
                    }
                }

                if (IsDrafted)
                {
                    String status = "DR";
                    shipment.SetDocStatus(status);
                    shipment.Save(Get_TrxName());
                    if (!DOCSTATUS_Drafted.Equals(status))
                    {
                        _processMsg = "@VAM_Inv_InOut_ID@: " + shipment.GetProcessMsg();
                        return null;
                    }
                }
                //	Manually Process Shipment
                else
                {
                    String statuss = shipment.CompleteIt();
                    shipment.SetDocStatus(statuss);
                    shipment.Save(Get_TrxName());
                    if (!DOCSTATUS_Completed.Equals(statuss))
                    {
                        _processMsg = "@VAM_Inv_InOut_ID@: " + shipment.GetProcessMsg();
                        return null;
                    }
                }
            }
            catch
            {
                return null;
                //ShowMessage.Error("MOrder",null,"CreateShipment");
            }
            return shipment;
        }

        /// <summary>
        /// Create line with the reference of Container
        /// </summary>
        /// <param name="inout"></param>
        /// <param name="ioLine"></param>
        /// <param name="oLine"></param>
        /// <param name="VAM_Locator_ID"></param>
        /// <param name="Qty"></param>
        /// <param name="oproduct"></param>
        /// <returns></returns>
        private String CreateShipmentLineContainer(MVAMInvInOut inout, MVAMInvInOutLine ioLine, MVABOrderLine oLine, int VAM_Locator_ID, Decimal Qty, bool disalowNegativeInventory, MVAMProduct oproduct)
        {
            String pMsg = null;
            List<RecordContainer> shipLine = new List<RecordContainer>();

            // JID_1746: Create Lines for Charge / (Resource - Service - Expense) type product based on setting on Tenant to "Allow Non Item type".
            if (oproduct != null && oproduct.GetProductType() == MVAMProduct.PRODUCTTYPE_Item)
            {
                MVAMProductCategory productCategory = MVAMProductCategory.GetOfProduct(GetCtx(), oLine.GetVAM_Product_ID());

                RecordContainer recordContainer = null;
                bool existingRecord = false;
                String sql = "";
                if (isContainerApplicable)
                {
                    // Pick data from Container Storage based on Material Policy
                    sql = @"SELECT s.VAM_PFeature_SetInstance_ID ,s.VAM_ProductContainer_ID, s.Qty
                           FROM VAM_ContainerStorage s 
                           LEFT OUTER JOIN VAM_PFeature_SetInstance asi ON (s.VAM_PFeature_SetInstance_ID=asi.VAM_PFeature_SetInstance_ID)
                           WHERE NOT EXISTS (SELECT * FROM VAM_ProductContainer p WHERE isactive = 'N' AND p.VAM_ProductContainer_ID = NVL(s.VAM_ProductContainer_ID , 0)) 
                           AND s.VAF_Client_ID= " + oLine.GetVAF_Client_ID() + @"
                           AND s.VAF_Org_ID=" + oLine.GetVAF_Org_ID() + @"
                           AND s.VAM_Locator_ID = " + VAM_Locator_ID + @" 
                           AND s.VAM_Product_ID=" + oLine.GetVAM_Product_ID() + @"
                           AND s.Qty > 0 ";
                    if (oLine.GetVAM_PFeature_SetInstance_ID() != 0)
                    {
                        sql += " AND NVL(s.VAM_PFeature_SetInstance_ID , 0)=" + oLine.GetVAM_PFeature_SetInstance_ID();
                    }
                    if (productCategory.GetMMPolicy() == X_VAM_ProductCategory.MMPOLICY_LiFo)
                        sql += " ORDER BY asi.GuaranteeDate ASC, s.MMPolicyDate DESC, s.VAM_ContainerStorage_ID DESC";
                    else if (productCategory.GetMMPolicy() == X_VAM_ProductCategory.MMPOLICY_FiFo)
                        sql += " ORDER BY asi.GuaranteeDate ASC, s.MMPolicyDate ASC , s.VAM_ContainerStorage_ID ASC";
                }
                else
                {
                    sql = @"SELECT s.VAM_PFeature_SetInstance_ID ,0 AS VAM_ProductContainer_ID, s.QtyOnHand AS Qty
                        FROM VAM_Storage s WHERE s.VAM_Locator_ID = " + VAM_Locator_ID + @" 
                           AND s.VAM_Product_ID=" + oLine.GetVAM_Product_ID() + @"
                           AND s.QtyOnHand > 0 ";
                    if (oLine.GetVAM_PFeature_SetInstance_ID() != 0)
                    {
                        sql += " AND NVL(s.VAM_PFeature_SetInstance_ID , 0)=" + oLine.GetVAM_PFeature_SetInstance_ID();
                    }
                }
                DataSet ds = DB.ExecuteDataset(sql, null, Get_Trx());
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    int containerASI = 0;
                    int containerId = 0;
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        existingRecord = false;
                        if (i > 0)
                        {
                            #region  Find existing record based on respective parameter
                            if (shipLine.Count > 0)
                            {
                                // Find existing record based on respective parameter
                                RecordContainer iRecord = null;
                                containerASI = Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_PFeature_SetInstance_ID"]);
                                containerId = Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_ProductContainer_ID"]);

                                if (shipLine.Exists(x => (x.VAM_Locator_ID == VAM_Locator_ID) &&
                                                         (x.VAM_Product_ID == oLine.GetVAM_Product_ID()) &&
                                                         (x.M_ASI_ID == containerASI) &&
                                                         (x.VAM_ProductContainer_ID == containerId)
                                                    ))
                                {
                                    iRecord = shipLine.Find(x => (x.VAM_Locator_ID == VAM_Locator_ID) &&
                                                                 (x.VAM_Product_ID == oLine.GetVAM_Product_ID()) &&
                                                                 (x.M_ASI_ID == containerASI) &&
                                                                 (x.VAM_ProductContainer_ID == containerId)
                                                           );
                                    if (iRecord != null)
                                    {
                                        // create object of existing record
                                        existingRecord = true;
                                        ioLine = new VAdvantage.Model.MVAMInvInOutLine(GetCtx(), iRecord.VAM_Inv_InOutLine_ID, Get_Trx());
                                    }
                                }
                            }
                            #endregion
                        }

                        if (!existingRecord && i != 0)
                        {
                            // Create new object of shipline
                            ioLine = new MVAMInvInOutLine(inout);
                        }

                        Decimal containerQty = Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["Qty"]);
                        Decimal lineCreatedQty = (Decimal.Subtract(Qty, containerQty) >= 0 ? ((i + 1) != ds.Tables[0].Rows.Count ? containerQty : Qty) : Qty);
                        if (!existingRecord)
                        {
                            ioLine.SetOrderLine(oLine, VAM_Locator_ID, lineCreatedQty);
                            ioLine.SetVAM_ProductContainer_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_ProductContainer_ID"]));
                        }
                        ioLine.SetQty(Decimal.Add(ioLine.GetQtyEntered(), lineCreatedQty));
                        if (oLine.GetQtyEntered().CompareTo(oLine.GetQtyOrdered()) != 0)
                        {
                            ioLine.SetQtyEntered(Decimal.Multiply(lineCreatedQty, (Decimal.Divide(oLine.GetQtyEntered(), (oLine.GetQtyOrdered())))));
                        }
                        ioLine.SetVAM_PFeature_SetInstance_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAM_PFeature_SetInstance_ID"]));
                        if (!ioLine.Save(Get_TrxName()))
                        {
                            ValueNamePair pp = VLogger.RetrieveError();
                            if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                                pMsg = "Could not create Shipment Line. " + pp.GetName();
                            else
                                pMsg = "Could not create Shipment Line";
                            return pMsg;
                        }
                        else
                        {
                            #region  created list - for updating on same record
                            // created list - for updating on same record
                            if (!shipLine.Exists(x => (x.VAM_Locator_ID == ioLine.GetVAM_Locator_ID()) &&
                                                     (x.VAM_Product_ID == ioLine.GetVAM_Product_ID()) &&
                                                     (x.M_ASI_ID == ioLine.GetVAM_PFeature_SetInstance_ID()) &&
                                                     (x.VAM_ProductContainer_ID == ioLine.GetVAM_ProductContainer_ID())
                                                     ))
                            {
                                recordContainer = new RecordContainer();
                                recordContainer.VAM_Inv_InOutLine_ID = ioLine.GetVAM_Inv_InOutLine_ID();
                                recordContainer.VAM_Locator_ID = ioLine.GetVAM_Locator_ID();
                                recordContainer.VAM_Product_ID = ioLine.GetVAM_Product_ID();
                                recordContainer.M_ASI_ID = ioLine.GetVAM_PFeature_SetInstance_ID();
                                recordContainer.VAM_ProductContainer_ID = ioLine.GetVAM_ProductContainer_ID();
                                shipLine.Add(recordContainer);
                            }
                            #endregion

                            // Qty Represent "Remaining Qty" whose shipline line to be created
                            Qty -= lineCreatedQty;
                            if (Qty <= 0)
                                break;
                        }
                    }
                }
                ds.Dispose();
            }

            // When Disallow Negative Inventory is FALSE - then create new line with remainning qty
            if (Qty != 0)
            {
                ioLine = null;
                RecordContainer iRecord = null;
                if (shipLine.Exists(x => (x.VAM_Locator_ID == VAM_Locator_ID) &&
                                                    (x.VAM_Product_ID == oLine.GetVAM_Product_ID()) &&
                                                    (x.M_ASI_ID == oLine.GetVAM_PFeature_SetInstance_ID()) &&
                                                    (x.VAM_ProductContainer_ID == 0)))
                {
                    iRecord = shipLine.Find(x => (x.VAM_Locator_ID == VAM_Locator_ID) &&
                                                 (x.VAM_Product_ID == oLine.GetVAM_Product_ID()) &&
                                                 (x.M_ASI_ID == oLine.GetVAM_PFeature_SetInstance_ID()) &&
                                                 (x.VAM_ProductContainer_ID == 0));
                    if (iRecord != null)
                    {
                        // create object of existing record
                        ioLine = new VAdvantage.Model.MVAMInvInOutLine(GetCtx(), iRecord.VAM_Inv_InOutLine_ID, Get_Trx());
                    }
                }

                // when no recod found on above filteation - then need to create new object
                if (ioLine == null)
                {
                    // Create new object of shipline
                    ioLine = new MVAMInvInOutLine(inout);
                }
                ioLine.SetOrderLine(oLine, VAM_Locator_ID, Qty);
                ioLine.SetVAM_ProductContainer_ID(0);
                ioLine.SetQty(Decimal.Add(ioLine.GetQtyEntered(), Qty));
                if (oLine.GetQtyEntered().CompareTo(oLine.GetQtyOrdered()) != 0)
                {
                    ioLine.SetQtyEntered(Decimal.Multiply(Qty, (Decimal.Divide(oLine.GetQtyEntered(), (oLine.GetQtyOrdered())))));
                }
                if (!ioLine.Save(Get_TrxName()))
                {
                    ValueNamePair pp = VLogger.RetrieveError();
                    if (pp != null && !string.IsNullOrEmpty(pp.GetName()))
                        pMsg = "Could not create Shipment Line. " + pp.GetName();
                    else
                        pMsg = "Could not create Shipment Line";
                    return pMsg;
                }
            }
            return pMsg;
        }


        /****************************************************************************************************/
        /* 	Create Invoice
            *	@param dt order document type
            *	@param shipment optional shipment
            *	@param invoiceDate invoice date
            *	@return invoice or null
            */
        private MVABInvoice CreateInvoice(MVABDocTypes dt, MVAMInvInOut shipment, DateTime? invoiceDate)
        {
            MVABInvoice invoice = new MVABInvoice(this, dt.GetVAB_DocTypesInvoice_ID(), invoiceDate);
            if (Util.GetValueOfInt(GetVAPOS_POSTerminal_ID()) > 0)
            {
                #region VAPOS_POSTerminal_ID > 0
                String DocSubTypeSO = dt.GetDocSubTypeSO();
                if (MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))
                {
                    if (GetVAPOS_POSTerminal_ID() != 0)
                    {
                        invoice.SetDocumentNo(GetDocumentNo());
                        try
                        {
                            int ConversionTypeId = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_CurrencyType_ID FROM VAPOS_POSTerminal WHERE 
                                                VAPOS_POSTerminal_ID=" + GetVAPOS_POSTerminal_ID()));
                            invoice.SetVAB_CurrencyType_ID(ConversionTypeId);

                            MVABOrder ord = new MVABOrder(GetCtx(), GetVAB_Order_ID(), null);

                            if (ord.GetVAPOS_CreditAmt() > 0)
                            {
                                invoice.SetIsPaid(false);
                                invoice.SetVAPOS_IsPaid(false);
                            }
                            else
                            {
                                invoice.SetIsPaid(true);
                                invoice.SetVAPOS_IsPaid(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Info("Paid Or ConversionType not Marked ====>>>> " + ex.Message);
                        }
                    }
                }

                if (Env.IsModuleInstalled("VA009_"))
                {
                    invoice.SetVA009_PaymentMethod_ID(GetVA009_PaymentMethod_ID());
                }
                #endregion
            }
            log.Info(dt.ToString());
            try
            {
                //SI_0181 : set Payment Method from order to invoice
                if (Env.IsModuleInstalled("VA009_"))
                {
                    invoice.SetVA009_PaymentMethod_ID(GetVA009_PaymentMethod_ID());
                }
                if (!invoice.Save(Get_TrxName()))
                {
                    _processMsg = "Could not create Invoice";
                    return null;
                }

                //	If we have a Shipment - use that as a base
                if (shipment != null)
                {
                    if (!INVOICERULE_AfterDelivery.Equals(GetInvoiceRule()))
                        SetInvoiceRule(INVOICERULE_AfterDelivery);
                    //
                    MVAMInvInOutLine[] sLines = shipment.GetLines(false);
                    for (int i = 0; i < sLines.Length; i++)
                    {
                        MVAMInvInOutLine sLine = sLines[i];
                        //
                        MVABInvoiceLine iLine = new MVABInvoiceLine(invoice);
                        iLine.SetShipLine(sLine);
                        //	Qty = Delivered	
                        iLine.SetQtyEntered(sLine.GetQtyEntered());
                        iLine.SetQtyInvoiced(sLine.GetMovementQty());
                        if (!iLine.Save(Get_TrxName()))
                        {
                            _processMsg = "Could not create Invoice Line from Shipment Line";
                            return null;
                        }
                        //
                        sLine.SetIsInvoiced(true);
                        if (!sLine.Save(Get_TrxName()))
                        {
                            log.Warning("Could not update Shipment line: " + sLine);
                        }
                    }


                    // Create Lines for Charge / (Resource - Service - Expense) type product based on setting on Tenant to "Allow Non Item type".
                    if (Util.GetValueOfString(GetCtx().GetContext("$AllowNonItem")).Equals("N"))
                    {
                        // Create Invoice Line for Charge / (Resource - Service - Expense) type product 
                        MVABOrderLine[] oLines = GetLinesOtherthanProduct();
                        for (int i = 0; i < oLines.Length; i++)
                        {
                            MVABOrderLine oLine = oLines[i];
                            //
                            MVABInvoiceLine iLine = new MVABInvoiceLine(invoice);
                            iLine.SetOrderLine(oLine);
                            //	Qty = Ordered - Invoiced	
                            iLine.SetQtyInvoiced(Decimal.Subtract(oLine.GetQtyOrdered(), oLine.GetQtyInvoiced()));
                            if (oLine.GetQtyOrdered().CompareTo(oLine.GetQtyEntered()) == 0)
                                iLine.SetQtyEntered(iLine.GetQtyInvoiced());
                            else
                                iLine.SetQtyEntered(Decimal.Multiply(iLine.GetQtyInvoiced(), (Decimal.Divide(oLine.GetQtyEntered(), oLine.GetQtyOrdered()))));
                            if (!iLine.Save(Get_TrxName()))
                            {
                                _processMsg = "Could not create Invoice Line from Order Line";
                                return null;
                            }
                        }
                    }
                }
                else    //	Create Invoice from Order
                {
                    if (!INVOICERULE_Immediate.Equals(GetInvoiceRule()))
                        SetInvoiceRule(INVOICERULE_Immediate);
                    //
                    MVABOrderLine[] oLines = GetLines();
                    for (int i = 0; i < oLines.Length; i++)
                    {
                        MVABOrderLine oLine = oLines[i];
                        //
                        MVABInvoiceLine iLine = new MVABInvoiceLine(invoice);
                        iLine.SetOrderLine(oLine);
                        //	Qty = Ordered - Invoiced	
                        iLine.SetQtyInvoiced(Decimal.Subtract(oLine.GetQtyOrdered(), oLine.GetQtyInvoiced()));
                        if (oLine.GetQtyOrdered().CompareTo(oLine.GetQtyEntered()) == 0)
                            iLine.SetQtyEntered(iLine.GetQtyInvoiced());
                        else
                            iLine.SetQtyEntered(Decimal.Multiply(iLine.GetQtyInvoiced(), (Decimal.Divide(oLine.GetQtyEntered(), oLine.GetQtyOrdered()))));
                        if (!iLine.Save(Get_TrxName()))
                        {
                            _processMsg = "Could not create Invoice Line from Order Line";
                            return null;
                        }
                    }
                }
                //	Manually Process Invoice
                String status = invoice.CompleteIt();
                invoice.SetDocStatus(status);
                invoice.Save(Get_TrxName());

                SetVAB_CashJRNLLine_ID(invoice.GetVAB_CashJRNLLine_ID());
                if (!DOCSTATUS_Completed.Equals(status))
                {
                    _processMsg = "@VAB_Invoice_ID@: " + invoice.GetProcessMsg();
                    return null;
                }
            }
            catch
            {
                //ShowMessage.Error("MOrder",null,"CreateInvoice");
            }
            return invoice;
        }

        /* 	Create Counter Document
         * 	@return counter order
         */
        private MVABOrder CreateCounterDoc()
        {
            //	Is this itself a counter doc ?
            if (GetRef_Order_ID() != 0)
                return null;

            //	Org Must be linked to BPartner
            MVAFOrg org = MVAFOrg.Get(GetCtx(), GetVAF_Org_ID());
            //jz int counterVAB_BusinessPartner_ID = org.getLinkedVAB_BusinessPartner_ID(Get_TrxName()); 
            int counterVAB_BusinessPartner_ID = org.GetLinkedVAB_BusinessPartner_ID(Get_TrxName());
            if (counterVAB_BusinessPartner_ID == 0)
                return null;
            //	Business Partner needs to be linked to Org
            //jz MBPartner bp = MBPartner.get (GetCtx(), getVAB_BusinessPartner_ID());
            MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_TrxName());
            int counterVAF_Org_ID = bp.GetVAF_OrgBP_ID_Int();
            if (counterVAF_Org_ID == 0)
                return null;

            //jz MBPartner counterBP = MBPartner.get (GetCtx(), counterVAB_BusinessPartner_ID);
            MVABBusinessPartner counterBP = new MVABBusinessPartner(GetCtx(), counterVAB_BusinessPartner_ID, Get_TrxName());
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

            // set counter Businees partner 
            SetCounterBPartner(counterBP, counterVAF_Org_ID, counterOrgInfo.GetVAM_Warehouse_ID());
            //	Deep Copy
            // JID_1300: If the PO is created with lines includes Product with attribute set instance, once the counter document is created on completion of PO i.e. SO, 
            // Attribute Set Instance values are not getting fetched into SO lines.
            MVABOrder counter = CopyFrom(this, GetDateOrdered(),
                VAB_DocTypesTarget_ID, true, true, Get_TrxName());
            //
            counter.SetDatePromised(GetDatePromised());     // default is date ordered 
                                                            //	Refernces (Should not be required
            counter.SetSalesRep_ID(GetSalesRep_ID());
            //
            counter.SetProcessing(false);
            counter.Save(Get_TrxName());

            //	Update copied lines
            MVABOrderLine[] counterLines = counter.GetLines(true, null);
            for (int i = 0; i < counterLines.Length; i++)
            {
                MVABOrderLine counterLine = counterLines[i];
                counterLine.SetOrder(counter);  //	copies header values (BP, etc.)
                counterLine.SetPrice();
                counterLine.SetTax();
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
        ///	Set Qtys to 0 - Sales: reverse all documents
        /// </summary>
        /// <returns>true if success</returns>
        public bool VoidIt()
        {
            MVABOrderLine[] lines = GetLines(true, "VAM_Product_ID");
            log.Info(ToString());

            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
            String DocSubTypeSO = dt.GetDocSubTypeSO();

            //JID_1474 If quantity released is greater than 0, then system will not allow to void blanket order record and give message: 'Please Void/Reverse its dependent transactions first'
            if (dt.GetDocBaseType() == MVABMasterDocType.DOCBASETYPE_BLANKETSALESORDER)
            {
                if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT SUM(qtyreleased) FROM VAB_OrderLine WHERE VAB_Order_ID = " + GetVAB_Order_ID(), null, Get_Trx())) > 0)
                {
                    _processMsg = "Please Void/Reverse its dependent transactions first";
                    return false;
                }
            }

            // Added by Vivek on 08/11/2017 assigned by Mukesh sir
            // return false if linked document is in completed or closed stage
            // when we void SO then system void all transaction which is linked with that SO
            if (MVABDocTypes.DOCSUBTYPESO_OnCreditOrder.Equals(DocSubTypeSO)    //	(W)illCall(I)nvoice
                    || MVABDocTypes.DOCSUBTYPESO_WarehouseOrder.Equals(DocSubTypeSO)    //	(W)illCall(P)ickup	
                    || MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))         //	(W)alkIn(R)eceipt
            {
                // when we void SO then system void all transaction which is linked with that SO
            }
            else
            {
                if (!linkedDocument(GetVAB_Order_ID()))
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "LinkedDocStatus");
                    return false;
                }
            }

            // Added by Vivek on 27/09/2017 assigned by Pradeep 
            // To check if associated PO is exist but not reversed in case of drop shipment
            if (IsSOTrx() && !IsReturnTrx())
            {
                if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VAB_OrderLine ol Inner Join VAB_Order o ON o.VAB_Order_ID=ol.VAB_Order_ID Where O.VAB_Order_ID=" + GetVAB_Order_ID() + " AND ol.IsDropShip='Y' AND O.IsSoTrx='Y' AND O.IsReturnTrx='N'")) > 0)
                {
                    if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VAB_Order Where Ref_Order_Id=" + GetVAB_Order_ID() + " AND IsDropShip='Y' AND IsSoTrx='N' AND IsReturnTrx='N' AND DocStatus NOT IN ('VO','RE')")) > 0)
                    {
                        _processMsg = "Associated purchase order must be voided or reversed first";
                        return false;
                    }
                }
            }
            for (int i = 0; i < lines.Length; i++)
            {
                MVABOrderLine line = lines[i];
                Decimal old = line.GetQtyOrdered();
                if (System.Math.Sign(old) != 0)
                {
                    line.AddDescription(Msg.GetMsg(Env.GetContext(), "Voided", true) + " (" + old + ")");
                    line.SetQtyLostSales(old);
                    line.SetQty(Env.ZERO);
                    line.SetLineNetAmt(Env.ZERO);

                    // Remove Reference of Requisition from PO line after Void.
                    line.Set_Value("VAM_RequisitionLine_ID", 0);
                    line.Save(Get_TrxName());
                }
            }
            AddDescription(Msg.GetMsg(Env.GetContext(), "Voided", true));
            //	Clear Reservations
            if (!ReserveStock(null, lines))
            {
                _processMsg = "Cannot unreserve Stock (void)";
                return false;
            }

            if (!CreateReversals())
                return false;

            //************* Changed ***************************
            // Set Status at Order to Rejected if it is Sales Order 
            MVABOrder ord = new MVABOrder(Env.GetCtx(), GetVAB_Order_ID(), Get_TrxName());
            if (IsSOTrx())
            {
                ord.SetStatusCode("R");
                ord.Save();
            }

            // JID_0658: After Creating PO from Open Requisition, & the PO record is Void, PO line reference is not getting removed from Requisition Line.
            DB.ExecuteQuery("UPDATE VAM_RequisitionLine SET VAB_OrderLine_ID = 0 WHERE VAB_OrderLine_ID IN (SELECT VAB_OrderLine_ID FROM VAB_Order WHERE VAB_Order_ID = " + GetVAB_Order_ID() + ")", null, Get_TrxName());

            SetProcessed(true);
            SetDocAction(DOCACTION_None);
            return true;
        }

        /* Create Shipment/Invoice Reversals
        * 	@return true if success
        */
        private bool CreateReversals()
        {
            try
            {
                //	Cancel only Sales 
                if (!IsSOTrx() || Util.GetValueOfBool(Get_Value("IsSalesQuotation")))
                    return true;

                log.Info("");
                StringBuilder Info = new StringBuilder();
                // JID_0216: System void/reverse the Shipment and invoice related to SO and voided document number will be displayed with the document name. 
                //	Reverse All *Shipments*
                //Info.Append("@VAM_Inv_InOut_ID@:");
                Info.Append(Msg.GetMsg(GetCtx(), "Shipment") + ":");
                MVAMInvInOut[] shipments = GetShipments(false);   //	get all (line based)
                for (int i = 0; i < shipments.Length; i++)
                {
                    MVAMInvInOut ship = shipments[i];
                    //	if closed - ignore
                    if (MVAMInvInOut.DOCSTATUS_Closed.Equals(ship.GetDocStatus())
                        || MVAMInvInOut.DOCSTATUS_Reversed.Equals(ship.GetDocStatus())
                        || MVAMInvInOut.DOCSTATUS_Voided.Equals(ship.GetDocStatus()))
                        continue;
                    ship.Set_TrxName(Get_TrxName());

                    //	If not completed - void - otherwise reverse it
                    if (!MVAMInvInOut.DOCSTATUS_Completed.Equals(ship.GetDocStatus()))
                    {
                        if (ship.VoidIt())
                            ship.SetDocStatus(MVAMInvInOut.DOCSTATUS_Voided);
                    }
                    //	Create new Reversal with only that order
                    else if (!ship.IsOnlyForOrder(this))
                    {
                        ship.ReverseCorrectIt(this);
                        //	shipLine.setDocStatus(MVAMInvInOut.DOCSTATUS_Reversed);
                        Info.Append(" Parial ").Append(ship.GetDocumentNo());
                    }
                    else if (ship.ReverseCorrectIt()) //	completed shipment
                    {
                        ship.SetDocStatus(MVAMInvInOut.DOCSTATUS_Reversed);
                        Info.Append(" ").Append(ship.GetDocumentNo());
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(ship.GetProcessMsg()))
                        {
                            Info.Append(" ").Append(ship.GetProcessMsg());
                            //_processMsg = ship.GetProcessMsg() + " - " + ship;
                        }
                        else
                        {
                            Info.Append(" ").Append(Msg.GetMsg(GetCtx(), "ErrorReverse"));
                            //_processMsg = "Could not reverse Shipment " + ship;
                        }
                        _processMsg = Info.ToString();
                        return false;
                    }
                    ship.SetDocAction(MVAMInvInOut.DOCACTION_None);
                    ship.Save(Get_TrxName());
                }   //	for all shipments

                //	Reverse All *Invoices*
                Info.Append(" - @VAB_Invoice_ID@:");
                //Info.Append(Msg.GetMsg(GetCtx(), "SalesOrder"));
                MVABInvoice[] invoices = GetInvoices(false);   //	get all (line based)
                for (int i = 0; i < invoices.Length; i++)
                {
                    MVABInvoice invoice = invoices[i];
                    //	if closed - ignore
                    if (MVABInvoice.DOCSTATUS_Closed.Equals(invoice.GetDocStatus())
                        || MVABInvoice.DOCSTATUS_Reversed.Equals(invoice.GetDocStatus())
                        || MVABInvoice.DOCSTATUS_Voided.Equals(invoice.GetDocStatus()))
                        continue;
                    invoice.Set_TrxName(Get_TrxName());

                    //	If not completed - void - otherwise reverse it
                    if (!MVABInvoice.DOCSTATUS_Completed.Equals(invoice.GetDocStatus()))
                    {
                        if (invoice.VoidIt())
                            invoice.SetDocStatus(MVABInvoice.DOCSTATUS_Voided);
                    }
                    else if (invoice.ReverseCorrectIt())    //	completed invoice
                    {
                        invoice.SetDocStatus(MVABInvoice.DOCSTATUS_Reversed);
                        Info.Append(" ").Append(invoice.GetDocumentNo());
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(invoice.GetProcessMsg()))
                        {
                            Info.Append(" ").Append(invoice.GetProcessMsg());
                            //_processMsg = invoice.GetProcessMsg() + " - " + invoice;
                        }
                        else
                        {
                            Info.Append(" ").Append(Msg.GetMsg(GetCtx(), "ErrorReverse"));
                            //_processMsg = "Could not reverse Invoice " + invoice;
                        }
                        _processMsg = Info.ToString();
                        return false;
                    }
                    invoice.SetDocAction(MVABInvoice.DOCACTION_None);
                    invoice.Save(Get_TrxName());
                }   //	for all shipments

                //	Reverse All *RMAs*
                //Info.Append("@VAB_Order_ID@:");
                Info.Append(" - " + Msg.GetMsg(GetCtx(), "RMA") + ":");
                MVABOrder[] rmas = GetRMAs();
                for (int i = 0; i < rmas.Length; i++)
                {
                    MVABOrder rma = rmas[i];
                    //	if closed - ignore
                    if (MVABOrder.DOCSTATUS_Closed.Equals(rma.GetDocStatus())
                        || MVABOrder.DOCSTATUS_Reversed.Equals(rma.GetDocStatus())
                        || MVABOrder.DOCSTATUS_Voided.Equals(rma.GetDocStatus()))
                        continue;
                    rma.Set_TrxName(Get_TrxName());

                    //	If not completed - void - otherwise reverse it
                    if (!MVABOrder.DOCSTATUS_Completed.Equals(rma.GetDocStatus()))
                    {
                        if (rma.VoidIt())
                            rma.SetDocStatus(MVAMInvInOut.DOCSTATUS_Voided);
                    }
                    //	Create new Reversal with only that order
                    else if (rma.ReverseCorrectIt()) //	completed shipment
                    {
                        rma.SetDocStatus(MVABOrder.DOCSTATUS_Reversed);
                        Info.Append(" ").Append(rma.GetDocumentNo());
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(rma.GetProcessMsg()))
                        {
                            Info.Append(" ").Append(rma.GetProcessMsg());
                            //_processMsg = rma.GetProcessMsg() + " - " + rma;
                        }
                        else
                        {
                            Info.Append(" ").Append(Msg.GetMsg(GetCtx(), "ErrorReverse"));
                            //_processMsg = "Could not reverse RMA " + rma;
                        }
                        _processMsg = Info.ToString();
                        return false;
                    }
                    rma.SetDocAction(MVAMInvInOut.DOCACTION_None);
                    rma.Save(Get_TrxName());
                }   //	for all shipments

                _processMsg = Info.ToString();
            }
            catch
            {
                //ShowMessage.Error("MOrder",null,"CreateReversals");
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                    _processMsg = "Could not CreateReversals. " + pp.GetName();
                else
                    _processMsg = "Could not CreateReversals.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Close Document. Cancel not delivered Quantities
        /// </summary>
        /// <returns>true if success</returns>
        public bool CloseIt()
        {
            log.Info(ToString());
            //	Close Not delivered Qty - SO/PO
            MVABOrderLine[] lines = GetLines(true, "VAM_Product_ID");
            for (int i = 0; i < lines.Length; i++)
            {
                MVABOrderLine line = lines[i];
                Decimal old = line.GetQtyOrdered();
                if (old.CompareTo(line.GetQtyDelivered()) != 0)
                {
                    line.SetQtyLostSales(Decimal.Subtract(line.GetQtyOrdered(), line.GetQtyDelivered()));
                    line.SetQtyOrdered(line.GetQtyDelivered());
                    //Set property to true because close event is called
                    line.SetIsClosedDocument(true);
                    //	QtyEntered unchanged
                    line.AddDescription("Close (" + old + ")");
                    line.Save(Get_TrxName());
                }
            }
            //	Clear Reservations
            if (!ReserveStock(null, lines))
            {
                _processMsg = "Cannot unreserve Stock (close)";
                return false;
            }
            SetProcessed(true);
            SetDocAction(DOCACTION_None);
            return true;
        }

        /// <summary>
        /// Reverse Correction - same void
        /// </summary>
        /// <returns>true if success</returns>
        public bool ReverseCorrectIt()
        {
            log.Info(ToString());
            return VoidIt();
        }

        /// <summary>
        /// Reverse Accrual - none
        /// </summary>
        /// <returns>false</returns>
        public bool ReverseAccrualIt()
        {
            log.Info(ToString());
            return false;
        }

        /// <summary>
        /// Re-activate.
        /// </summary>
        /// <returns>true if success</returns>
        public bool ReActivateIt()
        {
            try
            {
                log.Info(ToString());

                if (GetVAPOS_POSTerminal_ID() > 0 && (GetVA018_VoucherAmount() > 0 || GetVA204_RewardAmt() > 0))
                {
                    _processMsg = "Voucher Or Reward amount redeemed against this order, can-not reactivate";
                    log.Warning("Voucher  Or Reward amount redeemed against this order, can-not reactivate");
                    return false;
                }

                // JID_1035 before reactivating the order user need to void the payment first if orderschedule  exist against current order
                if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT( VA009_OrderPaySchedule_ID ) FROM VA009_OrderPaySchedule WHERE VAB_Order_id =" + GetVAB_Order_ID() + " AND (VAB_Payment_id !=0 OR VAB_CashJRNLLine_id!=0)")) > 0)
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "PaymentmustvoidedFirst");
                    return false;
                }

                MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
                String DocSubTypeSO = dt.GetDocSubTypeSO();
                MVABOrderLine[] lines = null;
                // Added by Vivek on 08/11/2017 assigned by Mukesh sir
                // return false if linked document is in completed or closed stage
                if (MVABDocTypes.DOCSUBTYPESO_OnCreditOrder.Equals(DocSubTypeSO)    //	(W)illCall(I)nvoice
                    || MVABDocTypes.DOCSUBTYPESO_WarehouseOrder.Equals(DocSubTypeSO)    //	(W)illCall(P)ickup	
                    || MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))         //	(W)alkIn(R)eceipt
                {
                    // when we void SO then system void all transaction which is linked with that SO
                }
                else
                {
                    // JID_1362 - User can reactivate record even the depenedent transaction available into system
                    ////if (!linkedDocument(GetVAB_Order_ID()))
                    ////{
                    ////    _processMsg = Msg.GetMsg(GetCtx(), "LinkedDocStatus");
                    ////    return false;
                    ////}
                }

                // Added by Vivek on 27/09/2017 assigned by Pradeep 
                // To check if associated PO is exist but not reversed  in case of drop shipment
                if (IsSOTrx() && !IsReturnTrx())
                {
                    if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VAB_OrderLine ol Inner Join VAB_Order o ON o.VAB_Order_ID=ol.VAB_Order_ID Where O.VAB_Order_ID=" + GetVAB_Order_ID() + " AND ol.IsDropShip='Y' AND O.IsSoTrx='Y' AND O.IsReturnTrx='N'")) > 0)
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VAB_Order Where Ref_Order_Id=" + GetVAB_Order_ID() + " AND IsDropShip='Y' AND IsSoTrx='N' AND IsReturnTrx='N' AND DocStatus NOT IN ('VO','RE')")) > 0)
                        {
                            _processMsg = "Associated purchase order must be voided or reversed first";
                            return false;
                        }
                    }
                }

                // set reserved qty to 0 when order is reactivating
                // Added by Vivek on 27/02/2018 assigned by mukesh sir
                /* SI_0561 : when we Re-Activate -- we can not set as "0" to QtyReserved because we are not updating qtyeserved on storage.
                             Otherwise system reserving again the same qty which define on the same record*/
                //MVABOrderLine line = null;
                //lines = GetLines(true, "VAM_Product_ID");
                //for (int i = 0; i < lines.Length; i++)
                //{
                //    line = new MVABOrderLine(GetCtx(), _lines[i].GetVAB_OrderLine_ID(), Get_Trx());
                //    line.SetQtyReserved(0);
                //    line.Save(Get_Trx());
                //}

                //	Replace Prepay with POS to revert all doc
                if (MVABDocTypes.DOCSUBTYPESO_PrepayOrder.Equals(DocSubTypeSO))
                {
                    MVABDocTypes newDT = null;
                    MVABDocTypes[] dts = MVABDocTypes.GetOfClient(GetCtx());
                    for (int i = 0; i < dts.Length; i++)
                    {
                        MVABDocTypes type = dts[i];
                        if (MVABDocTypes.DOCSUBTYPESO_PrepayOrder.Equals(type.GetDocSubTypeSO()))
                        {
                            if (type.IsDefault() || newDT == null)
                                newDT = type;
                        }
                    }
                    if (newDT == null)
                        return false;
                    else
                    {
                        SetVAB_DocTypes_ID(newDT.GetVAB_DocTypes_ID());
                        SetIsReturnTrx(newDT.IsReturnTrx());
                    }
                }

                if (dt.GetDocBaseType() == "BOO")  // if (dt.GetValue() == "BPO" || dt.GetValue() == "BSO")
                {
                    MVABOrder mo = new MVABOrder(GetCtx(), GetVAB_Order_ID(), null);
                    if (DateTime.Now.Date > mo.GetOrderValidTo().Value.Date)
                    {
                        _processMsg = "Validity of Order has been finished";
                        log.Info("Validity of Order has been finished");
                        return false;
                    }
                    lines = GetLines(true, "VAM_Product_ID");
                    int count = 0;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        Decimal? qtyRel = MVABUOMConversion.ConvertProductTo(GetCtx(), lines[i].GetVAM_Product_ID(), lines[i].GetVAB_UOM_ID(), lines[i].GetQtyReleased());
                        if (qtyRel != null)
                        {
                            if (qtyRel >= lines[i].GetQtyEstimation())
                                count++;
                        }
                        else
                        {
                            if (lines[i].GetQtyReleased() >= lines[i].GetQtyEstimation())
                                count++;
                        }
                    }

                    if (count == lines.Length)
                    {
                        _processMsg = "All Estimated Quantites has been released.";
                        log.Warning("All Estimated Quantites has been released.");
                        return false;
                    }
                }


                //	PO - just re-open
                if (!IsSOTrx())
                {
                    log.Info("Existing documents not modified - " + dt);
                }
                //	Reverse Direct Documents
                else if (MVABDocTypes.DOCSUBTYPESO_OnCreditOrder.Equals(DocSubTypeSO)   //	(W)illCall(I)nvoice
                    || MVABDocTypes.DOCSUBTYPESO_WarehouseOrder.Equals(DocSubTypeSO)    //	(W)illCall(P)ickup	
                    || MVABDocTypes.DOCSUBTYPESO_POSOrder.Equals(DocSubTypeSO))         //	(W)alkIn(R)eceipt
                {
                    if (!CreateReversals())
                        return false;
                    else if (GetVAPOS_POSTerminal_ID() > 0) //POS Terminal Order
                    {
                        if (GetVAPOS_RefPOSTerminal_ID() < 1)
                        {
                            SetVAPOS_RefPOSTerminal_ID(GetVAPOS_POSTerminal_ID());
                        }

                        SetVAPOS_POSTerminal_ID(0);
                        //SetVAPOS_RefPOSTerminal_ID
                        //Remove data from POS specific fields
                        SetVAPOS_CashPaid(0);
                        SetVAPOS_CreditAmt(0);
                        SetVAPOS_PayAmt(0);
                        SetVA205_Amounts("");
                        SetVA205_Currencies("");
                        SetVA205_RetAmounts("0");
                        SetVA205_Amounts("");
                        SetVAPOS_ReturnAmt(0);
                        SetVAPOS_TPPAmt(0);
                        SetVAPOS_TPPInfo("");
                        Save(Get_Trx());
                    }
                }
                else
                {
                    log.Info("Existing documents not modified - SubType=" + DocSubTypeSO);
                }

                // Set Value in Re-Activated when record is reactivated
                if (Get_ColumnIndex("IsReActivated") > 0)
                {
                    SetIsReActivated(true);
                }

                SetDocAction(DOCACTION_Complete);
                SetProcessed(false);
            }
            catch
            {
                //ShowMessage.Error("MOrder", null, "SetBPartner");
            }
            return true;
        }

        /// <summary>
        /// Get Summary
        /// </summary>
        /// <returns>Summary of Document</returns>
        public String GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetDocumentNo());
            //	: Grand Total = 123.00 (#1)
            sb.Append(": ").
                Append(Msg.Translate(GetCtx(), "GrandTotal")).Append("=").Append(GetGrandTotal());
            if (_lines != null)
                sb.Append(" (#").Append(_lines.Length).Append(")");
            //	 - Description
            if (GetDescription() != null && GetDescription().Length > 0)
                sb.Append(" - ").Append(GetDescription());
            return sb.ToString();
        }

        /// <summary>
        /// Get Process Message
        /// </summary>
        /// <returns>clear text error message</returns>
        public String GetProcessMsg()
        {
            return _processMsg;
        }

        /// <summary>
        /// Get Document Owner (Responsible)
        /// </summary>
        /// <returns>VAF_UserContact_ID</returns>
        public int GetDoc_User_ID()
        {
            return GetSalesRep_ID();
        }

        /// <summary>
        /// Get Document Approval Amount
        /// </summary>
        /// <returns>amount</returns>
        public Decimal GetApprovalAmt()
        {
            return GetGrandTotal();
        }

        /// <summary>
        /// Get Latest Shipment for the Order
        /// </summary>
        /// <param name="VAB_DocTypes_ID"></param>
        /// <param name="VAM_Warehouse_ID"></param>
        /// <param name="VAB_BusinessPartner_ID"></param>
        /// <param name="VAB_BPart_Location_ID"></param>
        /// <returns>latest shipment</returns>
        public MVAMInvInOut GetOpenInOut(int VAB_DocTypes_ID, int VAM_Warehouse_ID, int VAB_BusinessPartner_ID, int VAB_BPart_Location_ID)
        {
            //	TODO: getShipment if linked on line
            MVAMInvInOut inout = null;
            String sql = "SELECT VAM_Inv_InOut_ID " +
            "FROM VAM_Inv_InOut WHERE VAB_Order_ID=" + GetVAB_Order_ID()
           + " AND VAM_Warehouse_ID=" + VAM_Warehouse_ID
           + " AND VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID
           + " AND VAB_BPart_Location_ID= " + VAB_BPart_Location_ID
           + " AND VAB_DocTypes_ID= " + VAB_DocTypes_ID
            + " AND DocStatus IN ('DR','IP') " +
            " ORDER BY Created DESC";
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql, null, Get_TrxName());
                if (idr.Read())
                {
                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(idr[0]), Get_TrxName());
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }
            //
            return inout;
        }

        /// <summary>
        /// Get Linekd Document for the Order
        /// </summary>
        /// <param name="VAB_Order_ID"></param>
        /// <returns>True/False</returns>
        private bool linkedDocument(int VAB_Order_ID)
        {
            // SI_0595_3 : check orderline id exist on invoiceline or inoutline. if exist then not able to reverse the current order.
            string sql = @"Select SUM(Result) From (
                           SELECT COUNT(il.VAB_Orderline_id) AS Result FROM VAM_Inv_InOut i INNER JOIN VAM_Inv_InOutLine il ON i.VAM_Inv_InOut_id = il.VAM_Inv_InOut_id
                           INNER JOIN VAB_Orderline ol ON ol.VAB_Orderline_id = il.VAB_Orderline_id
                           WHERE ol.VAB_Order_ID  = " + VAB_Order_ID + @" AND i.DocStatus NOT IN ('RE' , 'VO')
                         UNION ALL
                          SELECT COUNT(il.VAB_Orderline_id) AS Result FROM VAB_Invoice i INNER JOIN VAB_InvoiceLine il ON i.VAB_Invoice_id = il.VAB_Invoice_id
                          INNER JOIN VAB_Orderline ol ON ol.VAB_Orderline_id = il.VAB_Orderline_id
                          WHERE ol.VAB_Order_ID  = " + VAB_Order_ID + @" AND i.DocStatus NOT IN ('RE' , 'VO')) t";
            int _countOrder = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));

            //check order id exist on LCDetail or PODetail or SODetail. if exist then not able to reverse the current order 
            if (_countOrder == 0 && Env.IsModuleInstalled("VA026_"))
            {
                sql = @"Select SUM(Result) From (                                                  
                                SELECT COUNT(o.VAB_Order_ID) AS Result FROM VA026_LCDetail lc INNER JOIN VAB_Order o ON lc.VAB_Order_ID=o.VAB_Order_ID WHERE 
                                lc.DocStatus NOT IN ('RE' , 'VO') AND o.VAB_Order_ID=" + VAB_Order_ID + @"
                                UNION ALL
                                SELECT COUNT(o.VAB_Order_ID) AS Result FROM VA026_LCDetail lc INNER JOIN VAB_Order o ON lc.VA026_Order_ID=o.VAB_Order_ID WHERE 
                                lc.DocStatus NOT IN ('RE' , 'VO') AND o.VAB_Order_ID=" + VAB_Order_ID + @"
                                UNION ALL
                                SELECT COUNT(o.VAB_Order_ID) AS Result FROM VA026_PODetail po INNER JOIN VA026_LCDetail lc ON po.VA026_LCDetail_ID=lc.VA026_LCDetail_ID
                                INNER JOIN VAB_Order o ON po.VAB_Order_ID=o.VAB_Order_ID 
                                WHERE lc.DocStatus NOT IN ('RE' , 'VO') AND  o.VAB_Order_ID=" + VAB_Order_ID + @"
                                UNION ALL
                                SELECT COUNT(o.VAB_Order_ID) AS Result FROM VA026_SODetail so INNER JOIN VA026_LCDetail lc ON so.VA026_LCDetail_ID=lc.VA026_LCDetail_ID
                                INNER JOIN VAB_Order o ON so.VAB_Order_ID=o.VAB_Order_ID 
                                WHERE lc.DocStatus NOT IN ('RE' , 'VO') AND  o.VAB_Order_ID=" + VAB_Order_ID + @") t";

                _countOrder = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));
            }

            if (_countOrder > 0)
            {
                return false;
            }
            return true;
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

        private void SetCounterBPartner(MVABBusinessPartner BPartner, int counterAdOrgId, int counterMWarehouseId)
        {
            counterBPartner = BPartner;
            counterOrgId = counterAdOrgId;
            counterWarehouseId = counterMWarehouseId;
        }

        private MVABBusinessPartner GetCounterBPartner()
        {
            return counterBPartner;
        }

        private int GetCounterOrgID()
        {
            return counterOrgId;
        }

        private int GetCounterWarehouseID()
        {
            return counterWarehouseId;
        }

        #endregion
    }

    public class RecordContainer
    {
        public int VAM_Inv_InOutLine_ID { get; set; }
        public int VAM_Locator_ID { get; set; }
        public int VAM_Product_ID { get; set; }
        public int M_ASI_ID { get; set; }
        public int VAM_ProductContainer_ID { get; set; }
    }


    public class BudgetControl
    {
        public int VAGL_Budget_ID { get; set; }
        public int VAGL_BudgetActivation_ID { get; set; }
        public int VAB_AccountBook_ID { get; set; }
        public int Account_ID { get; set; }
        public int VAF_Org_ID { get; set; }
        public int VAM_Product_ID { get; set; }
        public int VAB_BusinessPartner_ID { get; set; }
        public int VAB_BillingCode_ID { get; set; }
        public int VAB_AddressFrom_ID { get; set; }
        public int VAB_AddressTo_ID { get; set; }
        public int VAB_Promotion_ID { get; set; }
        public int VAF_OrgTrx_ID { get; set; }
        public int VAB_Project_ID { get; set; }
        public int VAB_SalesRegionState_ID { get; set; }
        public int UserElement1_ID { get; set; }
        public int UserElement2_ID { get; set; }
        public int UserElement3_ID { get; set; }
        public int UserElement4_ID { get; set; }
        public int UserElement5_ID { get; set; }
        public int UserElement6_ID { get; set; }
        public int UserElement7_ID { get; set; }
        public int UserElement8_ID { get; set; }
        public int UserElement9_ID { get; set; }
        public int UserList1_ID { get; set; }
        public int UserList2_ID { get; set; }
        public Decimal ControlledAmount { get; set; }
        public String WhereClause { get; set; }
    }

}