namespace VAdvantage.Model{
/** Generated Model - DO NOT CHANGE */
using System;using System.Text;using VAdvantage.DataBase;using VAdvantage.Common;using VAdvantage.Classes;using VAdvantage.Process;using VAdvantage.Model;using VAdvantage.Utility;using System.Data;/** Generated Model for VAM_ProductCost_ForeignCurrency
 *  @author Raghu (Updated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAM_Cost_ForeignCurrency : PO{public X_VAM_Cost_ForeignCurrency (Context ctx, int VAM_ProductCost_ForeignCurrency_ID, Trx trxName) : base (ctx, VAM_ProductCost_ForeignCurrency_ID, trxName){/** if (VAM_ProductCost_ForeignCurrency_ID == 0){SetVAM_ProductCost_ForeignCurrency_ID (0);} */
}public X_VAM_Cost_ForeignCurrency (Ctx ctx, int VAM_ProductCost_ForeignCurrency_ID, Trx trxName) : base (ctx, VAM_ProductCost_ForeignCurrency_ID, trxName){/** if (VAM_ProductCost_ForeignCurrency_ID == 0){SetVAM_ProductCost_ForeignCurrency_ID (0);} */
}/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction  
*/
public X_VAM_Cost_ForeignCurrency (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName){}/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_Cost_ForeignCurrency (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName){}/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_Cost_ForeignCurrency (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName){}/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAM_Cost_ForeignCurrency(){ Table_ID = Get_Table_ID(Table_Name); model = new KeyNamePair(Table_ID,Table_Name);}/** Serial Version No */
static long serialVersionUID = 27763325247458L;/** Last Updated Timestamp 12/8/2016 5:55:30 PM */
public static long updatedMS = 1481199930669L;/** VAF_TableView_ID=1000686 */
public static int Table_ID; // =1000686;
/** TableName=VAM_ProductCost_ForeignCurrency */
public static String Table_Name="VAM_ProductCost_ForeignCurrency";
protected static KeyNamePair model;protected Decimal accessLevel = new Decimal(7);/** AccessLevel
@return 7 - System - Client - Org 
*/
protected override int Get_AccessLevel(){return Convert.ToInt32(accessLevel.ToString());}/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Context ctx){POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);return poi;}/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx){POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);return poi;}/** Info
@return info
*/
public override String ToString(){StringBuilder sb = new StringBuilder ("X_VAM_ProductCost_ForeignCurrency[").Append(Get_ID()).Append("]");return sb.ToString();}/** Set Business Partner.
@param VAB_BusinessPartner_ID Identifies a Customer/Prospect */
public void SetVAB_BusinessPartner_ID (int VAB_BusinessPartner_ID){if (VAB_BusinessPartner_ID <= 0) Set_Value ("VAB_BusinessPartner_ID", null);else
Set_Value ("VAB_BusinessPartner_ID", VAB_BusinessPartner_ID);}/** Get Business Partner.
@return Identifies a Customer/Prospect */
public int GetVAB_BusinessPartner_ID() {Object ii = Get_Value("VAB_BusinessPartner_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Currency.
@param VAB_Currency_ID The Currency for this record */
public void SetVAB_Currency_ID (int VAB_Currency_ID){if (VAB_Currency_ID <= 0) Set_Value ("VAB_Currency_ID", null);else
Set_Value ("VAB_Currency_ID", VAB_Currency_ID);}/** Get Currency.
@return The Currency for this record */
public int GetVAB_Currency_ID() {Object ii = Get_Value("VAB_Currency_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Invoice.
@param VAB_Invoice_ID Invoice Identifier */
public void SetVAB_Invoice_ID (int VAB_Invoice_ID){if (VAB_Invoice_ID <= 0) Set_Value ("VAB_Invoice_ID", null);else
Set_Value ("VAB_Invoice_ID", VAB_Invoice_ID);}/** Get Invoice.
@return Invoice Identifier */
public int GetVAB_Invoice_ID() {Object ii = Get_Value("VAB_Invoice_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Order.
@param VAB_Order_ID Sales Order */
public void SetVAB_Order_ID (int VAB_Order_ID){if (VAB_Order_ID <= 0) Set_Value ("VAB_Order_ID", null);else
Set_Value ("VAB_Order_ID", VAB_Order_ID);}/** Get Order.
@return Sales Order */
public int GetVAB_Order_ID() {Object ii = Get_Value("VAB_Order_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set CostPerUnit.
@param CostPerUnit CostPerUnit */
public void SetCostPerUnit (Decimal? CostPerUnit){Set_Value ("CostPerUnit", (Decimal?)CostPerUnit);}/** Get CostPerUnit.
@return CostPerUnit */
public Decimal GetCostPerUnit() {Object bd =Get_Value("CostPerUnit");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Accumulated Amt.
@param CumulatedAmt Total Amount */
public void SetCumulatedAmt (Decimal? CumulatedAmt){Set_Value ("CumulatedAmt", (Decimal?)CumulatedAmt);}/** Get Accumulated Amt.
@return Total Amount */
public Decimal GetCumulatedAmt() {Object bd =Get_Value("CumulatedAmt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Accumulated Qty.
@param CumulatedQty Total Quantity */
public void SetCumulatedQty (Decimal? CumulatedQty){Set_Value ("CumulatedQty", (Decimal?)CumulatedQty);}/** Get Accumulated Qty.
@return Total Quantity */
public Decimal GetCumulatedQty() {Object bd =Get_Value("CumulatedQty");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Export.
@param Export_ID Export */
public void SetExport_ID (String Export_ID){if (Export_ID != null && Export_ID.Length > 50){log.Warning("Length > 50 - truncated");Export_ID = Export_ID.Substring(0,50);}Set_Value ("Export_ID", Export_ID);}/** Get Export.
@return Export */
public String GetExport_ID() {return (String)Get_Value("Export_ID");}/** Set Attribute Set Instance.
@param VAM_PFeature_SetInstance_ID Product Attribute Set Instance */
public void SetVAM_PFeature_SetInstance_ID (int VAM_PFeature_SetInstance_ID){if (VAM_PFeature_SetInstance_ID <= 0) Set_Value ("VAM_PFeature_SetInstance_ID", null);else
Set_Value ("VAM_PFeature_SetInstance_ID", VAM_PFeature_SetInstance_ID);}/** Get Attribute Set Instance.
@return Product Attribute Set Instance */
public int GetVAM_PFeature_SetInstance_ID() {Object ii = Get_Value("VAM_PFeature_SetInstance_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Cost Element.
@param VAM_ProductCostElement_ID Product Cost Element */
public void SetVAM_ProductCostElement_ID (int VAM_ProductCostElement_ID){if (VAM_ProductCostElement_ID <= 0) Set_Value ("VAM_ProductCostElement_ID", null);else
Set_Value ("VAM_ProductCostElement_ID", VAM_ProductCostElement_ID);}/** Get Cost Element.
@return Product Cost Element */
public int GetVAM_ProductCostElement_ID() {Object ii = Get_Value("VAM_ProductCostElement_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set VAM_ProductCost_ForeignCurrency_ID.
@param VAM_ProductCost_ForeignCurrency_ID VAM_ProductCost_ForeignCurrency_ID */
public void SetVAM_ProductCost_ForeignCurrency_ID (int VAM_ProductCost_ForeignCurrency_ID){if (VAM_ProductCost_ForeignCurrency_ID < 1) throw new ArgumentException ("VAM_ProductCost_ForeignCurrency_ID is mandatory.");Set_ValueNoCheck ("VAM_ProductCost_ForeignCurrency_ID", VAM_ProductCost_ForeignCurrency_ID);}/** Get VAM_ProductCost_ForeignCurrency_ID.
@return VAM_ProductCost_ForeignCurrency_ID */
public int GetVAM_ProductCost_ForeignCurrency_ID() {Object ii = Get_Value("VAM_ProductCost_ForeignCurrency_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Product.
@param VAM_Product_ID Product, Service, Item */
public void SetVAM_Product_ID (int VAM_Product_ID){if (VAM_Product_ID <= 0) Set_Value ("VAM_Product_ID", null);else
Set_Value ("VAM_Product_ID", VAM_Product_ID);}/** Get Product.
@return Product, Service, Item */
public int GetVAM_Product_ID() {Object ii = Get_Value("VAM_Product_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}}
}