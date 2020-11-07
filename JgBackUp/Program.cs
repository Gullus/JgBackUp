using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace JgBackUp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var pause = false;

            if (args.Length > 0)
            {
                if (args[0].Contains("?"))
                {
                    Console.WriteLine("p    Pause nach der Ausführung");
                    Environment.Exit(0);
                }
                else if (args[0].Contains("p"))
                    pause = true;
            }

            var pfad = AppDomain.CurrentDomain.BaseDirectory;
            var pfadDaten = pfad + "Daten";
            var dateiDb = pfadDaten + @"\JgBackUp.bak";
            var tagAkt = DateTime.Now.DayOfWeek.ToString().Substring(0, 2);

            if (!Directory.Exists(pfadDaten))
            {
                Helper.Ausgabe(ConsoleColor.Yellow, "Erstelle Directory " + pfadDaten);
                try
                {
                    Directory.CreateDirectory(pfadDaten);
                }
                catch (Exception ex)
                {
                    Helper.Ausgabe(ConsoleColor.Red, "Fehler beim erstellen " + dateiDb, ex);
                    Environment.Exit(1);
                }
            }

            Helper.Ausgabe(ConsoleColor.White, "Optionen laden");

            var tagAlt = "";
            var dateiTag = pfadDaten + @"\MerkeTag.bin";
            if (File.Exists(dateiTag))
                tagAlt = File.ReadAllText(dateiTag);
            if (tagAlt != tagAkt)
            {
                File.WriteAllText(dateiTag, tagAkt);
                if (File.Exists(dateiDb))
                {
                    try
                    {
                        File.Delete(dateiDb);
                    }
                    catch (Exception ex)
                    {
                        Helper.Ausgabe(ConsoleColor.Red, "Fehler Löschen von " + dateiDb, ex);
                        Environment.Exit(1);
                    }
                }
            }

            var dateiOpt = pfad + "opt.txt";
            if (!File.Exists(dateiOpt))
            {
                Helper.Ausgabe(ConsoleColor.Red, dateiOpt + " wurde nicht gefunden.");
                Environment.Exit(1);
            }
            var optionen = await File.ReadAllLinesAsync(pfad + "opt.txt");
            var opt = new Dictionary<string, string>();

            foreach (var zeile in optionen)
            {
                var feld = zeile.Split(new char[] { ':' });
                if (feld.Length > 1)
                    opt.Add(feld[0].Trim().ToLower(), feld[1].Trim());
            }

            Helper.Ausgabe(ConsoleColor.Yellow, "Beginne DB Update");

            try
            {
                Helper.Backup(opt["database"], opt["sqlverbindung"], dateiDb);
            }
            catch (Exception ex)
            {
                Helper.Ausgabe(ConsoleColor.Red, "Fehler bei DB Backup. Grund: ", ex);
                Environment.Exit(1);
            }

            Helper.Ausgabe(ConsoleColor.White, "Datei kopieren");

            try
            {
                File.Copy(dateiDb, $"{pfadDaten}\\{opt["database"]}_{tagAkt}", true);
            }
            catch (Exception ex)
            {
                Helper.Ausgabe(ConsoleColor.Red, "Fehler beim kopieren. Grund: ", ex);
                Environment.Exit(1);
            }

            Helper.Ausgabe(ConsoleColor.White, "Upload auf FTP Server");

            var timer = new Timer(5000);
            timer.Elapsed += (sender, e) => Console.Write(".");
            timer.Start();

            try
            {
                using (var client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(opt["ftpusername"], opt["ftpkennwort"]);
                    client.UploadFile($"ftp://{opt["ftphost"]}/{opt["database"]}.bak", WebRequestMethods.Ftp.UploadFile, dateiDb);
                }
            }
            catch (Exception ex)
            {
                Helper.Ausgabe(ConsoleColor.Red, "Fehler beim Upload auf FTP Server. " + ex.Message);
            }

            timer.Stop();
            Console.WriteLine("transfare abgeschlossen");

            Helper.Ausgabe(ConsoleColor.White, "Sicherung erfolgreich abgeschlossen.");

            if (pause)
            {
                Console.WriteLine();
                Console.WriteLine("Beenden mit Enter");
                Console.ReadLine();
            }
        }
    }
}
