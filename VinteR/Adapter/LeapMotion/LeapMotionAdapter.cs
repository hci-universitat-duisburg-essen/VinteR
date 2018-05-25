﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leap;
using System.Diagnostics;
using VinteR.Configuration;

namespace VinteR.Adapter.LeapMotion
{
    class LeapMotionAdapter : IInputAdapter
    {
        public event MocapFrameAvailableEventHandler FrameAvailable;

        public bool ShouldRun => _configurationService.GetConfiguration().Adapters.LeapMotion.Enabled;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private Controller controller;
        private LeapMotionEventHandler listener;
        private readonly IConfigurationService _configurationService;


        /**
         * Init Leap Motion Listener and add subscriber methods for controller events
         */
        public LeapMotionAdapter(IConfigurationService configurationService)
        {
            this._configurationService = configurationService;
        }

        /**
         * Destructor
         */
        ~LeapMotionAdapter()
        {
            // controller.RemoveListener(listener);
            controller?.Dispose();
            Logger.Info("Destructor Leap Motion Adapter finished");
        }

        public void Run(Stopwatch synchronizationWatch)
        {
            controller = new Controller();
            listener = new LeapMotionEventHandler(synchronizationWatch, this);
            controller.Connect += listener.OnServiceConnect;
            controller.Device += listener.OnConnect;
            controller.DeviceLost += listener.OnDisconnect;
            controller.FrameReady += listener.OnFrame;

            Logger.Info("Init Leap Motion Adapter complete");
        }

        public void Stop()
        {
        }

        public virtual void OnFrameAvailable(Model.MocapFrame frame)
        {
            if (FrameAvailable != null) // Check if there are subscribers to the event
            {
                FrameAvailable(this, frame);
            }
        }
    }
}
