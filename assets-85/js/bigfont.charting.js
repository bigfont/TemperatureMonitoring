(function () {

    'use strict';
    /*global window:false, $:false */
    /*jslint plusplus: true, white: true */

    var charting, UPDATE_INTERVAL_IN_MILLISECONDS_FOR_FETCHING_FRESH_DATA, HIGH_RESOLUTION_MAX_RANGE_IN_MILLISECONDS;

    UPDATE_INTERVAL_IN_MILLISECONDS_FOR_FETCHING_FRESH_DATA = (5 * window.bigfont.constants.MILLISECONDS_PER_MINUTE);
    HIGH_RESOLUTION_MAX_RANGE_IN_MILLISECONDS = (12 * window.bigfont.constants.MILLISECONDS_PER_HOUR);

    charting = {};
    window.bigfont.charting = charting;

    function xaxisDateTimeFormatter(v, axis) {

        var localDateObj, friendlyLocalDateString;
        localDateObj = window.bigfont.datetime.convertUtcJavaScriptTimestampIntoLocalizedDateObject(v);
        friendlyLocalDateString = window.bigfont.datetime.convertDateObjectIntoFriendlyDateString(localDateObj, true);
        return friendlyLocalDateString;
    }

    function yaxisTemperatureFormatter(v, axis) {

        var friendlyTemperatureString;
        friendlyTemperatureString = v.toFixed(0) + "&deg;C";
        return friendlyTemperatureString;

    }

    // set the options for the Flot chart
    function getGenericChartOptions() {

        var options = {
            legend: {
                show: false
            },
            xaxis: {
                color: "black", // label font color
                labelWidth: 75,
                tickColor: "silver", // line color 
                tickFormatter: xaxisDateTimeFormatter,
                ticks: 6, // setting ticks is only a suggestion, flot still decides 
                mode: "time",
                position: "bottom"
            },
            yaxis: {
                color: "black",
                labelWidth: 50,
                tickColor: "silver",
                tickFormatter: yaxisTemperatureFormatter,
                position: "left"
            },
            series: {
                lines: { show: true, lineWidth: 1, fill: false },
                points: { show: true, radius: 1 }, // they look ugly, slow things down, and aren't necessary
                bars: { show: false }, // they mess stuff up for now.
                color: 'red' // the color of the line
            },
            grid: {
                show: true,
                labelMargin: 15,
                hoverable: true,
                color: "black"
            },
            selection: {
                mode: "x",
                color: "#2670DA"
            }
        };
        return options;
    }

    function isHighResolution(zoomRange) {

        var isHighRes, zoomRangeInMilliseconds;
        isHighRes = false;
        // if we are zooming in
        if (zoomRange !== undefined) {
            // get the zoom range in milliseconds
            zoomRangeInMilliseconds = Math.abs(zoomRange.xaxis.from - zoomRange.xaxis.to);
            if (zoomRangeInMilliseconds !== undefined && zoomRangeInMilliseconds <= HIGH_RESOLUTION_MAX_RANGE_IN_MILLISECONDS) {
                // and if appropriate
                isHighRes = true;
            }
        }
        return isHighRes;
    }

    function getHighOrLowResolutionChartData(chartNumber, chartClass, zoomRange) {

        var chartData;

        // if the zoom range in days is defined and is within high resolution threshold
        if (isHighResolution(zoomRange)) {

            // get the high resolution data
            chartData = [window.bigfont.charting.chartDataCollection_highRes[parseInt(chartNumber, window.bigfont.constants.RADIX_DECIMAL)]];

        } else {

            // otherwise get the low resolution data
            chartData = [window.bigfont.charting.chartDataCollection_lowRes[parseInt(chartNumber, window.bigfont.constants.RADIX_DECIMAL)]];

        }

        return chartData;

    }

    function customizeTheChartOptions(chartNumber, chartClass, zoomRange, chartOptions) {

        // we need to get the highResChartData 
        // in order to determine its maximum and minimum value
        var highResChartData;

        // if we are zooming in
        if (zoomRange !== undefined) {

            // then we need to set the chart xaxis and yaxis
            // to the proper zoom level
            chartOptions.xaxis.min = zoomRange.xaxis.from;
            chartOptions.xaxis.max = zoomRange.xaxis.to;
            chartOptions.yaxis.min = zoomRange.yaxis.from;
            chartOptions.yaxis.max = zoomRange.yaxis.to;

        } else {

            // if we do NOT have zoom ranges,
            // then we set the chart yaxis
            // to the extremes of the high resolution yaxis values
            // so that the overview chart and the details chart 
            // always have the same yaxis range even when
            // the overview is lowRes and the details is highRes     
            // this prevents the details from cutting of part of the chart on zoom                        
            highResChartData = window.bigfont.charting.chartDataCollection_highRes[parseInt(chartNumber, window.bigfont.constants.RADIX_DECIMAL)];
            chartOptions.yaxis.min = highResChartData.minTemperature - 5;
            chartOptions.yaxis.max = highResChartData.maxTemperature + 5;

        }

        if (isHighResolution(zoomRange)) {

            chartOptions.series.color = 'green';

        }

        return chartOptions;

    }

    function plotSpecificChart(chartNumber, chartClass, zoomRange) {

        var chartDiv, chartData, genericChartOptions, customChartOptions, chartObj;

        // get the chartDiv
        chartDiv = $('div.' + chartClass + '[data-chart-number=' + chartNumber + ']');

        // get the chartData
        chartData = getHighOrLowResolutionChartData(chartNumber, chartClass, zoomRange);

        // get the genericChartOptions
        genericChartOptions = getGenericChartOptions();

        // customize the chart axes
        customChartOptions = customizeTheChartOptions(chartNumber, chartClass, zoomRange, genericChartOptions);

        // plot and capture the plot obj
        chartObj = $.plot(chartDiv, chartData, customChartOptions);

        return chartObj;

    }

    function plotUnselectedEventHandler(event) {

        var chartNumber;

        // get the chartNumber
        chartNumber = $(event.target).attr('data-chart-number');

        // replot the details chart
        plotSpecificChart(chartNumber, 'chart-details');

    }

    function plotSelectedEventHandler(event, ranges) {

        var chartNumber, detailsChartObj, overviewPlotObj;

        // get the chartNumber
        chartNumber = $(event.target).attr('data-chart-number');

        // replot the details chart with the new axis
        detailsChartObj = plotSpecificChart(chartNumber, 'chart-details', ranges);

        // replot the overview chart and update its range selector
        overviewPlotObj = plotSpecificChart(chartNumber, 'chart-overview');
        overviewPlotObj.setSelection(ranges, true);

    }

    function replot() {

        // TODO Implement replot (nice-to-have)
        // this is NOT in the requirements
    }

    function plot() {

        var overviewPlotObj, detailsChartObj, article, detailsDiv, overviewDiv, chartName, chartNumber;

        // get the article which will contain the charts
        article = $('article');

        for (chartNumber = 0; chartNumber < window.bigfont.charting.chartDataCollection_lowRes.length; ++chartNumber) {

            // get the chartName from its dataCollection
            chartName = window.bigfont.charting.chartDataCollection_lowRes[chartNumber].label;

            // add the chart header        
            $('<h1/>', { text: chartName }).appendTo(article);

            // add the overviewDiv
            $('<h2/>', { text: 'Overview' }).appendTo(article);
            overviewDiv = $('<div/>', { 'class': 'chart chart-overview', 'data-chart-number': chartNumber }).appendTo(article);

            // add the detailsDiv and its toolbox
            $('<h2/>', { text: 'Details' }).appendTo(article);
            detailsDiv = $('<div/>', { 'class': 'chart chart-details', 'data-chart-number': chartNumber }).appendTo(article);

            // plot the details and overview charts
            detailsChartObj = plotSpecificChart(chartNumber, 'chart-details');
            overviewPlotObj = plotSpecificChart(chartNumber, 'chart-overview');

        }

    }

    function addEventHandlers() {

        // enable selection plugin on both charts
        $('div.chart').bind("plotselected", function (event, ranges) {

            // then handle it
            plotSelectedEventHandler(event, ranges);

        });

        // enable unselection on the overview chart
        $('div.chart.chart-overview').bind("plotunselected", function (event) {

            // then handle it
            plotUnselectedEventHandler(event);

        });

    }

    charting.getDataPoints = function getDataPoints(showProgressBars) {

        var jqxhr;

        jqxhr = $.ajax({
            type: "GET",
            url: window.bigfont.constants.BASE_URL + "/WcfRestService.svc/SendFlotDataSet",
            dataType: "json"
        });

        // sometimes we do NOT want to show the fetching data progress bar
        // for instance, on data update we do not need to do this
        // because fetching happens in the background
        if (showProgressBars !== undefined && showProgressBars === true) {

            window.bigfont.progress.startProgress('fetching-data', true);

        }

        jqxhr.done(function (obj) {

            // store the data locally
            // so that we can repot without having to refetch
            window.bigfont.charting.chartDataCollection_highRes = obj[0];
            window.bigfont.charting.chartDataCollection_lowRes = obj[1];


            if ($('div.chart').length === 0) {

                // if there are no charts on the page
                // then we are plotting for the first time
                plot();
                addEventHandlers();

            } else {

                // otherwise we are replotting
                replot();

            }

            window.bigfont.progress.stopProgress();

        });

        // refetch the data and replot it every (t) milliseconds
        // so that the user has fresh data
        // note: calling getDataPoints without parameters means that the showProgressBars function parameter is undefined
        window.setTimeout(getDataPoints, UPDATE_INTERVAL_IN_MILLISECONDS_FOR_FETCHING_FRESH_DATA);

    };

} ());