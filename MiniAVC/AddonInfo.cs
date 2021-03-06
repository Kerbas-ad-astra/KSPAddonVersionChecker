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
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

#endregion

namespace MiniAVC
{
    public class AddonInfo
    {
        #region Fields

        private static readonly VersionInfo actualKspVersion;

        private readonly string path;

        private VersionInfo kspVersion;
        private VersionInfo kspVersionMax;
        private VersionInfo kspVersionMin;

        #endregion

        #region Constructors

        public AddonInfo(string path, string json)
        {
            try
            {
                this.path = path;
                this.Parse(json);
            }
            catch
            {
                this.ParseError = true;
                throw;
            }
            finally
            {
                if (this.ParseError)
                {
                    Logger.Log("Version file contains errors: " + path);
                }
            }
        }

        static AddonInfo()
        {
            actualKspVersion = new VersionInfo(Versioning.version_major, Versioning.version_minor, Versioning.Revision);
        }

        #endregion

        #region Properties

        public static VersionInfo ActualKspVersion
        {
            get { return actualKspVersion; }
        }

        public string Download { get; private set; }

        public GitHubInfo GitHub { get; private set; }

        public bool IsCompatible
        {
            get { return this.IsCompatibleKspVersion || ((this.kspVersionMin != null || this.kspVersionMax != null) && this.IsCompatibleKspVersionMin && this.IsCompatibleKspVersionMax); }
        }

        public bool IsCompatibleGitHubVersion
        {
            get { return this.GitHub == null || this.GitHub.Version == null || this.Version == this.GitHub.Version; }
        }

        public bool IsCompatibleKspVersion
        {
            get { return Equals(this.KspVersion, actualKspVersion); }
        }

        public bool IsCompatibleKspVersionMax
        {
            get { return this.KspVersionMax >= actualKspVersion; }
        }

        public bool IsCompatibleKspVersionMin
        {
            get { return this.KspVersionMin <= actualKspVersion; }
        }

        public VersionInfo KspVersion
        {
            get { return (this.kspVersion ?? actualKspVersion); }
        }

        public VersionInfo KspVersionMax
        {
            get { return (this.kspVersionMax ?? VersionInfo.MaxValue); }
        }

        public VersionInfo KspVersionMin
        {
            get { return (this.kspVersionMin ?? VersionInfo.MinValue); }
        }

        public string Name { get; private set; }

        public bool ParseError { get; private set; }

        public string Url { get; private set; }

        public VersionInfo Version { get; private set; }

        #endregion

        #region Methods: public

        public void FetchRemoteData()
        {
            if (this.GitHub != null)
            {
                this.GitHub.FetchRemoteData();
            }
        }

        public override string ToString()
        {
            return this.path +
                   "\n\tNAME: " + (String.IsNullOrEmpty(this.Name) ? "NULL (required)" : this.Name) +
                   "\n\tURL: " + (String.IsNullOrEmpty(this.Url) ? "NULL" : this.Url) +
                   "\n\tDOWNLOAD: " + (String.IsNullOrEmpty(this.Download) ? "NULL" : this.Download) +
                   "\n\tGITHUB: " + (this.GitHub != null ? this.GitHub.ToString() : "NULL") +
                   "\n\tVERSION: " + (this.Version != null ? this.Version.ToString() : "NULL (required)") +
                   "\n\tKSP_VERSION: " + this.KspVersion +
                   "\n\tKSP_VERSION_MIN: " + (this.kspVersionMin != null ? this.kspVersionMin.ToString() : "NULL") +
                   "\n\tKSP_VERSION_MAX: " + (this.kspVersionMax != null ? this.kspVersionMax.ToString() : "NULL") +
                   "\n\tCompatibleKspVersion: " + this.IsCompatibleKspVersion +
                   "\n\tCompatibleKspVersionMin: " + this.IsCompatibleKspVersionMin +
                   "\n\tCompatibleKspVersionMax: " + this.IsCompatibleKspVersionMax +
                   "\n\tCompatibleGitHubVersion: " + this.IsCompatibleGitHubVersion;
        }

        #endregion

        #region Methods: private

        private static string FormatCompatibleUrl(string url)
        {
            if (!url.Contains("github.com"))
            {
                return url;
            }

            url = url.Replace("github.com", "raw.githubusercontent.com");
            url = url.Replace("/tree/", "/");
            url = url.Replace("/blob/", "/");
            return url;
        }

        private static VersionInfo GetVersion(object obj)
        {
            if (obj is Dictionary<string, object>)
            {
                return ParseVersion(obj as Dictionary<string, object>);
            }
            return new VersionInfo((string)obj);
        }

        private static VersionInfo ParseVersion(Dictionary<string, object> data)
        {
            var version = new VersionInfo();

            foreach (var key in data.Keys)
            {
                switch (key.ToUpper())
                {
                    case "MAJOR":
                        version.Major = (long)data[key];
                        break;

                    case "MINOR":
                        version.Minor = (long)data[key];
                        break;

                    case "PATCH":
                        version.Patch = (long)data[key];
                        break;

                    case "BUILD":
                        version.Build = (long)data[key];
                        break;
                }
            }

            return version;
        }

        private void Parse(string json)
        {
            var data = Json.Deserialize(json) as Dictionary<string, object>;
            if (data == null)
            {
                this.ParseError = true;
                return;
            }
            foreach (var key in data.Keys)
            {
                switch (key.ToUpper())
                {
                    case "NAME":
                        this.Name = (string)data[key];
                        break;

                    case "URL":
                        this.Url = FormatCompatibleUrl((string)data[key]);
                        break;

                    case "DOWNLOAD":
                        this.Download = (string)data[key];
                        break;

                    case "GITHUB":
                        this.GitHub = new GitHubInfo(data[key], this);
                        break;

                    case "VERSION":
                        this.Version = GetVersion(data[key]);
                        break;

                    case "KSP_VERSION":
                        this.kspVersion = GetVersion(data[key]);
                        break;

                    case "KSP_VERSION_MIN":
                        this.kspVersionMin = GetVersion(data[key]);
                        break;

                    case "KSP_VERSION_MAX":
                        this.kspVersionMax = GetVersion(data[key]);
                        break;
                }
            }
        }

        #endregion

        #region Nested Type: GitHubInfo

        public class GitHubInfo
        {
            #region Fields

            private readonly AddonInfo addonInfo;

            #endregion

            #region Constructors

            public GitHubInfo(object obj, AddonInfo addonInfo)
            {
                this.addonInfo = addonInfo;
                this.ParseJson(obj);
            }

            #endregion

            #region Properties

            public bool AllowPreRelease { get; private set; }

            public bool ParseError { get; private set; }

            public string Repository { get; private set; }

            public string Tag { get; private set; }

            public string Username { get; private set; }

            public VersionInfo Version { get; private set; }

            #endregion

            #region Methods: public

            public void FetchRemoteData()
            {
                try
                {
                    using (var www = new WWW("https://api.github.com/repos/" + this.Username + "/" + this.Repository + "/releases"))
                    {
                        while (!www.isDone)
                        {
                            Thread.Sleep(100);
                        }
                        if (www.error == null)
                        {
                            this.ParseGitHubJson(www.text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }

            public override string ToString()
            {
                return this.Username + "/" + this.Repository +
                       "\n\t\tLatestRelease: " + (this.Version != null ? this.Version.ToString() : "NULL") +
                       "\n\t\tAllowPreRelease: " + this.AllowPreRelease;
            }

            #endregion

            #region Methods: private

            private void ParseGitHubJson(string json)
            {
                try
                {
                    var obj = Json.Deserialize(json) as List<object>;
                    if (obj == null || obj.Count == 0)
                    {
                        this.ParseError = true;
                        return;
                    }

                    foreach (Dictionary<string, object> data in obj)
                    {
                        if (!this.AllowPreRelease && (bool)data["prerelease"])
                        {
                            continue;
                        }

                        var tag = (string)data["tag_name"];
                        var version = GetVersion(data["tag_name"]);

                        if (version == null || version <= this.Version)
                        {
                            continue;
                        }

                        this.Version = version;
                        this.Tag = tag;

                        if (String.IsNullOrEmpty(this.addonInfo.Download))
                        {
                            this.addonInfo.Download = "https://github.com/" + this.Username + "/" + this.Repository + "/releases/tag/" + this.Tag;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }

            private void ParseJson(object obj)
            {
                var data = obj as Dictionary<string, object>;
                if (data == null)
                {
                    this.ParseError = true;
                    return;
                }

                foreach (var key in data.Keys)
                {
                    switch (key)
                    {
                        case "USERNAME":
                            this.Username = (string)data[key];
                            break;

                        case "REPOSITORY":
                            this.Repository = (string)data[key];
                            break;

                        case "ALLOW_PRE_RELEASE":
                            this.AllowPreRelease = (bool)data[key];
                            break;
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}