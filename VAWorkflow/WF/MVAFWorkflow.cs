﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MWorkflow
 * Purpose        : 
 * Class Used     : MWorkflow inherits X_VAF_Workflow
 * Chronological    Development
 * Raghunandan      04-May-2009 
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
using System.Data;
using System.Data.SqlClient;
using VAdvantage.SqlExec;

using System.Threading;
using VAdvantage.Utility;
using VAdvantage.Logging;
using VAdvantage.ProcessEngine;
namespace VAdvantage.WF
{
    /// <summary>
    /// Workflow Modal
    /// </summary>
    public class MVAFWorkflow : X_VAF_Workflow
    {

        #region Private Variables
        //WF Nodes
        private List<MVAFWFlowNode> _nodes = new List<MVAFWFlowNode>();
        private string _name_trl = null;
        //Translated Description
        private string _description_trl = null;
        //	Translated Help		
        private string _help_trl = null;
        //	Translation Flag	
        private bool _translated = false;
        //	Single Cache		
        private static CCache<int, MVAFWorkflow> _cache = new CCache<int, MVAFWorkflow>("VAF_Workflow", 20);
        /**	Document Value Cache			*/
        private static CCache<string, MVAFWorkflow[]> _cacheDocValue = new CCache<string, MVAFWorkflow[]>("VAF_Workflow", 5);
        /**	Static Logger	*/
        private static VLogger _log = VLogger.GetVLogger(typeof(MVAFWorkflow).FullName);

        #endregion

        /// <summary>
        /// Create/Load Workflow
        /// </summary>
        /// <param name="ctx">Ctx</param>
        /// <param name="VAF_Workflow_ID">ID</param>
        /// <param name="trxName">transaction</param>
        public MVAFWorkflow(Ctx ctx, int VAF_Workflow_ID, Trx trxName)
            : base(ctx, VAF_Workflow_ID, trxName)
        {
            if (VAF_Workflow_ID == 0)
            {
                //	setVAF_Workflow_ID (0);
                //	setValue (null);
                //	setName (null);
                SetAccessLevel(ACCESSLEVEL_Organization);
                SetAuthor("Vienna, Inc.");
                SetDurationUnit(DURATIONUNIT_Day);
                SetDuration(1);
                SetRecordType(RecordType_UserMaintained);	// U
                SetIsDefault(false);
                SetPublishStatus(PUBLISHSTATUS_UnderRevision);	// U
                SetVersion(0);
                SetCost(Utility.Env.ZERO);
                SetWaitingTime(0);
                SetWorkingTime(0);
            }
            LoadTrl();
            LoadNodes();
        }
        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">result set</param>
        /// <param name="trxName">transaction</param>
        public MVAFWorkflow(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
            LoadTrl();
            LoadNodes();
        }


        /// <summary>
        /// Load Translation
        /// </summary>
        private void LoadTrl()
        {
            if (Utility.Env.IsBaseLanguage(GetCtx(), "") || Get_ID() == 0)
                return;
            String sql = "SELECT Name, Description, Help FROM VAF_Workflow_TL WHERE VAF_Workflow_ID=" + Get_ID() + " AND VAF_Language='" + Utility.Env.GetVAF_Language(GetCtx()) + "'";// lang.GetVAF_Language();
            DataSet ds = null;
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, null);
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow rs = ds.Tables[0].Rows[i];
                    _name_trl = rs[0].ToString();
                    _description_trl = rs[1].ToString();
                    _help_trl = rs[2].ToString();
                    _translated = true;
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            log.Fine("Translated=" + _translated);
        }

        /// <summary>
        ///Load All Nodes
        /// </summary>
        private void LoadNodes()
        {
            String sql = "SELECT * FROM VAF_WFlow_Node WHERE VAF_Workflow_ID=" + Get_ID() + " AND IsActive='Y'"; //jz VAF_Workflow_ID: changed in AD?
            DataSet ds = null;
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, Get_Trx());

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow rs = ds.Tables[0].Rows[i];
                    _nodes.Add(new MVAFWFlowNode(GetCtx(), rs, Get_Trx()));
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            log.Fine("#" + _nodes.Count);
        }

        /// <summary>
        /// Get the active nodes
        /// </summary>
        /// <param name="ordered">ordered ordered array</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>array of nodes</returns>
        public MVAFWFlowNode[] GetNodes(bool ordered, int VAF_Client_ID)
        {
            if (ordered)
                return GetNodesInOrder(VAF_Client_ID);
            List<MVAFWFlowNode> list = new List<MVAFWFlowNode>();
            for (int i = 0; i < _nodes.Count; i++)
            {
                MVAFWFlowNode node = _nodes[i];
                if (!node.IsActive())
                    continue;
                if (node.GetVAF_Client_ID() == 0 || node.GetVAF_Client_ID() == VAF_Client_ID)
                    list.Add(node);
            }
            MVAFWFlowNode[] retValue = new MVAFWFlowNode[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Get Number of Nodes
        /// </summary>
        /// <returns>number of nodes</returns>
        public int GetNodeCount()
        {
            return _nodes.Count;
        }

        /// <summary>
        /// Get the first node
        /// </summary>
        /// <returns>array of next nodes</returns>
        public MVAFWFlowNode GetFirstNode()
        {
            return GetNode(GetVAF_WFlow_Node_ID());
        }

        /// <summary>
        /// Get Duration Base in Seconds
        /// </summary>
        /// <returns>duration unit in seconds</returns>
        public long GetDurationBaseSec()
        {
            if (GetDurationUnit() == null)
                return 0;
            else if (DURATIONUNIT_Second.Equals(GetDurationUnit()))
                return 1;
            else if (DURATIONUNIT_Minute.Equals(GetDurationUnit()))
                return 60;
            else if (DURATIONUNIT_Hour.Equals(GetDurationUnit()))
                return 3600;
            else if (DURATIONUNIT_Day.Equals(GetDurationUnit()))
                return 86400;
            else if (DURATIONUNIT_Month.Equals(GetDurationUnit()))
                return 2592000;
            else if (DURATIONUNIT_Year.Equals(GetDurationUnit()))
                return 31536000;
            return 0;
        }

        /// <summary>
        /// Get Workflow from Cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAF_Workflow_ID">id</param>
        /// <returns>workflow</returns>
        public static MVAFWorkflow Get(Ctx ctx, int VAF_Workflow_ID)
        {
            int key = VAF_Workflow_ID;
            MVAFWorkflow retValue = _cache[key];
            if (retValue != null)
                return retValue;
            retValue = new MVAFWorkflow(ctx, VAF_Workflow_ID, null);
            if (retValue.Get_ID() != 0)
                _cache.Add(key, retValue);
            return retValue;
        }

        /// <summary>
        /// Get Duration CalendarField
        /// </summary>
        /// <returns>Calendar.MINUTE, etc.</returns>
        public int GetDurationCalendarField()
        {
            if (GetDurationUnit() == null)
            {
                return GlobalVariable.Minute;
            }
            else if (DURATIONUNIT_Second.Equals(GetDurationUnit()))
            {
                return GlobalVariable.Second;
            }
            else if (DURATIONUNIT_Minute.Equals(GetDurationUnit()))
            {
                return GlobalVariable.Minute;
            }
            else if (DURATIONUNIT_Hour.Equals(GetDurationUnit()))
            {
                return GlobalVariable.Hour;
            }
            else if (DURATIONUNIT_Day.Equals(GetDurationUnit()))
            {
                return GlobalVariable.DayOfYear;
            }
            else if (DURATIONUNIT_Month.Equals(GetDurationUnit()))
            {
                return GlobalVariable.Month;
            }
            else if (DURATIONUNIT_Year.Equals(GetDurationUnit()))
            {
                return GlobalVariable.Year;
            }
            return GlobalVariable.Month;
        }

        /// <summary>
        /// Get Doc Value Workflow
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAF_Client_ID">client</param>
        /// <param name="VAF_TableView_ID">table</param>
        /// <returns>document value workflow array or null</returns>
        public static MVAFWorkflow[] GetDocValue(Ctx ctx, int VAF_Client_ID, int VAF_TableView_ID)
        {
            String key = "C" + VAF_Client_ID + "T" + VAF_TableView_ID;
            //Reload
            if (_cacheDocValue.IsReset())
            {
                String sql = "SELECT * FROM VAF_Workflow "
                    + "WHERE WorkflowType='V' AND IsActive='Y' AND IsValid='Y' "
                    + "ORDER BY VAF_Client_ID, VAF_TableView_ID";
                List<MVAFWorkflow> list = new List<MVAFWorkflow>();
                String oldKey = "";
                String newKey = null;
                DataSet ds = null;
                try
                {
                    ds = DataBase.DB.ExecuteDataset(sql, null, null);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow rs = ds.Tables[0].Rows[i];
                        MVAFWorkflow wf = new MVAFWorkflow(ctx, rs, null);
                        newKey = "C" + wf.GetVAF_Client_ID() + "T" + wf.GetVAF_TableView_ID();
                        if (!newKey.Equals(oldKey) && list.Count > 0)
                        {
                            MVAFWorkflow[] wfs = new MVAFWorkflow[list.Count];
                            wfs = list.ToArray();
                            _cacheDocValue.Add(oldKey, wfs);
                            list = new List<MVAFWorkflow>();
                        }
                        oldKey = newKey;
                        list.Add(wf);
                    }
                    ds = null;

                }
                catch (Exception e)
                {
                    _log.Log(Level.SEVERE, sql, e);
                }
                // 	Last one
                if (list.Count > 0)
                {
                    MVAFWorkflow[] wfs = new MVAFWorkflow[list.Count];
                    wfs = list.ToArray();
                    _cacheDocValue.Add(oldKey, wfs);
                }
                _log.Config("#" + _cacheDocValue.Count);
            }
            //	Look for Entry
            MVAFWorkflow[] retValue = (MVAFWorkflow[])_cacheDocValue[key];
            return retValue;
        }

        /// <summary>
        /// Get Node with ID in Workflow
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">ID</param>
        /// <returns>node or null</returns>
        protected MVAFWFlowNode GetNode(int VAF_WFlow_Node_ID)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                MVAFWFlowNode node = (MVAFWFlowNode)_nodes[i];
                if (node.GetVAF_WFlow_Node_ID() == VAF_WFlow_Node_ID)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// Get the next nodes
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">ID</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>array of next nodes or null</returns>
        public MVAFWFlowNode[] GetNextNodes(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode node = GetNode(VAF_WFlow_Node_ID);
            if (node == null || node.GetNextNodeCount() == 0)
                return null;
            MVAFWFlowNextNode[] nexts = node.GetTransitions(VAF_Client_ID);
            List<MVAFWFlowNode> list = new List<MVAFWFlowNode>();
            for (int i = 0; i < nexts.Length; i++)
            {
                MVAFWFlowNode next = GetNode(nexts[i].GetVAF_WF_Next_ID());
                if (next != null)
                    list.Add(next);
            }
            //	Return Nodes
            MVAFWFlowNode[] retValue = new MVAFWFlowNode[list.Count];
            //list.toArray(retValue);
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Get The Nodes in Sequence Order
        /// </summary>
        /// <param name="VAF_Client_ID">client</param>
        /// <returns>Nodes in sequence</returns>
        private MVAFWFlowNode[] GetNodesInOrder(int VAF_Client_ID)
        {
            List<MVAFWFlowNode> list = new List<MVAFWFlowNode>();
            AddNodesSF(list, GetVAF_WFlow_Node_ID(), VAF_Client_ID);	//	start with first
            //	Remaining Nodes
            if (_nodes.Count != list.Count)
            {
                //	Add Stand alone
                for (int n = 0; n < _nodes.Count; n++)
                {
                    MVAFWFlowNode node = (MVAFWFlowNode)_nodes[n];
                    if (!node.IsActive())
                        continue;
                    if (node.GetVAF_Client_ID() == 0 || node.GetVAF_Client_ID() == VAF_Client_ID)
                    {
                        bool found = false;
                        for (int i = 0; i < list.Count; i++)
                        {
                            MVAFWFlowNode existing = (MVAFWFlowNode)list[i];
                            if (existing.GetVAF_WFlow_Node_ID() == node.GetVAF_WFlow_Node_ID())
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            log.Log(Level.WARNING, "Added Node w/o transition: " + node);
                            list.Add(node);
                        }
                    }
                }
            }
            //
            MVAFWFlowNode[] nodeArray = new MVAFWFlowNode[list.Count];
            nodeArray = list.ToArray();
            return nodeArray;
        }

        /// <summary>
        ///	Add Nodes recursively (depth first) to Ordered List
        /// </summary>
        /// <param name="list">list to add to</param>
        /// <param name="VAF_WFlow_Node_ID">start node id</param>
        /// <param name="VAF_Client_ID">for client</param>
        private void AddNodesDF(List<MVAFWFlowNode> list, int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode node = GetNode(VAF_WFlow_Node_ID);
            if (node != null && !list.Contains(node))
            {
                list.Add(node);
                //	Get Dependent
                MVAFWFlowNextNode[] nexts = node.GetTransitions(VAF_Client_ID);
                for (int i = 0; i < nexts.Length; i++)
                {
                    if (nexts[i].IsActive())
                        AddNodesDF(list, nexts[i].GetVAF_WF_Next_ID(), VAF_Client_ID);
                }
            }
        }

        /// <summary>
        /// Add Nodes recursively (sibling first) to Ordered List
        /// </summary>
        /// <param name="list">list to add to</param>
        /// <param name="VAF_WFlow_Node_ID">start node id</param>
        /// <param name="VAF_Client_ID">for client</param>
        private void AddNodesSF(List<MVAFWFlowNode> list, int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode node = GetNode(VAF_WFlow_Node_ID);
            if (node != null
                && (node.GetVAF_Client_ID() == 0 || node.GetVAF_Client_ID() == VAF_Client_ID))
            {
                if (!list.Contains(node))
                    list.Add(node);
                MVAFWFlowNextNode[] nexts = node.GetTransitions(VAF_Client_ID);
                for (int i = 0; i < nexts.Length; i++)
                {
                    MVAFWFlowNode child = GetNode(nexts[i].GetVAF_WF_Next_ID());
                    if (!child.IsActive())
                        continue;
                    if (child.GetVAF_Client_ID() == 0
                        || child.GetVAF_Client_ID() == VAF_Client_ID)
                    {
                        if (!list.Contains(child))
                            list.Add(child);
                    }
                }
                //	Remainder Nodes not connected
                for (int i = 0; i < nexts.Length; i++)
                {
                    if (nexts[i].IsActive())
                        AddNodesSF(list, nexts[i].GetVAF_WF_Next_ID(), VAF_Client_ID);
                }
            }
        }

        /// <summary>
        /// Get first transition (Next Node) of ID
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>next VAF_WFlow_Node_ID or 0</returns>
        public int GetNext(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode[] nodes = GetNodesInOrder(VAF_Client_ID);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].GetVAF_WFlow_Node_ID() == VAF_WFlow_Node_ID)
                {
                    MVAFWFlowNextNode[] nexts = nodes[i].GetTransitions(VAF_Client_ID);
                    if (nexts.Length > 0)
                        return nexts[0].GetVAF_WF_Next_ID();
                    return 0;
                }
            }
            return 0;
        }

        /// <summary>
        ///Get Transitions (NodeNext) of ID
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>array of next nodes</returns>
        public MVAFWFlowNextNode[] GetNodeNexts(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode[] nodes = GetNodesInOrder(VAF_Client_ID);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].GetVAF_WFlow_Node_ID() == VAF_WFlow_Node_ID)
                {
                    return nodes[i].GetTransitions(VAF_Client_ID);
                }
            }
            return null;
        }

        /// <summary>
        /// Get (first) Previous Node of ID
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>next VAF_WFlow_Node_ID or 0</returns>
        public int GetPrevious(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode[] nodes = GetNodesInOrder(VAF_Client_ID);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].GetVAF_WFlow_Node_ID() == VAF_WFlow_Node_ID)
                {
                    if (i > 0)
                        return nodes[i - 1].GetVAF_WFlow_Node_ID();
                    return 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get very Last Node
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">ignored</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>next VAF_WFlow_Node_ID or 0</returns>
        public int GetLast(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode[] nodes = GetNodesInOrder(VAF_Client_ID);
            if (nodes.Length > 0)
                return nodes[nodes.Length - 1].GetVAF_WFlow_Node_ID();
            return 0;
        }

        /// <summary>
        /// Is this the first Node
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>true if first node</returns>
        public bool IsFirst(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            return VAF_WFlow_Node_ID == GetVAF_WFlow_Node_ID();
        }

        /// <summary>
        /// Is this the last Node
        /// </summary>
        /// <param name="VAF_WFlow_Node_ID">id</param>
        /// <param name="VAF_Client_ID">for client</param>
        /// <returns>true if last node</returns>
        public bool IsLast(int VAF_WFlow_Node_ID, int VAF_Client_ID)
        {
            MVAFWFlowNode[] nodes = GetNodesInOrder(VAF_Client_ID);
            return VAF_WFlow_Node_ID == nodes[nodes.Length - 1].GetVAF_WFlow_Node_ID();
        }	//	isLast

        /// <summary>
        /// Get Name
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
        /// String Representation
        /// </summary>
        /// <returns>info</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MWorkflow[");
            sb.Append(Get_ID()).Append("-").Append(GetName())
                .Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">newRecord new</param>
        /// <returns>true</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            Validate();
            return true;
        }

        /// <summary>
        /// After Save.
        /// </summary>
        /// <param name="newRecord">new record</param>
        /// <param name="success">success</param>
        /// <returns>true if save complete (if not overwritten true)</returns>
        protected override bool AfterSave(bool newRecord, bool success)
        {
            log.Fine("Success=" + success);
            if (success && newRecord)
            {
                //	save all nodes -- Creating new Workflow
                MVAFWFlowNode[] nodes = GetNodesInOrder(0);
                for (int i = 0; i < nodes.Length; i++)
                    nodes[i].Save(Get_Trx());
            }

            if (newRecord)
            {
                int VAF_Role_ID = GetCtx().GetVAF_Role_ID();
                MWorkflowAccess wa = new MWorkflowAccess(this, VAF_Role_ID);
                wa.Save();
            }
            //	Menu/Workflow
            else if (Is_ValueChanged("IsActive") || Is_ValueChanged("Name")
                || Is_ValueChanged("Description") || Is_ValueChanged("Help"))
            {
                MVAFMenuConfig[] menues = MVAFMenuConfig.Get(GetCtx(), "VAF_Workflow_ID=" + GetVAF_Workflow_ID());
                for (int i = 0; i < menues.Length; i++)
                {
                    menues[i].SetIsActive(IsActive());
                    menues[i].SetName(GetName());
                    menues[i].SetDescription(GetDescription());
                    menues[i].Save();
                }
                X_VAF_WFlow_Node[] nodes = MVAFScreen.GetWFNodes(GetCtx(), "VAF_Workflow_ID=" + GetVAF_Workflow_ID());
                for (int i = 0; i < nodes.Length; i++)
                {
                    bool changed = false;
                    if (nodes[i].IsActive() != IsActive())
                    {
                        nodes[i].SetIsActive(IsActive());
                        changed = true;
                    }
                    if (nodes[i].IsCentrallyMaintained())
                    {
                        nodes[i].SetName(GetName());
                        nodes[i].SetDescription(GetDescription());
                        nodes[i].SetHelp(GetHelp());
                        changed = true;
                    }
                    if (changed)
                        nodes[i].Save();
                }
            }

            return success;
        }

        /// <summary>
        /// Start Workflow.
        /// </summary>
        /// <param name="pi">Info (Record_ID)</param>
        /// <returns>process</returns>
        public MVAFWFlowHandler Start(ProcessInfo pi)
        {
            MVAFWFlowHandler retValue = null;
            try
            {
                retValue = new MVAFWFlowHandler(this, pi);
                retValue.Save();
                retValue.StartWork();
                pi.SetSummary(Msg.GetMsg(GetCtx(), "Processing", true));
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, e.Message, e);
                pi.SetSummary(e.Message, true);
                retValue = null;
            }
            return retValue;
        }

        //Thread worker = null;
        /// <summary>
        /// Start Workflow and Wait for completion.
        /// </summary>
        /// <param name="pi">process info with Record_ID record for the workflow</param>
        /// <returns>process</returns>
        public MVAFWFlowHandler StartWait(ProcessInfo pi)
        {
            const int SLEEP = 500;		//	1/2 sec
            const int MAXLOOPS = 160;// 50;// 30;		//	15 sec
            //
            MVAFWFlowHandler process = Start(pi);
            if (process == null)
                return null;

            //Causes the currently executing thread object to temporarily pause 
            //and allow other threads to execute. 
            //Thread.yield();
            Thread.Sleep(0);

            StateEngine state = process.GetState();
            //worker = new Thread(new ThreadStart(process.Run));
            //worker.Start();
            int loops = 0;
            while (!state.IsClosed() && !state.IsSuspended() && !state.IsBackground())
            {
                if (loops > MAXLOOPS)
                {
                    // MessageBox.Show("Timeout after sec " + ((SLEEP * MAXLOOPS) / 1000));
                    pi.SetSummary(Msg.GetMsg(GetCtx(), "ProcessRunning", true));
                    pi.SetIsTimeout(true);
                    return process;
                }
                try
                {
                    Thread.Sleep(SLEEP);
                    loops++;
                }
                catch (Exception e)
                {
                    log.Log(Level.SEVERE, "Interrupted", e);
                    pi.SetSummary("Interrupted");
                    return process;
                }
                //Thread.yield();
                Thread.Sleep(0);
                state = process.GetState();
            }
            String summary = process.GetProcessMsg();

            // Change to get the Error Message and Display the Message
            ValueNamePair vp = VLogger.RetrieveAdvDocNoError();
            if (vp != null)
            {
                summary = vp.GetValue();
            }
            // Change to get the Error Message and Display the Message

            if (summary == null || summary.Trim().Length == 0)
            {
                // in case of Suspend (User Approval) show the workflow node on which it is suspended for approval
                if (state != null && state.GetState() == StateEngine.STATE_SUSPENDED)
                {
                    string node = Util.GetValueOfString(DB.ExecuteScalar(@"SELECT n.Name FROM VAF_WFlow_Task ac INNER JOIN VAF_WFlow_Node n ON ac.VAF_WFlow_Node_ID = n.VAF_WFlow_Node_ID WHERE
                                  ac.VAF_WFlow_Handler_ID = " + process.Get_ID() + " AND ac.WFState = '" + StateEngine.STATE_SUSPENDED + "'"));
                    if (!String.IsNullOrEmpty(node))
                    {
                        summary = state.ToString() + " " + Msg.GetMsg(GetCtx(), "For") + " " + node;
                    }
                }
                else
                {
                    summary = state.ToString();
                }
            }

            pi.SetSummary(summary, state.IsTerminated() || state.IsAborted());
            log.Fine(summary);
            return process;
        }

        /// <summary>
        /// Validate workflow.
        /// Sets Valid flag
        /// </summary>
        /// <returns>errors or ""</returns>
        public String Validate()
        {
            StringBuilder errors = new StringBuilder();
            //
            if (GetVAF_WFlow_Node_ID() == 0)
                errors.Append(" - No Start Node");
            //
            if (WORKFLOWTYPE_DocumentValue.Equals(GetWorkflowType())
                && (GetDocValueLogic() == null || GetDocValueLogic().Length == 0))
                errors.Append(" - No Document Value Logic");
            //	final
            bool valid = errors.Length == 0;
            SetIsValid(valid);
            if (!valid)
            {
                log.Info("validate: " + errors);
            }
            return errors.ToString();
        }

        public bool StartWF(ProcessInfo _pi)
        {
            MVAFWFlowHandler wfProcess = null;
            if (_pi.IsBatch())
                wfProcess = Start(_pi);      //	may return null
            else
                wfProcess = StartWait(_pi);  //	may return null
            return wfProcess != null;
        }


    }
}