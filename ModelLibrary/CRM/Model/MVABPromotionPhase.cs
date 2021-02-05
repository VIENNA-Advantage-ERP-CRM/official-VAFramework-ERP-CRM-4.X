﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Data;
using VAdvantage.Classes;
using VAdvantage.Utility;
using VAdvantage.DataBase;
using VAdvantage.Logging;
using VAdvantage.Model;

namespace VAdvantage.Model
{
    public class MVABPromotionPhase : X_VAB_PromotionPhase
    {

        
        public MVABPromotionPhase(Ctx ctx, int VAB_PromotionPhase_ID, Trx trxName)
            : base(ctx, VAB_PromotionPhase_ID, trxName)
        {
            
        }

        public MVABPromotionPhase(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

    }
}