﻿using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsLog
    {
        void Start();
        void Stop();
        int Log(IList<KeyImpression> impressions);
    }
}
