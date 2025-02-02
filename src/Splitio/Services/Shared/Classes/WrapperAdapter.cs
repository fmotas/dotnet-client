﻿using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class WrapperAdapter : IWrapperAdapter
    {
        private static readonly ISplitLogger _log = GetLogger(typeof(IWrapperAdapter));

        public ReadConfigData ReadConfig(ConfigurationOptions config, ISplitLogger log)
        {
            var data = new ReadConfigData();
            var ipAddressesEnabled = config.IPAddressesEnabled ?? true;

#if NETSTANDARD2_0 || NET6_0 || NET5_0
            data.SdkVersion = ".NET_CORE-" + SplitSdkVersion();
#else
            data.SdkVersion = ".NET-" + SplitSdkVersion();
#endif
            data.SdkMachineName = GetSdkMachineName(config, ipAddressesEnabled, log);
            data.SdkMachineIP = GetSdkMachineIP(config, ipAddressesEnabled, log);

            return data;
        }

        public void TaskWaitAndDispose(params Task[] tasks)
        {
            try
            {
                foreach (var t in tasks)
                {
                    if (t == null || t.Status == TaskStatus.Canceled) continue;

                    t.Wait();
                    t.Dispose();
                }
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
            }
        }

        public Task TaskDelay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            return Task.Delay(millisecondsDelay, cancellationToken);
        }

        public Task TaskDelay(int millisecondsDelay)
        {
            return Task.Delay(millisecondsDelay);
        }

        public Task<Task> WhenAny(params Task[] tasks)
        {
            return Task.WhenAny(tasks);
        }

        public async Task<T> TaskFromResult<T>(T result)
        {
            return await Task.FromResult(result);
        }

        private string SplitSdkVersion()
        {
#if NETSTANDARD2_0 || NET6_0 || NET5_0
            return typeof(Split).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
#else
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
#endif
        }

        public static ISplitLogger GetLogger(Type type)
        {
#if NETSTANDARD2_0 || NET6_0 || NET5_0
            return new MicrosoftExtensionsLogging(type);
#else
            return new CommonLogging(type);
#endif
        }

        public static ISplitLogger GetLogger(string type)
        {
#if NETSTANDARD2_0 || NET6_0 || NET5_0
            return new MicrosoftExtensionsLogging(type);
#else
            return new CommonLogging(type);
#endif
        }

        #region Private Methods
        private string GetSdkMachineName(ConfigurationOptions config, bool ipAddressesEnabled, ISplitLogger log)
        {
            if (ipAddressesEnabled)
            {
                try
                {
                    return config.SdkMachineName ?? Environment.MachineName;
                }
                catch (Exception e)
                {
                    log.Warn("Exception retrieving machine name.", e);
                    return Constants.Gral.Unknown;
                }
            }
            else if(config.CacheAdapterConfig?.Type == AdapterType.Redis)
            {
                return Constants.Gral.NA;
            }

            return string.Empty;
        }

        private string GetSdkMachineIP(ConfigurationOptions config, bool ipAddressesEnabled, ISplitLogger log)
        {
            if (ipAddressesEnabled)
            {
                try
                {
#if NETSTANDARD2_0 || NET6_0 || NET5_0
                    var hostAddressesTask = Dns.GetHostAddressesAsync(Environment.MachineName);
                    hostAddressesTask.Wait();
                    return config.SdkMachineIP ?? hostAddressesTask.Result.Where(x => x.AddressFamily == AddressFamily.InterNetwork && x.IsIPv6LinkLocal == false).Last().ToString();
#else
                    return config.SdkMachineIP ?? Dns.GetHostAddresses(Environment.MachineName).Where(x => x.AddressFamily == AddressFamily.InterNetwork && x.IsIPv6LinkLocal == false).Last().ToString();
#endif
                }
                catch (Exception e)
                {
                    log.Warn("Exception retrieving machine IP.", e);
                    return Constants.Gral.Unknown;
                }
            }
            else if (config.CacheAdapterConfig?.Type == AdapterType.Redis)
            {
                return Constants.Gral.NA;
            }

            return string.Empty;
        }
        #endregion
    }
}
