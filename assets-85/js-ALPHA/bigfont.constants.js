(function () {

    'use strict';
    /*global window:false */
    /*jslint plusplus: true, white: true */

    var constants;

    constants = {};
    window.bigfont.constants = constants;

    constants.MILLISECONDS_PER_SECOND = 1000;
    constants.MILLISECONDS_PER_MINUTE = 60000;
    constants.MILLISECONDS_PER_HOUR = (60 * constants.MILLISECONDS_PER_MINUTE);
    constants.MILLISECONDS_PER_DAY = (24 * constants.MILLISECONDS_PER_HOUR);
    constants.RADIX_DECIMAL = 10;
    constants.BASE_URL = window.location.protocol + '//' + window.location.hostname;

} ());