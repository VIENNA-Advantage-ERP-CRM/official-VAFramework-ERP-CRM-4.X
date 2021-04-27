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
    /** Generated Model for VAM_BOMVAMProduct
     *  @author Jagmohan Bhatt (generated) 
     *  @version Vienna Framework 1.1.1 - $Id$ */
    public class X_VAM_BOMVAMProduct : PO
    {
        public X_VAM_BOMVAMProduct(Context ctx, int VAM_BOMVAMProduct_ID, Trx trxName)
            : base(ctx, VAM_BOMVAMProduct_ID, trxName)
        {
            /** if (VAM_BOMVAMProduct_ID == 0)
            {
            SetBOMVAMProductType (null);	// S
            SetBOMQty (0.0);	// 1
            SetIsPhantom (false);
            SetLeadTimeOffset (0);
            SetLine (0);	// @SQL=SELECT NVL(MAX(Line),0)+10 AS DefaultValue FROM VAM_BOMVAMProduct WHERE VAM_BOM_ID=@VAM_BOM_ID@
            SetVAM_BOMVAMProduct_ID (0);
            SetVAM_BOM_ID (0);
            }
             */
        }
        public X_VAM_BOMVAMProduct(Ctx ctx, int VAM_BOMVAMProduct_ID, Trx trxName)
            : base(ctx, VAM_BOMVAMProduct_ID, trxName)
        {
            /** if (VAM_BOMVAMProduct_ID == 0)
            {
            SetBOMVAMProductType (null);	// S
            SetBOMQty (0.0);	// 1
            SetIsPhantom (false);
            SetLeadTimeOffset (0);
            SetLine (0);	// @SQL=SELECT NVL(MAX(Line),0)+10 AS DefaultValue FROM VAM_BOMVAMProduct WHERE VAM_BOM_ID=@VAM_BOM_ID@
            SetVAM_BOMVAMProduct_ID (0);
            SetVAM_BOM_ID (0);
            }
             */
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAM_BOMVAMProduct(Context ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAM_BOMVAMProduct(Ctx ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAM_BOMVAMProduct(Ctx ctx, IDataReader dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }
        /** Static Constructor 
         Set Table ID By Table Name
         added by ->Harwinder */
        static X_VAM_BOMVAMProduct()
        {
            Table_ID = Get_Table_ID(Table_Name);
            model = new KeyNamePair(Table_ID, Table_Name);
        }
        /** Serial Version No */
        //static long serialVersionUID 27562514378503L;
        /** Last Updated Timestamp 7/29/2010 1:07:41 PM */
        public static long updatedMS = 1280389061714L;
        /** VAF_TableView_ID=801 */
        public static int Table_ID;
        // =801;

        /** TableName=VAM_BOMVAMProduct */
        public static String Table_Name = "VAM_BOMVAMProduct";

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
        protected override POInfo InitPO(Context ctx)
        {
            POInfo poi = POInfo.GetPOInfo(ctx, Table_ID);
            return poi;
        }
        /** Load Meta Data
        @param ctx context
        @return PO Info
        */
        protected override POInfo InitPO(Ctx ctx)
        {
            POInfo poi = POInfo.GetPOInfo(ctx, Table_ID);
            return poi;
        }
        /** Info
        @return info
        */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("X_VAM_BOMVAMProduct[").Append(Get_ID()).Append("]");
            return sb.ToString();
        }

        /** BOMVAMProductType VAF_Control_Ref_ID=349 */
        public static int BOMVAMProductTYPE_VAF_Control_Ref_ID = 349;
        /** Alternative = A */
        public static String BOMVAMProductTYPE_Alternative = "A";
        /** Alternative (Default) = D */
        public static String BOMVAMProductTYPE_AlternativeDefault = "D";
        /** Optional Product = O */
        public static String BOMVAMProductTYPE_OptionalProduct = "O";
        /** Standard Product = S */
        public static String BOMVAMProductTYPE_StandardProduct = "S";
        /** Outside Processing = X */
        public static String BOMVAMProductTYPE_OutsideProcessing = "X";
        /** Is test a valid value.
        @param test testvalue
        @returns true if valid **/
        public bool IsBOMVAMProductTypeValid(String test)
        {
            return test.Equals("A") || test.Equals("D") || test.Equals("O") || test.Equals("S") || test.Equals("X");
        }
        /** Set Component Type.
        @param BOMVAMProductType BOM Product Type */
        public void SetBOMVAMProductType(String BOMVAMProductType)
        {
            if (BOMVAMProductType == null) throw new ArgumentException("BOMVAMProductType is mandatory");
            if (!IsBOMVAMProductTypeValid(BOMVAMProductType))
                throw new ArgumentException("BOMVAMProductType Invalid value - " + BOMVAMProductType + " - Reference_ID=349 - A - D - O - S - X");
            if (BOMVAMProductType.Length > 1)
            {
                log.Warning("Length > 1 - truncated");
                BOMVAMProductType = BOMVAMProductType.Substring(0, 1);
            }
            Set_Value("BOMVAMProductType", BOMVAMProductType);
        }
        /** Get Component Type.
        @return BOM Product Type */
        public String GetBOMVAMProductType()
        {
            return (String)Get_Value("BOMVAMProductType");
        }
        /** Set BOM Quantity.
        @param BOMQty Bill of Materials Quantity */
        public void SetBOMQty(Decimal? BOMQty)
        {
            if (BOMQty == null) throw new ArgumentException("BOMQty is mandatory.");
            Set_Value("BOMQty", (Decimal?)BOMQty);
        }
        /** Get BOM Quantity.
        @return Bill of Materials Quantity */
        public Decimal GetBOMQty()
        {
            Object bd = Get_Value("BOMQty");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Description.
        @param Description Optional short description of the record */
        public void SetDescription(String Description)
        {
            if (Description != null && Description.Length > 255)
            {
                log.Warning("Length > 255 - truncated");
                Description = Description.Substring(0, 255);
            }
            Set_Value("Description", Description);
        }
        /** Get Description.
        @return Optional short description of the record */
        public String GetDescription()
        {
            return (String)Get_Value("Description");
        }
        /** Set Comment.
        @param Help Comment, Help or Hint */
        public void SetHelp(String Help)
        {
            if (Help != null && Help.Length > 2000)
            {
                log.Warning("Length > 2000 - truncated");
                Help = Help.Substring(0, 2000);
            }
            Set_Value("Help", Help);
        }
        /** Get Comment.
        @return Comment, Help or Hint */
        public String GetHelp()
        {
            return (String)Get_Value("Help");
        }
        /** Set Phantom.
        @param IsPhantom Phantom Component */
        public void SetIsPhantom(Boolean IsPhantom)
        {
            Set_Value("IsPhantom", IsPhantom);
        }
        /** Get Phantom.
        @return Phantom Component */
        public Boolean IsPhantom()
        {
            Object oo = Get_Value("IsPhantom");
            if (oo != null)
            {
                if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
                return "Y".Equals(oo);
            }
            return false;
        }
        /** Set Lead Time Offset.
        @param LeadTimeOffset Optional Lead Time offest before starting production */
        public void SetLeadTimeOffset(int LeadTimeOffset)
        {
            Set_Value("LeadTimeOffset", LeadTimeOffset);
        }
        /** Get Lead Time Offset.
        @return Optional Lead Time offest before starting production */
        public int GetLeadTimeOffset()
        {
            Object ii = Get_Value("LeadTimeOffset");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Line No.
        @param Line Unique line for this document */
        public void SetLine(int Line)
        {
            Set_Value("Line", Line);
        }
        /** Get Line No.
        @return Unique line for this document */
        public int GetLine()
        {
            Object ii = Get_Value("Line");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Get Record ID/ColumnName
        @return ID/ColumnName pair */
        public KeyNamePair GetKeyNamePair()
        {
            return new KeyNamePair(Get_ID(), GetLine().ToString());
        }
        /** Set Attribute Set Instance.
        @param VAM_PFeature_SetInstance_ID Product Attribute Set Instance */
        public void SetVAM_PFeature_SetInstance_ID(int VAM_PFeature_SetInstance_ID)
        {
            if (VAM_PFeature_SetInstance_ID <= 0) Set_Value("VAM_PFeature_SetInstance_ID", null);
            else
                Set_Value("VAM_PFeature_SetInstance_ID", VAM_PFeature_SetInstance_ID);
        }
        /** Get Attribute Set Instance.
        @return Product Attribute Set Instance */
        public int GetVAM_PFeature_SetInstance_ID()
        {
            Object ii = Get_Value("VAM_PFeature_SetInstance_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Alternative Group.
        @param VAM_BOMSubsitutue_ID Product BOM Alternative Group */
        public void SetVAM_BOMSubsitutue_ID(int VAM_BOMSubsitutue_ID)
        {
            if (VAM_BOMSubsitutue_ID <= 0) Set_Value("VAM_BOMSubsitutue_ID", null);
            else
                Set_Value("VAM_BOMSubsitutue_ID", VAM_BOMSubsitutue_ID);
        }
        /** Get Alternative Group.
        @return Product BOM Alternative Group */
        public int GetVAM_BOMSubsitutue_ID()
        {
            Object ii = Get_Value("VAM_BOMSubsitutue_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set BOM Component.
        @param VAM_BOMVAMProduct_ID Bill of Material Component (Product) */
        public void SetVAM_BOMVAMProduct_ID(int VAM_BOMVAMProduct_ID)
        {
            if (VAM_BOMVAMProduct_ID < 1) throw new ArgumentException("VAM_BOMVAMProduct_ID is mandatory.");
            Set_ValueNoCheck("VAM_BOMVAMProduct_ID", VAM_BOMVAMProduct_ID);
        }
        /** Get BOM Component.
        @return Bill of Material Component (Product) */
        public int GetVAM_BOMVAMProduct_ID()
        {
            Object ii = Get_Value("VAM_BOMVAMProduct_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set BOM.
        @param VAM_BOM_ID Bill of Material */
        public void SetVAM_BOM_ID(int VAM_BOM_ID)
        {
            if (VAM_BOM_ID < 1) throw new ArgumentException("VAM_BOM_ID is mandatory.");
            Set_ValueNoCheck("VAM_BOM_ID", VAM_BOM_ID);
        }
        /** Get BOM.
        @return Bill of Material */
        public int GetVAM_BOM_ID()
        {
            Object ii = Get_Value("VAM_BOM_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Change Notice.
        @param M_ChangeNotice_ID Bill of Materials (Engineering) Change Notice (Version) */
        public void SetM_ChangeNotice_ID(int M_ChangeNotice_ID)
        {
            if (M_ChangeNotice_ID <= 0) Set_Value("M_ChangeNotice_ID", null);
            else
                Set_Value("M_ChangeNotice_ID", M_ChangeNotice_ID);
        }
        /** Get Change Notice.
        @return Bill of Materials (Engineering) Change Notice (Version) */
        public int GetM_ChangeNotice_ID()
        {
            Object ii = Get_Value("M_ChangeNotice_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }

        /** VAM_ProductBOM_ID VAF_Control_Ref_ID=162 */
        public static int VAM_ProductBOM_ID_VAF_Control_Ref_ID = 162;
        /** Set BOM Product.
        @param VAM_ProductBOM_ID Bill of Material Component Product */
        public void SetVAM_ProductBOM_ID(int VAM_ProductBOM_ID)
        {
            if (VAM_ProductBOM_ID <= 0) Set_Value("VAM_ProductBOM_ID", null);
            else
                Set_Value("VAM_ProductBOM_ID", VAM_ProductBOM_ID);
        }
        /** Get BOM Product.
        @return Bill of Material Component Product */
        public int GetVAM_ProductBOM_ID()
        {
            Object ii = Get_Value("VAM_ProductBOM_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Product Operation.
        @param VAM_ProductOperation_ID Product Manufacturing Operation */
        public void SetVAM_ProductOperation_ID(int VAM_ProductOperation_ID)
        {
            if (VAM_ProductOperation_ID <= 0) Set_Value("VAM_ProductOperation_ID", null);
            else
                Set_Value("VAM_ProductOperation_ID", VAM_ProductOperation_ID);
        }
        /** Get Product Operation.
        @return Product Manufacturing Operation */
        public int GetVAM_ProductOperation_ID()
        {
            Object ii = Get_Value("VAM_ProductOperation_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Sequence.
        @param SeqNo Method of ordering elements;
         lowest number comes first */
        public void SetSeqNo(int SeqNo)
        {
            Set_Value("SeqNo", SeqNo);
        }
        /** Get Sequence.
        @return Method of ordering elements;
         lowest number comes first */
        public int GetSeqNo()
        {
            Object ii = Get_Value("SeqNo");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }

        ////Manfacturing
        /** Set Component BOM.
@param VAM_ProductBOMVersion_ID BOM for a component */
        public void SetVAM_ProductBOMVersion_ID(int VAM_ProductBOMVersion_ID)
        {
            if (VAM_ProductBOMVersion_ID <= 0) Set_Value("VAM_ProductBOMVersion_ID", null);
            else
                Set_Value("VAM_ProductBOMVersion_ID", VAM_ProductBOMVersion_ID);
        }
        /** Get Component BOM.
        @return BOM for a component */
        public int GetVAM_ProductBOMVersion_ID()
        {
            Object ii = Get_Value("VAM_ProductBOMVersion_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }

        /** Set Operation Sequence No.
@param OperationSeqNo Indicates the work order operation sequence number in which the BOM component is to be consumed */
        public void SetOperationSeqNo(int OperationSeqNo)
        {
            Set_Value("OperationSeqNo", OperationSeqNo);
        }
        /** Get Operation Sequence No.
        @return Indicates the work order operation sequence number in which the BOM component is to be consumed */
        public int GetOperationSeqNo()
        {
            Object ii = Get_Value("OperationSeqNo");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Supply Type.
@param SupplyType Supply type for components */
        public void SetSupplyType(String SupplyType)
        {
            if (SupplyType == null) throw new ArgumentException("SupplyType is mandatory");
            if (!IsSupplyTypeValid(SupplyType))
            {
                throw new ArgumentException("SupplyType Invalid value - " + SupplyType + " - Reference_ID=444 - A - O - P");
            }
            if (SupplyType.Length > 1)
            {
                log.Warning("Length > 1 - truncated");
                SupplyType = SupplyType.Substring(0, 1);
            }
            Set_Value("SupplyType", SupplyType);
        }
        /** Get Supply Type.
        @return Supply type for components */
        public String GetSupplyType()
        {
            return (String)Get_Value("SupplyType");
        }

        /** Is test a valid value.
@param test testvalue
@returns true if valid **/
        public bool IsSupplyTypeValid(String test)
        {
            return test.Equals("A") || test.Equals("O") || test.Equals("P");
        }


        /** BasisType VAF_Control_Ref_ID=1000032 */
        public static int BASISTYPE_VAF_Control_Ref_ID = 1000032;/** Per Batch = B */
        public static String BASISTYPE_PerBatch = "B";/** Per Item = I */
        public static String BASISTYPE_PerItem = "I";/** Is test a valid value.
@param test testvalue
@returns true if valid **/
        public bool IsBasisTypeValid(String test) { return test.Equals("B") || test.Equals("I"); }/** Set Cost Basis Type.
@param BasisType Indicates the option to consume and charge materials and resources */
        public void SetBasisType(String BasisType)
        {
            if (BasisType == null) throw new ArgumentException("BasisType is mandatory"); if (!IsBasisTypeValid(BasisType))
                throw new ArgumentException("BasisType Invalid value - " + BasisType + " - Reference_ID=1000032 - B - I"); if (BasisType.Length > 1) { log.Warning("Length > 1 - truncated"); BasisType = BasisType.Substring(0, 1); } Set_Value("BasisType", BasisType);
        }


//        /** Set Operation Sequence No.
//@param OperationSeqNo Indicates the work order operation sequence number in which the BOM component is to be consumed */
//        public void SetOperationSeqNo(int OperationSeqNo) { Set_Value("OperationSeqNo", OperationSeqNo); }/** Get Operation Sequence No.
//@return Indicates the work order operation sequence number in which the BOM component is to be consumed */
//        public int GetOperationSeqNo() { Object ii = Get_Value("OperationSeqNo"); if (ii == null) return 0; return Convert.ToInt32(ii); }


    }

}