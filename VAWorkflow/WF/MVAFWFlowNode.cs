﻿
/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MWFNode
 * Purpose        : 
 * Class Used     : MWFNode inherits X_VAF_WFlow_Node
 * Chronological    Development
 * Raghunandan      01-May-2009 
  ******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Model;
using VAdvantage.Common;
using System.Drawing;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.SqlExec;

using VAdvantage.Logging;
using VAdvantage.Utility;

namespace VAdvantage.WF
{
    public class MVAFWFlowNode : X_VAF_WFlow_Node
    {
        #region Private variable
        private const long SERIALVERSIONUID = 1L;
        //Next Modes
        private List<MVAFWFlowNextNode> _next = new List<MVAFWFlowNextNode>();
        //Translated Name
        private string _name_trl = null;
        //Translated Description	
        private string _description_trl = null;
        //Translated Help
        private string _help_trl = null;
        //Translation Flag
        private bool _translated = false;
        //Column
        private MVAFColumn _column = null;
        //Process Parameters
        private MVAFWFlowNodePara[] _paras = null;
        //Duration Base MS	
        private long _durationBaseMS = -1;
        private static CCache<int, MVAFWFlowNode> _cache = new CCache<int, MVAFWFlowNode>("VAF_WFlow_Node", 50);
        #endregion

        /// <summary>
        ///Standard Constructor - save to cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVAFWFlowNode(Ctx ctx, int VAF_WFlow_Node_ID, Trx trxName)
            : base(ctx, VAF_WFlow_Node_ID, trxName)
        {
            if (VAF_WFlow_Node_ID == 0)
            {
                //	setVAF_WFlow_Node_ID (0);
                //	setVAF_Workflow_ID (0);
                //	setValue (null);
                //	setName (null);
                SetAction(ACTION_WaitSleep);
                SetCost(0);
                SetDuration(0);
                SetEntityType(ENTITYTYPE_UserMaintained);	// U
                SetIsCentrallyMaintained(true);	// Y
                SetJoinElement(JOINELEMENT_XOR);	// X
                SetDurationLimit(0);
                SetSplitElement(SPLITELEMENT_XOR);	// X
                SetWaitingTime(0);
                SetXPosition(0);
                SetYPosition(0);
            }
            //	Save to Cache
            if (Get_ID() != 0)
            {
                _cache.Add(GetVAF_WFlow_Node_ID(), this);
            }
        }

        /// <summary>
        /// Parent Constructor
        /// </summary>
        /// <param name="wf">workflow (parent)</param>
        /// <param name="Value">value</param>
        /// <param name="Name">name</param>
        public MVAFWFlowNode(MVAFWorkflow wf, String value, String name)
            : base(wf.GetCtx(), 0, wf.Get_Trx())
        {
            SetClientOrg(wf);
            SetVAF_Workflow_ID(wf.GetVAF_Workflow_ID());
            SetValue(value);
            SetName(name);
            _durationBaseMS = wf.GetDurationBaseSec() * 1000;
        }

        /// <summary>
        /// Load Constructor - save to cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">set to load info from</param>
        /// <param name="trxName">transaction</param>
        public MVAFWFlowNode(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
            LoadNext();
            LoadTrl();
            //	Save to Cache            
            _cache.Add((int)GetVAF_WFlow_Node_ID(), this);
        }

        /// <summary>
        /// Get WF Node from Cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <returns>MWFNode</returns>
        public static MVAFWFlowNode Get(Ctx ctx, int VAF_WFlow_Node_ID)
        {
            int key = VAF_WFlow_Node_ID;
            MVAFWFlowNode retValue = _cache[key];
            if (retValue != null)
                return retValue;
            retValue = new MVAFWFlowNode(ctx, VAF_WFlow_Node_ID, null);
            if (retValue.Get_ID() != 0)
                _cache.Add(key, retValue);
            return retValue;
        }

        /// <summary>
        ///Set Client Org
        /// </summary>
        /// <param name="VAF_Client_ID">client</param>
        /// <param name="VAF_Org_ID">org</param>
        //public  void SetClientOrg(int VAF_Client_ID, int VAF_Org_ID)
        //{
        //    base.SetClientOrg(VAF_Client_ID, VAF_Org_ID);
        //}

        /// <summary>
        ///	Load Next
        /// </summary>
        private void LoadNext()
        {
            String sql = "SELECT * FROM VAF_WFlow_NextNode WHERE VAF_WFlow_Node_ID=" + Get_ID() + " AND IsActive='Y' ORDER BY SeqNo";
            bool splitAnd = SPLITELEMENT_AND.Equals(GetSplitElement());
            DataSet ds = null;
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, Get_Trx());
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow dr = ds.Tables[0].Rows[i];
                    MVAFWFlowNextNode next = new MVAFWFlowNextNode(GetCtx(), dr, Get_Trx());
                    next.SetFromSplitAnd(splitAnd);
                    _next.Add(next);
                }
                ds = null;
            }
            catch (SqlException e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            log.Fine("#" + _next.Count);
        }

        /// <summary>
        ///Load Translation
        /// </summary>
        private void LoadTrl()
        {
            if (Utility.Env.IsBaseLanguage(GetCtx(),"VAF_Workflow") || Get_ID() == 0)
                return;
            String sql = "SELECT Name, Description, Help FROM VAF_WFlow_Node_TL WHERE VAF_WFlow_Node_ID=" + Get_ID() + " AND VAF_Language='" + Utility.Env.GetVAF_Language(GetCtx()) + "'";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                while (idr.Read())
                {
                    _name_trl = idr[0].ToString();
                    _description_trl = idr[1].ToString();
                    _help_trl = idr[2].ToString();
                    _translated = true;
                }
                idr.Close();
            }
            catch (SqlException e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }
            log.Fine("Trl=" + _translated);
        }

        /// <summary>
        ///Get Number of Next Nodes
        /// </summary>
        /// <returns>number of next nodes</returns>
        public int GetNextNodeCount()
        {
            return _next.Count;
        }

        /// <summary>
        ///Get Name
        /// </summary>
        /// <param name="translated">translated</param>
        /// <returns>Name</returns>
        public String GetName(bool translated)
        {
            if (translated && _translated)
                return _name_trl;
            return GetName();
        }

        /// <summary>
        /// Get Description
        /// </summary>
        /// <param name="translated">translated</param>
        /// <returns>Description</returns>
        public String GetDescription(bool translated)
        {
            if (translated && _translated)
                return _description_trl;
            return GetDescription();
        }

        /// <summary>
        ///Get Help
        /// </summary>
        /// <param name="translated">translated</param>
        /// <returns>Name</returns>
        public String GetHelp(bool translated)
        {
            if (translated && _translated)
                return _help_trl;
            return GetHelp();
        }
         
        /// <summary>
        ///Set Position
        /// </summary>
        /// <param name="position">position point</param>
        public void SetPosition(Point position)
        {
            SetPosition(position.X, position.Y);
        }

        /// <summary>
        /// Set Position
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public void SetPosition(int x, int y)
        {
            SetXPosition(x);
            SetYPosition(y);
        }

        /// <summary>
        ///Get Position
        /// </summary>
        /// <returns>position point</returns>
        public Point GetPosition()
        {
            return new Point(GetXPosition(), GetYPosition());
        }

        /// <summary>
        ///Get Action Info
        /// </summary>
        /// <returns>info</returns>
        public String GetActionInfo()
        {
            String action = GetAction();
            if (ACTION_AppsProcess.Equals(action))
                return "Process:VAF_Job_ID=" + GetVAF_Job_ID();
            else if (ACTION_DocumentAction.Equals(action))
                return "DocumentAction=" + GetDocAction();
            else if (ACTION_AppsReport.Equals(action))
                return "Report:VAF_Job_ID=" + GetVAF_Job_ID();
            else if (ACTION_AppsTask.Equals(action))
                return "Task:VAF_Task_ID=" + GetVAF_Task_ID();
            else if (ACTION_SetVariable.Equals(action))
                return "SetVariable:VAF_Column_ID=" + GetVAF_Column_ID();
            else if (ACTION_SubWorkflow.Equals(action))
                return "Workflow:VAF_Workflow_ID=" + GetVAF_Workflow_ID();
            else if (ACTION_UserChoice.Equals(action))
                return "UserChoice:VAF_Column_ID=" + GetVAF_Column_ID();
            else if (ACTION_UserWorkbench.Equals(action))
                return "Workbench:?";
            else if (ACTION_UserForm.Equals(action))
                return "Form:VAF_Page_ID=" + GetVAF_Page_ID();
            else if (ACTION_UserWindow.Equals(action))
                return "Window:VAF_Screen_ID=" + GetVAF_Screen_ID();
            else if (ACTION_WaitSleep.Equals(action))
                return "Sleep:WaitTime=" + GetWaitTime();
            return "??";
        }

        /// <summary>
        /// Get Attribute Name
        /// @see model.X_VAF_WFlow_Node#getAttributeName()
        /// </summary>
        /// <returns>Attribute Name</returns>
        public new String GetAttributeName()
        {
            if (GetVAF_Column_ID() == 0)
                return base.GetAttributeName();
            //	We have a column
            String attribute = base.GetAttributeName();
            if (attribute != null && attribute.Length > 0)
                return attribute;
            // change to pick Name instead of Column Name to be set on WF Activity or Event
            SetAttributeName(GetColumn().GetName());
            return base.GetAttributeName();
        }

        /// <summary>
        ///Get Column
        /// </summary>
        /// <returns>column if valid</returns>
        public MVAFColumn GetColumn()
        {
            if (GetVAF_Column_ID() == 0)
                return null;
            if (_column == null)
                _column = MVAFColumn.Get(GetCtx(), GetVAF_Column_ID());
            return _column;
        }

        /// <summary>
        ///Is this an Approval setp?
        /// </summary>
        /// <returns>true if User Approval</returns>
        public bool IsUserApproval()
        {
            if (!ACTION_UserChoice.Equals(GetAction()))
                return false;
            return (GetColumn() != null && "IsApproved".Equals(GetColumn().GetColumnName()));
        }

        /// <summary>
        ///Is this a User Choice step?
        /// </summary>
        /// <returns>true if User Choice</returns>
        public bool IsUserChoice()
        {
            return ACTION_UserChoice.Equals(GetAction());
        }

        /// <summary>
        ///Is this a Manual user step?
        /// </summary>
        /// <returns>true if Window/Form/Workbench</returns>
        public bool IsUserManual()
        {
            if (ACTION_UserForm.Equals(GetAction())
                || ACTION_UserWindow.Equals(GetAction())
                || ACTION_UserWorkbench.Equals(GetAction()))
                return true;
            return false;
        }

        /// <summary>
        ///Get Duration in ms
        /// </summary>
        /// <returns>duration in ms</returns>
        public long GetDurationMS()
        {
            long duration = base.GetDuration();
            if (duration == 0)
                return 0;
            if (_durationBaseMS == -1)
                _durationBaseMS = GetWorkflow().GetDurationBaseSec() * 1000;
            return duration * _durationBaseMS;
        }

        /// <summary>
        ///Get Duration Limit in ms
        /// </summary>
        /// <returns>duration limit in ms</returns>
        public long GetDurationLimitMS()
        {
            long limit = base.GetDurationLimit();
            if (limit == 0)
                return 0;
            if (_durationBaseMS == -1)
                _durationBaseMS = GetWorkflow().GetDurationBaseSec() * 1000;
            return limit * _durationBaseMS;
        }

        /// <summary>
        ///Get Duration CalendarField
        /// </summary>
        /// <returns>Calendar.MINUTE, etc.</returns>
        public int GetDurationCalendarField()
        {
            return GetWorkflow().GetDurationCalendarField();
        }

        /// <summary>
        ///Calculate Dynamic Priority
        /// </summary>
        /// <param name="seconds">second after created</param>
        /// <returns>dyn prio</returns>
        public int CalculateDynamicPriority(int seconds)
        {
            if (seconds == 0 || GetDynPriorityUnit() == null
                //|| GetDynPriorityChange() == null
                || GetDynPriorityChange() == 0
                || Utility.Env.ZERO.CompareTo(GetDynPriorityChange()) == 0)
                return 0;
            //
            Decimal divide = Utility.Env.ZERO;
            if (DYNPRIORITYUNIT_Minute.Equals(GetDynPriorityUnit()))
                divide = new Decimal(60);
            else if (DYNPRIORITYUNIT_Hour.Equals(GetDynPriorityUnit()))
                divide = new Decimal(3600);
            else if (DYNPRIORITYUNIT_Day.Equals(GetDynPriorityUnit()))
                divide = new Decimal(86400);
            else
                return 0;
            //
            //Decimal change = new Decimal(seconds).divide(divide, BigDecimal.ROUND_DOWN).multiply(getDynPriorityChange());
            Decimal change = Decimal.Multiply(Decimal.Round(Decimal.Divide(new Decimal(seconds), divide), MidpointRounding.AwayFromZero), GetDynPriorityChange());

            //Decimal change = seconds / Math.Round(divide) * (GetDynPriorityChange());
            return (int)change;
        }

        /// <summary>
        ///Get Workflow
        /// </summary>
        /// <returns>workflow</returns>
        public MVAFWorkflow GetWorkflow()
        {
            return MVAFWorkflow.Get(GetCtx(), GetVAF_Workflow_ID());
        }

        /// <summary>
        /// String Representation
        /// </summary>
        /// <returns>info</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MWFNode[");
            sb.Append(Get_ID())
                .Append("-").Append(GetName())
                .Append(",Action=").Append(GetActionInfo())
                .Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// User String Representation
        /// </summary>
        /// <returns>info</returns>
        public String ToStringX()
        {
            StringBuilder sb = new StringBuilder("MWFNode[");
            sb.Append(GetName())
                .Append("-").Append(GetActionInfo())
                .Append("]");
            return sb.ToString();
        }

        /// <summary>
        ///	Before Save
        /// </summary>
        /// <param name="newRecord">newRecord</param>
        /// <returns>true if can be saved</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            String action = GetAction();
            if (action.Equals(ACTION_WaitSleep))
            {
                ;
            }
            else if (action.Equals(ACTION_AppsProcess) || action.Equals(ACTION_AppsReport))
            {
                if (GetVAF_Job_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAF_Job_ID"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_AppsTask))
            {
                if (GetVAF_Task_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAF_Task_ID"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_DocumentAction))
            {
                if (GetDocAction() == null || GetDocAction().Length == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "DocAction"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_EMail))
            {
                if (GetVAR_MailTemplate_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAR_MailTemplate_ID"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_SetVariable))
            {
                if (GetAttributeValue() == null)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "AttributeValue"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_SubWorkflow))
            {
                if (GetVAF_Workflow_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAF_Workflow_ID"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_UserChoice))
            {
                if (GetVAF_Column_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAF_Column_ID"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_UserForm))
            {
                if (GetVAF_Page_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAF_Page_ID"));
                    return false;
                }
            }
            else if (action.Equals(ACTION_UserWindow))
            {
                if (GetVAF_Screen_ID() == 0)
                {
                    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAF_Screen_ID"));
                    return false;
                }
            }
            //else if (action.equals(ACTION_UserWorkbench)) 
            //{
            //&& getAD_Workbench_ID() == 0)
            //    log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "AD_Workbench_ID"));
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// After Save
        /// </summary>
        /// <param name="newRecord">new</param>
        /// <param name="success">success</param>
        /// <returns>saved</returns>
        protected override bool AfterSave(bool newRecord, bool success)
        {
            if (!success)
                return success;
            TranslationTable.Save(this, newRecord);
            return true;
        }

        /// <summary>
        /// After Delete
        /// </summary>
        /// <param name="success">success</param>
        /// <returns>deleted</returns>
        protected override bool AfterDelete(bool success)
        {
            if (TranslationTable.IsActiveLanguages(false))
                TranslationTable.Delete(this);
            return success;
        }

        /// <summary>
        /// Get the transitions
        /// </summary>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>array of next nodes</returns>
        public MVAFWFlowNextNode[] GetTransitions(int VAF_Client_ID)
        {
            List<MVAFWFlowNextNode> list = new List<MVAFWFlowNextNode>();
            for (int i = 0; i < _next.Count; i++)
            {
                MVAFWFlowNextNode next = _next[i];
                if (next.GetVAF_Client_ID() == 0 || next.GetVAF_Client_ID() == VAF_Client_ID)
                    list.Add(next);
            }
            MVAFWFlowNextNode[] retValue = new MVAFWFlowNextNode[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        ///Get Parameters
        /// </summary>
        /// <returns>array of parameters</returns>
        public MVAFWFlowNodePara[] GetParameters()
        {
            if (_paras == null)
                _paras = MVAFWFlowNodePara.GetParameters(GetCtx(), GetVAF_WFlow_Node_ID());
            return _paras;
        }
    }
}