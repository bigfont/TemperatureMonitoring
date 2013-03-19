(function () {

    'use strict';
    /*global window:false */
    /*jslint plusplus: true, white: true */

    var datetime;

    datetime = {};
    window.bigfont.datetime = datetime;

    // turn a Date object into a friendly date string
    datetime.convertDateObjectIntoFriendlyDateObject = function (d) {

        var obj, month_names;

        obj = {

            monthNumber: 0,
            monthName: '',
            monthAbbreviation: '',
            dayNumber: 0,
            yearNumber: 0,
            hour24: 0,
            amPM: '',
            hour12: 0,
            minuteNumber: 0,
            minuteClockDisplay: ''

        };

        month_names = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

        // monthAbbreviation
        obj.monthNumber = parseInt(d.getMonth(), window.bigfont.constants.RADIX_DECIMAL); // zero indexed (0-11)
        obj.monthName = month_names[obj.monthNumber];
        obj.monthAbbreviation = obj.monthName.substring(0, 3);

        // dayNumber & yearNumber
        obj.dayNumber = parseInt(d.getDate(), window.bigfont.constants.RADIX_DECIMAL); // one indexed (1-31)
        obj.yearNumber = parseInt(d.getFullYear(), window.bigfont.constants.RADIX_DECIMAL);

        // hour12 and amPM
        obj.hour24 = parseInt(d.getHours(), window.bigfont.constants.RADIX_DECIMAL); // zero indexed (0-23)        
        obj.amPM = 'AM';
        if (obj.hour24 === 0) {

            // 12 AM
            obj.hour12 = 12;

        } else if (obj.hour24 <= 12) {

            // 1 AM to 12 AM 
            obj.hour12 = parseInt(obj.hour24, window.bigfont.constants.RADIX_DECIMAL);

        } else {

            // 1 PM to 11 PM
            obj.hour12 = parseInt(obj.hour24, window.bigfont.constants.RADIX_DECIMAL) % 12;
            obj.amPM = 'PM';

        }

        // minuteClockDispay
        obj.minuteNumber = parseInt(d.getMinutes(), window.bigfont.constants.RADIX_DECIMAL);
        obj.minuteClockDispay = (obj.minuteNumber < 10 ? '0' : '') + obj.minuteNumber;

        return obj;

    };

    datetime.convertDateObjectIntoFriendlyDateString = function (d, shorten) {

        var obj, friendly;

        obj = datetime.convertDateObjectIntoFriendlyDateObject(d);

        if (shorten) {
            friendly = obj.monthAbbreviation + ' ' + obj.dayNumber + '<br>' + ' ' + obj.hour12 + ':' + obj.minuteClockDispay + ' ' + obj.amPM;
        } else {
            friendly = obj.monthAbbreviation + ' ' + obj.dayNumber + ', ' + obj.yearNumber + ' ' + obj.hour12 + ':' + obj.minuteClockDispay + ' ' + obj.amPM;
        }

        return friendly;

    };

    datetime.convertUtcJavaScriptTimestampIntoLocalizedDateObject = function (utcJavascriptTimestamp) {

        var localizedDateObject;
        // when we call the Date() constructor with a UTC javascript timestamp
        // the constructor creates a JavaScript Date object for the timestamp in local time
        localizedDateObject = new Date(parseInt(utcJavascriptTimestamp, window.bigfont.constants.RADIX_DECIMAL));
        return localizedDateObject;

    };

    datetime.getAbsoluteDifferenceInDaysBetweenTwoJavascriptTimestamps = function (javascriptTimestampOne, javascriptTimestampTwo) {
        var milliseconds, seconds, minutes, hours, days;
        milliseconds = Math.abs(javascriptTimestampOne - javascriptTimestampTwo);
        seconds = milliseconds / 1000;
        minutes = seconds / 60;
        hours = minutes / 60;
        days = hours / 24;
        return days;
    };

} ());