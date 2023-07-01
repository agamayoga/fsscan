/******************************************************************************************
 * The MIT License (MIT)
 *
 * Copyright (c) 2020 Agama Yoga
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ******************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

[assembly: AssemblyTitle("FileSystemScan")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Agama")]
[assembly: AssemblyProduct("FileSystemScan")]
[assembly: AssemblyCopyright("Copyright © Agama 2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("9038ffad-95a4-47da-b6cd-9554189a7f17")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace Agama
{
    /// <summary>
    /// File System Scanner
    /// </summary>
    public class Program
    {
        public static bool Force = false; //Force overwrite?
        public static string DriveLetter = null;
        public static string InputPath = null;
        public static string OutputPath = null;
        public static string CompareSource = null;
        public static string CompareTarget = null;

        public static void Main(string[] args)
        {
            //Print help message if the required arguments are not set
            if (args == null || args.Length == 0 || args[0] == "--help")
            {
                Usage();
                return;
            }

            //Handle global exception
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var error = e.ExceptionObject as Exception;
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(error.Message);
                Console.ForegroundColor = color;
                Environment.Exit(-1);
            };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--version":
                        Console.WriteLine(GetAssemblyVersion());
                        Environment.Exit(0);
                        break;
                        
                    case "-f":
                        Force = true;
                        break;
                        
                    case "-s":
                        DriveLetter = args[++i];
                        break;

                    case "-i":
                        InputPath = args[++i];
                        if (!File.Exists(InputPath))
                        {
                            throw new Exception("Input file not found: " + InputPath);
                        }
                        break;

                    case "-o":
                        OutputPath = args[++i];
                        if (File.Exists(OutputPath) && !Force)
                        {
                            throw new Exception("Output file already exists: " + OutputPath);
                        }
                        break;

                    case "-c":
                        CompareSource = args[++i];
                        if (!File.Exists(CompareSource))
                        {
                            throw new Exception("File not found: " + CompareSource);
                        }

                        CompareTarget = args[++i];
                        if (!File.Exists(CompareTarget))
                        {
                            throw new Exception("File not found: " + CompareTarget);
                        }
                        break;

                    default:
                    case "--help":
                        Usage();
                        return;
                }
            }

            if (!string.IsNullOrEmpty(CompareSource) && !string.IsNullOrEmpty(CompareTarget))
            {
                CompareDrive(CompareSource, CompareTarget);
            }
            else if (!string.IsNullOrEmpty(DriveLetter))
            {
                ScanDrive(DriveLetter);
            }
            else
            {
                throw new Exception("Verify the command line arguments or try --help");
            }
        }

        /// <summary>
        /// Prints help message to the console.
        /// </summary>
        public static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  fsscan [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help               Print this message");
            Console.WriteLine("  --version            Print version");
            Console.WriteLine("  -f                   Force overwrite if file exists");
            Console.WriteLine("  -s [dir]             Scan for folders and files");
            Console.WriteLine("  -i [file]            Input json file to resume");
            Console.WriteLine("  -o [file]            Output json file to save the scan result");
            Console.WriteLine("  -c [file1] [file2]   Compare json files for differences");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  fsscan -s C: -o drive_c.json");
            Console.WriteLine("  fsscan -s D: -o drive_d.json");
            Console.WriteLine("  fsscan -c drive_c.json drive_d.json -o result.json");
        }
        
        public static string GetAssemblyVersion()
        {
            return typeof(Program).Assembly.GetName().Version.ToString();
        }

        public static void ScanDrive(string drive)
        {
            var list = new List<RecordInfo>();

            long total = -1;
            bool isDriveLetter = true;

            if (FileSystemHelper.IsDriveReady(drive))
            {
                isDriveLetter = true;
            }
            else if (Directory.Exists(drive))
            {
                isDriveLetter = false;
            }
            else
            {
                throw new Exception("Drive not found: " + drive);
            }

            if (!string.IsNullOrEmpty(InputPath))
            {
                string resume = File.ReadAllText(InputPath);
                list = JSON.Parse<List<RecordInfo>>(resume);
            }

            if (isDriveLetter)
            {
                //string drive = "C:\\";
                if (drive.Length == 1) drive += ":\\";
                if (drive.Length == 2) drive += "\\";
                if (drive.Length > 3) throw new Exception("Check the drive letter!");
                total = FileSystemHelper.GetTotalSize(drive);
            }
            else
            {
                total = FileSystemHelper.GetTotalSize(drive.Substring(0, 2));
            }

            Console.WriteLine("Scanning " + drive + " (" + FileSystemHelper.BytesToString(total) + ")");
            Console.WriteLine();

            var progress = new ProgressBar(40);
            //var progress = new ProgressBar(60);
            //for (int i = 0; i <= 100; i++)
            {
                //progress.Update(i);
                //System.Threading.Thread.Sleep(50);
            }

            long count = 0;
            long current = 0;
            long errors = 0;

            progress.Update(0, current, count);

            int previousSave = DateTime.UtcNow.Minute;

            FileSystemWalker fsw = new FileSystemWalker();
            fsw.FileFound += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(InputPath))
                {
                    //Skip existing
                    var item = list.FirstOrDefault(x => x.Path == e.Path && x.IsDirectory == e.IsDirectory);
                    if (item != null)
                    {
                        if (!item.IsDirectory && item.Length > 0)
                        {
                            current += item.Length.Value;
                        }
                        double percent = 100 * current / total;
                        progress.Update(percent, current, ++count);
                        return;
                    }
                }

                //Console.WriteLine("Found: " + e.Path);				
                var record = new RecordInfo();
                record.Path = e.Path;
                record.IsDirectory = e.IsDirectory;

                try
                {
                    string path = e.Path;
                    if (e.IsDirectory)
                    {
                        var di = new DirectoryInfo(path);
                        record.Length = null;
                        record.Created = di.CreationTimeUtc;
                        record.Modified = di.LastWriteTimeUtc;
                        record.LastAccess = di.LastAccessTimeUtc;
                    }
                    else
                    {
                        var fi = new FileInfo(path);
                        record.Length = fi.Length;
                        record.Created = fi.CreationTimeUtc;
                        record.Modified = fi.LastWriteTimeUtc;
                        record.LastAccess = fi.LastAccessTimeUtc;
                        current += fi.Length;

                        using (var stream = File.OpenRead(path))
                        {
                            record.SHA1 = SHA1(stream);
                        }
                    }

                    double percent = 100 * current / total;
                    progress.Update(percent, current, ++count);
                    //System.Threading.Thread.Sleep(20);

                    if (DateTime.UtcNow.Minute != previousSave)
                    {
                        previousSave = DateTime.UtcNow.Minute;
                        //Console.WriteLine();
                        //Console.WriteLine("Saving manifest");
                        SaveManifest(list);
                    }
                }
                catch (Exception ex)
                {
                    record.Error = ex.Message;
                    errors++;
                }

                list.Add(record);
            };
            fsw.FileError += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(InputPath))
                {
                    //Skip existing
                    if (list.Any(x => x.Path == e.Path))
                    {
                        errors++;
                        return;
                    }
                }

                //Console.WriteLine("Error: " + e.Path);				
                var record = new RecordInfo();
                record.Path = e.Path;
                record.IsDirectory = true;
                record.Error = e.Error;
                list.Add(record);
                errors++;
            };
            fsw.Scan(drive);

            progress.Update(100, current, count);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(string.Format("Found {0} files and folders, {1} errors, {2}% of drive space", count, errors, (100d * current / total).ToString("N1", CultureInfo.InvariantCulture)));
            Console.WriteLine();
            Console.WriteLine("Done!");

            SaveManifest(list);
        }

        private static void CompareDrive(string src, string dst)
        {
            var checklist = new List<string>();
            var conflictlist = new List<ConflictInfo>();

            var srclist = JSON.Parse<List<RecordInfo>>(File.ReadAllText(src));
            var dstlist = JSON.Parse<List<RecordInfo>>(File.ReadAllText(dst));

            Console.WriteLine("Comparing...");
            Console.WriteLine("A: " + srclist.Count + " records");
            Console.WriteLine("B: " + dstlist.Count + " records");
            Console.WriteLine();

            //long count = Math.Max(srclist.Count, dstlist.Count);
            long count = 0;
            long current = 0;
            long conflicts = 0;
            long total = srclist.Where(x => x.Length > 0).Sum(x => x.Length.Value);
            if (total <= 0) total = 1;

            var progress = new ProgressBar(40);
            progress.Update(0, current, count);

            foreach (var srcitem in srclist)
            {
                string srcpath = srcitem.Path.Substring(3).ToLowerInvariant();
                checklist.Add(srcpath);

                var dstitem = dstlist.LastOrDefault(x => x.Path.Substring(3).ToLowerInvariant() == srcpath && x.IsDirectory == srcitem.IsDirectory);
                if (dstitem != null)
                {
                    if (!dstitem.IsDirectory && dstitem.Length != srcitem.Length)
                    {
                        conflicts++;
                        conflictlist.Add(new ConflictInfo()
                        {
                            Path = srcitem.Path,
                            Message = "File size mismatch!"
                        });
                    }
                    else if (!dstitem.IsDirectory && dstitem.SHA1 != srcitem.SHA1)
                    {
                        conflicts++;
                        conflictlist.Add(new ConflictInfo()
                        {
                            Path = srcitem.Path,
                            Message = "SHA1 checksum mismatch!"
                        });
                    }
                }
                else
                {
                    conflicts++;
                    conflictlist.Add(new ConflictInfo()
                    {
                        Path = srcitem.Path,
                        Message = "File is missing on the secondary drive!"
                    });
                }

                count++;
                if (srcitem.Length > 0)
                {
                    current += srcitem.Length.Value;
                }
                double percent = 100 * current / total;
                progress.Update(percent, current, count);
            }

            if (checklist.Count != dstlist.Count)
            {
                foreach (var dstitem in dstlist)
                {
                    string dstpath = dstitem.Path.Substring(3).ToLowerInvariant();
                    if (checklist.Contains(dstpath))
                    {
                        continue;
                    }

                    checklist.Add(dstpath);

                    var srcitem = srclist.LastOrDefault(x => x.Path.Substring(3).ToLowerInvariant() == dstpath && x.IsDirectory == dstitem.IsDirectory);
                    if (srcitem == null)
                    {
                        conflicts++;
                        conflictlist.Add(new ConflictInfo()
                        {
                            Path = dstitem.Path,
                            Message = "File is missing on the primary drive!"
                        });
                    }

                    count++;
                    //if (dstitem.Length > 0)
                    //{
                    //    current += dstitem.Length.Value;
                    //}
                    //progress.Update(0, current, count);
                }
            }

            progress.Update(100, current, count);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(string.Format("Found {0} conflicts", conflicts));
            Console.WriteLine();

            SaveManifest(conflictlist);
            Console.WriteLine("Done!");
        }

        public class JSON
        {
            public static T Parse<T>(string json)
            {
                var instance = typeof(T);
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var deserializer = new DataContractJsonSerializer(instance);
                    return (T)deserializer.ReadObject(ms);
                }
            }

            public static string Stringify(object data)
            {
                string json = null;
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
                using (var stream = new MemoryStream())
                {
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(data.GetType(), settings);
                        serializer.WriteObject(writer, data);
                        writer.Flush();
                        byte[] array = stream.ToArray();
                        json = Encoding.UTF8.GetString(array, 0, array.Length);
                    }
                }

                return json;
            }
        }

        public static void SaveManifest(object list)
        {
            if (!string.IsNullOrEmpty(OutputPath))
            {
                string json = JSON.Stringify(list);
                File.WriteAllText(OutputPath, json);
            }
        }

        /// <summary>
        /// Generates chechum from a string and prints output to the console.
        /// </summary>
        /// <param name="content">Plain text content.</param>
        public static void GenerateContentChecksum(string content)
        {
            using (var stream = new MemoryStream())
            {
                byte[] data = Encoding.UTF8.GetBytes(content);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                string hash = SHA1(stream);
                Console.Write(hash);
            }
        }

        /// <summary>
        /// Generates checksum for a file and prints output to the console.
        /// </summary>
        /// <param name="path">Path to a file.</param>
        public static void GenerateFileChecksum(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path argument is missing!");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File does not exist!" + Environment.NewLine + "Path: " + path);
            }

            using (var stream = File.OpenRead(path))
            {
                string hash = SHA1(stream);
                WriteEntry(hash, path);
            }
        }

        /// <summary>
        /// Generates checksum for files in the directory
        /// </summary>
        /// <param name="directory"></param>
        public static void GenerateDirectoryChecksum(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Path argument is missing!");
            }

            if (!Directory.Exists(directory))
            {
                throw new FileNotFoundException("Directory does not exist!" + Environment.NewLine + "Path: " + directory);
            }

            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                try
                {
                    string path = file;
                    using (var stream = File.OpenRead(path))
                    {
                        string hash = SHA1(stream);

                        //Simplify path, e.g. ".\sha1.exe" -> "sha1.exe"
                        if (path.StartsWith(@".\"))
                        {
                            path = path.Substring(2);
                        }

                        WriteEntry(hash, path);
                    }
                }
                catch
                {
                    //Warning: file is not listed, may be used by another process
                }
            }
        }

        /// <summary>
        /// Verifies checkum of a file or list of files and prints output to the console.
        /// </summary>
        /// <param name="path">Path to a .sha1 file.</param>
        public static void Verify(string path)
        {
            //Default console text color
            var color = Console.ForegroundColor;

            using (var stream = File.OpenRead(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = Regex.Match(line, @"^(?<sha1>[\w\d]{32})\s\*(?<file>.+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            string status = "    ";
                            string sha1 = match.Groups["sha1"].Value;
                            string file = match.Groups["file"].Value;

                            if (File.Exists(file))
                            {
                                using (var fs = File.OpenRead(file))
                                {
                                    //Compare the SHA1 of the file and .sha1 entry
                                    string checksum = SHA1(fs);
                                    bool ok = (checksum == sha1);
                                    status = ok ? " OK " : "FAIL";

                                    Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                                }
                            }
                            else
                            {
                                //File is missing
                                status = "MISS";

                                Console.ForegroundColor = ConsoleColor.Yellow;
                            }

                            //Print the result
                            string report = string.Format("[{0}] {1}", status, file);
                            Console.WriteLine(report);
                            Console.ForegroundColor = color;
                        }
                    }
                }
            }

            //Restore the default color
            Console.ForegroundColor = color;
        }

        /// <summary>
        /// Writes an entry to the .sha1 list.
        /// </summary>
        /// <param name="sha1"></param>
        /// <param name="path"></param>
        public static void WriteEntry(string sha1, string path)
        {
            Console.WriteLine(sha1 + " *" + path);
        }

        /// <summary>
        /// Generates SHA1 checksum of a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string SHA1(Stream stream)
        {
            var crypto = new SHA1CryptoServiceProvider();
            var bytes = crypto.ComputeHash(stream);
            var builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    [DataContract]
    public class RecordInfo
    {
        public const string ISO8601 = "yyyy-MM-ddTHH:mm:ssZ";

        [DataMember(Name = "path", IsRequired = false, EmitDefaultValue = false, Order = 1)]
        public string Path { get; set; }

        [DataMember(Name = "dir", IsRequired = false, EmitDefaultValue = false, Order = 2)]
        public bool IsDirectory { get; set; }

        [DataMember(Name = "md5", IsRequired = false, EmitDefaultValue = false, Order = 10)]
        public string MD5 { get; set; }

        [DataMember(Name = "sha1", IsRequired = false, EmitDefaultValue = false, Order = 11)]
        public string SHA1 { get; set; }

        [DataMember(Name = "sha256", IsRequired = false, EmitDefaultValue = false, Order = 12)]
        public string SHA256 { get; set; }

        [DataMember(Name = "length", IsRequired = false, EmitDefaultValue = false, Order = 20)]
        public long? Length { get; set; }

        [DataMember(Name = "created", IsRequired = false, EmitDefaultValue = false, Order = 30)]
        public string CreatedString
        {
            get
            {
                return this.Created != null ? this.Created.Value.ToString(ISO8601) : null;
            }
            set
            {
                this.Created = (value != null ? (DateTime?)DateTime.ParseExact(value, ISO8601, CultureInfo.InvariantCulture) : null);
            }
        }

        public DateTime? Created { get; set; }

        [DataMember(Name = "modified", IsRequired = false, EmitDefaultValue = false, Order = 31)]
        public string ModifiedString
        {
            get
            {
                return this.Modified != null ? this.Modified.Value.ToString(ISO8601) : null;
            }
            set
            {
                this.Modified = (value != null ? (DateTime?)DateTime.ParseExact(value, ISO8601, CultureInfo.InvariantCulture) : null);
            }
        }

        public DateTime? Modified { get; set; }

        [DataMember(Name = "accessed", IsRequired = false, EmitDefaultValue = false, Order = 32)]
        public string LastAccessString
        {
            get
            {
                return this.LastAccess != null ? this.LastAccess.Value.ToString(ISO8601) : null;
            }
            set
            {
                this.LastAccess = (value != null ? (DateTime?)DateTime.ParseExact(value, ISO8601, CultureInfo.InvariantCulture) : null);
            }
        }

        public DateTime? LastAccess { get; set; }

        [DataMember(Name = "error", IsRequired = false, EmitDefaultValue = false, Order = 95)]
        public string Error { get; set; }
    }

    [DataContract]
    public class ConflictInfo
    {
        [DataMember(Name = "path", IsRequired = false, EmitDefaultValue = false, Order = 1)]
        public string Path { get; set; }

        [DataMember(Name = "message", IsRequired = false, EmitDefaultValue = false, Order = 2)]
        public string Message { get; set; }
    }

    public class FileSystemWalker
    {
        protected void ScanRecursive(DirectoryInfo dir, string searchPattern)
        {
            OnFileFound(dir.FullName, true);

            try
            {
                foreach (FileInfo f in dir.GetFiles(searchPattern))
                {
                    OnFileFound(f.FullName, false);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dir.FullName);                
                //return;  // We alredy got an error trying to access dir so dont try to access it again
                OnFileError(dir.FullName, ex.Message);
                return;
            }

            try
            {
                foreach (DirectoryInfo d in dir.GetDirectories())
                {
                    ScanRecursive(d, searchPattern);
                }
            }
            catch (Exception ex)
            {
                OnFileError(dir.FullName, ex.Message);
            }
        }

        public void Scan(string directory)
        {
            //DirectoryInfo di = new DirectoryInfo("A:\\");
            DirectoryInfo di = new DirectoryInfo(directory);
            ScanRecursive(di, "*");
            OnCompleted();
        }

        public event EventHandler<FileEventArgs> FileFound;
        protected virtual void OnFileFound(string path, bool isDirectory)
        {
            if (FileFound != null)
            {
                FileFound(this, new FileEventArgs() { Path = path, IsDirectory = isDirectory });
            }
        }

        public event EventHandler<FileEventArgs> FileError;
        protected virtual void OnFileError(string path, string error)
        {
            if (FileError != null)
            {
                FileError(this, new FileEventArgs() { Path = path, Error = error });
            }
        }

        public event EventHandler<EventArgs> Completed;
        protected virtual void OnCompleted()
        {
            if (Completed != null)
            {
                Completed(this, new EventArgs());
            }
        }
    }

    public class FileSystemHelper
    {
        public static bool IsDriveReady(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name.ToLower().StartsWith(driveName.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        public static long GetTotalSize(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name.ToLower().StartsWith(driveName.ToLower()))
                {
                    return drive.TotalSize;
                }
            }
            return -1;
        }

        public static long GetFreeSpace(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name.ToLower().StartsWith(driveName.ToLower()))
                {
                    return drive.TotalFreeSpace;
                }
            }
            return -1;
        }

        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString("N1", CultureInfo.InvariantCulture) + suf[place];
        }
    }

    public class FileEventArgs : EventArgs
    {
        public string Path
        {
            get;
            set;
        }

        public bool IsDirectory
        {
            get;
            set;
        }

        public string Error
        {
            get;
            set;
        }
    }

    //Thanks to https://gist.github.com/prabirshrestha/1105125
    public class ProgressBar
    {
        private int next = 0;
        private int _lastOutputLength;
        private readonly int _maximumWidth;
        private readonly char[] _jokers = new char[] { '\\', '|', '/', '-' };

        public ProgressBar(int maximumWidth)
        {
            _maximumWidth = maximumWidth;
        }

        public void Update(double percent, long length, long count)
        {
            // Remove the last state           
            string clear = string.Empty.PadRight(_lastOutputLength, '\b');

            Show(clear);

            // Generate new state           
            int width = (int)(percent / 100 * _maximumWidth);
            int fill = _maximumWidth - width;
            char joker = _jokers[next++];
            if (next >= _jokers.Length) next = 0;

            string size = FileSystemHelper.BytesToString(length);

            //string output = string.Format(" [ {0}{1} ] {2}%", string.Empty.PadLeft(width, '='), string.Empty.PadLeft(fill, ' '), percent.ToString("0.0", CultureInfo.InvariantCulture));
            string output = string.Format("{3} {4} [{0}{1}] {2}% {5}", string.Empty.PadLeft(width, '='), string.Empty.PadLeft(fill, ' '), percent.ToString("0.0", CultureInfo.InvariantCulture),
            "".PadLeft(10 - size.Length) + size,
            "".PadLeft(10 - count.ToString().Length) + count,
            joker);
            Show(output);
            _lastOutputLength = output.Length;
        }

        protected void Show(string value)
        {
            Console.Write(value);
        }
    }
}