(function (hangfire) {
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

    var BaseGraph = function () {
        this.height = 200;
    };

    BaseGraph.prototype.update = function () {
        var graph = this._graph;

        var width = $(graph.element).innerWidth();
        if (width !== graph.width) {
            graph.configure({
                width: width,
                height: this.height
            });
        }

        graph.update();
    };

    BaseGraph.prototype._initGraph = function (element, settings, xSettings, ySettings) {
        var graph = this._graph = new Rickshaw.Graph($.extend({
            element: element,
            width: $(element).innerWidth(),
            height: this.height,
            interpolation: 'linear',
            stroke: true
        }, settings));

        this._hoverDetail = new Rickshaw.Graph.HoverDetail({
            graph: graph,
            yFormatter: function (y) { return Math.floor(y); },
            xFormatter: function (x) { return moment(new Date(x * 1000)).format("LLLL"); }
        });

        if (xSettings) {
            this._xAxis = new Rickshaw.Graph.Axis.Time($.extend({ graph: graph }, xSettings));
        }

        if (ySettings) {
            this._yAxis = new Rickshaw.Graph.Axis.Y($.extend({
                graph: graph,
                tickFormat: Rickshaw.Fixtures.Number.formatKMBT
            }, ySettings));
        }

        graph.render();
    }

    hangfire.RealtimeGraph = (function() {
        function RealtimeGraph(element, succeeded, failed, succeededStr, failedStr) {
            this._succeeded = succeeded;
            this._failed = failed;

            this._initGraph(element, {
                renderer: 'bar',
                series: new Rickshaw.Series.FixedDuration([
                        { name: failedStr, color: '#d9534f' },
                        { name: succeededStr, color: '#5cb85c' }
                ],
                    undefined,
                    { timeInterval: 2000, maxDataPoints: 100 }
                )
            }, null, {});
        }

        RealtimeGraph.prototype = Object.create(BaseGraph.prototype);

        RealtimeGraph.prototype.appendHistory = function (statistics) {
            var newSucceeded = parseInt(statistics["succeeded:count"].intValue);
            var newFailed = parseInt(statistics["failed:count"].intValue);

            if (this._succeeded !== null && this._failed !== null) {
                var succeeded = newSucceeded - this._succeeded;
                var failed = newFailed - this._failed;

                this._graph.series.addData({ failed: failed, succeeded: succeeded });
                this._graph.render();
            }
            
            this._succeeded = newSucceeded;
            this._failed = newFailed;
        };

        return RealtimeGraph;
    })();

    hangfire.HistoryGraph = (function() {
        function HistoryGraph(element, succeeded, failed, succeededStr, failedStr) {
            this._initGraph(element, {
                renderer: 'area',
                series: [
                    {
                        color: '#d9534f',
                        data: failed,
                        name: failedStr
                    }, {
                        color: '#6ACD65',
                        data: succeeded,
                        name: succeededStr
                    }
                ]
            }, {}, { ticksTreatment: 'glow' });
        }

        HistoryGraph.prototype = Object.create(BaseGraph.prototype);

        return HistoryGraph;
    })();

    hangfire.StatisticsPoller = (function() {
        function StatisticsPoller(metricsCallback, statisticsUrl, pollInterval) {
            this._metricsCallback = metricsCallback;
            this._listeners = [];
            this._statisticsUrl = statisticsUrl;
            this._pollInterval = pollInterval;
            this._intervalId = null;
        }

        StatisticsPoller.prototype.start = function () {
            var self = this;

            var intervalFunc = function() {
                try {
                    $.post(self._statisticsUrl, { metrics: self._metricsCallback() }, function(data) {
                        self._notifyListeners(data);
                    });
                } catch (e) {
                    console.log(e);
                }
            };

            this._intervalId = setInterval(intervalFunc, this._pollInterval);
        };

        StatisticsPoller.prototype.stop = function() {
            if (this._intervalId !== null) {
                clearInterval(this._intervalId);
                this._intervalId = null;
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
            this._createGraphs();
            this._poller.start();
        }

        Page.prototype._createGraphs = function() {
            var realtime = this.realtimeGraph = this._createRealtimeGraph('realtimeGraph');
            var history = this.historyGraph = this._createHistoryGraph('historyGraph');

            var debounce = function (fn, timeout) {
                var timeoutId = -1;
                return function() {
                    if (timeoutId > -1) {
                        window.clearTimeout(timeoutId);
                    }
                    timeoutId = window.setTimeout(fn, timeout);
                };
            };

            window.onresize = debounce(function () {
                realtime.update();
                history.update();
            }, 125);
        };

        Page.prototype._createRealtimeGraph = function(elementId) {
            var realtimeElement = document.getElementById(elementId);
            if (realtimeElement) {
                var succeeded = parseInt($(realtimeElement).data('succeeded'));
                var failed = parseInt($(realtimeElement).data('failed'));

                var succeededStr = $(realtimeElement).data('succeeded-string');
                var failedStr = $(realtimeElement).data('failed-string');
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

                updateListState();
            });
        };

        return Page;
    })();
})(window.Hangfire = window.Hangfire || {});

$(function () {
    Hangfire.page = new Hangfire.Page(Hangfire.config);
});
