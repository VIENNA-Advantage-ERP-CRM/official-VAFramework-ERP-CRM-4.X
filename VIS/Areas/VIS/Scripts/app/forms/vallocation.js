; (function (VIS, $) {

    function VAllocation() {
        this.frame;
        this.windowNo;
        var ctx = VIS.Env.getCtx();
        var $self = this;
        var $root = $('<div class="vis-allocate-root vis-forms-container">');
        var $row1 = $('<div class="vis-leftpanel-wrapper vis-pad-0 vis-leftsidebarouterwrap">');
        //set height of filters div and Process Button div
        var $innerRow = $('<div class="vis-leftpanel-inn-wrap">');
        var rowContiner = $('<div class="vis-allocation-rightContainer vis-formouterwrpdiv">');
        var $row2 = $('<div class="vis-allocate-paymentdiv" >');
        var $row3 = $('<div class="vis-allocate-cashdiv" >');
        var $row4 = $('<div class="vis-allocate-invoicediv" >');
        //added GL Grid for GL Allocation
        var $row5 = $('<div class="vis-allocate-gldiv" style = "height:49%" >');
        var $row6 = $('<div >');

        var summation = "Σ";

        var $OrgFilter = null;
        var $vSearchBPartner = null;
        //VIS_0045: control for conversion type in left panel
        var $vConversionType = null;
        //Search Filter for Invoice and Payment Grid
        var $vInvPayMethod = null;
        var $vPayMethod = null;
        var $cmbCurrency = $('<select class="vis-allocation-currencycmb" ></select>');
        var $cmbOrg = null;
        //Declare variables
        var $cmbDocType = $('<select class="sel-allocation"></select>');
        var $payDocType = $('<select class="sel-allocation"></select>');
        var $cashPayType = $('<select class="sel-allocation"></select>');
        var $invDocbaseType = $('<select class="sel-allocation"></select>');
        var $payDocbaseType = $('<select class="sel-allocation"></select>');
        var $txtDocNo, fromDate, toDate, docNo, c_docType_ID, payMethod_ID = null;
        var $fromDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $toDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $pfromDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $ptoDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $cfromDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $ctoDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $gfromDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $gtoDate = $('<input class="date-allocation" type="date" max="9999-12-31"></input>');
        var $srchInv = $('<input value = "" placeholder = "Search..." type = "text" id = _SrchTxtbx_"' + $self.windowNo + '>');
        var docType, c_DocType_ID = null;
        //---------------------
        var $divPayment = null;
        var $divCashline = null;
        var $divInvoice = null;

        //Declared variables for gl-allocation
        var $divGl = null;
        var $allocationFrom = $('<select class="vis-allocation-currencycmb" ></select>');
        var $allocationTo = $('<select class="vis-allocation-currencycmb" ></select>');
        var $vchkGlVoucher = null;
        var $vchkGlInvoice = null;
        // var $divChkGLInvoice = null;
        var $lblglSum = null;
        var $glSelectAll = null;
        var _noGL = 0;
        var $glLineGrid = null;
        var $DivGllineGrid;
        var readOnlyGL = true;
        var glTotal = 0;
        var colGlCheck = false;
        var prevAmt = 0;
        var presAmt = 0;
        var getGLChanges = [];
        //End

        var $gridPayment = null;
        var $gridCashline = null;
        var $gridInvoice = null;

        //Added for Select All Functionality by Manjot assigned by Mukesh sir and Savita
        var $invSelectAll = null;
        var $paymentSelctAll = null;
        var $cashSelctAll = null;
        var maxDate = null;

        var $vchkMultiCurrency = null;
        var $vchkAllocation = null;

        //Added for inter-business partner work assigned by puneet and mukesh sir
        var $vchkBPAllocation = null;
        // inter-company 
        var $vchICompanyAllocation = null;
        var _isInterBPartner = false;
        var _isInterCompany = false;
        var $dateAcct = null;
        var $date = null;
        var conversionDate = "";
        var $conversionDate = null;
        var $conversionDiv = null;

        var _C_BPartner_ID = 0;
        var _C_Currency_ID = 0;
        var _payment = 10;
        var _paymentCash = 8;
        var date = null;
        var _open = 7;
        var _openPay = 8;
        var _openInv = 9;
        var _discount = 8;
        var _writeOff = 11;
        var _applied = 12;

        var $lblPaymentSum = null;
        var $lblCashSum = null;
        var $lblInvoiceSum = null;

        var _noInvoices = 0;
        var _noPayments = 0;
        var _noCashLines = 0;

        //search for each grid
        var $srchPayment = null;
        var $srchInvoice = null;
        var $srchCashJournal = null;
        var $srchGL = null;

        var $srchbtnPayment = null;
        var $srchbtnInvoice = null;
        var $srchbtnCashJournal = null;
        var $srchbtnGL = null;

        // array for selected GL Journal
        var SelectedGL = [];
        var totalselectedgl = 0;
        var $glclrbtn = null;

        // array for selected Cash Journal
        var selectedCashlines = [];
        var totalselectedcash = 0;
        var $cashclrbtn = null;

        // array for selected payements
        var selectedPayments = [];
        var totalselectedpay = 0;
        var $payclrbtn = null;
        // array for selected invoices
        var selectedInvoices = [];
        var totalselectedinv = 0;
        var $clrbtn = null;

        var colInvCheck = false;
        var colPayCheck = false;
        var colCashCheck = false;

        //for set isBusyIndicatorHidden() as true
        var setallgridsLoaded = false;

        var $vtxtDifference = null;
        var $vbtnAllocate = null;
        //added new search button for search in left panel
        var $vbtnSearch = null;
        var $vlblAllocCurrency = null;
        //var $vchkAutoWriteOff = null;

        var selection = null;
        var $bsyDiv = null;
        var self = this;

        var readOnlyCash = true;
        var readOnlyPayment = false;

        var paymentTotal = 0;
        var invoiceTotal = 0;

        var countVA009 = 0;
        var stdPrecision = 0;

        //Culture seperator object
        var format = VIS.DisplayType.GetNumberFormat(VIS.DisplayType.Amount);
        var dotFormatter = VIS.Env.isDecimalPoint();

        /* Variable for Paging*/
        var PAGESIZE = 50;
        var pageNoInvoice = 1, gridPgnoInvoice = 1, invoiceRecord = 0;
        var pageNoCashJournal = 1, gridPgnoCashJounal = 1, CashRecord = 0;
        var pageNoPayment = 1, gridPgnoPayment = 1, paymentRecord = 0;
        var pageNoGL = 1, gridPgnoGL = 1, glRecord = 0;
        var isPaymentGridLoaded = false, isCashGridLoaded = false, isInvoiceGridLoaded = false, isGLGridLoaded = false;
        var C_ConversionType_ID = 0;

        //array to get all the date of selected grid records
        var _allDates = [];

        // is used to compare org of payment / invoice / cash journal with org of selecetd dropdown
        var isOrgMatched = true;
        //Culture seperator object
        var culture = new VIS.CultureSeparator();
        var baseUrl = VIS.Application.contextUrl;
        var dataSetUrl = baseUrl + "JsonData/JDataSetWithCode";
        var executeScalar = function (sql, params, callback) {
            var async = callback ? true : false;
            var dataIn = { sql: sql, page: 1, pageSize: 0 }

            var value = null;


            getDataSetJString(dataIn, async, function (jString) {
                dataSet = new VIS.DB.DataSet().toJson(jString);
                var dataSet = new VIS.DB.DataSet().toJson(jString);
                if (dataSet.getTable(0).getRows().length > 0) {
                    value = dataSet.getTable(0).getRow(0).getCell(0);

                }
                else { value = null; }
                dataSet.dispose();
                dataSet = null;
                if (async) {
                    callback(value);
                }
            });

            return value;
        };

        var elements = [
            "AD_Org_ID",
            "C_BPartner_ID",
            "C_Currency_ID",
            "C_Payment_ID ",
            "C_CashLine_ID",
            "C_Cash_ID",
            "C_Invoice_ID ",
            "Date",
            "DocumentNo",
            "TrxCurrency",
            "C_ConversionType_ID",
            "Amount",
            "ConvertedAmount",
            "OpenAmount",
            "multiplierap",
            "AppliedAmount",
            "C_ConversionType_ID",
            "RECEIPTNO",
            "PaymentType",
            "ccashlineid",
            "ScheduleDate",
            "cinvoiceid",
            "DiscountAmt",
            "DocBaseType",
            "WriteOffAmount",
            "C_InvoicePaySchedule_ID",
            "DateAcct",
            "GL_JOURNALLINE_ID",
            "GL_Journal_ID",
            "DocType",
            "Doc_Base_Type",
            "FromDate",
            "Payment_Type",
            "CreditOrDebit",
            "Account",
            "VIS_Check",
            "AD_OrgTrx_ID",
            "PaymentMethod"
        ];

        VIS.translatedTexts = VIS.Msg.translate(VIS.Env.getCtx(), elements, true);

        //DataSet String
        function getDataSetJString(data, async, callback) {
            var result = null;
            // data.sql = VIS.secureEngine.encrypt(data.sql);
            $.ajax({
                url: dataSetUrl,
                type: "POST",
                datatype: "json",
                contentType: "application/json; charset=utf-8",
                async: async,
                data: JSON.stringify(data)
            }).done(function (json) {
                result = json;
                if (callback) {
                    callback(json);
                }
                //return result;
            });
            return result;
        };

        //initialize();

        this.Initialize = function () {
            _C_Currency_ID = ctx.getContextAsInt("$C_Currency_ID");   //  default
            countVA009 = executeScalar("VIS_131");
            fillLookups();
            loadDocType();
            loadPaymentType()
            loadpayDocType();
            loadinvDocbaseType();
            loadpayDocbaseType();
            loadOrg();  // load Organization
            loadCurrencyPrecision();
            loadOrganization();
            //load allocation combo
            loadAllocationCmb();
            //end
            createRow1();
            createRow2();
            createRow3();
            createRow4();
            //added for gl-allocation
            createRow5();
            rowContiner.append($row2).append($row3).append($row4).append($row5).append($row6);
            //---Set Business Partner Mandatory---Neha
            //$vSearchBPartner.getControl().next().addClass('vis-ev-col-mandatory');
            //---Resize the Parameter div and Display and hide Payment and Cash div----Neha-----
            //$row1.resizable({
            //    handles: 'e',
            //    minWidth: 226,
            //    maxWidth: 600,
            //    resize: function (event, ui) {
            //        var width = ui.size.width;
            //        if (width > 197) {
            //            rowContiner.width($(document).width() - (width + 40));
            //        }
            //    }
            //});
            $row2.css('display', '');
            $row3.css('display', 'none');
            //------------------------------
            $root.append($row1.append($innerRow)).append(rowContiner);
            // to set process button static in design it will not scroll with other filters
            var $LeftPanelBottomWrap = $('<div class="vis-allocation-leftpanel-bottomDiv">');
            var $DiffernceWrapDiv = $('<div class="vis-DiffernceWrapDiv">');
            $LeftPanelBottomWrap.append($DiffernceWrapDiv);

            $DiffernceWrapDiv.append('<div class="vis-allocation-leftControls">' +
                '<span class="vis-allocation-inputLabels">' + VIS.Msg.getMsg("Difference")
                + '</span>');
            $DiffernceWrapDiv.append('<div class="vis-allocation-leftControls">'
                + '<span class="vis-allocation-lblCurrnecy" style=" padding: 0 5px; "></span>'
                + '<span class="vis-allocation-lbldifferenceAmt">'
                + '</span>');
            var $divProcess = $('<div style="padding-right: 15px;float: right; margin-top: 10px;">');
            $divProcess.append(' <a class="vis-group-btn vis-group-create vis-group-grayBtn" style="float: right;">' + VIS.Msg.getMsg('Process') + '</a>');
            //$row1.append($divProcess);
            var $divSearch = $('<div style="padding-right: 15px;float: right; margin-top: 10px;">');
            $divSearch.append(' <a class="vis-group-create vis-group-grayBtn" id="vis_srchbtn_' + $self.windowNo + '" style="float: left;">' + VIS.Msg.getMsg('Search') + '</a>');
            $row1.append($LeftPanelBottomWrap);
            $LeftPanelBottomWrap.append($divProcess);
            $LeftPanelBottomWrap.append($divSearch);
            $root.append($row1.append($innerRow).append($LeftPanelBottomWrap)).append(rowContiner);
            $vbtnAllocate = $root.find('.vis-group-btn');
            $vbtnAllocate.css({ "pointer-events": "none", "opacity": "0.5" });
            $vbtnSearch = $root.find('#vis_srchbtn_' + $self.windowNo + '');
            createBusyIndicator();
            eventHandling();
            //added to load all blank grids
            blankAllGrids();
            //added for gl-allocation
            $row5.css('display', 'none');
            //end

            $vtxtDifference = $DiffernceWrapDiv.find('.vis-allocation-lbldifferenceAmt');
            $vlblAllocCurrency = $DiffernceWrapDiv.find('.vis-allocation-lblCurrnecy');

            $root.find(".vis-allocation-resultdiv").css({ "width": "100%", "margin": "0", "bottom": "0", "position": "inherit" });
            $bsyDiv[0].style.visibility = "hidden";
        };


        function eventHandling() {
            //Organization 
            $OrgFilter.on("change", function (e) {
                ctx.setContext($self.windowNo, "OrgID", VIS.Utility.Util.getValueOfInt($OrgFilter.val()));
                // Clear conversion type on Org Change
                $vConversionType.getControl().val('');
                //when select MultiCurrency without selecting conversionDate it will clear the grids
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                //clear the girds based on which grid is active (true).
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    // Commented because we need to search on button click suggested by ashish
                    //if ($allocationFrom.val() != 0 && $allocationTo.val() != 0) {
                    //    if (allgridsLoaded()) {
                    //        if ($gridInvoice) {
                    //            clrInvoice(e);
                    //        }
                    //        if ($glLineGrid) {
                    //            clrGLLine(e);
                    //        }
                    //        if ($gridPayment) {
                    //            clrPayment(e);
                    //        }
                    //        if ($gridCashline) {
                    //            clrCashLine(e);
                    //        }
                    //    }
                    //    else {
                    //        //VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                    //        blankAllGrids();
                    //    }
                    //}
                }
                else {
                    $vSearchBPartner.getControl().addClass('vis-ev-col-mandatory'); //highlight the field
                    blankAllGrids();
                }
            });

            $vSearchBPartner.fireValueChanged = bpValueChanged;

            //VIS_0045: event when we change conversion type in left panel
            $vConversionType.fireValueChanged = conversionTypeChanged;

            $cmbCurrency.on("change", function (e) {
                vetoableChange("C_Currency_ID", $cmbCurrency.val());
                //when select MultiCurrency without selecting conversionDate it will clear the grid's
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    // Commented because we need to search on button click suggested by ashish
                    //if ($allocationFrom.val() != 0 && $allocationTo.val() != 0) {
                    //    if (allgridsLoaded()) {
                    //        if ($gridInvoice) {
                    //            clrInvoice(e);
                    //        }
                    //        if ($glLineGrid) {
                    //            clrGLLine(e);
                    //        }
                    //        if ($gridPayment) {
                    //            clrPayment(e);
                    //        }
                    //        if ($gridCashline) {
                    //            clrCashLine(e);
                    //        }
                    //    }
                    //    else {
                    //        //VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                    //        blankAllGrids();
                    //    }
                    //}
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    blankAllGrids();
                }
                //if ($cmbCurrency.val() > 0) {
                //    $cmbCurrency.removeClass('vis-ev-col-mandatory');
                //}
                //else {
                //    $cmbCurrency.addClass('vis-ev-col-mandatory');
                //}
                //Commetd code because now we want to search data on search button not on every control's event
                //////loadBPartner();
                //loadCurrencyPrecision(parseInt($cmbCurrency.val()));
            });

            $vchkMultiCurrency.on("change", function (e) {
                vetoableChange("Date", $vchkMultiCurrency.is(':checked'));
                //when select MultiCurrency without selecting conversionDate it will clear the grid's 
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    // Commented because we need to search on button click suggested by ashish
                    // blankAllGrids();
                    // clearRightPanelFilters(); //clear right side  filters and selected records
                    $conversionDate.val('');
                    return;
                }
                //clear the grids which is true
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    // Commented because we need to search on button click suggested by ashish
                    //if ($allocationFrom.val() != 0 && $allocationTo.val() != 0) {
                    //    if (allgridsLoaded()) {
                    //        if ($gridInvoice) {
                    //            clrInvoice(e);
                    //        }
                    //        if ($glLineGrid) {
                    //            clrGLLine(e);
                    //        }
                    //        if ($gridPayment) {
                    //            clrPayment(e);
                    //        }
                    //        if ($gridCashline) {
                    //            clrCashLine(e);
                    //        }
                    //    }
                    //}
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    blankAllGrids();
                }
                if (!$vchkMultiCurrency.is(':checked')) {
                    $conversionDate.val('');
                }
                //Commetd code because now we want to search data on search button not on every control's event
                //////loadBPartner();
            });

            $vchkAllocation.on("change", function (e) {
                if ($vchkGlVoucher.is(':checked')) {
                    $(".vis-allocate-invoicediv").insertBefore(".vis-allocate-gldiv");
                    $row2.css('display', 'none'); // Payment Grid
                    $row3.css('display', 'none');// Cash Grid
                    $row5.css('display', 'block');// GL journal grid
                    $row4.css('display', 'block'); // Invoice Grid
                    if ($gridInvoice)
                        $gridInvoice.refresh();
                    if ($glLineGrid)
                        $glLineGrid.refresh();
                }
                vchkapplocationChange();
            });

            $vbtnSearch.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                //VIS_0045: Conversion type message thorw only when Inv to Inv or Inv to Pay allocation
                if ($vchkMultiCurrency.is(':checked') && ($conversionDate.val() == "" ||
                    ($vConversionType.getValue() == null &&
                        (($allocationFrom.val() == "I" && $allocationTo.val() == "P") ||
                            ($allocationFrom.val() == "P" && $allocationTo.val() == "I") ||
                            ($allocationFrom.val() == "I" && $allocationTo.val() == "I"))))) {
                    if ($conversionDate.val() == "") {
                        VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    }
                    else if ($vConversionType.getValue() == null) {
                        VIS.ADialog.warn("VIS_SlctcnvrsnType");
                    }
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        clearRightPanelFilters(); //clear right side  filters and selected records
                        searchbyParameters();
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $vSearchBPartner.getControl().addClass('vis-ev-col-mandatory'); //highlight the field
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    blankAllGrids(); // clear the records from selected grids
                }
            });

            //added for gl-allocation
            $vchkGlVoucher.on("change", function (e) {
                $vchkAllocation.prop('checked', false);
                if ($vchkGlVoucher.is(':checked')) {
                    $(".vis-allocate-invoicediv").insertBefore(".vis-allocate-gldiv");
                    $row4.css('display', 'block');// Invoice Grid
                    $row5.css('display', 'block');// GL journal grid
                    $row2.css('display', 'none'); // Payment Grid
                    $row3.css('display', 'none');// Cash Grid
                    if ($gridInvoice)
                        $gridInvoice.refresh();
                    if ($glLineGrid)
                        $glLineGrid.refresh();
                }
                else {
                    $(".vis-allocate-invoicediv").insertBefore(".vis-allocate-paymentdiv");
                    $row4.css('display', 'block');// Invoice Grid
                    $row5.css('display', 'none');// GL journal grid
                    $row2.css('display', 'block'); // Payment Grid
                    $row3.css('display', 'none');// Cash Grid
                    if ($gridInvoice)
                        $gridInvoice.refresh();
                    if ($gridPayment)
                        $gridPayment.refresh();
                }
                //////loadGLDataGrid(e);
            });

            $vchkGlInvoice.on("change", function (e) {
            });
            //end

            $date.on("change", function (e) {
                /** Commented because we need to stop the load business partner on selection of date
                //vetoableChange("Date", $date.val());
                //loadBPartner();
                */
                if (getSelectedRecordsCount()) {
                    if (new Date($date.val()) < maxDate) {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectMaxDate") + Globalize.format(new Date(maxDate), "yyyy-MM-dd"), "");
                        $date.val(Globalize.format(new Date(maxDate), "yyyy-MM-dd"));
                        return false;
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("PleaseSelectRecord"), "");
                }
            });
            $vbtnAllocate.on("click", function (e) {
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) == 0) {
                    //it returns Message if not selected Business Partner.
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $vSearchBPartner.getControl().addClass('vis-ev-col-mandatory'); //highlight the field
                    clearRightPanelFilters(); //clear all selected filters from right Panel
                    blankAllGrids(); //clear records from all grids
                    return false;
                }
                if (getSelectedRecordsCount()) {
                    if ($date.val().trim() == "") {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("InvalidDateFormat"), "");
                        return false;
                    }
                    //Handled in Central America Time Zone.
                    //if (new Date($date.val()) < maxDate) {
                    if ($date.val() < Globalize.format(new Date(maxDate), "yyyy-MM-dd")) {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectMaxDate") + Globalize.format(new Date(maxDate), "yyyy-MM-dd"), "");
                        $date.val(Globalize.format(new Date(maxDate), "yyyy-MM-dd"));
                        return false;
                    }

                    if (isOrgMatched) {
                        //Org Manadatory because system will create view allocation in the selected org.
                        if (parseInt($cmbOrg.val()) > 0)
                            allocate();
                        else
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("ValidateOrg"), "");
                    }
                    else {
                        /*Selected records are having a different organization than parameter organization. Do you want to proceed?*/
                        VIS.ADialog.confirm("ConfirmForDiffOrg", true, "", "Confirm", function (result) {
                            if (result) {
                                //Org Manadatory because system will create view allocation in the selected org.
                                if (parseInt($cmbOrg.val()) > 0)
                                    allocate();
                                else
                                    VIS.ADialog.info("", true, VIS.Msg.getMsg("ValidateOrg"), "");
                            }
                            else {
                                return;
                            }
                        });
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("PleaseSelectRecord"), "");
                }
            });
            //hide the AutoWriteOff Checkbox 
            //$vchkAutoWriteOff.on("click", function (e) {
            //    autoWriteOff();
            //    calculate();
            //});
            //-----Controls events and load Invoice Grid--Neha
            //$txtDocNo.on("change", function (e) {
            //    //Commetd code because now we want to search data on search button not on every control's event
            //    //////loadInvoice();
            //});
            //filter the records based on Payment Method in Invocie grid
            $vInvPayMethod.fireValueChanged = invPayMethodChanged;
            //filter the records based on Doctype in Invocie grid
            $cmbDocType.on("change", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadInvoice();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $cmbDocType.val(0);
                        }
                    }
                    else {
                        $cmbDocType.val(0);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $cmbDocType.val(0);
                }
            });

            //filter the records based on Payment Method in Payment grid
            $vPayMethod.fireValueChanged = payMethodChanged;
            //filter the records based on Doctype in Payment grid
            $payDocType.on("change", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedPayments();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $payDocType.val(0);
                        }
                    }
                    else {
                        $payDocType.val(0);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $payDocType.val(0);
                }
            });

            //filter the records based on DocBaseType in Payment grid
            $payDocbaseType.on("change", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedPayments();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $payDocbaseType.val(0);
                        }
                    }
                    else {
                        $payDocbaseType.val(0);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $payDocbaseType.val(0);
                }
            });

            //filter the records based on DocBaseType in Invoice grid
            $invDocbaseType.on("change", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadInvoice();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $invDocbaseType.val(0);
                        }
                    }
                    else {
                        $invDocbaseType.val(0);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $invDocbaseType.val(0);
                }
            });

            //filter the records based on paymentType in Cash Journal Line grid
            $cashPayType.on("change", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedCashLines();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $cashPayType.val(0);
                        }
                    }
                    else {
                        $cashPayType.val(0);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $cashPayType.val(0);
                }
            });

            // filteration based on from and toDates for Invoice Grid
            $fromDate.on("blur", function (e) {
                var dateVal = Date.parse($fromDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadInvoice();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $fromDate.val('');
                        }
                    }
                    else {
                        $fromDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $fromDate.val('');
                }
            });
            $toDate.on("blur", function (e) {
                var dateVal = Date.parse($toDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadInvoice();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $toDate.val('');
                        }
                    }
                    else {
                        $toDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $toDate.val('');
                }
            });

            // filteration based on from and toDates for Payment Grid
            $pfromDate.on("blur", function (e) {
                var dateVal = Date.parse($pfromDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedPayments();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $pfromDate.val('');
                        }
                    }
                    else {
                        $pfromDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $pfromDate.val('');
                }
            });
            $ptoDate.on("blur", function (e) {
                var dateVal = Date.parse($ptoDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedPayments();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $ptoDate.val('');
                        }
                    }
                    else {
                        $ptoDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $ptoDate.val('');
                }
            });

            //filteration based on from and toDates for cash journal Grid
            $cfromDate.on("blur", function (e) {
                var dateVal = Date.parse($cfromDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedCashLines();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $cfromDate.val('');
                        }
                    }
                    else {
                        $cfromDate.val('');
                    }

                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $cfromDate.val('');
                }
            });
            $ctoDate.on("blur", function (e) {
                var dateVal = Date.parse($ctoDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedCashLines();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $ctoDate.val('');
                        }
                    }
                    else {
                        $ctoDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $ctoDate.val('');
                }
            });

            // filteration based on from and toDates for GL journal Grid
            $gfromDate.on("blur", function (e) {
                var dateVal = Date.parse($gfromDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadGLVoucher();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $gfromDate.val('');
                        }
                    }
                    else {
                        $gfromDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $gfromDate.val('');
                }
            });
            $gtoDate.on("blur", function (e) {
                var dateVal = Date.parse($gtoDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadGLVoucher();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $gtoDate.val('');
                        }
                    }
                    else {
                        $gtoDate.val('');
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $gtoDate.val('');
                }
            });

            //click event for search Invoice records which is based on Document No
            $srchInvoice.on("keypress", function (e) {
                if (e.keyCode == 13) {
                    //when select MultiCurrency without selecting conversionDate it will return a Message
                    if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                        VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                        blankAllGrids();
                        clearRightPanelFilters(); //clear right side  filters and selected records
                        return;
                    }
                    if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                        if (checkisSelectedAllocationFromAndTo()) {
                            if (allgridsLoaded()) {
                                gridPgnoInvoice = 1;
                                loadInvoice();
                            }
                            else {
                                VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            }
                        }
                    }
                    else {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    }
                    // $srchInvoice.val('');
                }
            });
            $srchbtnInvoice.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            gridPgnoInvoice = 1;
                            loadInvoice();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
                //$srchInvoice.val('');
            });

            //click event for search Payment records which is based on Document No
            $srchPayment.on("keypress", function (e) {
                if (e.keyCode == 13) {
                    //when select MultiCurrency without selecting conversionDate it will return a Message
                    if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                        VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                        blankAllGrids();
                        clearRightPanelFilters(); //clear right side  filters and selected records
                        return;
                    }
                    if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                        if (checkisSelectedAllocationFromAndTo()) {
                            if (allgridsLoaded()) {
                                gridPgnoPayment = 1;
                                loadUnallocatedPayments();
                            }
                            else {
                                VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            }
                        }
                    }
                    else {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    }
                    //  $srchPayment.val('');
                }
            });
            $srchbtnPayment.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            gridPgnoPayment = 1;
                            loadUnallocatedPayments();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
                // $srchPayment.val('');
            });

            //click event for search Cash Journal line records which is based on Document No
            $srchCashJournal.on("keypress", function (e) {
                if (e.keyCode == 13) {
                    //when select MultiCurrency without selecting conversionDate it will return a Message
                    if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                        VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                        blankAllGrids();
                        clearRightPanelFilters(); //clear right side  filters and selected records
                        return;
                    }
                    if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                        if (checkisSelectedAllocationFromAndTo()) {
                            if (allgridsLoaded()) {
                                gridPgnoCashJounal = 1;
                                loadUnallocatedCashLines();
                            }
                            else {
                                VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            }
                        }
                    }
                    else {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    }
                    // $srchCashJournal.val('');
                }
            });
            $srchbtnCashJournal.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            gridPgnoCashJounal = 1;
                            loadUnallocatedCashLines();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
                // $srchCashJournal.val('');
            });

            //click event for search GL Journal line records which is based on Document No
            $srchGL.on("keypress", function (e) {
                if (e.keyCode == 13) {
                    //when select MultiCurrency without selecting conversionDate it will return a Message
                    if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                        VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                        blankAllGrids();
                        clearRightPanelFilters(); //clear right side  filters and selected records
                        return;
                    }
                    if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                        if (checkisSelectedAllocationFromAndTo()) {
                            if (allgridsLoaded()) {
                                gridPgnoGL = 1;
                                loadGLVoucher();
                            }
                            else {
                                VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            }
                        }
                    }
                    else {
                        VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    }
                    //$srchGL.val('');
                }
            });

            $srchbtnGL.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            gridPgnoGL = 1;
                            loadGLVoucher();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
                //$srchGL.val('');
            });
            //-----For Select All buttons on grid by Manjot assigned by Mukesh sir and Savita
            $invSelectAll.on("change", function (e) {
                //window.setTimeout(function () {
                $bsyDiv[0].style.visibility = "visible";
                var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
                for (var i = 0; i < chk.length; i++) {
                    $(chk[i]).prop('checked', $invSelectAll.prop("checked"));

                    //VA228:Break statement when max limit 50 reached
                    if (!checkCheckedLimitOnSelectAllCheckBox(chk[i], true, false, false, false))
                        break;

                    //$(chk[i]).change(e);
                    $gridInvoice.editChange.call($gridInvoice, chk[i], i, 0, e);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridInvoice.trigger(eData);
                }

                // Clear Selected invoices array when we de-select the select all checkbox. work done for to hold all the selected invoices
                if ($invSelectAll.prop("checked") == false)
                    selectedInvoices = [];
                $bsyDiv[0].style.visibility = "hidden";
                //}, 300);
            });

            //change event for checkbox to select/unselet all for Payment grid
            $paymentSelctAll.on("change", function (e) {
                $bsyDiv[0].style.visibility = "visible";
                var chk = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]');
                for (var i = 0; i < chk.length; i++) {
                    $(chk[i]).prop('checked', $paymentSelctAll.prop("checked"));

                    if (!checkCheckedLimitOnSelectAllCheckBox(chk[i], false, true, false, false))
                        break;

                    //$(chk[i]).change(e);
                    $gridPayment.editChange.call($gridPayment, chk[i], i, 0, e);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridPayment.trigger(eData);
                }
                if ($paymentSelctAll.prop("checked") == false)
                    selectedPayments = [];
                $bsyDiv[0].style.visibility = "hidden";
            });

            //change event forf checkbox to select/unselect all for Cash Journal line
            $cashSelctAll.on("change", function (e) {
                $bsyDiv[0].style.visibility = "visible";
                var chk = $('#grid_' + $gridCashline.name + '_records td[col="0"]').find('input[type="checkbox"]');
                for (var i = 0; i < chk.length; i++) {
                    $(chk[i]).prop('checked', $cashSelctAll.prop("checked"));

                    if (!checkCheckedLimitOnSelectAllCheckBox(chk[i], false, false, true, false))
                        break;

                    //$(chk[i]).change(e);
                    $gridCashline.editChange.call($gridCashline, chk[i], i, 0, e);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridCashline.trigger(eData);
                }
                if ($cashSelctAll.prop("checked") == false)
                    selectedCashlines = [];
                $bsyDiv[0].style.visibility = "hidden";
            });



            //check all grids are loaded or not
            // return true or false based on condition
            function allgridsLoaded() {
                if (isPaymentGridLoaded == true && isCashGridLoaded == true && isInvoiceGridLoaded == true && isGLGridLoaded == true) {
                    return true;
                }
                return false;
            }

            //validation for allocationFrom and allocationTo Dropdowns are selected or not.
            // return true or false based on condition
            function checkisSelectedAllocationFromAndTo() {
                if ($allocationTo.val() == 0 && $allocationFrom.val() == 0) {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("allocationFrom_And_allocationTo"), "");
                    $allocationFrom.addClass('vis-ev-col-mandatory');
                    $allocationTo.addClass('vis-ev-col-mandatory');
                    return false;
                }
                else if ($allocationFrom.val() == 0) {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("selectAllocationFrom"), "");
                    $allocationTo.val(0);
                    $allocationFrom.addClass('vis-ev-col-mandatory');
                    $allocationTo.addClass('vis-ev-col-mandatory');
                    return false;
                }
                else if ($allocationTo.val() == 0) {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("selectAllocationTo"), "");
                    $allocationTo.addClass('vis-ev-col-mandatory');
                    return false;
                }
                return true;
            }

            //change event for checkbox to select/unselect all for gl-allocation grid
            $glSelectAll.on("change", function (e) {
                $bsyDiv[0].style.visibility = "visible";
                if ($glSelectAll.prop("checked") == false) {
                    getGLChanges = [];
                }
                var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
                for (var i = 0; i < chk.length; i++) {
                    $(chk[i]).prop('checked', $glSelectAll.prop("checked"));

                    if (!checkCheckedLimitOnSelectAllCheckBox(chk[i], false, false, false, true))
                        break;

                    //$(chk[i]).change(e);
                    $glLineGrid.editChange.call($glLineGrid, chk[i], i, 0, e);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $glLineGrid.trigger(eData);
                }
                if ($glSelectAll.prop("checked") == false)
                    SelectedGL = [];
                $bsyDiv[0].style.visibility = "hidden";
            });
            //end 
            //set/reset the background-color change event
            $cmbOrg.on("change", function (e) {
                if (parseInt($cmbOrg.val()) > 0)
                    $cmbOrg.removeClass('vis-ev-col-mandatory');
                else
                    $cmbOrg.addClass('vis-ev-col-mandatory');

            });
            //Currency Conversion Date change event
            $conversionDate.on("blur", function (e) {
                var dateVal = Date.parse($conversionDate.val());
                var currentTime = new Date(parseInt(dateVal));
                //check if date is valid
                //this check will work for 01/01/1970 on words
                if (!isNaN(currentTime.getTime()) && currentTime.getTime() < 0) {
                    VIS.ADialog.warn("VIS_InvalidDate");
                    return;
                }

                conversionDate = $conversionDate.val();
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                //clear and refresh the grids which is true for selected BusinessPartner and Currency based on ConversionDate
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    // Commented because we need to search on button click suggested by ashish
                    //if ($allocationFrom.val() != 0 && $allocationTo.val() != 0) {
                    //    if (allgridsLoaded()) {
                    //        if ($gridInvoice) {
                    //            clrInvoice(e);
                    //        }
                    //        if ($glLineGrid) {
                    //            clrGLLine(e);
                    //        }
                    //        if ($gridPayment) {
                    //            clrPayment(e);
                    //        }
                    //        if ($gridCashline) {
                    //            clrCashLine(e);
                    //        }
                    //    }
                    //}
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    blankAllGrids();
                }
                //Commetd code because now we want to search data on search button not on every control's event
                //////loadBPartner();
            });
            //---For inter-business Partner data load in all grids
            $vchkBPAllocation.on("change", function (e) {
                vetoableChange("Date", $vchkBPAllocation.is(':checked'));
                _isInterBPartner = $vchkBPAllocation.is(':checked');
                // Commented because we need to search on button click suggested by ashish
                // loadBPartner();
            });
            //filter based on inter company checkbox to get  records
            $vchICompanyAllocation.on("change", function (e) {
                vetoableChange("Date", $vchICompanyAllocation.is(':checked'));
                _isInterCompany = $vchICompanyAllocation.is(':checked');
                // loadBPartner();
            });
            //clear selected invoices records from Invoice grid 
            $clrbtn.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            if ($gridInvoice) {
                                clrInvoice(e);
                            }
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
            });

            //clear selected journal records from GL journal grid
            $glclrbtn.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            if ($glLineGrid) {
                                clrGLLine(e);
                            }
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
            });
            //clear selected cash journals records from Cash Journal grid
            $cashclrbtn.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            if ($gridCashline) {
                                clrCashLine(e);
                            }
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
            });
            //clear selected payment records from Payment grid  
            $payclrbtn.on("click", function (e) {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            if ($gridPayment) {
                                clrPayment(e);
                            }
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                        }
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                }
            });

            //allocation From combo event
            $allocationFrom.on("change", function (e) {
                if ($allocationFrom.val() == 0) {
                    $allocationFrom.addClass('vis-ev-col-mandatory');
                }
                else
                    $allocationFrom.removeClass('vis-ev-col-mandatory');
                //if allocation from is change then we have to clear the selection of invoices
                clearInvoiceArrays(e);

                if ($allocationFrom.val() == "P") { // In case of Payment Cash Option must be Hide
                    $allocationTo.find("option[value=C]").hide();
                    $allocationTo.find("option[value=P]").show();
                    $allocationTo.find("option[value=G]").show();
                }
                else if ($allocationFrom.val() == "C") { // In case of Cash Payment Option must be Hide
                    $allocationTo.find("option[value=P]").hide();
                    $allocationTo.find("option[value=C]").show();// for Cash2Cash
                    $allocationTo.find("option[value=G]").show();
                }
                else if ($allocationFrom.val() == "G") { // In case of GL Journal GL Journal Option must be Hide
                    $allocationTo.find("option[value=P]").show();
                    $allocationTo.find("option[value=C]").show();
                    $allocationTo.find("option[value=G]").hide();
                }
                else { //we need to show all option
                    $allocationTo.find("option[value=C]").show();
                    $allocationTo.find("option[value=P]").show();
                    $allocationTo.find("option[value=G]").show();
                }
                $allocationTo.val(0);
                $allocationTo.addClass('vis-ev-col-mandatory');
                loadGrids($allocationFrom.val());
                displayGrids($allocationFrom.val(), $allocationTo.val());
                //whenever change the allocationTo then the rightpanel filters will be cleared!
                clearRightPanelFilters();
            });

            $allocationTo.on("change", function (e) {

                //both allocationTo and From must be a value then only display the grids.
                if ($allocationTo.val() != 0 && $allocationFrom.val() != 0) {
                    $allocationTo.removeClass('vis-ev-col-mandatory');
                    //if allocation from is change then we have to clear the selection of invoices
                    clearInvoiceArrays(e);

                    var allocfrm = $allocationFrom.val();
                    var allocto = $allocationTo.val();
                    loadGrids($allocationTo.val());
                    displayGrids(allocfrm, allocto);
                }
                else {
                    blankAllGrids();
                    $allocationTo.addClass('vis-ev-col-mandatory');
                }
                //whenever change the allocationTo then the rightpanel filters will be cleared!
                clearRightPanelFilters();
            });

            function payMethodChanged() {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadUnallocatedPayments();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $vPayMethod.setValue(null);
                        }
                    }
                    else {
                        $vPayMethod.setValue(null);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $vPayMethod.setValue(null);
                }
            };

            function invPayMethodChanged() {
                //when select MultiCurrency without selecting conversionDate it will return a Message
                if ($vchkMultiCurrency.is(':checked') && $conversionDate.val() == "") {
                    VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                    blankAllGrids();
                    clearRightPanelFilters(); //clear right side  filters and selected records
                    return;
                }
                if (VIS.Utility.Util.getValueOfInt($vSearchBPartner.value) > 0) {
                    if (checkisSelectedAllocationFromAndTo()) {
                        if (allgridsLoaded()) {
                            loadInvoice();
                        }
                        else {
                            VIS.ADialog.info("", true, VIS.Msg.getMsg("VIS_Search_Btn"), "");
                            $vInvPayMethod.setValue(null);
                        }
                    }
                    else {
                        $vInvPayMethod.setValue(null);
                    }
                }
                else {
                    VIS.ADialog.info("", true, VIS.Msg.getMsg("SelectBusinessPartnerFirst"), "");
                    $vInvPayMethod.setValue(null);
                }
            }
        };

        //refresh all filters from right Panel
        function clearRightPanelFilters() {
            $cashPayType.val(0);
            $invDocbaseType.val(0);
            $payDocbaseType.val(0);
            $payDocType.val(0);
            $cmbDocType.val(0);
            $vInvPayMethod.setValue(null);
            $vPayMethod.setValue(null);
            $fromDate.val('');
            $toDate.val('');
            $pfromDate.val('');
            $ptoDate.val('');
            $cfromDate.val('');
            $ctoDate.val('');
            $gfromDate.val('');
            $gtoDate.val('');
            //clear the search fields
            $srchPayment.val('');
            $srchInvoice.val('');
            $srchGL.val('');
            $srchCashJournal.val('');
            selectedCashlines = [];
            selectedInvoices = [];
            selectedPayments = [];
            SelectedGL = [];
            //refresh AccountDate
            $dateAcct.val(Globalize.format(new Date(), "yyyy-MM-dd"));
            refreshLabel();
        }

        ////to load GL Grid and define events of Gl Grid
        //function loadGLGrid() {
        //    if (glLineGrid != undefined && glLineGrid != null) {
        //        glLineGrid.destroy();
        //        glLineGrid = null;
        //    }
        //    glLineGrid = $divGl.w2grid({
        //        name: "VIS_GLLineGrid_" + $self.windowNo,
        //        //show: {
        //        //    toolbar: true,
        //        //    toolbarSearch: false,
        //        //    toolbarInput: false,
        //        //    searchAll: false
        //        //},
        //        multiSelect: true,
        //        columns: [
        //            { field: "SelectRow", caption: 'check', size: '50px', editable: { type: 'checkbox' } },
        //            { field: "AD_Org_ID", caption: VIS.translatedTexts.AD_Org_ID, size: '85px', hidden: true },
        //            { field: "OrgName", caption: VIS.translatedTexts.AD_Org_ID, size: '105px', hidden: false, sortable: false },
        //            {
        //                field: "DATEACCT", caption: VIS.translatedTexts.DateAcct, render: function (record, index, col_index) {
        //                    var val = record["DATEACCT"];
        //                    return new Date(val).toLocaleDateString();
        //                }, size: '105px', hidden: false, sortable: false
        //            },
        //            { field: "DOCUMENTNO", caption: VIS.translatedTexts.DocumentNo, size: '120px', hidden: false, sortable: false },
        //            { field: "Isocode", caption: VIS.translatedTexts.TrxCurrency, size: '105px', hidden: false },
        //            { field: "ConversionName", caption: VIS.translatedTexts.C_ConversionType_ID, size: '105px', hidden: false, sortable: false },
        //            {
        //                field: "ConvertedAmount", caption: VIS.Msg.getMsg("Amount"), size: '150px', hidden: false, sortable: false, render: function (record, index, col_index) {
        //                    var val = record["ConvertedAmount"];
        //                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
        //                }
        //            },
        //            {
        //                field: "OpenAmount", caption: VIS.translatedTexts.OpenAmount, size: '150px', hidden: false, sortable: false, render: function (record, index, col_index) {
        //                    var val = record["OpenAmount"];
        //                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
        //                }
        //            },
        //            {
        //                field: "AppliedAmt", caption: VIS.translatedTexts.AppliedAmount, size: '150px', hidden: false, sortable: false, render: function (record, index, col_index) {
        //                    var val = record["AppliedAmt"];
        //                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
        //                }
        //            },
        //            {
        //                field: "DATEDOC", caption: VIS.translatedTexts.Date, render: function (record, index, col_index) {
        //                    var val = record["DATEDOC"];
        //                    return new Date(val).toLocaleDateString();
        //                }, size: '80px', hidden: true
        //            },
        //            { field: "GL_JOURNALLINE_ID", caption: VIS.translatedTexts.GL_JOURNALLINE_ID, size: '150px', hidden: true },
        //            { field: "C_ConversionType_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true },
        //            { field: "GL_Journal_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true }

        //        ],
        //        onClick: function (event) {
        //            glCellClicked(event);
        //            getMaxDate();

        //        },
        //        onChange: function (event) {
        //            // glCellChanged(event);
        //            glCellChanged(event);
        //            //alert("onChange");
        //        },
        //        //onDblClick: function (event) {
        //        //    glDoubleClicked(event);
        //        //},
        //        onSelect: function (event) {
        //            if (event.all) {
        //                event.onComplete = function () {
        //                    //alert("eventall");
        //                    //$divPayment.find('#grid_openformatgrid_records').on('scroll', cartPaymentScroll);
        //                }
        //            }
        //            //else
        //            //  alert("eventSingle");
        //        },
        //        //on edit grid columns need to prevent dot or comma according to culture
        //        onEditField: function (event) {
        //            event.onComplete = function (event) {
        //                id = event.recid;
        //                if (event.column == 14 || event.column == 13 || event.column == 10) {
        //                    $('#grid_openformatgrid_rec_' + $self.windowNo + '_edit_' + id + '_' + event.column).keydown(function (event) {
        //                        var isDotSeparator = culture.isDecimalSeparatorDot(window.navigator.language);

        //                        if (!isDotSeparator && (event.keyCode == 190 || event.keyCode == 110)) {// , separator
        //                            return false;
        //                        }
        //                        else if (isDotSeparator && event.keyCode == 188) { // . separator
        //                            return false;
        //                        }
        //                        if (event.target.value.contains(".") && (event.which == 110 || event.which == 190 || event.which == 188)) {
        //                            this.value = this.value.replace('.', '');
        //                        }
        //                        if (event.target.value.contains(",") && (event.which == 110 || event.which == 190 || event.which == 188)) {
        //                            this.value = this.value.replace(',', '');
        //                        }
        //                        if (event.keyCode != 8 && event.keyCode != 9 && (event.keyCode < 37 || event.keyCode > 40) &&
        //                            (event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)
        //                            && event.keyCode != 109 && event.keyCode != 189 && event.keyCode != 110
        //                            && event.keyCode != 144 && event.keyCode != 188 && event.keyCode != 190) {
        //                            return false;
        //                        }
        //                    });
        //                }
        //            };
        //        }
        //    });
        //};

        //to clear the Invoice Grid
        function clrInvoice(e) {
            var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
            for (var i = 0; i < chk.length; i++) {
                $(chk[i]).prop('checked', false);
                //$(chk[i]).change(e);
                $gridInvoice.editChange.call($gridInvoice, chk[i], i, 0, e);
                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                $gridInvoice.trigger(eData);
            }
            selectedInvoices = [];
            $invSelectAll.prop('checked', false);
            $cmbDocType.val(0);
            $invDocbaseType.val(0);
            $vInvPayMethod.setValue(null);
            $srchInvoice.val('');
            $fromDate.val('');
            $toDate.val('');
            loadInvoice();
        }
        //to clear the GLLine Grid
        function clrGLLine(e) {
            var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
            for (var i = 0; i < chk.length; i++) {
                $(chk[i]).prop('checked', false);
                //$(chk[i]).change(e);
                $glLineGrid.editChange.call($glLineGrid, chk[i], i, 0, e);
                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                $glLineGrid.trigger(eData);
            }
            SelectedGL = [];
            $glSelectAll.prop('checked', false);
            $srchGL.val('');
            $gfromDate.val('');
            $gtoDate.val('');
            loadGLVoucher();
        }
        //to clear the Cash Grid
        function clrCashLine(e) {
            var chk = $('#grid_' + $gridCashline.name + '_records td[col="0"]').find('input[type="checkbox"]');
            for (var i = 0; i < chk.length; i++) {
                $(chk[i]).prop('checked', false);
                //$(chk[i]).change(e);
                $gridCashline.editChange.call($gridCashline, chk[i], i, 0, e);
                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                $gridCashline.trigger(eData);
            }
            selectedCashlines = [];
            $cashSelctAll.prop('checked', false);
            $srchCashJournal.val('');
            $cashPayType.val(0);
            $cfromDate.val('');
            $ctoDate.val('');
            loadUnallocatedCashLines();
        }
        //to clear the Payment Grid
        function clrPayment(e) {
            var chk = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]');
            for (var i = 0; i < chk.length; i++) {
                $(chk[i]).prop('checked', false);
                //$(chk[i]).change(e);
                $gridPayment.editChange.call($gridPayment, chk[i], i, 0, e);
                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                $gridPayment.trigger(eData);
            }
            selectedPayments = [];
            $paymentSelctAll.prop('checked', false);
            $payDocbaseType.val(0);
            $payDocType.val(0);
            $vPayMethod.setValue(null);
            $srchPayment.val('');
            $pfromDate.val('');
            $ptoDate.val('');
            loadUnallocatedPayments();
        }

        //to load Data in GL Grid
        function loadGLDataGrid(e) {

            if (!$glLineGrid) {
                loadGLGrid();
                $($($glLineGrid.box)[0]).find('.w2ui-search-down').css('margin-top', '4px');
                $($($glLineGrid.box)[0]).find('.w2ui-search-clear').css('margin-top', '4px');
                $('.vis-allocate-glSum').css('text-align', 'right');
                $('.vis-allocate-glSum').css('margin-right', '10px');
            }
            if (!$gridInvoice) {
                loadInvoice();
            }

            if ($vchkGlVoucher.is(':checked')) {
                $bsyDiv[0].style.visibility = "visible";
                readOnlyGL = false;
                //Set False to select all 
                $glSelectAll.prop('checked', false);
                $invSelectAll.prop('checked', false);
                $glLineGrid.refresh();
                loadGLVoucher();
            }
            else {
                $vchkAllocation.prop('checked', false);
                $vchkGlInvoice.prop('checked', false);
                $glLineGrid.clear();
                //$divChkGLInvoice.css('display', 'none');
                readOnlyGL = true;
                $glSelectAll.prop('checked', false);
                $invSelectAll.prop('checked', false);
                vchkapplocationChange();
                if ($gridInvoice) {
                    $gridInvoice.refresh();
                }
            }

            if ($gridInvoice) {
                var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
                for (var i = 0; i < chk.length; i++) {
                    $(chk[i]).prop('checked', $invSelectAll.prop("checked"));
                    //$(chk[i]).change(e);
                    $gridInvoice.editChange.call($gridInvoice, chk[i], i, 0, e);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridInvoice.trigger(eData);
                }
            }
            displayGrids($allocationFrom.val(), $allocationTo.val());
        };
        //to load all grids 
        function loadGrids(value) {
            if (value == "P") {
                if ($gridPayment) {
                    $gridPayment.refresh();
                }
            }
            else if (value == "C") {
                if ($gridCashline) {
                    $gridCashline.refresh();
                }
            }
            else if (value == "I") {
                if ($gridInvoice) {
                    $gridInvoice.refresh();
                }
            }
            else if (value == "G") {
                if ($glLineGrid) {
                    $glLineGrid.refresh();
                }
            }
        };
        // to display grids based on selected allocation From and TO
        function displayGrids(allocFrm, allocTo) {
            $row2.css('display', 'none'); // Payment Grid
            $row3.css('display', 'none');// Cash Grid
            $row4.css('display', 'none'); // Invoice Grid
            $row5.css('display', 'none');// GL journal grid
            readOnlyCash = true;
            readOnlyPayment = true;
            readOnlyGL = true;

            if (allocFrm == "P") {
                readOnlyPayment = false;
                $row2.css('display', 'block'); // Payment Grid
                if ($gridPayment) {
                    $gridPayment.clear();
                }
                //////loadUnallocatedPayments();
            }
            else if (allocFrm == "C") {
                readOnlyCash = false;
                $row3.css('display', 'block');// Cash Grid
                if ($gridCashline) {
                    $gridCashline.clear();
                }
                //////loadUnallocatedCashLines();
            }
            else if (allocFrm == "I") {
                $row4.css('display', 'block'); // Invoice Grid
                if ($gridInvoice) {
                    $gridInvoice.clear();
                }
                //////if ($gridInvoice) {
                //////    loadInvoice();
                //////}
            }
            else if (allocFrm == "G") {
                $row5.css('display', 'block');// GL journal grid
                //$bsyDiv[0].style.visibility = "visible";
                readOnlyGL = false;
                if ($glLineGrid) {
                    $glLineGrid.clear();
                }
                //////loadGLVoucher();
            }

            if (allocTo == "P") {
                readOnlyPayment = false;
                $row2.css('display', 'block'); // Payment Grid
                if ($gridPayment) {
                    $gridPayment.clear();
                }
                //////loadUnallocatedPayments();
            }
            else if (allocTo == "C") {
                readOnlyCash = false;
                $row3.css('display', 'block');// Cash Grid
                if ($gridCashline) {
                    $gridCashline.clear();
                }
                //////loadUnallocatedCashLines();
            }
            else if (allocTo == "I") {
                $row4.css('display', 'block'); // Invoice Grid
                if ($gridInvoice) {
                    $gridInvoice.clear();
                }
                ////////if ($gridInvoice) {
                ////////    loadInvoice();
                ////////}
            }
            else if (allocTo == "G") {
                $row5.css('display', 'block');// GL journal grid
                //$bsyDiv[0].style.visibility = "visible";
                readOnlyGL = false;
                if ($glLineGrid) {
                    $glLineGrid.clear();
                }
                ////////loadGLVoucher();
            }
        };
        //function to clear the selection of invoices
        function clearInvoiceArrays(e) {
            if ($gridInvoice != null) {
                var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
                for (var i = 0; i < chk.length; i++) {
                    $(chk[i]).prop('checked', false);
                    //$(chk[i]).change(e);
                    $gridInvoice.editChange.call($gridInvoice, chk[i], i, 0, e);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridInvoice.trigger(eData);
                }
                selectedInvoices = [];
            }
        };
        //end

        function fillLookups() {
            var data = null;
            //var value = VIS.MLookupFactory.getMLookUp(ctx, self.windowNo, 3505, VIS.DisplayType.TableDir);
            var value = VIS.MLookupFactory.get(ctx, self.windowNo, 0, VIS.DisplayType.TableDir, "C_Currency_ID", 0, false, "IsMyCurrency='Y'"); //shown the records only IsMycurrency is true.
            data = value.getData(true, true, false, false);
            var flag = 0;
            if (data != null && data != undefined && data.length > 0) {
                for (var i = 0; i < data.length; i++) {
                    $cmbCurrency.append('<option value=' + data[i].Key + '>' + data[i].Name + '</option>');
                    if (data[i].Key == _C_Currency_ID) {
                        $cmbCurrency.val(_C_Currency_ID);
                        flag = 1;
                    }
                }
                if (flag == 0) {
                    $cmbCurrency.val(0);
                    $cmbCurrency.addClass('vis-ev-col-mandatory');
                }
            }

            value = VIS.MLookupFactory.getMLookUp(ctx, self.windowNo, 3499, VIS.DisplayType.Search);
            $vSearchBPartner = new VIS.Controls.VTextBoxButton("C_BPartner_ID", true, false, true, VIS.DisplayType.Search, value);

            //VIS_0045: create and assin control to conversion type
            value = VIS.MLookupFactory.get(ctx, self.windowNo, 10294, VIS.DisplayType.TableDir, "C_ConversionType_ID", 0, false,
                "C_ConversionType.IsActive = 'Y' AND C_ConversionType.AD_Org_ID IN (0, @OrgID@)");
            $vConversionType = new VIS.Controls.VComboBox("C_ConversionType_ID", false, false, true, value, 50);

            value = VIS.MLookupFactory.get(ctx, self.windowNo, 0, VIS.DisplayType.TableDir, "VA009_PaymentMethod_ID", 0, false,
                "VA009_PAYMENTMETHOD.VA009_PAYMENTMETHOD_ID IN (SELECT VA009_PAYMENTMETHOD_ID FROM VA009_PAYMENTMETHOD WHERE VA009_PAYMENTBASETYPE NOT IN ('B','C','P') )");
            $vPayMethod = new VIS.Controls.VComboBox("VA009_PaymentMethod_ID", false, false, true, value, 50);

            value = VIS.MLookupFactory.get(ctx, self.windowNo, 0, VIS.DisplayType.TableDir, "VA009_PaymentMethod_ID", 0, false,
                "VA009_PAYMENTMETHOD.IsActive='Y' AND VA009_PAYMENTMETHOD.AD_Client_ID = @AD_Client_ID@");
            $vInvPayMethod = new VIS.Controls.VComboBox("VA009_PaymentMethod_ID", false, false, true, value, 50);
        };

        // Set Mandatory and non mandatory---Neha
        function SetMandatory(Value) {
            if (Value)
                return '#FFB6C1';
            else
                return 'White';
        };

        //load PaymentType Dropdown for filter the records in Cash Journal Line.
        function loadPaymentType() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "PaymentAllocation/GetPaymentType",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $cashPayType.empty();
                        //removed static text suggested by ashish
                        $cashPayType.append("<option value='0'></option>");
                        for (var i = 0; i < result.length; i++) {
                            paymentType = VIS.Utility.Util.getValueOfString(result[i].Name);
                            paymentType_ID = VIS.Utility.Util.getValueOfString(result[i].Value);
                            $cashPayType.append(" <option value=" + paymentType_ID + ">" + paymentType + "</option>");
                        }
                    }
                    else {
                        //removed static text suggested by ashsih
                        $cashPayType.append("<option value= '0'></option>");
                    }
                    $cashPayType.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };

        //load payDocbaseType to filter the records in the invoice grid
        function loadinvDocbaseType() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "PaymentAllocation/GetDocbaseType",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $invDocbaseType.empty();
                        $invDocbaseType.append("<option value='0'></option>");
                        for (var i = 0; i < result.length; i++) {
                            docBaseType = VIS.Utility.Util.getValueOfString(result[i].DocbaseType);
                            docBaseName = VIS.Utility.Util.getValueOfString(result[i].Name);
                            $invDocbaseType.append(" <option value=" + docBaseType + ">" + docBaseName + "</option>");
                        }
                    }
                    else {
                        $invDocbaseType.append("<option value= '0'></option>");
                    }
                    $invDocbaseType.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };
        //load payDocbaseType to filter the records in the Payment grid
        function loadpayDocbaseType() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "PaymentAllocation/GetpayDocbaseType",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $payDocbaseType.empty();
                        $payDocbaseType.append("<option value='0'></option>");
                        for (var i = 0; i < result.length; i++) {
                            docbaseType = VIS.Utility.Util.getValueOfString(result[i].DocbaseType);
                            docBaseName = VIS.Utility.Util.getValueOfString(result[i].Name);
                            $payDocbaseType.append(" <option value=" + docbaseType + ">" + docBaseName + "</option>");
                        }
                    }
                    else {
                        $payDocbaseType.append("<option value='0'></option>");
                    }
                    $payDocbaseType.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };
        //load payDocType for payment grid to filter the records.
        function loadpayDocType() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "PaymentAllocation/GetpayDocType",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $payDocType.empty();
                        $payDocType.append("<option value='0'></option>");
                        for (var i = 0; i < result.length; i++) {
                            docType = VIS.Utility.Util.getValueOfString(result[i].DocType);
                            c_DocType_ID = VIS.Utility.Util.getValueOfInt(result[i].C_DocType_ID);
                            $payDocType.append(" <option value=" + c_DocType_ID + ">" + docType + "</option>");
                        }
                    }
                    else {
                        $payDocType.append("<option value= 0></option>");
                    }
                    $payDocType.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };
        //-------Load Document Type Grid-----------Neha------------
        function loadDocType() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "PaymentAllocation/GetDocType",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $cmbDocType.empty();
                        $cmbDocType.append("<option value= 0 ></option>");
                        for (var i = 0; i < result.length; i++) {
                            docType = VIS.Utility.Util.getValueOfString(result[i].DocType);
                            c_DocType_ID = VIS.Utility.Util.getValueOfInt(result[i].C_DocType_ID);
                            $cmbDocType.append(" <option value=" + c_DocType_ID + ">" + docType + "</option>");
                        }
                    }
                    else {
                        $cmbDocType.append("<option value= 0 ></option>");
                    }
                    $cmbDocType.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };

        //-----Load Invoice Grid----Neha
        function loadInvoice() {
            if (_C_BPartner_ID > 0) {
                AD_Org_ID = $OrgFilter.val();
                _C_Currency_ID = $cmbCurrency.val();
                if (_C_Currency_ID == 0) {
                    return VIS.ADialog.info("Message", true, "select currency", "");
                }
                fromDate = $fromDate.val();
                toDate = $toDate.val();
                //---If ToDate is less than From Date--Display error message-----Neha
                if (toDate != "") {
                    if (fromDate > toDate) {
                        $toDate.val('');
                        return VIS.ADialog.info("Message", true, VIS.Msg.getMsg("VIS_FromDateGreater"), "");
                    }
                }
                $bsyDiv[0].style.visibility = "visible";
                pageNoInvoice = 1, gridPgnoInvoice = 1, invoiceRecord = 0;
                isInvoiceGridLoaded = false;
                C_ConversionType_ID = 0;
                var chk = $vchkMultiCurrency.is(':checked');
                var getDate = null;
                date = $date.val();
                if (date != null && date != undefined && date != "") {
                    getDate = VIS.DB.to_date(date);
                }
                else
                    getDate = VIS.DB.to_date(new Date());
                //---Get values and pass values to the controller----Neha
                docNo = $txtDocNo.val();
                c_docType_ID = $cmbDocType.val();
                docBaseType = $invDocbaseType.val();
                payMethod_ID = VIS.Utility.Util.getValueOfInt($vInvPayMethod.getValue());
                srchText = $srchInvoice.val();
                if (fromDate != null && fromDate != undefined && fromDate != "") {
                    VIS.DB.to_date(fromDate);
                }
                if (toDate != null && toDate != undefined && toDate != "") {
                    VIS.DB.to_date(toDate);
                }

                $.ajax({
                    url: VIS.Application.contextUrl + "PaymentAllocation/GetInvoice",
                    data: {
                        AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID,
                        isInterBPartner: _isInterBPartner, chk: chk, date: getDate, page: pageNoInvoice,
                        size: PAGESIZE, docNo: docNo, c_docType_ID: c_docType_ID, docBaseType: docBaseType,
                        PaymentMethod_ID: payMethod_ID, fromDate: fromDate, toDate: toDate, conversionDate: conversionDate,
                        srchText: srchText, isInterComp: _isInterCompany, conversionType_ID: $vConversionType.getValue()
                    },
                    async: true,
                    success: function (result) {
                        var data = JSON.parse(result);
                        if (data) {
                            if (pageNoInvoice == 1 && data.length > 0) {
                                invoiceRecord = data[0].InvoiceRecord;
                                gridPgnoInvoice = Math.ceil(invoiceRecord / PAGESIZE);
                            }

                            //if any invoice is selected and we load another invoice from db than we have to check weather that invoice is already selected or not. if already selected tha skip that.
                            if (selectedInvoices.length > 0) {
                                totalselectedinv = selectedInvoices.length;
                                for (var i = 0; i < data.length; i++) {

                                    var filterObj = selectedInvoices.filter(function (e) {
                                        return e.C_InvoicePaySchedule_ID == data[i]["C_InvoicePaySchedule_ID"];
                                    });

                                    if (filterObj.length == 0) {
                                        data[i]["recid"] = selectedInvoices.length + i;
                                        selectedInvoices.push(data[i]);
                                    }
                                }
                                data = selectedInvoices;
                                // Clear Selected invoices array when we de-select the select all checkbox. work done for to hold all the selected invoices
                                selectedInvoices = [];
                            }
                            //end

                            bindInvoiceGrid(data, chk);
                            //Amount
                            _openInv = 9;
                            // _discount = chk ? 7 : 5;
                            _discount = 10;
                            _writeOff = 13;
                            _applied = 14;
                            //added window no to find grid to implement scroll  issue : scroll was not working properly
                            $divInvoice.find('#grid_openformatgridinvoice_' + $self.windowNo + '_records').on('scroll', cartInvoiceScroll);
                            isInvoiceGridLoaded = true;
                            isBusyIndicatorHidden();
                        }
                    },
                    error: function (err) {
                        $bsyDiv[0].style.visibility = "hidden";
                    }
                });
            }
        };

        //----Laod Comobo Allocation From and Allocation To
        function loadAllocationCmb() {
            $allocationFrom.empty();
            $allocationTo.empty();
            $allocationFrom.append("<option value=0></option>");
            $allocationTo.append("<option value=0></option>");
            $allocationFrom.append('<option value="C">' + VIS.translatedTexts.C_Cash_ID + '</option>');
            $allocationTo.append('<option value="C">' + VIS.translatedTexts.C_Cash_ID + '</option>');
            $allocationFrom.append('<option value="I">' + VIS.translatedTexts.C_Invoice_ID + '</option>');
            $allocationTo.append('<option value="I">' + VIS.translatedTexts.C_Invoice_ID + '</option>');
            $allocationFrom.append('<option value="G">' + VIS.translatedTexts.GL_Journal_ID + '</option>');
            $allocationTo.append('<option value="G">' + VIS.translatedTexts.GL_Journal_ID + '</option>');
            $allocationFrom.append('<option value="P">' + VIS.translatedTexts.C_Payment_ID + '</option>');
            $allocationTo.append('<option value="P">' + VIS.translatedTexts.C_Payment_ID + '</option>');
        };

        function createRow1() {
            //to do design
            var $divOrg = $('<div class="vis-allocation-leftControls">');

            $divOrg.append('<div class="input-group vis-input-wrap"><div class="vis-control-wrap"><select class="vis-allocation-currencycmb" id=VIS_Org_' + $self.windowNo + '></select><label title="View organization" >' + VIS.translatedTexts.AD_Org_ID + '</label>');
            var $divBp = $('<div class="vis-allocation-leftControls">');

            var $Leftformfieldwrp = $('<div class="input-group vis-input-wrap">');
            var $Leftformfieldctrlwrp = $('<div class="vis-control-wrap">');
            var $Leftformfieldbtnwrap = $('<div class="input-group-append">');
            $divBp.append($Leftformfieldwrp);
            $Leftformfieldwrp.append($Leftformfieldctrlwrp);
            $Leftformfieldwrp.append($Leftformfieldbtnwrap);
            $Leftformfieldctrlwrp.append($vSearchBPartner.getControl().addClass("vis-allocation-bpartner").attr('data-placeholder', '').attr('placeholder', ' ').attr('data-hasbtn', ' ')).append('<label>' + VIS.translatedTexts.C_BPartner_ID + '</label>');
            $Leftformfieldbtnwrap.append($vSearchBPartner.getBtn(0));

            var $divCu = $('<div class="vis-allocation-leftControls">');
            var $Leftformfieldwrp = $('<div class="input-group vis-input-wrap">');
            var $Leftformfieldctrlwrp = $('<div class="vis-control-wrap">');
            $divCu.append($Leftformfieldwrp);
            $Leftformfieldwrp.append($Leftformfieldctrlwrp);
            $Leftformfieldctrlwrp.append($cmbCurrency).append('<label>' + VIS.translatedTexts.C_Currency_ID + '</label>');
            $innerRow.append($divOrg).append($divBp).append($divCu);

            //added for sequence of multicurrency checkbox
            var $multiCurr = $('<div class="vis-allocation-leftControls">'
                + '<div class="input-group vis-input-wrap">'
                + '<div class="vis-control-wrap">'
                + '<label class="vis-ec-col-lblchkbox">'
                + '<input name="vchkMultiCurrency" class="vis-allocation-multicheckbox" type="checkbox">'
                + VIS.Msg.getMsg("MultiCurrency")
                + '</label>'
                + '</div></div></div>'
                + '<div class="vis-allocation-leftControls" id = VIS_cnvrDateDiv_' + $self.windowNo + ' >'
                + '<div class="input-group vis-input-wrap">'
                + '<div class="vis-control-wrap">'
                + '<input  class="vis-allocation-date" style="display:block;" id=VIS_cmbConversionDate_' + $self.windowNo + ' type="date" max="9999-12-31">'
                + '<label title="View allocation will be created on this date" type="date" >' + VIS.Msg.getMsg("ConversionDate") + '</label>'
                + '</div></div></div>');
            $innerRow.append($multiCurr);

            // VIS_0045: Add Currency Type
            $divCu = $('<div class="vis-allocation-leftControls vis-conversionType" style="display:none">');
            $Leftformfieldwrp = $('<div class="input-group vis-input-wrap">');
            $Leftformfieldctrlwrp = $('<div class="vis-control-wrap">');
            $divCu.append($Leftformfieldwrp);
            $Leftformfieldwrp.append($Leftformfieldctrlwrp);
            $Leftformfieldctrlwrp.append($vConversionType.getControl()).append('<label>' + VIS.translatedTexts.C_ConversionType_ID + '</label>');
            $innerRow.append($divCu);
            //end

            //added for enhancement of new combo regarding allocation from and to
            var $divallocFrom = $('<div class="vis-allocation-leftControls">');
            var $Leftformfieldwrp = $('<div class="input-group vis-input-wrap">');
            var $Leftformfieldctrlwrp = $('<div class="vis-control-wrap">');
            $divallocFrom.append($Leftformfieldwrp);
            $Leftformfieldwrp.append($Leftformfieldctrlwrp);
            $Leftformfieldctrlwrp.append($allocationFrom).append('<label> ' + VIS.Msg.getMsg("AllocationFrom") + '</label>');

            var $divallocTo = $('<div class="vis-allocation-leftControls">');
            var $Leftformfieldwrp = $('<div class="input-group vis-input-wrap">');
            var $Leftformfieldctrlwrp = $('<div class="vis-control-wrap">');
            $divallocTo.append($Leftformfieldwrp);
            $Leftformfieldwrp.append($Leftformfieldctrlwrp);
            $Leftformfieldctrlwrp.append($allocationTo).append('<label> ' + VIS.Msg.getMsg("AllocationTo") + '</label>');
            $innerRow.append($divallocFrom).append($divallocTo);
            //end

            var $rowOne = $('<div class="vis-allocation-leftControls" id="cashMaindiv"  style="display: none !important;">'
                + '<input  class="vis-allocation-cashbox"  type="checkbox">'
                + '<label>' + VIS.Msg.getMsg("Cash") + '</label>'
                + '</div>'
                //Added checkbox for gl-allocation
                + '<div class="vis-allocation-leftControls" id="glMaindiv" style="display: none !important;">'
                + '<input  class="vis-allocation-glvoucher"  type="checkbox">'
                + '<label>' + VIS.Msg.getMsg("GL Voucher") + '</label>'
                + '</div>'

                // + '<div class="vis-allocation-leftControls vis-allocation-glinvoiceDiv" style="display: none !important;">'
                // + '<input  class="vis-allocation-glinvoice"  type="checkbox">'
                // + '<label>' + VIS.translatedTexts.C_Invoice_ID + '</label>'
                // + '</div>'

                //end
                //+ '<div class="vis-allocation-leftControls">'
                //+ '<input  class="vis-allocation-autowriteoff"  type="checkbox">'
                //+ '<label>' + VIS.Msg.getMsg("AutoWriteOff") + '</label>'
                //+ '</div>'
                //inter company checkbox and label added 
                + '<div class="vis-allocation-leftControls"> <div class= "input-group vis-input-wrap" > <div class="vis-control-wrap"> '
                + '<label class="vis-ec-col-lblchkbox">'
                + '<input id=VIS_chkbxICompanyAllocation_' + $self.windowNo + ' class="vis-allocation-interComp" type="checkbox">'
                + VIS.Msg.getMsg("InterCompanyAllocation")
                + '</label>'
                + '</div> </div> </div>'

                + '<div class="vis-allocation-leftControls"> <div class= "input-group vis-input-wrap" > <div class="vis-control-wrap">'
                + '<label class="vis-ec-col-lblchkbox">'
                + '<input id=VIS_chkbxBPAllocation_' + $self.windowNo + ' class="vis-allocation-interBP" type="checkbox">'
                + VIS.Msg.getMsg("BPAllocation")
                + '</label>'
                + '</div></div> </div>'
                //---------------------------Added new parameters----Neha----3 August 2018---Asked by Amit
                //+ '<div class="vis-allocation-leftControls">'
                //+ '<div class="panel-group" style="margin: 0px;" id="accordion" role="tablist" aria-multiselectable="true">'
                //+ '<div class="panel panel-default">'
                //+ '<div class="panel-heading" role="tab" id="headingOne">'
                //+ '<h4 class="panel-title">'
                //+ '<a role="button" data-toggle="collapse" data-parent="#accordion" href="#collapseOne" aria-expanded="true" aria-controls="collapseOne" class="VIS-Accordion-head collapsed"><span>' + VIS.Msg.getMsg("InvoiceFilter")
                //+ '</span><i class="glyphicon glyphicon-chevron-down pull-right"></i>'
                //+ '</a>'
                //+ '</h4>'
                //+ '</div>'
                //+ '<div id="collapseOne" class="panel-collapse collapse" role="tabpanel" aria-labelledby="headingOne" style="height: 0px;">'
                //+ '<div class="panel-body" style=" overflow: auto; height: 140px !important; ">'
                //+ '<div class="vis-allocation-leftControls">'
                //+ '<span class="vis-allocation-inputLabels">' + VIS.Utility.encodeText(VIS.Msg.getMsg("Document_No")) + '</span>'
                //+ ' <input class="vis-allocation-docNo" type="textbox"></input>'
                //+ '</div>'
                //+ '<div class="vis-allocation-leftControls vis-allocation-cmbdoctype">'
                //+ '<span class="vis-allocation-inputLabels">' + VIS.Msg.getMsg("DocType") + '</span>'
                //+ '</div>'
                //+ '</div>'
                //+ '</div>'
                //+ '</div>'
                //+ '</div>'
                //+ '</div>'
                //+ '</div>'
            );
            $innerRow.append($rowOne);
            //$rowOne.find(".vis-allocation-cmbdoctype .vis-control-wrap").append($cmbDocType).append('<label>' + VIS.Msg.getMsg("DocType") + '</label>');
            $rowOne.find(".panel-body").append('<div class="vis-allocation-leftControls">'
                //+ '<div class="input-group vis-input-wrap">'
                //+ '<div class="vis-control-wrap">'
                //+ '<input  class="vis-allocation-fromDate"  type="date">'
                //+ '<label>' + VIS.Msg.getMsg("VIS_FromDate") + '</label>'
                //+ '</div></div></div>'
                + '<div class="vis-allocation-leftControls">'
                + '<div class="input-group vis-input-wrap">'
                + '<div class="vis-control-wrap">'
                + '<input  class="vis-allocation-toDate"  type="date">'
                + '<label>' + VIS.Msg.getMsg("VIS_ToDate") + '</label>'
                + '</div></div></div>');
            //$divBp.find(".vis-allocation-bpartner").css('width', 'calc(100% - 30px)');
            //-----------Neha-----------------
            $vchkMultiCurrency = $innerRow.find('.vis-allocation-multicheckbox');
            $vchkAllocation = $innerRow.find('.vis-allocation-cashbox');
            //Added for gl-allocation
            $vchkGlVoucher = $innerRow.find('.vis-allocation-glvoucher');
            $vchkGlInvoice = $innerRow.find('.vis-allocation-glinvoice');
            //$divChkGLInvoice = $innerRow.find('.vis-allocation-glinvoiceDiv');
            //end
            //$vchkAutoWriteOff = $innerRow.find('.vis-allocation-autowriteoff');

            //-----get controls values------------------
            $txtDocNo = $innerRow.find('.vis-allocation-docNo');
            //$fromDate = $innerRow.find('.vis-allocation-fromDate');
            //$toDate = $innerRow.find('.vis-allocation-toDate');
            //------------------------------------------
            var $resultDiv = $('<div class="vis-allocation-resultdiv">');
            //$resultDiv.append('<div class="vis-allocation-leftControls">' +
            //    '<span class="vis-allocation-inputLabels">' + VIS.Msg.getMsg("Difference")
            //    + '</span>');
            //$resultDiv.append('<div class="vis-allocation-leftControls" style=" margin-bottom: 10px; ">'
            //    + '<span class="vis-allocation-lblCurrnecy"></span>'
            //    + '<span class="vis-allocation-lbldifferenceAmt" style="float:right;">'
            //    + '</span>');

            $resultDiv.append('<div class="vis-allocation-leftControls"><div class="input-group vis-input-wrap"><div class="vis-control-wrap"> <select class="vis-allocation-currencycmb" id=VIS_cmbOrg_' + $self.windowNo + '></select><label title=' + VIS.Msg.getMsg("VIS_AllocOrgTitle") + '>' + VIS.Msg.getMsg("VIS_AllocOrg") + '</label>');

            $resultDiv.append('<div class="vis-allocation-leftControls">'
                + '<div class="input-group vis-input-wrap">'
                + '<div class="vis-control-wrap">'
                + '<input  class="vis-allocation-date" disabled  id=VIS_cmbDate_' + $self.windowNo + ' type="date">'
                + '<label title="View allocation will be created on this date" type="date" >' + VIS.Msg.getMsg("TransactionDate") + '</label>'
                + '</div></div></div>');
            $resultDiv.append('<div class="vis-allocation-leftControls">'
                + '<div class="input-group vis-input-wrap">'
                + '<div class="vis-control-wrap">'
                + '<input  class="vis-allocation-date" disabled id=VIS_cmbAcctDate_' + $self.windowNo + ' type="date">'
                + '<label title="View allocation will be created on this date" type="date" >' + VIS.Msg.getMsg("DateAcct") + '</label>'
                + '</div></div></div>');


            $innerRow.append($resultDiv);
            $date = $innerRow.find('#VIS_cmbDate_' + $self.windowNo);
            $date.val(Globalize.format(new Date(), "yyyy-MM-dd"));
            $dateAcct = $innerRow.find('#VIS_cmbAcctDate_' + $self.windowNo);
            $dateAcct.val(Globalize.format(new Date(), "yyyy-MM-dd"));
            $conversionDate = $innerRow.find('#VIS_cmbConversionDate_' + $self.windowNo);
            $conversionDiv = $innerRow.find('#VIS_cnvrDateDiv_' + $self.windowNo);
            $conversionDiv.css('display', 'none');
            $cmbOrg = $innerRow.find('#VIS_cmbOrg_' + $self.windowNo);
            $OrgFilter = $innerRow.find('#VIS_Org_' + $self.windowNo);
            $vchkBPAllocation = $innerRow.find('#VIS_chkbxBPAllocation_' + $self.windowNo);
            $vchICompanyAllocation = $innerRow.find('#VIS_chkbxICompanyAllocation_' + $self.windowNo);
        };

        function createRow2() {
            $row2.append('<div class="d-flex doc-allocation"><div class="vis-doc-AllocOuter-wrap"><div><p>' + VIS.translatedTexts.C_Payment_ID
                + '</p>  <input type="checkbox" id="paymentselectall" /><p style="margin: 0 4px;"></p> <span id="pclrbutton_' + $self.windowNo
                + '" style="cursor: pointer;margin: 2px 0 0;" class="glyphicon glyphicon-refresh" title=' + VIS.Msg.getMsg("Reset")
                + '></span></div><p class="vis-allocate-paymentSum"> 0 ' + VIS.Msg.getMsg("SelectedLines") + ' - ' + summation
                + ' 0.00</p></div> <div class="vis-allocation-topFields-wrap"><div class="input-group vis-input-wrap"><div class="vis-control-wrap"><input placeholder="'
                + VIS.Msg.getMsg("SearchResults") + '" data-hasbtn=" " type="text" id="paySrch' + $self.windowNo + '" /><label>' + VIS.Msg.getMsg("SearchResults")
                + '</label></div><div class="input-group-append"><a class="input-group-text" id="_SrchpBtn_' + $self.windowNo
                + '"><span class="glyphicon glyphicon-search"></span></a></div></div><div class="input-group vis-input-wrap" id="docType_' + $self.windowNo
                + '"></div><div class="payBaseType input-group vis-input-wrap" id="docbaseType_' + $self.windowNo + '"></div>'
                + '<div class="payMethod input-group vis-input-wrap" id="payPayMethod_' + $self.windowNo + '"></div>'
                + '<div class="vis-fdate-allocation input-group vis-input-wrap"></div><div class="vis-tdate-allocation input-group vis-input-wrap"></div></div></div> ')
                .append(' <div class= "vis-allocation-payment-grid" ></div> ');//.append(' < p class= "vis-allocate-paymentSum" > 0 - Sum 0.00</p > ');
            $divPayment = $row2.find('.vis-allocation-payment-grid');
            $lblPaymentSum = $row2.find('.vis-allocate-paymentSum');
            $paymentSelctAll = $row2.find('#paymentselectall');
            $srchPayment = $row2.find("#paySrch" + $self.windowNo + '');
            $srchbtnPayment = $row2.find("#_SrchpBtn_" + $self.windowNo + '');
            $row2.find("#docType_" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($payDocType).append('<label>' + VIS.translatedTexts.DocType + '</label>'));
            $row2.find("#docbaseType_" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($payDocbaseType).append('<label>' + VIS.translatedTexts.DocBaseType + '</label>'));
            $row2.find("#payPayMethod_" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($vPayMethod.getControl()).append('<label>' + VIS.translatedTexts.PaymentMethod + '</label>'));
            $row2.find(".vis-fdate-allocation").append($('<div class="vis-control-wrap">').append($pfromDate).append('<label>' + VIS.translatedTexts.FromDate + '</label>'));
            $row2.find(".vis-tdate-allocation").append($('<div class="vis-control-wrap">').append($ptoDate).append('<label>' + VIS.Msg.getMsg("ToDate") + '</label>'));
            //get control of clear button
            $payclrbtn = $row2.find('#pclrbutton_' + $self.windowNo);
        };

        function createRow3() {
            $row3.append('<div class="d-flex cash-allocation"><div class="vis-doc-AllocOuter-wrap"><div><p>' + VIS.translatedTexts.C_CashLine_ID + '</p> <input type="checkbox" id="cashselectall" /><p style="margin: 0 4px;"></p> <span id="cclrbutton_' + $self.windowNo + '" style="cursor: pointer;margin: 2px 0 0;" class="glyphicon glyphicon-refresh"  title=' + VIS.Msg.getMsg("Reset") + '></span></div><p class="vis-allocate-cashSum"> 0 ' + VIS.Msg.getMsg("SelectedLines") + ' - ' + summation + ' 0.00</p></div><div class="vis-allocation-topFields-wrap"><div class="input-group vis-input-wrap"><div class="vis-control-wrap"><input placeholder="' + VIS.Msg.getMsg("SearchResults") + '" data-hasbtn=" " type="text" id="cashSrch' + $self.windowNo + '" /><label>' + VIS.Msg.getMsg("SearchResults") + '</label></div><div class="input-group-append"><a class="input-group-text" id="_SrchcBtn_' + $self.windowNo + '"><span class="glyphicon glyphicon-search"></span></a></div></div><div class="input-group vis-input-wrap payType" id="_paymentType' + $self.windowNo + '"></div><div class="input-group vis-input-wrap vis-fdate-allocation"></div><div class="input-group vis-input-wrap vis-tdate-allocation"></div></div></div>').append('<div  class="vis-allocation-cashLine-grid"></div>');//.append('<p class="vis-allocate-cashSum">0-Sum 0.00</p>');
            $divCashline = $row3.find('.vis-allocation-cashLine-grid');
            $lblCashSum = $row3.find('.vis-allocate-cashSum');
            $cashSelctAll = $row3.find('#cashselectall');
            $srchCashJournal = $row3.find("#cashSrch" + $self.windowNo + '');
            $srchbtnCashJournal = $row3.find("#_SrchcBtn_" + $self.windowNo + '');
            $row3.find("#_paymentType" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($cashPayType).append('<label>' + VIS.translatedTexts.Payment_Type + '</label>'));
            $row3.find(".vis-fdate-allocation").append($('<div class="vis-control-wrap">').append($cfromDate).append('<label>' + VIS.translatedTexts.FromDate + '</label>'));
            $row3.find(".vis-tdate-allocation").append($('<div class="vis-control-wrap">').append($ctoDate).append('<label>' + VIS.Msg.getMsg("ToDate") + '</label>'));
            //get control of clear button
            $cashclrbtn = $row3.find('#cclrbutton_' + $self.windowNo);
        };

        function createRow4() {
            //
            $row4.append('<div class="d-flex in-allocation"><div class="vis-doc-AllocOuter-wrap"><div><span>' + VIS.translatedTexts.C_Invoice_ID
                + '</span> <input type="checkbox" id="invoiceselectall" /><span style="margin: 0 4px;"></span> <span id="clrbutton_' + $self.windowNo
                + '" style="cursor: pointer;margin: 2px 0 0;" class="glyphicon glyphicon-refresh"  title=' + VIS.Msg.getMsg("Reset")
                + '></span></div><p class="vis-allocate-invoiceSum"> 0' + VIS.Msg.getMsg("SelectedLines") + ' - ' + summation
                + ' 0.00</p></div><div class="vis-allocation-topFields-wrap"><div class="input-group vis-input-wrap"><div class="vis-control-wrap"><input placeholder="'
                + VIS.Msg.getMsg("SearchResults") + '" data-hasbtn=" " type="text" id="invSrch' + $self.windowNo + '" /><label>' + VIS.Msg.getMsg("SearchResults")
                + '</label></div><div class="input-group-append"><a class="input-group-text" id="_SrchBtn_' + $self.windowNo
                + '"><span class="glyphicon glyphicon-search"></span></a></div></div><div class="input-group vis-input-wrap" id="docType' + $self.windowNo
                + '"></div><div class="invBaseType input-group vis-input-wrap" id="invbaseType_' + $self.windowNo + '"></div>'
                + '<div class="payMethod input-group vis-input-wrap" id="invPayMethod_' + $self.windowNo + '"></div>'
                + '<div class="vis-fdate-allocation input-group vis-input-wrap"></div><div class="vis-tdate-allocation input-group vis-input-wrap"></div></div></div>')
                .append('<div  class="vis-allocation-invoice-grid"></div>');//.append('<p class="vis-allocate-invoiceSum">0-Sum 0.00</p>');
            $divInvoice = $row4.find('.vis-allocation-invoice-grid');
            $lblInvoiceSum = $row4.find('.vis-allocate-invoiceSum');
            $invSelectAll = $row4.find('#invoiceselectall');
            $srchInvoice = $row4.find("#invSrch" + $self.windowNo + '');
            $srchbtnInvoice = $row4.find("#_SrchBtn_" + $self.windowNo + '');
            $row4.find("#docType" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($cmbDocType).append('<label>' + VIS.translatedTexts.DocType + '</label>'));
            $row4.find("#invbaseType_" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($invDocbaseType).append('<label>' + VIS.translatedTexts.DocBaseType + '</label>'));
            $row4.find("#invPayMethod_" + $self.windowNo + '').append($('<div class="vis-control-wrap">').append($vInvPayMethod.getControl()).append('<label>' + VIS.translatedTexts.PaymentMethod + '</label>'));
            $row4.find(".vis-fdate-allocation").append($('<div class="vis-control-wrap">').append($fromDate).append('<label>' + VIS.translatedTexts.FromDate + '</label>'));
            $row4.find(".vis-tdate-allocation").append($('<div class="vis-control-wrap">').append($toDate).append('<label>' + VIS.Msg.getMsg("ToDate") + '</label>'));
            //get control of clear button
            $clrbtn = $row4.find('#clrbutton_' + $self.windowNo);
        };

        //added grid design for gl-allocation
        function createRow5() {
            $row5.append('<div class="d-flex  gl-allocation"><div class="vis-doc-AllocOuter-wrap"><div><span>' + VIS.translatedTexts.GL_Journal_ID + '</span> <input type="checkbox" id="glselectall" /><span style="margin: 0 4px;"></span> <span id="gclrbutton_' + $self.windowNo + '" style="cursor: pointer;margin: 2px 0 0;" class="glyphicon glyphicon-refresh"  title=' + VIS.Msg.getMsg("Reset") + '></span></div><p class="vis-allocate-glSum"> 0' + VIS.Msg.getMsg("SelectedLines") + ' - ' + summation + ' 0.00</p></div><div class="vis-allocation-topFields-wrap"><div class="input-group vis-input-wrap"><div class="vis-control-wrap"><input placeholder="' + VIS.Msg.getMsg("SearchResults") + '" data-hasbtn=" " type="text" id="glSrch' + $self.windowNo + '" /><label>' + VIS.Msg.getMsg("SearchResults") + '</label></div><div class="input-group-append"><a class="input-group-text" id="_SrchglBtn_' + $self.windowNo + '"><span class="glyphicon glyphicon-search"></span></a></div></div><div class="input-group vis-input-wrap vis-fdate-allocation"></div><div class="input-group vis-input-wrap vis-tdate-allocation"></div></div></div>').append('<div  class="vis-allocation-gl-grid"></div>');
            $divGl = $row5.find('.vis-allocation-gl-grid');
            $lblglSum = $row5.find('.vis-allocate-glSum');
            $glSelectAll = $row5.find('#glselectall');
            $srchGL = $row5.find("#glSrch" + $self.windowNo + '');
            $srchbtnGL = $row5.find("#_SrchglBtn_" + $self.windowNo + '');
            $row5.find(".vis-fdate-allocation").append($('<div class="vis-control-wrap">').append($gfromDate).append('<label>' + VIS.translatedTexts.FromDate + '</label>'));
            $row5.find(".vis-tdate-allocation").append($('<div class="vis-control-wrap">').append($gtoDate).append('<label>' + VIS.Msg.getMsg("ToDate") + '</label>'));
            //get control of clear button
            $glclrbtn = $row5.find('#gclrbutton_' + $self.windowNo);
        };
        //end

        //get Organization Filter in left panel
        function loadOrg() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "VIS/PaymentAllocation/GetOrg",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $OrgFilter.empty();
                        //$OrgFilter.addClass('vis-ev-col-mandatory');
                        //$OrgFilter.addClass('vis-ev-col-mandatory');
                        if (VIS.Env.getCtx().getAD_Org_ID() != 0) {
                            $OrgFilter.append("<option value=" + VIS.Env.getCtx().getAD_Org_ID() + ">" + VIS.Env.getCtx().ctx["#AD_Org_Name"] + "</option>");
                        }
                        for (var i = 0; i < result.length; i++) {
                            if (result[i].Value != VIS.Env.getCtx().getAD_Org_ID()) {
                                OrgName = VIS.Utility.Util.getValueOfString(result[i].Name);
                                AD_Org_ID = VIS.Utility.Util.getValueOfInt(result[i].Value);
                                $OrgFilter.append(" <option value=" + AD_Org_ID + ">" + OrgName + "</option>");
                            }
                        }
                    }
                    else {
                        $OrgFilter.append("<option value=0></option>");
                    }
                    if (VIS.context.getContextAsInt("#AD_Org_ID") > 0) {
                        $OrgFilter.val(VIS.context.getContextAsInt("#AD_Org_ID"));
                        //$OrgFilter.removeClass('vis-ev-col-mandatory');
                        $OrgFilter.removeClass('vis-ev-col-mandatory');
                    }
                    else
                        $OrgFilter.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };
        /**
        *To get all the  Organization which are accessable by login user
        *@alias    loadCurrency
        *@return {list} list of Organization Name and ID.
        */
        function loadOrganization() {
            $.ajax({
                type: "POST",
                url: VIS.Application.contextUrl + "VIS/PaymentAllocation/GetOrganization",
                dataType: "json",
                success: function (data) {
                    var result = JSON.parse(data);
                    if (result) {
                        $cmbOrg.empty();
                        $cmbOrg.addClass('vis-ev-col-mandatory');
                        $cmbOrg.append("<option value=0></option>");
                        for (var i = 0; i < result.length; i++) {
                            OrgName = VIS.Utility.Util.getValueOfString(result[i].Name);
                            AD_Org_ID = VIS.Utility.Util.getValueOfInt(result[i].Value);
                            $cmbOrg.append(" <option value=" + AD_Org_ID + ">" + OrgName + "</option>");
                        }
                    }
                    else {
                        $cmbOrg.append("<option value=0></option>");
                    }
                    if (VIS.context.getContextAsInt("#AD_Org_ID") > 0) {
                        $cmbOrg.val(VIS.context.getContextAsInt("#AD_Org_ID"));
                        $cmbOrg.removeClass('vis-ev-col-mandatory');
                    }
                    else
                        $cmbOrg.prop('selectedIndex', 0);
                },
                error: function (er) {
                    console.log(er);
                }
            });
        };

        /*
        Create busyIndicator
        */
        function createBusyIndicator() {
            $bsyDiv = $('<div class="vis-busyindicatorouterwrap"><div class="vis-busyindicatorinnerwrap"><i class="vis-busyindicatordiv"></i></div></div>');
            $bsyDiv[0].style.visibility = "hidden";
            $root.append($bsyDiv);
        };

        /// <summary>
        ///  Load Business Partner Info
        ///  - Payments
        ///  - Invoices
        /// </summary>
        function loadBPartner() {
            if (_C_BPartner_ID == 0 || _C_Currency_ID == 0) {
                return;
            }
            // If BP is selected then  set mandatory false---Neha
            $vSearchBPartner.getControl().removeClass('vis-ev-col-mandatory');
            $bsyDiv[0].style.visibility = "visible";
            // initialize Pageno as 1 when we change BP
            pageNoInvoice = 1, gridPgnoInvoice = 1, invoiceRecord = 0;
            pageNoCashJournal = 1, gridPgnoCashJounal = 1, CashRecord = 0;
            pageNoPayment = 1, gridPgnoPayment = 1, paymentRecord = 0;
            pageNoGL = 1, gridPgnoGL = 1, glRecord = 0;
            //when we save data at facing with busy indicator is visible so, to avoid that set all the grids loaded as true
            if (setallgridsLoaded == true) {
                isPaymentGridLoaded == true, isCashGridLoaded == true, isInvoiceGridLoaded == true, isGLGridLoaded == true;
                setallgridsLoaded = false;
            }
            else {
                isPaymentGridLoaded = false, isCashGridLoaded = false, isInvoiceGridLoaded = false, isGLGridLoaded = false;
            }

            C_ConversionType_ID = 0;
            var getDate = null;
            date = $date.val();
            if (date != null && date != undefined && date != "") {
                getDate = VIS.DB.to_date(date);
            }
            else
                getDate = VIS.DB.to_date(new Date());

            var chk = $vchkMultiCurrency.is(':checked');
            var AD_Role_ID = ctx.getAD_Role_ID();
            var AD_User_ID = ctx.getAD_User_ID();
            var role = VIS.MRole.getDefault();
            //  Need to have both values
            //	Async BPartner Test
            var key = _C_BPartner_ID;
            /********************************
         *  Load unallocated Payments
         *      1-TrxDate, 2-DocumentNo, (3-Currency, 4-PayAmt,)
         *      5-ConvAmt, 6-ConvOpen, 7-Allocated
         */
            loadUnallocatedPayments();

            /********************************
         *  Load unallocated Cash lines
         *      1-TrxDate, 2-DocumentNo, (3-Currency, 4-PayAmt,)
         *      5-ConvAmt, 6-ConvOpen, 7-Allocated
         */
            loadUnallocatedCashLines();

            loadInvoice();//Neha

            // refresh label on load
            refreshLabel();
            //after allocation Show the Payment grid for avoid the validation added set allocationTo value as P.
            $allocationFrom.val("P");
            $allocationFrom.trigger("change");
            $allocationTo.val("P");
            $allocationTo.trigger("change");
            if ($allocationFrom.val() == 0 && $allocationFrom.val() == 0) {
                blankAllGrids();
            }
            else {
                displayGrids($allocationFrom.val(), $allocationTo.val());
            }
            //$bsyDiv[0].style.visibility = "hidden";
        };

        /********************************
*  Load unallocated Payments
*      1-TrxDate, 2-DocumentNo, (3-Currency, 4-PayAmt,)
*      5-ConvAmt, 6-ConvOpen, 7-Allocated
*/
        function loadUnallocatedPayments() {
            AD_Org_ID = $OrgFilter.val();
            _C_Currency_ID = $cmbCurrency.val();
            if (_C_BPartner_ID == 0 || _C_Currency_ID == 0) {
                return;
            }
            fromDate = $pfromDate.val();
            toDate = $ptoDate.val();
            //---If ToDate is less than From Date--Display error message-----
            if (toDate != "") {
                if (fromDate > toDate) {
                    $ptoDate.val('');
                    return VIS.ADialog.info("Message", true, VIS.Msg.getMsg("VIS_FromDateGreater"), "");
                }
            }
            pageNoPayment = 1, gridPgnoPayment = 1, paymentRecord = 0;
            isPaymentGridLoaded = false;
            var chk = $vchkMultiCurrency.is(':checked');
            c_docType_ID = $payDocType.val();
            docBaseType = $payDocbaseType.val();
            payMethod_ID = VIS.Utility.Util.getValueOfInt($vPayMethod.getValue());
            srchText = $srchPayment.val();
            $bsyDiv[0].style.visibility = "visible";
            //---Get values and pass values to the controller----
            if (fromDate != null && fromDate != undefined && fromDate != "") {
                VIS.DB.to_date(fromDate);
            }
            if (toDate != null && toDate != undefined && toDate != "") {
                VIS.DB.to_date(toDate);
            }
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/GetPayments",
                data: { AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID, isInterBPartner: _isInterBPartner, chk: chk, page: pageNoPayment, size: PAGESIZE, c_docType_ID: c_docType_ID, docBaseType: docBaseType, PaymentMethod_ID: payMethod_ID, fromDate: fromDate, toDate: toDate, srchText: srchText, isInterComp: _isInterCompany },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {
                        if (pageNoPayment == 1 && data.length > 0) {
                            paymentRecord = data[0].PaymentRecord;
                            gridPgnoPayment = Math.ceil(paymentRecord / PAGESIZE);
                        }
                        //if any payment is selected and we load another payement from db than we have to check weather that invoice is already selected or not. if already selected tha skip that.
                        if (selectedPayments.length > 0) {
                            totalselectedpay = selectedPayments.length;
                            for (var i = 0; i < data.length; i++) {

                                var filterObj = selectedPayments.filter(function (e) {
                                    return e.CpaymentID == data[i]["CpaymentID"];
                                });

                                if (filterObj.length == 0) {
                                    data[i]["recid"] = selectedPayments.length + i;
                                    selectedPayments.push(data[i]);
                                }
                            }
                            data = selectedPayments;
                            // Clear Selected payments array when we de-select the select all checkbox. work done for to hold all the selected payements
                            selectedPayments = [];
                        }
                        //end
                        bindPaymentGrid(data, chk);
                        //added window no to find grid to implement scroll  issue : scroll was not working properly
                        $divPayment.find('#grid_openformatgrid_' + $self.windowNo + '_records').on('scroll', cartPaymentScroll);
                        isPaymentGridLoaded = true;
                        isBusyIndicatorHidden();
                    }
                },
                error: function (err) {
                    $bsyDiv[0].style.visibility = "hidden";
                }
            });
        };

        /********************************
    *  Load unallocated Cash lines
    *      1-TrxDate, 2-DocumentNo, (3-Currency, 4-PayAmt,)
    *      5-ConvAmt, 6-ConvOpen, 7-Allocated
    */
        function loadUnallocatedCashLines() {
            _C_Currency_ID = $cmbCurrency.val();
            if (_C_BPartner_ID == 0 || _C_Currency_ID == 0) {
                return;
            }
            pageNoCashJournal = 1, gridPgnoCashJounal = 1, CashRecord = 0;
            isCashGridLoaded = false;
            AD_Org_ID = $OrgFilter.val();
            fromDate = $cfromDate.val();
            toDate = $ctoDate.val();
            paymentType = $cashPayType.val();
            srchText = $srchCashJournal.val();
            //---If ToDate is less than From Date--Display error message-----
            if (toDate != "") {
                if (fromDate > toDate) {
                    $ctoDate.val('');
                    return VIS.ADialog.info("Message", true, VIS.Msg.getMsg("VIS_FromDateGreater"), "");
                }
            }
            var chk = $vchkMultiCurrency.is(':checked');
            if (fromDate != null && fromDate != undefined && fromDate != "") {
                VIS.DB.to_date(fromDate);
            }
            if (toDate != null && toDate != undefined && toDate != "") {
                VIS.DB.to_date(toDate);
            }
            $bsyDiv[0].style.visibility = "visible";
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/GetCashJounral",
                data: { AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID, isInterBPartner: _isInterBPartner, chk: chk, page: pageNoCashJournal, size: PAGESIZE, fromDate: fromDate, toDate: toDate, paymentType_ID: paymentType, srchText: srchText, isInterComp: _isInterCompany },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {
                        if (pageNoCashJournal == 1 && data.length > 0) {
                            CashRecord = data[0].CashRecord;
                            gridPgnoCashJounal = Math.ceil(CashRecord / PAGESIZE);
                        }
                        //if any invoice is selected and we load another cash lines from db than we have to check weather that cash lines is already selected or not. if already selected tha skip that.
                        if (selectedCashlines.length > 0) {
                            totalselectedcash = selectedCashlines.length;
                            for (var i = 0; i < data.length; i++) {

                                var filterObj = selectedCashlines.filter(function (e) {
                                    return e.CcashlineiID == data[i]["CcashlineiID"];
                                });

                                if (filterObj.length == 0) {
                                    data[i]["recid"] = selectedCashlines.length + i;
                                    selectedCashlines.push(data[i]);
                                }
                            }
                            data = selectedCashlines;
                            // Clear Selected cash lines array when we de-select the select all checkbox. work done for to hold all the selected cash lines
                            selectedCashlines = [];
                        }
                        //end
                        bindCashline(data, chk);
                        //added window no to find grid to implement scroll  issue : scroll was not working properly
                        $divCashline.find('#grid_openformatgridcash_' + $self.windowNo + '_records').on('scroll', cartCashScroll);
                        isCashGridLoaded = true;
                        isBusyIndicatorHidden();
                        //_payment = 7;
                    }
                },
                error: function (err) {
                    $bsyDiv[0].style.visibility = "hidden";
                }
            });
        };

        function loadCurrencyPrecision() {
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/GetCurrencyPrecision",
                data: { _C_Currency_ID: _C_Currency_ID },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {
                        stdPrecision = data;
                    }
                },
                error: function (err) {
                }
            });

        };

        // is used to shown busy indicator when we change BP and other parameters
        function isBusyIndicatorHidden() {
            if (isPaymentGridLoaded == true && isCashGridLoaded == true && isInvoiceGridLoaded == true && isGLGridLoaded == true) {
                $bsyDiv[0].style.visibility = "hidden";
            }
            else {
                $bsyDiv[0].style.visibility = "visible";
            }
        }

        //scroll implementation for GL grid
        function cartGLScroll() {
            if ($(this).scrollTop() > 0) {
                $bsyDiv[0].style.visibility = "visible";
                if ($(this).scrollTop() + $(this).innerHeight() >= this.scrollHeight) {
                    if (pageNoGL < gridPgnoGL) {
                        pageNoGL++;
                        getGLDataOnScroll(pageNoGL, PAGESIZE);
                    }
                }
                $bsyDiv[0].style.visibility = "hidden";
            }
        };
        function getGLDataOnScroll(pageNoGL, PAGESIZE) {
            var chk = $vchkMultiCurrency.is(':checked');
            _C_Currency_ID = $cmbCurrency.val();
            if (_C_Currency_ID == 0) {
                return VIS.ADialog.info("Message", true, "select currency", "");
            }
            var getDate = null;
            date = $date.val();
            if (date != null && date != undefined && date != "") {
                getDate = VIS.DB.to_date(date);
            }
            else
                getDate = VIS.DB.to_date(new Date());

            //c_docType_ID = $payDocType.val();
            //c_docbaseType_ID = $payDocbaseType.name;
            if (fromDate != null && fromDate != undefined && fromDate != "") {
                VIS.DB.to_date(fromDate);
            }
            if (toDate != null && toDate != undefined && toDate != "") {
                VIS.DB.to_date(toDate);
            }
            $.ajax({
                url: VIS.Application.contextUrl + "VIS/PaymentAllocation/GetGLData",
                dataType: "json",
                data: { AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID, page: pageNoGL, size: PAGESIZE, fromDate: fromDate, toDate: toDate, srchText: srchText, chk: chk, isInterComp: _isInterCompany },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {

                        //if any GL is selected and we load another GL from db than we have to check weather that Gl is already selected or not. if already selected tha skip that.
                        if (SelectedGL.length > 0) {
                            //debugger;
                            totalselectedgl = SelectedGL.length;
                            for (var i = 0; i < data.length; i++) {

                                var filterObj = SelectedGL.filter(function (e) {
                                    return e.GL_JOURNALLINE_ID == data[i]["GL_JOURNALLINE_ID"];
                                });

                                if (filterObj.length == 0) {
                                    data[i]["recid"] = SelectedGL.length + i;
                                    SelectedGL.push(data[i]);
                                }
                            }
                            data = SelectedGL;
                            // Clear Selected GL array when we de-select the select all checkbox. work done for to hold all the selected GL's
                            SelectedGL = [];
                        }
                        //end

                        bindGLGridOnScroll(data);
                    }
                },
                error: function (err) {

                }
            });
        };
        //scroll implementation for payment grid
        function cartPaymentScroll() {
            if ($(this).scrollTop() > 0) {
                $bsyDiv[0].style.visibility = "visible";
                if ($(this).scrollTop() + $(this).innerHeight() >= this.scrollHeight) {
                    if (pageNoPayment < gridPgnoPayment) {
                        pageNoPayment++;
                        getPaymentDataOnScroll(pageNoPayment, PAGESIZE);
                    }
                }
                $bsyDiv[0].style.visibility = "hidden";
            }
        };
        function getPaymentDataOnScroll(pageNoPayment, PAGESIZE) {
            _C_Currency_ID = $cmbCurrency.val();
            if (_C_Currency_ID == 0) {
                return VIS.ADialog.info("Message", true, "select currency", "");
            }
            var chk = $vchkMultiCurrency.is(':checked');
            var getDate = null;
            date = $date.val();
            if (date != null && date != undefined && date != "") {
                getDate = VIS.DB.to_date(date);
            }
            else
                getDate = VIS.DB.to_date(new Date());

            c_docType_ID = $payDocType.val();
            docBaseType = $payDocbaseType.val();
            payMethod_ID = VIS.Utility.Util.getValueOfInt($vPayMethod.getValue());
            srchText = $srchPayment.val();
            if (fromDate != null && fromDate != undefined && fromDate != "") {
                VIS.DB.to_date(fromDate);
            }
            if (toDate != null && toDate != undefined && toDate != "") {
                VIS.DB.to_date(toDate);
            }
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/GetPayments",
                data: { AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID, isInterBPartner: _isInterBPartner, chk: chk, page: pageNoPayment, size: PAGESIZE, c_docType_ID: c_docType_ID, docBaseType: docBaseType, PaymentMethod_ID: payMethod_ID, fromDate: fromDate, toDate: toDate, srchText: srchText, isInterComp: _isInterCompany },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {
                        var count = $gridPayment.records.length;
                        var newData = [];
                        //if any payment is selected and we load another payment from db than we have to check weather that payment is already selected or not. if already selected tha skip that.
                        if (selectedPayments.length > 0) {
                            totalselectedpay = selectedPayments.length;
                            for (var i = 0; i < data.length; i++) {
                                var filterObj = selectedPayments.filter(function (e) {
                                    return e.CpaymentID == data[i]["CpaymentID"];
                                });

                                if (filterObj.length == 0) {
                                    data[i]["recid"] = count + i;
                                    newData.push(data[i]);
                                }
                            }
                            data = newData;
                            // Clear Selected payments array when we de-select the select all checkbox. work done for to hold all the selected payments
                            selectedPayments = [];
                            newData = [];
                        }
                        //end
                        bindPaymentGridOnScroll(data);
                    }
                },
                error: function (err) {

                }
            });
        };

        // scroll implementation for cash grid
        function cartCashScroll() {
            if ($(this).scrollTop() > 0) {
                $bsyDiv[0].style.visibility = "visible";
                if ($(this).scrollTop() + $(this).innerHeight() >= this.scrollHeight) {
                    if (pageNoCashJournal < gridPgnoCashJounal) {
                        pageNoCashJournal++;
                        getCashDataOnScroll(pageNoCashJournal, PAGESIZE);
                    }
                }
                $bsyDiv[0].style.visibility = "hidden";
            }
        };
        function getCashDataOnScroll(pageNoCashJournal, PAGESIZE) {
            _C_Currency_ID = $cmbCurrency.val();
            if (_C_Currency_ID == 0) {
                return VIS.ADialog.info("Message", true, "select currency", "");
            }
            var chk = $vchkMultiCurrency.is(':checked');
            var getDate = null;
            date = $date.val();
            paymentType = $cashPayType.val();
            srchText = $srchCashJournal.val();
            if (date != null && date != undefined && date != "") {
                getDate = VIS.DB.to_date(date);
            }
            else
                getDate = VIS.DB.to_date(new Date());

            if (fromDate != null && fromDate != undefined && fromDate != "") {
                VIS.DB.to_date(fromDate);
            }
            if (toDate != null && toDate != undefined && toDate != "") {
                VIS.DB.to_date(toDate);
            }
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/GetCashJounral",
                data: { AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID, isInterBPartner: _isInterBPartner, chk: chk, page: pageNoCashJournal, size: PAGESIZE, fromDate: fromDate, toDate: toDate, paymentType_ID: paymentType, srchText: srchText, isInterComp: _isInterCompany },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {
                        var count = $gridCashline.records.length;
                        var newData = [];
                        if (selectedCashlines.length > 0) {
                            totalselectedcash = selectedCashlines.length;
                            for (var i = 0; i < data.length; i++) {
                                var filterObj = selectedCashlines.filter(function (e) {
                                    return e.CcashlineiID == data[i]["CcashlineiID"];
                                });

                                if (filterObj.length == 0) {
                                    data[i]["recid"] = count + i;
                                    newData.push(data[i]);
                                }
                            }
                            data = newData;
                            // Clear Selected invoices array when we de-select the select all checkbox. work done for to hold all the selected invoices
                            selectedCashlines = [];
                            newData = [];
                        }
                        //end
                        bindCashlineGridOnScroll(data);
                    }
                },
                error: function (err) {

                }
            });
        };

        // scroll implementation for invoice grid
        function cartInvoiceScroll() {
            if ($(this).scrollTop() > 0) {
                $bsyDiv[0].style.visibility = "visible";
                if ($(this).scrollTop() + $(this).innerHeight() >= this.scrollHeight) {
                    if (pageNoInvoice < gridPgnoInvoice) {
                        pageNoInvoice++;
                        getInvoiceDataOnScroll(pageNoInvoice, PAGESIZE);
                    }
                }
                $bsyDiv[0].style.visibility = "hidden";
            }
        };
        function getInvoiceDataOnScroll(pageNoInvoice, PAGESIZE) {
            var chk = $vchkMultiCurrency.is(':checked');
            var getDate = null;
            date = $date.val();
            if (date != null && date != undefined && date != "") {
                getDate = VIS.DB.to_date(date);
            }
            else
                getDate = VIS.DB.to_date(new Date());

            //---Get values and pass values to the controller----Neha
            docNo = $txtDocNo.val();
            _C_Currency_ID = $cmbCurrency.val();
            if (_C_Currency_ID == 0) {
                return VIS.ADialog.info("Message", true, "select currency", "");
            }
            c_docType_ID = $cmbDocType.val();
            docBaseType = $invDocbaseType.val();
            payMethod_ID = VIS.Utility.Util.getValueOfInt($vInvPayMethod.getValue());
            srchText = $srchInvoice.val();
            if (fromDate != null && fromDate != undefined && fromDate != "") {
                VIS.DB.to_date(fromDate);
            }
            if (toDate != null && toDate != undefined && toDate != "") {
                VIS.DB.to_date(toDate);
            }

            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/GetInvoice",
                data: {
                    AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID,
                    isInterBPartner: _isInterBPartner, chk: chk, date: getDate, page: pageNoInvoice,
                    size: PAGESIZE, docNo: docNo, c_docType_ID: c_docType_ID, docBaseType: docBaseType,
                    PaymentMethod_ID: payMethod_ID, fromDate: fromDate, toDate: toDate, conversionDate: conversionDate,
                    srchText: srchText, isInterComp: _isInterCompany, conversionType_ID: $vConversionType.getValue()
                },
                async: true,
                success: function (result) {
                    var data = JSON.parse(result);
                    if (data) {

                        //if any invoice is selected and we load another invoice from db than we have to check weather that invoice is already selected or not. if already selected tha skip that.
                        var newData = [];/* contain on scroll data */
                        var count = $gridInvoice.records.length;
                        if (selectedInvoices.length > 0) {
                            totalselectedinv = selectedInvoices.length;
                            for (var i = 0; i < data.length; i++) {
                                var filterObj = selectedInvoices.filter(function (e) {
                                    return e.C_InvoicePaySchedule_ID == data[i]["C_InvoicePaySchedule_ID"];
                                });

                                if (filterObj.length == 0) {
                                    data[i]["recid"] = count + i;
                                    newData.push(data[i]);
                                }
                            }
                            data = newData;
                            // Clear Selected invoices array when we de-select the select all checkbox. work done for to hold all the selected invoices
                            selectedInvoices = [];
                            newData = [];
                        }
                        //end

                        bindInvoiceGridOnScroll(data);
                        //Amount
                        _openInv = 9;
                        _discount = 10;
                        _writeOff = 13;
                        _applied = 14;
                    }
                },
                error: function (err) {

                }
            });
        };

        //GL grid bind
        //render to culture format
        function bindGLGridOnScroll(data) {
            var rows = [];
            var colkeys = [];

            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            // check records exist in grid or not
            // for updating record id 
            var count = 0;
            if ($glLineGrid.records.length > 0) {
                count = $glLineGrid.records.length;
            };

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = count + r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna == "SelectRow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            // bind rows with grid
            w2utils.encodeTags(rows);
            $glLineGrid.add(rows);

            // when we load new payment either after filter or directly and if any payment is selected already than we need to trigger it's click event.
            if (totalselectedgl > 0) {
                for (var i = 0; i < totalselectedgl; i++) {
                    var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $glLineGrid.editChange.call($glLineGrid, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $glLineGrid.trigger(eData);
                }
            }
            totalselectedgl = 0;
            //end

            rows = [];
            colkeys = [];
            //added window no to find grid to implement scroll  issue : scroll was not working properly
            $divGl.find('#grid_openformatglgrid_' + $self.windowNo + '_records').on('scroll', cartGLScroll);
        };

        //binding GL Grid 
        function bindGLGrid(data, chk) {
            var columns = [];
            columns.push({ field: "SelectRow", columnName: "SelectRow", caption: VIS.translatedTexts.VIS_Check, size: '50px', editable: { type: 'checkbox' }, sortable: false });
            columns.push({ field: "AD_Org_ID", columnName: "AD_Org_ID", caption: VIS.translatedTexts.AD_Org_ID, size: '85px', hidden: true });
            columns.push({ field: "OrgName", columnName: "OrgName", caption: VIS.translatedTexts.AD_Org_ID, size: '105px', hidden: false, sortable: true });
            columns.push({
                field: "DATEACCT", columnName: "DATEACCT", caption: VIS.translatedTexts.DateAcct, render: function (record, index, col_index) {
                    var val = record["DATEACCT"];
                    return new Date(val).toLocaleDateString();
                }, size: '105px', hidden: false, sortable: true
            });
            columns.push({ field: "DOCUMENTNO", columnName: "DOCUMENTNO", caption: VIS.translatedTexts.DocumentNo, size: '120px', hidden: false, sortable: true });
            //added new Column Account - on 24/09/2020
            columns.push({ field: "Account", columnName: "Account", caption: VIS.translatedTexts.Account, size: '150px', hidden: false, sortable: true });
            columns.push({ field: "Isocode", columnName: "Isocode", caption: VIS.translatedTexts.TrxCurrency, size: '105px', hidden: false, sortable: true });
            columns.push({ field: "ConversionName", columnName: "ConversionName", caption: VIS.translatedTexts.C_ConversionType_ID, size: '105px', hidden: false, sortable: true });
            columns.push({
                field: "ConvertedAmount", columnName: "ConvertedAmount", caption: VIS.Msg.getMsg("Amount"), attr: 'align=right', size: '150px', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["ConvertedAmount"];
                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
                }
            });
            columns.push({
                field: "OpenAmount", columnName: "OpenAmount", caption: VIS.translatedTexts.OpenAmount, attr: 'align=right', size: '150px', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["OpenAmount"];
                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
                }
            });
            columns.push({
                field: "AppliedAmt", columnName: "AppliedAmt", caption: VIS.translatedTexts.AppliedAmount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["AppliedAmt"];
                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
                }
            });
            columns.push({
                field: "DATEDOC", columnName: "DATEDOC", caption: VIS.translatedTexts.Date, render: function (record, index, col_index) {
                    var val = record["DATEDOC"];
                    return new Date(val).toLocaleDateString();
                }, size: '80px', hidden: true
            });
            columns.push({ field: "GL_JOURNALLINE_ID", columnName: "GL_JOURNALLINE_ID", caption: VIS.translatedTexts.GL_JOURNALLINE_ID, size: '150px', hidden: true });
            columns.push({ field: "C_ConversionType_ID", columnName: "C_ConversionType_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true });
            columns.push({ field: "GL_Journal_ID", columnName: "GL_Journal_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true });

            var rows = [];

            var colkeys = [];

            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna == "SelectRow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            if ($glLineGrid != undefined && $glLineGrid != null) {
                $glLineGrid.destroy();
                $glLineGrid = null;
            }

            $glLineGrid = $divGl.w2grid({
                name: 'openformatglgrid_' + $self.windowNo,
                //show: {
                //    toolbar: true,
                //    toolbarSearch: false,
                //    toolbarInput: false,
                //    searchAll: false
                //},
                multiSelect: true,
                columns: columns,
                records: rows,
                onChange: function (event) {
                    if (checkCheckedLimitOnCellCheckboxClick(event, false, false, false, true)) {
                        glCellChanged(event);
                        //getMaxDate();
                    }
                },
                onClick: function (event) {
                    glCellClicked(event);
                    getMaxDate();
                },
                onDblClick: function (event) {
                    //glDoubleClicked(event);
                    event.preventDefault();
                    return;
                },
                onSelect: function (event) {
                    if (event.all) {
                        event.onComplete = function () {
                            //added window no to find grid to implement scroll  issue : scroll was not working properly
                            $divGl.find('#grid_openformatglgrid_' + $self.windowNo + '_records').on('scroll', cartGLScroll);
                        }
                    }
                },
                //on edit grid columns need to prevent dot or comma according to culture
                onEditField: function (event) {
                    event.onComplete = function (event) {
                        id = event.recid;

                        $('#grid_openformatglgrid_' + $self.windowNo + '_edit_' + id + '_10').keydown(function (event) {
                            var isDotSeparator = culture.isDecimalSeparatorDot(window.navigator.language);

                            if (!isDotSeparator && (event.keyCode == 190 || event.keyCode == 110)) {// , separator
                                return false;
                            }
                            else if (isDotSeparator && event.keyCode == 188) { // . separator
                                return false;
                            }
                            if (event.target.value.contains(".") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                this.value = this.value.replace('.', '');
                            }
                            if (event.target.value.contains(",") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                this.value = this.value.replace(',', '');
                            }
                            if (event.keyCode != 8 && event.keyCode != 9 && (event.keyCode < 37 || event.keyCode > 40) &&
                                (event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)
                                && event.keyCode != 109 && event.keyCode != 189 && event.keyCode != 110
                                && event.keyCode != 144 && event.keyCode != 188 && event.keyCode != 190) {
                                return false;
                            }
                        });
                    };
                }
            });
            // when we load new payment either after filter or directly and if any payment is selected already than we need to trigger it's click event.
            if (totalselectedgl > 0) {
                for (var i = 0; i < totalselectedgl; i++) {
                    var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $glLineGrid.editChange.call($glLineGrid, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $glLineGrid.trigger(eData);
                }
            }
            totalselectedgl = 0;
            //end
            $glLineGrid.autoLoad = true;
            $($($glLineGrid.box)[0]).find('.w2ui-search-down').css('margin-top', '4px');
            $($($glLineGrid.box)[0]).find('.w2ui-search-clear').css('margin-top', '4px');
        };

        //payment grid bind
        //render to culture format
        function bindPaymentGrid(data, chk) {
            var columns = [];
            columns.push({ field: 'SelectRow', columnName: "SelectRow", caption: VIS.translatedTexts.VIS_Check, size: '50px', editable: { type: 'checkbox' }, sortable: false });
            columns.push({ field: "AD_Org_ID", columnName: "AD_Org_ID", caption: VIS.translatedTexts.AD_Org_ID, size: '85px', hidden: true });
            columns.push({ field: "OrgName", columnName: "OrgName", caption: VIS.translatedTexts.AD_Org_ID, size: '85px', hidden: false, sortable: true });
            columns.push({
                field: "DATEACCT", columnName: "DATEACCT", caption: VIS.translatedTexts.DateAcct, size: '85px', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["DATEACCT"];
                    return new Date(val).toLocaleDateString();
                }
            });
            columns.push({ field: "Documentno", columnName: "Documentno", caption: VIS.translatedTexts.DocumentNo, size: '120px', hidden: false, sortable: true });
            columns.push({ field: "DocBaseType", columnName: "DocBaseType", caption: VIS.translatedTexts.DocBaseType, size: '120px', hidden: false, sortable: true });
            //new column payment menthod name added into grid
            columns.push({ field: "PayName", columnName: "PayName", caption: VIS.translatedTexts.PaymentMethod, size: '120px', hidden: false, sortable: true });
            //if (chk) {
            //    columns.push({ field: "Isocode", caption: VIS.translatedTexts.TrxCurrency, size: '85px', hidden: false });
            //}
            //else {
            columns.push({ field: "Isocode", columnName: "Isocode", caption: VIS.translatedTexts.TrxCurrency, size: '85px', hidden: false, sortable: true });
            //}
            columns.push({ field: "ConversionName", columnName: "ConversionName", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: false, sortable: true });
            if (chk) {
                //render column into float with culture format
                columns.push({
                    field: "Payment", columnName: "Payment", caption: VIS.translatedTexts.Amount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                        var val = record["Payment"];
                        return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                    }
                });
            }
            else {
                columns.push({ field: "Payment", columnName: "Payment", caption: VIS.translatedTexts.Amount, size: '150px', hidden: true });
            }
            //render column into float with culture format
            columns.push({
                field: "ConvertedAmount", columnName: "ConvertedAmount", caption: VIS.translatedTexts.ConvertedAmount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["ConvertedAmount"];
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            });
            //render column into float with culture format
            columns.push({
                field: "OpenAmt", columnName: "OpenAmt", caption: VIS.translatedTexts.OpenAmount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["OpenAmt"];
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            });
            columns.push({ field: "Multiplierap", columnName: "Multiplierap", caption: VIS.translatedTexts.multiplierap, size: '150px', hidden: true });
            //render column into float with culture format
            columns.push({
                field: "AppliedAmt", columnName: "AppliedAmt", caption: VIS.translatedTexts.AppliedAmount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {

                    var val = record["AppliedAmt"];
                    val = checkcommaordot(event, val, val);
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }, editable: { type: 'number' }
            });
            columns.push({
                field: "Date1", columnName: "Date1", caption: VIS.translatedTexts.Date, size: '80px', hidden: true, render: function (record, index, col_index) {
                    var val = record["Date1"];
                    return new Date(val).toLocaleDateString();
                }
            });
            columns.push({ field: "CpaymentID", columnName: "CpaymentID", caption: VIS.translatedTexts.c_payment_id, size: '150px', hidden: true });
            columns.push({ field: "C_ConversionType_ID", columnName: "C_ConversionType_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true });

            var rows = [];

            var colkeys = [];

            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna == "SelectRow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            if ($gridPayment != undefined && $gridPayment != null) {
                $gridPayment.destroy();
                $gridPayment = null;
            }

            $gridPayment = $divPayment.w2grid({
                name: 'openformatgrid_' + $self.windowNo,
                //show: { toolbar: true },
                multiSelect: true,
                columns: columns,
                records: rows,
                onClick: function (event) {
                    paymentCellClicked(event);
                    getMaxDate();
                },
                onChange: function (event) {
                    if (checkCheckedLimitOnCellCheckboxClick(event, false, true, false, false)) {
                        paymentCellChanged(event);
                    }
                },
                onDblClick: function (event) {
                    paymentDoubleClicked(event);
                },
                onSelect: function (event) {
                    if (event.all) {
                        event.onComplete = function () {
                            //added window no to find grid to implement scroll  issue : scroll was not working properly
                            $divPayment.find('#grid_openformatgrid_' + $self.windowNo + '_records').on('scroll', cartPaymentScroll);
                        }
                    }
                },
                //on edit grid columns need to prevent dot or comma according to culture
                onEditField: function (event) {
                    event.onComplete = function (event) {
                        id = event.recid;
                        //event.column is a AppliedAmt column sequence No
                        $('#grid_openformatgrid_' + $self.windowNo + '_edit_' + id + '_12').keydown(function (event) {
                            var isDotSeparator = culture.isDecimalSeparatorDot(window.navigator.language);

                            if (!isDotSeparator && (event.keyCode == 190 || event.keyCode == 110)) {// , separator
                                return false;
                            }
                            else if (isDotSeparator && event.keyCode == 188) { // . separator
                                return false;
                            }
                            if (event.target.value.contains(".") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                this.value = this.value.replace('.', '');
                            }
                            if (event.target.value.contains(",") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                this.value = this.value.replace(',', '');
                            }
                            if (event.keyCode != 8 && event.keyCode != 9 && (event.keyCode < 37 || event.keyCode > 40) &&
                                (event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)
                                && event.keyCode != 109 && event.keyCode != 189 && event.keyCode != 110
                                && event.keyCode != 144 && event.keyCode != 188 && event.keyCode != 190) {
                                return false;
                            }
                        });
                    };
                }
            });
            // when we load new payment either after filter or directly and if any payment is selected already than we need to trigger it's click event.
            if (totalselectedpay > 0) {
                for (var i = 0; i < totalselectedpay; i++) {
                    var chk = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $gridPayment.editChange.call($gridPayment, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridPayment.trigger(eData);
                }
            }
            totalselectedpay = 0;
            //end
            $gridPayment.autoLoad = true;
            $($($gridPayment.box)[0]).find('.w2ui-search-down').css('margin-top', '4px');
            $($($gridPayment.box)[0]).find('.w2ui-search-clear').css('margin-top', '4px');
        };
        function bindPaymentGridOnScroll(data) {
            var rows = [];
            var colkeys = [];

            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            // check records exist in grid or not
            // for updating record id 
            var count = 0;
            if ($gridPayment.records.length > 0) {
                count = $gridPayment.records.length;
            };

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = count + r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna == "SelectRow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            // bind rows with grid
            w2utils.encodeTags(rows);
            $gridPayment.add(rows);

            // when we load new payment either after filter or directly and if any payment is selected already than we need to trigger it's click event.
            if (totalselectedpay > 0) {
                for (var i = 0; i < totalselectedpay; i++) {
                    var chk = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $gridPayment.editChange.call($gridPayment, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridPayment.trigger(eData);
                }
            }
            totalselectedpay = 0;
            //end
            rows = [];
            colkeys = [];
            //added window no to find grid to implement scroll  issue : scroll was not working properly
            $divPayment.find('#grid_openformatgrid_' + $self.windowNo + '_records').on('scroll', cartPaymentScroll);
        };

        //Cash line grid bind
        //culture work
        function bindCashline(data, chk) {
            var columns = [];
            columns.push({ field: 'SelectRow', columnName: "SelectRow", caption: VIS.translatedTexts.VIS_Check, size: '50px', editable: { type: 'checkbox' }, sortable: false });
            columns.push({ field: "AD_Org_ID", columnName: "AD_Org_ID", caption: VIS.translatedTexts.AD_Org_ID, size: '85px', hidden: true, sortable: true });
            columns.push({ field: "OrgName", columnName: "OrgName", caption: VIS.translatedTexts.AD_Org_ID, size: '105px', hidden: false, sortable: true });
            columns.push({
                field: "DATEACCT", columnName: "DATEACCT", caption: VIS.translatedTexts.DateAcct, size: '105px', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["DATEACCT"];
                    return new Date(val).toLocaleDateString();
                }
            });
            columns.push({ field: "ReceiptNo", columnName: "ReceiptNo", caption: VIS.translatedTexts.RECEIPTNO, size: '120px', hidden: false, sortable: true });
            columns.push({ field: "VSS_paymenttype", columnName: "VSS_paymenttype", caption: "Payment ID", size: '120px', hidden: true });
            columns.push({ field: "Payment", columnName: "Payment", caption: "Payment Type", size: '120px', hidden: false, sortable: true });
            columns.push({ field: "Isocode", columnName: "Isocode", caption: VIS.Msg.getMsg("TrxCurrency"), size: '105px', hidden: false, sortable: true });
            columns.push({ field: "ConversionName", columnName: "ConversionName", caption: VIS.translatedTexts.C_ConversionType_ID, size: '105px', hidden: false, sortable: true });
            if (chk) {
                //render column into float with culture format
                columns.push({
                    field: "Amount", columnName: "Amount", caption: VIS.translatedTexts.Amount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                        var val = record["Amount"];
                        return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                    }
                });
            }
            else {
                columns.push({ field: "Amount", columnName: "Amount", caption: VIS.translatedTexts.Amount, size: '150px', hidden: true });
            }
            //render column into float with culture format
            columns.push({
                field: "ConvertedAmount", columnName: "ConvertedAmount", caption: VIS.Msg.getMsg("ConvertedAmount"), size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["ConvertedAmount"];
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            });
            //render column into float with culture format
            columns.push({
                field: "OpenAmt", columnName: "OpenAmt", caption: VIS.translatedTexts.OpenAmount, size: '150px', hidden: false, attr: 'align=right', sortable: true, render: function (record, index, col_index) {
                    var val = record["OpenAmt"];
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            });
            //render column into float with culture format
            columns.push({
                field: "AppliedAmt", columnName: "AppliedAmt", caption: VIS.translatedTexts.AppliedAmount, size: '150px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["AppliedAmt"];
                    val = checkcommaordot(event, val, val);
                    return parseFloat(val).toLocaleString(navigator.language, { maximumFractionDigits: stdPrecision, minimumFractionDigits: stdPrecision });
                }, editable: { type: 'number' }
            });
            columns.push({
                field: "Created", columnName: "Created", caption: VIS.translatedTexts.Date, size: '80px', hidden: true, sortable: true, render: function (record, index, col_index) {
                    var val = record["Created"];
                    return new Date(val).toLocaleDateString();
                }
            });
            //if (chk == true) {
            //    columns.push({ field: "Isocode", caption: VIS.Msg.getMsg("TrxCurrency"), size: '85px', hidden: false });
            //}
            //else {
            //    columns.push({ field: "Isocode", caption: VIS.Msg.getMsg("TrxCurrency"), size: '85px', hidden: true });
            //}
            //if (chk == true) {
            //    //render column into float with culture format
            //    columns.push({
            //        field: "Amount", caption: VIS.translatedTexts.Amount, size: '150px', hidden: false, render: function (record, index, col_index) {
            //            var val = record["Amount"];
            //            return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
            //        }
            //    });
            //}
            //else {
            //    columns.push({ field: "Amount", caption: VIS.translatedTexts.Amount, size: '150px', hidden: true });
            //}
            columns.push({ field: "CcashlineiID", columnName: "CcashlineiID", caption: VIS.translatedTexts.ccashlineid, size: '150px', hidden: true });
            columns.push({ field: "Multiplierap", columnName: "Multiplierap", caption: VIS.translatedTexts.multiplierap, hidden: true });
            columns.push({ field: "C_ConversionType_ID", columnName: "C_ConversionType_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true });
            var rows = [];

            var colkeys = [];


            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna.toLower() == "selectrow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            if ($gridCashline != undefined && $gridCashline != null) {
                $gridCashline.destroy();
                $gridCashline = null;
            }

            $gridCashline = $divCashline.w2grid({
                name: 'openformatgridcash_' + $self.windowNo,
                multiSelect: true,
                //show: { toolbar: true },
                columns: columns,
                records: rows,
                onChange: function (event) {
                    if (checkCheckedLimitOnCellCheckboxClick(event, false, false, true, false)) {
                        cashCellChanged(event);
                    }
                },
                onClick: function (event) {
                    cashCellClicked(event);
                    getMaxDate();
                },
                onDblClick: function (event) {
                    cashDoubleClicked(event);
                },
                onSelect: function (event) {
                    if (event.all) {
                        event.onComplete = function () {
                            //added window no to find grid to implement scroll  issue : scroll was not working properly
                            $divCashline.find('#grid_openformatgridcash_' + $self.windowNo + '_records').on('scroll', cartCashScroll);
                        }
                    }
                },
                //on edit grid columns need to prevent dot or comma according to culture
                onEditField: function (event) {
                    event.onComplete = function (event) {
                        id = event.recid;
                        //event.column is a AppliedAmt column sequence No
                        $('#grid_openformatgridcash_' + $self.windowNo + '_edit_' + id + '_12').keydown(function (event) {
                            var isDotSeparator = culture.isDecimalSeparatorDot(window.navigator.language);

                            if (!isDotSeparator && (event.keyCode == 190 || event.keyCode == 110)) {// , separator
                                return false;
                            }
                            else if (isDotSeparator && event.keyCode == 188) { // . separator
                                return false;
                            }
                            if (event.target.value.contains(".") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                this.value = this.value.replace('.', '');
                            }
                            if (event.target.value.contains(",") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                this.value = this.value.replace(',', '');
                            }
                            if (event.keyCode != 8 && event.keyCode != 9 && (event.keyCode < 37 || event.keyCode > 40) &&
                                (event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)
                                && event.keyCode != 109 && event.keyCode != 189 && event.keyCode != 110
                                && event.keyCode != 144 && event.keyCode != 188 && event.keyCode != 190) {
                                return false;
                            }
                        });
                    };
                }
            });

            // when we load new cashJournal either after filter or directly and if any cashJournal is selected already than we need to trigger it's click event.
            if (totalselectedcash > 0) {
                for (var i = 0; i < totalselectedcash; i++) {
                    var chk = $('#grid_' + $gridCashline.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $gridCashline.editChange.call($gridCashline, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridCashline.trigger(eData);
                }
            }
            totalselectedcash = 0;
            //end
            $gridCashline.autoLoad = true;
            $($($gridCashline.box)[0]).find('.w2ui-search-down').css('margin-top', '4px');
            $($($gridCashline.box)[0]).find('.w2ui-search-clear').css('margin-top', '4px');
        };
        function bindCashlineGridOnScroll(data) {
            var rows = [];
            var colkeys = [];

            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            // check records exist in grid or not
            // for updating record id 
            var count = 0;
            if ($gridCashline.records.length > 0) {
                count = $gridCashline.records.length;
            };

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = count + r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna.toLower() == "selectrow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            // bind rows with grid
            w2utils.encodeTags(rows);
            $gridCashline.add(rows);

            rows = [];
            colkeys = [];
            //added window no to find grid to implement scroll  issue : scroll was not working properly
            $divCashline.find('#grid_openformatgridcash_' + $self.windowNo + '_records').on('scroll', cartCashScroll);
        };

        /**
         * check checkbox checked limit when click on select all checkbox max 50 records are allow to select
         * @param {any} chk
         * @param {any} isInv
         * @param {any} isPayment
         * @param {any} isCash
         * @param {any} isGl
         */
        function checkCheckedLimitOnSelectAllCheckBox(chk, isInv, isPayment, isCash, isGl) {
            var result = true;
            var checked = null;
            if (isInv)
                checked = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]:checked');
            else if (isPayment)
                checked = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]:checked');
            else if (isCash)
                checked = $('#grid_' + $gridCashline.name + '_records td[col="0"]').find('input[type="checkbox"]:checked');
            else if (isGl)
                checked = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]:checked');

            //VA228:User can select maximum 50 records assigned by amit
            if (checked != null && checked.length > 50) {
                $(chk).prop('checked', false);
                result = false;
            }
            return result;
        }
        /**
         * check checked checkbox on click checkbox inside grid max 50 records are allow to select
         * @param {any} event
         * @param {any} isInv
         * @param {any} isPayment
         * @param {any} isCash
         * @param {any} isGl
         */
        function checkCheckedLimitOnCellCheckboxClick(event, isInv, isPayment, isCash, isGl) {
            var result = true;
            var gridId = null;
            if (isInv)
                gridId = $gridInvoice.name;
            else if (isPayment)
                gridId = $gridPayment.name;
            else if (isCash)
                gridId = $gridCashline.name;
            else if (isGl)
                gridId = $glLineGrid.name;

            var checked = $('#grid_' + gridId + '_records td[col="0"]').find('input[type="checkbox"]:checked');
            //VA228:User can select maximum 50 records assigned by amit
            if (checked != null && checked.length > 50) {
                var chk = $('#grid_' + gridId + '_records td[col="0"]').find('input[type="checkbox"]');
                $(chk[event.recid]).prop('checked', false);
                VIS.ADialog.warn("VIS_MaxLimitReached");
                event.preventDefault();
                result = false;
            }
            return result;
        }

        //Invoice grid bind
        function bindInvoiceGrid(data, chk) {
            var columns = [];
            columns.push({ field: 'SelectRow', columnName: "SelectRow", caption: VIS.translatedTexts.VIS_Check, size: '50px', editable: { type: 'checkbox' }, sortable: false });
            columns.push({ field: "AD_Org_ID", columnName: "AD_Org_ID", caption: VIS.translatedTexts.AD_Org_ID, size: '85px', hidden: true });
            columns.push({ field: "OrgName", columnName: "OrgName", caption: VIS.translatedTexts.AD_Org_ID, size: '105px', hidden: false, sortable: true });
            columns.push({
                field: "DATEACCT", columnName: "DATEACCT", caption: VIS.translatedTexts.DateAcct, size: '105px', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["DATEACCT"];
                    return new Date(val).toLocaleDateString();
                }
            });
            columns.push({ field: "Documentno", columnName: "Documentno", caption: VIS.translatedTexts.DocumentNo, size: '120px', hidden: false, sortable: true });
            columns.push({
                field: "InvoiceScheduleDate", columnName: "InvoiceScheduleDate", caption: VIS.translatedTexts.ScheduleDate, size: '100px', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["InvoiceScheduleDate"];
                    return new Date(val).toLocaleDateString();
                }
            });
            columns.push({ field: "DocBaseType", columnName: "DocBaseType", caption: VIS.translatedTexts.DocBaseType, size: '120px', hidden: false, sortable: true });
            //new column payment method name added into grid
            columns.push({ field: "PayName", columnName: "PayName", caption: VIS.translatedTexts.PaymentMethod, size: '120px', hidden: false, sortable: true });
            columns.push({ field: "Isocode", columnName: "Isocode", caption: VIS.Msg.getMsg("TrxCurrency"), size: '85px', hidden: false, sortable: true });
            columns.push({ field: "ConversionName", columnName: "ConversionName", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: false, sortable: true });
            if (chk) {
                //render column into float with culture format
                columns.push({
                    field: "Currency", columnName: "Currency", caption: VIS.translatedTexts.Amount, size: '100px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                        var val = record["Currency"];
                        return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });

                    }
                });
            }
            else {
                columns.push({ field: "Currency", columnName: "Currency", caption: VIS.translatedTexts.Amount, size: '100px', hidden: true });
            }
            //render column into float with culture format
            columns.push({
                field: "Converted", columnName: "Converted", caption: VIS.Msg.getMsg("ConvertedAmount"), size: '100px', hidden: false, sortable: true, attr: 'align=right', render: function (record, index, col_index) {
                    var val = record["Converted"];
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            });
            //render column into float with culture format
            columns.push({
                field: "Amount", columnName: "Amount", caption: VIS.translatedTexts.OpenAmount, size: '100px', hidden: false, sortable: true, attr: 'align=right', render: function (record, index, col_index) {
                    var val = record["Amount"];
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            });
            //render column into float with culture format
            columns.push({
                field: "Discount", columnName: "Discount", caption: VIS.translatedTexts.DiscountAmt, size: '100px', hidden: false, sortable: true, attr: 'align=right', render: function (record, index, col_index) {
                    var val = record["Discount"];
                    val = checkcommaordot(event, val, val);
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }, editable: { type: 'number' }
            });
            columns.push({
                field: "Date1", columnName: "Date1", caption: VIS.translatedTexts.Date, size: '80px', hidden: true, render: function (record, index, col_index) {
                    var val = record["Date1"];
                    return new Date(val).toLocaleDateString();
                }
            });
            columns.push({ field: "CinvoiceID", columnName: "AD_Org_ID", caption: VIS.translatedTexts.cinvoiceid, size: '150px', hidden: true });
            //if (chk) {
            //    columns.push({ field: "Isocode", caption: VIS.Msg.getMsg("TrxCurrency"), size: '85px', hidden: false });
            //}
            //else {
            //    columns.push({ field: "Isocode", caption: VIS.Msg.getMsg("TrxCurrency"), size: '85px', hidden: true });
            //}
            columns.push({ field: "Multiplierap", columnName: "Multiplierap", caption: VIS.translatedTexts.multiplierap, hidden: true });
            //render column into float with culture format
            columns.push({
                field: "Writeoff", columnName: "Writeoff", caption: VIS.translatedTexts.WriteOffAmount, size: '100px', attr: 'align=right', hidden: false, sortable: true, render: function (record, index, col_index) {
                    var val = record["Writeoff"];
                    val = checkcommaordot(event, val, val);
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }, editable: { type: 'number' }
            });
            //render column into float with culture format
            columns.push({
                field: "AppliedAmt", columnName: "AppliedAmt", caption: VIS.translatedTexts.AppliedAmount, size: '100px', hidden: false, sortable: true, attr: 'align=right', render: function (record, index, col_index) {
                    var val = record["AppliedAmt"];
                    val = checkcommaordot(event, val, val);
                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }, editable: { type: 'number' }
            });
            if (countVA009 > 0) {
                columns.push({ field: "C_InvoicePaySchedule_ID", columnName: "C_InvoicePaySchedule_ID", caption: VIS.translatedTexts.C_InvoicePaySchedule_ID, size: '100px', hidden: true });
            }
            columns.push({ field: "C_ConversionType_ID", columnName: "C_ConversionType_ID", caption: VIS.translatedTexts.C_ConversionType_ID, size: '85px', hidden: true });

            var rows = [];

            var colkeys = [];


            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna.toLower() == "selectrow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            if ($gridInvoice != undefined && $gridInvoice != null) {
                $gridInvoice.destroy();
                $gridInvoice = null;
            }

            $gridInvoice = $divInvoice.w2grid({
                name: 'openformatgridinvoice_' + $self.windowNo,
                //show: { toolbar: true },
                multiSelect: true,
                columns: columns,
                records: rows,
                onChange: function (event) {
                    if (checkCheckedLimitOnCellCheckboxClick(event, true, false, false, false)) {
                        invoiceCellChanged(event);
                        getMaxDate();
                    }
                },
                onClick: function (event) {
                    invoiceCellClicked(event);
                    getMaxDate();
                },
                onDblClick: function (event) {
                    invoiceDoubleClicked(event);
                },
                onSelect: function (event) {
                    if (event.all) {
                        event.onComplete = function () {
                            //added window no to find grid to implement scroll  issue : scroll was not working properly
                            $divInvoice.find('#grid_openformatgridinvoice_' + $self.windowNo + '_records').on('scroll', cartInvoiceScroll);
                        }
                    }
                },
                //on edit grid columns need to prevent dot or comma according to culture
                onEditField: function (event) {
                    event.onComplete = function (event) {
                        id = event.recid;
                        //event.column 12 for DiscountAmt, 16 for WriteOffAmt and 17 for AppliedAmt column sequence.
                        if (event.column == 12 || event.column == 16 || event.column == 17) {
                            $('#grid_openformatgridinvoice_' + $self.windowNo + '_edit_' + id + '_' + event.column).keydown(function (event) {
                                var isDotSeparator = culture.isDecimalSeparatorDot(window.navigator.language);

                                if (!isDotSeparator && (event.keyCode == 190 || event.keyCode == 110)) {// , separator
                                    return false;
                                }
                                else if (isDotSeparator && event.keyCode == 188) { // . separator
                                    return false;
                                }
                                if (event.target.value.contains(".") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                    this.value = this.value.replace('.', '');
                                }
                                if (event.target.value.contains(",") && (event.which == 110 || event.which == 190 || event.which == 188)) {
                                    this.value = this.value.replace(',', '');
                                }
                                if (event.keyCode != 8 && event.keyCode != 9 && (event.keyCode < 37 || event.keyCode > 40) &&
                                    (event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)
                                    && event.keyCode != 109 && event.keyCode != 189 && event.keyCode != 110
                                    && event.keyCode != 144 && event.keyCode != 188 && event.keyCode != 190) {
                                    return false;
                                }
                            });
                        }
                    };
                }
            });
            // when we load new invoice schedules either after filter or directly and if any invoice schedule is selected already than we need to trigger it's click event.
            if (totalselectedinv > 0) {
                for (var i = 0; i < totalselectedinv; i++) {
                    var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $gridInvoice.editChange.call($gridInvoice, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridInvoice.trigger(eData);
                }
            }
            totalselectedinv = 0;
            //end
            $gridInvoice.autoLoad = true;
            $($($gridInvoice.box)[0]).find('.w2ui-search-down').css('margin-top', '4px');
            $($($gridInvoice.box)[0]).find('.w2ui-search-clear').css('margin-top', '4px');
        };
        function bindInvoiceGridOnScroll(data) {
            var rows = [];
            var colkeys = [];

            if (data.length > 0) {
                colkeys = Object.keys(data[0]);
            }

            // check records exist in grid or not
            // for updating record id 
            var count = 0;
            if ($gridInvoice.records.length > 0) {
                count = $gridInvoice.records.length;
            };

            for (var r = 0; r < data.length; r++) {
                var singleRow = {};
                singleRow['recid'] = count + r;
                for (var c = 0; c < colkeys.length; c++) {
                    var colna = colkeys[c];
                    if (colna.toLower() == "selectrow") {
                        singleRow[colna] = false;
                    }
                    else {
                        singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
                    }
                }
                rows.push(singleRow);
            }

            // bind rows with grid
            w2utils.encodeTags(rows);
            $gridInvoice.add(rows);

            // when we load new invoice schedules either after filter or directly and if any invoice schedule is selected already than we need to trigger it's click event.
            if (totalselectedinv > 0) {
                for (var i = 0; i < totalselectedinv; i++) {
                    var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
                    $(chk[i]).prop('checked', true);
                    $gridInvoice.editChange.call($gridInvoice, chk[i], i, 0, event);
                    var eData = { "type": "click", "phase": "before", "target": "grid", "recid": i, "index": i, "isStopped": false, "isCan//celled": false, "onComplete": null };
                    $gridInvoice.trigger(eData);
                }
            }
            totalselectedinv = 0;
            //end

            rows = [];
            colkeys = [];
            //added window no to find grid to implement scroll  issue : scroll was not working properly
            $divInvoice.find('#grid_openformatgridinvoice_' + $self.windowNo + '_records').on('scroll', cartInvoiceScroll);
        };

        function paymentDoubleClicked(event) {
            if ($gridPayment.columns[event.column] == undefined) {
                event.preventDefault();
                return;
            }
            if ($gridPayment.columns[event.column].field == "AppliedAmt") {
                var getChanges = $gridPayment.getChanges();
                if (getChanges == undefined || getChanges.length == 0) {
                    $gridPayment.columns[event.column].editable = false;
                    return;
                }

                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element[0].SelectRow == undefined) {
                    $gridPayment.columns[event.column].editable = false;
                    return;
                }
                //changed column type from text to number on edit event
                $gridPayment.columns[event.column].editable = { type: 'number' }
                $gridPayment.columns[event.column].render = function (record, index, col_index) {

                    var val = 0;
                    if ($gridPayment.columns[event.column].field == "AppliedAmt") {
                        if (record.changes == undefined || record.changes.AppliedAmt == undefined || record.changes.SelectRow == undefined) {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.AppliedAmt == undefined)) {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.OpenAmt);
                                //val = record.changes.AppliedAmt;
                                record.changes.AppliedAmt = record.OpenAmt;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                            else {
                                val = 0;
                            }
                        }
                        else {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.AppliedAmt == undefined)) {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.OpenAmt);
                                //val = record.changes.AppliedAmt;
                                record.changes.AppliedAmt = record.OpenAmt;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                            else {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.changes.AppliedAmt, record.AppliedAmt);
                                //val = record.changes.AppliedAmt;
                                //record.changes.AppliedAmt = record.changes.AppliedAmt.toString();
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                        }
                    }
                    //if ($gridPayment.columns[event.column].field == "Writeoff") {
                    //    if (record.changes == undefined || record.changes.Writeoff == undefined || record.changes.SelectRow == undefined) {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Writeoff == undefined)) {
                    //            record.changes.Writeoff = checkcommaordot(event, record.Writeoff, record.Writeoff);
                    //            val = record.changes.Writeoff;
                    //        }
                    //        else {
                    //            val = 0;
                    //        }
                    //    }
                    //    else {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Writeoff == undefined)) {
                    //            record.changes.Writeoff = checkcommaordot(event, record.Writeoff, record.Writeoff);
                    //            val = record.changes.Writeoff;
                    //        }
                    //        else {
                    //            record.changes.Writeoff = checkcommaordot(event, record.changes.Writeoff, record.Writeoff);
                    //            val = record.changes.Writeoff;
                    //        }
                    //    }
                    //}
                    //if ($gridPayment.columns[event.column].field == "Discount") {
                    //    if (record.changes == undefined || record.changes.Discount == undefined || record.changes.SelectRow == undefined) {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Discount == undefined)) {
                    //            record.changes.Discount = checkcommaordot(event, record.Discount, record.Discount);
                    //            val = record.changes.Discount;
                    //        }
                    //        else {
                    //            val = 0;
                    //        }
                    //    }
                    //    else {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Discount == undefined)) {
                    //            record.changes.Discount = checkcommaordot(event, record.Discount, record.Discount);
                    //            val = record.changes.Discount;
                    //        }
                    //        else {
                    //            record.changes.Discount = checkcommaordot(event, record.changes.Discount, record.Discount);
                    //            val = record.changes.Discount;
                    //        }
                    //    }
                    //}
                    //else if ($gridPayment.columns[event.column].field == "Writeoff") {
                    //    if (record.changes == undefined || record.changes.Writeoff == undefined) {
                    //        val = 0;
                    //    }
                    //    else {
                    //        record.changes.Writeoff = checkcommaordot(event, record.changes.Writeoff, record.Writeoff);
                    //        val = record.changes.Writeoff;
                    //    }
                    //}
                    //else if ($gridPayment.columns[event.column].field == "Discount") {
                    //    if (record.changes == undefined || record.changes.Discount == undefined) {
                    //        val = 0;
                    //    }
                    //    else {
                    //        record.changes.Writeoff = checkcommaordot(event, record.changes.Writeoff, record.Discount);
                    //        val = record.changes.Discount;
                    //    }
                    //}

                    if (parseFloat(val) > 0 && parseFloat(val) > parseFloat(record.OpenAmt)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        val = parseFloat(record.OpenAmt);
                    }
                    else if (parseFloat(record.OpenAmt) < 0 && parseFloat(val) < parseFloat(record.OpenAmt)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        val = parseFloat(record.OpenAmt);
                    }
                    else if (parseFloat(record.OpenAmt) > 0 && parseFloat(val) < 0) {
                        VIS.ADialog.warn("VIS_AppliedAmtleszero");
                        val = parseFloat(record.OpenAmt);
                    }
                    //when the OpenAmt is Equal to AppliedAmt
                    if (record.OpenAmt != 0 && record.AppliedAmt != 0 && record.OpenAmt != "" && record.AppliedAmt != "" && record.changes != undefined && record.changes.SelectRow == true) {
                        if (record.OpenAmt == record.AppliedAmt && parseFloat(record.changes.AppliedAmt) == record.AppliedAmt) {
                            calculate();
                        }
                    }

                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                };
            }
            //End
        };

        function cashDoubleClicked(event) {
            if ($gridCashline.columns[event.column] == undefined) {
                event.preventDefault();
                return;
            }
            if ($gridCashline.columns[event.column].field == "AppliedAmt") {
                var getChanges = $gridCashline.getChanges();
                if (getChanges == undefined || getChanges.length == 0) {
                    $gridCashline.columns[event.column].editable = false;
                    return;
                }

                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element[0].SelectRow == undefined) {
                    $gridCashline.columns[event.column].editable = false;
                    return;
                }
                //changed column type from text to number on edit event
                $gridCashline.columns[event.column].editable = { type: 'number' }
                $gridCashline.columns[event.column].render = function (record, index, col_index) {

                    var val = 0;
                    if ($gridCashline.columns[event.column].field == "AppliedAmt") {
                        if (record.changes == undefined || record.changes.AppliedAmt == undefined || record.changes.SelectRow == undefined) {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.AppliedAmt == undefined)) {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.OpenAmt);
                                //val = record.changes.AppliedAmt;
                                record.changes.AppliedAmt = record.OpenAmt;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                            else {
                                val = 0;
                            }
                        }
                        else {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.AppliedAmt == undefined)) {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.OpenAmt);
                                //val = record.changes.AppliedAmt;
                                record.changes.AppliedAmt = record.OpenAmt;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                            else {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.changes.AppliedAmt, record.OpenAmt);
                                //val = record.changes.AppliedAmt;
                                //record.changes.AppliedAmt = record.OpenAmt;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                        }
                    }
                    //if ($gridCashline.columns[event.column].field == "Writeoff") {
                    //    if (record.changes == undefined || record.changes.Writeoff == undefined || record.changes.SelectRow == undefined) {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Writeoff == undefined)) {
                    //            record.changes.Writeoff = checkcommaordot(event, record.Writeoff, record.Writeoff);
                    //            val = record.changes.Writeoff;
                    //        }
                    //        else {
                    //            val = 0;
                    //        }
                    //    }
                    //    else {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Writeoff == undefined)) {
                    //            record.changes.Writeoff = checkcommaordot(event, record.Writeoff, record.Writeoff);
                    //            val = record.changes.Writeoff;
                    //        }
                    //        else {
                    //            record.changes.Writeoff = checkcommaordot(event, record.changes.Writeoff, record.Writeoff);
                    //            val = record.changes.Writeoff;
                    //        }
                    //    }
                    //}
                    //if ($gridCashline.columns[event.column].field == "Discount") {
                    //    if (record.changes == undefined || record.changes.Discount == undefined || record.changes.SelectRow == undefined) {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Discount == undefined)) {
                    //            record.changes.Discount = checkcommaordot(event, record.Discount, record.Discount);
                    //            val = record.changes.Discount;
                    //        }
                    //        else {
                    //            val = 0;
                    //        }
                    //    }
                    //    else {
                    //        if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Discount == undefined)) {
                    //            record.changes.Discount = checkcommaordot(event, record.Discount, record.Discount);
                    //            val = record.changes.Discount;
                    //        }
                    //        else {
                    //            record.changes.Discount = checkcommaordot(event, record.changes.Discount, record.Discount);
                    //            val = record.changes.Discount;
                    //        }
                    //    }
                    //}
                    //else if ($gridCashline.columns[event.column].field == "Writeoff") {
                    //    if (record.changes == undefined || record.changes.Writeoff == undefined) {
                    //        val = 0;
                    //    }
                    //    else {
                    //        record.changes.Writeoff = checkcommaordot(event, record.changes.Writeoff, record.Writeoff);
                    //        val = record.changes.Writeoff;
                    //    }
                    //}
                    //else if ($gridCashline.columns[event.column].field == "Discount") {
                    //    if (record.changes == undefined || record.changes.Discount == undefined) {
                    //        val = 0;
                    //    }
                    //    else {
                    //        record.changes.Discount = checkcommaordot(event, record.changes.Discount, record.Discount);
                    //        val = record.changes.Discount;
                    //    }
                    //}

                    if (parseFloat(val) > 0 && parseFloat(val) > parseFloat(record.OpenAmt)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        val = parseFloat(record.OpenAmt);
                    }
                    else if (parseFloat(record.OpenAmt) < 0 && parseFloat(val) < parseFloat(record.OpenAmt)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        val = parseFloat(record.OpenAmt);
                    }
                    else if (parseFloat(record.OpenAmt) > 0 && parseFloat(val) < 0) {
                        VIS.ADialog.warn("VIS_AppliedAmtleszero");
                        val = parseFloat(record.OpenAmt);
                    }
                    //when the OpenAmt is Equal to AppliedAmt
                    if (record.OpenAmt != 0 && record.AppliedAmt != 0 && record.OpenAmt != "" && record.AppliedAmt != "" && record.changes != undefined && record.changes.SelectRow == true) {
                        if (record.OpenAmt == record.AppliedAmt && parseFloat(record.changes.AppliedAmt) == record.AppliedAmt) {
                            calculate();
                        }
                    }

                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
                //End
            }
        };

        function invoiceDoubleClicked(event) {
            if ($gridInvoice.columns[event.column] == undefined) {
                event.preventDefault();
                return;
            }
            if ($gridInvoice.columns[event.column].field == "AppliedAmt" ||
                $gridInvoice.columns[event.column].field == "Writeoff" ||
                $gridInvoice.columns[event.column].field == "Discount") {
                var getChanges = $gridInvoice.getChanges();
                if (getChanges == undefined || getChanges.length == 0) {
                    $gridInvoice.columns[event.column].editable = false;
                    return;
                }

                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element[0].SelectRow == undefined) {
                    $gridInvoice.columns[event.column].editable = false;
                    return;
                }
                //changed column type from text to number on edit event
                $gridInvoice.columns[event.column].editable = { type: 'number' }
                $gridInvoice.columns[event.column].render = function (record, index, col_index) {

                    var val = 0;
                    if ($gridInvoice.columns[event.column].field == "AppliedAmt") {
                        if (record.changes == undefined || record.changes.AppliedAmt == undefined || record.changes.SelectRow == undefined) {

                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.AppliedAmt == undefined)) {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.Amount, record.Amount);
                                //val = record.changes.AppliedAmt;
                                record.changes.AppliedAmt = record.Amount;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                            else {
                                val = 0;
                            }
                        }
                        else {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.AppliedAmt == undefined)) {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.Amount, record.Amount);
                                //val = record.changes.AppliedAmt;
                                record.changes.AppliedAmt = record.Amount;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                            else {
                                //record.changes.AppliedAmt = checkcommaordot(event, record.changes.AppliedAmt, record.Amount);
                                //val = record.changes.AppliedAmt;
                                //record.changes.AppliedAmt = record.Amount;
                                val = convertAppliedAmtculture(record.changes.AppliedAmt.toString(), dotFormatter);
                            }
                        }
                    }
                    else if ($gridInvoice.columns[event.column].field == "Writeoff") {
                        if (record.changes == undefined || record.changes.Writeoff == undefined || record.changes.SelectRow == undefined) {

                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Writeoff == undefined)) {
                                //record.changes.Writeoff = checkcommaordot(event, record.Writeoff, record.Writeoff);
                                //val = record.changes.Writeoff;
                                record.changes.Writeoff = record.Writeoff;
                                val = convertAppliedAmtculture(record.changes.Writeoff.toString(), dotFormatter);
                            }
                            else {
                                val = 0;
                            }
                        }
                        else {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Writeoff == undefined)) {
                                //record.changes.Writeoff = checkcommaordot(event, record.Writeoff, record.Writeoff);
                                //val = record.changes.Writeoff;
                                record.changes.Writeoff = record.Writeoff;
                                val = convertAppliedAmtculture(record.changes.Writeoff.toString(), dotFormatter);
                            }
                            else {
                                //record.changes.Writeoff = checkcommaordot(event, record.changes.Writeoff, record.Writeoff);
                                //val = record.changes.Writeoff;
                                //record.changes.Writeoff = record.Writeoff;
                                val = convertAppliedAmtculture(record.changes.Writeoff.toString(), dotFormatter);
                            }
                        }
                    }
                    else if ($gridInvoice.columns[event.column].field == "Discount") {
                        if (record.changes == undefined || record.changes.Discount == undefined || record.changes.SelectRow == undefined) {

                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Discount == undefined)) {
                                //record.changes.Discount = checkcommaordot(event, record.Discount, record.Discount);
                                //val = record.changes.Discount;
                                record.changes.Discount = record.Discount;
                                val = convertAppliedAmtculture(record.changes.Discount.toString(), dotFormatter);
                            }
                            else {
                                val = 0;
                            }
                        }
                        else {
                            if ((record.changes != undefined) && (record.changes.SelectRow != undefined) && (record.changes.SelectRow == true && record.changes.Discount == undefined)) {
                                //record.changes.Discount = checkcommaordot(event, record.Discount, record.Discount);
                                //val = record.changes.Discount;
                                record.changes.Discount = record.Discount;
                                val = convertAppliedAmtculture(record.changes.Discount.toString(), dotFormatter);
                            }
                            else {
                                //record.changes.Discount = checkcommaordot(event, record.changes.Discount, record.Discount);
                                //val = record.changes.Discount;
                                val = convertAppliedAmtculture(record.changes.Discount.toString(), dotFormatter);
                            }
                        }
                    }
                    if (parseFloat(val) > 0 && parseFloat(val) > parseFloat(record.Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        val = parseFloat(record.Amount);
                    }
                    else if (parseFloat(record.Amount) < 0 && parseFloat(val) < parseFloat(record.Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        val = parseFloat(record.Amount);
                    }
                    else if (parseFloat(record.Amount) > 0 && parseFloat(val) < 0) {
                        VIS.ADialog.warn("VIS_AppliedAmtleszero");
                        val = parseFloat(record.Amount);
                    }
                    //when the OpenAmt is Equal to AppliedAmt
                    if (record.Amount != 0 && record.AppliedAmt != 0 && record.OpenAmt != "" && record.AppliedAmt != "" && record.changes != undefined && record.changes.SelectRow == true) {
                        if (parseFloat(record.Amount) === record.AppliedAmt && parseFloat(record.changes.AppliedAmt) == record.AppliedAmt) {
                            if (record.changes.Discount != 0 || record.changes.Writeoff != 0) {
                                var totAmt = parseFloat(record.changes.Discount) + parseFloat(record.changes.Writeoff) + parseFloat(record.changes.AppliedAmt);
                                if (Math.abs(totAmt) > Math.abs(parseFloat(record.Amount))) {
                                    VIS.ADialog.warn("AppliedAmtgrtr");
                                    if ($gridInvoice.columns[event.column].field == "Discount") {
                                        record.changes.Discount = 0;
                                        val = record.changes.Discount;
                                    }
                                    else if ($gridInvoice.columns[event.column].field == "Writeoff") {
                                        record.changes.Writeoff = 0;
                                        val = record.changes.Writeoff;
                                    }
                                    else if ($gridInvoice.columns[event.column].field == "AppliedAmt") {
                                        record.changes.AppliedAmt = 0;
                                        val = record.changes.AppliedAmt;
                                    }
                                }
                            }
                            calculate();
                        }
                    }

                    return parseFloat(val).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
                }
            }
            //End
        };
        //handle culture for AppliedAmt, writeOffAmt and DiscountAmt
        function convertAppliedAmtculture(appliedAmount, dotFormatter) {
            var val = 0;
            typeof appliedAmount === "string" ? appliedAmount : appliedAmount = appliedAmount.toString();
            if (!dotFormatter) {
                if (appliedAmount.contains("−") && !appliedAmount.contains(".")) {
                    appliedAmount = (-1 * format.GetConvertedNumber(appliedAmount, dotFormatter)).toString();
                }
                else if (appliedAmount.contains(",")) {
                    appliedAmount = format.GetConvertedNumber(appliedAmount, dotFormatter).toString();
                }
                val = appliedAmount != "" ? appliedAmount : val;
            }
            else {
                if (appliedAmount.contains("−")) {
                    appliedAmount = (-1 * format.GetConvertedNumber(appliedAmount, dotFormatter)).toString();
                }
                else if (appliedAmount.contains(".")) {
                    appliedAmount = format.GetConvertedNumber(appliedAmount, dotFormatter).toString();
                }
                val = appliedAmount != "" ? appliedAmount : val;
            }
            return val;
        }
        // function to check comma or dot from given value and return new value
        function checkcommaordot(event, val, amt) {
            var foundComma = false;
            event.value_new = VIS.Utility.Util.getValueOfString(val);
            if (event.value_new.contains(".")) {
                foundComma = true;
                var indices = [];
                for (var i = 0; i < event.value_new.length; i++) {
                    if (event.value_new[i] === ".")
                        indices.push(i);
                }
                if (indices.length > 1) {
                    event.value_new = removeAllButLast(event.value_new, '.');
                }
            }
            if (event.value_new.contains(",")) {
                if (foundComma) {
                    event.value_new = removeAllButLast(event.value_new, ',');
                }
                else {
                    var indices = [];
                    for (var i = 0; i < event.value_new.length; i++) {
                        if (event.value_new[i] === ",")
                            indices.push(i);
                    }
                    if (indices.length > 1) {
                        event.value_new = removeAllButLast(event.value_new, ',');
                    }
                    else {
                        event.value_new = event.value_new.replace(",", ".");
                    }
                }
            }
            if (event.value_new == "") {
                event.value_new = "0";
            }
            return event.value_new;
        };

        // Remove all seperator but only bring last seperator
        function removeAllButLast(amt, seprator) {
            var parts = amt.split(seprator);
            amt = parts.slice(0, -1).join('') + '.' + parts.slice(-1);
            if (amt.indexOf('.') == (amt.length - 1)) {
                amt = amt.replace(".", "");
            }
            return amt;
        };

        // Remove all seperator but only bring last seperator
        function removeAllButLast1(amt, seprator) {
            var parts = amt.split(seprator);
            amt = parts.slice(0, -1).join('') + parts.slice(-1);
            if (amt.indexOf((seprator == '.' ? ',' : '.')) == (amt.length - 1)) {
                amt = amt.replace((seprator == '.' ? ',' : '.'), "");
            }
            return amt;
        };
        //added for gl-allocation
        //function glDoubleClicked(event) {
        //    if (glLineGrid.columns[event.column].field == "AppliedAmt") {
        //        var getChanges = glLineGrid.getChanges();
        //        if (getChanges == undefined || getChanges.length == 0) {
        //            return;
        //        }

        //        var element = $.grep(getChanges, function (ele, index) {
        //            return parseInt(ele.recid) == parseInt(event.recid);
        //        });
        //        if (element == null || element == undefined || element[0].SelectRow == undefined) {
        //            glLineGrid.columns[event.column].editable = false;
        //            return;
        //        }

        //        /*** after discussion with ashish we finalized that gl journal applied amount will not be editable because we can not split gl 
        //             journal and we can not use gl journal partialy **/

        //        //if (element[0].SelectRow == true) {
        //        //    glLineGrid.columns[event.column].editable = { type: 'text' };
        //        //}
        //        //else {
        //        glLineGrid.columns[event.column].editable = false;
        //        //    return;
        //        //}
        //    }
        //};
        //end

        function invoiceCellClicked(event) {
            var colIndex = $gridInvoice.getColumn('AppliedAmt', true);
            //get the index of SelectRow Column checkbox from grid
            var selectColIndex = $gridInvoice.getColumn('SelectRow', true);
            if (event.column == 0 || event.column == null) {
                var getChanges = $gridInvoice.getChanges();
                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element.length == 0 || element[0].SelectRow == undefined) {
                    //Set value to 0 when element is null
                    $gridInvoice.records[event.recid]["AppliedAmt"] = 0;
                    $gridInvoice.records[event.recid]["Writeoff"] = 0;
                    $gridInvoice.records[event.recid]["Discount"] = 0;
                    $gridInvoice.refreshCell(event.recid, "AppliedAmt");
                    $gridInvoice.refreshCell(event.recid, "Writeoff");
                    $gridInvoice.refreshCell(event.recid, "Discount");
                    getMaxDate();
                }
                else {
                    if (element[0].SelectRow == true) {
                        // when we select a record, check conversion type is same or not.
                        // if not then not to select this record
                        if (C_ConversionType_ID == 0) {
                            C_ConversionType_ID = $gridInvoice.get(event.recid).C_ConversionType_ID;
                        }
                        /*VIS_0045: conversion type matched when allocate with GL and Cash*/
                        else if ((!readOnlyCash || !readOnlyGL) && C_ConversionType_ID != $gridInvoice.get(event.recid).C_ConversionType_ID) {

                            var $x = document.getElementsByName(event.target);
                            if ($x.length > 0)
                                $x = $x[0];

                            var $z = $($x).find("#grid_openformatgridinvoice_rec_" + event.recid + "");

                            if ($z.length > 0)
                                $z = $z[0];
                            if ($($z).find("input:checked").prop('checked')) {
                                $($z).find("input:checked").prop('checked', false);
                            }

                            //changed 0 to event.recid because we need to refresh selected row before it was working only for 1st row.
                            $gridInvoice.get(event.recid).changes = false;
                            $gridInvoice.columns[colIndex].editable = false;
                            $gridInvoice.get(event.recid).changes.AppliedAmt = "0";
                            $gridInvoice.refreshCell(event.recid, "AppliedAmt");
                            $gridInvoice.get(event.recid).changes.Discount = "0";
                            $gridInvoice.refreshCell(event.recid, "Discount");
                            $gridInvoice.get(event.recid).changes.Writeoff = "0";
                            $gridInvoice.refreshCell(event.recid, "Writeoff");
                            VIS.ADialog.warn(("VIS_ConversionNotMatched"));
                        }
                        if ($vchkMultiCurrency.is(':checked')) {
                            if ($conversionDate.val() == "") {
                                VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                                $gridInvoice.get(event.recid).changes = false;
                                $gridInvoice.unselect(event.recid);
                                $gridInvoice.columns[colIndex].editable = false;
                                $gridInvoice.get(event.recid).changes.AppliedAmt = "0";
                                $gridInvoice.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $gridInvoice.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $gridInvoice.editChange.call($gridInvoice, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $gridInvoice.trigger(eData);
                                return false;
                            }
                            var ConversnDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");
                            $dateAcct.val(Globalize.format(new Date(ConversnDate), "yyyy-MM-dd"));
                        }
                        else {
                            getMaxDate();
                        }
                    }
                    else {
                        $gridInvoice.unselect(event.recid);
                        $gridInvoice.columns[colIndex].editable = false;
                        //changed 0 to event.recid because we need to refresh selected row before it was working only for 1st row.
                        $gridInvoice.get(event.recid).changes.AppliedAmt = "0";
                        $gridInvoice.refreshCell(event.recid, "AppliedAmt");
                        $gridInvoice.get(event.recid).changes.Discount = "0";
                        $gridInvoice.refreshCell(event.recid, "Discount");
                        $gridInvoice.get(event.recid).changes.Writeoff = "0";
                        $gridInvoice.refreshCell(event.recid, "Writeoff");
                    }

                }
                //check weather someone clicked on select column checkbox in grid
                if ((selectColIndex == event.column || event.column == null) && ((element[0] != undefined) && !(element[0].SelectRow == undefined))) {
                    $gridInvoice.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount);
                }
                tableChanged(event.recid, event.column, true, false);
            }
        };

        function paymentCellClicked(event) {
            if (readOnlyPayment) {
                event.preventDefault();
                return;
            }
            var colIndex = $gridPayment.getColumn('AppliedAmt', true);
            //get the index of SelectRow Column checkbox from grid
            var selectColIndex = $gridPayment.getColumn('SelectRow', true);
            if (event.column == 0 || event.column == null) {
                var getChanges = $gridPayment.getChanges();
                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element.length == 0 || element[0].SelectRow == undefined) {
                    //Set value to 0 when element is null
                    $gridPayment.records[event.recid]["AppliedAmt"] = 0;
                    //$gridPayment.records[event.recid]["Writeoff"] = 0;
                    //$gridPayment.records[event.recid]["Discount"] = 0;
                    $gridPayment.refreshCell(event.recid, "AppliedAmt");
                    getMaxDate();
                }
                else {
                    if (element[0].SelectRow == true) {
                        // Get conversion type of selected record
                        let elementConversion = $.grep(getChanges, function (ele, index) {
                            return ele.SelectRow == true;
                        });

                        // when multicurrency true, then get conversion type from parameter
                        if ($vchkMultiCurrency.is(':checked')) {
                            C_ConversionType_ID = $vConversionType.getValue();
                        }
                        else if (readOnlyGL && (elementConversion != null && elementConversion.length > 0)) {
                            /* when not allocated with GL, and first payment selected then overwrite conversion type with payment record */
                            C_ConversionType_ID = $gridPayment.get(elementConversion[0].recid).C_ConversionType_ID;
                        }
                        // when we select a record, check conversion type is same or not.
                        // if not then not to select this record
                        if (C_ConversionType_ID == 0) {
                            C_ConversionType_ID = $gridPayment.get(event.recid).C_ConversionType_ID;
                        }
                        else if (C_ConversionType_ID != $gridPayment.get(event.recid).C_ConversionType_ID) {

                            var $x = document.getElementsByName(event.target);
                            if ($x.length > 0)
                                $x = $x[0];

                            var $z = $($x).find("#grid_openformatgrid_rec_" + event.recid + "");

                            if ($z.length > 0)
                                $z = $z[0];
                            if ($($z).find("input:checked").prop('checked')) {
                                $($z).find("input:checked").prop('checked', false);
                            }

                            $gridPayment.get(event.recid).changes = false;
                            $gridPayment.unselect(event.recid);
                            $gridPayment.columns[colIndex].editable = false;
                            $gridPayment.get(event.recid).changes.AppliedAmt = "0";
                            $gridPayment.refreshCell(event.recid, "AppliedAmt");
                            VIS.ADialog.warn(("VIS_ConversionNotMatched"));
                        }
                        if ($vchkMultiCurrency.is(':checked')) {
                            if ($conversionDate.val() == "") {
                                VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                                $gridPayment.get(event.recid).changes = false;
                                $gridPayment.unselect(event.recid);
                                $gridPayment.columns[colIndex].editable = false;
                                $gridPayment.get(event.recid).changes.AppliedAmt = "0";
                                $gridPayment.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $gridPayment.editChange.call($gridPayment, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $gridPayment.trigger(eData);
                                return false;
                            }
                            var DATEACCT = $gridPayment.records[event.recid].DATEACCT;
                            var ConversnDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");
                            DATEACCT = Globalize.format(new Date(DATEACCT), "yyyy-MM-dd");
                            if (ConversnDate != DATEACCT) {
                                VIS.ADialog.warn("VIS_SelectSameDate");
                                var colIndex = $gridPayment.getColumn('DATEACCT', true);
                                $gridPayment.get(event.recid).changes = false;
                                $gridPayment.unselect(event.recid);
                                $gridPayment.columns[colIndex].editable = false;
                                $gridPayment.get(event.recid).changes.AppliedAmt = "0";
                                $gridPayment.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $gridPayment.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $gridPayment.editChange.call($gridPayment, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $gridPayment.trigger(eData);
                                return false;
                            }
                        }
                    }
                    else {
                        $gridPayment.unselect(event.recid);
                        $gridPayment.columns[colIndex].editable = false;
                        $gridPayment.get(event.recid).changes.AppliedAmt = "0";
                        $gridPayment.refreshCell(event.recid, "AppliedAmt");
                    }

                }
                //check weather someone clicked on select column checkbox in grid
                if ((selectColIndex == event.column || event.column == null) && ((element[0] != undefined) && !(element[0].SelectRow == undefined))) {
                    $gridPayment.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($gridPayment.get(event.recid).OpenAmt);
                }
                tableChanged(event.recid, event.column, false, false);
            }
        };

        function cashCellClicked(event) {
            if (readOnlyCash) {
                event.preventDefault();
                return;
            }
            var colIndex = $gridCashline.getColumn('AppliedAmt', true);
            //get the index of SelectRow Column checkbox from grid
            var selectColIndex = $gridCashline.getColumn('SelectRow', true);
            if (event.column == 0 || event.column == null) {
                var getChanges = $gridCashline.getChanges();
                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element.length == 0 || element[0].SelectRow == undefined) {
                    //Set value to 0 when element is null
                    $gridCashline.records[event.recid]["AppliedAmt"] = 0;
                    //$gridCashline.records[event.recid]["Writeoff"] = 0;
                    //$gridCashline.records[event.recid]["Discount"] = 0;
                    $gridCashline.refreshCell(0, "AppliedAmt");
                }
                else {
                    if (element[0].SelectRow == true) {
                        // when we select a record, check conversion type is same or not.
                        // if not then not to select this record
                        if (C_ConversionType_ID == 0) {
                            C_ConversionType_ID = $gridCashline.get(event.recid).C_ConversionType_ID;
                        }
                        else if (C_ConversionType_ID != $gridCashline.get(event.recid).C_ConversionType_ID) {

                            var $x = document.getElementsByName(event.target);
                            if ($x.length > 0)
                                $x = $x[0];

                            var $z = $($x).find("#grid_openformatgridcash_rec_" + event.recid + "");

                            if ($z.length > 0)
                                $z = $z[0];
                            if ($($z).find("input:checked").prop('checked')) {
                                $($z).find("input:checked").prop('checked', false);
                            }

                            $gridCashline.get(event.recid).changes = false;
                            $gridCashline.unselect(event.recid);
                            $gridCashline.columns[colIndex].editable = false;
                            $gridCashline.refreshCell(0, "AppliedAmt");
                            VIS.ADialog.warn(("VIS_ConversionNotMatched"));
                        }

                        if ($vchkMultiCurrency.is(':checked')) {
                            if ($conversionDate.val() == "") {
                                VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                                $gridCashline.get(event.recid).changes = false;
                                $gridCashline.unselect(event.recid);
                                $gridCashline.columns[colIndex].editable = false;
                                $gridCashline.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $gridCashline.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $gridCashline.editChange.call($gridCashline, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $gridCashline.trigger(eData);
                                return false;
                            }
                            var DATEACCT = $gridCashline.records[event.recid].DATEACCT;
                            var ConversnDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");
                            DATEACCT = Globalize.format(new Date(DATEACCT), "yyyy-MM-dd");
                            if (ConversnDate != DATEACCT) {
                                VIS.ADialog.warn("VIS_SelectSameDate");
                                var colIndex = $gridCashline.getColumn('DATEACCT', true);
                                $gridCashline.get(event.recid).changes = false;
                                $gridCashline.unselect(event.recid);
                                $gridCashline.columns[colIndex].editable = false;
                                $gridCashline.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $gridCashline.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $gridCashline.editChange.call($gridCashline, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $gridCashline.trigger(eData);
                                return false;
                            }
                        }
                    }
                    else {
                        $gridCashline.unselect(event.recid);
                        $gridCashline.columns[colIndex].editable = false;
                        $gridCashline.get(0).changes.AppliedAmt = "0";
                        $gridCashline.refreshCell(0, "AppliedAmt");
                    }
                }
                //check weather someone clicked on select column checkbox in grid
                if ((selectColIndex == event.column || event.column == null) && ((element[0] != undefined) && !(element[0].SelectRow == undefined))) {
                    //$gridCashline.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).Amount);
                    $gridCashline.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt);
                }
                tableChanged(event.recid, event.column, false, true);
            }
        };

        // whenever click event will fire in GL Line grid this function will execute
        function glCellClicked(event) {
            if (readOnlyGL) {
                event.preventDefault();
                return;
            }
            var colIndex = $glLineGrid.getColumn('AppliedAmt', true);
            //get the index of SelectRow Column checkbox from grid
            var selectColIndex = $glLineGrid.getColumn('SelectRow', true);
            if (event.column == 0 || event.column == null) {
                var getChanges = $glLineGrid.getChanges();
                var element = $.grep(getChanges, function (ele, index) {
                    return parseInt(ele.recid) == parseInt(event.recid);
                });
                if (element == null || element[0] == undefined || element.length == 0 || element[0].SelectRow == undefined) {
                    //Set value to 0 when element is null
                    $glLineGrid.records[event.recid]["AppliedAmt"] = 0;
                    $glLineGrid.refreshCell(event.recid, "AppliedAmt");
                    getMaxDate();
                    //when unselect the record it will remove that record in getGLChanges list.
                    for (var x = 0; x < getGLChanges.length; x++) {
                        if (getGLChanges[x].recid == event.recid) {
                            getGLChanges.splice(x, 1);
                        }
                    }
                }
                else {
                    if (element[0].SelectRow == true) {
                        // when we select a record, check conversion type is same or not.
                        // if not then not to select this record
                        if (C_ConversionType_ID == 0) {
                            C_ConversionType_ID = $glLineGrid.get(event.recid).C_ConversionType_ID;
                        }
                        else if (C_ConversionType_ID != $glLineGrid.get(event.recid).C_ConversionType_ID) {

                            var $x = document.getElementsByName(event.target);
                            if ($x.length > 0)
                                $x = $x[0];

                            var $z = $($x).find("#grid_openformatglgrid_rec_" + event.recid + "");

                            if ($z.length > 0)
                                $z = $z[0];
                            if ($($z).find("input:checked").prop('checked')) {
                                $($z).find("input:checked").prop('checked', false);
                            }

                            $glLineGrid.get(event.recid).changes = false;
                            $glLineGrid.unselect(event.recid);
                            $glLineGrid.columns[colIndex].editable = false;
                            $glLineGrid.get(event.recid).changes.AppliedAmt = "0";
                            $glLineGrid.refreshCell(event.recid, "AppliedAmt");
                            VIS.ADialog.warn(("VIS_ConversionNotMatched"));
                        }
                        if ($vchkMultiCurrency.is(':checked')) {
                            if ($conversionDate.val() == "") {
                                VIS.ADialog.warn("VIS_SlctcnvrsnDate");
                                $glLineGrid.get(event.recid).changes = false;
                                $glLineGrid.unselect(event.recid);
                                $glLineGrid.columns[colIndex].editable = false;
                                $glLineGrid.get(event.recid).changes.AppliedAmt = "0";
                                $glLineGrid.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $glLineGrid.editChange.call($glLineGrid, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $glLineGrid.trigger(eData);
                                return false;
                            }
                            var DATEACCT = $glLineGrid.records[event.recid].DATEACCT;
                            var ConversnDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");
                            DATEACCT = Globalize.format(new Date(DATEACCT), "yyyy-MM-dd");
                            if (ConversnDate != DATEACCT) {
                                VIS.ADialog.warn("VIS_SelectSameDate");
                                var colIndex = $glLineGrid.getColumn('DATEACCT', true);
                                $glLineGrid.get(event.recid).changes = false;
                                $glLineGrid.unselect(event.recid);
                                $glLineGrid.columns[colIndex].editable = false;
                                $glLineGrid.get(event.recid).changes.AppliedAmt = "0";
                                $glLineGrid.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $glLineGrid.editChange.call($glLineGrid, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $glLineGrid.trigger(eData);
                                return false;
                            }
                        }

                        //Restricting to Select both Credit and Debit Amount's in GL Journal gird
                        getGLChanges.push(element[0]);
                        if (getGLChanges.length == 1) {
                            prevAmt = $glLineGrid.get(event.recid).OpenAmount;
                        }
                        else if (getGLChanges.length > 1) {
                            presAmt = $glLineGrid.get(event.recid).OpenAmount;
                            if (!(parseFloat(prevAmt) > 0 && parseFloat(presAmt) > 0) && !(parseFloat(prevAmt) < 0 && parseFloat(presAmt) < 0)) {
                                if (!$glSelectAll.is(':checked')) {
                                    VIS.ADialog.info("", true, VIS.translatedTexts.CreditOrDebit);
                                }
                                $glLineGrid.get(event.recid).changes = false;
                                $glLineGrid.unselect(event.recid);
                                $glLineGrid.columns[colIndex].editable = false;
                                $glLineGrid.get(event.recid).changes.AppliedAmt = "0";
                                $glLineGrid.refreshCell(event.recid, "AppliedAmt");
                                var chk = $('#grid_' + $glLineGrid.name + '_records td[col="0"]').find('input[type="checkbox"]');
                                $(chk[event.recid]).prop('checked', false);
                                $glLineGrid.editChange.call($glLineGrid, chk[event.recid], event.recid, 0, event);
                                var eData = { "type": "click", "phase": "before", "target": "grid", "recid": event.recid, "index": event.recid, "isStopped": false, "isCan//celled": false, "onComplete": null };
                                $glLineGrid.trigger(eData);
                                for (var x = 0; x < getGLChanges.length; x++) {
                                    if (getGLChanges[x].recid == event.recid) {
                                        getGLChanges.splice(x, 1);
                                    }
                                }
                                if (!$glSelectAll.is(':checked')) {
                                    return false;
                                }
                            }
                        }
                    }
                    else {
                        $glLineGrid.unselect(event.recid);
                        $glLineGrid.columns[colIndex].editable = false;
                        $glLineGrid.get(event.recid).changes.AppliedAmt = "0";
                        $glLineGrid.refreshCell(event.recid, "AppliedAmt");
                    }

                }
                //check weather someone clicked on select column checkbox in grid
                if ((selectColIndex == event.column || event.column == null) && ((element[0] != undefined) && !(element[0].SelectRow == undefined))) {
                    $glLineGrid.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($glLineGrid.get(event.recid).OpenAmount);
                }
                glTableChanged(event.recid, event.column);
            }
        };
        function paymentCellChanged(event) {
            if (readOnlyPayment) {
                event.preventDefault();
                return;
            }
            var colIndex = $gridPayment.getColumn('AppliedAmt', true);
            var selectColIndex = $gridPayment.getColumn('SelectRow', true);
            var changedValue = 0;

            if ($gridPayment.columns[colIndex].editable == undefined) {
                return;
            }

            // when Applied amount cell changed 
            changedValue = event.value_new != "" ? event.value_new : changedValue;
            if (event.column == colIndex) {
                if (!dotFormatter) {
                    if (!changedValue.toString().contains("−") && !changedValue.toString().contains(",")) {
                        if (!isNaN(changedValue))
                            changedValue = format.GetFormatedValue(changedValue, "init", dotFormatter).toString();
                    }
                    else if (changedValue.toString().contains("−")) {
                        changedValue = (-1 * format.GetConvertedNumber(changedValue, dotFormatter)).toString();
                    }
                    else {
                        changedValue = format.GetConvertedNumber(changedValue, dotFormatter).toString();
                    }
                }
                else {
                    if (changedValue.toString().contains("−")) {
                        changedValue = (-1 * format.GetConvertedNumber(changedValue, dotFormatter)).toString();
                    }
                }
                //validate changed Amount is valid number or not
                if (isNaN(changedValue)) {
                    VIS.ADialog.warn("VIS_ValidAmount");
                    event.preventDefault();
                    return;
                }
            }

            if ($gridPayment.getChanges(event.recid) != undefined && $gridPayment.getChanges(event.recid).length > 0 && $gridPayment.get(event.recid).changes) {
                // if changes are there like  checkbox is cheked, then we have to set value in changes becoz textbox in grid show data from changes...
                if (VIS.Utility.Util.getValueOfDecimal(changedValue) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) > VIS.Utility.Util.getValueOfDecimal($gridPayment.get(event.recid).OpenAmt)) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    event.preventDefault();
                    return;
                }
                else if (VIS.Utility.Util.getValueOfDecimal($gridPayment.get(event.recid).OpenAmt) < 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < VIS.Utility.Util.getValueOfDecimal($gridPayment.get(event.recid).OpenAmt)) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    event.preventDefault();
                    return;
                }
                //added for gl-allocation
                else if (VIS.Utility.Util.getValueOfDecimal($gridPayment.get(event.recid).OpenAmt) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < 0) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    event.preventDefault();
                    return;
                }

                $gridPayment.get(event.recid).changes.AppliedAmt = event.value_new != "" ? event.value_new : changedValue;
                $gridPayment.refreshCell(event.recid, "AppliedAmt");
            }
            else {
                if (event.column > 0) {
                    $gridPayment.set(event.recid, { AppliedAmt: event.value_new != "" ? event.value_new : changedValue });
                }
            }
            if (event.column == selectColIndex || event.column == null) {
                if ($vchkMultiCurrency.is(':checked')) {
                    if (VIS.Utility.Util.getValueOfDecimal($gridPayment.get(event.recid).OpenAmt) == 0) {
                        VIS.ADialog.warn("NotFoundCurrencyRate");
                        event.preventDefault();
                        return;
                    }
                    ////logic to not set greater appliedAmount then open amount
                    //else if (parseFloat($gridPayment.get(event.index).OpenAmt) > parseFloat($gridPayment.get(event.index).AppliedAmt)) {

                    //}
                    //else {
                    //    $gridPayment.set(0, { "AppliedAmt": $gridPayment.get(event.index).OpenAmt });
                    //}
                }
                //else {
                //    //logic to not set greater appliedAmount then open amount
                //    if (parseFloat($gridPayment.get(event.index).OpenAmt) > parseFloat($gridPayment.get(event.index).AppliedAmt)) {

                //    }
                //    else {
                //        $gridPayment.set(0, { "AppliedAmt": $gridPayment.get(event.index).OpenAmt });
                //    }
                //}
            }
            //when AppliedAmt cell changed then only this function will call
            if (colIndex == event.column) {
                tableChanged(event.index, event.column, false, false);
            }
        };

        //added for gl-allocation
        function glCellChanged(event) {
            if (readOnlyGL) {
                event.preventDefault();
                return;
            }
            var colIndex = $glLineGrid.getColumn('AppliedAmt', true);

            if ($glLineGrid.columns[colIndex].editable == undefined) {
                return;
            }

            if ($glLineGrid.getChanges(event.recid) != undefined && $glLineGrid.getChanges(event.recid).length > 0 && $glLineGrid.get(event.recid).changes) {
                // if changes are there like  checkbox is cheked, then we have to set value in changes becoz textbox in grid show data from changes...
                if (VIS.Utility.Util.getValueOfDecimal(event.value_new) > 0 && VIS.Utility.Util.getValueOfDecimal(event.value_new) > VIS.Utility.Util.getValueOfDecimal($glLineGrid.get(event.recid).OpenAmount)) {
                    $glLineGrid.set(0, { "AppliedAmt": VIS.Utility.Util.getValueOfDecimal($glLineGrid.get(event.recid).OpenAmount) });
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    event.preventDefault();
                    return;
                }
                else if (VIS.Utility.Util.getValueOfDecimal($glLineGrid.get(event.recid).OpenAmount) < 0 && VIS.Utility.Util.getValueOfDecimal(event.value_new) < VIS.Utility.Util.getValueOfDecimal($glLineGrid.get(event.recid).OpenAmount)) {
                    $glLineGrid.set(0, { "AppliedAmt": VIS.Utility.Util.getValueOfDecimal($glLineGrid.get(event.recid).OpenAmount) });
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    event.preventDefault();
                    return;
                }

                if (colIndex == event.column)
                    $glLineGrid.get(event.recid).changes.AppliedAmt = event.value_new;

                $glLineGrid.refreshCell(event.recid, "AppliedAmt");
            }
            else {
                if (event.column > 0) {
                    $glLineGrid.set(event.recid, { AppliedAmt: event.value_new });
                }
            }

            if (event.column == colIndex) {
                //logic to not set greater appliedAmount then open amount
                if (parseFloat($glLineGrid.get(event.recid).OpenAmount) > parseFloat($glLineGrid.get(event.recid).AppliedAmt)) {

                }
                else {
                    $glLineGrid.set(0, { "AppliedAmt": $glLineGrid.get(event.recid).OpenAmount });
                }
                glTableChanged(event.index, event.column);
            }
            //calculate();
        };
        //end

        function cashCellChanged(event) {
            if (readOnlyCash) {
                event.preventDefault();
                return;
            }
            var colIndex = $gridCashline.getColumn('AppliedAmt', true);
            var selectColIndex = $gridCashline.getColumn('SelectRow', true);
            var changedValue = 0;

            if ($gridCashline.columns[colIndex].editable == undefined) {
                return;
            }

            // when Applied amount cell changed 
            changedValue = event.value_new != "" ? event.value_new : changedValue;
            if (event.column == colIndex) {
                if (!dotFormatter) {
                    if (!changedValue.toString().contains("−") && !changedValue.toString().contains(",")) {
                        if (!isNaN(changedValue))
                            changedValue = format.GetFormatedValue(changedValue, "init", dotFormatter).toString();
                    }
                    else if (changedValue.toString().contains("−")) {
                        changedValue = (-1 * format.GetConvertedNumber(changedValue, dotFormatter)).toString();
                    }
                    else {
                        changedValue = format.GetConvertedNumber(changedValue, dotFormatter).toString();
                    }
                }
                else {
                    if (changedValue.toString().contains("−")) {
                        changedValue = (-1 * format.GetConvertedNumber(changedValue, dotFormatter)).toString();
                    }
                }
                //validate changed Amount is valid number or not
                if (isNaN(changedValue)) {
                    VIS.ADialog.warn("VIS_ValidAmount");
                    event.preventDefault();
                    return;
                }
            }

            if ($gridCashline.getChanges(event.recid) != undefined && $gridCashline.getChanges(event.recid).length > 0 && $gridCashline.get(event.recid).changes) {
                // if changes are there like  checkbox is cheked, then we have to set value in changes becoz textbox in grid show data from changes...
                if (VIS.Utility.Util.getValueOfDecimal(changedValue) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) > VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt)) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    //$gridCashline.get(event.recid).changes.AppliedAmt = $gridCashline.get(event.recid).OpenAmt;
                    event.preventDefault();
                    return;
                }
                else if (VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt) < 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt)) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    //$gridCashline.get(event.recid).changes.AppliedAmt = $gridCashline.get(event.recid).OpenAmt;
                    event.preventDefault();
                    return;
                }
                //added for gl-allocation
                else if (VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < 0) {
                    //$gridCashline.set(0, { "AppliedAmt": VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt) });
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    event.preventDefault();
                    return;
                }
                $gridCashline.get(event.recid).changes.AppliedAmt = event.value_new != "" ? event.value_new : changedValue;
                $gridCashline.refreshCell(event.recid, "AppliedAmt");
            }
            else {
                if (event.column > 0) {
                    $gridCashline.set(event.recid, { AppliedAmt: event.value_new != "" ? event.value_new : changedValue });
                }
            }
            if (event.column == selectColIndex || event.column == null) {
                if ($vchkMultiCurrency.is(':checked')) {
                    if (VIS.Utility.Util.getValueOfDecimal($gridCashline.get(event.recid).OpenAmt) == 0) {
                        VIS.ADialog.warn("NotFoundCurrencyRate");
                        event.preventDefault();
                        return;
                    }
                }
            }
            //when AppliedAmt cell changed then only this function will call
            if (colIndex == event.column) {
                tableChanged(event.index, event.column, false, true);
            }
        };

        function invoiceCellChanged(event) {

            var colIndex = $gridInvoice.getColumn('AppliedAmt', true);
            var wcolIndex = $gridInvoice.getColumn('Writeoff', true);
            var dcolIndex = $gridInvoice.getColumn('Discount', true);
            var selectColIndex = $gridInvoice.getColumn('SelectRow', true);
            var discountchng = 0;
            var appliedAmt = 0;
            var writeOff = 0;
            var changedValue = 0;
            if ($gridInvoice.columns[colIndex].editable == undefined && $gridInvoice.columns[wcolIndex].editable == undefined && $gridInvoice.columns[dcolIndex].editable == undefined) {
                return;
            }

            // when Applied amount cell changed 
            changedValue = event.value_new != "" ? event.value_new : changedValue;
            if (event.column == colIndex || event.column == wcolIndex || event.column == dcolIndex) {
                if (!dotFormatter) {
                    if (!changedValue.toString().contains("−") && !changedValue.toString().contains(",")) {
                        if (!isNaN(changedValue))
                            changedValue = format.GetFormatedValue(changedValue, "init", dotFormatter).toString();
                    }
                    else if (changedValue.toString().contains("−")) {
                        changedValue = (-1 * format.GetConvertedNumber(changedValue, dotFormatter)).toString();
                    }
                    else {
                        changedValue = format.GetConvertedNumber(changedValue, dotFormatter).toString();
                    }
                }
                else {
                    if (changedValue.toString().contains("−")) {
                        changedValue = (-1 * format.GetConvertedNumber(changedValue, dotFormatter)).toString();
                    }
                }
                //validate changed Amount is valid number or not
                if (isNaN(changedValue)) {
                    VIS.ADialog.warn("VIS_ValidAmount");
                    event.preventDefault();
                    return;
                }
                //get the changes and converting amount into standard culture.
                if ($gridInvoice.get(event.recid).changes.AppliedAmt != undefined && event.column != colIndex) {
                    appliedAmt = convertAppliedAmtculture($gridInvoice.get(event.recid).changes.AppliedAmt, dotFormatter);
                }
                else {
                    appliedAmt = changedValue;
                }
                if ($gridInvoice.get(event.recid).changes.Discount != undefined && event.column != dcolIndex) {
                    discountchng = convertAppliedAmtculture($gridInvoice.get(event.recid).changes.Discount, dotFormatter);
                }
                else {
                    discountchng = changedValue;
                }
                if ($gridInvoice.get(event.recid).changes.Writeoff != undefined && event.column != wcolIndex) {
                    writeOff = convertAppliedAmtculture($gridInvoice.get(event.recid).changes.Writeoff, dotFormatter);
                }
                else {
                    writeOff = changedValue;
                }
            }

            if ($gridInvoice.getChanges(event.recid) != undefined && $gridInvoice.getChanges(event.recid).length > 0 && $gridInvoice.get(event.recid).changes) {
                // if changes are there like  checkbox is cheked, then we have to set value in changes becoz textbox in grid show data from changes...
                if (VIS.Utility.Util.getValueOfDecimal(changedValue) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) > VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    //$gridInvoice.get(event.recid).changes.AppliedAmt = (event.value_previous != undefined ? event.value_previous : $gridInvoice.get(event.recid).Amount);
                    //$gridInvoice.refreshCell(event.recid, "AppliedAmt");
                    event.preventDefault();
                    return;
                }
                else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) < 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                    VIS.ADialog.warn("AppliedAmtgrtr");
                    //$gridInvoice.get(event.recid).changes.AppliedAmt = (event.value_previous != undefined ? event.value_previous : $gridInvoice.get(event.recid).Amount);
                    //$gridInvoice.refreshCell(event.recid, "AppliedAmt");
                    event.preventDefault();
                    return;
                }
                //Restrict the negative value when OpenAmt is positive Value
                else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < 0) {
                    VIS.ADialog.warn("VIS_AppliedAmtleszero");
                    event.preventDefault();
                    return;
                }
                //if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).changes.Discount) > 0) {
                //    discountchng = VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).changes.Discount);
                //}
                //else
                //    discountchng = VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Discount);
                // check if applied amt is greater than open amount added by vivek on 06/01/2018                
                if (colIndex == event.column) {
                    // check if discount+writeoff + applied is gretaer than open amount added by vivek on 06/01/2018
                    if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) < 0 && VIS.Utility.Util.getValueOfDecimal((VIS.Utility.Util.getValueOfDecimal(changedValue) +
                        VIS.Utility.Util.getValueOfDecimal(writeOff) + VIS.Utility.Util.getValueOfDecimal(discountchng)).toFixed(stdPrecision))
                        < VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        //$gridInvoice.get(event.recid).changes.AppliedAmt = (event.value_previous != undefined ? event.value_previous : $gridInvoice.get(event.recid).Amount);
                        //$gridInvoice.refreshCell(event.recid, "AppliedAmt");
                        event.preventDefault();
                        return;
                    }
                    else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal((VIS.Utility.Util.getValueOfDecimal(changedValue) +
                        VIS.Utility.Util.getValueOfDecimal(writeOff) + VIS.Utility.Util.getValueOfDecimal(discountchng)).toFixed(stdPrecision))
                        > VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        //$gridInvoice.get(event.recid).changes.AppliedAmt = (event.value_previous != undefined ? event.value_previous : $gridInvoice.get(event.recid).Amount);
                        //$gridInvoice.refreshCell(event.recid, "AppliedAmt");
                        event.preventDefault();
                        return;
                    }
                    //when the AppliedAmt will not be allow -Ve Value when OpenAmt have +Ve Value
                    else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < 0) {
                        VIS.ADialog.warn("VIS_AppliedAmtleszero");
                        event.preventDefault();
                        return;
                    }

                    $gridInvoice.get(event.recid).changes.AppliedAmt = event.value_new != "" ? event.value_new : changedValue;
                    $gridInvoice.refreshCell(event.recid, "AppliedAmt");
                }

                else if (wcolIndex == event.column) {
                    if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) < 0 && VIS.Utility.Util.getValueOfDecimal((VIS.Utility.Util.getValueOfDecimal(changedValue) +
                        VIS.Utility.Util.getValueOfDecimal(appliedAmt) + VIS.Utility.Util.getValueOfDecimal(discountchng)).toFixed(stdPrecision))
                        < VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        event.preventDefault();
                        return;
                    }
                    else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal((VIS.Utility.Util.getValueOfDecimal(changedValue) +
                        VIS.Utility.Util.getValueOfDecimal(appliedAmt) + VIS.Utility.Util.getValueOfDecimal(discountchng)).toFixed(stdPrecision))
                        > VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        event.preventDefault();
                        return;
                    }
                    // WriteOffAmt will not be allow -Ve Value when OpenAmt have +Ve Value
                    else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < 0) {
                        VIS.ADialog.warn("VIS_AppliedAmtleszero");
                        event.preventDefault();
                        return;
                    }

                    $gridInvoice.get(event.recid).changes.Writeoff = event.value_new != "" ? event.value_new : changedValue;
                    $gridInvoice.refreshCell(event.recid, "Writeoff");
                }
                else if (dcolIndex == event.column) {
                    if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) < 0 && VIS.Utility.Util.getValueOfDecimal((VIS.Utility.Util.getValueOfDecimal(changedValue) +
                        VIS.Utility.Util.getValueOfDecimal(appliedAmt) + VIS.Utility.Util.getValueOfDecimal(writeOff)).toFixed(stdPrecision))
                        < VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        event.preventDefault();
                        return;
                    }
                    else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal((VIS.Utility.Util.getValueOfDecimal(changedValue) +
                        VIS.Utility.Util.getValueOfDecimal(appliedAmt) + VIS.Utility.Util.getValueOfDecimal(writeOff)).toFixed(stdPrecision))
                        > VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount)) {
                        VIS.ADialog.warn("AppliedAmtgrtr");
                        event.preventDefault();
                        return;
                    }
                    //DiscountAmt will not be allow -Ve Value when OpenAmt have +Ve Value
                    else if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) > 0 && VIS.Utility.Util.getValueOfDecimal(changedValue) < 0) {
                        VIS.ADialog.warn("VIS_AppliedAmtleszero");
                        event.preventDefault();
                        return;
                    }

                    $gridInvoice.get(event.recid).changes.Discount = event.value_new != "" ? event.value_new : changedValue;
                    $gridInvoice.refreshCell(event.recid, "Discount");
                }
            }
            else {
                if (event.column > 0) {
                    if (colIndex == event.column) {
                        $gridInvoice.set(event.recid, { AppliedAmt: event.value_new != "" ? event.value_new : changedValue });
                    }
                    else if (wcolIndex == event.column) {
                        $gridInvoice.set(event.recid, { Writeoff: event.value_new != "" ? event.value_new : changedValue });
                    }
                    else if (dcolIndex == event.column) {
                        $gridInvoice.set(event.recid, { Discount: event.value_new != "" ? event.value_new : changedValue });
                    }
                }
            }

            //if we click on select column checkbox in grid
            if (selectColIndex == event.column || event.column == null) {
                if ($vchkMultiCurrency.is(':checked')) {
                    if (VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount) == 0) {
                        VIS.ADialog.warn("NotFoundCurrencyRate");
                        event.preventDefault();
                        return;
                    }
                    else {
                        $gridInvoice.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount);
                    }
                }
                else {
                    $gridInvoice.records[event.recid]["AppliedAmt"] = VIS.Utility.Util.getValueOfDecimal($gridInvoice.get(event.recid).Amount);
                }
            }

            if (event.column == $gridInvoice.getColumn('Discount', true)
                || event.column == $gridInvoice.getColumn('Writeoff', true)
                || event.column == $gridInvoice.getColumn('AppliedAmt', true)) {

                tableChanged(event.index, event.column, true, false);

            }
        };

        function tableChanged(rowIndex, colIndex, isInvoice, cash) {
            var row = rowIndex;
            var col = colIndex;
            if (col == null || col == undefined) {
                col = 0;
            }
            var AllocationDate = null;
            //  Payments
            if (!isInvoice && !cash) {
                //TableModel payment = vdgvInvoice.getModel();
                if (col == 0) {
                    var columns = $gridPayment.columns;
                    colPayCheck = getBoolValue($gridPayment.getChanges(), row);

                    //AppliedAmt---//get column index from grid
                    _payment = getIndexFromArray(columns, "AppliedAmt");
                    var payemntCol = columns[_payment].field;
                    //  selected - set payment amount
                    var changes = $gridPayment.get(row).changes;

                    if (colPayCheck) {
                        // prepared same array for grid when we select any payment from payment grid and push that object into selected payment array.
                        var record = $gridPayment.records[row];
                        var rcdRow = {
                            SelectRow: record.SelectRow,
                            //InvoiceRecord: changes.InvoiceRecord,
                            Date1: record.Date1,
                            Documentno: record.Documentno,
                            CpaymentID: record.CpaymentID,
                            Isocode: record.Isocode,
                            //Currency: record.Currency,
                            ConvertedAmount: record.ConvertedAmount,
                            OpenAmt: record.OpenAmt,
                            Payment: record.Payment,
                            Multiplierap: record.Multiplierap,
                            DocBaseType: record.DocBaseType,
                            //Writeoff: record.Writeoff,
                            AppliedAmt: record.AppliedAmt,
                            //C_InvoicePaySchedule_ID: record.C_InvoicePaySchedule_ID,
                            //InvoiceScheduleDate: record.InvoiceScheduleDate,
                            C_ConversionType_ID: record.C_ConversionType_ID,
                            ConversionName: record.ConversionName,
                            DATEACCT: record.DATEACCT,
                            AD_Org_ID: record.AD_Org_ID,
                            OrgName: record.OrgName,
                            PayName: record.PayName
                        };
                        selectedPayments.push(rcdRow);

                        //OpenAmt--//get column index from grid
                        _openPay = getIndexFromArray(columns, "OpenAmt");

                        var amount = parseFloat($gridPayment.get(row)[columns[_openPay].field]);

                        _value = removeAllButLast1(parseFloat(amount).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }), dotFormatter ? "," : ".");

                        //Date1--//get column index from grid
                        var _date1 = getIndexFromArray(columns, "Date1");
                        AllocationDate = new Date($gridPayment.get(row)[columns[_date1].field]);

                        if (payemntCol == "AppliedAmt") {
                            if (changes != null && changes != undefined) {
                                changes.AppliedAmt = _value;
                                $gridPayment.refreshCell(row, "AppliedAmt");
                            }
                            else {
                                $gridPayment.set(row, { "AppliedAmt": _value });
                            }
                        }
                    }
                    else    //  de-selected
                    {
                        //if (payemntCol == "AppliedAmt") {
                        //    if (changes != null && changes != undefined) {
                        //        changes.AppliedAmt = 0;
                        //        $gridPayment.refreshCell(row, "AppliedAmt");
                        //    }
                        //    else {
                        //        $gridPayment.set(row, { "AppliedAmt": 0 });
                        //    }
                        //}
                        for (var x = 0; x < selectedPayments.length; x++) {
                            if (selectedPayments[x].CpaymentID == $gridPayment.records[row].CpaymentID) {
                                selectedPayments.splice(x, 1);
                            }
                        }
                        if (payemntCol == "AppliedAmt") {
                            if (changes != null && changes != undefined) {
                                changes.AppliedAmt = amount;
                                $gridPayment.records[row]["AppliedAmt"] = 0;
                                $gridPayment.refreshCell(row, "AppliedAmt");
                            }
                        }
                        var isAllUncheckedInvoice = false;
                        for (var i = 0; i < $gridInvoice.getChanges().length; i++) {
                            if (!($gridInvoice.getChanges()[i].SelectRow == undefined || $gridInvoice.getChanges()[i].SelectRow == false))
                                isAllUncheckedInvoice = true;
                        }

                        var isAllUncheckedCash = false;
                        for (var i = 0; i < $gridCashline.getChanges().length; i++) {
                            if (!($gridCashline.getChanges()[i].SelectRow == undefined || $gridCashline.getChanges()[i].SelectRow == false))
                                isAllUncheckedCash = true;
                        }

                        var isAllUncheckedPayment = false;
                        for (var i = 0; i < $gridPayment.getChanges().length; i++) {
                            if (!($gridPayment.getChanges()[i].SelectRow == undefined || $gridPayment.getChanges()[i].SelectRow == false))
                                isAllUncheckedPayment = true;
                        }
                        //added for GL grid to validate ConversionType
                        var isAllUncheckedGL = false;
                        for (var i = 0; i < $glLineGrid.getChanges().length; i++) {
                            if (!($glLineGrid.getChanges()[i].SelectRow == undefined || $glLineGrid.getChanges()[i].SelectRow == false))
                                isAllUncheckedGL = true;
                        }

                        if (!isAllUncheckedGL && !isAllUncheckedInvoice && !isAllUncheckedCash && !isAllUncheckedPayment)
                            C_ConversionType_ID = 0;

                        $gridPayment.columns[_payment].editable = false;
                    }
                }
            }

            //Cash Lines
            else if (cash) {

                if (col == 0) {
                    var columns = $gridCashline.columns;
                    colCashCheck = getBoolValue($gridCashline.getChanges(), row);

                    //AppliedAmt--- //get column index from grid
                    _paymentCash = getIndexFromArray(columns, "AppliedAmt");
                    var payemntCol = columns[_paymentCash].field;
                    //  selected - set payment amount
                    var changes = $gridCashline.get(row).changes;
                    //  selected - set payment amount
                    if (colCashCheck) {

                        // prepared same array for grid when we select any cash line from cash line grid and push that object into selected cash line array.
                        var record = $gridCashline.records[row];
                        //debugger;
                        var rcdRow = {
                            SelectRow: record.SelectRow,
                            //InvoiceRecord: changes.InvoiceRecord,
                            Date1: record.Date1,
                            ReceiptNo: record.ReceiptNo,
                            VSS_paymenttype: record.VSS_paymenttype,
                            Isocode: record.Isocode,
                            CcashlineiID: record.CcashlineiID,
                            Amount: record.Amount,
                            OpenAmt: record.OpenAmt,
                            Payment: record.Payment,
                            Multiplierap: record.Multiplierap,
                            DocBaseType: record.DocBaseType,
                            ConvertedAmount: record.ConvertedAmount,
                            AppliedAmt: record.AppliedAmt,
                            //C_InvoicePaySchedule_ID: record.C_InvoicePaySchedule_ID,
                            //InvoiceScheduleDate: record.InvoiceScheduleDate,
                            C_ConversionType_ID: record.C_ConversionType_ID,
                            ConversionName: record.ConversionName,
                            DATEACCT: record.DATEACCT,
                            AD_Org_ID: record.AD_Org_ID,
                            OrgName: record.OrgName
                        };
                        selectedCashlines.push(rcdRow);
                        //OpenAmt--//get column index from grid
                        _open = getIndexFromArray(columns, "OpenAmt");
                        var amount = parseFloat($gridCashline.get(row)[columns[_open].field]);
                        //remove the thousand separator from the value
                        _value = removeAllButLast1(parseFloat(amount).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }), dotFormatter ? "," : ".");
                        if (payemntCol == "AppliedAmt") {
                            if (changes != null && changes != undefined) {
                                changes.AppliedAmt = _value;
                                $gridCashline.refreshCell(row, "AppliedAmt");
                            }
                            else {
                                $gridCashline.set(row, { "AppliedAmt": _value });
                            }
                        }
                        //************************************** Changed
                    }
                    else    //  de-selected
                    {
                        //************************************** Changed
                        //if (payemntCol == "AppliedAmt") {
                        //    if (changes != null && changes != undefined) {
                        //        changes.AppliedAmt = 0;
                        //        $gridCashline.refreshCell(row, "AppliedAmt");
                        //    }
                        //    else {
                        //        $gridCashline.set(row, { "AppliedAmt": 0 });
                        //    }
                        //}
                        for (var x = 0; x < selectedCashlines.length; x++) {
                            if (selectedCashlines[x].CcashlineiID == $gridCashline.records[row].CcashlineiID) {
                                selectedCashlines.splice(x, 1);
                            }
                        }
                        if (payemntCol == "AppliedAmt") {
                            if (changes != null && changes != undefined) {
                                changes.AppliedAmt = amount;
                                $gridCashline.records[row]["AppliedAmt"] = 0;
                                $gridCashline.refreshCell(row, "AppliedAmt");
                            }
                        }
                        var isAllUncheckedInvoice = false;
                        for (var i = 0; i < $gridInvoice.getChanges().length; i++) {
                            if (!($gridInvoice.getChanges()[i].SelectRow == undefined || $gridInvoice.getChanges()[i].SelectRow == false))
                                isAllUncheckedInvoice = true;
                        }

                        var isAllUncheckedCash = false;
                        for (var i = 0; i < $gridCashline.getChanges().length; i++) {
                            if (!($gridCashline.getChanges()[i].SelectRow == undefined || $gridCashline.getChanges()[i].SelectRow == false))
                                isAllUncheckedCash = true;
                        }

                        var isAllUncheckedPayment = false;
                        for (var i = 0; i < $gridPayment.getChanges().length; i++) {
                            if (!($gridPayment.getChanges()[i].SelectRow == undefined || $gridPayment.getChanges()[i].SelectRow == false))
                                isAllUncheckedPayment = true;
                        }

                        //added for GL grid to validate ConversionType
                        var isAllUncheckedGL = false;
                        for (var i = 0; i < $glLineGrid.getChanges().length; i++) {
                            if (!($glLineGrid.getChanges()[i].SelectRow == undefined || $glLineGrid.getChanges()[i].SelectRow == false))
                                isAllUncheckedGL = true;
                        }

                        if (!isAllUncheckedGL && !isAllUncheckedInvoice && !isAllUncheckedCash && !isAllUncheckedPayment)
                            C_ConversionType_ID = 0;

                        $gridCashline.columns[_paymentCash].editable = false;
                    }
                }
            }
            //  Invoice Selection
            else if (col == 0) {
                var columns = $gridInvoice.columns;

                colInvCheck = getBoolValue($gridInvoice.getChanges(), row);
                //  selected - set payment amount
                var changes = $gridInvoice.get(row).changes;
                //Writeoff--//get column index from grid
                _writeOff = getIndexFromArray(columns, "Writeoff");
                var writeOff = columns[_writeOff].field;
                //AppliedAmt-- //get column index from grid
                _applied = getIndexFromArray(columns, "AppliedAmt");
                var applied = columns[_applied].field;
                //  selected - set applied amount
                if (colInvCheck) {

                    // prepared same array for grid when we select any invoice from invoice grid and push that object into selected invoice array.
                    var record = $gridInvoice.records[row];
                    var rcdRow = {
                        SelectRow: record.SelectRow,
                        InvoiceRecord: record.InvoiceRecord,
                        Date1: record.Date1,
                        Documentno: record.Documentno,
                        CinvoiceID: record.CinvoiceID,
                        Isocode: record.Isocode,
                        Currency: record.Currency,
                        Converted: record.Converted,
                        Amount: record.Amount,
                        Discount: record.Discount,
                        Multiplierap: record.Multiplierap,
                        DocBaseType: record.DocBaseType,
                        Writeoff: record.Writeoff,
                        AppliedAmt: record.AppliedAmt,
                        C_InvoicePaySchedule_ID: record.C_InvoicePaySchedule_ID,
                        InvoiceScheduleDate: record.InvoiceScheduleDate,
                        C_ConversionType_ID: record.C_ConversionType_ID,
                        ConversionName: record.ConversionName,
                        DATEACCT: record.DATEACCT,
                        AD_Org_ID: record.AD_Org_ID,
                        OrgName: record.OrgName,
                        PayName: record.PayName
                    };
                    selectedInvoices.push(rcdRow);

                    //Amount-- //get column index from grid
                    _openInv = getIndexFromArray(columns, "Amount");
                    var amount = parseFloat($gridInvoice.get(row)[columns[_openInv].field]);
                    //Discount  //get column index from grid
                    _discount = getIndexFromArray(columns, "Discount");
                    amount = amount - parseFloat($gridInvoice.get(row)[columns[_discount].field]);
                    //remove the thousand separator form the value
                    _value = removeAllButLast1(parseFloat(amount).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }), dotFormatter ? "," : ".");
                    if (applied == "AppliedAmt") {
                        if (changes != null && changes != undefined) {
                            changes.Writeoff = 0;
                            changes.AppliedAmt = _value;
                            //get column index from grid
                            _discount = getIndexFromArray(columns, "Discount");
                            changes.Discount = removeAllButLast1(parseFloat($gridInvoice.get(row)[columns[_discount].field]).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }), dotFormatter ? "," : ".");
                            $gridInvoice.refreshCell(row, "Writeoff");
                            $gridInvoice.refreshCell(row, "AppliedAmt");
                            $gridInvoice.refreshCell(row, "Discount");
                        }
                        else {
                            $gridInvoice.set(row, { "Writeoff": 0 });
                            $gridInvoice.set(row, { "AppliedAmt": _value });

                        }
                    }
                    //************************************** Changed
                }
                else    //  de-selected
                {
                    // remove invoice schedule from array when we de-select any schedule from invoice grid.
                    for (var x = 0; x < selectedInvoices.length; x++) {
                        if (selectedInvoices[x].C_InvoicePaySchedule_ID == $gridInvoice.records[row].C_InvoicePaySchedule_ID) {
                            selectedInvoices.splice(x, 1);
                        }
                    }

                    if (applied == "AppliedAmt") {
                        if (changes != null && changes != undefined) {
                            changes.Writeoff = amount;
                            changes.AppliedAmt = amount;
                            changes.Discount = amount;                            //Discount---//get column index from grid
                            _discount = getIndexFromArray(columns, "Discount");
                            //changes.Discount = parseFloat($gridInvoice.get(row)[columns[_discount].field]);
                            $gridInvoice.records[row]["AppliedAmt"] = 0;
                            $gridInvoice.refreshCell(row, "Writeoff");
                            $gridInvoice.refreshCell(row, "AppliedAmt");
                            $gridInvoice.refreshCell(row, "Discount");
                        }
                        else {
                            //$gridInvoice.set(row, { "Writeoff": 0 });
                            //$gridInvoice.set(row, { "AppliedAmt": amount });
                        }
                    }
                    var isAllUncheckedInvoice = false;
                    for (var i = 0; i < $gridInvoice.getChanges().length; i++) {
                        if (!($gridInvoice.getChanges()[i].SelectRow == undefined || $gridInvoice.getChanges()[i].SelectRow == false))
                            isAllUncheckedInvoice = true;
                    }

                    var isAllUncheckedCash = false;
                    for (var i = 0; i < $gridCashline.getChanges().length; i++) {
                        if (!($gridCashline.getChanges()[i].SelectRow == undefined || $gridCashline.getChanges()[i].SelectRow == false))
                            isAllUncheckedCash = true;
                    }

                    var isAllUncheckedPayment = false;
                    for (var i = 0; i < $gridPayment.getChanges().length; i++) {
                        if (!($gridPayment.getChanges()[i].SelectRow == undefined || $gridPayment.getChanges()[i].SelectRow == false))
                            isAllUncheckedPayment = true;
                    }

                    //added for GL grid to validate ConversionType
                    var isAllUncheckedGL = false;
                    for (var i = 0; i < $glLineGrid.getChanges().length; i++) {
                        if (!($glLineGrid.getChanges()[i].SelectRow == undefined || $glLineGrid.getChanges()[i].SelectRow == false))
                            isAllUncheckedGL = true;
                    }

                    if (!isAllUncheckedGL && !isAllUncheckedInvoice && !isAllUncheckedCash && !isAllUncheckedPayment)
                        C_ConversionType_ID = 0;

                    $gridInvoice.columns[_applied].editable = false;
                }
            }

            //  Invoice - Try to balance entry
            //if ($vchkAutoWriteOff.is(':checked')) {
            //    autoWriteOff();
            //}
            calculate();
        };

        //to get index of column from array
        function getIndexFromArray(columns, name) {
            var index = 0;
            for (var i = 0; i < columns.length; i++) {
                if (columns[i].field == name) {
                    index = i;
                    break;
                }
            }
            return index;
        };

        //added for gl-allocation
        function glTableChanged(rowIndex, colIndex) {

            var row = rowIndex;
            var col = colIndex;
            if (col == null || col == undefined) {
                col = 0;
            }

            var AllocationDate = null;
            //  GL Allocation
            if (col == 0) {
                var columns = $glLineGrid.columns;
                colGlCheck = getBoolValue($glLineGrid.getChanges(), row);
                var payemntCol = columns[10].field;
                //  selected - set payment amount
                var changes;
                if (!$glLineGrid.get(row)) { }
                else
                    changes = $glLineGrid.get(row).changes;

                if (colGlCheck) {
                    var record = null;
                    // prepared same array for grid when we select any payment from payment grid and push that object into selected payment array.
                    for (var j = 0; $glLineGrid.records.length; j++) {
                        if ($glLineGrid.records[j].recid == parseInt(row)) {
                            record = $glLineGrid.records[j];
                            break;
                        }
                    }
                    //var record = glLineGrid.records[row];
                    var rcdRow = {
                        SelectRow: record.SelectRow,
                        GLRecords: changes.GLRecords,
                        Date1: record.Date1,
                        DOCUMENTNO: record.DOCUMENTNO,
                        //added account column
                        Account: record.Account,
                        DATEDOC: record.DATEDOC,
                        Isocode: record.Isocode,
                        GL_JOURNALLINE_ID: record.GL_JOURNALLINE_ID,
                        GL_Journal_ID: record.GL_Journal_ID,
                        OpenAmount: record.OpenAmount,
                        //Payment: record.Payment,
                        //Multiplierap: record.Multiplierap,
                        //DocBaseType: record.DocBaseType,
                        ConvertedAmount: record.ConvertedAmount,
                        AppliedAmt: record.AppliedAmt,
                        //C_InvoicePaySchedule_ID: record.C_InvoicePaySchedule_ID,
                        //InvoiceScheduleDate: record.InvoiceScheduleDate,
                        C_ConversionType_ID: record.C_ConversionType_ID,
                        ConversionName: record.ConversionName,
                        DATEACCT: record.DATEACCT,
                        AD_Org_ID: record.AD_Org_ID,
                        OrgName: record.OrgName
                    };
                    SelectedGL.push(rcdRow);
                    var amount = parseFloat($glLineGrid.get(row)[columns[9].field]);
                    AllocationDate = new Date($glLineGrid.get(row)[columns[11].field]);
                    if (payemntCol == "AppliedAmt") {
                        if (changes != null && changes != undefined) {
                            changes.AppliedAmt = amount;
                            $glLineGrid.set(row, { "AppliedAmt": amount });
                            $glLineGrid.refreshCell(row, "AppliedAmt");
                        }
                        //else {
                        //    //amount = 0;
                        //    glLineGrid.set(row, { "AppliedAmt": amount });
                        //}
                    }
                }
                else    //  de-selected
                {
                    //de-select from SelectedGL list.
                    for (var x = 0; x < SelectedGL.length; x++) {
                        if (SelectedGL[x].GL_JOURNALLINE_ID == $glLineGrid.records[row].GL_JOURNALLINE_ID) {
                            SelectedGL.splice(x, 1);
                        }
                    }
                    if (payemntCol == "AppliedAmt") {
                        if (changes != null && changes != undefined) {
                            changes.AppliedAmt = amount;
                            $glLineGrid.records[row]["AppliedAmt"] = 0;
                            $glLineGrid.refreshCell(row, "AppliedAmt");
                        }
                    }
                    //added for Cash Journal, Payment and Invoice grid(s) changes to validate ConversionType
                    var isAllUncheckedInvoice = false;
                    for (var i = 0; i < $gridInvoice.getChanges().length; i++) {
                        if (!($gridInvoice.getChanges()[i].SelectRow == undefined || $gridInvoice.getChanges()[i].SelectRow == false))
                            isAllUncheckedInvoice = true;
                    }

                    var isAllUncheckedCash = false;
                    for (var i = 0; i < $gridCashline.getChanges().length; i++) {
                        if (!($gridCashline.getChanges()[i].SelectRow == undefined || $gridCashline.getChanges()[i].SelectRow == false))
                            isAllUncheckedCash = true;
                    }

                    var isAllUncheckedPayment = false;
                    for (var i = 0; i < $gridPayment.getChanges().length; i++) {
                        if (!($gridPayment.getChanges()[i].SelectRow == undefined || $gridPayment.getChanges()[i].SelectRow == false))
                            isAllUncheckedPayment = true;
                    }

                    var isAllUncheckedGL = false;
                    for (var i = 0; i < $glLineGrid.getChanges().length; i++) {
                        if (!($glLineGrid.getChanges()[i].SelectRow == undefined || $glLineGrid.getChanges()[i].SelectRow == false))
                            isAllUncheckedGL = true;
                    }
                    if (!isAllUncheckedGL && !isAllUncheckedInvoice && !isAllUncheckedCash && !isAllUncheckedPayment) {
                        C_ConversionType_ID = 0;
                    }
                }
            }
            calculate();
        };
        //end

        function getBoolValue(changes, row) {
            if (changes == null || changes.length == 0) {
                return false;
            }
            var element = $.grep(changes, function (ele, index) {
                return parseInt(ele.recid) == row;
            });
            if (element == null || element[0] == undefined || element.length == 0 || element[0].SelectRow == undefined) {
                return false;
            }
            else {
                return element[0].SelectRow;;
            }

        };

        //function autoWriteOff() {

        //    invoiceTotal = 0;
        //    paymentTotal = 0;
        //    // if cash record is selected means our cash grid is not readonly
        //    //if ($vchkAllocation.is(':checked')) {
        //    if (!readOnlyCash) {
        //        var cashChanges = $gridCashline.getChanges();
        //        for (var i = 0; i < cashChanges.length; i++) {
        //            if (cashChanges[i].SelectRow == true) {
        //                paymentTotal += parseFloat(cashChanges[i].AppliedAmt);
        //            }
        //        }
        //    }
        //    else {
        //        var paymentChanges = $gridPayment.getChanges();
        //        for (var i = 0; i < paymentChanges.length; i++) {
        //            if (paymentChanges[i].SelectRow == true) {
        //                paymentTotal += parseFloat(paymentChanges[i].AppliedAmt);
        //            }
        //        }
        //    }
        //    var invoiceChanges = $gridInvoice.getChanges();
        //    var lastRow = null;
        //    var columns = $gridInvoice.columns;
        //    for (var i = invoiceChanges.length - 1; i >= 0; i--) {
        //        if (invoiceChanges[i].SelectRow == true) {
        //            invoiceTotal += parseFloat(invoiceChanges[i].AppliedAmt);
        //            if (lastRow == null) {
        //                lastRow = $gridInvoice.get(invoiceChanges[i].recid);
        //                //Amount
        //                if (lastRow[columns[_openInv].field] <= 0) {
        //                    lastRow = null;
        //                }
        //            }

        //        }
        //    }

        //    if (invoiceTotal > paymentTotal) {
        //        var difference = invoiceTotal - paymentTotal;
        //        if ((invoiceTotal * .05) >= difference) {
        //            if (lastRow != null) {
        //                lastRow.changes.Writeoff = difference;
        //                lastRow.changes.AppliedAmt = lastRow.changes.AppliedAmt - difference;
        //                $gridInvoice.refreshCell(lastRow.recid, "Writeoff");
        //                $gridInvoice.refreshCell(lastRow.recid, "AppliedAmt");
        //            }
        //        }
        //    }
        //};

        function bpValueChanged() {
            vetoableChange($vSearchBPartner.getName(), $vSearchBPartner.value);
            //added for gl-allocation
            //////loadGLDataGrid($vSearchBPartner.event);
        };

        // Conversion Type Change Event
        function conversionTypeChanged() {
            vetoableChange($vConversionType.getName(), $vConversionType.getValue());
        };

        function getMaxDate() {
            _allDates = [];
            _dateAcct = [];
            for (var i = 0; i < $gridPayment.getChanges().length; i++) {
                if ($gridPayment.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($gridPayment.getChanges()[i].SelectRow == true) {
                        var row = $gridPayment.records[$gridPayment.getChanges()[i].recid].DATEACCT;
                        _allDates.push(new Date(row));
                        var DATEACCT = $gridPayment.records[$gridPayment.getChanges()[i].recid].DATEACCT;
                        _dateAcct.push(new Date(DATEACCT));
                        $dateAcct.val(Globalize.format(new Date(Math.max.apply(null, _dateAcct)), "yyyy-MM-dd"));
                        //_dateAcct = [];
                    }
                }
            }
            for (var i = 0; i < $gridInvoice.getChanges().length; i++) {
                if ($gridInvoice.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($gridInvoice.getChanges()[i].SelectRow == true) {
                        var row = $gridInvoice.records[$gridInvoice.getChanges()[i].recid].Date1; //changed schedule date to invoice date suggested by Mukesh sir, ravi and amit.
                        _allDates.push(new Date(row));
                        if ($gridPayment.getChanges().length == 0 && $gridCashline.getChanges().length == 0 && $glLineGrid.getChanges().length == 0) {
                            var DATEACCT = $gridInvoice.records[$gridInvoice.getChanges()[i].recid].DATEACCT;
                            _dateAcct.push(new Date(DATEACCT));
                            $dateAcct.val(Globalize.format(new Date(Math.max.apply(null, _dateAcct)), "yyyy-MM-dd"));
                        }
                    }
                }
            }
            for (var i = 0; i < $gridCashline.getChanges().length; i++) {
                if ($gridCashline.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($gridCashline.getChanges()[i].SelectRow == true) {
                        var row = $gridCashline.records[$gridCashline.getChanges()[i].recid].DATEACCT;
                        _allDates.push(new Date(row));
                        if ($gridPayment.getChanges().length == 0 && $gridInvoice.getChanges().length == 0 && $glLineGrid.getChanges().length == 0) {
                            var DATEACCT = $gridCashline.records[$gridCashline.getChanges()[i].recid].DATEACCT;
                            _dateAcct.push(new Date(DATEACCT));
                            $dateAcct.val(Globalize.format(new Date(Math.max.apply(null, _dateAcct)), "yyyy-MM-dd"));
                        }
                    }
                }
            }
            for (var i = 0; i < $glLineGrid.getChanges().length; i++) {
                if ($glLineGrid.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($glLineGrid.getChanges()[i].SelectRow == true) {
                        var row = null;
                        for (var j = 0; $glLineGrid.records.length; j++) {
                            if ($glLineGrid.records[j].recid == $glLineGrid.getChanges()[i].recid) {
                                row = $glLineGrid.records[j].DATEACCT;
                                break;
                            }
                        }
                        //var row = $glLineGrid.records[glLineGrid.getChanges()[i].recid].DATEACCT;
                        _allDates.push(new Date(row));
                        if ($gridPayment.getChanges().length == 0 && $gridCashline.getChanges().length == 0 && $gridInvoice.getChanges().length == 0) {
                            var DATEACCT = null;
                            for (var j = 0; $glLineGrid.records.length; j++) {
                                if ($glLineGrid.records[j].recid == $glLineGrid.getChanges()[i].recid) {
                                    DATEACCT = $glLineGrid.records[j].DATEACCT;
                                    break;
                                }
                            }
                            //var DATEACCT = glLineGrid.records[glLineGrid.getChanges()[i].recid].DATEACCT;
                            _dateAcct.push(new Date(DATEACCT));
                            $dateAcct.val(Globalize.format(new Date(Math.max.apply(null, _dateAcct)), "yyyy-MM-dd"));
                        }
                    }
                }
            }
            maxDate = new Date(Math.max.apply(null, _allDates));
            if (_allDates.length == 0 || _allDates == "Invalid Date")
                maxDate = new Date();
            //$date.val(Globalize.format(new Date(maxDate), "yyyy-MM-dd"));
            //$dateAcct.val(Globalize.format(new Date(maxDate), "yyyy-MM-dd"));
            $dateAcct.val(Globalize.format(maxDate, "yyyy-MM-dd"));
        };

        //load unallocated GL Journal Lines
        //added for gl-allocation
        function loadGLVoucher() {
            if (_C_BPartner_ID > 0) {
                var chk = $vchkMultiCurrency.is(':checked');
                _C_Currency_ID = $cmbCurrency.val();
                if (_C_Currency_ID == 0) {
                    return VIS.ADialog.info("Message", true, "select currency", "");
                }
                AD_Org_ID = $OrgFilter.val();
                fromDate = $gfromDate.val();
                toDate = $gtoDate.val();
                srchText = $srchGL.val();
                $bsyDiv[0].style.visibility = "visible";
                pageNoGL = 1, gridPgnoGL = 1, glRecord = 0;
                isGLGridLoaded = false;
                //---If ToDate is less than From Date--Display error message-----
                if (toDate != "") {
                    if (fromDate > toDate) {
                        $gtoDate.val('');
                        return VIS.ADialog.info("Message", true, VIS.Msg.getMsg("VIS_FromDateGreater"), "");
                    }
                }
                if (fromDate != null && fromDate != undefined && fromDate != "") {
                    VIS.DB.to_date(fromDate);
                }
                if (toDate != null && toDate != undefined && toDate != "") {
                    VIS.DB.to_date(toDate);
                }
                $.ajax({
                    url: VIS.Application.contextUrl + "VIS/PaymentAllocation/GetGLData",
                    dataType: "json",
                    data: { AD_Org_ID: AD_Org_ID, _C_Currency_ID: _C_Currency_ID, _C_BPartner_ID: _C_BPartner_ID, page: pageNoGL, size: PAGESIZE, fromDate: fromDate, toDate: toDate, srchText: srchText, chk: chk, isInterComp: _isInterCompany },
                    async: true,
                    success: function (result) {
                        //if (result) {
                        //    callbackLoadGlLines(result);
                        //}
                        var data = JSON.parse(result);
                        if (data) {
                            if (pageNoGL == 1 && data.length > 0) {
                                glRecord = data[0].GLRecords;
                                gridPgnoGL = Math.ceil(glRecord / PAGESIZE);
                            }
                            //if any invoice is selected and we load another invoice from db than we have to check weather that invoice is already selected or not. if already selected tha skip that.
                            if (SelectedGL.length > 0) {
                                totalselectedgl = SelectedGL.length;
                                for (var i = 0; i < data.length; i++) {

                                    var filterObj = SelectedGL.filter(function (e) {
                                        return e.GL_JOURNALLINE_ID == data[i]["GL_JOURNALLINE_ID"];
                                    });

                                    if (filterObj.length == 0) {
                                        data[i]["recid"] = SelectedGL.length + i;
                                        SelectedGL.push(data[i]);
                                    }
                                }
                                data = SelectedGL;
                                // Clear Selected gl array when we de-select the select all checkbox. work done for to hold all the selected invoices
                                SelectedGL = [];
                            }
                            //end
                            //callbackLoadGlLines(data);
                            bindGLGrid(data, chk);
                            //added window no to find grid to implement scroll  issue : scroll was not working properly
                            $divGl.find('#grid_openformatglgrid_' + $self.windowNo + '_records').on('scroll', cartGLScroll);
                            isGLGridLoaded = true;
                            isBusyIndicatorHidden();
                        }
                    },
                    error: function (err) {
                        $bsyDiv[0].style.visibility = "hidden";
                    }
                });
            }
        };

        //function callbackLoadGlLines(data) {
        //    if (data != null) {
        //        //var data = JSON.parse(data);
        //        var rows = [];

        //        var colkeys = [];


        //        if (data.length > 0) {
        //            colkeys = Object.keys(data[0]);
        //        }
        //        // check records exist in grid or not
        //        // for updating record id 
        //        var count = 0;
        //        if (glLineGrid.records.length > 0) {
        //            count = glLineGrid.records.length;
        //        };
        //        for (var r = 0; r < data.length; r++) {
        //            var singleRow = {};
        //            singleRow['recid'] = count + r;
        //            for (var c = 0; c < colkeys.length; c++) {
        //                var colna = colkeys[c];
        //                if (colna.toLower() == "selectrow") {
        //                    singleRow[colna] = false;
        //                }
        //                else {
        //                    singleRow[colna] = VIS.Utility.encodeText(data[r][colna]);
        //                }
        //            }
        //            rows.push(singleRow);
        //        }
        //        // bind rows with grid
        //        w2utils.encodeTags(rows);
        //        //glLineGrid.add(rows);
        //        glLineGrid.records = rows;
        //        glLineGrid.refresh();
        //    }
        //    $bsyDiv[0].style.visibility = "hidden";
        //};
        //end

        function getSelectedRecordsCount() {
            _allDates = [];
            isOrgMatched = true;
            for (var i = 0; i < $gridPayment.getChanges().length; i++) {
                if ($gridPayment.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($gridPayment.getChanges()[i].SelectRow == true) {
                        var row = $gridPayment.records[$gridPayment.getChanges()[i].recid].DATEACCT;
                        // check org matched or not 
                        if (isOrgMatched && parseInt($cmbOrg.val()) != parseInt($gridPayment.records[$gridPayment.getChanges()[i].recid].AD_Org_ID)) {
                            isOrgMatched = false;
                        }
                        _allDates.push(new Date(row));
                    }
                }
            }
            for (var i = 0; i < $gridInvoice.getChanges().length; i++) {
                if ($gridInvoice.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($gridInvoice.getChanges()[i].SelectRow == true) {
                        var row = $gridInvoice.records[$gridInvoice.getChanges()[i].recid].Date1; //changed schedule date to invoice date suggested by Mukesh sir, ravi and amit.
                        // check org matched or not 
                        if (isOrgMatched && parseInt($cmbOrg.val()) != parseInt($gridInvoice.records[$gridInvoice.getChanges()[i].recid].AD_Org_ID)) {
                            isOrgMatched = false;
                        }
                        _allDates.push(new Date(row));
                    }
                }
            }
            for (var i = 0; i < $gridCashline.getChanges().length; i++) {
                if ($gridCashline.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($gridCashline.getChanges()[i].SelectRow == true) {
                        var row = $gridCashline.records[$gridCashline.getChanges()[i].recid].DATEACCT;
                        // check org matched or not 
                        if (isOrgMatched && parseInt($cmbOrg.val()) != parseInt($gridCashline.records[$gridCashline.getChanges()[i].recid].AD_Org_ID)) {
                            isOrgMatched = false;
                        }
                        _allDates.push(new Date(row));
                    }
                }
            }
            //added for gl-allocation
            for (var i = 0; i < $glLineGrid.getChanges().length; i++) {
                if ($glLineGrid.getChanges()[i].SelectRow === undefined) {
                }
                else {
                    if ($glLineGrid.getChanges()[i].SelectRow == true) {
                        var row = $glLineGrid.records[$glLineGrid.getChanges()[i].recid].DATEACCT;
                        _allDates.push(new Date(row));
                    }
                }
            }

            if (_allDates.length == 0)
                return false;
            return true;
        };

        /**
        *  Vetoable Change Listener.
        *  - Business Partner
        *  - Currency
        * 	- Date
        *  @param e event
        */
        function vetoableChange(name, value) {
            if (name != "C_BPartner_ID" && value == null) {
                //clear right side  filters and selected records
                clearRightPanelFilters();
                //added to load all blank grids
                blankAllGrids();
                return;
            }

            //  BPartner
            if (name == "C_BPartner_ID") {
                //if (_C_BPartner_ID != value) {
                //    //clear right side  filters and selected records
                //    clearRightPanelFilters();
                //    //added to load all blank grids
                //    blankAllGrids();
                //}
                _C_BPartner_ID = value;
                if (_C_BPartner_ID > 0) {
                    // If BP is selected then  set mandatory false---Neha
                    $vSearchBPartner.getControl().removeClass('vis-ev-col-mandatory');
                }
                //////loadBPartner();
            }
            //	Currency
            else if (name == "C_Currency_ID") {
                _C_Currency_ID = parseInt(value);
                ////////loadBPartner();
            }
            //	Date for Multi-Currency
            else if (name == "Date" && $vchkMultiCurrency) {
                //////loadBPartner();
                refreshLabel();
            }
        }

        function calculate() {
            $bsyDiv[0].style.visibility = "visible";
            var format = VIS.DisplayType.GetNumberFormat(VIS.DisplayType.Amount);
            var allocDate = new Date();
            allocDate = null;
            //  Payment******************
            var totalPay = parseFloat(0);
            _noPayments = 0;
            var rowsPayment = null;
            var rowsCash = null;
            var rowsInvoice = null;
            //added for gl-allocation
            var rowsGL = null;
            //end
            if ($gridPayment)
                rowsPayment = $gridPayment.getChanges();
            if ($gridCashline)
                rowsCash = $gridCashline.getChanges();
            if ($gridInvoice)
                rowsInvoice = $gridInvoice.getChanges();
            if ($glLineGrid)
                rowsGL = $glLineGrid.getChanges();


            if (rowsPayment != null) {
                for (var i = 0; i < rowsPayment.length; i++) {
                    if (rowsPayment[i].SelectRow == true) {
                        var currnetRow = $gridPayment.get(rowsPayment[i].recid);
                        var ts = new Date(currnetRow.Date1);
                        var timeUtil = new VIS.TimeUtil();
                        allocDate = timeUtil.max(allocDate, ts);
                        var keys = Object.keys(currnetRow);
                        //var bd = parseFloat(checkcommaordot(event, rowsPayment[i][keys[keys.indexOf("AppliedAmt")]])).toFixed(stdPrecision);
                        var bd;
                        if (rowsPayment[i][keys[keys.indexOf("AppliedAmt")]] != "" && rowsPayment[i][keys[keys.indexOf("AppliedAmt")]] != undefined) {
                            if (rowsPayment[i][keys[keys.indexOf("AppliedAmt")]].toString().contains("-") ||
                                rowsPayment[i][keys[keys.indexOf("AppliedAmt")]].toString().contains("−")) {
                                bd = (-1 * Math.abs(format.GetConvertedNumber(rowsPayment[i][keys[keys.indexOf("AppliedAmt")]].toString(), dotFormatter))).toFixed(stdPrecision);
                            }
                            else {
                                bd = format.GetConvertedNumber(rowsPayment[i][keys[keys.indexOf("AppliedAmt")]].toString(), dotFormatter).toFixed(stdPrecision);
                            }
                            bd = parseFloat(bd);
                        }
                        else {
                            bd = 0;
                            console.log("Payment Row: " + rowsPayment[i][keys[keys.indexOf("AppliedAmt")]] + ", RowNo:-" + i);
                        }
                        totalPay = totalPay + (isNaN(bd) ? 0 : bd);  //  Applied Pay
                        _noPayments++;
                    }
                }
                //decimal digits as per culture.
                totalPay = totalPay.toFixed(stdPrecision);
                totalPay = parseFloat(totalPay);
            }
            $lblPaymentSum.text(_noPayments + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + parseFloat(totalPay).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }) + " ");//VIS.Msg.getMsg("SelectedPayments") 

            //  Cash******************
            var totalCash = parseFloat(0);
            _noCashLines = 0;
            if (rowsCash != null) {
                for (var i = 0; i < rowsCash.length; i++) {
                    if (rowsCash[i].SelectRow == true) {
                        var currnetRow = $gridCashline.get(rowsCash[i].recid);
                        var ts = new Date(currnetRow.Created);
                        var timeUtil = new VIS.TimeUtil();
                        allocDate = timeUtil.max(allocDate, ts);
                        //************************************** Changed
                        var keys = Object.keys(currnetRow);
                        var bd;
                        if (rowsCash[i][keys[keys.indexOf("AppliedAmt")]] != "" && rowsCash[i][keys[keys.indexOf("AppliedAmt")]] != undefined) {
                            //var bd = parseFloat(checkcommaordot(event, rowsCash[i][keys[keys.indexOf("AppliedAmt")]])).toFixed(stdPrecision);
                            if (rowsCash[i][keys[keys.indexOf("AppliedAmt")]].toString().contains("-") ||
                                rowsCash[i][keys[keys.indexOf("AppliedAmt")]].toString().contains("−")) {
                                bd = (-1 * Math.abs(format.GetConvertedNumber(rowsCash[i][keys[keys.indexOf("AppliedAmt")]].toString(), dotFormatter))).toFixed(stdPrecision);
                            }
                            else {
                                bd = format.GetConvertedNumber(rowsCash[i][keys[keys.indexOf("AppliedAmt")]].toString(), dotFormatter).toFixed(stdPrecision);
                            }
                            bd = parseFloat(bd);
                        } else {
                            bd = 0;
                            console.log("Cash Row: " + rowsCash[i][keys[keys.indexOf("AppliedAmt")]] + ", RowNo:-" + i);
                        }
                        totalCash = totalCash + (isNaN(bd) ? 0 : bd);  //  Applied Pay
                        _noCashLines++;
                    }
                }
                //decimal digits as per culture.
                totalCash = totalCash.toFixed(stdPrecision);
                totalCash = parseFloat(totalCash);
            }
            $lblCashSum.text(_noCashLines + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + parseFloat(totalCash).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }) + " ");//VIS.Msg.getMsg("SelectedCashlines") + 


            //  Invoices******************
            var totalInv = parseFloat(0);
            _noInvoices = 0;
            if (rowsInvoice != null) {
                for (var i = 0; i < rowsInvoice.length; i++) {
                    if (rowsInvoice[i].SelectRow == true) {
                        var currnetRow = $gridInvoice.get(rowsInvoice[i].recid);
                        var ts;
                        if (countVA009 > 0) {
                            ts = new Date(currnetRow.InvoiceScheduleDate);
                        }
                        else {
                            ts = new Date(currnetRow.Date1);
                        }
                        //when Invoice Schedule Date is null then ts become Invalid Date
                        if (ts == "Invalid Date") {
                            ts = new Date(currnetRow.Date1);
                        }

                        var timeUtil = new VIS.TimeUtil();
                        allocDate = timeUtil.max(allocDate, ts);
                        var keys = Object.keys(currnetRow);
                        var bd;
                        //VA228:Check applied amount is undefined
                        if (rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]] != "" && rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]] != undefined) {
                            //bd = parseFloat(checkcommaordot(event, rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]])).toFixed(stdPrecision);
                            if (rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]].toString().contains("-") ||
                                rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]].toString().contains("−")) {
                                bd = (-1 * Math.abs(format.GetConvertedNumber(rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]].toString(), dotFormatter))).toFixed(stdPrecision);
                            }
                            else {
                                bd = format.GetConvertedNumber(rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]].toString(), dotFormatter).toFixed(stdPrecision);
                            }
                            bd = parseFloat(bd);
                        }
                        else {
                            bd = 0;
                            console.log("Invoice Row: " + rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]] + ", RowNo:-" + i);
                        }
                        totalInv = totalInv + (isNaN(bd) ? 0 : bd);  //  Applied Inv
                        _noInvoices++;
                    }
                }
                //decimal digits as per culture.
                totalInv = totalInv.toFixed(stdPrecision);
                totalInv = parseFloat(totalInv);
            }
            $lblInvoiceSum.text(_noInvoices + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + parseFloat(totalInv).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }) + " ");//VIS.Msg.getMsg("SelectedInvoices") +

            //added for gl-allocation

            var totalGL = parseFloat(0);
            _noGL = 0;
            if (rowsGL != null) {
                for (var i = 0; i < rowsGL.length; i++) {
                    if (rowsGL[i].SelectRow == true) {
                        var currnetRow = $glLineGrid.get(rowsGL[i].recid);
                        ts = new Date(currnetRow.Date1);
                        var timeUtil = new VIS.TimeUtil();
                        allocDate = timeUtil.max(allocDate, ts);
                        var keys = Object.keys(currnetRow);
                        var bd;
                        if (rowsGL[i][keys[keys.indexOf("AppliedAmt")]] != "" && rowsGL[i][keys[keys.indexOf("AppliedAmt")]] != undefined) {
                            bd = parseFloat(rowsGL[i][keys[keys.indexOf("AppliedAmt")]]).toFixed(stdPrecision);
                            bd = parseFloat(bd);
                        }
                        else {
                            bd = 0;
                            console.log("GL Row: " + rowsInvoice[i][keys[keys.indexOf("AppliedAmt")]] + ", RowNo:-" + i);
                        }
                        totalGL = totalGL + (isNaN(bd) ? 0 : bd);  //  Applied GL
                        _noGL++;
                    }
                }
                //decimal digits as per culture.
                totalGL = totalGL.toFixed(stdPrecision);
                totalGL = parseFloat(totalGL);
            }
            $lblglSum.text(_noGL + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + parseFloat(totalGL).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision }) + " ");//VIS.Msg.getMsg("SelectedGL") +
            //end

            //	Set AllocationDate
            // not to update date. now it always shows as current date
            //  Set Allocation Currency
            if ($cmbCurrency.children("option").filter(":selected").text() != null) {
                $vlblAllocCurrency.text($cmbCurrency.children("option").filter(":selected").text());
            }
            //  Difference 
            //  Difference --New Logic for Invoice-(cash+payment)-by raghu 18-jan-2011 //  Cash******************
            //var difference = parseFloat((parseFloat(totalPay) + parseFloat(totalCash)) - (parseFloat(totalInv) + parseFloat(totalGL))).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
            var difference = parseFloat((totalPay + totalCash) - (totalInv + totalGL)).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
            $vtxtDifference.text(difference);
            if (parseFloat(difference) == parseFloat(0)) {
                $vbtnAllocate.prop("readonly", false);
                $vbtnAllocate.css({ "pointer-events": "auto", "opacity": "1" });
            }
            else {
                $vbtnAllocate.prop("readonly", true);
                $vbtnAllocate.css({ "pointer-events": "none", "opacity": "0.5" });
            }
            $bsyDiv[0].style.visibility = "hidden";
        };

        // when we change or load business partner, refresh label
        function refreshLabel() {
            //var format = VIS.DisplayType.GetNumberFormat(VIS.DisplayType.Amount); 
            //set the amount as culture format.
            var totalPay = parseFloat(0).toLocaleString(navigator.language, { minimumFractionDigits: stdPrecision, maximumFractionDigits: stdPrecision });
            $lblPaymentSum.text(0 + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + totalPay + " ");//VIS.Msg.getMsg("SelectedPayments") +
            $lblCashSum.text(0 + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + totalPay + " "); //VIS.Msg.getMsg("SelectedCashlines") +
            $lblInvoiceSum.text(0 + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + totalPay + " ");//VIS.Msg.getMsg("SelectedInvoices") +
            $lblglSum.text(0 + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + totalPay + " ");//VIS.Msg.getMsg("SelectedGL") +
            $vtxtDifference.text(totalPay);
            //Set False to select all 
            $invSelectAll.prop('checked', false);
            $paymentSelctAll.prop('checked', false);
            $cashSelctAll.prop('checked', false);
            $glSelectAll.prop('checked', false);
            //to expand the height of all grids
            $('.vis-allocation-payment-grid, .vis-allocation-cashLine-grid, .vis-allocation-invoice-grid, .vis-allocation-gl-grid').css('height', '80%');
            //$('.vis-allocation-leftControls').prop('style', ' margin-bottom: 5px');
            $('#cashMaindiv').css('display', 'none');
            $('#glMaindiv').css('display', 'none');
            if ($vchkMultiCurrency.is(':checked')) {
                $conversionDiv.css('display', 'block');
                //VIS_0045: Show Conversion Type when either allocaton From/To is Invoice
                if (($allocationFrom.val() == "I" && $allocationTo.val() == "P") ||
                    ($allocationFrom.val() == "P" && $allocationTo.val() == "I") ||
                    ($allocationFrom.val() == "I" && $allocationTo.val() == "I")) {
                    $innerRow.find('.vis-conversionType').css('display', 'block');
                }
                else {
                    $innerRow.find('.vis-conversionType').css('display', 'none');
                    $vConversionType.getControl().val('');
                }
            }
            else {
                $conversionDiv.css('display', 'none');
                //VIS_0045: Hide Conversion Type when either allocaton From/To is Invoice
                $innerRow.find('.vis-conversionType').css('display', 'none');
                $vConversionType.getControl().val('');
            }
        };

        function vchkapplocationChange() {
            var rowsPayment;
            var rowsCash;
            if ($gridPayment != undefined)
                rowsPayment = $gridPayment.getChanges();
            if ($gridCashline != undefined)
                rowsCash = $gridCashline.getChanges();
            //Set False to select all 
            $paymentSelctAll.prop('checked', false);
            $cashSelctAll.prop('checked', false);
            //added for gl-allocation
            $vchkGlInvoice.prop('checked', false);
            //end
            // if cash record is selected means our cash grid is not readonly
            //if ($vchkAllocation.is(':checked')) {
            if (!readOnlyCash) {
                readOnlyCash = false;
                readOnlyPayment = true;
                if (rowsPayment != null) {
                    for (var i = 0; i < rowsPayment.length; i++) {
                        var boolValue = getBoolValue($gridPayment.getChanges(), rowsPayment[i].recid);
                        if (boolValue) {
                            var changes = $gridPayment.get(rowsPayment[i].recid).changes;
                            if (changes != null && changes != undefined) {
                                changes.SelectRow = false;
                                changes.AppliedAmt = 0;
                                $gridPayment.refreshCell(rowsPayment[i].recid, "SelectRow");
                                $gridPayment.refreshCell(rowsPayment[i].recid, "AppliedAmt");
                            }
                        }
                    }
                    if ($gridPayment)
                        $gridPayment.refresh();
                }
                //---Display Cash Grid and hide Payment Grid and refresh cash grid-----Neha
                $row2.css('display', 'none');
                $row3.css('display', '');
                if ($gridCashline)
                    $gridCashline.refresh();
                //------------------------------
            }
            else {
                readOnlyCash = true;
                readOnlyPayment = false;
                if (rowsCash != null) {
                    for (var i = 0; i < rowsCash.length; i++) {
                        var boolValue = getBoolValue($gridCashline.getChanges(), rowsCash[i].recid);
                        if (boolValue) {
                            var changes = $gridCashline.get(rowsCash[i].recid).changes;
                            if (changes != null && changes != undefined) {
                                changes.SelectRow = false;
                                changes.AppliedAmt = 0;
                                $gridCashline.refreshCell(rowsCash[i].recid, "SelectRow");
                                $gridCashline.refreshCell(rowsCash[i].recid, "AppliedAmt");
                            }
                        }
                    }
                    if ($gridCashline)
                        $gridCashline.refresh();
                }
                //---Display Payment Grid and hide Cash Grid and refresh Payment grid-----Neha
                $row3.css('display', 'none');
                $row2.css('display', '');
                if ($gridPayment)
                    $gridPayment.refresh();
            }
            calculate();
        };

        function allocate() {
            var canContinue = true;
            if (invoiceTotal > paymentTotal) {
                var difference = invoiceTotal - paymentTotal;
                if ((invoiceTotal * .05) <= difference) {
                    canContinue = false;
                    VIS.ADialog.confirm("AllocationWriteOffWarn", true, "", "Confirm", function (result) {
                        if (result) {
                            allocateNow();
                        }
                    });
                }
            }
            else if (paymentTotal > invoiceTotal) {
                var difference = paymentTotal - invoiceTotal;
                if ((paymentTotal * .05) >= difference) {
                    canContinue = VIS.ADialog.error("AllocationWriteOffWarn");
                }
            }
            if (canContinue) {
                allocateNow();
            }

        };

        function allocateNow() {
            $bsyDiv[0].style.visibility = "visible";
            var rowsPayment = $gridPayment.getChanges();
            var rowsCash = $gridCashline.getChanges();
            var rowsInvoice = $gridInvoice.getChanges();
            //added for gl-allocation
            var rowsGLVoucher = $glLineGrid.getChanges();
            var DateTrx = $date.val();
            var DateAcct = $dateAcct.val();
            //added for gl-allocation
            if ($allocationFrom.val() == "G" || $allocationTo.val() == "G") {
                //   needs to check in case of Party JV allocation weather you are allocating payment with JV Or you are allocating CashLine with JV.
                glData(rowsPayment, rowsInvoice, rowsCash, rowsGLVoucher, DateTrx, DateAcct);
            }
            else if ($allocationFrom.val() == "C" || $allocationTo.val() == "C") {
                saveCashData(rowsCash, rowsInvoice, DateTrx, DateAcct);
            }
            else {
                savePaymentData(rowsPayment, rowsInvoice, DateTrx, DateAcct);
            }
        };

        function saveCashData(rowsCash, rowsInvoice, DateTrx, DateAcct) {

            if (_noInvoices + _noCashLines == 0)
                return "";

            // check period is open or not
            // also check is Non Business Day?
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/CheckPeriodState",
                data: { DateTrx: $dateAcct.val(), AD_Org_ID: $cmbOrg.val() },
                async: false,
                success: function (result) {
                    if (result != "") {
                        //alert(result);
                        VIS.ADialog.info("", true, result, "");
                        setallgridsLoaded = true;
                        loadBPartner();
                        $bsyDiv[0].style.visibility = "hidden";
                        return "";
                    }
                    else {
                        var payment = "";
                        var applied = "";
                        var discount = "";
                        var writeOff = "";
                        var open = "";
                        var C_CurrencyType_ID = 0;

                        //var paymentData = [];
                        var cashData = [];
                        var invoiceData = [];

                        var chk = $vchkMultiCurrency.is(':checked');// isMultiCurrency
                        if (chk) {
                            var conversionDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");// Conversion Date
                        }
                        else {
                            var conversionDate = Globalize.format(new Date(), "yyyy-MM-dd");
                        }
                        //for (var i = 0; i < rowsPayment.length; i++) {
                        //    var row = $gridPayment.get(rowsPayment[i].recid);
                        //    C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                        //    paymentData.push({
                        //        AppliedAmt: rowsPayment[i].AppliedAmt, Date: row.Date1, Converted: row.ConvertedAmount, CpaymentID: row.CpaymentID, Documentno: row.Documentno, Isocode: row.Isocode,
                        //        Multiplierap: row.Multiplierap, OpenAmt: row.OpenAmt, Payment: row.Payment, Org: parseInt($cmbOrg.val())
                        //    });
                        //}
                        if (rowsCash.length > 0) {
                            var keys = Object.keys($gridCashline.get(0));
                            // is uesd to pick applied amount
                            payment = keys[keys.indexOf("AppliedAmt")];
                            applied = keys[keys.indexOf("AppliedAmt")];
                            for (var i = 0; i < rowsCash.length; i++) {
                                var row = $gridCashline.get(rowsCash[i].recid);
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                if (rowsCash[i].AppliedAmt != 0 && rowsCash[i].AppliedAmt != undefined) {
                                    var appliedAmt = rowsCash[i].AppliedAmt;
                                    appliedAmt = convertAppliedAmtculture(appliedAmt, dotFormatter);
                                    cashData.push({
                                        AppliedAmt: appliedAmt, Date: row.Created, Amount: row.Amount, ccashlineid: row.CcashlineiID, Converted: row.ConvertedAmount, Isocode: row.Isocode,
                                        Multiplierap: row.Multiplierap, OpenAmt: row.OpenAmt, ReceiptNo: row.ReceiptNo, Org: parseInt($cmbOrg.val())
                                    });
                                }
                            }
                        }

                        if (rowsInvoice.length > 0) {
                            var keys = Object.keys($gridInvoice.get(0));
                            applied = keys[keys.indexOf("AppliedAmt")];
                            discount = keys[keys.indexOf("Discount")];
                            writeOff = keys[keys.indexOf("Writeoff")];
                            open = keys[keys.indexOf("Amount")];
                            for (var i = 0; i < rowsInvoice.length; i++) {
                                var row = $gridInvoice.get(rowsInvoice[i].recid);
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                var discounts = rowsInvoice[i].Discount;
                                if (discounts == undefined) {
                                    discounts = row.Discount;
                                }
                                var appliedamts = rowsInvoice[i].AppliedAmt;
                                if (appliedamts == undefined) {
                                    appliedamts = row.AppliedAmt;
                                }
                                var writeoffs = rowsInvoice[i].Writeoff;
                                if (writeoffs == undefined) {
                                    writeoffs = row.Writeoff;
                                }
                                if (appliedamts != 0 && appliedamts != undefined) {
                                    appliedamts = convertAppliedAmtculture(appliedamts, dotFormatter);
                                    discounts = convertAppliedAmtculture(discounts, dotFormatter);
                                    writeoffs = convertAppliedAmtculture(writeoffs, dotFormatter);
                                    if (countVA009 <= 0) {
                                        invoiceData.push({
                                            AppliedAmt: appliedamts, Discount: discounts, Writeoff: writeoffs,
                                            cinvoiceid: row.CinvoiceID, Converted: row.Converted, Currency: row.Currency,
                                            Date: row.Date1, Docbasetype: row.DocBaseType,
                                            documentno: row.Documentno, Isocode: row.Isocode, Multiplierap: row.Multiplierap, Amount: row.Amount, Org: parseInt($cmbOrg.val())
                                        });
                                    }
                                    else {
                                        invoiceData.push({
                                            AppliedAmt: appliedamts, Discount: discounts, Writeoff: writeoffs,
                                            cinvoiceid: row.CinvoiceID, Converted: row.Converted, Currency: row.Currency,
                                            //Date: row.Date1, Docbasetype: row.DocBaseType,
                                            // send invoice schedule date if va009 module is updated
                                            Date: row.InvoiceScheduleDate, Docbasetype: row.DocBaseType,
                                            documentno: row.Documentno, Isocode: row.Isocode, Multiplierap: row.Multiplierap, Amount: row.Amount,
                                            c_invoicepayschedule_id: row.C_InvoicePaySchedule_ID, Org: parseInt($cmbOrg.val())
                                        });
                                    }
                                }
                            }
                        }


                        $.ajax({
                            url: VIS.Application.contextUrl + "PaymentAllocation/SaveCashData",
                            type: 'POST',
                            data: ({
                                cashData: JSON.stringify(cashData), invoiceData: JSON.stringify(invoiceData), currency: $cmbCurrency.val(),
                                isCash: true, _C_BPartner_ID: _C_BPartner_ID, _windowNo: self.windowNo, payment: payment, DateTrx: $date.val(), appliedamt: applied
                                , discount: discount, writeOff: writeOff, open: open, DateAcct: DateAcct, _CurrencyType_ID: C_CurrencyType_ID, isInterBPartner: false, conversionDate: conversionDate, chk: chk
                            }),
                            success: function (result) {
                                // Clear Selected invoices array when we de-select the select all checkbox. work done for to hold all the selected invoices
                                selectedInvoices = [];
                                VIS.ADialog.info("", true, result, "");
                                setallgridsLoaded = true;
                                loadBPartner();
                                $bsyDiv[0].style.visibility = "hidden";
                            },
                            error: function (result) {
                                VIS.ADialog.info("", true, result, "");
                                $bsyDiv[0].style.visibility = "hidden";
                            }
                        });

                    }
                },
                error: function (result) {
                    VIS.ADialog.info("", true, result, "");
                    $bsyDiv[0].style.visibility = "hidden";
                }
            });
        };

        function savePaymentData(rowsPayment, rowsInvoice, DateTrx, DateAcct) {

            if (_noInvoices + _payment == 0)
                return "";

            // check period is open or not
            // also check is Non Business Day?
            $.ajax({
                url: VIS.Application.contextUrl + "PaymentAllocation/CheckPeriodState",
                data: { DateTrx: $date.val(), AD_Org_ID: $cmbOrg.val() },
                async: false,
                success: function (result) {
                    if (result != "") {
                        VIS.ADialog.info("", true, result, "");
                        setallgridsLoaded = true;
                        loadBPartner();
                        $bsyDiv[0].style.visibility = "hidden";
                        return "";
                    }
                    else {
                        var payment = "";
                        var applied = "";
                        var discount = "";
                        var writeOff = "";
                        var open = "";
                        var C_CurrencyType_ID = 0;

                        var paymentData = [];
                        //var cashData = [];
                        var invoiceData = [];

                        var chk = $vchkMultiCurrency.is(':checked');
                        if (chk) {
                            var conversionDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");
                        }
                        else {
                            var conversionDate = Globalize.format(new Date(), "yyyy-MM-dd");
                        }
                        for (var i = 0; i < rowsPayment.length; i++) {
                            var row = $gridPayment.get(rowsPayment[i].recid);
                            var keys = Object.keys($gridPayment.get(0));
                            payment = keys[keys.indexOf("AppliedAmt")];
                            //VIS_0045: In case of Multi-Currency, pass conversion type as selected conversion type
                            if (chk) {
                                C_CurrencyType_ID = $vConversionType.getValue();
                            }
                            else {
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                            }
                            //payment = keys[10];
                            if (rowsPayment[i].AppliedAmt != undefined && rowsPayment[i].AppliedAmt != 0) {
                                var appliedAmt = rowsPayment[i].AppliedAmt;
                                appliedAmt = convertAppliedAmtculture(appliedAmt, dotFormatter);
                                paymentData.push({
                                    appliedamt: appliedAmt, date: row.Date1, converted: row.ConvertedAmount, cpaymentid: row.CpaymentID, documentno: row.Documentno, isocode: row.Isocode,
                                    multiplierap: row.Multiplierap, openamt: row.OpenAmt, payment: row.Payment, Org: parseInt($cmbOrg.val())
                                });
                            }
                        }
                        //if (rowsCash.length > 0) {
                        //    var keys = Object.keys($gridCashline.get(0));
                        //    for (var i = 0; i < rowsCash.length; i++) {
                        //        var row = $gridCashline.get(rowsCash[i].recid);

                        //        cashData.push({
                        //            appliedamt: rowsCash[i].AppliedAmt, date: row.Created, amount: row.Amount, ccashlineid: row.CcashlineiID, converted: row.ConvertedAmount, isocode: row.Isocode,
                        //            multiplierap: row.Multiplierap, openamt: row.OpenAmt, receiptno: row.ReceiptNo, Org: parseInt($cmbOrg.val())
                        //        });
                        //    }
                        //}

                        if (rowsInvoice.length > 0) {
                            var keys = Object.keys($gridInvoice.get(0));
                            applied = keys[keys.indexOf("AppliedAmt")];
                            payment = keys[keys.indexOf("AppliedAmt")];
                            discount = keys[keys.indexOf("Discount")];
                            writeOff = keys[keys.indexOf("Writeoff")];
                            open = keys[keys.indexOf("Amount")];
                            for (var i = 0; i < rowsInvoice.length; i++) {
                                var row = $gridInvoice.get(rowsInvoice[i].recid);
                                //VIS_0045: In case of Multi-Currency, pass conversion type as selected conversion type
                                if (chk) {
                                    C_CurrencyType_ID = $vConversionType.getValue();
                                }
                                else {
                                    C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                }
                                var discounts = rowsInvoice[i].Discount;
                                if (discounts == undefined) {
                                    discounts = row.Discount;
                                }

                                var appliedamts = rowsInvoice[i].AppliedAmt;
                                if (appliedamts == undefined) {
                                    appliedamts = row.AppliedAmt;
                                }
                                var writeoffs = rowsInvoice[i].Writeoff;
                                if (writeoffs == undefined) {
                                    writeoffs = row.Writeoff;
                                }
                                if (appliedamts != 0 && appliedamts != undefined) {
                                    appliedamts = convertAppliedAmtculture(appliedamts, dotFormatter);
                                    discounts = convertAppliedAmtculture(discounts, dotFormatter);
                                    writeoffs = convertAppliedAmtculture(writeoffs, dotFormatter);
                                    if (countVA009 <= 0) {
                                        invoiceData.push({
                                            appliedamt: appliedamts, discount: discounts, writeoff: writeoffs,
                                            cinvoiceid: row.CinvoiceID, converted: row.Converted, currency: row.Currency,
                                            date: row.Date1, docbasetype: row.DocBaseType,
                                            documentno: row.Documentno, isocode: row.Isocode, multiplierap: row.Multiplierap, amount: row.Amount, Org: parseInt($cmbOrg.val())
                                        });
                                    }
                                    else {
                                        invoiceData.push({
                                            appliedamt: appliedamts, discount: discounts, writeoff: writeoffs,
                                            cinvoiceid: row.CinvoiceID, converted: row.Converted, currency: row.Currency,
                                            //date: row.Date1, docbasetype: row.DocBaseType,
                                            // send invoice schedule date if va009 module is updated
                                            date: row.InvoiceScheduleDate, docbasetype: row.DocBaseType,
                                            DocumentNo: row.Documentno, Isocode: row.Isocode, Multiplierap: row.Multiplierap, Amount: row.Amount,
                                            c_invoicepayschedule_id: row.C_InvoicePaySchedule_ID, Org: parseInt($cmbOrg.val())
                                        });
                                    }
                                }
                            }
                        }
                        // if cash record is selected means our cash grid is not readonly
                        var cashcheck = false;
                        if (!readOnlyCash) {
                            cashcheck = true;
                        }

                        $.ajax({
                            url: VIS.Application.contextUrl + "PaymentAllocation/SavePaymentData",
                            type: 'POST',
                            data: ({
                                paymentData: JSON.stringify(paymentData), invoiceData: JSON.stringify(invoiceData), currency: $cmbCurrency.val(),
                                _C_BPartner_ID: _C_BPartner_ID, _windowNo: self.windowNo, payment: payment, DateTrx: $date.val(), appliedamt: applied
                                , discount: discount, writeOff: writeOff, open: open, DateAcct: DateAcct, _CurrencyType_ID: C_CurrencyType_ID, isInterBPartner: false, conversionDate: conversionDate, chk: chk
                            }),
                            success: function (result) {
                                // Clear Selected invoices array when we de-select the select all checkbox. work done for to hold all the selected invoices
                                selectedInvoices = [];
                                selectedPayments = [];
                                setallgridsLoaded = true;
                                loadBPartner();
                                VIS.ADialog.info("", true, result, "");
                                $bsyDiv[0].style.visibility = "hidden";
                            },
                            error: function () {
                                VIS.ADialog.info("", true, result, "");
                                $bsyDiv[0].style.visibility = "hidden";
                            }
                        });
                    }
                },
                error: function (result) {
                    VIS.ADialog.info("", true, result, "");
                    $bsyDiv[0].style.visibility = "hidden";
                }
            });
        };

        //added for gl-allocation
        function glData(rowsPayment, rowsInvoice, rowsCash, rowsGLVoucher, DateTrx, DateAcct) {
            $.ajax({
                url: VIS.Application.contextUrl + "VIS/PaymentAllocation/CheckPeriodState",
                data: { DateTrx: $date.val(), AD_Org_ID: $cmbOrg.val() },
                async: false,
                success: function (result) {
                    if (result != "") {
                        var e;
                        VIS.ADialog.info("", true, result, "");
                        setallgridsLoaded = true;
                        loadBPartner();
                        //loadGLDataGrid(e);
                        $bsyDiv[0].style.visibility = "hidden";
                        return "";
                    }
                    else {
                        $bsyDiv[0].style.visibility = "visible";
                        var payment = "";
                        var applied = "";
                        var discount = "";
                        var writeOff = "";
                        var open = "";
                        var C_CurrencyType_ID = 0;
                        var paymentData = [];
                        var cashData = [];
                        var glData = [];
                        var invoiceData = [];
                        var chk = $vchkMultiCurrency.is(':checked');// isMultiCurrency
                        if (chk) {
                            var conversionDate = Globalize.format(new Date($conversionDate.val()), "yyyy-MM-dd");// Conversion Date
                        }
                        else {
                            var conversionDate = Globalize.format(new Date(), "yyyy-MM-dd");
                        }

                        //Payment Data
                        if (rowsPayment.length > 0) {
                            var keys = Object.keys($gridPayment.get(0));
                            payment = keys[keys.indexOf("AppliedAmt")];
                            for (var i = 0; i < rowsPayment.length; i++) {
                                var row = $gridPayment.get(rowsPayment[i].recid);
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                if (rowsPayment[i].AppliedAmt != undefined && rowsPayment[i].AppliedAmt != 0) {
                                    var appliedAmt = rowsPayment[i].AppliedAmt;
                                    appliedAmt = convertAppliedAmtculture(appliedAmt, dotFormatter);
                                    paymentData.push({
                                        AppliedAmt: appliedAmt, Date: row.Date1, Converted: row.ConvertedAmount, cpaymentid: row.CpaymentID, Documentno: row.Documentno, Isocode: row.Isocode,
                                        Multiplierap: row.Multiplierap, OpenAmt: row.OpenAmt, Payment: row.Payment, Org: parseInt($cmbOrg.val()), IsPaid: false, paidAmt: 0, payment: payment
                                    });
                                }
                            }
                        }

                        //Cash Data
                        if (rowsCash.length > 0) {
                            var keys = Object.keys($gridCashline.get(0));
                            payment = keys[keys.indexOf("AppliedAmt")];
                            for (var i = 0; i < rowsCash.length; i++) {
                                var row = $gridCashline.get(rowsCash[i].recid);
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                if (rowsCash[i].AppliedAmt != 0 && rowsCash[i].AppliedAmt != undefined) {
                                    var appliedAmt = rowsCash[i].AppliedAmt;
                                    appliedAmt = convertAppliedAmtculture(appliedAmt, dotFormatter);
                                    cashData.push({
                                        AppliedAmt: appliedAmt, Date: row.Created, Amount: row.Amount, ccashlineid: row.CcashlineiID, Converted: row.ConvertedAmount, Isocode: row.Isocode,
                                        Multiplierap: row.Multiplierap, OpenAmt: row.OpenAmt, ReceiptNo: row.ReceiptNo, Org: parseInt($cmbOrg.val()), IsPaid: false, paidAmt: 0, payment: payment
                                    });
                                }
                            }
                        }

                        if (rowsInvoice.length > 0) {
                            var keys = Object.keys($gridInvoice.get(0));
                            applied = keys[keys.indexOf("AppliedAmt")];
                            discount = keys[keys.indexOf("Discount")];
                            writeOff = keys[keys.indexOf("Writeoff")];
                            open = keys[keys.indexOf("Amount")];
                            for (var i = 0; i < rowsInvoice.length; i++) {
                                var row = $gridInvoice.get(rowsInvoice[i].recid);
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                var discounts = rowsInvoice[i].Discount;
                                if (discounts == undefined) {
                                    discounts = row.Discount;
                                }
                                var appliedamts = rowsInvoice[i].AppliedAmt;
                                if (appliedamts == undefined) {
                                    appliedamts = row.AppliedAmt;
                                }
                                var writeoffs = rowsInvoice[i].Writeoff;
                                if (writeoffs == undefined) {
                                    writeoffs = row.Writeoff;
                                }
                                if (appliedamts != 0 && appliedamts != undefined) {
                                    appliedamts = convertAppliedAmtculture(appliedamts, dotFormatter);
                                    discounts = convertAppliedAmtculture(discounts, dotFormatter);
                                    writeoffs = convertAppliedAmtculture(writeoffs, dotFormatter);
                                    if (countVA009 <= 0) {
                                        invoiceData.push({
                                            AppliedAmt: appliedamts, Discount: discounts, Writeoff: writeoffs,
                                            cinvoiceid: row.CinvoiceID, Converted: row.Converted, Currency: row.Currency,
                                            Date: row.Date1, Docbasetype: row.DocBaseType, applied: applied, discount: discount, open: open, writeOff: writeOff,
                                            documentno: row.Documentno, Isocode: row.Isocode, Multiplierap: row.Multiplierap, Amount: row.Amount, Org: parseInt($cmbOrg.val()), IsPaid: false, paidAmt: 0
                                        });
                                    }
                                    else {
                                        invoiceData.push({
                                            AppliedAmt: appliedamts, Discount: discounts, Writeoff: writeoffs,
                                            cinvoiceid: row.CinvoiceID, Converted: row.Converted, Currency: row.Currency,
                                            //Date: row.Date1, Docbasetype: row.DocBaseType,
                                            // send invoice schedule date if va009 module is updated
                                            Date: row.InvoiceScheduleDate, Docbasetype: row.DocBaseType, applied: applied, discount: discount, open: open, writeOff: writeOff,
                                            documentno: row.Documentno, Isocode: row.Isocode, Multiplierap: row.Multiplierap, Amount: row.Amount,
                                            c_invoicepayschedule_id: row.C_InvoicePaySchedule_ID, Org: parseInt($cmbOrg.val()), IsPaid: false, paidAmt: 0
                                        });
                                    }
                                }
                            }
                        }

                        if (rowsGLVoucher.length > 0) {

                            for (var i = 0; i < rowsGLVoucher.length; i++) {
                                var row = $glLineGrid.get(rowsGLVoucher[i].recid);
                                C_CurrencyType_ID = parseInt(row.C_ConversionType_ID);
                                if (rowsGLVoucher[i].AppliedAmt != undefined && rowsGLVoucher[i].AppliedAmt != 0) {
                                    glData.push({
                                        AppliedAmt: rowsGLVoucher[i].AppliedAmt, Date: row.DATEACCT, DateDoc: row.DATEDOC, DocumentNo: row.DOCUMENTNO,
                                        OpenAmount: row.OpenAmount, GL_JournalLine_ID: row.GL_JOURNALLINE_ID, GL_Journal_ID: row.GL_Journal_ID,
                                        ConvertedAmount: row.ConvertedAmount, Org: parseInt($cmbOrg.val()), IsPaid: false, paidAmt: 0
                                    });
                                }
                            }
                        }
                        saveGLData(JSON.stringify(paymentData), JSON.stringify(invoiceData), JSON.stringify(cashData), JSON.stringify(glData), $date.val(), C_CurrencyType_ID, applied, discount, open, payment, writeOff, conversionDate, chk);
                    }
                },
                error: function (result) {
                    VIS.ADialog.info("", true, result, "");
                    $bsyDiv[0].style.visibility = "hidden";
                }
            });
        };

        function saveGLData(rowsPayment, rowsInvoice, rowsCash, rowsGLVoucher, DateTrx, C_CurrencyType_ID, applied, discount, open, payment, writeOff, conversionDate, chk) {
            $.ajax({
                url: VIS.Application.contextUrl + "VIS/PaymentAllocation/saveGLJData",
                type: 'POST',
                data: {
                    paymentData: rowsPayment, invoiceData: rowsInvoice, cashData: rowsCash, glData: rowsGLVoucher, DateTrx: DateTrx, _windowNo: self.windowNo,
                    C_Currency_ID: _C_Currency_ID, C_BPartner_ID: _C_BPartner_ID, AD_Org_ID: $cmbOrg.val(), C_CurrencyType_ID: C_CurrencyType_ID,
                    DateAcct: $dateAcct.val(), applied: applied, discount: discount, open: open, payment: payment, writeOff: writeOff, conversionDate: conversionDate, chk: chk
                },
                async: false,
                success: function (result) {
                    //var e;
                    //VIS.ADialog.info("", true, result, "");
                    selectedInvoices = [];
                    selectedPayments = [];
                    selectedCashlines = [];
                    SelectedGL = [];
                    getGLChanges = []; //clear the array which is holding Credit or Debit Amount records.
                    setallgridsLoaded = true;
                    loadBPartner();
                    //loadGLDataGrid(e);
                    if ($gridCashline)
                        $gridCashline.refresh();
                    if ($glLineGrid)
                        $glLineGrid.refresh();
                    if ($gridInvoice)
                        $gridInvoice.refresh();
                    if ($gridPayment)
                        $gridPayment.refresh();
                    var format = VIS.DisplayType.GetNumberFormat(VIS.DisplayType.Amount);
                    $lblglSum.text(0 + " " + VIS.Msg.getMsg("SelectedLines") + " - " + summation + " " + format.GetFormatedValue(0) + " ");//VIS.Msg.getMsg("SelectedGL") +
                    $vtxtDifference.text(format.GetFormatedValue(0));
                    //$vchkGlVoucher.trigger("click");
                    $bsyDiv[0].style.visibility = "hidden";
                    VIS.ADialog.info("", true, result, "");
                },
                error: function (result) {
                    VIS.ADialog.info("", true, result, "");
                    $bsyDiv[0].style.visibility = "hidden";
                }
            });
        };
        //end

        /**Work done for searching from left parameters */
        function searchbyParameters() {
            console.info("BPartner Name : " + $vSearchBPartner.getName() + " , Value : " + $vSearchBPartner.value);
            console.info("Currency ID : " + $cmbCurrency.val());
            console.info("Allocation From : " + $allocationFrom.val());
            console.info("Allocation To : " + $allocationTo.val());
            console.info("MultiCurrency : " + $vchkMultiCurrency.is(':checked'));
            //VIS_0045: Selection Parameter value
            console.info("Currency Type : " + $vConversionType.getName() + " , Value : " + $vConversionType.getValue());
            console.info("Organization : " + $cmbOrg.val());
            console.info("Document Number : " + $txtDocNo.val());
            console.info("Document Type : " + $cmbDocType.val());
            console.info("Pay Document Type : " + $payDocType.val());
            console.info("From Date : " + $fromDate.val());
            console.info("To Date : " + $toDate.val());
            console.info("Conversion Date : " + $conversionDate.val());
            $bsyDiv[0].style.visibility = "visible";
            // initialize Pageno as 1 when we change BP
            pageNoInvoice = 1, gridPgnoInvoice = 1, invoiceRecord = 0;
            pageNoCashJournal = 1, gridPgnoCashJounal = 1, CashRecord = 0;
            pageNoPayment = 1, gridPgnoPayment = 1, paymentRecord = 0;
            pageNoGL = 1, gridPgnoGL = 1, glRecord = 0;
            isPaymentGridLoaded = false, isCashGridLoaded = false, isInvoiceGridLoaded = false, isGLGridLoaded = false;
            C_ConversionType_ID = 0;
            /********************************
         *  Load unallocated Payments
         *      1-TrxDate, 2-DocumentNo, (3-Currency, 4-PayAmt,)
         *      5-ConvAmt, 6-ConvOpen, 7-Allocated
         */
            // did work for send only call to specific controller which is selected 
            if ($allocationFrom.val() == "P" | $allocationTo.val() == "P")
                loadUnallocatedPayments();
            else
                isPaymentGridLoaded = true;

            /********************************
         *  Load unallocated Cash lines
         *      1-TrxDate, 2-DocumentNo, (3-Currency, 4-PayAmt,)
         *      5-ConvAmt, 6-ConvOpen, 7-Allocated
         */
            // did work for send only call to specific controller which is selected 
            if ($allocationFrom.val() == "C" | $allocationTo.val() == "C")
                loadUnallocatedCashLines();
            else
                isCashGridLoaded = true;

            // did work for send only call to specific controller which is selected 
            if ($allocationFrom.val() == "I" | $allocationTo.val() == "I")
                loadInvoice();
            else
                isInvoiceGridLoaded = true;

            // did work for send only call to specific controller which is selected 
            if ($allocationFrom.val() == "G" | $allocationTo.val() == "G")
                loadGLVoucher();
            else
                isGLGridLoaded = true;
            //show the selected grids
            displayGrids($allocationFrom.val(), $allocationTo.val());
        };

        /**Load all grids as blank */
        function blankAllGrids() {
            var isMultiCurrency = $vchkMultiCurrency.is(':checked');
            //loadGLGrid();
            bindInvoiceGrid([], isMultiCurrency);
            bindPaymentGrid([], isMultiCurrency);
            bindCashline([], isMultiCurrency);
            bindGLGrid([], isMultiCurrency);
            //$($(glLineGrid.box)[0]).find('.w2ui-search-down').css('margin-top', '4px');
            //$($(glLineGrid.box)[0]).find('.w2ui-search-clear').css('margin-top', '4px');
            $('.vis-allocate-glSum').css('text-align', 'right');
            $('.vis-allocate-glSum').css('margin-right', '10px');
        };

        this.disposeComponent = function () {
            if ($gridPayment != undefined && $gridPayment != null) {
                $gridPayment.destroy();
                $gridPayment = null;
            }
            if ($gridCashline != undefined && $gridCashline != null) {
                $gridCashline.destroy();
                $gridCashline = null;
            }
            if ($gridInvoice != undefined && $gridInvoice != null) {
                $gridInvoice.destroy();
                $gridInvoice = null;
            }
            if ($glLineGrid != undefined && $glLineGrid != null) {
                $glLineGrid.destroy();
                $glLineGrid = null;
            }
            $invSelectAll = null;
            $paymentSelctAll = null;
            $cashSelctAll = null;
            $glLineGrid = null;
            _allDates = null;
        };
        this.getRoot = function () {
            return $root;
        };

        this.refreshUI = function () {
            /*Refresh Grid on Focus*/
            $gridPayment.resize();
            $gridCashline.resize();
            $gridInvoice.resize();
            $glLineGrid.resize();
        }
    };

    VAllocation.prototype.init = function (windowNo, frame) {
        this.frame = frame;
        this.windowNo = windowNo;
        this.Initialize();
        this.frame.getContentGrid().append(this.getRoot());
    };

    VAllocation.prototype.refresh = function () {
        this.refreshUI();
    };

    VAllocation.prototype.dispose = function () {
        /*CleanUp Code */
        //dispose this component
        this.disposeComponent();
        //call frame dispose function
        if (this.frame)
            this.frame.dispose();
        this.frame = null;
    };

    VIS.Apps = VIS.Apps || {};
    VIS.Apps.AForms = VIS.Apps.AForms || {};
    VIS.Apps.AForms.VAllocation = VAllocation;

})(VIS, jQuery)