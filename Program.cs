using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using iTunesLib;
using Discord;
using System.Timers;
using System.Windows.Forms;
using System.Drawing;

namespace AppleMusicRichPresence
{

    class Program
    {

        private static NotifyIcon NotifyIcon;
        //
        //
        private static long APPLICATION_ID { get; } = 804191030811033651;
        public static string LARGE_IMAGE { get; } = "icon";

        //
        //
        public static Discord.Discord Discord { get; }
            = new Discord.Discord(APPLICATION_ID, (UInt64)CreateFlags.Default);

        //Yeah I called it Apple Music
        public static iTunesApp AppleMusic { get; }
            = new iTunesApp();
        public static AppleMusicActivityManager AppleMusicActivityManager { get; }
            = new AppleMusicActivityManager();

        private static bool active = true;


        static void FormStart() {

            ContextMenuStrip Menu = new ContextMenuStrip();
            Menu.Items.Add("Made by AM.K3Y :D").Enabled = false;
            Menu.Items.Add("Exit", null, OnExit);

            NotifyIcon = new NotifyIcon();
            NotifyIcon.Icon = Properties.Resources.icon;
            NotifyIcon.ContextMenuStrip = Menu;
            NotifyIcon.Text = "Apple Music Rich Presence";
            NotifyIcon.Visible = true;

            Application.Run();
        }
        static void OnExit(object sender, EventArgs e) {
            Application.Exit();
            AppleMusic.OnPlayerPlayEvent -= AppleMusic_OnPlayerPlayEvent;
            AppleMusic.OnPlayerStopEvent -= AppleMusic_OnPlayerStopEvent;
            System.Runtime.InteropServices.Marshal.ReleaseComObject(AppleMusic);
            Discord.GetActivityManager().ClearActivity(Result=> { });
            active = false;
        }

        static void Main(string[] args)
        {
            
            Thread UIThread = new Thread(FormStart);
            UIThread.Start();

            switch (AppleMusic.PlayerState) {
                case ITPlayerState.ITPlayerStatePlaying :
                    AppleMusicActivityManager.SetPlayActivity();
                    break;
                case ITPlayerState.ITPlayerStateStopped :
                    AppleMusicActivityManager.SetPauseActivity();
                    break;
                default:
                    break;
            }

            AppleMusic.OnPlayerPlayEvent += AppleMusic_OnPlayerPlayEvent;
            AppleMusic.OnPlayerStopEvent += AppleMusic_OnPlayerStopEvent;

            // System.Environment.SetEnvironmentVariable("DISCORD_INSTANCE_ID", "0");

            while (active) {
                Discord.RunCallbacks();
                Thread.Sleep(100);
            }
        }

        private static void AppleMusic_OnPlayerStopEvent(object iTrack)
        {
            Console.WriteLine("> Track Stopped...");
            AppleMusicActivityManager.SetPauseActivity();
        }

        private static void AppleMusic_OnPlayerPlayEvent(object iTrack)
        {
            Console.WriteLine("> Track Started...");
            AppleMusicActivityManager.SetPlayActivity();
        }
    }

    class AppleMusicActivityManager {

        private bool paused = false;
        private System.Timers.Timer idleTimer = new System.Timers.Timer(20000);

        public AppleMusicActivityManager() {
            idleTimer.AutoReset = false;
            idleTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                if (paused)
                {
                    Program.Discord.GetActivityManager().ClearActivity((Result) => {
                        Console.WriteLine("");
                    });
                 /*   Program.Discord.GetActivityManager().UpdateActivity(emptyActivity, Result =>
                    {
                        Console.WriteLine("Updated activity with result " + Result);
                    });
                 */
                }
            };
        }

        private Activity emptyActivity = new Activity {
            Type = ActivityType.Listening,
            //Details = "Idling...",
            Assets = new ActivityAssets
            {
                LargeImage = Program.LARGE_IMAGE,
                LargeText = "Apple Music"
            },
        };

        private Activity MakeTrackActivity(bool showTimeStamp)
        {
            IITTrack track = Program.AppleMusic.CurrentTrack;
            if (track == null) return emptyActivity;

            Console.WriteLine("Activity Detail ---------");
            Console.WriteLine("Name： " + track.Name);
            Console.WriteLine("Artist： " + track.Artist);
            Console.WriteLine("Duration： " + track.Duration);
            Console.WriteLine("End Time：" + DateTimeOffset.Now.ToUnixTimeMilliseconds()
                            + (long)track.Duration * 1000L);
            Console.WriteLine("--------------------------");

            //for utf8 strings, The type of fields "detail" and "state" in Activity has been change to byte[] 
            byte[] detail = Encoding.UTF8.GetBytes(track.Artist);
            //To meet the packet requirement, this will fill the rest empty parts
            detail = Enumerable.Concat(detail, Enumerable.Repeat<byte>(0, 128 - detail.Length)).ToArray();
            byte[] state = Encoding.UTF8.GetBytes(track.Name);
            state = Enumerable.Concat(state, Enumerable.Repeat<byte>(0, 128 - state.Length)).ToArray();

            // fuck u discord'



            return new Activity
            {

                Type = ActivityType.Listening,
                Details = detail,
                State = state,
                Assets = new ActivityAssets
                {
                    LargeImage = Program.LARGE_IMAGE,
                    LargeText = "Apple Music"
                },
                Timestamps =
                {
                    End = showTimeStamp ?
                   DateTimeOffset.Now.ToUnixTimeMilliseconds()
                            + (long)track.Duration * 1000L - Program.AppleMusic.PlayerPosition * 1000L
                            : 0L
                },
                Secrets =
                { 
                    Join = "secret1",
                    Spectate = "secret2",
                    Match = "secret3"
                },
                Instance = true,
            };
        }

        public void SetPauseActivity() {

            Program.Discord.GetActivityManager().UpdateActivity(MakeTrackActivity(false), Result => {
                Console.WriteLine("Updated activity with result " + Result);
            });
            paused = true;
            idleTimer.Start();
        }

        public void SetPlayActivity() {
            Program.Discord.GetActivityManager().UpdateActivity(MakeTrackActivity(true), Result => {
                Console.WriteLine("Updated activity with result " + Result);
            });
            paused = false;
            idleTimer.Stop();
        }
    }
}