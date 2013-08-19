$(function () {
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
        yFormatter: function (y) { return Math.floor(y); }
    });

    graph.render();

    var timeInterval = 2000;
    var i = 0;
    var hangFire = {};
    setInterval(function () {
        $.getJSON($('#stats').data('refresh-url'), null, function (data) {
            var succeeded, failed;
            console.log('asdasd');
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
    }, timeInterval);
});