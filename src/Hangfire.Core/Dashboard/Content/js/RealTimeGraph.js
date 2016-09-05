(function (hangFire) {
    hangFire.RealtimeGraph = (function () {
        function RealtimeGraph(element, succeeded, failed, succeededStr, failedStr) {
            this._succeeded = succeeded;
            this._failed = failed;

            this._graph = new Rickshaw.Graph({
                element: element,
                width: $(element).innerWidth(),
                height: 200,
                renderer: 'bar',
                interpolation: 'linear',
                stroke: true,

                series: new Rickshaw.Series.FixedDuration([
                        { name: failedStr, color: '#d9534f' },
                        { name: succeededStr, color: '#5cb85c' }
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
                yFormatter: function (y) { return Math.floor(y); },
                xFormatter: function (x) { return moment(new Date(x * 1000)).format("LLLL"); }
            });

            this._graph.render();
        }

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

        RealtimeGraph.prototype.update = function () {
            this._graph.update();
        };

        return RealtimeGraph;
    })();
}(window.Hangfire = window.Hangfire || {}));