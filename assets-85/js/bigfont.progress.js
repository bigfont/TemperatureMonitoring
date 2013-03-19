(function () {

    'use strict';
    /*global $:false, window:false, bigfont:false */
    /*jslint plusplus: true, white: true */

    var progress;
    progress = {};
    window.bigfont.progress = progress;

    progress.UPDATE_INTERVAL = 100; // one tenth of a second
    progress.PAUSE_FOR_EFFECT = 1000;
    progress.MAX_VALUE = 100;
    progress.progressContainer = null;
    progress.progressElement = null;
    progress.progressFallbackSpan = null;
    progress.isFullScreen = null;
    progress.timeoutID = null;

    function resetProgressBarState() {

        progress.progressContainer = null;
        progress.progressElement = null;
        progress.progressFallbackSpan = null;
        progress.isFullScreen = null;
        progress.timeoutID = null;

    }

    function toggleProgressVisibility() {

        var html;
        html = $('html');

        if (progress.progressContainer.hasClass('hidden')) {

            // show 
            progress.progressContainer.removeClass('hidden').addClass('visible').show(); // ie again

            if (progress.isFullScreen) {
                html.addClass('invisible');
            }


        } else {

            // hide
            progress.progressContainer.removeClass('visible').addClass('hidden').hide(); // its very hard to hide something in IE!!!

            if (progress.isFullScreen) {
                html.removeClass('invisible');
            }

        }
    }

    function updateProgress() {

        var cp, cs;

        // get the current values
        cp = progress.progressElement.val();
        cs = parseInt(progress.progressFallbackSpan.text(), bigfont.constants.RADIX_DECIMAL);

        // increment the current values by one
        ++cp;
        ++cs;

        // if current progress bar value exceeds the max, reset it to zero
        if (cp > progress.progressElement.attr('max')) {
            cp = 0;
        }

        // increment by one and update the elements
        progress.progressElement.val(cp);
        progress.progressFallbackSpan.text(cs);

    }

    function startProgress(progressID, isFullScreen) {

        if (progress.progressContainer === undefined || progress.progressContainer === null) {

            $('input').prop('disabled', true);

            // get progress element and legacy fallback
            progress.progressContainer = $('aside#' + progressID);
            progress.progressElement = $(progress.progressContainer.find('progress')[0]);
            progress.progressFallbackSpan = $(progress.progressContainer.find('span')[0]);

            progress.isFullScreen = isFullScreen;

            progress.progressElement.val(0);
            progress.progressElement.prop('max', progress.MAX_VALUE);
            progress.progressFallbackSpan.text(0);

            toggleProgressVisibility();
            updateProgress();
            progress.timeoutID = window.setInterval(updateProgress, progress.UPDATE_INTERVAL);

        }

    }

    function stopProgress() {

        if (progress.progressContainer !== undefined && progress.progressContainer !== null) {

            // prevent more calls to update progress
            window.clearInterval(progress.timeoutID);

            // set the progress element value to its max
            progress.progressElement.val(progress.MAX_VALUE);

            // pause for effect, lest the progress bar sometimes just flashes on and off too quickly, 
            // and then...            
            window.setTimeout(function () {

                // hide the progress bar
                toggleProgressVisibility();

                // re-enable any disabled inputs
                $('input').prop('disabled', false);

                // reset the state of the progress bar
                resetProgressBarState();

            }, progress.PAUSE_FOR_EFFECT);

        }

    }

    progress.startProgress = startProgress;
    progress.stopProgress = stopProgress;

} ());