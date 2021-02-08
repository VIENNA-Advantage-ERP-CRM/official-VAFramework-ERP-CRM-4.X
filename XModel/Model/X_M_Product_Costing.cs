namespace VAdvantage.Model
{

/** Generated Model - DO NOT CHANGE */
using System;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Model;
using VAdvantage.Utility;
using System.Data;
/** Generated Model for VAM_ProductCosting
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAM_ProductCosting : PO
{
public X_VAM_ProductCosting (Context ctx, int VAM_ProductCosting_ID, Trx trxName) : base (ctx, VAM_ProductCosting_ID, trxName)
{
/** if (VAM_ProductCosting_ID == 0)
{
SetVAB_AccountBook_ID (0);
SetCostAverage (0.0);
SetCostAverageCumAmt (0.0);
SetCostAverageCumQty (0.0);
SetCostStandard (0.0);
SetCostStandardCumAmt (0.0);
SetCostStandardCumQty (0.0);
SetCostStandardPOAmt (0.0);
SetCostStandardPOQty (0.0);
SetCurrentCostPrice (0.0);
SetFutureCostPrice (0.0);
SetVAM_Product_ID (0);
SetPriceLastInv (0.0);
SetPriceLastPO (0.0);
SetTotalInvAmt (0.0);
SetTotalInvQty (0.0);
}
 */
}
public X_VAM_ProductCosting (Ctx ctx, int VAM_ProductCosting_ID, Trx trxName) : base (ctx, VAM_ProductCosting_ID, trxName)
{
/** if (VAM_ProductCosting_ID == 0)
{
SetVAB_AccountBook_ID (0);
SetCostAverage (0.0);
SetCostAverageCumAmt (0.0);
SetCostAverageCumQty (0.0);
SetCostStandard (0.0);
SetCostStandardCumAmt (0.0);
SetCostStandardCumQty (0.0);
SetCostStandardPOAmt (0.0);
SetCostStandardPOQty (0.0);
SetCurrentCostPrice (0.0);
SetFutureCostPrice (0.0);
SetVAM_Product_ID (0);
SetPriceLastInv (0.0);
SetPriceLastPO (0.0);
SetTotalInvAmt (0.0);
SetTotalInvQty (0.0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_ProductCosting (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_ProductCosting (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_ProductCosting (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAM_ProductCosting()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514380854L;
/** Last Updated Timestamp 7/29/2010 1:07:44 PM */
public static long updatedMS = 1280389064065L;
/** VAF_TableView_ID=327 */
public static int Table_ID;
 // =327;

/** TableName=VAM_ProductCosting */
public static String Table_Name="VAM_ProductCosting";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(3);
/** AccessLevel
@return 3 - Client - Org 
*/
protected override int Get_AccessLevel()
{
return Convert.ToInt32(accessLevel.ToString());
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO(Context ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Info
@return info
*/
public override String ToString()
{
StringBuilder sb = new StringBuilder ("X_VAM_ProductCosting[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Accounting Schema.
@param VAB_AccountBook_ID Rules for accounting */
public void SetVAB_AccountBook_ID (int VAB_AccountBook_ID)
{
if (VAB_AccountBook_ID < 1) throw new ArgumentException ("VAB_AccountBook_ID is mandatory.");
Set_ValueNoCheck ("VAB_AccountBook_ID", VAB_AccountBook_ID);
}
/** Get Accounting Schema.
@return Rules for accounting */
public int GetVAB_AccountBook_ID() 
{
Object ii = Get_Value("VAB_AccountBook_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetVAB_AccountBook_ID().ToString());
}
/** Set Average Cost.
@param CostAverage Weighted average costs */
public void SetCostAverage (Decimal? CostAverage)
{
if (CostAverage == null) throw new ArgumentException ("CostAverage is mandatory.");
Set_ValueNoCheck ("CostAverage", (Decimal?)CostAverage);
}
/** Get Average Cost.
@return Weighted average costs */
public Decimal GetCostAverage() 
{
Object bd =Get_Value("CostAverage");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Average Cost Amount Sum.
@param CostAverageCumAmt Cumulative average cost amounts (internal) */
public void SetCostAverageCumAmt (Decimal? CostAverageCumAmt)
{
if (CostAverageCumAmt == null) throw new ArgumentException ("CostAverageCumAmt is mandatory.");
Set_ValueNoCheck ("CostAverageCumAmt", (Decimal?)CostAverageCumAmt);
}
/** Get Average Cost Amount Sum.
@return Cumulative average cost amounts (internal) */
public Decimal GetCostAverageCumAmt() 
{
Object bd =Get_Value("CostAverageCumAmt");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Average Cost Quantity Sum.
@param CostAverageCumQty Cumulative average cost quantities (internal) */
public void SetCostAverageCumQty (Decimal? CostAverageCumQty)
{
if (CostAverageCumQty == null) throw new ArgumentException ("CostAverageCumQty is mandatory.");
Set_ValueNoCheck ("CostAverageCumQty", (Decimal?)CostAverageCumQty);
}
/** Get Average Cost Quantity Sum.
@return Cumulative average cost quantities (internal) */
public Decimal GetCostAverageCumQty() 
{
Object bd =Get_Value("CostAverageCumQty");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Standard Cost.
@param CostStandard Standard Costs */
public void SetCostStandard (Decimal? CostStandard)
{
if (CostStandard == null) throw new ArgumentException ("CostStandard is mandatory.");
Set_ValueNoCheck ("CostStandard", (Decimal?)CostStandard);
}
/** Get Standard Cost.
@return Standard Costs */
public Decimal GetCostStandard() 
{
Object bd =Get_Value("CostStandard");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Std Cost Amount Sum.
@param CostStandardCumAmt Standard Cost Invoice Amount Sum (internal) */
public void SetCostStandardCumAmt (Decimal? CostStandardCumAmt)
{
if (CostStandardCumAmt == null) throw new ArgumentException ("CostStandardCumAmt is mandatory.");
Set_ValueNoCheck ("CostStandardCumAmt", (Decimal?)CostStandardCumAmt);
}
/** Get Std Cost Amount Sum.
@return Standard Cost Invoice Amount Sum (internal) */
public Decimal GetCostStandardCumAmt() 
{
Object bd =Get_Value("CostStandardCumAmt");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Std Cost Quantity Sum.
@param CostStandardCumQty Standard Cost Invoice Quantity Sum (internal) */
public void SetCostStandardCumQty (Decimal? CostStandardCumQty)
{
if (CostStandardCumQty == null) throw new ArgumentException ("CostStandardCumQty is mandatory.");
Set_ValueNoCheck ("CostStandardCumQty", (Decimal?)CostStandardCumQty);
}
/** Get Std Cost Quantity Sum.
@return Standard Cost Invoice Quantity Sum (internal) */
public Decimal GetCostStandardCumQty() 
{
Object bd =Get_Value("CostStandardCumQty");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Std PO Cost Amount Sum.
@param CostStandardPOAmt Standard Cost Purchase Order Amount Sum (internal) */
public void SetCostStandardPOAmt (Decimal? CostStandardPOAmt)
{
if (CostStandardPOAmt == null) throw new ArgumentException ("CostStandardPOAmt is mandatory.");
Set_ValueNoCheck ("CostStandardPOAmt", (Decimal?)CostStandardPOAmt);
}
/** Get Std PO Cost Amount Sum.
@return Standard Cost Purchase Order Amount Sum (internal) */
public Decimal GetCostStandardPOAmt() 
{
Object bd =Get_Value("CostStandardPOAmt");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Std PO Cost Quantity Sum.
@param CostStandardPOQty Standard Cost Purchase Order Quantity Sum (internal) */
public void SetCostStandardPOQty (Decimal? CostStandardPOQty)
{
if (CostStandardPOQty == null) throw new ArgumentException ("CostStandardPOQty is mandatory.");
Set_ValueNoCheck ("CostStandardPOQty", (Decimal?)CostStandardPOQty);
}
/** Get Std PO Cost Quantity Sum.
@return Standard Cost Purchase Order Quantity Sum (internal) */
public Decimal GetCostStandardPOQty() 
{
Object bd =Get_Value("CostStandardPOQty");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Current Cost Price.
@param CurrentCostPrice The currently used cost price */
public void SetCurrentCostPrice (Decimal? CurrentCostPrice)
{
if (CurrentCostPrice == null) throw new ArgumentException ("CurrentCostPrice is mandatory.");
Set_Value ("CurrentCostPrice", (Decimal?)CurrentCostPrice);
}
/** Get Current Cost Price.
@return The currently used cost price */
public Decimal GetCurrentCostPrice() 
{
Object bd =Get_Value("CurrentCostPrice");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Future Cost Price.
@param FutureCostPrice Future Cost Price */
public void SetFutureCostPrice (Decimal? FutureCostPrice)
{
if (FutureCostPrice == null) throw new ArgumentException ("FutureCostPrice is mandatory.");
Set_Value ("FutureCostPrice", (Decimal?)FutureCostPrice);
}
/** Get Future Cost Price.
@return Future Cost Price */
public Decimal GetFutureCostPrice() 
{
Object bd =Get_Value("FutureCostPrice");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Product.
@param VAM_Product_ID Product, Service, Item */
public void SetVAM_Product_ID (int VAM_Product_ID)
{
if (VAM_Product_ID < 1) throw new ArgumentException ("VAM_Product_ID is mandatory.");
Set_ValueNoCheck ("VAM_Product_ID", VAM_Product_ID);
}
/** Get Product.
@return Product, Service, Item */
public int GetVAM_Product_ID() 
{
Object ii = Get_Value("VAM_Product_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Last Invoice Price.
@param PriceLastInv Price of the last invoice for the product */
public void SetPriceLastInv (Decimal? PriceLastInv)
{
if (PriceLastInv == null) throw new ArgumentException ("PriceLastInv is mandatory.");
Set_ValueNoCheck ("PriceLastInv", (Decimal?)PriceLastInv);
}
/** Get Last Invoice Price.
@return Price of the last invoice for the product */
public Decimal GetPriceLastInv() 
{
Object bd =Get_Value("PriceLastInv");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Last PO Price.
@param PriceLastPO Price of the last purchase order for the product */
public void SetPriceLastPO (Decimal? PriceLastPO)
{
if (PriceLastPO == null) throw new ArgumentException ("PriceLastPO is mandatory.");
Set_ValueNoCheck ("PriceLastPO", (Decimal?)PriceLastPO);
}
/** Get Last PO Price.
@return Price of the last purchase order for the product */
public Decimal GetPriceLastPO() 
{
Object bd =Get_Value("PriceLastPO");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Total Invoice Amount.
@param TotalInvAmt Cumulative total lifetime invoice amount */
public void SetTotalInvAmt (Decimal? TotalInvAmt)
{
if (TotalInvAmt == null) throw new ArgumentException ("TotalInvAmt is mandatory.");
Set_ValueNoCheck ("TotalInvAmt", (Decimal?)TotalInvAmt);
}
/** Get Total Invoice Amount.
@return Cumulative total lifetime invoice amount */
public Decimal GetTotalInvAmt() 
{
Object bd =Get_Value("TotalInvAmt");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Total Invoice Quantity.
@param TotalInvQty Cumulative total lifetime invoice quantity */
public void SetTotalInvQty (Decimal? TotalInvQty)
{
if (TotalInvQty == null) throw new ArgumentException ("TotalInvQty is mandatory.");
Set_ValueNoCheck ("TotalInvQty", (Decimal?)TotalInvQty);
}
/** Get Total Invoice Quantity.
@return Cumulative total lifetime invoice quantity */
public Decimal GetTotalInvQty() 
{
Object bd =Get_Value("TotalInvQty");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
}

}
