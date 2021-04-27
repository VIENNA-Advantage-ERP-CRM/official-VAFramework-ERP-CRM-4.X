﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using VAdvantage.Logging;

namespace VAdvantage.Classes
{/****************************************************************************?
     * 
     *     VLookUpInfo class
     *  Info Class for Lookup SQL (ValueObject)
     * 
    *****************************************************************************/
    public class VLookUpInfo
    {
        #region "Declaration"

        /** SQL Query       */
        public string query = null;

        public string queryAll = null;

        /** Table Name      */
        public string tableName = "";
        /** Key Column      */
        public string keyColumn = "";
        /** Zoom Window     */
        public int zoomWindow;
        /** Zoom Window     */
        public int zoomWindowPO;
        /** Zoom Query      */
        public IQuery zoomQuery = null;

        /** Direct Access Query 	*/
        public string queryDirect = "";
        /** Parent Flag     */
        public bool isParent = false;
        /** Key Flag     	*/
        public bool isKey = false;
        /** Validation code */
        public string validationCode = "";
        /** Validation flag */
        public bool isValidated = false;

        /**	VAF_Column_Info or VAF_Job_Para	*/
        public int column_ID;
        /** Real VAF_Control_Ref_ID				*/
        public int VAF_Control_Ref_Value_ID;
        /** CreadedBy?updatedBy					*/
        public bool isCreadedUpdatedBy = false;

        /* Display Column SQL Query*/
        public string displayColSubQ = null;

        #endregion

        /// <summary>
        /// Construtor
        /// </summary>
        public VLookUpInfo()
        {
        }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="qry">sql qry text</param>
        /// <param name="tablename">table name</param>
        /// <param name="keyColName">key col name</param>
        /// <param name="zoomWindowId">zoom win id</param>
        /// <param name="zoomWindowPOId">zoom windoe po id</param>
        /// <param name="zoomqry">zoom sql qury text</param>
        public VLookUpInfo(string qry, string tablename, string keyColName, int zoomWindowId, int zoomWindowPOId, IQuery zoomqry)
        {
            if (qry == null)
                throw new System.Exception("SqlQuery is null");
            query = qry;
            if (keyColName == null)
                throw new System.Exception("keyColumn is null");
            tableName = tablename;
            keyColumn = keyColName;
            zoomWindow = zoomWindowId;
            zoomWindowPO = zoomWindowPOId;
            zoomQuery = zoomqry;
        }

        /// <summary>
        /// Clone class object
        /// </summary>
        /// <returns></returns>
        public VLookUpInfo Clone()
        {
            VLookUpInfo clone = new VLookUpInfo(this.query, this.tableName, this.keyColumn, this.zoomWindow, this.zoomWindowPO, this.zoomQuery);
            return clone;
        }

        /// <summary>
        /// Get first VAF_Control_Ref_ID of a matching Reference Name.
        /// Can have SQL LIKE place holders.
        /// (This is more a development tool than used for production)
        /// </summary>
        /// <param name="ReferenceName">reference name</param>
        /// <returns>VAF_Control_Ref_ID</returns>
        public static int GetVAF_Control_Ref_ID(String ReferenceName)
        {
            int RetValue = 0;
            String sql = "SELECT VAF_Control_Ref_ID,Name,ValidationType,IsActive "
                + "FROM VAF_Control_Ref WHERE Name LIKE @name";

            IDataReader dr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@name", ReferenceName);
                dr = DataBase.DB.ExecuteReader(sql, param);
                //
                int i = 0;
                int id = 0;
                String RefName = "";
                String ValidationType = "";
                bool IsActive = false;
                while (dr.Read())
                {
                    id = Utility.Util.GetValueOfInt(dr[0].ToString());
                    if (i == 0)
                        RetValue = id;
                    RefName = dr[1].ToString();
                    ValidationType = dr[2].ToString();
                    IsActive = dr[3].ToString().Equals("Y");
                    VLogger.Get().Config("VAF_Control_Ref Name=" + RefName + ", ID=" + id + ", Type=" + ValidationType + ", Active=" + IsActive);
                }
                dr.Close();
                dr = null;
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                    dr = null;
                }
                VLogger.Get().Log(Level.SEVERE, sql, e);
            }
            return RetValue;
        }


        /// <summary>
        ///  Get first VAF_Column_ID of matching ColumnName.
        ///  Can have SQL LIKE place holders.
        ///  (This is more a development tool than used for production)
        /// </summary>
        /// <param name="ColumnName">column name</param>
        /// <returns>VAF_Column_ID</returns>
        public static int GetVAF_Column_ID(String columnName)
        {
            int RetValue = 0;
            String sql = "SELECT c.VAF_Column_ID,c.ColumnName,t.tableName "
                + "FROM VAF_Column c, VAF_TableView t "
                + "WHERE c.ColumnName LIKE @name AND c.VAF_TableView_ID=t.VAF_TableView_ID";
            IDataReader dr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@name", columnName);
                dr = DataBase.DB.ExecuteReader(sql, param);
                //
                int i = 0;
                int id = 0;
                String colName = "";
                String tabName = "";
                while (dr.Read())
                {
                    id = Utility.Util.GetValueOfInt(dr[0].ToString());
                    if (i == 0)
                        RetValue = id;
                    colName = dr[1].ToString();
                    tabName = dr[2].ToString();
                    VLogger.Get().Config("Name=" + colName + ", ID=" + id + ", Table=" + tabName);
                }
                dr.Close();
                dr = null;
                param = null;
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                    dr = null;
                }
                VLogger.Get().Log(Level.SEVERE, sql, e);
            }
            return RetValue;
        }
    }

    /***********************************************************************************/

    //    LookupDisplayColumn
    //Lookup Display Column Value Object

    /**********************************************************************************/
    public class LookupDisplayColumn
    {
        /// <summary>
        ///lookup Column Value Object
        /// </summary>
        /// <param name="columnName">coumn name</param>
        /// <param name="isTranslated">translated</param>
        /// <param name="VAF_Control_Ref_ID">display type</param>
        /// <param name="VAF_Control_Ref_Value_ID">list/table ref id</param>
        public LookupDisplayColumn(string columnName, bool isTranslated,
            int VAF_Control_Ref_ID, int VAF_Control_Ref_Value_ID)
        {
            ColumnName = columnName;
            IsTranslated = isTranslated;
            DisplayType = VAF_Control_Ref_ID;
            AD_Ref_Val_ID = VAF_Control_Ref_Value_ID;
        }	//

        /** Column Name		*/
        public string ColumnName;
        /** Translated		*/
        public bool IsTranslated;
        /** Display Type	*/
        public int DisplayType;
        /** Value Reference	*/
        public int AD_Ref_Val_ID;

        /// <summary>
        ///String Representation
        /// </summary>
        /// <returns>class ifo text</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("LookupDisplayColumn[");
            sb.Append("ColumnName=").Append(ColumnName);
            if (IsTranslated)
                sb.Append(",IsTranslated");
            sb.Append(",DisplayType=").Append(DisplayType);
            if (AD_Ref_Val_ID != 0)
                sb.Append(",AD_Ref_val_ID=").Append(AD_Ref_Val_ID);
            sb.Append("]");
            return sb.ToString();
        }
    }

    /************************************************************************************/
}