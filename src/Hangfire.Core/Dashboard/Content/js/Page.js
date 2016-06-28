(function (hangFire) {
    hangFire.Page = (function () {
        var url;
        function Page(config) {
            this._metrics = new Hangfire.Metrics();

            var self = this;
            this._poller = new Hangfire.StatisticsPoller(
                function () { return self._metrics.getNames(); },
                config.pollUrl,
                config.pollInterval);

            this._initialize();
            this._createGraphs();
            this._poller.start();
        }

        Page.prototype._createGraphs = function() {
            this.realtimeGraph = this._createRealtimeGraph('realtimeGraph');
            this.historyGraph = this._createHistoryGraph('historyGraph');
            
            var debounce = function (fn, timeout) {
                var timeoutId = -1;
                return function() {
                    if (timeoutId > -1) {
                        window.clearTimeout(timeoutId);
                    }
                    timeoutId = window.setTimeout(fn, timeout);
                };
            };

            var self = this;
            window.onresize = debounce(function () {
                $('#realtimeGraph').html('');
                $('#historyGraph').html('');

                self._createGraphs();
            }, 125);
        };

        Page.prototype._createRealtimeGraph = function(elementId) {
            var realtimeElement = document.getElementById(elementId);
            var succeeded = parseInt($(realtimeElement).data('succeeded'));
            var failed = parseInt($(realtimeElement).data('failed'));

            var succeededStr = $(realtimeElement).data('succeeded-string');
            var failedStr = $(realtimeElement).data('failed-string');

            if (realtimeElement) {
                var realtimeGraph = new Hangfire.RealtimeGraph(realtimeElement, succeeded, failed, succeededStr, failedStr);

                this._poller.addListener(function (data) {
                    realtimeGraph.appendHistory(data);
                });

                return realtimeGraph;
            }

            return null;
        };

        Page.prototype._createHistoryGraph = function(elementId) {
            var historyElement = document.getElementById(elementId);
            if (historyElement) {
                var createSeries = function (obj) {
                    var series = [];
                    for (var date in obj) {
                        if (obj.hasOwnProperty(date)) {
                            var value = obj[date];
                            var point = { x: Date.parse(date) / 1000, y: value };
                            series.unshift(point);
                        }
                    }
                    return series;
                };

                var succeeded = createSeries($(historyElement).data("succeeded"));
                var failed = createSeries($(historyElement).data("failed"));

                var succeededStr = $(historyElement).data('succeeded-string');
                var failedStr = $(historyElement).data('failed-string');

                return new hangfire.HistoryGraph(historyElement, succeeded, failed, succeededStr, failedStr);
            }

            return null;
        };

        Page.prototype._initialize = function() {
            var updateRelativeDates = function () {
                $('*[data-moment]').each(function () {
                    var $this = $(this);
                    var timestamp = $this.data('moment');

                    if (timestamp) {
                        var time = moment(timestamp, 'X');
                        $this.html(time.fromNow())
                            .attr('title', time.format('llll'))
                            .attr('data-container', 'body');
                    }
                });

                $('*[data-moment-title]').each(function () {
                    var $this = $(this);
                    var timestamp = $this.data('moment-title');

                    if (timestamp) {
                        var time = moment(timestamp, 'X');
                        $this.prop('title', time.format('llll'))
                            .attr('data-container', 'body');
                    }
                });
            };

            updateRelativeDates();
            setInterval(updateRelativeDates, 30 * 1000);

            $('*[title]').tooltip();

            var self = this;
            $('*[data-metric]').each(function () {
                var name = $(this).data('metric');
                self._metrics.addElement(name, this);
            });

            this._poller.addListener(function (metrics) {
                for (var name in metrics) {
                    var elements = self._metrics.getElements(name);
                    for (var i = 0; i < elements.length; i++) {
                        var metric = metrics[name];
                        var metricClass = metric ? "metric-" + metric.style : "metric-null";
                        var highlighted = metric && metric.highlighted ? "highlighted" : null;
                        var value = metric ? metric.value : null;

                        $(elements[i])
                            .text(value)
                            .closest('.metric')
                            .removeClass()
                            .addClass(["metric", metricClass, highlighted].join(' '));
                    }
                }
            });

            $(document).on('click', '*[data-ajax]', function (e) {
                var $this = $(this);
                var confirmText = $this.data('confirm');

                if (!confirmText || confirm(confirmText)) {
                    $this.prop('disabled');
                    var loadingDelay = setTimeout(function() {
                        $this.button('loading');
                    }, 100);

                    $.post($this.data('ajax'), function() {
                        clearTimeout(loadingDelay);
                        window.location.reload();
                    });
                }

                e.preventDefault();
            });

            $(document).on('click', '.expander', function (e) {
                var $expander = $(this),
                    $expandable = $expander.closest('tr').next().find('.expandable');

                if (!$expandable.is(':visible')) {
                    $expander.text('Less details...');
                }

                $expandable.slideToggle(
					150, 
					function() {
					    if (!$expandable.is(':visible')) {
					        $expander.text('More details...');
					    }
					});
                e.preventDefault();
            });

            $('.js-jobs-list').each(function () {
                var container = this;

                var selectRow = function(row, isSelected) {
                    var $checkbox = $('.js-jobs-list-checkbox', row);
                    if ($checkbox.length > 0) {
                        $checkbox.prop('checked', isSelected);
                        $(row).toggleClass('highlight', isSelected);
                    }
                };

                var toggleRowSelection = function(row) {
                    var $checkbox = $('.js-jobs-list-checkbox', row);
                    if ($checkbox.length > 0) {
                        var isSelected = $checkbox.is(':checked');
                        selectRow(row, !isSelected);
                    }
                };

                var setListState = function (state) {
                    $('.js-jobs-list-select-all', container)
                        .prop('checked', state === 'all-selected')
                        .prop('indeterminate', state === 'some-selected');
                    
                    $('.js-jobs-list-command', container)
                        .prop('disabled', state === 'none-selected');
                };

                var updateListState = function() {
                    var selectedRows = $('.js-jobs-list-checkbox', container).map(function() {
                        return $(this).prop('checked');
                    }).get();

                    var state = 'none-selected';

                    if (selectedRows.length > 0) {
                        state = 'some-selected';

                        if ($.inArray(false, selectedRows) === -1) {
                            state = 'all-selected';
                        } else if ($.inArray(true, selectedRows) === -1) {
                            state = 'none-selected';
                        }
                    }

                    setListState(state);
                };

                $(this).on('click', '.js-jobs-list-checkbox', function(e) {
                    selectRow(
                        $(this).closest('.js-jobs-list-row').first(),
                        $(this).is(':checked'));

                    updateListState();

                    e.stopPropagation();
                });

                $(this).on('click', '.js-jobs-list-row', function (e) {
                    if ($(e.target).is('a')) return;

                    toggleRowSelection(this);
                    updateListState();
                });

                $(this).on('click', '.js-jobs-list-select-all', function() {
                    var selectRows = $(this).is(':checked');

                    $('.js-jobs-list-row', container).each(function() {
                        selectRow(this, selectRows);
                    });

                    updateListState();
                });

                $(this).on('click', '.js-jobs-list-command', function(e) {
                    var $this = $(this);
                    var confirmText = $this.data('confirm');

                    var jobs = $("input[name='jobs[]']:checked", container).map(function() {
                        return $(this).val();
                    }).get();

                    if (!confirmText || confirm(confirmText)) {
                        $this.prop('disabled');
                        var loadingDelay = setTimeout(function () {
                            $this.button('loading');
                        }, 100);

                        $.post($this.data('url'), { 'jobs[]': jobs }, function () {
                            clearTimeout(loadingDelay);
                            window.location.reload();
                        });
                    }
                    e.preventDefault();
                });


                $(this).on('click', '.js-jobs-filter-command', function (e) {
                    url = document.location.search;
                    var filterValueString = $("#filterValueString").val();
                    var filterMethodString = $("#filterMethodString").val();
                    var filterStartDateTime = $("#startDateTime").datepick().val();
                    var filterEndDateTime = $("#endDateTime").datepick().val();
                    var addedFilterString = prepFilterStringParameters(filterValueString, filterMethodString);
                    var addedDateTimeFilterStrings = prepFilterDateTimeParameters(filterStartDateTime, filterEndDateTime);
                    if (addedDateTimeFilterStrings || addedFilterString) redirectToFirstPage();
                    
                    document.location.search = url;
                })
                
                var prepFilterStringParameters = function (filterString, filterMethodString) {
                    var result = false;
                    var checked = $("#filterOnMethodName").is(':checked');
                    if (url === '' && (filterString !== '' || filterMethodString !== '')) {
                        var parameters = [];                        
                        if (filterString !== '') parameters[parameters.length] = ["filterString", filterString].join('=');
                        if (checked && filterMethodString !== '') parameters[parameters.length] = ["filterMethodString", filterMethodString].join('=');
                        if (parameters.length > 0) url = '?' + parameters.join('&');
                        result = true;
                    } else {
                        var res1 = updateOrRemoveSingleParameter("filterString", filterString);
                        var res2 = updateOrRemoveSingleParameter("filterMethodString", filterMethodString);
                        var parameters = url.substr(1).split('&');

                        if (!res1 && filterString !== '') {
                            parameters[parameters.length] = ["filterString", filterString].join('=');
                            result = true;
                        }

                        if (checked && !res2 && filterMethodString !== '') {
                            parameters[parameters.length] = ["filterMethodString", filterMethodString].join('=');  
                            result = true;
                        }
                        
                        if ( parameters.length > 0 ) url = '?' + parameters.join('&');
                    }
                    return result;
                }

                var updateOrRemoveSingleParameter = function (parameter, value) {
                    var result = false;
                    var parameters = url.substr(1).split('&');
                    var elements;
                    var i = 0;
                    var checked = true;
                    if(parameter !== "filterString") checked = $("#filterOnMethodName").is(':checked');
                    for (i = 0; i < parameters.length; i++) {
                        elements = parameters[i].split('=');
                        if (elements[0] === parameter) {
                            if (value === '' || !checked || typeof value === 'undefined' ) {
                                parameters.splice(i, 1);
                                result = false;
                            } else if (value != '') {
                                elements[1] = value;
                                parameters[i] = elements.join('=');
                                result = true;
                            }
                        }
                    }
                    if (parameters.length > 0) url = '?' + parameters.join('&');
                    else url = '';

                    return result;
                }

                var prepFilterDateTimeParameters = function (startDateTime, endDateTime) {
                    if (typeof startDateTime === 'undefined' || typeof endDateTime === 'undefined') return false;
                    var sDate = startDateTime.split(' ')[0].split('/'),
                    eDate = endDateTime.split(' ')[0].split('/'),
                    sTime = startDateTime.split(' ')[1].split(':'),
                    eTime = endDateTime.split(' ')[1].split(':');

                    
                    var startSeconds = new Date(sDate[2],sDate[1],sDate[0], sTime[0],sTime[1]);
                    var endSeconds = new Date(eDate[2], eDate[1], eDate[0], eTime[0],eTime[1]);
                    var checked = $("#filterOnDateTime").is(':checked');
                    if ((startSeconds - endSeconds) <= 0 && checked) {
                        return addOrModifyDateTimeParameters(sDate, eDate, sTime, eTime);
                    }
                    else {
                        return removeDateTimeParameters();
                    }
                }

                var addOrModifyDateTimeParameters = function (sDate, eDate, sTime, eTime) {
                    if (url === '') {
                        url = '?' + "startDate=" + sDate.join('-') + '&' + "endDate=" + eDate.join('-') + '&' + "startTime=" + sTime.join('-') + '&' + "endTime=" + eTime.join('-');
                    } else {
                        var parameters = url.substr(1).split('&');
                        var element;
                        var foundStartDate = false,
                        foundEndDate = false,
                        foundStartTime = false,
                        foundEndTime = false;
                        for (var i = 0; i < parameters.length; i++) {
                            element = parameters[i].split('=');
                            if (element[0] === "startDate") {
                                element[1] = sDate.join('-');
                                foundStartDate = true;
                            } else if (element[0] === "endDate") {
                                element[1] = eDate.join('-');
                                foundEndDate = true;
                            } else if (element[0] === "startTime") {
                                element[1] = sTime.join('-');
                                foundStartTime = true;
                            } else if (element[0] === "endTime") {
                                element[1] = eTime.join('-');
                                foundEndTime = true;
                            }
                            parameters[i] = element.join('=');
                        }
                        if (!foundStartDate) {
                            parameters[parameters.length] = ["startDate", sDate.join('-')].join('=');
                        }
                        if (!foundEndDate) {
                            parameters[parameters.length] = ["endDate", eDate.join('-')].join('=');
                        }
                        if (!foundStartTime) {
                            parameters[parameters.length] = ["startTime", sTime.join('-')].join('=');
                        }
                        if (!foundEndTime) {
                            parameters[parameters.length] = ["endTime", eTime.join('-')].join('=');
                        }
                        url = '?' + parameters.join('&');
                    }
                    return true;
                }

                var removeDateTimeParameters = function () {
                    if (url !== '') {
                        var parameters = url.substr(1).split('&');
                        var element;
                        var i = 0;
                        do {
                            element = parameters[i].split('=');
                            if (element[0] === "startDate" || element[0] === "endDate" || element[0] === "startTime" || element[0] === "endTime") {
                                parameters.splice(i, 1);
                            } else {
                                i++;
                            }
                        } while (i < parameters.length)
                        url = '?' + parameters.join('&');
                    }
                    return false;
                }

                var redirectToFirstPage = function () {
                    if (url.indexOf("from") > -1 ) {
                        var parameters = url.substr(1).split('&');
                        var element;
                        for (var i = 0; i < parameters.length; i++) {
                            element = parameters[i].split('=');
                            if (element[0] === "from") {
                                element[1] = 0;
                                parameters[i] = element.join('=');
                                break;
                            }
                        }
                        url = '?' + parameters.join('&');
                    }
                }
                
                $(this).on('click', '.js-jobs-filtertext-clear', function (e) {                    
                    $("#filterValueString").val('');
                })
                      

                $(this).on('click', '.js-jobs-filterOnDateTime-checked', function (e) {
                    var checked = $("#filterOnDateTime").is(':checked');
                    if (checked) {
                        $("#datetime-filters").show();              
                    }
                    else {
                        $("#datetime-filters").hide();           
                    }
                })
                  
                $(this).on('click', '.js-jobs-filterOnMethodName-checked', function (e) {
                    var checked = $("#filterOnMethodName").is(':checked');
                    if (checked) {
                        $("#filterMethodString").show();
                    }
                    else {
                        $("#filterMethodString").hide();
                    }
                })

                $(".datetimeselector-start").datetimepicker({
                    maxDate: '0'
                });
                $(".datetimeselector-end").datetimepicker({                    
                    maxDate: '0'
                });
                updateListState();
            });            
        };

        return Page;
    })();
})(window.Hangfire = window.Hangfire || {});

$(function () {
    Hangfire.page = new Hangfire.Page(Hangfire.config);
});
