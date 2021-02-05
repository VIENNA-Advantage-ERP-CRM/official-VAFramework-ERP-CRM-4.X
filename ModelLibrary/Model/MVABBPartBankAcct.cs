﻿/********************************************************
 * Module Name    : Vframwork
 * Purpose        : BP Bank Account Model
 * Class Used     : X_VAB_BPart_Bank_Acct
 * Chronological Development
 * Raghunandan    24-June-2009
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Common;
using VAdvantage.Utility;
using System.Data;
//////using System.Windows.Forms;
using VAdvantage.SqlExec;
using VAdvantage.DataBase;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVABBPartBankAcct : X_VAB_BPart_Bank_Acct
    {
        /** Bank Link			*/
        private MVABBank _bank = null;
        /**	Logger	*/
        private static VLogger _log = VLogger.GetVLogger(typeof(MVABBPartBankAcct).FullName);
        /**
         * 	Get Accounst Of BPartner
         *	@param ctx context
         *	@param VAB_BusinessPartner_ID bpartner
         *	@return
         */
        public static MVABBPartBankAcct[] GetOfBPartner(Ctx ctx, int VAB_BusinessPartner_ID)
        {
            String sql = "SELECT * FROM VAB_BPart_Bank_Acct WHERE VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID
                + " AND IsActive='Y'";
            List<MVABBPartBankAcct> list = new List<MVABBPartBankAcct>();
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                 idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVABBPartBankAcct(ctx, dr, null));
                }
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                _log.Log(Level.SEVERE, sql, e);
            }
            finally { dt = null; }
            MVABBPartBankAcct[] retValue = new MVABBPartBankAcct[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
         * 	Constructor
         *	@param ctx context
         *	@param VAB_BPart_Bank_Acct_ID BP bank account
         *	@param trxName transaction
         */
        public MVABBPartBankAcct(Ctx ctx, int VAB_BPart_Bank_Acct_ID, Trx trxName)
            : base(ctx, VAB_BPart_Bank_Acct_ID, trxName)
        {

            if (VAB_BPart_Bank_Acct_ID == 0)
            {
                //	setVAB_BusinessPartner_ID (0);
                SetIsACH(false);
                SetBPBankAcctUse(BPBANKACCTUSE_Both);
            }
        }

        /**
         * 	Constructor
         *	@param ctx context
         *	@param dr result set
         *	@param trxName transaction
         */
        public MVABBPartBankAcct(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {

        }

        /**
         * 	Constructor
         *	@param ctx context
         * 	@param bp BP
         *	@param bpc BP Contact
         * 	@param location Location
         */
        public MVABBPartBankAcct(Ctx ctx, MVABBusinessPartner bp, MVAFUserContact bpc, MLocation location)
            : this(ctx, 0, bp.Get_TrxName())
        {

            SetIsACH(false);
            //
            SetVAB_BusinessPartner_ID(bp.GetVAB_BusinessPartner_ID());
            //
            SetA_Name(bpc.GetName());
            SetA_EMail(bpc.GetEMail());
            //
            SetA_Street(location.GetAddress1());
            SetA_City(location.GetCity());
            SetA_Zip(location.GetPostal());
            SetA_State(location.GetRegionName(true));
            SetA_Country(location.GetCountryName());
        }

        /**
         * 	Is Direct Deposit
         *	@return true if dd
         */
        public bool IsDirectDeposit()
        {
            if (!IsACH())
                return false;
            String s = GetBPBankAcctUse();
            if (s == null)
                return true;
            return (s.Equals(BPBANKACCTUSE_Both) || s.Equals(BPBANKACCTUSE_DirectDeposit));
        }

        /**
         * 	Is Direct Debit
         *	@return true if dd
         */
        public bool IsDirectDebit()
        {
            if (!IsACH())
                return false;
            String s = GetBPBankAcctUse();
            if (s == null)
                return true;
            return (s.Equals(BPBANKACCTUSE_Both) || s.Equals(BPBANKACCTUSE_DirectDebit));
        }


        /**
         * 	Get Bank
         *	@return bank
         */
        public MVABBank GetBank()
        {
            int VAB_Bank_ID = GetVAB_Bank_ID();
            if (VAB_Bank_ID == 0)
                return null;
            if (_bank == null)
                _bank = new MVABBank(GetCtx(), VAB_Bank_ID, Get_TrxName());
            return _bank;
        }

        /**
         * 	Get Routing No
         *	@return routing No
         */
        public new String GetRoutingNo()
        {
            MVABBank bank = GetBank();
            String rt = base.GetRoutingNo();
            if (bank != null)
                rt = bank.GetRoutingNo();
            return rt;
        }

        /**
         * 	Before Save
         *	@param newRecord new
         *	@return true
         */
        protected override bool BeforeSave(bool newRecord)
        {
            //	maintain routing on bank level
            if (IsACH() && GetBank() != null)
                SetRoutingNo(null);
            //	Verify Bank
            MVABBank bank = GetBank();
            if (bank != null)
            {
                BankVerificationInterface verify = bank.GetVerificationClass();
                if (verify != null)
                {
                    String errorMsg = verify.VerifyRoutingNo(bank.GetVAB_Country_ID(), GetRoutingNo());
                    if (errorMsg != null)
                    {
                        log.SaveError("Error", "@Invalid@ @RoutingNo@ " + errorMsg);
                        return false;
                    }
                    //
                    errorMsg = verify.VerifyAccountNo(bank, GetAccountNo());
                    if (errorMsg != null)
                    {
                        log.SaveError("Error", "@Invalid@ @AccountNo@ " + errorMsg);
                        return false;
                    }
                    errorMsg = verify.VerifyBBAN(bank, GetBBAN());
                    if (errorMsg != null)
                    {
                        log.SaveError("Error", "@Invalid@ @BBAN@ " + errorMsg);
                        return false;
                    }
                    errorMsg = verify.VerifyIBAN(bank, GetIBAN());
                    if (errorMsg != null)
                    {
                        log.SaveError("Error", "@Invalid@ @IBAN@ " + errorMsg);
                        return false;
                    }
                }
            }
            return true;
        }

        /**
         *	String Representation
         * 	@return info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MBP_BankAccount[")
                .Append(Get_ID())
                .Append(", Name=").Append(GetA_Name())
                .Append("]");
            return sb.ToString();
        }
    }
}