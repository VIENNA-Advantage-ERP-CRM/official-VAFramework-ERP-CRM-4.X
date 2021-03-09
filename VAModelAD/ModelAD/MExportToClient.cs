﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Utility;
using System.Data.SqlClient;
using System.Data;
using VAdvantage.DataBase;

namespace VAdvantage.Model
{
    public class MExportToClient : X_VAF_ExportData
    {
        public MExportToClient(Ctx ctx, int VAF_ExportData_ID, Trx trxName)
            : base(ctx, VAF_ExportData_ID, trxName)
        {
            if (VAF_ExportData_ID == 0)
            {
            }
        }

        public static int Get(int Record_ID_1, int Record_ID_2, int Table_ID)
        {
            String sql = "Select VAF_ExportData_ID from VAF_ExportData where Record_ID = " + Record_ID_1 + " AND VAF_ColOne_ID = " + Record_ID_2 + " AND VAF_TableView_ID = " + Table_ID;

            int id = 0;
            int imex = 0;
            IDataReader dr = CoreLibrary.DataBase.DB.ExecuteReader(sql);
            bool blReturn = false;
            while (dr.Read())
            {
                id = Convert.ToInt32(dr[0].ToString());
            }
            dr.Close();
            if (id > 0)
            {
                sql = "delete from VAF_ExportData where Record_ID = " + Record_ID_1 + " AND VAF_ColOne_ID = " + Record_ID_2 + " AND VAF_TableView_ID = " + Table_ID;
                int res = CoreLibrary.DataBase.DB.ExecuteQuery(sql);
                if (res == 1)
                    imex = 1;
            }
            else
            {
                MExportToClient mex = new MExportToClient(Env.GetCtx(), id, null);
                mex.SetRecord_ID(Record_ID_1);
                mex.SetVAF_ColOne_ID(Record_ID_2);
                mex.SetVAF_TableView_ID(Table_ID);
                blReturn = mex.Save();

                if (blReturn)
                    imex = 0;
            }

            return imex;            
        }

        public static int Get(int Record_ID, int Table_ID)
        {
            String sql = "Select VAF_ExportData_ID from VAF_ExportData where Record_ID = " + Record_ID + " AND VAF_TableView_ID = " + Table_ID;

            int id = 0;
            int imex = 0;
            IDataReader dr = CoreLibrary.DataBase.DB.ExecuteReader(sql);
            bool blReturn = false;
            while (dr.Read())
            {
                id = Convert.ToInt32(dr[0].ToString());
            }
            dr.Close();
            if (id > 0)
            {
                sql = "delete from VAF_ExportData where Record_ID = " + Record_ID + " AND VAF_TableView_ID = " + Table_ID;
                int res = CoreLibrary.DataBase.DB.ExecuteQuery(sql);
                if (res == 1)
                    imex = 1;
            }
            else
            {
                MExportToClient mex = new MExportToClient(Env.GetCtx(), id, null);
                mex.SetRecord_ID(Record_ID);
                mex.SetVAF_TableView_ID(Table_ID);
                blReturn = mex.Save();

                if (blReturn)
                    imex = 0;
            }

            return imex;
        }
    }


}
