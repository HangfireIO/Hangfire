(function(hangFire) {
    hangFire.RealtimeGraph = (function() {
        function RealtimeGraph(element) {
            this._succeeded = null;
            this._failed = null;
            
            this._graph = new Rickshaw.Graph({
                element: element,
                width: 800,
                height: 200,
                renderer: 'bar',
                interpolation: 'linear',

                series: new Rickshaw.Series.FixedDuration([
                        { name: 'failed', color: '#d9534f' },
                        { name: 'succeeded', color: '#5cb85c' }
                ],
                    undefined,
                    { timeInterval: 2000, maxDataPoints: 100 }
                )
            });

            var xAxis = new Rickshaw.Graph.Axis.Time({ graph: this._graph });
            var yAxis = new Rickshaw.Graph.Axis.Y({
                graph: this._graph,
                tickFormat: Rickshaw.Fixtures.Number.formatKMBT
            });

            var hoverDetail = new Rickshaw.Graph.HoverDetail({
                graph: this._graph,
                yFormatter: function (y) { return Math.floor(y); }
            });

            this._graph.render();
        }

        RealtimeGraph.prototype.appendHistory = function (statistics) {
            if (this._succeeded !== null && this._failed !== null) {
                var succeeded = statistics.succeeded - this._succeeded;
                var failed = statistics.failed - this._failed;

                this._graph.series.addData({ failed: failed, succeeded: succeeded });
                this._graph.render();
            }
            
            this._succeeded = statistics.succeeded;
            this._failed = statistics.failed;
        };

        return RealtimeGraph;
    })();

    hangFire.HistoryGraph = (function() {
        function HistoryGraph(element, succeeded, failed) {
            var graph = new Rickshaw.Graph({
                element: element,
                width: 800,
                height: 200,
                series: [
                    {
                        color: '#d9534f',
                        data: failed,
                        name: 'Failed'
                    }, {
                        color: '#5cb85c',
                        data: succeeded,
                        name: 'Succeeded'
                    }
                ]
            });

            var xAxis = new Rickshaw.Graph.Axis.Time({ graph: graph });
            var yAxis = new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: Rickshaw.Fixtures.Number.formatKMBT,
                tickTreatment: 'glow'
            });
            
            var hoverDetail = new Rickshaw.Graph.HoverDetail({
                graph: graph,
                yFormatter: function(y) { return Math.floor(y); }
            });

            graph.render();
        }

        return HistoryGraph;
    })();

    hangFire.StatisticsPoller = (function() {
        function StatisticsPoller(statisticsUrl, pollInterval) {
            this._listeners = [];
            this._statisticsUrl = statisticsUrl;
            this._pollInterval = pollInterval;
            this._intervalId = null;
        }

        StatisticsPoller.prototype.start = function () {
            var self = this;
            
            this._intervalId = setInterval(function () {
                try {
                    $.getJSON(self._statisticsUrl, null, function (data) {
                        self._notifyListeners(data);
                    });
                } catch (e) {
                    console.log(e);
                }

            }, this._pollInterval);
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

    hangFire.Page = (function() {
        function Page(config) {
            this._poller = new HangFire.StatisticsPoller(
                config.pollUrl, config.pollInterval);

            this.realtimeGraph = this._createRealtimeGraph('realtimeGraph');
            this.historyGraph = this._createHistoryGraph('historyGraph');

            this._registerStatisticsUpdater();

            this._poller.start();

            this._initialize();
        }

        Page.prototype._createRealtimeGraph = function(elementId) {
            var realtimeElement = document.getElementById(elementId);
            if (realtimeElement) {
                var realtimeGraph = new HangFire.RealtimeGraph(realtimeElement);

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

                return new HangFire.HistoryGraph(historyElement, succeeded, failed);
            }

            return null;
        };

        Page.prototype._registerStatisticsUpdater = function() {
            this._poller.addListener(function (data) {
                for (var property in data) {
                    if (data.hasOwnProperty(property)) {
                        $('#stats-' + property).text(data[property]);
                    }
                }
            });
        };

        Page.prototype._initialize = function() {
            var updateRelativeDates = function () {
                $('*[data-moment]').each(function () {
                    var $this = $(this);
                    var time = moment($this.data('moment'), 'X');
                    $this.html(time.fromNow())
                        .attr('title', time.format('llll'))
                        .attr('data-container', 'body');
                });
            };

            updateRelativeDates();
            setInterval(updateRelativeDates, 30 * 1000);

            $('*[title]').tooltip();

            $(document).on('click', '*[data-ajax]', function (e) {
                var $this = $(this);

                var loadingDelay = setTimeout(function () {
                    $this.button('loading');
                }, 100);

                $.post($this.data('ajax'), function () {
                    clearTimeout(loadingDelay);
                    $this.button('reset');
                    window.location.reload();
                });

                e.preventDefault();
            });

            $(document).on('click', '.expander', function () {
                $(this).closest('tr').next().find('.expandable').slideToggle(150);
            });
        };

        return Page;
    })();
})(window.HangFire = window.HangFire || {});

$(function () {
    HangFire.page = new HangFire.Page(HangFire.config);
});