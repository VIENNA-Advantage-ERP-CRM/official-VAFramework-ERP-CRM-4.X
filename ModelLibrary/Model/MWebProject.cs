﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MWebProject
 * Purpose        : Web Project Model
 * Class Used     : X_CM_WebProject
 * Chronological    Development
 * Deepak           05-Feb-2010
  ******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Process;
using VAdvantage.Classes;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Logging;
using VAdvantage.Utility;


namespace VAdvantage.Model
{
    public class MWebProject: X_CM_WebProject
{
	/// <summary>
	/// Get MWebProject from Cache
	/// </summary>
	/// <param name="ctx">context</param>
	/// <param name="CM_WebProject_ID">id</param>
	/// <returns>MWebproject</returns>
	public static MWebProject Get(Ctx ctx, int CM_WebProject_ID)
	{
		int key = Utility.Util.GetValueOfInt(CM_WebProject_ID);
		MWebProject retValue = (MWebProject)_cache[key];
		if (retValue != null)
			return retValue;
		retValue = new MWebProject (ctx, CM_WebProject_ID, null);
		if (retValue.Get_ID () == CM_WebProject_ID)
			_cache.Add(key, retValue);
		return retValue;
	} //	get

	/**	Cache						*/
	private static CCache<int, MWebProject> _cache 
		= new CCache<int, MWebProject> ("CM_WebProject", 5);
	
	
	/// <summary>
	///	Web Project
	/// </summary>
	/// <param name="ctx">context</param>
	/// <param name="CM_WebProject_ID">id</param>
	/// <param name="trxName">transaction</param>
	public MWebProject (Ctx ctx, int CM_WebProject_ID, Trx trxName):	base(ctx, CM_WebProject_ID, trxName)
	{
	
	}	//	MWebProject

	/// <summary>
    /// Web Project
	/// </summary>
	/// <param name="ctx">context</param>
	/// <param name="rs">datarow</param>
	/// <param name="trxName">trx</param>
	public MWebProject (Ctx ctx, DataRow dr, Trx trxName):base(ctx, dr, trxName)
	{
		
	}
    public MWebProject(Ctx ctx, IDataReader idr, Trx trxName)
        : base(ctx,idr, trxName)
    {

    }	
	
	/// <summary>
    /// Before Save
	/// </summary>
	/// <param name="newRecord">new</param>
	/// <returns>true</returns>
	protected override bool BeforeSave (bool newRecord)
	{
		//	Create Trees
		if (newRecord)
		{
			MVAFTreeInfo tree = new MVAFTreeInfo (GetCtx(), 
				GetName()+MVAFTreeInfo.TREETYPE_CMContainer, MVAFTreeInfo.TREETYPE_CMContainer, Get_TrxName());
			if (!tree.Save())
				return false;
			SetVAF_TreeInfoCMC_ID(tree.GetVAF_TreeInfo_ID());
			//
			tree = new MVAFTreeInfo (GetCtx(), 
				GetName()+MVAFTreeInfo.TREETYPE_CMContainerStage, MVAFTreeInfo.TREETYPE_CMContainerStage, Get_TrxName());
			if (!tree.Save())
				return false;
			SetVAF_TreeInfoCMS_ID(tree.GetVAF_TreeInfo_ID());
			//
			tree = new MVAFTreeInfo (GetCtx(), 
				GetName()+MVAFTreeInfo.TREETYPE_CMVACMLayout, MVAFTreeInfo.TREETYPE_CMVACMLayout, Get_TrxName());
			if (!tree.Save())
				return false;
			SetVAF_TreeInfoCMT_ID(tree.GetVAF_TreeInfo_ID());
			//
			tree = new MVAFTreeInfo (GetCtx(), 
				GetName()+MVAFTreeInfo.TREETYPE_CMMedia, MVAFTreeInfo.TREETYPE_CMMedia, Get_TrxName());
			if (!tree.Save())
				return false;
			SetVAF_TreeInfoCMM_ID(tree.GetVAF_TreeInfo_ID());
		}
		return true;
	}	//	beforeSave
	
	/// <summary>
	///	After Save.
	/// </summary>
	/// <param name="newRecord">insert</param>
	/// <param name="success">success</param>
    /// <returns>true if saved</returns>
	protected override bool AfterSave (bool newRecord, bool success)
	{
		if (!success)
			return success;
		if (!newRecord)
		{
			// Clean Web Project Cache
		}
		return success;
	}	//	afterSave
	
}	//	MWebProject

}
