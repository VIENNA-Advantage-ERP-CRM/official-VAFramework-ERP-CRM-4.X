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
/** Generated Model for W_Basket
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_W_Basket : PO
{
public X_W_Basket (Context ctx, int W_Basket_ID, Trx trxName) : base (ctx, W_Basket_ID, trxName)
{
/** if (W_Basket_ID == 0)
{
SetVAF_UserContact_ID (0);
SetSession_ID (null);
SetW_Basket_ID (0);
}
 */
}
public X_W_Basket (Ctx ctx, int W_Basket_ID, Trx trxName) : base (ctx, W_Basket_ID, trxName)
{
/** if (W_Basket_ID == 0)
{
SetVAF_UserContact_ID (0);
SetSession_ID (null);
SetW_Basket_ID (0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_W_Basket (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_W_Basket (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_W_Basket (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_W_Basket()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514384898L;
/** Last Updated Timestamp 7/29/2010 1:07:48 PM */
public static long updatedMS = 1280389068109L;
/** VAF_TableView_ID=402 */
public static int Table_ID;
 // =402;

/** TableName=W_Basket */
public static String Table_Name="W_Basket";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(4);
/** AccessLevel
@return 4 - System 
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
StringBuilder sb = new StringBuilder ("X_W_Basket[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set User/Contact.
@param VAF_UserContact_ID User within the system - Internal or Business Partner Contact */
public void SetVAF_UserContact_ID (int VAF_UserContact_ID)
{
if (VAF_UserContact_ID < 1) throw new ArgumentException ("VAF_UserContact_ID is mandatory.");
Set_Value ("VAF_UserContact_ID", VAF_UserContact_ID);
}
/** Get User/Contact.
@return User within the system - Internal or Business Partner Contact */
public int GetVAF_UserContact_ID() 
{
Object ii = Get_Value("VAF_UserContact_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Business Partner.
@param VAB_BusinessPartner_ID Identifies a Business Partner */
public void SetVAB_BusinessPartner_ID (int VAB_BusinessPartner_ID)
{
if (VAB_BusinessPartner_ID <= 0) Set_Value ("VAB_BusinessPartner_ID", null);
else
Set_Value ("VAB_BusinessPartner_ID", VAB_BusinessPartner_ID);
}
/** Get Business Partner.
@return Identifies a Business Partner */
public int GetVAB_BusinessPartner_ID() 
{
Object ii = Get_Value("VAB_BusinessPartner_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set EMail Address.
@param EMail Electronic Mail Address */
public void SetEMail (String EMail)
{
if (EMail != null && EMail.Length > 60)
{
log.Warning("Length > 60 - truncated");
EMail = EMail.Substring(0,60);
}
Set_Value ("EMail", EMail);
}
/** Get EMail Address.
@return Electronic Mail Address */
public String GetEMail() 
{
return (String)Get_Value("EMail");
}
/** Set Price List.
@param VAM_PriceList_ID Unique identifier of a Price List */
public void SetVAM_PriceList_ID (int VAM_PriceList_ID)
{
if (VAM_PriceList_ID <= 0) Set_Value ("VAM_PriceList_ID", null);
else
Set_Value ("VAM_PriceList_ID", VAM_PriceList_ID);
}
/** Get Price List.
@return Unique identifier of a Price List */
public int GetVAM_PriceList_ID() 
{
Object ii = Get_Value("VAM_PriceList_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Session ID.
@param Session_ID Session ID */
public void SetSession_ID (String Session_ID)
{
if (Session_ID == null) throw new ArgumentException ("Session_ID is mandatory.");
if (Session_ID.Length > 60)
{
log.Warning("Length > 60 - truncated");
Session_ID = Session_ID.Substring(0,60);
}
Set_Value ("Session_ID", Session_ID);
}
/** Get Session ID.
@return Session ID */
public String GetSession_ID() 
{
return (String)Get_Value("Session_ID");
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetSession_ID());
}
/** Set W_Basket_ID.
@param W_Basket_ID Web Basket */
public void SetW_Basket_ID (int W_Basket_ID)
{
if (W_Basket_ID < 1) throw new ArgumentException ("W_Basket_ID is mandatory.");
Set_ValueNoCheck ("W_Basket_ID", W_Basket_ID);
}
/** Get W_Basket_ID.
@return Web Basket */
public int GetW_Basket_ID() 
{
Object ii = Get_Value("W_Basket_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
}

}