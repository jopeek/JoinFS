﻿using System;
using System.Collections.Generic;
#if !CONSOLE
using System.Windows.Forms;
#endif
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Net;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using JoinFS.Properties;
using JoinFS.CAVModels;

namespace JoinFS
{
    public class Main
    {
        // default settings
        public const int DEFAULT_ACTIVITY_CIRCLE = 40;
        public const int DEFAULT_FOLLOW_DISTANCE = 80;

        const int MAX_INSTANCES = 4;

        /// <summary>
        /// Current version
        /// </summary>
        public static string version = "0.0.0";

        /// <summary>
        /// Current version
        /// </summary>
        public static string name = "";

        /// <summary>
        /// Storage path
        /// </summary>
        public string storagePath = ".";

        /// <summary>
        /// Documents path
        /// </summary>
        public string documentsPath = ".";

        /// <summary>
        /// Unique identifier for this installation
        /// </summary>
        public Guid guid = Guid.Empty;
        public uint uuid = 0;

        // modules
        public Sim sim;
        public Network network;
        public Substitution substitution;
        public Recorder recorder;
        public AddressBook addressBook;
        public Log log;
        public Monitor monitor;
        public Euroscope euroscope;
        public Whazzup whazzup;
        public Notes notes;
        public Stats stats;
        public VariableMgr variableMgr;

        /// <summary>
        /// Settings
        /// </summary>
        public bool settingsPortEnabled = false;
        public ushort settingsPort = Network.DEFAULT_PORT;
        public string settingsNickname = "";
        public bool settingsConnectOnLaunch = false;
        public int settingsActivityCircle = DEFAULT_ACTIVITY_CIRCLE;
        public bool settingsAtc = false;
        public string settingsAtcAirport = "";
        public bool settingsHub = false;
        public string settingsHubDomain = "";
        public string settingsHubName = "";
        public string settingsHubAbout = "";
        public string settingsHubVoip = "";
        public string settingsHubEvent = "";
        public string settingsPassword = "";
        public bool settingsWhazzup = false;
        public bool settingsWhazzupPublic = false;
        public bool settingsMultiObjects = false;
        public bool settingsMinimized = false;
        public bool settingsNoGui = false;
        public bool settingsNoSim = false;
        public bool settingsLoop = false;
        public bool settingsXplane = false;
        public bool settingsTcas = false;
        public bool settingsScan = false;
        public string settingsConnectionString = "";
#if XPLANE || CONSOLE
        public bool settingsGenerateCsl = false;
        public bool settingsSkipCsl = false;
#endif

#if CONSOLE
        public bool settingsBackground = false;
#endif

        /// <summary>
        /// shutdown JoinFS
        /// </summary>
        public string shutdown = null;

        /// <summary>
        /// Multithreading lock object
        /// </summary>
        public object conch = new object();

        /// <summary>
        /// instance ID
        /// </summary>
        public int instanceCount = 1;

        /// <summary>
        /// Thread for updates
        /// </summary>
        Thread workThread;
        /// <summary>
        /// Finish work flag
        /// </summary>
        public volatile bool workFinish = false;
        /// <summary>
        /// Close main
        /// </summary>
        public void Close()
        {
            // check for thread
            if (workThread != null)
            {
                // notify thread
                workFinish = true;
                workThread.Join(5000);
            }

            // close systems
            if (network != null)
            {
                // leave session
                network.Leave();
                // close network
                network.localNode.Close();
                // monitor
                MonitorEvent("Closed UDP port " + network.localNode.GetLocalNuid().port);
            }
            sim ?. Close();

            // save settings
            Settings.Default.Save();
        }

        // record time at launch
        Stopwatch stopwatch;

        // get time since launch
        public double ElapsedTime
        {
            get
            {
                return (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
            }
        }

        /// <summary>
        /// Construct main
        /// </summary>
        public Main()
        {
            try
            {
                Logger.WriteLog("Starting JoinFS Server.");

                // get product name and file version
                Assembly assembly = Assembly.GetExecutingAssembly();
                // get name
                name = assembly.GetName().Name;
                // construct three part version number
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                version = info.FileMajorPart.ToString() + '.' + info.FileMinorPart.ToString() + '.' + info.FileBuildPart.ToString();

                // get settings
                settingsPortEnabled = Settings.Default.LocalPortEnabled;
                settingsPort = Settings.Default.LocalPort;
                settingsNickname = Settings.Default.Nickname;
                settingsConnectOnLaunch = Settings.Default.ConnectOnLaunch;
                settingsActivityCircle = Settings.Default.ActivityCircle;
                settingsAtc = Settings.Default.Atc;
                settingsAtcAirport = Settings.Default.AtcAirport.ToUpper();
                settingsHub = Settings.Default.Hub;
                settingsHubDomain = Settings.Default.HubAddress;
                settingsHubName = Settings.Default.HubName;
                settingsHubAbout = Settings.Default.HubAbout;
                settingsHubVoip = Settings.Default.HubVoIP;
                settingsHubEvent = Settings.Default.HubEvent;
                settingsPassword = Settings.Default.Password;
                settingsWhazzup = Settings.Default.Whazzup;
                settingsWhazzupPublic = Settings.Default.WhazzupGlobal;
                settingsMultiObjects = Settings.Default.MultipleObjectsEveryone;
                settingsLoop = Settings.Default.Loop;
                settingsXplane = Settings.Default.XPlane;
                settingsTcas = Settings.Default.TCAS;
                settingsScan = Settings.Default.ModelScanOnConnection;
                settingsConnectionString = Settings.Default.cavConnectionString;

#if XPLANE || CONSOLE
                settingsGenerateCsl = Settings.Default.GenerateCsl;
                settingsSkipCsl = Settings.Default.SkipCsl;
#endif

#if SERVER
                settingsNoSim = true;
#endif

#if XPLANE
                settingsXplane = true;
#endif

                // command line optins
                bool doCreate = false;
                string doJoin = null;
                bool doGlobal = false;
                string doPlay = "";
                bool doRecord = false;
                bool doQuit = false;
#if XPLANE || CONSOLE
                bool doPlugin = false;
#endif
                string simFolder = "";

                // get command line
                string[] args = Environment.GetCommandLineArgs();
                // process command line
                for (int index = 1; index < args.Length; index++)
                {
                    // check for option
                    if (args[index].Length > 0 && args[index][0] == '-')
                    {
                        // handle -- type options as well
                        string option = args[index].StartsWith("--") ? args[index].Substring(1) : args[index];
                        // process option
                        switch (option)
                        {
                            case "-play":
                                // next parameter
                                index++;
                                // check for file name
                                if (index < args.Length)
                                {
                                    // play specified recording
                                    doPlay = args[index];
                                }
                                break;

                            case "-record":
                                // start new recording
                                doRecord = true;
                                break;

                            case "-loop":
                                // enable looping
                                settingsLoop = true;
                                break;

                            case "-create":
                                // create network
                                doCreate = true;
                                break;

                            case "-rejoin":
                                // join network
                                doJoin = Settings.Default.JoinAddress;
                                break;

                            case "-join":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // join network
                                    doJoin = args[index];
                                }
                                break;

                            case "-port":
                                // next parameter
                                index++;
                                // check for file name
                                if (index < args.Length)
                                {
                                    // get port
                                    if (ushort.TryParse(args[index], NumberStyles.Number, CultureInfo.InvariantCulture, out settingsPort))
                                    {
                                        // use port
                                        settingsPortEnabled = true;
                                    }
                                }
                                break;

                            case "-hub":
                                // enable hub mode
                                settingsHub = true;
                                break;

                            case "-hubdomain":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // update hub domain
                                    settingsHubDomain = args[index];
                                }
                                break;

                            case "-hubname":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length && args[index].Length >= 3)
                                {
                                    // update hub name
                                    settingsHubName = args[index];
                                }
                                break;

                            case "-hubabout":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // update hub about
                                    settingsHubAbout = args[index];
                                }
                                break;

                            case "-hubvoip":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // update hub voice server
                                    settingsHubVoip = args[index];
                                }
                                break;

                            case "-hubevent":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // update hub event
                                    settingsHubEvent = args[index];
                                }
                                break;

                            case "-nickname":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // update nickname
                                    settingsNickname = args[index];
                                }
                                break;

                            case "-activitycircle":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    if (int.TryParse(args[index], NumberStyles.Number, CultureInfo.InvariantCulture, out int val))
                                    {
                                        // update activity circle
                                        settingsActivityCircle = val;
                                    }
                                }
                                break;

                            case "-follow":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    if (Int32.TryParse(args[index], NumberStyles.Number, CultureInfo.InvariantCulture, out int val))
                                    {
                                        // update follow distance
                                        Settings.Default.FollowDistance = val;
                                    }
                                }
                                break;

                            case "-atc":
                                // enable ATC mode
                                settingsAtc = true;
                                break;

                            case "-airport":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // write airport
                                    settingsAtcAirport = args[index].Substring(0, Math.Min(4, args[index].Length)).ToUpper();
                                }
                                break;

                            case "-autobroadcast":
                                // update auto broadcast
                                Settings.Default.AutoBroadcast = true;
                                break;

                            case "-lowbandwidth":
                                // update low bandwidth enabled
                                Settings.Default.LowBandwidth = true;
                                break;

                            case "-minimize":
                                // minimize
                                settingsMinimized = true;
                                break;

                            case "-global":
                                doGlobal = true;
                                break;

                            case "-whazzup":
                                settingsWhazzup = true;
                                break;

                            case "-whazzup-public":
                                settingsWhazzup = true;
                                settingsWhazzupPublic = true;
                                break;

                            case "-nosim":
                                settingsNoSim = true;
                                settingsConnectOnLaunch = false;
                                break;

                            case "-nogui":
                                settingsNoGui = true;
                                break;

                            case "-quit":
                                doQuit = true;
                                settingsNoGui = true;
                                break;

                            case "-multiobjects":
                                settingsMultiObjects = true;
                                break;

                            case "-xplane":
                                settingsXplane = true;
                                break;

                            case "-tcas":
                                settingsTcas = true;
                                break;

                            case "-simfolder":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // get sim folder
                                    simFolder = args[index];
                                }
                                break;
#if XPLANE || CONSOLE
                            case "-installplugin":
                                // do plugin
                                doPlugin = true;
                                break;
#endif

                            case "-password":
                                // next parameter
                                index++;
                                // check for parameter
                                if (index < args.Length)
                                {
                                    // get password
                                    settingsPassword = args[index];
                                }
                                break;

                            case "-scan":
                                settingsScan = true;
                                break;

#if XPLANE || CONSOLE
                            case "-generatecsl":
                                settingsGenerateCsl = true;
                                break;

                            case "-skipcsldone":
                                settingsSkipCsl = true;
                                break;
#endif

#if CONSOLE
                            case "-background":
                                settingsBackground = true;
                                break;

                            case "-help":
                                Console.WriteLine(name + " (" + version + ")");
                                Console.WriteLine("");
                                Console.WriteLine("Usage: dotnet " + name + ".dll [options]");
                                Console.WriteLine("");
                                Console.WriteLine("  --create               " + Resources.strings.Options_Create);
                                Console.WriteLine("  --join <address>       " + Resources.strings.Options_Join);
                                Console.WriteLine("  --rejoin               " + Resources.strings.Options_Rejoin);
                                Console.WriteLine("  --global               " + Resources.strings.Options_Global);
                                Console.WriteLine("  --nickname <name>      " + Resources.strings.Options_Nickname);
                                Console.WriteLine("  --port                 " + Resources.strings.Options_Port);
                                Console.WriteLine("  --hub                  " + Resources.strings.Tip_HubMode);
                                Console.WriteLine("  --hubdomain <domain>   " + Resources.strings.Tip_HubDomain);
                                Console.WriteLine("  --hubname <name>       " + Resources.strings.Tip_HubName);
                                Console.WriteLine("  --hubabout <details>   " + Resources.strings.Tip_HubAbout);
                                Console.WriteLine("  --hubvoip <details>    " + Resources.strings.Tip_HubVoice);
                                Console.WriteLine("  --hubevent <details>   " + Resources.strings.Tip_HubEvent);
                                Console.WriteLine("  --password <password>  " + Resources.strings.Tip_Password);
                                Console.WriteLine("  --play <.jfs file>     " + Resources.strings.Options_Play);
                                Console.WriteLine("  --record               " + Resources.strings.Options_Record);
                                Console.WriteLine("  --loop                 " + Resources.strings.Tip_Loop);
                                Console.WriteLine("  --activitycircle <nm>  " + Resources.strings.Options_ActivityCircle);
                                Console.WriteLine("  --follow <nm>          " + Resources.strings.Options_Follow);
                                Console.WriteLine("  --atc                  " + Resources.strings.Options_Atc);
                                Console.WriteLine("  --airport <code>       " + Resources.strings.Options_Airport);
                                Console.WriteLine("  --lowbandwidth         " + Resources.strings.Tip_LowBandwidth);
                                Console.WriteLine("  --whazzup              " + Resources.strings.Tip_Whazzup);
                                Console.WriteLine("  --whazzup-public       " + Resources.strings.Tip_WhazzupGlobal);
                                Console.WriteLine("  --minimize             " + Resources.strings.Options_Minimize);
                                Console.WriteLine("  --background           " + Resources.strings.Options_Background);
                                Console.WriteLine("  --nosim                " + Resources.strings.Options_NoSim);
                                Console.WriteLine("  --nogui                " + Resources.strings.Options_NoGui);
                                Console.WriteLine("  --multiobjects         " + Resources.strings.Tip_MultiObjects);
                                Console.WriteLine("  --simfolder \"<folder>\" " + Resources.strings.Options_SimFolder);
                                Console.WriteLine("  --scan                 " + Resources.strings.ScanForModels);
                                Console.WriteLine("  --generatecsl          " + Resources.strings.Option_GenerateCsl);
                                Console.WriteLine("  --skipcsldone          " + Resources.strings.Option_SkipCsl);
                                Console.WriteLine("  --xplane               " + Resources.strings.Tip_Xplane);
                                Console.WriteLine("  --installplugin        " + Resources.strings.Options_InstallPlugin);
                                Console.WriteLine("  --tcas                 " + Resources.strings.Tip_TCAS);
                                Console.WriteLine("  --quit                 " + Resources.strings.Options_Quit);
                                Console.WriteLine("  --help                 " + Resources.strings.Options_Help);
                                Console.WriteLine("");
                                Console.WriteLine("Interactive key commands:");
                                Console.WriteLine("");
                                Console.WriteLine("S                        Show session details");
                                Console.WriteLine("A                        Show aircraft details");
                                Console.WriteLine("Escape                   Close JoinFS");
                                Console.WriteLine("Ctrl + N                 Toggle the network connection");
                                Console.WriteLine("Ctrl + S                 Toggle the simulator connection");
                                Console.WriteLine("Ctrl + Q                 Scan for models");
                                Console.WriteLine("");
                                Console.WriteLine("");
                                Console.WriteLine("Install the X-Plane plugin:");
                                Console.WriteLine("");
                                Console.WriteLine("dotnet " + name + ".dll --installplugin --simfolder \"<folder>\"");
                                throw new Exception();
#endif
                        }
                    }
                }

                // create stopwatch
                stopwatch = Stopwatch.StartNew();



                DatabaseHelper dbHelper = new DatabaseHelper(settingsConnectionString);
                dbHelper.CleanupDatabase();

                // get all JoinFS instances
                Process[] instances = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location));
                // get this instance
                Process thisInstance = System.Diagnostics.Process.GetCurrentProcess();

                // check for quit
                if (doQuit)
                {
                    // for each instance
                    foreach (var instance in instances)
                    {
                        // check for this instance
                        if (instance.Id != thisInstance.Id)
                        {
                            // kill instance
                            instance.Kill();
                        }
                    }
                    // shutdown
                    shutdown = "";
                }

                // check for maximum instance count
                if (instances.Length > MAX_INSTANCES)
                {
                    // close
                    shutdown = Resources.strings.MaxInstances;
                }

                try
                {
                    // get storage path
                    storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), name);
                    // check if it does not exist
                    if (Directory.Exists(storagePath) == false)
                    {
                        // create storage path
                        Directory.CreateDirectory(storagePath);
                    }

                    // get documents path
                    documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), name);
                    // check if it does not exist
                    if (Directory.Exists(documentsPath) == false)
                    {
                        // create storage path
                        Directory.CreateDirectory(documentsPath);
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message);
                }

                // read guid
                try
                {
                    // check for invalid guid
                    if (Settings.Default.LastPosition == Guid.Empty)
                    {
                        // generate new one
                        Settings.Default.LastPosition = Guid.NewGuid();
                    }

                    // get guid
                    guid = Settings.Default.LastPosition;
                }
                catch
                {
                    // create new guid
                    guid = Guid.NewGuid();
                    // save guid
                    Settings.Default.LastPosition = guid;
                }

#if !NO_HUBS
                // four byte identifier
                uuid = Network.MakeUuid(guid);
#endif

                // create monitor module
                monitor = new Monitor(this);

#if FS2020
                MonitorEvent("Starting JoinFS-FS2020 v" + version);
#elif XPLANE
                MonitorEvent("Starting JoinFS-XPLANE v" + version);
#elif P3DV4
                MonitorEvent("Starting JoinFS-P3DV4 v" + version);
#elif FSX
                MonitorEvent("Starting JoinFS-FSX v" + version);
#elif SERVER
                MonitorEvent("Starting JoinFS-SERVER v" + version);
#elif CONSOLE
                MonitorEvent("Starting JoinFS-CONSOLE v" + version);
#else
                MonitorEvent("Starting JoinFS v" + version);
#endif

                // create modules
                log = new Log(this);
                sim = new Sim(this);
                network = new Network(this);
#if !SERVER
                substitution = new Substitution(this);
#endif
                recorder = new Recorder(this);
                addressBook = new AddressBook(this);
                euroscope = new Euroscope(this);
                whazzup = new Whazzup(this);
                notes = new Notes(this);
                stats = new Stats();
                variableMgr = new VariableMgr(this);

                // check for specified sim folder
                if (simFolder.Length > 0)
                {
                    // initialize sim folder
                    substitution.LoadFolders();
                    substitution.simFolder = simFolder;
                    substitution.SaveFolders();
                    Settings.Default.XPlaneFolder = simFolder;
                }

#if NO_CREATE
                // disable hub mode
                doCreate = false;
                settingsHub = false;
#endif

#if CONSOLE
                // disable nogui option
                settingsNoGui = true;
#endif

                // load airports
                LoadAirportDetails();
                // load log
                log.Load();
#if XPLANE || CONSOLE
                // check for xplane
                if (settingsXplane)
                {
                    // load substitution
                    ScheduleSubstitutionLoad();
                }
                // check for install plugin
                if (doPlugin)
                {
                    sim.xplane.InstallPlugin(Settings.Default.XPlaneFolder);
                    shutdown = "Finished plugin install.";
                }
#endif

                // port
                ushort port = settingsPortEnabled ? settingsPort : Network.DEFAULT_PORT;
                // open port
                if (network.localNode.Open(port))
                {
                    // monitor
                    MonitorEvent("Opened UDP port " + port);
                    if (settingsHub)
                    {
                        // monitor
                        MonitorEvent("Started hub '" + settingsHubName + "'");
                    }
                }

                // check for Hub mode enabled
                if (doCreate || settingsHub)
                {
                    // leave network
                    network.ScheduleLeave();
                    // create network
                    network.ScheduleCreate();
                }

                // check for join global option
                if (doGlobal || Settings.Default.Global)
                {
                    // auto join
                    network.ScheduleJoinGlobal();
                }

                // check for join option
                if (doJoin != null)
                {
                    // join network
#if NO_HUBS
                    Join(Network.DecodeIP(doJoin.TrimStart(' ').TrimEnd(' ')));
#else
                    Join(doJoin.TrimStart(' ').TrimEnd(' '));
#endif
                }

                // check for play option
                if (doPlay != "")
                {
                    // try to open file
                    try
                    {
                        StreamReader stream = new StreamReader(doPlay);
                        if (stream != null)
                        {
                            // try to open file
                            BinaryReader reader = new BinaryReader(stream.BaseStream);
                            if (reader != null)
                            {
                                // read recording
                                recorder.Read(reader);
                                // start playing
                                recorder.StartPlay();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message);
                    }
                }

                // check for record option
                if (doRecord)
                {
                    // start recording
                    recorder.StartRecord(false);
                }
                
                // monitor
                MonitorEvent("Start complete");

                // start work thread
                workThread = new Thread(new ThreadStart(DoWork));
                workThread.Start();
            }
            catch (Exception ex)
            {
                shutdown = "ERROR - " + ex.Message;
            }
        }

        public static void LaunchEncoded(string command)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(Program.Code(command, false, 1234))
            {
                UseShellExecute = true
            };
            p.Start();
        }

        public static void Launch(string command)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(command)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        /// <summary>
        /// Scheduled operations
        /// </summary>
        volatile bool scheduleSubstitutionLoad = false;
        volatile bool scheduleSubstitutionSave = false;
        volatile bool scheduleSubstitutionClear = false;
        volatile bool scheduleHeightAdjustmentLoad = false;
        volatile bool scheduleHeightAdjustmentSave = false;

        /// <summary>
        /// Schedule substitution load
        /// </summary>
        public void ScheduleSubstitutionLoad()
        {
            scheduleSubstitutionLoad = true;
        }

        /// <summary>
        /// Schedule substitution save
        /// </summary>
        public void ScheduleSubstitutionSave()
        {
            scheduleSubstitutionSave = true;
        }

        /// <summary>
        /// Schedule substitution clear
        /// </summary>
        public void ScheduleSubstitutionClear()
        {
            scheduleSubstitutionClear = true;
        }

        /// <summary>
        /// Schedule height adjustment load
        /// </summary>
        public void ScheduleHeightAdjustmentLoad()
        {
            scheduleHeightAdjustmentLoad = true;
        }

        /// <summary>
        /// Schedule height adjustment save
        /// </summary>
        public void ScheduleHeightAdjustmentSave()
        {
            scheduleHeightAdjustmentSave = true;
        }

        /// <summary>
        /// Work thread
        /// </summary>
        void DoWork()
        {
            // create stopwatch
            Stopwatch sw = Stopwatch.StartNew();

            while (workFinish == false)
            {
                // get start time
                long start = sw.ElapsedMilliseconds;

                lock (conch)
                {
                    sim ?. DoWork();
                    network.DoWork();
                    recorder.DoWork();
                    euroscope.DoWork();
                    whazzup.DoWork();
                    notes.DoWork();

                    // check for scheduled model match clear
                    if (scheduleSubstitutionClear)
                    {
                        // clear model matching
                        substitution ?. Clear();
                        // reset
                        scheduleSubstitutionClear = false;
                    }

                    // check for scheduled model match load
                    if (scheduleSubstitutionLoad)
                    {
                        // load model matching
                        substitution ?. Load();
                        // reset
                        scheduleSubstitutionLoad = false;
                    }

                    // check for scheduled model match save
                    if (scheduleSubstitutionSave)
                    {
                        // save model matching
                        substitution ?. Save();
                        // reset
                        scheduleSubstitutionSave = false;
                    }

                    // check for scheduled height adjustment load
                    if (scheduleHeightAdjustmentLoad)
                    {
                        // load
                        sim ?. LoadHeightAdjustments();
                        // reset
                        scheduleHeightAdjustmentLoad = false;
                    }

                    // check for scheduled model match save
                    if (scheduleHeightAdjustmentSave)
                    {
                        // save
                        sim ?. SaveHeightAdjustments();
                        // reset
                        scheduleHeightAdjustmentSave = false;
                    }
                }

                // get duration of work
                long duration = sw.ElapsedMilliseconds - start;
                // calculate sleep time
                long sleep = 5 - duration;

                // check for sleep
                if (sleep > 0)
                {
                    Thread.Sleep((int)sleep);
                }
            }
        }

        /// <summary>
        /// log an event
        /// </summary>
        /// <param name="s"></param>
        public void MonitorEvent(string s)
        {
            if (monitor != null)
            {
                lock (conch)
                {
                    monitor.Write(s);
                }
            }
        }

        /// <summary>
        /// log an event
        /// </summary>
        /// <param name="s"></param>
        public void MonitorNetwork(string s)
        {
            if (monitor != null && monitor.network)
            {
                lock (conch)
                {
                    monitor.Write(s);
                }
            }
        }

        /// <summary>
        /// log an event
        /// </summary>
        /// <param name="s"></param>
        public void MonitorVariables(string s)
        {
            if (monitor != null && monitor.variables)
            {
                lock (conch)
                {
                    monitor.Write(s);
                }
            }
        }

        /// <summary>
        /// Show UI messages
        /// </summary>
        public volatile string scheduleShowMessage = null;
        public volatile bool scheduleAskSimConnect = false;
        public volatile bool scheduleNickname = false;
        public volatile bool scheduleLogin = false;
        public volatile bool scheduleFlightPlan = false;
        public volatile bool scheduleScanForModels = false;
        public volatile bool scheduleAskPlugin = false;

        /// <summary>
        /// Show message to the user
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="title">Title</param>
        public void ShowMessage(string message)
        {
            // monitor event
            MonitorEvent(message);
            // check if not showing message
            if (scheduleShowMessage == null)
            {
                // schedule message
                scheduleShowMessage = message;
            }
        }

#region Airport

        /// <summary>
        /// Airport details
        /// </summary>
        public struct Airport
        {
            /// <summary>
            /// Latitude
            /// </summary>
            public float latitude;
            /// <summary>
            /// Longitude
            /// </summary>
            public float longitude;
            /// <summary>
            /// Elevation
            /// </summary>
            public short elevation;
        }

        /// <summary>
        /// List of airports
        /// </summary>
        public Dictionary<string, Airport> airportList = new Dictionary<string, Airport>();

        /// <summary>
        /// Get details of airports
        /// </summary>
        /// <param name="code">ICAO code</param>
        public void LoadAirportDetails()
        {
            try
            {
                // get resource
                using (Stream data = new MemoryStream(Properties.Resources.Airports))
                {
                    // open reader
                    BinaryReader reader = new BinaryReader(data);
                    while (reader.PeekChar() != -1)
                    {
                        // read code
                        string code = reader.ReadString();
                        // new airport
                        Airport airport = new Airport { };
                        // read details
                        airport.latitude = reader.ReadSingle();
                        airport.longitude = reader.ReadSingle();
                        airport.elevation = reader.ReadInt16();
                        // check if airport entry exists
                        if (airportList.ContainsKey(code) == false)
                        {
                            // add airport to list
                            airportList.Add(code, airport);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);
            }
        }

#endregion

#region Forms


#if !CONSOLE
        /// <summary>
        /// global font
        /// </summary>
        public Font dataFont = new Font("Verdana", 9.0f);

        /// <summary>
        /// application icon
        /// </summary>
        public Icon icon = null;

        /// <summary>
        /// Forms
        /// </summary>
        public MainForm mainForm = null;
        public HubsForm hubsForm = null;
        public AddressBookForm addressBookForm = null;
#if !SERVER
        public AtcForm atcForm = null;
        public AircraftForm aircraftForm = null;
        public ObjectsForm objectsForm = null;
        public MatchingForm matchingForm = null;
#endif
        public SessionForm sessionForm = null;
        public RecorderForm recorderForm = null;
        public MonitorForm monitorForm = null;
        public ShortcutsForm shortcutsForm = null;
        public OptionsForm optionsForm = null;

        /// <summary>
        /// Open all forms
        /// </summary>
        public void OpenForms()
        {
            try
            {
                // load application icon
                icon = Properties.Resources.App;
            }
            catch (Exception ex)
            {
                ShowMessage("ERROR - Failed to load application icon - " + ex.Message);
            }

#if EVAL
            // show message
            MessageBox.Show("For evaluation purposes only", Application.ProductName);
#endif

            // check for first run
            if (Settings.Default.FirstRun)
            {
                // no longer first run
                Settings.Default.FirstRun = false;

                // check for no simulator option
                if (settingsNoSim == false)
                {
#if XPLANE
                    // install plugin
                    scheduleAskPlugin = true;
#endif
                    // install SimConnect
                    //                    scheduleAskSimConnect = true;
                }
            }

            // create forms
            addressBookForm = new AddressBookForm(this);
#if !SERVER
            aircraftForm = new AircraftForm(this);
            objectsForm = new ObjectsForm(this);
            atcForm = new AtcForm(this);
            matchingForm = new MatchingForm(this);
#endif
            recorderForm = new RecorderForm(this);
            sessionForm = new SessionForm(this);
            monitorForm = new MonitorForm(this);
            shortcutsForm = new ShortcutsForm(this);
            optionsForm = new OptionsForm(this);
#if !NO_HUBS
            hubsForm = new HubsForm(this);
#endif
            mainForm = new MainForm(this);

            mainForm.Show();

#if !NO_HUBS
            // hide hubs initially
            if (Settings.Default.HubsFormOpen)
            {
                hubsForm.Show();
            }
            else
            {
                hubsForm.Hide();
            }
#endif
            if (Settings.Default.BookmarksFormOpen)
            {
                addressBookForm.Show();
            }
            else
            {
                addressBookForm.Hide();
            }
#if !SERVER
            if (Settings.Default.AircraftFormOpen)
            {
                aircraftForm.Show();
            }
            else
            {
                aircraftForm.Hide();
            }
            if (Settings.Default.ObjectsFormOpen)
            {
                objectsForm.Show();
            }
            else
            {
                objectsForm.Hide();
            }
            if (Settings.Default.AtcFormOpen)
            {
                atcForm.Show();
            }
            else
            {
                atcForm.Hide();
            }
            if (Settings.Default.MatchingFormOpen)
            {
                matchingForm.Show();
            }
            else
            {
                matchingForm.Hide();
            }
#endif
            if (Settings.Default.RecorderFormOpen)
            {
                recorderForm.Show();
            }
            else
            {
                recorderForm.Hide();
            }
            if (Settings.Default.SessionFormOpen)
            {
                sessionForm.Show();
            }
            else
            {
                sessionForm.Hide();
            }
            if (Settings.Default.MonitorFormOpen)
            {
                monitorForm.Show();
            }
            else
            {
                monitorForm.Hide();
            }
            if (Settings.Default.ShortcutsFormOpen)
            {
                shortcutsForm.Show();
            }
            else
            {
                shortcutsForm.Hide();
            }
            if (Settings.Default.OptionsFormOpen)
            {
                optionsForm.Show();
            }
            else
            {
                optionsForm.Hide();
            }
        }

#endif

#endregion

        /// <summary>
        /// Already attempted a used password
        /// </summary>
        public bool attempedUsedPassword = false;

        /// <summary>
        /// Join by user id
        /// </summary>
        /// <param name="uuid"></param>
        public void Join(uint uuid)
        {
            // leave existing network
            network.ScheduleLeave();
            // reset attempted used password
            attempedUsedPassword = false;
            // send out a request for the end point
            network.ScheduleJoinUser(uuid);
#if !CONSOLE
            // update window
            mainForm ?. RefreshNetwork();
#endif
        }

        /// <summary>
        /// Join by end point
        /// </summary>
        /// <param name="endPoint"></param>
        public void Join(IPEndPoint endPoint)
        {
            // leave existing network
            network.ScheduleLeave();
            // reset attempted used password
            attempedUsedPassword = false;
            // join session
            network.ScheduleJoin(endPoint, 0);
#if !CONSOLE
            // update window
            mainForm ?. RefreshNetwork();
#endif
        }

        /// <summary>
        /// Join by text
        /// </summary>
        /// <param name="addressText"></param>
        public void Join(string addressText)
        {
#if !CONSOLE
            // refresh join box
            if (mainForm != null)
            {
                mainForm.refreshJoinCombo = true;
            }
#endif

            // check for hub mode
            if (settingsHub)
            {
                // check for valid text
                if (addressText.Length > 0)
                {
                    lock (conch)
                    {
                        // convert address to end point
                        if (network.MakeEndPoint(addressText, Network.DEFAULT_PORT, out IPEndPoint endPoint))
                        {
                            // submit hub
                            network.ScheduleSubmitHub(endPoint);
                        }
                    }
                }
                // done
                return;
            }

#if !NO_GLOBAL
            // check for 
            if (addressText.Equals(Resources.strings.Global))
            {
                // leave existing network
                network.ScheduleLeave();
                // schedule join
                network.ScheduleJoinGlobal();
#if !CONSOLE
                // update window
                mainForm ?. RefreshNetwork();
#endif
                // finished
                return;
            }
#endif

            // check for uuid
            uint uuid = Network.MakeUuid(addressText);
            // check for valid uuid
            if (uuid != 0)
            {
                // join uuid
                Join(uuid);
                // done
                return;
            }

            // check for hub
            Network.Hub hub;
            // find address in the address book
            hub = network.hubList.Find(h => h.name.Equals(addressText));
            // if hub found
            if (hub != null)
            {
                // join endpoint
                Join(hub.endPoint);
                // done
                return;
            }

            // check for entry
            AddressBook.AddressBookEntry entry;
            // find entry
            entry = addressBook.entries.Find(f => f.name.Equals(addressText));
            // if entry found
            if (entry != null)
            {
                // check for valid uuid
                if (entry.uuid != 0)
                {
                    // join uuid
                    Join(entry.uuid);
                    // done
                    return;
                }

                // join endpoint
                Join(entry.endPoint);
                // done
                return;
            }

            // parse ip address
            string[] parts = addressText.Split(':');

            // check for ip address
            if (parts.Length > 0)
            {
                // get remote port
                int remotePort = Network.DEFAULT_PORT;
                if (parts.Length > 1 && Int32.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out remotePort) == false)
                {
                    ShowMessage(Resources.strings.InvalidPortPrefix + " '" + parts[1] + "'. " + Resources.strings.InvalidPortSuffix);
                }
                else
                {
                    if (IPAddress.TryParse(parts[0], out IPAddress address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        // join endpoint
                        Join(new IPEndPoint(address, remotePort));
                        // done
                        return;
                    }

                    bool result = false;
                    lock (conch)
                    {
                        result = network.DnsLookup(parts[0], out address);
                    }
                    // try DNS lookup
                    if (result)
                    {
                        // join endpoint
                        Join(new IPEndPoint(address, remotePort));
                        // done
                        return;
                    }
                }
            }

            // failed
            ShowMessage(Resources.strings.InvalidJoinAddress);
        }

        public void ToggleSimulator()
        {
            // check for sim
            if (sim != null)
            {
                lock (conch)
                {
                    // check if simulator connected
                    if (sim.Connected || sim.Connecting)
                    {
                        sim.Close();
                    }
                    else
                    {
                        sim.Connect();
                    }
                }
            }
        }

        public void ToggleNetwork()
        {
            lock (conch)
            {
                // get connected state
                bool connected = network.localNode.CurrentState != LocalNode.State.Unconnected;

                // check if user join scheduled
                if (network.scheduleJoinUser)
                {
                    // stop join
                    network.scheduleJoinUser = false;
                }
                else if (connected)
                {
                    // leave network
                    network.ScheduleLeave();
                }
                else
                {
                    // leave network
                    network.ScheduleLeave();
                    // create network
                    network.ScheduleCreate();
                }
            }
        }

        public void MonitorSessionDetails()
        {
            // check if connected
            if (network.localNode.Connected)
            {
                MonitorEvent("Session:");
                MonitorEvent("  ADDRESS NICKNAME CALLSIGN CONNECTED LATENCY AIRCRAFT OBJECTS VERSION SIMULATOR");
                string line = " ";
                line += " " + network.localNode.GetLocalNuid();
                line += " " + settingsNickname;
                line += " " + network.GetLocalCallsign();
                line += " " + network.localNode.Connected;
                line += " " + 0.0f;
                line += " " + sim.objectList.FindAll(o => o is Sim.Aircraft && sim.IsBroadcast(o)).Count;
                line += " " + sim.objectList.FindAll(o => (o is Sim.Aircraft) == false && o.owner == Sim.Obj.Owner.Sim).Count;
                line += " " + version;
                line += " " + (sim != null ? sim.GetSimulatorName() : "");
                MonitorEvent(line);
                // for each node
                foreach (var node in network.nodeList)
                {
                    line = " ";
                    line += " " + node.Key;
                    line += " " + node.Value.nickname;
                    line += " " + network.GetNodeCallsign(node.Key);
                    line += " " + network.localNode.NodeReceiveEstablished(node.Key);
                    line += " " + network.localNode.GetNodeRTT(node.Key);
                    line += " " + sim.objectList.FindAll(o => o.ownerNuid == node.Key && o is Sim.Aircraft).Count;
                    line += " " + sim.objectList.FindAll(o => o.ownerNuid == node.Key && (o is Sim.Aircraft) == false).Count;
                    line += " " + network.GetNodeVersion(node.Key);
                    line += " " + network.GetNodeSimulator(node.Key);
                    MonitorEvent(line);
                }
                MonitorEvent("Total " + (1 + network.nodeList.Count) + " user(s)");
            }
            else
            {
                MonitorEvent("Session: " + Resources.strings.NotConnected);
            }
        }

        public void MonitorAircraft(Sim.Aircraft aircraft)
        {
            // get user position
            Sim.Pos userPosition = sim ?. userAircraft ?. Position;
            // get aircraft position
            Sim.Pos aircraftPosition = aircraft.Position;
            string distance = "-";
            string heading = "-";
            string altitude = "-";
            string speed = "-";
            // check for user aircraft
            if (userPosition != null && aircraftPosition != null)
            {
                // get distance to aircraft
                double d = Vector.GeodesicDistance(aircraftPosition.geo.x, aircraftPosition.geo.z, userPosition.geo.x, userPosition.geo.z);
                // convert to nautical miles
                distance = (d * 0.00053995680346).ToString("N2");
            }

            // check for valid aircraft position
            if (aircraftPosition != null)
            {
                // set heading
                heading = ((int)(aircraftPosition.angles.y * 180.0 / Math.PI)).ToString("D3");
                // set altitude
                altitude = (aircraftPosition.geo.y * Sim.FEET_PER_METRE).ToString("N0");
            }

            // check for mach approach
            speed = (Math.Sqrt(aircraft.netVelocity.linear.x * aircraft.netVelocity.linear.x + aircraft.netVelocity.linear.z * aircraft.netVelocity.linear.z) * 1.9438444925).ToString("N0");

            string line = " ";
            line += " " + aircraft.flightPlan.callsign;
            line += " " + network.GetNodeName(aircraft.ownerNuid);
            line += " " + distance + "nm";
            line += " " + heading;
            line += " " + altitude + "ft";
            line += " " + speed + "kt";
            line += " " + aircraft.ModelTitle;
            line += " " + sim.IsBroadcast(aircraft);
            MonitorEvent(line);
        }

        public void MonitorAircraftDetails()
        {
            // check if connected
            if (sim != null && sim.Connected)
            {
                MonitorEvent("Aircraft:");
                MonitorEvent("  CALLSIGN OWNER DISTANCE HEADING ALTITUDE SPEED SUBMODEL BROADCAST");

                // total aircraft
                int total = 0;
                // add user aircraft
                foreach (var obj in sim.objectList)
                {
                    if (obj.owner == Sim.Obj.Owner.Me)
                    {
                        MonitorAircraft(obj as Sim.Aircraft);
                        total++;
                    }
                }

                // add network aircraft
                foreach (var obj in sim.objectList)
                {
                    if (obj is Sim.Aircraft && obj.owner == Sim.Obj.Owner.Network)
                    {
                        // check for show ignored aircraft
                        if (Settings.Default.IncludeIgnoredAircraft || log.IgnoreNode(obj.ownerNuid) == false)
                        {
                            MonitorAircraft(obj as Sim.Aircraft);
                            total++;
                        }
                    }
                }

                // add recorder aircraft
                foreach (var obj in sim.objectList)
                {
                    if (obj is Sim.Aircraft && obj.owner == Sim.Obj.Owner.Recorder)
                    {
                        MonitorAircraft(obj as Sim.Aircraft);
                        total++;
                    }
                }

                // check for simulator aircraft
                if (Settings.Default.IncludeSimulatorAircraft)
                {
                    // add any other aircraft
                    foreach (var obj in sim.objectList)
                    {
                        if (obj is Sim.Aircraft && obj.owner == Sim.Obj.Owner.Sim)
                        {
                            // check for show ignored aircraft
                            if (Settings.Default.IncludeIgnoredAircraft || log.IgnoreName((obj as Sim.Aircraft).flightPlan.callsign) == false)
                            {
                                MonitorAircraft(obj as Sim.Aircraft);
                                total++;
                            }
                        }
                    }
                }

                MonitorEvent("Total " + total + " aircraft");
            }
            else
            {
                MonitorEvent("Aircraft: " + Resources.strings.NotConnected);
            }
        }
    }

    static class Program
    {
        public static string Code(string s, bool bToCode, int nKey)
        {
            if (s == null)
                return null;

            const char cLow = '!';
            const char cHigh = '~';

            const int nRange = (int)cHigh - cLow + 1;
            const string csCaesar = @"THEFIVBOXNGWZARDSJUMPQCKLY" + // Low to High, rearranged (will be nRange in length)
                                    @"mywaftvexdzoquipsblchngjrk" +
                                    @"1407329685" +
                                    @"!""#$%&'()*+,-./:;<=>?@" +
                                    @"[\]^_`{|}~";

            if (s.Length == 0)
                return s;

            StringBuilder sb = new StringBuilder(s);

            // find last valid char & trim ignored ones
            int nLast = sb.Length - 1;
            int nEnd = nLast;
            while (nEnd >= 0 && (sb[nEnd] < cLow || sb[nEnd] > cHigh))
                --nEnd;
            if (nEnd != nLast)
                sb.Remove(nEnd + 1, sb.Length - nEnd - 1);

            // find first valid char & trim ignored ones
            nLast = sb.Length - 1;
            int nStart = 0;
            while (nStart <= nLast && (sb[nStart] < cLow || sb[nStart] > cHigh))
                ++nStart;
            if (nStart != 0)
                sb.Remove(0, nStart);

            if (sb.Length == 0)
                return sb.ToString();

            nLast = sb.Length - 1;

            // if decoding, reverse-substitute
            int nChar;
            if (!bToCode)
            {
                for (int i = 0; i <= nLast; ++i)
                {
                    if (sb[i] < cLow || sb[i] > cHigh)
                        continue;

                    nChar = csCaesar.IndexOf(sb[i]);

                    if (nChar >= 0)
                        sb[i] = (char)((int)cLow + nChar);
                }
            }

            // mangle
            int nInc = 0;
            int k;
            const int nPasses = 11;
            Random rnd = new Random(nKey);
            for (int j = 0; j < nPasses; ++j)
            {
                k = (bToCode) ? j : nPasses - j - 1;
                if ((k & 1) == 0)
                { nStart = nLast; nEnd = -1; nInc = -1; }   // from last downto first
                else
                { nStart = 0; nEnd = nLast + 1; nInc = 1; }   // from first upto last

                for (int i = nStart; i != nEnd; i += nInc)
                {
                    if (sb[i] < cLow || sb[i] > cHigh)
                        continue;

                    nChar = sb[i] - cLow;

                    if (bToCode)
                        nChar = nRange - 1 - nChar;

                    if (bToCode)
                        nChar += rnd.Next(nRange - 1);
                    else
                        nChar -= rnd.Next(nRange - 1);

                    if (nChar >= nRange)
                        nChar -= nRange;
                    else if (nChar < 0)
                        nChar += nRange;

                    if (!bToCode)
                        nChar = nRange - 1 - nChar;

                    sb[i] = (char)(nChar + cLow);
                }
            }

            // if encoding, substitute
            if (bToCode)
            {
                for (int i = 0; i <= nLast; ++i)
                {
                    if (sb[i] < cLow || sb[i] > cHigh)
                        continue;

                    nChar = sb[i] - cLow;

                    sb[i] = csCaesar[nChar];
                }
            }

            return sb.ToString();
        }

        //public class CookieAwareWebClient : WebClient
        //{
        //    private CookieContainer cookie = new CookieContainer();

        //    protected override WebRequest GetWebRequest(Uri address)
        //    {
        //        WebRequest request = base.GetWebRequest(address);
        //        if (request is HttpWebRequest)
        //        {
        //            (request as HttpWebRequest).CookieContainer = cookie;
        //        }
        //        return request;
        //    }
        //}

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Main main = new Main();

            // check for gui
            if (main.settingsNoGui)
            {
                // continue until shutdown
                while (main.shutdown == null)
                {
#if CONSOLE
                    // check if background is not set
                    if (main.settingsBackground == false)
                    {
                        ConsoleKeyInfo info = Console.ReadKey(true);
                        if (info.Modifiers == 0 && info.Key == ConsoleKey.Escape) main.shutdown = "Shutting Down...";
                        else if (info.Modifiers == 0 && info.Key == ConsoleKey.S) main.MonitorSessionDetails();
                        else if (info.Modifiers == 0 && info.Key == ConsoleKey.A) main.MonitorAircraftDetails();
                        else if (info.Modifiers == ConsoleModifiers.Control && info.Key == ConsoleKey.N) main.ToggleNetwork();
                        else if (info.Modifiers == ConsoleModifiers.Control && info.Key == ConsoleKey.S) main.ToggleSimulator();
                        else if (info.Modifiers == ConsoleModifiers.Control && info.Key == ConsoleKey.Q) main.substitution ?. Scan(true);
                    }
#else
                    // sleep
                    Thread.Sleep(100);
#endif
                }
            }
#if !CONSOLE
            else if (main.shutdown == null)
            {
                // launch gui
                Application.EnableVisualStyles();
#if NET6_0_OR_GREATER
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
                Application.SetCompatibleTextRenderingDefault(false);
                main.OpenForms();
                Application.Run(main.mainForm);
            }
#endif

            // check for message
            if (main.shutdown != null && main.shutdown.Length > 0)
            {
                // monitor
                main.MonitorEvent(main.shutdown);
            }

            main.Close();
        }
    }
}
