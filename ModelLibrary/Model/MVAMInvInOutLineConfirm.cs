﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAMInvInOutLineConfirm
 * Purpose        : For 2nd tab of the shipment window
 * Class Used     : X_VAM_Inv_InOutLineConfirm
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
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
//////using System.Windows.Forms;
//using VAdvantage.Controls;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
//using System.Web.UI;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVAMInvInOutLineConfirm : X_VAM_Inv_InOutLineConfirm
    {
        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAM_Inv_InOutLineConfirm_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVAMInvInOutLineConfirm(Ctx ctx, int VAM_Inv_InOutLineConfirm_ID, Trx trxName)
            : base(ctx, VAM_Inv_InOutLineConfirm_ID, trxName)
        {

            if (VAM_Inv_InOutLineConfirm_ID == 0)
            {
                //	setVAM_Inv_InOutConfirm_ID (0);
                //	setVAM_Inv_InOutLine_ID (0);
                //	setTargetQty (Env.ZERO);
                //	setConfirmedQty (Env.ZERO);
                SetDifferenceQty(Env.ZERO);
                SetScrappedQty(Env.ZERO);
                SetProcessed(false);
            }
        }

        /// <summary>
        /// Load Construvtor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">result set</param>
        /// <param name="trxName">transaction</param>
        public MVAMInvInOutLineConfirm(Ctx ctx, DataRow dr, Trx trxName) :
            base(ctx, dr, trxName)
        {
            //super(ctx, dr, trxName);
        }	

        /// <summary>
        /// Parent Construvtor
        /// </summary>
        /// <param name="header">parent</param>
        public MVAMInvInOutLineConfirm(MVAMInvInOutConfirm header)
            : this(header.GetCtx(), 0, header.Get_TrxName())
        {
            SetClientOrg(header);
            SetVAM_Inv_InOutConfirm_ID(header.GetVAM_Inv_InOutConfirm_ID());
        }	

        //Ship Line				*/
        private MVAMInvInOutLine _line = null;

        /// <summary>
        /// Set Shipment Line
        /// </summary>
        /// <param name="line">shipment line</param>
        public void SetInOutLine(MVAMInvInOutLine line)
        {
            SetVAM_Inv_InOutLine_ID(line.GetVAM_Inv_InOutLine_ID());
            SetTargetQty(line.GetMovementQty());	//	Confirmations in Storage UOM	
            SetConfirmedQty(GetTargetQty());		//	suggestion
            _line = line;
        }	

        /// <summary>
        ///  	Get Shipment Line
        /// </summary>
        /// <returns>line</returns>
        public MVAMInvInOutLine GetLine()
        {
            if (_line == null)
                _line = new MVAMInvInOutLine(GetCtx(), GetVAM_Inv_InOutLine_ID(), Get_TrxName());
            return _line;
        }	


        /// <summary>
        ///      Process Confirmation Line.
        ///  	- Update InOut Line
        /// </summary>
        /// <param name="isSOTrx">sales order</param>
        /// <param name="confirmType">type</param>
        /// <returns>success</returns>
        public Boolean ProcessLine(bool isSOTrx, String confirmType)
        {
            MVAMInvInOutLine line = GetLine();

            //	Customer
            if (MVAMInvInOutConfirm.CONFIRMTYPE_CustomerConfirmation.Equals(confirmType))
            {
                line.SetConfirmedQty(GetConfirmedQty());
            }

            //	Drop Ship
            else if (MVAMInvInOutConfirm.CONFIRMTYPE_DropShipConfirm.Equals(confirmType))
            {

            }

            //	Pick or QA
            else if (MVAMInvInOutConfirm.CONFIRMTYPE_PickQAConfirm.Equals(confirmType))
            {
                line.SetTargetQty(GetTargetQty());
                line.SetMovementQty(GetConfirmedQty());	//	Entered NOT changed
                line.SetPickedQty(GetConfirmedQty());
                //
                line.SetScrappedQty(GetScrappedQty());
            }

            //	Ship or Receipt
            else if (MVAMInvInOutConfirm.CONFIRMTYPE_ShipReceiptConfirm.Equals(confirmType))
            {
                //Arpit 
                if (GetDifferenceQty() > 0)
                {
                    GetCtx().SetContext("DifferenceQty_", VAdvantage.Utility.Util.GetValueOfString(GetDifferenceQty()));
                }
                MProduct _pro = new MProduct(GetCtx(), line.GetVAM_Product_ID(), Get_TrxName());
                if (_pro.GetVAB_UOM_ID() != line.GetVAB_UOM_ID())
                {
                    decimal? pc = null;
                    pc = MVABUOMConversion.ConvertProductFrom(GetCtx(), line.GetVAM_Product_ID(), GetVAB_UOM_ID(), GetTargetQty());
                    line.SetTargetQty(Util.GetValueOfDecimal( pc)); //TargetQty

                    Decimal qty = GetConfirmedQty();
                    Boolean isReturnTrx = line.GetParent().IsReturnTrx();
                    /* In PO receipts and SO Returns, we have the responsibility 
                     * for scrapped quantity
                     */
                    if ((!isSOTrx && !isReturnTrx) || (isSOTrx && isReturnTrx))
                        qty = Decimal.Add(qty, GetScrappedQty());
                    pc = MVABUOMConversion.ConvertProductFrom(GetCtx(), line.GetVAM_Product_ID(), GetVAB_UOM_ID(), qty);
                    line.SetMovementQty(Util.GetValueOfDecimal(pc)); //MovementQty

                    pc = MVABUOMConversion.ConvertProductFrom(GetCtx(), line.GetVAM_Product_ID(), GetVAB_UOM_ID(), GetScrappedQty());
                    line.SetScrappedQty(Util.GetValueOfDecimal(pc));  //ScrappedQty 

                    pc = MVABUOMConversion.ConvertProductFrom(GetCtx(), line.GetVAM_Product_ID(), GetVAB_UOM_ID(), GetConfirmedQty());
                    line.SetConfirmedQty(Util.GetValueOfDecimal(pc)); //confirm Qty

                }
                else
                {
                    line.SetTargetQty(GetTargetQty());
                    Decimal qty = GetConfirmedQty();
                    Boolean isReturnTrx = line.GetParent().IsReturnTrx();

                    /* In PO receipts and SO Returns, we have the responsibility 
                     * for scrapped quantity
                     */
                    if ((!isSOTrx && !isReturnTrx) || (isSOTrx && isReturnTrx))
                        qty = Decimal.Add(qty, GetScrappedQty());
                    line.SetMovementQty(qty);				//	Entered NOT changed
                    //
                    line.SetScrappedQty(GetScrappedQty());
                    // vikas 12/28/2015 Mantis Issue (0000335)
                    line.SetConfirmedQty(GetConfirmedQty());
                }
            }
            //	Vendor
            else if (MVAMInvInOutConfirm.CONFIRMTYPE_VendorConfirmation.Equals(confirmType))
            {
                line.SetConfirmedQty(GetConfirmedQty());
            }

            return line.Save(Get_TrxName());
        }	

        /// <summary>
        ///	Is Fully Confirmed
        /// </summary>
        /// <returns>true if Target = Confirmed qty</returns>
        public Boolean IsFullyConfirmed()
        {
            return GetTargetQty().CompareTo(GetConfirmedQty()) == 0;
        }


        /// <summary>
        /// Before Delete - do not delete
        ///
        /// </summary>
        /// <returns>false </returns>
        protected override Boolean BeforeDelete()
        {
            log.SaveError("Error", Msg.GetMsg(GetCtx(), "CannotDelete"));
            return false;
        }	

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">new</param>
        /// <returns>true</returns>
        protected override Boolean BeforeSave(Boolean newRecord)
        {
            //	Calculate Difference = Target - Confirmed - Scrapped
            Decimal difference = GetTargetQty();
            difference = Decimal.Subtract(difference, GetConfirmedQty());
            difference = Decimal.Subtract(difference, GetScrappedQty());
            SetDifferenceQty(difference);
            //
            return true;
        }	

    }
}
