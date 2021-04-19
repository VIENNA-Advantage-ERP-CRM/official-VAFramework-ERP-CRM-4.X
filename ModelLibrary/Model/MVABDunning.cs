﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVABDunning
 * Purpose        : Dunning Model
 * Class Used     : X_VAB_Dunning
 * Chronological    Development
 * Raghunandan     10-Nov-2009
  ******************************************************/
using System;
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
using VAdvantage.Logging;
namespace VAdvantage.Model
{
    public class MVABDunning : X_VAB_Dunning
    {
        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="VAB_Dunning_ID"></param>
        /// <param name="trxName"></param>
        public MVABDunning(Ctx ctx, int VAB_Dunning_ID, Trx trxName)
            : base(ctx, VAB_Dunning_ID, trxName)
        {

        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="rs"></param>
        /// <param name="trxName"></param>
        public MVABDunning(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {

        }
    }
}