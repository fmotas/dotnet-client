﻿using Newtonsoft.Json;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorage : ITelemetryInitProducer, ITelemetryEvaluationProducer
    {
        private readonly IRedisAdapter _redisAdapter;
        private readonly ISplitLogger _log;
        private readonly string _userPrefix;
        private readonly string _sdkVersion;
        private readonly string _machineIp;
        private readonly string _machineName;

        private string TelemetryLatencyKey => "{prefix}.SPLITIO.telemetry.latencies"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        private string TelemetryExceptionKey => "{prefix}.SPLITIO.telemetry.exceptions"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        private string TelemetryInitKey => "{prefix}.SPLITIO.telemetry.init"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        public RedisTelemetryStorage(IRedisAdapter redisAdapter,
            string userPrefix,
            string sdkVersion,
            string machineIp,
            string machineName,
            ISplitLogger log = null)
        {
            _redisAdapter = redisAdapter;
            _userPrefix = userPrefix;
            _sdkVersion = sdkVersion;
            _machineIp = machineIp;
            _machineName = machineName;
            _log = log ?? WrapperAdapter.GetLogger(typeof(RedisTelemetryStorage));
        }

        public void RecordConfigInit(Config config)
        {
            var jsonData = JsonConvert.SerializeObject(new
            {
                t = new { oM = config.OperationMode, st = config.Storage, aF = config.ActiveFactories, rF = config.RedundantActiveFactories, t = config.Tags },
                m = new { i = _machineIp, n = _machineName, s = _sdkVersion }
            });

            _redisAdapter.HashSet(TelemetryInitKey, $"{_sdkVersion}/{_machineName}/{_machineIp}", jsonData);
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _redisAdapter.HashIncrement(TelemetryLatencyKey, $"{_sdkVersion}/{_machineName}/{_machineIp}/{method.GetString()}/{bucket}", 1);
        }

        public void RecordException(MethodEnum method)
        {
            _redisAdapter.HashIncrement(TelemetryExceptionKey, $"{_sdkVersion}/{_machineName}/{_machineIp}/{method.GetString()}", 1);
        }

        public void RecordNonReadyUsages()
        {
            // No-Op.
        }

        public void RecordBURTimeout()
        {
            // No-Op.
        }
    }
}
