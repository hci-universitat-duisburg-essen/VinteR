﻿using System;
using VinteR.Configuration;
using VinteR.Model;
using VinteR.Rest;
using VinteR.Streaming;

namespace VinteR.MainApplication
{
    public class MainApplication : IMainApplication
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private enum ApplicationMode
        {
            Live,
            Play,
            Waiting
        }

        private readonly string _startMode;
        private readonly IRecordService _recordService;
        private readonly IPlaybackService _playbackService;
        private readonly IRestRouter[] _restRouters;
        private readonly IRestServer _restServer;
        private readonly IStreamingServer _streamingServer;
        private ApplicationMode _currentMode;

        public MainApplication(IConfigurationService configurationService, 
            IRecordService recordService,
            IPlaybackService playbackService, 
            IRestServer restServer,
            IRestRouter[] routers,
            IStreamingServer streamingServer)
        {
            _startMode = configurationService.GetConfiguration().StartMode;
            _recordService = recordService;
            _playbackService = playbackService;
            _streamingServer = streamingServer;
            _restServer = restServer;
            _restRouters = routers;
            _currentMode = ApplicationMode.Waiting;
        }

        public void Start()
        {
            _restServer.Start();

            // start streaming server
            _streamingServer.Start();

            _playbackService.FrameAvailable += _streamingServer.Send;
            _recordService.FrameAvailable += _streamingServer.Send;

            foreach (var restRouter in _restRouters)
            {
                restRouter.OnPlayCalled += HandleOnPlayCalled;
                restRouter.OnPausePlaybackCalled += HandleOnPausePlaybackCalled;
                restRouter.OnStopPlaybackCalled += HandleOnStopPlaybackCalled;
                restRouter.OnJumpPlaybackCalled += HandleOnJumpPlaybackCalled;
                restRouter.OnRecordSessionCalled += HandleOnRecordSessionCalled;
                restRouter.OnStopRecordCalled += HandleOnStopRecordCalled;
            }

            switch (_startMode)
            {
                case "record":
                    StartRecord();
                    break;
                case "playback":
                    // nothing to to without session to play
                    break;
            }
        }

        private Session HandleOnStopRecordCalled()
        {
            return StopRecord();
        }

        private Session HandleOnRecordSessionCalled()
        {
            return StartRecord();
        }

        private void HandleOnJumpPlaybackCalled(object sender, uint millis)
        {
            JumpPlayback(millis);
        }

        private void HandleOnStopPlaybackCalled(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void HandleOnPausePlaybackCalled(object sender, EventArgs e)
        {
            PausePlayback();
        }

        private Session HandleOnPlayCalled(string source, string sessionName)
        {
            return StartPlayback(source, sessionName);
        }

        public Session StartRecord()
        {
            switch (_currentMode)
            {
                case ApplicationMode.Live:
                    Logger.Warn("Already recording");
                    break;
                case ApplicationMode.Play:
                    StopPlayback();
                    _recordService.Start();
                    break;
                case ApplicationMode.Waiting:
                    _recordService.Start();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentMode = ApplicationMode.Live;
            return _recordService.Session;
        }

        public Session StopRecord()
        {
            if (_currentMode == ApplicationMode.Live)
            {
                _recordService.Stop();
                _currentMode = ApplicationMode.Waiting;
                return _recordService.Session;
            }

            Logger.Warn("Application not in record");
            return null;
        }

        public Session StartPlayback(string source, string sessionName)
        {
            Session session;
            switch (_currentMode)
            {
                case ApplicationMode.Live:
                    StopRecord();
                    session = _playbackService.Play(source, sessionName);
                    break;
                case ApplicationMode.Play:
                case ApplicationMode.Waiting:
                    session = _playbackService.Play(source, sessionName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentMode = _playbackService.IsPlaying 
                ? ApplicationMode.Play 
                : ApplicationMode.Waiting;
            return session;
        }

        public void PausePlayback()
        {
            if (_currentMode == ApplicationMode.Play)
            {
                _playbackService.Stop();
            }
            else
            {
                Logger.Warn("Application not in playback");
            }
        }

        public void StopPlayback()
        {
            if (_currentMode == ApplicationMode.Play)
            {
                _playbackService.Pause();
                _currentMode = ApplicationMode.Waiting;
            }
            else
            {
                Logger.Warn("Application not in playback");
            }
        }

        public void JumpPlayback(uint millis)
        {
            if (_currentMode == ApplicationMode.Play)
            {
                _playbackService.Jump(millis);
            }
            else
            {
                Logger.Warn("Application not in playback");
            }
        }

        public void Exit()
        {
            switch (_currentMode)
            {
                case ApplicationMode.Live:
                    _recordService.Stop();
                    break;
                case ApplicationMode.Play:
                    _playbackService.Stop();
                    break;
                case ApplicationMode.Waiting:
                    Logger.Info("All modes already stopped");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _restServer.Stop();
            _streamingServer.Stop();

            _playbackService.FrameAvailable -= _streamingServer.Send;
            _recordService.FrameAvailable -= _streamingServer.Send;

            Logger.Info("Application exited");
        }
    }
}