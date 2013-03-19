(function () {

    'use strict';
    /*global $:false, document:false, window:false, bigfont:false */
    /*jslint plusplus: true, white: true */

    function displayUserMessage(message, messageType) {

        var userMessageContainer = $('aside.user-message-variable');
        // notify user                 
        userMessageContainer.empty();
        userMessageContainer.append(message);
        userMessageContainer.removeClass('success failure info'); // remove the messageType classes
        userMessageContainer.addClass(messageType);

        if (userMessageContainer.is(":animated")) {

            userMessageContainer.stop(true, true);
            userMessageContainer.fadeIn('fast');

        } else {

            userMessageContainer.show('fast');

        }

        userMessageContainer.fadeOut(8000);
    }

    function displaySystemSettings() {

        var obj_systemSettings, table, th, tr, thRomID, inputRomID, tdFriendlyName, inputFriendlyName, tdTempThreshold, inputTempThreshold;

        // get the device dictionary that we stored in the page with jQuery
        obj_systemSettings = $('body').data('systemSettings');

        // populate the General Settings
        $('[name="data-store-duration-in-days"]').val(obj_systemSettings.DataStoreDurationInDays);
        $('[name="max-email-freq-in-hours"]').val(obj_systemSettings.HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails);
        $('[name="warning-email-recipients-csv"]').val(obj_systemSettings.WarningEmailRecipientsInCsv);

        // populate the Thermometer Specific Settings

        // get the table
        table = $("table#thermometer-specific-settings");
        table.empty();

        // create the table headers
        tr = $("<tr/>");
        th = $("<th/>", { text: "RomID", "class": "first" }).appendTo(tr);
        th = $("<th/>", { text: "Friendly Name", "class": "second" }).appendTo(tr);
        th = $("<th/>", { html: "Temperature Threshold &deg;C", "class": "third" }).appendTo(tr); // use html not text to render the degree symbol
        table.append(tr);

        $.each(obj_systemSettings.DeviceSettingsDictionary, function (index, val_device) {

            // create romID column            
            inputRomID = $("<input/>", { value: val_device.Key, name: "rom-id-" + index, type: "hidden" });
            thRomID = $("<th/>", { text: val_device.Key, scope: "row" }).append(inputRomID);

            // create friendlyName column
            inputFriendlyName = $("<input/>", {
                "class": "required alphanumeric",
                name: "friendly-name-" + index,
                value: val_device.Value.FriendlyName
            });
            tdFriendlyName = $("<td/>").append(inputFriendlyName);

            // create temp threshold column
            inputTempThreshold = $("<input/>", {
                "class": "required",
                max: 40,
                min: -40,
                name: "temp-threshold-" + index,
                type: "number",
                value: val_device.Value.TemperatureThreshold
            });
            tdTempThreshold = $("<td/>").append(inputTempThreshold);

            // create table row and append columns
            tr = $("<tr/>").append(thRomID).append(tdFriendlyName).append(tdTempThreshold);

            // append table row to table
            table.append(tr);

        });

    }

    function userHasConfirmedThatWeCanDeleteOldData() {

        var obj_systemSettings, newDuration, oldDuration, result;

        // get the system settings that we loaded from the server
        obj_systemSettings = $('body').data('systemSettings');

        // test the old dataStoreDurationDays against the new value
        oldDuration = parseInt(obj_systemSettings.DataStoreDurationInDays, bigfont.constants.RADIX_DECIMAL);
        newDuration = parseInt($('[name="data-store-duration-in-days"]').val(), bigfont.constants.RADIX_DECIMAL);

        // assume true
        result = true;

        if (newDuration < oldDuration) {

            // now assume false
            result = false;

            // if the new is shorter, then confirm deletion of old data
            if (window.confirm('Hey! You shrunk the data store duration. Are you sure you want to delete old data forever?')) {

                // the user is sure, so update the value
                obj_systemSettings.DataStoreDurationInDays = newDuration;
                result = true;

            }
        }

        return result;

    }

    function postSystemSettings(form) {

        var data, jqxhr;

        if (!userHasConfirmedThatWeCanDeleteOldData()) {

            return;

        }

        // serialize the form data using jQuery
        data = $(form).serialize();

        // POST to wcf in a way that mimics an html form POST
        jqxhr = $.ajax({
            type: "POST",
            url: window.bigfont.constants.BASE_URL + "/WcfRestService.svc/ReceiveSystemSettingsFromUserInterface",
            data: data,
            contentType: "application/x-www-form-urlencoded; charset=utf-8", // this content type is vital for mimicking a form POST
            dataType: "text"
        });

        jqxhr.done(function (obj) {

            var json;
            json = $.parseJSON(obj);

            if (json.Message === 'unauthorized') {

                // inform user
                displayUserMessage('Username and password did not match.', 'failure');

            } else {

                // refresh the chart data
                window.bigfont.charting.getDataPoints(false);
                // inform user
                displayUserMessage('Save complete.', 'success');

            }

        });

        // prevent the default form action
        return false;
    }

    function getSystemSettings() {

        var jqxhr = $.ajax({
            type: "GET",
            url: window.bigfont.constants.BASE_URL + "/WcfRestService.svc/SendSystemSettingsToUserInterface",
            dataType: "json"
        });

        jqxhr.done(function (obj) {

            // store the data locally                    
            $('body').data('systemSettings', obj);

            // display the system settings in the UI
            displaySystemSettings();

        });

    }

    function sendTestEmail() {

        var data, jqxhr, valid, form;

        // check for validity of just three fields not the entire form
        valid = $('.validate-for-email').valid();
        if (valid === 0) {
            return;
        }

        // get the form
        form = $('form');

        // serialize it
        data = $(form).serialize();

        // POST to wcf in a way that mimics an html form POST
        jqxhr = $.ajax({
            type: "POST",
            url: window.bigfont.constants.BASE_URL + "/WcfRestService.svc/SendTestEmail",
            data: data,
            contentType: "application/x-www-form-urlencoded; charset=utf-8", // this content type is vital for mimicking a form POST
            dataType: "text"
        });

        jqxhr.done(function (obj) {

            var json;
            json = $.parseJSON(obj);

            if (json.Message === 'unauthorized') {

                // inform user
                displayUserMessage('Username and password did not match.', 'failure');


            } else {

                // notify user                        
                displayUserMessage('Please check your email in about 5 minutes.', 'info');

            }

        });

        // prevent the default form action
        return false;
    }

    $(document).ready(function () {

        var validator;

        $.ajaxSetup({ cache: false });

        bigfont.charting.getDataPoints(true);

        getSystemSettings();

        // set the validate options for the form
        validator = $('form').validate({

            // customize the error message placement
            errorPlacement: function (error, element) {

                // check if the element is in a table
                var isInTable = $(element).parents('table').length > 0 ? true : false;

                if (isInTable === true) {
                    // if it's in a table, then just insert the error after the input element
                    error.insertAfter(element);
                } else {
                    // otherwise, insert the error after the first parent label
                    error.insertAfter(element.parent('label'));
                }
            },
            // prevent eager validation because it can be annoying
            // for whatever reason, jquery validate is always eagerly validating
            onfocusout: false,
            onkeyup: false,
            onclick: false
        });

        // handle the submit event
        $('form').submit(function (e) {

            var valid;

            // check the form validity
            valid = $('form').valid();

            if (valid) {

                // post the form data
                // TODO Server Side validation yet
                postSystemSettings(this);
            }

            // prevent the form default,
            // because we're posting via jQuery
            return false;
        });

        // refresh the settings to the last save point
        $('#refresh-system-settings-form').click(function () {

            getSystemSettings();
            validator.resetForm();

        });

        // send a test email
        $('#send-test-email').click(function () {

            sendTestEmail();

        });

        // show and hide help text
        $("#show-help").click(function (e) {

            e.stopPropagation();
            $("#help").show();

        });

        $(document).add("#close-help").click(function () {

            $("#help").hide();

        });

        $("#help").click(function (e) {

            e.stopPropagation();    // This is the preferred method.
            return false;           // This should not be used unless you do not want any click events registering inside the div

        });

    });

} ());