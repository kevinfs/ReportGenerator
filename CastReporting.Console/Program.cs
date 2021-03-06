﻿using Cast.Util;
using Cast.Util.Log;
using CastReporting.BLL;
using CastReporting.Console.Argument;
using CastReporting.Domain;
using CastReporting.Reporting.Builder;
using CastReporting.Reporting.ReportingModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace CastReporting.Console
{
    /// <summary>
    /// 
    /// </summary>
    class Program
    {
        private Program()
        {
            // Avoid instanciation of the class
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        { 
            string showhelp;
            

            LogHelper.SetPathLog(SettingsBLL.GetApplicationPath());
            
            SetCulture();
           
            LogHelper.Instance.LogInfo("Application started.");

            Environment.ExitCode = DoWork(args, out showhelp);

            if (!string.IsNullOrEmpty(showhelp)) System.Console.WriteLine(showhelp);

            // Uncomment if you want to see the console during debugging
            // System.Console.ReadLine();
        }


        /// <summary>
        /// 
        /// </summary>
        private static void SetCulture()
        {
            var settings = SettingsBLL.GetSetting();

            if (string.IsNullOrEmpty(settings.ReportingParameter.CultureName)) return;
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(settings.ReportingParameter.CultureName);

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="help"></param>
        /// <returns></returns>
        private static int DoWork(string[] args, out string help)
        {
            LogHelper.Instance.LogInfo("Read arguments.");
            var arguments = ReadArguments(args, out help); 

            if (!string.IsNullOrEmpty(help))
            {             
                return -1;
            } 

            string pathFile = GenerateReport(arguments, out help);

            if (!string.IsNullOrEmpty(help))
            {
                LogHelper.Instance.LogError(help);
                return -1;
            }
            else
            {
                LogHelper.Instance.LogInfo($"Report successfully generated in {pathFile}");
                return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="help"></param>
        /// <returns></returns>
        private static string GenerateReport(XmlCastReport arguments, out string help)
        {
            if (arguments.ReportType != null)
            {

                string reportPath = string.Empty; 
                string tmpReportFile = string.Empty;

                try
                {
                    help = string.Empty;

                    //Get RG settings
                    var settings = SettingsBLL.GetSetting();
                    LogHelper.Instance.LogInfo("RG settings have been read successfully");

                    //Initialize temporary directory
                    string workDirectory = SettingsBLL.GetApplicationPath();

                    //Initialize Web services

                    var connection = new WSConnection(arguments.Webservice.Name, arguments.Username.Name, arguments.Password.Name, string.Empty);
                    using (CommonBLL commonBLL = new CommonBLL(connection))
                    {

                        if (!commonBLL.CheckService())
                        {
                            help = $"Webservice can't be access or is bad formatted. Url:{arguments.Webservice.Name} Username:{arguments.Username.Name} Password:{arguments.Password.Name}";
                            return string.Empty;
                        }
                    }
                    LogHelper.Instance.LogInfo("Web services Initialized successfully");

                    List<Application> _apps = new List<Application>();
                    
                    try
                    {
                        using (CastDomainBLL castDomainBLL = new CastDomainBLL(connection))
                        {
                            _apps = castDomainBLL.GetCommonTaggedApplications(arguments.Tag?.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Instance.LogInfo("Error occured while trying get applications for the portfolio : " + ex.Message);
                    }

                    Application[] _selectedApps = _apps.ToArray<Application>();
                    LogHelper.Instance.LogInfo("Applications is the portfolio found successfully");
                    string[] _appsToIgnorePortfolioResult = PortfolioBLL.BuildPortfolioResult(connection, _selectedApps);
                    LogHelper.Instance.LogInfo("Build result for the portfolio");
                    List<Application> _n_apps = new List<Application>();
                    //Remove from Array the Ignored Apps
                    foreach (Application app in _selectedApps)
                    {
                        int intAppYes = 0;
                        foreach (string s in _appsToIgnorePortfolioResult)
                        {
                            if (s == app.Name)
                            {
                                intAppYes = 1;
                                break;
                            }
                            else
                            {
                                intAppYes = 0;
                            }
                        }

                        if (intAppYes == 0)
                        {
                            _n_apps.Add(app);
                        }
                    }
                    Application[] _n_selectedApps = _n_apps.ToArray();

                    List<Snapshot> _snapshots = new List<Snapshot>();

                    try
                    {
                        using (CastDomainBLL castDomainBLL = new CastDomainBLL(connection))
                        {
                            _snapshots = castDomainBLL.GetAllSnapshots(_n_selectedApps);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Instance.LogInfo("Error occured while trying get snapshots of applications for the portfolio : " + ex.Message);
                    }
                    LogHelper.Instance.LogInfo("Snapshots is the portfolio found successfully");
                    List<Snapshot> _n_snaps = new List<Snapshot>();
                    if (_snapshots != null)
                    {
                        Snapshot[] _selectedApps_snapshots = _snapshots.ToArray<Snapshot>();
                        var _snapsToIgnore = PortfolioSnapshotsBLL.BuildSnapshotResult(connection, _selectedApps_snapshots, true);
                        LogHelper.Instance.LogInfo("Build result for snapshots in portfolio");

                        foreach (Snapshot snap in _selectedApps_snapshots)
                        {
                            int intRemoveYes = 0;
                            foreach (string s in _snapsToIgnore)
                            {
                                if (s == snap.Href)
                                {
                                    intRemoveYes = 1;
                                    break;
                                }
                                else
                                {
                                    intRemoveYes = 0;
                                }
                            }
                            if (intRemoveYes == 0)
                            {
                                _n_snaps.Add(snap);
                            }
                        }

                        Snapshot[] _n_selectedApps_snapshots = _n_snaps.ToArray();

                        string tmpReportFileFlexi = string.Empty;

                        try
                        {

                            //Create temporary report
                            tmpReportFile = PathUtil.CreateTempCopy(workDirectory, Path.Combine(settings.ReportingParameter.TemplatePath + "\\Portfolio", arguments.Template.Name));
                            if (tmpReportFile.Contains(".xlsx"))
                            {
                                tmpReportFileFlexi = PathUtil.CreateTempCopyFlexi(workDirectory, arguments.Template.Name);
                            }
                            //Build report
                            ReportData reportData;
                            if (arguments.Category != null && arguments.Tag != null)
                            {
                                reportData = new ReportData()
                                {
                                    FileName = tmpReportFile,
                                    Application = null,
                                    CurrentSnapshot = null,
                                    PreviousSnapshot = null,
                                    RuleExplorer = new RuleBLL(connection),
                                    CurrencySymbol = "$",
                                    Applications = _n_selectedApps,
                                    Category = arguments.Category.Name,
                                    Tag = arguments.Tag.Name,
                                    Snapshots = _n_selectedApps_snapshots,
                                    IgnoresApplications = _appsToIgnorePortfolioResult,
                                    IgnoresSnapshots = _snapsToIgnore
                                };
                            }
                            else if (arguments.Category != null && arguments.Tag == null)
                            {
                                reportData = new ReportData()
                                {
                                    FileName = tmpReportFile,
                                    Application = null,
                                    CurrentSnapshot = null,
                                    PreviousSnapshot = null,
                                    RuleExplorer = new RuleBLL(connection),
                                    CurrencySymbol = "$",
                                    Applications = _n_selectedApps,
                                    Category = arguments.Category.Name,
                                    Tag = null,
                                    Snapshots = _n_selectedApps_snapshots,
                                    IgnoresApplications = _appsToIgnorePortfolioResult,
                                    IgnoresSnapshots = _snapsToIgnore,
                                    Parameter = settings.ReportingParameter
                                };
                            }
                            else if (arguments.Category == null && arguments.Tag != null)
                            {
                                reportData = new ReportData()
                                {
                                    FileName = tmpReportFile,
                                    Application = null,
                                    CurrentSnapshot = null,
                                    PreviousSnapshot = null,
                                    RuleExplorer = new RuleBLL(connection),
                                    CurrencySymbol = "$",
                                    Applications = _n_selectedApps,
                                    Category = null,
                                    Tag = arguments.Tag.Name,
                                    Snapshots = _n_selectedApps_snapshots,
                                    IgnoresApplications = _appsToIgnorePortfolioResult,
                                    IgnoresSnapshots = _snapsToIgnore,
                                    Parameter = settings.ReportingParameter
                                };
                            }
                            else
                            {
                                reportData = new ReportData()
                                {
                                    FileName = tmpReportFile,
                                    Application = null,
                                    CurrentSnapshot = null,
                                    PreviousSnapshot = null,
                                    RuleExplorer = new RuleBLL(connection),
                                    CurrencySymbol = "$",
                                    Applications = _n_selectedApps,
                                    Category = null,
                                    Tag = null,
                                    Snapshots = _n_selectedApps_snapshots,
                                    IgnoresApplications = _appsToIgnorePortfolioResult,
                                    IgnoresSnapshots = _snapsToIgnore,
                                    Parameter = settings.ReportingParameter
                                };
                            }

                            using (IDocumentBuilder docBuilder = BuilderFactory.CreateBuilder(reportData, tmpReportFile))
                            {
                                docBuilder.BuildDocument();
                            }
                            LogHelper.Instance.LogInfo("Report generated successfully");

                            //Set filte report              
                            SetFileName(arguments);

                            reportPath = Path.Combine(string.IsNullOrEmpty(settings.ReportingParameter.GeneratedFilePath) 
                                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) 
                                : settings.ReportingParameter.GeneratedFilePath, arguments.File.Name);
                            
                            if (tmpReportFile.Contains(".xlsx"))
                            {
                                tmpReportFile = tmpReportFileFlexi;
                            }

                            //Copy report file to the selected destination
                            File.Copy(tmpReportFile, reportPath, true);
                        }
                        finally
                        {
                            if (!string.IsNullOrEmpty(tmpReportFile)) File.Delete(tmpReportFile);
                        }
                    }


                    return reportPath;
                }
                catch (Exception ex)
                {
                    help = $"An exception occured : {ex}";
                    return string.Empty;
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tmpReportFile)) File.Delete(tmpReportFile);
                }
            }
            else
            {

                string tmpReportFile = string.Empty;
                string tmpReportFileFlexi = string.Empty;

                try
                {
                    help = string.Empty;

                    //Get RG settings
                    var settings = SettingsBLL.GetSetting();
                    LogHelper.Instance.LogInfo("RG settings have been read successfully");

                    //Initialize temporary directory
                    string workDirectory = SettingsBLL.GetApplicationPath();
                    tmpReportFile = PathUtil.CreateTempCopy(workDirectory, Path.Combine(settings.ReportingParameter.TemplatePath, arguments.Template.Name));
                    if (tmpReportFile.Contains(".xlsx"))
                    {
                        tmpReportFileFlexi = PathUtil.CreateTempCopyFlexi(workDirectory, Path.Combine(settings.ReportingParameter.TemplatePath, arguments.Template.Name));
                    }
                    //Initialize Web services

                    var connection = new WSConnection(arguments.Webservice.Name, arguments.Username.Name, arguments.Password.Name, string.Empty);
                    using (CommonBLL commonBLL = new CommonBLL(connection))
                    {
                        if (!commonBLL.CheckService())
                        {
                            help = $"Webservice can't be access or is bad formatted. Url:{arguments.Webservice.Name} Username:{arguments.Username.Name} Password:{arguments.Password.Name}";
                            return string.Empty;
                        }
                    }
                    LogHelper.Instance.LogInfo("Web services Initialized successfully");


                    //Initialize Application
                    Application application = GetApplication(arguments.Application.Name, connection);
                    if (application == null)
                    {
                        help = $"Application {arguments.Application.Name} can't be found.";
                        return string.Empty;
                    }
                    LogHelper.Instance.LogInfo($"Application {arguments.Application.Name} Initialized successfully");

                    //Initialize snapshots             
                    SetSnapshots(connection, application);
                    if (application.Snapshots == null)
                    {
                        help = "There is no snapshots for this application.";
                        return string.Empty;
                    }
                    LogHelper.Instance.LogInfo($"List of Snapshots from {arguments.Application.Name} Initialized successfully");

                    //Build Application results 
                    ApplicationBLL.BuildApplicationResult(connection, application);
                    LogHelper.Instance.LogInfo($"Application {arguments.Application.Name} results built successfully");


                    //Set current snapshot
                    Snapshot currentSnapshot = GetSnapshotOrDefault(arguments.Snapshot.Current, application.Snapshots, 0);
                    if (currentSnapshot == null)
                    {
                        help = $"Current snapshot {arguments.Snapshot.Current.Name} can't be found";
                        return string.Empty;
                    }
                    LogHelper.Instance.LogInfo($"Current snapshot {currentSnapshot.Name} initialized successfully");

                    //Build current snapshot results 
                    SnapshotBLL.BuildSnapshotResult(connection, currentSnapshot, true);
                    LogHelper.Instance.LogInfo($"Result of current snapshot {currentSnapshot.Name} built successfully");

                    //Set previous snapshot
                    Snapshot prevSnapshot = GetSnapshotOrDefault(arguments.Snapshot.Previous, application.Snapshots, 1);
                    if (!string.IsNullOrEmpty(arguments.Snapshot.Previous?.Name) && prevSnapshot == null)
                    {
                        help = $"Previous snapshot {arguments.Snapshot.Previous.Name} can't be found";
                        return string.Empty;
                    }
                    if (prevSnapshot != null) LogHelper.Instance.LogInfo($"Previous snapshot {prevSnapshot.Name} Initialized successfully");

                    //Build previous snapshot results 
                    if (prevSnapshot != null)
                    {
                        SnapshotBLL.BuildSnapshotResult(connection, prevSnapshot, false);
                        LogHelper.Instance.LogInfo($"Result of previous snapshot {prevSnapshot.Name}  built successfully");
                    }


                    //Build report              
                    ReportData reportData = new ReportData()
                    {
                        FileName = tmpReportFile,
                        Application = application,
                        CurrentSnapshot = currentSnapshot,
                        PreviousSnapshot = prevSnapshot,
                        Parameter = settings.ReportingParameter,
                        RuleExplorer = new RuleBLL(connection),
                        SnapshotExplorer = new SnapshotBLL(connection, currentSnapshot),
                        CurrencySymbol = "$"
                    };

                    using (IDocumentBuilder docBuilder = BuilderFactory.CreateBuilder(reportData, tmpReportFileFlexi))
                    {
                        docBuilder.BuildDocument();
                    }
                    LogHelper.Instance.LogInfo("Report generated successfully");

                    //Set filte report              
                    SetFileName(arguments);

                    var reportPath = Path.Combine(string.IsNullOrEmpty(settings.ReportingParameter.GeneratedFilePath)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) 
                        : settings.ReportingParameter.GeneratedFilePath, arguments.File.Name);

                    if (tmpReportFile.Contains(".xlsx"))
                    {
                        tmpReportFile = tmpReportFileFlexi;
                    }
                    File.Copy(tmpReportFile, reportPath, true);
                    LogHelper.Instance.LogInfo("Report moved to generation directory successfully");

                    return reportPath;
                }
                catch (Exception ex)
                {
                    help = $"An exception occured : {ex}";
                    return string.Empty;
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tmpReportFile)) File.Delete(tmpReportFile);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arguments"></param>
        private static void SetFileName(XmlCastReport arguments)
        {
            if (arguments.File == null)
            {
                arguments.File = new XmlTagName() { Name = null };
            }
            if (string.IsNullOrEmpty(arguments.File.Name))
            {
                arguments.File.Name = $"{Path.GetFileNameWithoutExtension(arguments.Template.Name)}_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}{Path.GetExtension(arguments.Template.Name)}";

            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentSnapshotName"></param>
        /// <param name="snapshosts"></param>
        /// <param name="indexDefault"></param>
        /// <returns></returns>
        private static Snapshot GetSnapshotOrDefault(XmlTagName currentSnapshotName, IEnumerable<Snapshot> snapshosts, int indexDefault)
        {
            var currentSnapshot = !string.IsNullOrEmpty(currentSnapshotName?.Name)
                ? snapshosts.FirstOrDefault(_ => $"{_.Name} - {_.Annotation.Version}" == currentSnapshotName.Name) 
                : snapshosts.OrderByDescending(_ => _.Annotation.Date.DateSnapShot).ElementAtOrDefault(indexDefault);
            return currentSnapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private static void SetSnapshots(WSConnection connection, Application application)
        {          
            using (ApplicationBLL applicationBLL = new ApplicationBLL(connection, application))
            {
                applicationBLL.SetSnapshots();                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="application"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private static Application GetApplication(string application, WSConnection connection)
        {
            List<Application> applications;

            using (CastDomainBLL castDomainBLL = new CastDomainBLL(connection))
            {
                applications = castDomainBLL.GetApplications();
            }

            return applications.FirstOrDefault(_ => _.Name == application);
        }


        #region Argument Management
        /// <summary>
        /// Read arguments from Main
        /// </summary>
        /// <param name="pArg"></param>
        /// <param name="pShowHelp"></param>
        /// <returns></returns>
        public static XmlCastReport ReadArguments(string[] pArg, out string pShowHelp)
        {
            pShowHelp = null;
            XmlCastReport argument;

            // Get Arguments File Name
            using (CRArgumentReader reader = new CRArgumentReader())
            {
                bool show;
                argument = reader.Load(pArg, out show);
                if (show)
                    pShowHelp = reader.Help;
            }

            return argument;
        }
        #endregion
    }
}
