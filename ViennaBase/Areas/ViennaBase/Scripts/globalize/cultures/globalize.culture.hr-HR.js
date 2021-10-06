/*
 * Globalize Culture hr-HR
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

(function( window, undefined ) {

var Globalize;

if ( typeof require !== "undefined" &&
	typeof exports !== "undefined" &&
	typeof module !== "undefined" ) {
	// Assume CommonJS
	Globalize = require( "globalize" );
} else {
	// Global variable
	Globalize = window.Globalize;
}

Globalize.addCultureInfo( "hr-HR", "default", {
	name: "hr-HR",
	englishName: "Croatian (Croatia)",
	nativeName: "hrvatski (Hrvatska)",
	language: "hr",
	numberFormat: {
		pattern: ["- n"],
		",": ".",
		".": ",",
		percent: {
			pattern: ["-n%","n%"],
			",": ".",
			".": ","
		},
		currency: {
			pattern: ["-n $","n $"],
			",": ".",
			".": ",",
			symbol: "kn"
		}
	},
	calendars: {
		standard: {
			"/": ".",
			firstDay: 1,
			days: {
				names: ["nedjelja","ponedjeljak","utorak","srijeda","četvrtak","petak","subota"],
				namesAbbr: ["ned","pon","uto","sri","čet","pet","sub"],
				namesShort: ["ne","po","ut","sr","če","pe","su"]
			},
			months: {
				names: ["siječanj","veljača","ožujak","travanj","svibanj","lipanj","srpanj","kolovoz","rujan","listopad","studeni","prosinac",""],
				namesAbbr: ["sij","vlj","ožu","tra","svi","lip","srp","kol","ruj","lis","stu","pro",""]
			},
			monthsGenitive: {
				names: ["siječnja","veljače","ožujka","travnja","svibnja","lipnja","srpnja","kolovoza","rujna","listopada","studenog","prosinca",""],
				namesAbbr: ["sij","vlj","ožu","tra","svi","lip","srp","kol","ruj","lis","stu","pro",""]
			},
			AM: null,
			PM: null,
			patterns: {
				d: "d.M.yyyy.",
				D: "d. MMMM yyyy.",
				t: "H:mm",
				T: "H:mm:ss",
				f: "d. MMMM yyyy. H:mm",
				F: "d. MMMM yyyy. H:mm:ss",
				M: "d. MMMM"
			}
		}
	},
	messages: {
	    "Connection": "Veza",
	    "Defaults": "Uobièajeno",
	    "Login": "prijava",
	    "File": "Datoteka",
	    "Exit": "Izlaz",
	    "Help": "Pomoæ",
	    "About": "O programu",
	    "Host": "Host",
	    "Database": "Baza podataka",
	    "User": "Korisnik",
	    "EnterUser": "Unos korisnika",
	    "Password": "Lozinka",
	    "EnterPassword": "Unos lozinke",
	    "Language": "Jezika",
	    "SelectLanguage": "Izbor jezika",
	    "Role": "Uloga",
	    "Client": "Klijent",
	    "Organization": "Organizacija",
	    "Date": "Datum",
	    "Warehouse": "Skladište",
	    "Printer": "Pisac",
	    "Connected": "Spojeno",
	    "NotConnected": "Nije spojeno",
	    "DatabaseNotFound": "Baza podataka nije pronadena",
	    "UserPwdError": "Lozinka ne odgovara korisniku",
	    "RoleNotFound": "Uloga nije pronadena",
	    "Authorized": "Autoriziran",
	    "Ok": "U redu",
	    "Cancel": "Otkazati",
	    "VersionConflict": "Konflikt verzija",
	    "VersionInfo": "Server <> Klijent",
	    "PleaseUpgrade": "Molim pokrenite nadogradnju programa",


	    //New Resource

	    "Back": "natrag",
	    "SelectRole": "Odaberite Uloga",
	    "SelectOrg": "Odaberite organizacija",
	    "SelectClient": "Odaberite klijenta",
	    "SelectWarehouse": "Odaberite galeriju",
	    "VerifyUserLanguage": "Provjera korisnika i jezik",
	    "LoadingPreference": "Učitavanje sučelja",
	    "Completed": "dovršen",
        "RememberMe": "Zapamti me",
        "FillMandatoryFields": "Ispunite obavezna polja",
        "BothPwdNotMatch": "Obje lozinke moraju se podudarati.",
        "mustMatchCriteria": "Minimalna duljina lozinke je 5. Lozinka mora imati najmanje 1 znak s velikim slovom, 1 znak malih slova, jedan poseban znak (@ $!% *? &) I jednu znamenku. Lozinka mora početi s znakom.",
        "NotLoginUser": "Korisnik se ne može prijaviti u sustav",
        "MaxFailedLoginAttempts": "Korisnički račun je zaključan. Maksimalni neuspjeli pokušaji prijave premašuju definiranu granicu. Molimo kontaktirajte administratora.",
        "UserNotFound": "Korisničko ime nije ispravno.",
        "RoleNotDefined": "Za ovog korisnika nije definirana uloga",
        "oldNewSamePwd": "stara i nova lozinka moraju se razlikovati.",
        "NewPassword": "Nova lozinka",
        "NewCPassword": "Potvrdi novu lozinku",
        "EnterOTP": "Unesite OTP",
        "WrongOTP": "Pogrešno ušao OTP",
        "ScanQRCode": "Skenirajte kôd pomoću Google Autentifikatora",
		"EnterVerCode": "Unesite OTP generiran od strane vaše mobilne aplikacije",
		"EnterVAVerCode": "Unesite OTP primljen na registrirani mobitel",
		"SkipThisTime": "Ovaj put preskočite",
		"ResendOTP": "Ponovno pošaljite OTP",
	}
});

}( this ));
