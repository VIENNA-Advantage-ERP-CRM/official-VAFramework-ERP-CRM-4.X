﻿/********************************************************
 * Class Name     : MVAGLBatchJRNL
 * Purpose        :  Journal Batch Model
 * Class Used     : X_VAGL_BatchJRNL,DocAction
 * Chronological    Development
 * Deepak           15-JAN-2010
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
using System.Data.SqlClient;
using System.IO;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVAGLBatchJRNL : X_VAGL_BatchJRNL, DocAction
    {
        /// <summary>
        ///	Create new Journal Batch by copying
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAGL_BatchJRNL_ID">journal batch</param>
        /// <param name="dateDoc">date of the document date</param>
        /// <param name="trxName">transaction</param>
        /// <returns>Journal Batch</returns>

        public static MVAGLBatchJRNL CopyFrom(Ctx ctx, int VAGL_BatchJRNL_ID,
            DateTime? dateDoc, Trx trxName)
        {
            MVAGLBatchJRNL from = new MVAGLBatchJRNL(ctx, VAGL_BatchJRNL_ID, trxName);
            if (from.GetVAGL_BatchJRNL_ID() == 0)
            {
                throw new ArgumentException("From Journal Batch not found VAGL_BatchJRNL_ID=" + VAGL_BatchJRNL_ID);
            }
            //
            MVAGLBatchJRNL to = new MVAGLBatchJRNL(ctx, 0, trxName);
            PO.CopyValues(from, to, from.GetVAF_Client_ID(), from.GetVAF_Org_ID());
            to.Set_ValueNoCheck("DocumentNo", null);
            to.Set_ValueNoCheck("VAB_YearPeriod_ID", null);
            to.SetDateAcct(dateDoc);
            to.SetDateDoc(dateDoc);
            to.SetDocStatus(DOCSTATUS_Drafted);
            to.SetDocAction(DOCACTION_Complete);
            to.SetIsApproved(false);
            to.SetProcessed(false);
            //
            if (!to.Save())
            {
                throw new Exception("Could not create Journal Batch");
            }

            if (to.CopyDetailsFrom(from) == 0)
            {
                throw new Exception("Could not create Journal Batch Details");
            }

            return to;
        }	//	copyFrom


        /// <summary>
        /// Standard Construvtore
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAGL_BatchJRNL_ID">id if 0 - create actual batch</param>
        /// <param name="trxName">transaction</param>
        public MVAGLBatchJRNL(Ctx ctx, int VAGL_BatchJRNL_ID, Trx trxName)
            : base(ctx, VAGL_BatchJRNL_ID, trxName)
        {
            //super (ctx, VAGL_BatchJRNL_ID, trxName);
            if (VAGL_BatchJRNL_ID == 0)
            {
                //	setVAGL_BatchJRNL_ID (0);	PK
                //	setDescription (null);
                //	setDocumentNo (null);
                //	setVAB_DocTypes_ID (0);
                SetPostingType(POSTINGTYPE_Actual);
                SetDocAction(DOCACTION_Complete);
                SetDocStatus(DOCSTATUS_Drafted);
                SetTotalCr(Env.ZERO);
                SetTotalDr(Env.ZERO);
                SetProcessed(false);
                SetProcessing(false);
                SetIsApproved(false);
            }
        }	//	MVAGLBatchJRNL

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">datarow</param>
        /// <param name="trxName">transaction</param>
        public MVAGLBatchJRNL(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
            //super(ctx, dr, trxName);
        }	//	MVAGLBatchJRNL

        /// <summary>
        /// Copy Constructor.Dos not copy: Dates/Period
        /// </summary>
        /// <param name="original">original</param>
        public MVAGLBatchJRNL(MVAGLBatchJRNL original)
            : this(original.GetCtx(), 0, original.Get_TrxName())
        {
            //this (original.getCtx(), 0, original.Get_TrxName());
            SetClientOrg(original);
            SetVAGL_BatchJRNL_ID(original.GetVAGL_BatchJRNL_ID());
            //
            //	setVAB_AccountBook_ID(original.getVAB_AccountBook_ID());
            //	setVAGL_Budget_ID(original.getVAGL_Budget_ID());
            SetVAGL_Group_ID(original.GetVAGL_Group_ID());
            SetPostingType(original.GetPostingType());
            SetDescription(original.GetDescription());
            SetVAB_DocTypes_ID(original.GetVAB_DocTypes_ID());
            SetControlAmt(original.GetControlAmt());
            //
            SetVAB_Currency_ID(original.GetVAB_Currency_ID());
            //	setVAB_CurrencyType_ID(original.getVAB_CurrencyType_ID());
            //	setCurrencyRate(original.getCurrencyRate());

            //	SetDateDoc(original.getDateDoc());
            //	setDateAcct(original.getDateAcct());
            //	setVAB_YearPeriod_ID(original.getVAB_YearPeriod_ID());
        }	//	MVAGLJRNL



        /// <summary>
        ///	Overwrite Client/Org if required
        /// </summary>
        /// <param name="VAF_Client_ID">client</param>
        /// <param name="VAF_Org_ID">org</param>
        public new void SetClientOrg(int VAF_Client_ID, int VAF_Org_ID)
        {
            //super.setClientOrg(VAF_Client_ID, VAF_Org_ID);
            base.SetClientOrg(VAF_Client_ID, VAF_Org_ID);
        }	//	setClientOrg


        /// <summary>
        /// Get Journal Lines
        /// </summary>
        /// <param name="requery">requery</param>
        /// <returns> Array of lines</returns>
        public MVAGLJRNL[] GetJournals(Boolean requery)
        {
            List<MVAGLJRNL> list = new List<MVAGLJRNL>();
            String sql = "SELECT * FROM VAGL_JRNL WHERE VAGL_BatchJRNL_ID=@param ORDER BY DocumentNo";
            IDataReader idr = null;
            SqlParameter[] param = new SqlParameter[1];
            try
            {
                //pstmt = DataBase.prepareStatement(sql, get_TrxName());
                //pstmt.setInt(1, getVAGL_BatchJRNL_ID());
                param[0] = new SqlParameter("@param", GetVAGL_BatchJRNL_ID());
                idr = DataBase.DB.ExecuteReader(sql, param, Get_TrxName());
                while (idr.Read())
                {
                    list.Add(new MVAGLJRNL(GetCtx(), idr, Get_TrxName()));
                }
                idr.Close();
            }
            catch (Exception ex)
            {
                log.Log(Level.SEVERE, sql, ex);
            }
            //
            MVAGLJRNL[] retValue = new MVAGLJRNL[list.Count];
            retValue = list.ToArray();
            return retValue;
        }	//	getJournals

        /// <summary>
        /// Copy Journal/Lines from other Journal Batch
        /// </summary>
        /// <param name="jb">Journal Batch</param>
        /// <returns>number of journals + lines copied</returns>
        public int CopyDetailsFrom(MVAGLBatchJRNL jb)
        {
            if (IsProcessed() || jb == null)
            {
                return 0;
            }
            int count = 0;
            int lineCount = 0;
            MVAGLJRNL[] froMVAGLJRNLs = jb.GetJournals(false);
            for (int i = 0; i < froMVAGLJRNLs.Length; i++)
            {
                MVAGLJRNL toJournal = new MVAGLJRNL(GetCtx(), 0, jb.Get_TrxName());
                PO.CopyValues(froMVAGLJRNLs[i], toJournal, GetVAF_Client_ID(), GetVAF_Org_ID());
                toJournal.SetVAGL_BatchJRNL_ID(GetVAGL_BatchJRNL_ID());
                toJournal.Set_ValueNoCheck("DocumentNo", null);	//	create new

                //Manish 18/7/2016. VAB_YearPeriod_ID was Null.. But column is mandatory in database so value can't be null.
                toJournal.Set_ValueNoCheck("VAB_YearPeriod_ID", froMVAGLJRNLs[i].GetVAB_YearPeriod_ID());
                // end
                toJournal.SetDateDoc(GetDateDoc());		//	dates from this Batch
                toJournal.SetDateAcct(GetDateAcct());
                toJournal.SetDocStatus(MVAGLJRNL.DOCSTATUS_Drafted);
                toJournal.SetDocAction(MVAGLJRNL.DOCACTION_Complete);
                toJournal.SetTotalCr(Env.ZERO);
                toJournal.SetTotalDr(Env.ZERO);
                toJournal.SetIsApproved(false);
                toJournal.SetIsPrinted(false);
                toJournal.SetPosted(false);
                toJournal.SetProcessed(false);
                if (toJournal.Save())
                {
                    count++;
                    lineCount += toJournal.CopyLinesFrom(froMVAGLJRNLs[i], GetDateAcct(), 'x');
                }
            }
            if (froMVAGLJRNLs.Length != count)
            {
                log.Log(Level.SEVERE, "Line difference - Journals=" + froMVAGLJRNLs.Length + " <> Saved=" + count);
            }

            return count + lineCount;
        }	//	copyLinesFrom

        /// <summary>
        /// Get Period
        /// </summary>
        /// <returns>period or null</returns>
        public MVABYearPeriod GetPeriod()
        {
            int VAB_YearPeriod_ID = GetVAB_YearPeriod_ID();
            if (VAB_YearPeriod_ID != 0)
            {
                return MVABYearPeriod.Get(GetCtx(), VAB_YearPeriod_ID);
            }
            return null;
        }	//	getPeriod

        /// <summary>
        /// Set Doc Date - Callout.Sets also acct date and period
        /// </summary>
        /// <param name="oldDateDoc">old</param>
        /// <param name="newDateDoc">new</param>
        /// <param name="windowNo">window no</param>
        public void SetDateDoc(String oldDateDoc,
                String newDateDoc, int windowNo)
        {
            if (newDateDoc == null || newDateDoc.Length == 0)
            {
                return;
            }
            DateTime? dateDoc = Utility.Util.GetValueOfDateTime(PO.ConvertToTimestamp(newDateDoc));
            if (dateDoc == null)
            {
                return;
            }
            SetDateDoc(dateDoc);
            SetDateAcct(dateDoc);
        }	//	SetDateDoc

        /// <summary>
        /// Set Acct Date - Callout.	Sets Period
        /// </summary>
        /// <param name="oldDateAcct">old</param>
        /// <param name="newDateAcct">new</param>
        /// <param name="windowNo">window no</param>
        public void SetDateAcct(String oldDateAcct,
               String newDateAcct, int windowNo)
        {
            if (newDateAcct == null || newDateAcct.Length == 0)
            {
                return;
            }
            DateTime? dateAcct = Utility.Util.GetValueOfDateTime(PO.ConvertToTimestamp(newDateAcct));
            if (dateAcct == null)
            {
                return;
            }
            SetDateAcct(dateAcct);
        }	//	setDateAcct

        /// <summary>
        /// Set Period - Callout.	Set Acct Date if required
        /// </summary>
        /// <param name="oldVAB_YearPeriod_ID">old</param>
        /// <param name="newVAB_YearPeriod_ID">new</param>
        /// <param name="windowNo">window no</param>
        public void SetVAB_YearPeriod_ID(String oldVAB_YearPeriod_ID,
                String newVAB_YearPeriod_ID, int windowNo)
        {
            if (newVAB_YearPeriod_ID == null || newVAB_YearPeriod_ID.Length == 0)
            {
                return;
            }
            //int VAB_YearPeriod_ID = Integer.parseInt(newVAB_YearPeriod_ID);
            int VAB_YearPeriod_ID = Utility.Util.GetValueOfInt(newVAB_YearPeriod_ID);
            if (VAB_YearPeriod_ID == 0)
            {
                return;
            }
            SetVAB_YearPeriod_ID(VAB_YearPeriod_ID);
        }	//	setVAB_YearPeriod_ID

        /// <summary>
        /// Set Accounting Date.Set also Period if not set earlier
        /// </summary>
        /// <param name="DateAcct">date</param>
        public new void SetDateAcct(DateTime? DateAcct)
        {
            //super.setDateAcct(DateAcct);
            base.SetDateAcct(DateAcct);
            if (DateAcct == null)
            {
                return;
            }
            if (GetVAB_YearPeriod_ID() != 0)
            {
                return;
            }
            int VAB_YearPeriod_ID = MVABYearPeriod.GetVAB_YearPeriod_ID(GetCtx(), DateAcct);
            if (VAB_YearPeriod_ID == 0)
            {
                log.Warning("Period not found");
            }
            else
            {
                base.SetVAB_YearPeriod_ID(VAB_YearPeriod_ID);
            }
        }	//	setDateAcct

        /// <summary>
        /// Set Period
        /// </summary>
        /// <param name="VAB_YearPeriod_ID">period</param>
        public new void SetVAB_YearPeriod_ID(int VAB_YearPeriod_ID)
        {
            base.SetVAB_YearPeriod_ID(VAB_YearPeriod_ID);
            if (VAB_YearPeriod_ID == 0)
            {
                return;
            }
            DateTime? dateAcct = GetDateAcct();
            //
            MVABYearPeriod period = GetPeriod();
            if (period != null && period.IsStandardPeriod())
            {
                if (!period.IsInPeriod(dateAcct))
                {
                    base.SetDateAcct(period.GetEndDate());
                }
            }
        }	//	setVAB_YearPeriod_ID

        /// <summary>
        /// Process document
        /// </summary>
        /// <param name="processAction">document action</param>
        /// <returns> true if performed</returns>
        public Boolean ProcessIt(String processAction)
        {
            m_processMsg = null;
            DocumentEngine engine = new DocumentEngine(this, GetDocStatus());
            return engine.ProcessIt(processAction, GetDocAction());
        }	//	process

        /**	Process Message 			*/
        private String m_processMsg = null;
        /**	Just Prepared Flag			*/
        private Boolean m_justPrepared = false;

        /// <summary>
        /// Unlock Document.
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean UnlockIt()
        {
            log.Info("unlockIt - " + ToString());
            SetProcessing(false);
            return true;
        }	//	unlockIt

        /// <summary>
        ///	Invalidate Document
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean InvalidateIt()
        {
            log.Info("invalidateIt - " + ToString());
            return true;
        }	//	invalidateIt

        /// <summary>
        /// Prepare Document
        /// </summary>
        /// <returns>new status (In Progress or Invalid) </returns>
        public String PrepareIt()
        {
            log.Info(ToString());
            m_processMsg = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_BEFORE_PREPARE);
            if (m_processMsg != null)
            {
                return DocActionVariables.STATUS_INVALID;
            }
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            //	Std Period open?
            if (!MVABYearPeriod.IsOpen(GetCtx(), GetDateAcct(), dt.GetDocBaseType(), GetVAF_Org_ID()))
            {
                m_processMsg = "@PeriodClosed@";
                return DocActionVariables.STATUS_INVALID;
            }

            // is Non Business Day?
            // JID_1205: At the trx, need to check any non business day in that org. if not fund then check * org.
            if (MVABNonBusinessDay.IsNonBusinessDay(GetCtx(), GetDateAcct(), GetVAF_Org_ID()))
            {
                m_processMsg = Common.Common.NONBUSINESSDAY;
                return DocActionVariables.STATUS_INVALID;
            }

            // JID_0521 - Restrict if debit and credit amount is not equal.-Mohit-12-jun-2019.
            if (GetTotalCr() != GetTotalDr())
            {
                m_processMsg = Msg.GetMsg(GetCtx(), "DBAndCRAmtNotEqual");
                return DocActionVariables.STATUS_INVALID;
            }

            //	Add up Amounts & prepare them
            MVAGLJRNL[] journals = GetJournals(false);
            if (journals.Length == 0)
            {
                m_processMsg = "@NoLines@";
                return DocActionVariables.STATUS_INVALID;
            }

            Decimal TotalDr = Env.ZERO;
            Decimal TotalCr = Env.ZERO;
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive())
                {
                    continue;
                }
                //	Prepare if not closed
                if (DOCSTATUS_Closed.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Voided.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Reversed.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Completed.Equals(journal.GetDocStatus()))
                {
                    ;
                }
                else
                {
                    String status = journal.PrepareIt();
                    if (!DocActionVariables.STATUS_INPROGRESS.Equals(status))
                    {
                        journal.SetDocStatus(status);
                        journal.Save();
                        m_processMsg = journal.GetProcessMsg();
                        return status;
                    }
                    journal.SetDocStatus(DOCSTATUS_InProgress);
                    journal.Save();
                }
                //
                //TotalDr = TotalDr.add(journal.getTotalDr());
                TotalDr = Decimal.Add(TotalDr, journal.GetTotalDr());
                TotalCr = Decimal.Add(TotalCr, journal.GetTotalCr());
            }
            SetTotalDr(TotalDr);
            SetTotalCr(TotalCr);

            //	Control Amount
            if (Env.ZERO.CompareTo(GetControlAmt()) != 0
                && GetControlAmt().CompareTo(GetTotalDr()) != 0)
            {
                m_processMsg = "@ControlAmtError@";
                return DocActionVariables.STATUS_INVALID;
            }

            //	Add up Amounts
            m_justPrepared = true;
            return DocActionVariables.STATUS_INPROGRESS;
        }	//	prepareIt

        /// <summary>
        ///	Approve Document
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean ApproveIt()
        {
            log.Info("approveIt - " + ToString());
            SetIsApproved(true);
            return true;
        }	//	approveIt

        /// <summary>
        /// Reject Approval
        /// </summary>
        /// <returns> true if success </returns>
        public Boolean RejectIt()
        {
            log.Info("rejectIt - " + ToString());
            SetIsApproved(false);
            return true;
        }	//	rejectIt

        /// <summary>
        /// Complete Document
        /// </summary>
        /// <returns>new status (Complete, In Progress, Invalid, Waiting ..)</returns>
        public String CompleteIt()
        {
            log.Info("completeIt - " + ToString());
            //	Re-Check
            if (!m_justPrepared)
            {
                String status = PrepareIt();
                if (!DocActionVariables.STATUS_INPROGRESS.Equals(status))
                {
                    return status;
                }
            }

            // JID_1290: Set the document number from completed document sequence after completed (if needed)
            SetCompletedDocumentNo();

            //	Implicit Approval
            ApproveIt();

            //	Add up Amounts & complete them
            MVAGLJRNL[] journals = GetJournals(true);
            Decimal? TotalDr = Env.ZERO;
            Decimal? TotalCr = Env.ZERO;
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive())
                {
                    journal.SetProcessed(true);
                    journal.SetDocStatus(DOCSTATUS_Voided);
                    journal.SetDocAction(DOCACTION_None);
                    journal.Save();
                    continue;
                }
                //	Complete if not closed
                if (DOCSTATUS_Closed.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Voided.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Reversed.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Completed.Equals(journal.GetDocStatus()))
                {
                    ;
                }
                else
                {
                    String status = journal.CompleteIt();
                    if (!DocActionVariables.STATUS_COMPLETED.Equals(status))
                    {
                        journal.SetDocStatus(status);
                        journal.Save();
                        m_processMsg = journal.GetProcessMsg();
                        return status;
                    }
                    journal.SetDocStatus(DOCSTATUS_Completed);
                    journal.Save();
                }
                //
                //TotalDr = TotalDr.add(journal.getTotalDr());
                TotalDr = Decimal.Add(TotalDr.Value, journal.GetTotalDr());
                TotalCr = Decimal.Add(TotalCr.Value, journal.GetTotalCr());
            }
            SetTotalDr(TotalDr.Value);
            SetTotalCr(TotalCr.Value);
            //	User Validation
            String valid = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_AFTER_COMPLETE);
            if (valid != null)
            {
                m_processMsg = valid;
                return DocActionVariables.STATUS_INVALID;
            }
            //
            SetProcessed(true);
            SetDocAction(DOCACTION_Close);
            return DocActionVariables.STATUS_COMPLETED;
        }	//	completeIt

        /// <summary>
        /// Set the document number from Completed Document Sequence after completed
        /// </summary>
        private void SetCompletedDocumentNo()
        {
            // if Reversal document then no need to get Document no from Completed sequence
            if (Get_ColumnIndex("IsReversal") > 0 && IsReversal())
            {
                return;
            }

            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            // if Overwrite Date on Complete checkbox is true.
            if (dt.IsOverwriteDateOnComplete())
            {
                SetDateDoc(DateTime.Now.Date);
                if (GetDateAcct().Value.Date < GetDateDoc().Value.Date)
                {
                    SetDateAcct(GetDateDoc());

                    //	Std Period open?
                    if (!MVABYearPeriod.IsOpen(GetCtx(), GetDateDoc(), dt.GetDocBaseType(), GetVAF_Org_ID()))
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
        /// Void Document.
        /// </summary>
        /// <returns>false </returns>
        public Boolean VoidIt()
        {
            log.Info("voidIt - " + ToString());
            return false;
        }	//	voidIt

        /// <summary>
        /// Close Document.
        /// </summary>
        /// <returns> true if success </returns>
        public Boolean CloseIt()
        {
            log.Info("closeIt - " + ToString());
            MVAGLJRNL[] journals = GetJournals(true);
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive() && !journal.IsProcessed())
                {
                    journal.SetProcessed(true);
                    journal.SetDocStatus(DOCSTATUS_Voided);
                    journal.SetDocAction(DOCACTION_None);
                    journal.Save();
                    continue;
                }
                if (DOCSTATUS_Drafted.Equals(journal.GetDocStatus())
                    || DOCSTATUS_InProgress.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Invalid.Equals(journal.GetDocStatus()))
                {
                    m_processMsg = "Journal not Completed: " + journal.GetSummary();
                    return false;
                }

                //	Close if not closed
                if (DOCSTATUS_Closed.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Voided.Equals(journal.GetDocStatus())
                    || DOCSTATUS_Reversed.Equals(journal.GetDocStatus()))
                {
                    ;
                }
                else
                {
                    if (!journal.CloseIt())
                    {
                        m_processMsg = "Cannot close: " + journal.GetSummary();
                        return false;
                    }
                    journal.Save();
                }
            }
            return true;
        }	//	closeIt

        /// <summary>
        /// Reverse Correction.As if nothing happened - same date
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean ReverseCorrectIt()
        {
            log.Info("reverseCorrectIt - " + ToString());
            MVAGLJRNL[] journals = GetJournals(true);
            //	check prerequisites
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive())
                {
                    continue;
                }
                //	All need to be closed/Completed
                if (DOCSTATUS_Completed.Equals(journal.GetDocStatus()))
                {
                    ;
                }
                else
                {
                    m_processMsg = "All Journals need to be Compleded: " + journal.GetSummary();
                    return false;
                }
            }

            //	Reverse it
            MVAGLBatchJRNL reverse = new MVAGLBatchJRNL(this);
            reverse.SetDateDoc(GetDateDoc());
            reverse.SetVAB_YearPeriod_ID(GetVAB_YearPeriod_ID());
            reverse.SetDateAcct(GetDateAcct());
            reverse.SetVAB_Year_ID(GetVAB_Year_ID());
            //	Reverse indicator

            if (reverse.Get_ColumnIndex("ReversalDoc_ID") > 0 && reverse.Get_ColumnIndex("IsReversal") > 0)
            {
                // set Reversal property for identifying, record is reversal or not during saving or for other actions
                reverse.SetIsReversal(true);
                // Set Orignal Document Reference
                reverse.SetReversalDoc_ID(GetVAGL_BatchJRNL_ID());
            }

            // for reversal document set Temp Document No to empty
            if (reverse.Get_ColumnIndex("TempDocumentNo") > 0)
            {
                reverse.SetTempDocumentNo("");
            }

            String description = reverse.GetDescription();
            if (description == null)
            {
                description = "** " + GetDocumentNo() + " **";
            }
            else
            {
                description += " ** " + GetDocumentNo() + " **";
                reverse.SetDescription(description);
            }
            if (!reverse.Save())
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                {
                    m_processMsg = pp.GetName() + " - " + "Could not reverse " + this;
                }
                else
                {
                    m_processMsg = "Could not reverse " + this;
                }
                return false;
            }
            //

            //	Reverse Journals
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive())
                {
                    continue;
                }
                if (journal.ReverseCorrectIt(reverse.GetVAGL_BatchJRNL_ID()) == null)
                {
                    m_processMsg = "Could not reverse " + journal;
                    return false;
                }
                journal.Save();
            }
            return true;
        }	//	reverseCorrectionIt

        /// <summary>
        /// Reverse Accrual.	Flip Dr/Cr - Use Today's date
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean ReverseAccrualIt()
        {
            log.Info("ReverseCorrectIt - " + ToString());
            MVAGLJRNL[] journals = GetJournals(true);
            //	check prerequisites
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive())
                {
                    continue;
                }
                //	All need to be closed/Completed
                if (DOCSTATUS_Completed.Equals(journal.GetDocStatus()))
                {
                    ;
                }
                else
                {
                    m_processMsg = "All Journals need to be Compleded: " + journal.GetSummary();
                    return false;
                }
            }
            //	Reverse it
            MVAGLBatchJRNL reverse = new MVAGLBatchJRNL(this);
            reverse.SetVAB_YearPeriod_ID(0);
            //reverse.SetDateDoc(new Timestamp(System.currentTimeMillis()));
            reverse.SetDateDoc(DateTime.Now);
            reverse.SetDateAcct(reverse.GetDateDoc());
            //	Reverse indicator
            String description = reverse.GetDescription();
            if (description == null)
            {
                description = "** " + GetDocumentNo() + " **";
            }
            else
            {
                description += " ** " + GetDocumentNo() + " **";
            }
            reverse.SetDescription(description);
            reverse.Save();

            //	Reverse Journals
            for (int i = 0; i < journals.Length; i++)
            {
                MVAGLJRNL journal = journals[i];
                if (!journal.IsActive())
                {
                    continue;
                }
                if (journal.ReverseCorrectIt(reverse.GetVAGL_BatchJRNL_ID()) == null)
                {
                    m_processMsg = "Could not reverse " + journal;
                    return false;
                }
                journal.Save();
            }
            return true;
        }	//	ReverseCorrectIt

        /// <summary>
        /// Re-activate - same as reverse correct
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean ReActivateIt()
        {
            log.Info("reActivateIt - " + ToString());

            //	setProcessed(false);
            if (ReverseCorrectIt())
            {
                return true;
            }
            return false;
        }	//	reActivateIt


        /// <summary>
        /// Get Summary
        /// </summary>
        /// <returns>Summary of Document</returns>
        public String GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetDocumentNo());
            //	: Total Lines = 123.00 (#1)
            sb.Append(": ")
            .Append(Msg.Translate(GetCtx(), "TotalDr")).Append("=").Append(GetTotalDr())
            .Append(" ")
            .Append(Msg.Translate(GetCtx(), "TotalCR")).Append("=").Append(GetTotalCr())
            .Append(" (#").Append(GetJournals(false).Length).Append(")");
            //	 - Description
            if (GetDescription() != null && GetDescription().Length > 0)
            {
                sb.Append(" - ").Append(GetDescription());
            }
            return sb.ToString();
        }	//	GetSummary

        /// <summary>
        /// String Representation
        /// </summary>
        /// <returns> info</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MVAGLBatchJRNL[");
            sb.Append(Get_ID()).Append(",").Append(GetDescription())
                .Append(",DR=").Append(GetTotalDr())
                .Append(",CR=").Append(GetTotalCr())
                .Append("]");
            return sb.ToString();
        }	//	toString

        /// <summary>
        ///	Get Document Info
        /// </summary>
        /// <returns>document info (untranslated)</returns>
        public String GetDocumentInfo()
        {
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
            return dt.GetName() + " " + GetDocumentNo();
        }	//	getDocumentInfo

        /// <summary>
        /// Create PDF
        /// </summary>
        /// <returns>File or null</returns>
        //public FileInfo createPDF ()
        //{
        //    try
        //    {
        //        File temp = File.createTempFile(get_TableName()+get_ID()+"_", ".pdf");
        //        return createPDF (temp);
        //    }
        //    catch (Exception e)
        //    {
        //        log.severe("Could not create PDF - " + e.getMessage());
        //    }
        //    return null;
        //}	//	getPDF
        public FileInfo CreatePDF()
        {
            try
            {
                string fileName = Get_TableName() + Get_ID() + "_" + CommonFunctions.GenerateRandomNo()
                                    + ".pdf"; //.pdf
                string filePath = Path.GetTempPath() + fileName;

                FileInfo temp = new FileInfo(filePath);
                if (!temp.Exists)
                {
                    return CreatePDF(temp);
                }
            }
            catch (Exception e)
            {
                log.Severe("Could not create PDF - " + e.Message);
            }
            return null;
        }
        /// <summary>
        /// Create PDF file
        /// </summary>
        /// <param name="file">file output file</param>
        /// <returns>file if success</returns>
        public FileInfo CreatePDF(FileInfo file)
        {
            //	ReportEngine re = ReportEngine.get (getCtx(), ReportEngine.INVOICE, getVAB_Invoice_ID());
            //	if (re == null)
            return null;
            //	return re.getPDF(file);
        }	//	createPDF


        /// <summary>
        ///	Get Process Message
        /// </summary>
        /// <returns>clear text error message</returns>
        public String GetProcessMsg()
        {
            return m_processMsg;
        }	//	getProcessMsg

        /// <summary>
        /// Get Document Owner (Responsible)
        /// </summary>
        /// <returns>VAF_UserContact_ID (Created By)</returns>
        public int GetDoc_User_ID()
        {
            return GetCreatedBy();
        }	//	getDoc_User_ID

        /// <summary>
        /// Get Document Approval Amount
        /// </summary>
        /// <returns>DR amount</returns>
        public Decimal GetApprovalAmt()
        {
            return GetTotalDr();
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

        }



        #endregion

    }

}