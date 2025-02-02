﻿using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IWrapperAdapter
    {
        ReadConfigData ReadConfig(ConfigurationOptions config, ISplitLogger log);
        Task TaskDelay(int millisecondsDelay);
        Task TaskDelay(int millisecondsDelay, CancellationToken cancellationToken);
        Task<Task> WhenAny(params Task[] tasks);
        Task<T> TaskFromResult<T>(T result);
        void TaskWaitAndDispose(params Task[] tasks);
    }
}
