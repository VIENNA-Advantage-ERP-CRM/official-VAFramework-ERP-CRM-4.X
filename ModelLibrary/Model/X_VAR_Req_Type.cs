﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVARRequestType : X_VAR_Req_Type
    {
        /**
         * 	Get Request Type (cached)
         *	@param ctx context
         *	@param VAR_Req_Type_ID id
         *	@return Request Type
         */
        public static MVARRequestType Get(Ctx ctx, int VAR_Req_Type_ID)
        {
            int key = VAR_Req_Type_ID;
            MVARRequestType retValue = (MVARRequestType)_cache[key];
            if (retValue == null)
            {
                retValue = new MVARRequestType(ctx, VAR_Req_Type_ID, null);
                _cache.Add(key, retValue);
            }
            return retValue;
        }

        // Static Logger					
        private static VLogger _log = VLogger.GetVLogger(typeof(MVARRequestType).FullName);
        /**	Cache							*/
        static private CCache<int, MVARRequestType> _cache = new CCache<int, MVARRequestType>("VAR_Req_Type", 10);

        /**
         * 	Get Default Request Type
         *	@param ctx context
         *	@return Request Type
         */
        public static MVARRequestType GetDefault(Ctx ctx)
        {
            MVARRequestType retValue = null;
            int VAF_Client_ID = ctx.GetVAF_Client_ID();
            String sql = "SELECT * FROM VAR_Req_Type "
                + "WHERE VAF_Client_ID IN (0,11) AND IsActive='Y'"
                + "ORDER BY IsDefault DESC, VAF_Client_ID DESC, VAR_Request_ID DESC";
            DataSet ds;
            try
            {
                ds = new DataSet();
                ds = DataBase.DB.ExecuteDataset(sql, null, null);
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    retValue = new MVARRequestType(ctx, dr, null);
                    if (!retValue.IsDefault())
                        retValue = null;
                    break;
                }
                ds = null;
            }
            catch (SqlException ex)
            {
                _log.Log(Level.SEVERE, sql, ex);
            }
            finally
            {
                ds = null;
            }
            return retValue;
        }


        /**
         * 	Standard Constructor
         *	@param ctx context
         *	@param VAR_Req_Type_ID id
         *	@param trxName transaction
         */
        public MVARRequestType(Ctx ctx, int VAR_Req_Type_ID, Trx trxName) :
            base(ctx, VAR_Req_Type_ID, trxName)
        {
            if (VAR_Req_Type_ID == 0)
            {
                //	SetVAR_Req_Type_ID (0);
                //	SetName (null);
                SetDueDateTolerance(7);
                SetIsDefault(false);
                SetIsEMailWhenDue(false);
                SetIsEMailWhenOverdue(false);
                SetIsSelfService(true);	// Y
                SetAutoDueDateDays(0);
                SetConfidentialType(CONFIDENTIALTYPE_PublicInformation);
                SetIsAutoChangeRequest(false);
                SetIsConfidentialInfo(false);
                SetIsIndexed(true);
                SetIsInvoiced(false);
            }
        }

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param rs result Set
         *	@param trxName transaction
         */
        public MVARRequestType(Ctx ctx, DataRow dr, Trx trxName) :
            base(ctx, dr, trxName)
        {

        }

        /** Next time stats to be created		*/
        private long _nextStats = 0;

        private int _openNo = 0;
        private int _totalNo = 0;
        private int _new30No = 0;
        private int _closed30No = 0;

        /**
         * 	Update Statistics
         */
        //synchronized
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void UpdateStatistics()
        {

            if (CommonFunctions.CurrentTimeMillis() < _nextStats)
                return;

            String sql = "SELECT "
                + "(SELECT COUNT(*) FROM VAR_Request r"
                + " INNER JOIN VAR_Req_Status s ON (r.VAR_Req_Status_ID=s.VAR_Req_Status_ID AND s.IsOpen='Y') "
                + "WHERE r.VAR_Req_Type_ID=x.VAR_Req_Type_ID) AS OpenNo, "
                + "(SELECT COUNT(*) FROM VAR_Request r "
                + "WHERE r.VAR_Req_Type_ID=x.VAR_Req_Type_ID) AS TotalNo, "
                + "(SELECT COUNT(*) FROM VAR_Request r "
                //jz + "WHERE r.VAR_Req_Type_ID=x.VAR_Req_Type_ID AND Created>SysDate-30) AS New30No, "
                + "WHERE r.VAR_Req_Type_ID=x.VAR_Req_Type_ID AND Created>addDays(SysDate,-30)) AS New30No, "
                + "(SELECT COUNT(*) FROM VAR_Request r"
                + " INNER JOIN VAR_Req_Status s ON (r.VAR_Req_Status_ID=s.VAR_Req_Status_ID AND s.IsClosed='Y') "
                //jz + "WHERE r.VAR_Req_Type_ID=x.VAR_Req_Type_ID AND r.Updated>SysDate-30) AS Closed30No "
                + "WHERE r.VAR_Req_Type_ID=x.VAR_Req_Type_ID AND r.Updated>addDays(SysDate,-30)) AS Closed30No "
                //
                + "FROM VAR_Req_Type x WHERE VAR_Req_Type_ID=" + GetVAR_Req_Type_ID();
            
            IDataReader idr=null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                if (idr.Read())
                {
                    _openNo =  Utility.Util.GetValueOfInt(idr[0]);
                    _totalNo = Utility.Util.GetValueOfInt(idr[1]);
                    _new30No = Utility.Util.GetValueOfInt(idr[2]);
                    _closed30No = Utility.Util.GetValueOfInt(idr[3]);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log (Level.SEVERE, sql, e);
            }
            
            _nextStats = CommonFunctions.CurrentTimeMillis() + 3600000;		//	every hour
        }

        /**
         * 	Get Total No of requests of type
         *	@return no
         */
        public int GetTotalNo()
        {
            UpdateStatistics();
            return _totalNo;
        }

        /**
         * 	Get Open No of requests of type
         *	@return no
         */
        public int GetOpenNo()
        {
            UpdateStatistics();
            return _openNo;
        }

        /**
         * 	Get Closed in last 30 days of type
         *	@return no
         */
        public int GetClosed30No()
        {
            UpdateStatistics();
            return _closed30No;
        }

        /**
         * 	Get New in the last 30 days of type
         *	@return no
         */
        public int GetNew30No()
        {
            UpdateStatistics();
            return _new30No;
        }

        /**
         * 	Get Requests of Type
         *	@param selfService self service
         *	@param VAB_BusinessPartner_ID id or 0 for public
         *	@return array of requests
         */
        public MVARRequest[] GetRequests(Boolean selfService, int VAB_BusinessPartner_ID)
        {
            String sql = "SELECT * FROM VAR_Request WHERE VAR_Req_Type_ID=" + GetVAR_Req_Type_ID();
            if (selfService)
                sql += " AND IsSelfService='Y'";
            if (VAB_BusinessPartner_ID == 0)
                sql += " AND ConfidentialType='A'";
            else
                sql += " AND (ConfidentialType='A' OR VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID + ")";
            sql += " ORDER BY DocumentNo DESC";
            //
            List<MVARRequest> list = new List<MVARRequest>();
            DataSet ds;
            try
            {
                ds = new DataSet();
                ds = DataBase.DB.ExecuteDataset(sql, null, null);
                foreach(DataRow dr in ds.Tables[0].Rows)
                {
                    list.Add(new MVARRequest(GetCtx(), dr, null));
                }
                ds = null;
            }
            catch (Exception e)
            {
                log.Log (Level.SEVERE, sql, e);
            }
            finally { ds = null; }

            MVARRequest[] retValue = new MVARRequest[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
         * 	Get public Requests of Type
         *	@return array of requests
         */
        public MVARRequest[] GetRequests()
        {
            return GetRequests(true, 0);
        }

        /**
         * 	Get Default VAR_Req_Status_ID for Type
         *	@return status or 0
         */
        public int GetDefaultVAR_Req_Status_ID()
        {
            if (GetVAR_Req_StatusCategory_ID() == 0)
            {
                MVARReqStatusCategory sc = MVARReqStatusCategory.GetDefault(GetCtx());
                if (sc == null)
                    sc = MVARReqStatusCategory.CreateDefault(GetCtx());
                if (sc != null && sc.GetVAR_Req_StatusCategory_ID() != 0)
                    SetVAR_Req_StatusCategory_ID(sc.GetVAR_Req_StatusCategory_ID());
            }
            if (GetVAR_Req_StatusCategory_ID() != 0)
            {
                MVARReqStatusCategory sc = MVARReqStatusCategory.Get(GetCtx(), GetVAR_Req_StatusCategory_ID());
                return sc.GetDefaultVAR_Req_Status_ID();
            }
            return 0;
        }

        /**
         * 	Before Save
         *	@param newRecord new
         *	@return true
         */
        protected override Boolean BeforeSave(Boolean newRecord)
        {
            if (GetVAR_Req_StatusCategory_ID() == 0)
            {
                MVARReqStatusCategory sc = MVARReqStatusCategory.GetDefault(GetCtx());
                if (sc != null && sc.GetVAR_Req_StatusCategory_ID() != 0)
                    SetVAR_Req_StatusCategory_ID(sc.GetVAR_Req_StatusCategory_ID());
            }
            return true;
        }

        /**
         * 	String Representation
         *	@return info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MVARRequestType[");
            sb.Append(Get_ID()).Append("-").Append(GetName())
                .Append("]");
            return sb.ToString();
        }

        /**
         * 	Get Sql to return single value for the Performance Indicator
         *	@param restrictions array of goal restrictions
         *	@param MeasureScope scope of this value  
         *	@param MeasureDataType data type
         *	@param reportDate optional report date
         *	@param role role
         *	@return sql for performance indicator
         */
        public String GetSqlPI(MVAPATargetRestriction[] restrictions, String measureScope, 
            String measureDataType, DateTime? reportDate, MVAFRole role)
        {
            String dateColumn = "Created";
            String orgColumn = "VAF_Org_ID";
            String bpColumn = "VAB_BusinessPartner_ID";
            String pColumn = "VAM_Product_ID";
            //	PlannedAmt -> PlannedQty -> Count
            StringBuilder sb = new StringBuilder("SELECT COUNT(*) "
                + "FROM VAR_Request WHERE VAR_Req_Type_ID=" + GetVAR_Req_Type_ID()
                + " AND Processed<>'Y'");
            //	Date Restriction

            if (MVAPAEvaluate.MEASUREDATATYPE_QtyAmountInTime.Equals(measureDataType)
                && !MVAPATarget.MEASUREDISPLAY_Total.Equals(measureScope))
            {
                if (reportDate == null)
                    reportDate = DateTime.Now;
                String dateString = DataBase.DB.TO_DATE((DateTime?)reportDate);
                String trunc = "D";
                if (MVAPATarget.MEASUREDISPLAY_Year.Equals(measureScope))
                    trunc = "Y";
                else if (MVAPATarget.MEASUREDISPLAY_Quarter.Equals(measureScope))
                    trunc = "Q";
                else if (MVAPATarget.MEASUREDISPLAY_Month.Equals(measureScope))
                    trunc = "MM";
                else if (MVAPATarget.MEASUREDISPLAY_Week.Equals(measureScope))
                    trunc = "W";
                //	else if (MVAPATarget.MEASUREDISPLAY_Day.equals(measureDisplay))
                //		;
                sb.Append(" AND TRUNC(")
                    .Append(dateColumn).Append(",'").Append(trunc).Append("')=TRUNC(")
                    .Append(DataBase.DB.TO_DATE((DateTime?)reportDate)).Append(",'").Append(trunc).Append("')");
            }	//	date
            //
            String sql = MVAPAEvaluateCalc.AddRestrictions(sb.ToString(), false, restrictions, role,
                "VAR_Request", orgColumn, bpColumn, pColumn,GetCtx());

            log.Fine(sql);
            return sql;
        }

        /**
         * 	Get Sql to value for the bar chart
         *	@param restrictions array of goal restrictions
         *	@param MeasureDisplay scope of this value  
         *	@param MeasureDataType data type
         *	@param startDate optional report start date
         *	@param role role
         *	@return sql for Bar Chart
         */
        public String GetSqlBarChart(MVAPATargetRestriction[] restrictions, String measureDisplay, 
            String measureDataType, DateTime? startDate, MVAFRole role)
        {
            String dateColumn = "Created";
            String orgColumn = "VAF_Org_ID";
            String bpColumn = "VAB_BusinessPartner_ID";
            String pColumn = "VAM_Product_ID";
            //
            StringBuilder sb = new StringBuilder("SELECT COUNT(*), ");
            String groupBy = null;
            String orderBy = null;
            //
            if (MVAPAEvaluate.MEASUREDATATYPE_QtyAmountInTime.Equals(measureDataType)
                && !MVAPATarget.MEASUREDISPLAY_Total.Equals(measureDisplay))
            {
                String trunc = "D";
                if (MVAPATarget.MEASUREDISPLAY_Year.Equals(measureDisplay))
                    trunc = "Y";
                else if (MVAPATarget.MEASUREDISPLAY_Quarter.Equals(measureDisplay))
                    trunc = "Q";
                else if (MVAPATarget.MEASUREDISPLAY_Month.Equals(measureDisplay))
                    trunc = "MM";
                else if (MVAPATarget.MEASUREDISPLAY_Week.Equals(measureDisplay))
                    trunc = "W";
                //	else if (MVAPATarget.MEASUREDISPLAY_Day.equals(MeasureDisplay))
                //		;
                orderBy = "TRUNC(" + dateColumn + ",'" + trunc + "')";
                //jz 0 is column position in EDB, Oracle doesn't take alias in group by
                //			groupBy = orderBy + ", 0 ";
                //			sb.append(groupBy)
                groupBy = orderBy + ", CAST(0 AS INTEGER) ";
                sb.Append(groupBy)
                    .Append("FROM VAR_Request ");
            }
            else
            {
                orderBy = "s.SeqNo";
                groupBy = "COALESCE(s.Name,TO_NCHAR('-')), s.VAR_Req_Status_ID, s.SeqNo ";
                sb.Append(groupBy)
                    .Append("FROM VAR_Request LEFT OUTER JOIN VAR_Req_Status s ON (VAR_Request.VAR_Req_Status_ID=s.VAR_Req_Status_ID) ");
            }
            //	Where
            sb.Append("WHERE VAR_Request.VAR_Req_Type_ID=").Append(GetVAR_Req_Type_ID())
                .Append(" AND VAR_Request.Processed<>'Y'");
            //	Date Restriction
            if (startDate != null
                && !MVAPATarget.MEASUREDISPLAY_Total.Equals(measureDisplay))
            {
                String dateString = DataBase.DB.TO_DATE((DateTime?)startDate);
                sb.Append(" AND ").Append(dateColumn)
                    .Append(">=").Append(dateString);
            }	//	date
            //
            String sql = MVAPAEvaluateCalc.AddRestrictions(sb.ToString(), false, restrictions, role,
                "VAR_Request", orgColumn, bpColumn, pColumn,GetCtx());
            if (groupBy != null)
                sql += " GROUP BY " + groupBy + " ORDER BY " + orderBy;
            //
            log.Fine(sql);
            return sql;
        }

        /**
         * 	Get Zoom Query
         * 	@param restrictions array of restrictions
         * 	@param MeasureDisplay display
         * 	@param date date
         * 	@param VAR_Req_Status_ID status
         * 	@param role role
         *	@return query
         */
        public Query GetQuery(MVAPATargetRestriction[] restrictions, String measureDisplay, 
            DateTime? date, int VAR_Req_Status_ID, MVAFRole role)
        {
            String dateColumn = "Created";
            String orgColumn = "VAF_Org_ID";
            String bpColumn = "VAB_BusinessPartner_ID";
            String pColumn = "VAM_Product_ID";
            //
            Query query = new Query("VAR_Request");
            query.AddRestriction("VAR_Req_Type_ID", "=", GetVAR_Req_Type_ID());
            //
            String where = null;
            if (VAR_Req_Status_ID != 0)
                where = "VAR_Req_Status_ID=" + VAR_Req_Status_ID;
            else
            {
                String trunc = "D";
                if (MVAPATarget.MEASUREDISPLAY_Year.Equals(measureDisplay))
                    trunc = "Y";
                else if (MVAPATarget.MEASUREDISPLAY_Quarter.Equals(measureDisplay))
                    trunc = "Q";
                else if (MVAPATarget.MEASUREDISPLAY_Month.Equals(measureDisplay))
                    trunc = "MM";
                else if (MVAPATarget.MEASUREDISPLAY_Week.Equals(measureDisplay))
                    trunc = "W";
                //	else if (MVAPATarget.MEASUREDISPLAY_Day.equals(MeasureDisplay))
                //		trunc = "D";
                where = "TRUNC(" + dateColumn + ",'" + trunc
                    + "')=TRUNC(" + DataBase.DB.TO_DATE(date) + ",'" + trunc + "')";
            }
            String whereRestriction = MVAPAEvaluateCalc.AddRestrictions(where + " AND Processed<>'Y' ",
                true, restrictions, role,
                "VAR_Request", orgColumn, bpColumn, pColumn,GetCtx());
            query.AddRestriction(whereRestriction);
            query.SetRecordCount(1);
            return query;
        }
    }
}