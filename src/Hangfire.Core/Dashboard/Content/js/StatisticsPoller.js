(function (hangFire) {
    hangFire.StatisticsPoller = (function () {
        function StatisticsPoller(metricsCallback, statisticsUrl, pollInterval) {
            this._metricsCallback = metricsCallback;
            this._listeners = [];
            this._statisticsUrl = statisticsUrl;
            this._pollInterval = pollInterval;
            this._intervalId = null;
        }

        StatisticsPoller.prototype.start = function () {
            var self = this;

            var intervalFunc = function () {
                try {
                    $.post(self._statisticsUrl, { metrics: self._metricsCallback() }, function (data) {
                        self._notifyListeners(data);
                    });
                } catch (e) {
                    console.log(e);
                }
            };

            this._intervalId = setInterval(intervalFunc, this._pollInterval);
        };

        StatisticsPoller.prototype.stop = function () {
            if (this._intervalId !== null) {
                clearInterval(this._intervalId);
                this._intervalId = null;
            }
        };

        StatisticsPoller.prototype.addListener = function (listener) {
            this._listeners.push(listener);
        };

        StatisticsPoller.prototype._notifyListeners = function (statistics) {
            var length = this._listeners.length;
            var i;

            for (i = 0; i < length; i++) {
                this._listeners[i](statistics);
            }
        };

        return StatisticsPoller;
    })();
})(window.Hangfire = window.Hangfire || {});