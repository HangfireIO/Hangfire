$(function () {
    (function() {
        var graph = new Rickshaw.Graph({
            element: document.getElementById("realtime"),
            width: 800,
            height: 200,
            renderer: 'line',
            interpolation: 'linear',

            series: new Rickshaw.Series.FixedDuration([
                    { name: 'failed', color: '#B1003E' },
                    { name: 'succeeded', color: '#006f68' }
                ],
                undefined,
                { timeInterval: 2000, maxDataPoints: 100 }
            )
        });

        var yAxis = new Rickshaw.Graph.Axis.Y({
            graph: graph,
            tickFormat: Rickshaw.Fixtures.Number.formatKMBT
        });

        var hoverDetail = new Rickshaw.Graph.HoverDetail({
            graph: graph,
            yFormatter: function(y) { return Math.floor(y); }
        });

        graph.render();

        var timeInterval = 2000;
        var i = 0;
        var hangFire = {};

        setInterval(function () {
            try {
                $.getJSON($('#stats').data('refresh-url'), null, function (data) {
                    var succeeded, failed;

                    for (var property in data) {
                        if (data.hasOwnProperty(property)) {
                            $('#stats-' + property).text(data[property]);
                        }
                    }

                    if (i !== 0) {
                        succeeded = data.succeeded - hangFire.succeeded;
                        failed = data.failed - hangFire.failed;

                        graph.series.addData({ failed: failed, succeeded: succeeded });
                        graph.render();
                    }

                    hangFire.succeeded = data.succeeded;
                    hangFire.failed = data.failed;

                    i++;
                });
            } catch(e) {
                console.log(e);
            } 
            
        }, timeInterval);
    })();

    (function () {
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

        var succeeded = createSeries($("#history").data("succeeded"));
        var failed = createSeries($("#history").data("failed"));

        var graph = new Rickshaw.Graph({
            element: document.getElementById('history'),
            width: 800,
            height: 200,
            renderer: 'line',
            interpolation: 'linear',
            series: [
                {
                    color: '#B1003E',
                    data: failed,
                    name: 'Failed'
                }, {
                    color: '#006f68',
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

        graph.render();

        var hoverDetail = new Rickshaw.Graph.HoverDetail({
            graph: graph,
            yFormatter: function(y) { return Math.floor(y); }
        });
    })();
});