(function (hangfire) {
    hangfire.config = {
        pollInterval: $("#hangfireConfig").data("pollinterval"),
        pollUrl: $("#hangfireConfig").data("pollurl"),
        locale: document.documentElement.lang
    };

    hangfire.ErrorAlert = (function () {
        function ErrorAlert(title, message) {
            this._errorAlert = $('#errorAlert');
            this._errorAlertTitle = $('#errorAlertTitle');
            this._errorAlertMessage = $('#errorAlertMessage');
            this._title = title;
            this._message = message;
        }

        ErrorAlert.prototype.show = function() {
            this._errorAlertTitle.html(this._title);
            this._errorAlertMessage.html(this._message);
            $('#errorAlert').slideDown('fast');
        };

        return ErrorAlert;
    })();

    hangfire.Metrics = (function() {
        function Metrics() {
            this._metrics = {};
        }

        Metrics.prototype.addElement = function(name, element) {
            if (!(name in this._metrics)) {
                this._metrics[name] = [];
            }

            this._metrics[name].push(element);
        };

        Metrics.prototype.getElements = function(name) {
            if (!(name in this._metrics)) {
                return [];
            }

            return this._metrics[name];
        };

        Metrics.prototype.getNames = function() {
            var result = [];
            var metrics = this._metrics;

            for (var name in metrics) {
                if (metrics.hasOwnProperty(name)) {
                    result.push(name);
                }
            }

            return result;
        };

        return Metrics;
    })();

    hangfire.RealtimeGraph = (function() {
        function RealtimeGraph(element, succeeded, failed, succeededStr, failedStr, pollInterval) {
            this._succeeded = succeeded;
            this._failed = failed;
            this._last = Date.now();
            this._pollInterval = pollInterval;
            
            this._chart = new Chart(element, {
                type: 'line',
                data: {
                    datasets: [
                        { label: succeededStr, borderColor: '#62B35F', backgroundColor: '#6FCD6D' },
                        { label: failedStr, borderColor: '#BB4847', backgroundColor: '#D55251' }
                    ]
                },
                options: {
                    scales: {
                        xAxes: [{
                            type: 'realtime',
                            realtime: { duration: 60 * 1000, delay: pollInterval },
                            time: { unit: 'second', tooltipFormat: 'LL LTS', displayFormats: { second: 'LTS', minute: 'LTS' } },
                            ticks: { maxRotation: 0 }
                        }],
                        yAxes: [{ ticks: { beginAtZero: true, precision: 0, min: 0, maxTicksLimit: 6, suggestedMax: 10 }, stacked: true }]
                    },
                    elements: { line: { tension: 0 }, point: { radius: 0 } },
                    animation: { duration: 0 },
                    hover: { animationDuration: 0 },
                    responsiveAnimationDuration: 0,
                    legend: { display: false },
                    tooltips: { mode: 'index', intersect: false }
                }
            });
        }

        RealtimeGraph.prototype.appendHistory = function (statistics) {
            var newSucceeded = parseInt(statistics["succeeded:count"].intValue);
            var newFailed = parseInt(statistics["failed:count"].intValue);
            var now = Date.now();

            if (this._succeeded !== null && this._failed !== null && (now - this._last < this._pollInterval * 2)) {
                var succeeded = Math.max(newSucceeded - this._succeeded, 0);
                var failed = Math.max(newFailed - this._failed, 0);

                this._chart.data.datasets[0].data.push({ x: new Date(), y: succeeded });
                this._chart.data.datasets[1].data.push({ x: new Date(), y: failed });   
                
                this._chart.update();
            }
            
            this._succeeded = newSucceeded;
            this._failed = newFailed;
            this._last = now;
        };

        return RealtimeGraph;
    })();

    hangfire.HistoryGraph = (function() {
        function HistoryGraph(element, succeeded, failed, succeededStr, failedStr) {
            var timeOptions = $(element).data('period') === 'week'
                ? { unit: 'day', tooltipFormat: 'LL', displayFormats: { day: 'll' } }
                : { unit: 'hour', tooltipFormat: 'LLL', displayFormats: { hour: 'LT', day: 'll' } };

            this._chart = new Chart(element, {
                type: 'line',
                data: {
                    datasets: [
                        { label: succeededStr, borderColor: '#62B35F', backgroundColor: '#6FCD6D', data: succeeded },
                        { label: failedStr, borderColor: '#BB4847', backgroundColor: '#D55251', data: failed }
                    ]
                },
                options: {
                    scales: {
                        xAxes: [{ type: 'time', time: timeOptions, ticks: { maxRotation: 0 } }],
                        yAxes: [{ ticks: { beginAtZero: true, precision: 0, maxTicksLimit: 6 }, stacked: true }]
                    },
                    elements: { line: { tension: 0 }, point: { radius: 0 } },
                    legend: { display: false },
                    tooltips: { mode: 'index', intersect: false }
                }
            });
        }

        return HistoryGraph;
    })();

    hangfire.StatisticsPoller = (function() {
        function StatisticsPoller(metricsCallback, statisticsUrl, pollInterval) {
            this._metricsCallback = metricsCallback;
            this._listeners = [];
            this._statisticsUrl = statisticsUrl;
            this._pollInterval = pollInterval;
            this._timeoutId = null;
        }

        StatisticsPoller.prototype.start = function () {
            var self = this;

            var intervalFunc = function() {
                try {
                    $.post(self._statisticsUrl, { metrics: self._metricsCallback() })
                        .done(function (data) {
                            self._notifyListeners(data);
                            if (self._timeoutId !== null) {
                                self._timeoutId = setTimeout(intervalFunc, self._pollInterval);
                            }
                        })
                        .fail(function (xhr) {
                            var errorAlert = new Hangfire.ErrorAlert(
                                'Unable to refresh the statistics:',
                                'the server responded with ' + xhr.status + ' (' + xhr.statusText
                                + '). Try reloading the page manually, or wait for automatic reload that will happen in a minute.');

                            errorAlert.show();
                            self._timeoutId = null;
                            setTimeout(function() { window.location.reload(); }, 60*1000);
                        });
                } catch (e) {
                    console.log(e);
                }
            };

            this._timeoutId = setTimeout(intervalFunc, this._pollInterval);
        };

        StatisticsPoller.prototype.stop = function() {
            if (this._timeoutId !== null) {
                clearTimeout(this._timeoutId);
                this._timeoutId = null;
            }
        };

        StatisticsPoller.prototype.addListener = function(listener) {
            this._listeners.push(listener);
        };

        StatisticsPoller.prototype._notifyListeners = function(statistics) {
            var length = this._listeners.length;
            var i;
            
            for (i = 0; i < length; i++) {
                this._listeners[i](statistics);
            }
        };

        return StatisticsPoller;
    })();

    hangfire.Page = (function() {
        function Page(config) {
            this._metrics = new Hangfire.Metrics();

            var self = this;
            this._poller = new Hangfire.StatisticsPoller(
                function () { return self._metrics.getNames(); },
                config.pollUrl,
                config.pollInterval);

            this._initialize(config.locale);

            this.realtimeGraph = this._createRealtimeGraph('realtimeGraph', config.pollInterval);
            this.historyGraph = this._createHistoryGraph('historyGraph');

            this._poller.start();
        };

        Page.prototype._createRealtimeGraph = function(elementId, pollInterval) {
            var realtimeElement = document.getElementById(elementId);
            if (realtimeElement) {
                var succeeded = parseInt($(realtimeElement).data('succeeded'));
                var failed = parseInt($(realtimeElement).data('failed'));

                var succeededStr = $(realtimeElement).data('succeeded-string');
                var failedStr = $(realtimeElement).data('failed-string');
                var realtimeGraph = new Hangfire.RealtimeGraph(realtimeElement, succeeded, failed, succeededStr, failedStr, pollInterval);

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
                            var point = { x: Date.parse(date), y: value };
                            series.unshift(point);
                        }
                    }
                    return series;
                };

                var succeeded = createSeries($(historyElement).data("succeeded"));
                var failed = createSeries($(historyElement).data("failed"));

                var succeededStr = $(historyElement).data('succeeded-string');
                var failedStr = $(historyElement).data('failed-string');

                return new Hangfire.HistoryGraph(historyElement, succeeded, failed, succeededStr, failedStr);
            }

            return null;
        };

        Page.prototype._initialize = function (locale) {
            moment.locale(locale);
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

                $('*[data-moment-local]').each(function () {
                    var $this = $(this);
                    var timestamp = $this.data('moment-local');

                    if (timestamp) {
                        var time = moment(timestamp, 'X');
                        $this.html(time.format('l LTS'));
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

            var csrfHeader = $('meta[name="csrf-header"]').attr('content');
            var csrfToken = $('meta[name="csrf-token"]').attr('content');

            if (csrfToken && csrfHeader) {
                var headers = {};
                headers[csrfHeader] = csrfToken;

                $.ajaxSetup({ headers: headers });
            }

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
                    $expander.text('Fewer details...');
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

                updateListState();
            });
        };

        return Page;
    })();
})(window.Hangfire = window.Hangfire || {});

$(function () {
    Hangfire.page = new Hangfire.Page(Hangfire.config);
});
