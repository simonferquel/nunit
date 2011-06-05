// ***********************************************************************
// Copyright (c) 2011 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Collections.Specialized;
using System.Threading;
using Microsoft.Win32;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// Provides static methods for accessing the NUnit config
    /// file 
    /// </summary>
    public class NUnitConfiguration
    {
        #region Class Constructor
        /// <summary>
        /// Class constructor initializes fields from config file
        /// </summary>
        static NUnitConfiguration()
        {
            try
            {
                NameValueCollection settings = GetConfigSection("NUnit/TestCaseBuilder");
                if (settings != null)
                {
                    string oldStyle = settings["OldStyleTestCases"];
                    if (oldStyle != null)
                            allowOldStyleTests = Boolean.Parse(oldStyle);
                }

                settings = GetConfigSection("NUnit/TestRunner");
                if (settings != null)
                {
                    string apartment = settings["ApartmentState"];
                    if (apartment != null)
                        apartmentState = (ApartmentState)
                            System.Enum.Parse(typeof(ApartmentState), apartment, true);

                    string priority = settings["ThreadPriority"];
                    if (priority != null)
                        threadPriority = (ThreadPriority)
                            System.Enum.Parse(typeof(ThreadPriority), priority, true);
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Invalid configuration setting in {0}",
                    AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                throw new ApplicationException(msg, ex);
            }
        }

        private static NameValueCollection GetConfigSection( string name )
        {
#if CLR_2_0 || CLR_4_0
            return (NameValueCollection)System.Configuration.ConfigurationManager.GetSection(name);
#else
			return (NameValueCollection)System.Configuration.ConfigurationSettings.GetConfig(name);
#endif
        }
        #endregion

        #region Public Properties

        #region AllowOldStyleTests
        private static bool allowOldStyleTests = false;
        public static bool AllowOldStyleTests
        {
            get { return allowOldStyleTests; }
        }
        #endregion

        #region ThreadPriority
        private static ThreadPriority threadPriority = ThreadPriority.Normal;
        public static ThreadPriority ThreadPriority
        {
            get { return threadPriority; }
        }
        #endregion

        #region ApartmentState
        private static ApartmentState apartmentState = ApartmentState.Unknown;
        public static ApartmentState ApartmentState
        {
            get { return apartmentState; }
            //set { apartmentState = value; }
        }
        #endregion

        #region BuildConfiguration
        public static string BuildConfiguration
        {
            get
            {
#if DEBUG
                    return "Debug";
#else
					return "Release";
#endif
            }
        }
        #endregion

        #region NUnitLibDirectory
        private static string nunitLibDirectory;
        /// <summary>
        /// Gets the path to the lib directory for the version and build
        /// of NUnit currently executing.
        /// </summary>
        public static string NUnitLibDirectory
        {
            get
            {
                if (nunitLibDirectory == null)
                {
                    nunitLibDirectory =
                        AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());
                }

                return nunitLibDirectory;
            }
        }
        #endregion

        #region NUnitBinDirectory
        private static string nunitBinDirectory;
        public static string NUnitBinDirectory
        {
            get
            {
                if (nunitBinDirectory == null)
                {
                    nunitBinDirectory = NUnitLibDirectory;
                    if (Path.GetFileName(nunitBinDirectory).ToLower() == "lib")
                        nunitBinDirectory = Path.GetDirectoryName(nunitBinDirectory);
                }

                return nunitBinDirectory;
            }
        }
        #endregion

        #region AddinDirectory
        private static string addinDirectory;
        public static string AddinDirectory
        {
            get
            {
                if (addinDirectory == null)
                {
                    addinDirectory = Path.Combine(NUnitBinDirectory, "addins");
                }

                return addinDirectory;
            }
        }
        #endregion

        #region TestAgentExePath
        //private static string testAgentExePath;
        //private static string TestAgentExePath
        //{
        //    get
        //    {
        //        if (testAgentExePath == null)
        //            testAgentExePath = Path.Combine(NUnitBinDirectory, "nunit-agent.exe");

        //        return testAgentExePath;
        //    }
        //}
        #endregion

        #region MonoExePath
        private static string monoExePath;
        public static string MonoExePath
        {
            get
            {
                if (monoExePath == null)
                {
                    string[] searchNames = IsWindows()
                        ? new string[] { "mono.bat", "mono.cmd", "mono.exe" }
                        : new string[] { "mono", "mono.exe" };
                    
                    monoExePath = FindOneOnPath(searchNames);

                    if (monoExePath == null && IsWindows())
                    {
                        RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Novell\Mono");
                        if (key != null)
                        {
                            string version = key.GetValue("DefaultCLR") as string;
                            if (version != null)
                            {
                                key = key.OpenSubKey(version);
                                if (key != null)
                                {
                                    string installDir = key.GetValue("SdkInstallRoot") as string;
                                    if (installDir != null)
                                        monoExePath = Path.Combine(installDir, @"bin\mono.exe");
                                }
                            }
                        }
                    }

                    if (monoExePath == null)
                        return "mono";
                }

                return monoExePath;
            }
        }

        private static string FindOneOnPath(string[] names)
        {
            //foreach (string dir in Environment.GetEnvironmentVariable("path").Split(new char[] { Path.PathSeparator }))
            //    foreach (string name in names)
            //    {
            //        string fullPath = Path.Combine(dir, name);
            //        if (File.Exists(fullPath))
            //            return name;
            //    }

            return null;
        }

        private static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
        #endregion

        #region ApplicationDataDirectory
        private static string applicationDirectory;
        public static string ApplicationDirectory
        {
            get
            {
                if (applicationDirectory == null)
                {
                    applicationDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "NUnit");
                }

                return applicationDirectory;
            }
        }
        #endregion

        #region HelpUrl
        public static string HelpUrl
        {
            get
            {
#if CLR_2_0 || CLR_4_0
                string helpUrl = ConfigurationManager.AppSettings["helpUrl"];
#else
                string helpUrl = ConfigurationSettings.AppSettings["helpUrl"];
#endif

                if (helpUrl == null)
                {
                    helpUrl = "http://nunit.org";
                    string dir = Path.GetDirectoryName(NUnitBinDirectory);
                    if ( dir != null )
                    {
                        dir = Path.GetDirectoryName(dir);
                        if ( dir != null )
                        {
                            string localPath = Path.Combine(dir, @"doc/index.html");
                            if (File.Exists(localPath))
                            {
                                UriBuilder uri = new UriBuilder();
                                uri.Scheme = "file";
                                uri.Host = "localhost";
                                uri.Path = localPath;
                                helpUrl = uri.ToString();
                            }
                        }
                    }
                }

                return helpUrl;
            }
        }
        #endregion

        #endregion
    }
}