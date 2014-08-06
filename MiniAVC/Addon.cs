﻿// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#region Using Directives

using System;
using System.IO;
using System.Net;
using System.Threading;

#endregion

namespace MiniAVC
{
    public class Addon
    {
        #region Fields

        private readonly string rootPath;
        private readonly AddonSettings settings;
        private AddonInfo localInfo;
        private AddonInfo remoteInfo;

        #endregion

        #region Contructors

        public Addon(string rootPath)
        {
            try
            {
                this.rootPath = rootPath;
                this.settings = AddonSettings.Load(rootPath);

                var files = Directory.GetFiles(this.rootPath, "*.version", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    this.HasVersionFile = true;
                    this.RunProcessLocalInfo(files[0]);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        #endregion

        #region Properties

        public string RootPath
        {
            get { return this.rootPath; }
        }

        public AddonSettings Settings
        {
            get { return this.settings; }
        }

        public AddonInfo LocalInfo
        {
            get { return this.localInfo; }
        }

        public AddonInfo RemoteInfo
        {
            get { return this.remoteInfo; }
        }

        public bool HasVersionFile { get; private set; }

        public bool IsLocalReady { get; private set; }

        public bool IsRemoteReady { get; private set; }

        public bool IsProcessingComplete { get; private set; }

        public bool IsUpdateAvailable
        {
            get { return this.IsProcessingComplete && this.RemoteInfo.Version > this.LocalInfo.Version && this.RemoteInfo.IsCompatibleKspVersion; }
        }

        public bool IsCompatible
        {
            get { return this.IsLocalReady && this.LocalInfo.IsCompatible; }
        }

        public string Name
        {
            get { return this.LocalInfo.Name; }
        }

        #endregion

        #region Public Methods

        public void RunProcessLocalInfo(string file)
        {
            ThreadPool.QueueUserWorkItem(this.ProcessLocalInfo, file);
        }

        public void RunProcessRemoteInfo()
        {
            ThreadPool.QueueUserWorkItem(this.ProcessRemoteInfo);
        }

        #endregion

        #region Private Methods

        private void ProcessLocalInfo(object state)
        {
            try
            {
                using (var stream = new StreamReader(File.OpenRead((string)state)))
                {
                    this.localInfo = new AddonInfo((string)state, stream.ReadToEnd());
                }
                if (!this.settings.FirstRun || string.IsNullOrEmpty(this.localInfo.Url))
                {
                    this.RunProcessRemoteInfo();
                }

                this.IsLocalReady = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        private void ProcessRemoteInfo(object state)
        {
            try
            {
                if (!this.Settings.AllowCheck || string.IsNullOrEmpty(this.localInfo.Url))
                {
                    this.remoteInfo = this.localInfo;
                    this.IsRemoteReady = true;
                    this.IsProcessingComplete = true;
                    Logger.Log(this.localInfo);
                    Logger.Blank();
                    return;
                }

                using (var web = WebRequest.Create(this.localInfo.Url).GetResponse())
                {
                    using (var stream = new StreamReader(web.GetResponseStream()))
                    {
                        this.remoteInfo = new AddonInfo(this.localInfo.Url, stream.ReadToEnd());
                    }
                }

                this.IsRemoteReady = true;
                this.IsProcessingComplete = true;
                Logger.Log(this.localInfo);
                Logger.Log(this.remoteInfo);
                Logger.Blank();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        #endregion
    }
}