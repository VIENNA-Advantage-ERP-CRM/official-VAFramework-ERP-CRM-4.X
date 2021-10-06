﻿/*
 * Globalize Culture sq-AL
 *
 * http://github.com/jquery/globalize
 *
 * Copyright Software Freedom Conservancy, Inc.
 * Dual licensed under the MIT or GPL Version 2 licenses.
 * http://jquery.org/license
 *
 * This file was generated by the Globalize Culture Generator
 * Translation: bugs found in this file need to be fixed in the generator
 */

(function (window, undefined) {

    var Globalize;

    if (typeof require !== "undefined" &&
        typeof exports !== "undefined" &&
        typeof module !== "undefined") {
        // Assume CommonJS
        Globalize = require("globalize");
    } else {
        // Global variable
        Globalize = window.Globalize;
    }

Globalize.addCultureInfo( "sq-AL", "default", {
	name: "sq-AL",
	englishName: "Albanian (Albania)",
	nativeName: "shqipe (Shqipëria)",
	language: "sq",
	numberFormat: {
		",": ".",
		".": ",",
		negativeInfinity: "-infinit",
		positiveInfinity: "infinit",
		percent: {
			",": ".",
			".": ","
		},
		currency: {
			pattern: ["-n$","n$"],
			",": ".",
			".": ",",
			symbol: "Lek"
		}
	},
	calendars: {
		standard: {
			"/": "-",
			firstDay: 1,
			days: {
				names: ["e diel","e hënë","e martë","e mërkurë","e enjte","e premte","e shtunë"],
				namesAbbr: ["Die","Hën","Mar","Mër","Enj","Pre","Sht"],
				namesShort: ["Di","Hë","Ma","Më","En","Pr","Sh"]
			},
			months: {
				names: ["janar","shkurt","mars","prill","maj","qershor","korrik","gusht","shtator","tetor","nëntor","dhjetor",""],
				namesAbbr: ["Jan","Shk","Mar","Pri","Maj","Qer","Kor","Gsh","Sht","Tet","Nën","Dhj",""]
			},
			AM: ["PD","pd","PD"],
			PM: ["MD","md","MD"],
			patterns: {
				d: "yyyy-MM-dd",
				D: "yyyy-MM-dd",
				t: "h:mm.tt",
				T: "h:mm:ss.tt",
				f: "yyyy-MM-dd h:mm.tt",
				F: "yyyy-MM-dd h:mm:ss.tt",
				Y: "yyyy-MM"
			}
		}
    },
    messages: {
        "Connection": "Lidhje",
        "Defaults": "defaults",
        "Login": "hyrje",
        "File": "skedar",
        "Exit": "dalje",
        "Help": "Ndihmë",
        "About": "për",
        "Host": "Mikpritës",
        "Database": "Baza e të dhënave",
        "User": "përdorues",
        "EnterUser": "Vendosni ID e Përdoruesit të Aplikimit",
        "Password": "fjalëkalim",
        "EnterPassword": "Vendosni fjalëkalimin e aplikacionit",
        "Language": "Gjuhe",
        "SelectLanguage": "Zgjidhni gjuhën tuaj",
        "Role": "rol",
        "Client": "klient",
        "Organization": "organizatë",
        "Date": "data",
        "Warehouse": "depo",
        "Printer": "printer",
        "Connected": "i lidhur",
        "NotConnected": "Nuk është e lidhur",
        "DatabaseNotFound": "Baza e të dhënave nuk u gjet",
        "UserPwdError": "Përdoruesi nuk përputhet me fjalëkalimin",
        "RoleNotFound": "Roli nuk u gjet / e plotë",
        "Authorized": "i autorizuar",
        "Ok": "Ne rregull",
        "Cancel": "anuloj",
        "VersionConflict": "Konflikti i versionit:",
        "VersionInfo": "Server <> Klient",
        "PleaseUpgrade": "Ju lutemi shkarkoni versionin e ri nga Serveri",
        "GoodMorning": "Miremengjes",
        "GoodAfternoon": "Mirembrema",
        "GoodEvening": "Mirembrema",

        //New Resource

        "Back": "prapa",
        "SelectRole": "Zgjidhni Rolin",
        "SelectOrg": "Zgjidhni Organizatën",
        "SelectClient": "Zgjidhni Klientin",
        "SelectWarehouse": "Zgjidhni Magazinë",
        "VerifyUserLanguage": "Verifikimi i përdoruesit dhe gjuhës",
        "LoadingPreference": "Preferenca për ngarkim",
        "Completed": "i përfunduar",
        //new
        "RememberMe": "Më kujto",
        "FillMandatoryFields": "Plotësoni fushat e detyrueshme",
        "BothPwdNotMatch": "Të dy fjalëkalimet duhet të përputhen.",
        "mustMatchCriteria": "Gjatësia minimale për fjalëkalim është 5. Fjalëkalimi duhet të ketë së paku 1 karakter të çështjes së sipërme, 1 karakter të çështjes më të ulët, një karakter të veçantë (@ $!% *? &) Dhe një shifër. Fjalëkalimi duhet të fillojë me karakter.",
        "NotLoginUser": "Përdoruesi nuk mund të identifikohet në sistem",
        "MaxFailedLoginAttempts": "Llogaria e përdoruesit është e bllokuar. Përpjekjet maksimale të dështimit të hyrjes tejkalojnë kufirin e përcaktuar. Ju lutemi kontaktoni administratorin.",
        "UserNotFound": "Emri i përdoruesit është i pasaktë.",
        "RoleNotDefined": "Asnjë rol i përcaktuar për këtë përdorues",
        "oldNewSamePwd": "fjalëkalimi i vjetër dhe fjalëkalimi i ri duhet të jenë të ndryshëm.",
        "NewPassword": "Fjalëkalime të reja VA",
        "NewCPassword": "Konfirmoni fjalëkalimin e ri të VA-së",
        "EnterOTP": "Hyni në OTP",
        "WrongOTP": "Hyrë e gabuar OTP",
        "ScanQRCode": "Skanoni kodin me Google Authenticator",
        "EnterVerCode": "Vendosni OTP të gjeneruar nga aplikacioni juaj celular",
        "PwdExpired": "Fjalëkalimi i përdoruesit skadoi",
        "ActDisabled": "Llogaria është çaktivizuar",
        "ActExpired": "Llogaria ka skaduar",
        "AdminUserNotFound": "Emri i administratorit nuk është i saktë.",
        "AdminUserPwdError": "Përdoruesi i administratorit nuk përputhet me fjalëkalimin",
        "AdminPwdExpired": "Fjalëkalimi i përdoruesit të administratorit skaduar",
        "AdminActDisabled": "Llogaria e administratorit është çaktivizuar",
        "AdminActExpired": "Llogaria e administratorit ka skaduar",
        "AdminMaxFailedLoginAttempts": "Admin e llogarisë së përdoruesit është e kyçur. Përpjekjet maksimale të dështuara të hyrjes tejkalojnë kufirin e përcaktuar. Ju lutemi kontaktoni administratorin.",
        "EnterVAVerCode": "Shkruani OTP të marrë në celularin tuaj të regjistruar",
        "SkipThisTime": "Kaloni këtë herë",
        "ResendOTP": "Ridërgo OTP -në",
    }
});

}(this));
