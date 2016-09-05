(function (hangFire) {
    hangFire.HistoryGraph = (function () {
        function HistoryGraph(element, succeeded, failed, succeededStr, failedStr) {
            this._graph = new Rickshaw.Graph({
                element: element,
                width: $(element).innerWidth(),
                height: 200,
                renderer: 'area',
                interpolation: 'linear',
                stroke: true,
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
            });

            var xAxis = new Rickshaw.Graph.Axis.Time({ graph: this._graph });
            var yAxis = new Rickshaw.Graph.Axis.Y({
                graph: this._graph,
                tickFormat: Rickshaw.Fixtures.Number.formatKMBT,
                tickTreatment: 'glow'
            });

            var hoverDetail = new Rickshaw.Graph.HoverDetail({
                graph: this._graph,
                yFormatter: function (y) { return Math.floor(y); },
                xFormatter: function (x) { return moment(new Date(x * 1000)).format("LLLL"); }
            });

            this._graph.render();
        }

        HistoryGraph.prototype.update = function () {
            this._graph.update();
        };

        return HistoryGraph;
    })();

}(window.hangfire = window.hangfire || {}));