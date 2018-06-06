﻿using VinteR.Adapter.OptiTrack;
using VinteR.Tests.Adapter.OptiTrack;
using VinteR.Tracking;

namespace VinteR.Tests
{
    public class VinterNinjectTestModule : VinterNinjectModule
    {
        public override void Load()
        {
            base.Load();
            Rebind<IOptiTrackClient>().To<OptiTrackMockClient>();
            Rebind<IAdapterTracker>().To<OptiTrackMockAdapterTracker>();
        }
    }
}