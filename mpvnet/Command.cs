﻿/**
 *mpv.net
 *Copyright(C) 2017 stax76
 *
 *This program is free software: you can redistribute it and/or modify
 *it under the terms of the GNU General Public License as published by
 *the Free Software Foundation, either version 3 of the License, or
 *(at your option) any later version.
 *
 *This program is distributed in the hope that it will be useful,
 *but WITHOUT ANY WARRANTY; without even the implied warranty of
 *MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 *GNU General Public License for more details.
 *
 *You should have received a copy of the GNU General Public License
 *along with this program. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using vbnet;
using System.Drawing;
using static vbnet.UI.MainModule;

namespace mpvnet
{
    public class Command
    {
        public string Name { get; set; }
        public Action<string[]> Action { get; set; }

        private static List<Command> commands;

        public static List<Command> Commands
        {
            get
            {
                if (commands == null)
                {
                    commands = new List<Command>();
                    var type = typeof(Command);
                    var methods = type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                    foreach (var i in methods)
                    {
                        var parameters = i.GetParameters();

                        if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != typeof(string[]))
                            continue;

                        var cmd = new Command() { Name = i.Name.Replace("_","-"), Action = (Action<string[]>)i.CreateDelegate(typeof(Action<string[]>)) };
                        commands.Add(cmd);
                    }
                }

                return commands;
            }
        }

        public static void open_files(string[] args)
        {
            MainForm.Instance.Invoke(new Action(() => {
                using (var d = new OpenFileDialog())
                {
                    d.Multiselect = true;
                    d.Filter = Misc.GetFilter(Misc.FileTypes);

                    if (d.ShowDialog() == DialogResult.OK)
                        mpv.LoadFiles(d.FileNames);
                }
            }));
        }

        public static void open_config_folder(string[] args)
        {
            ProcessHelp.Start(Folder.AppDataRoaming + "mpv");
        }

        public static void show_keys(string[] args)
        {
            ProcessHelp.Start(OS.GetTextEditor(), '"' + mpv.InputConfPath + '"');
        }

        public static void show_prefs(string[] args)
        {
            string filepath = Folder.AppDataRoaming + "mpv\\mpv.conf";

            if (!File.Exists(filepath))
            {
                var dirPath = Folder.AppDataRoaming + "mpv\\";

                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                File.WriteAllText(filepath, "# https://mpv.io/manual/master/#configuration-files");
            }

            ProcessHelp.Start(OS.GetTextEditor(), '"' + filepath + '"');
        }

        public static void show_info(string[] args)
        {
            try
            {
                var fi = new FileInfo(mpv.GetStringProp("path"));

                using (var mi = new MediaInfo(fi.FullName))
                {
                    var w = mi.GetInfo(StreamKind.Video, "Width");
                    var h = mi.GetInfo(StreamKind.Video, "Height");
                    var pos = TimeSpan.FromSeconds(mpv.GetIntProp("time-pos"));
                    var dur = TimeSpan.FromSeconds(mpv.GetIntProp("duration"));
                    var br = Convert.ToInt32(mi.GetInfo(StreamKind.Video, "BitRate")) / 1000;
                    var vf = mpv.GetStringProp("video-format").ToUpper();
                    var fn = fi.Name;

                    if (fn.Length > 60)
                        fn = fn.Insert(59, BR);

                    var info =
                        FormatTime(pos.TotalMinutes) + ":" +
                        FormatTime(pos.Seconds) + " / " +
                        FormatTime(dur.TotalMinutes) + ":" +
                        FormatTime(dur.Seconds) + "\n" +
                        ((int)(fi.Length / 1024 / 1024)).ToString() +
                        $" MB - {w} x {h}\n{vf} - {br} Kbps" + "\n" + fn;

                    mpv.Command("show-text", info, "5000");

                    string FormatTime(double value) => ((int)(Math.Floor(value))).ToString("00");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}