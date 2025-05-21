using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using zlib; // zlib.net 

//REN'PY RPA Extractor by Denis Solicen & SAn4Es_TV
//Made for Solicen.TEAM and Ren'Py games Translation Community
namespace Solicen.RenPy
{
    class Archive
    {
        static string[] files; static string[] indexes; static double version; static int Offset; static string HeaderText;
        static Dictionary<string, double> RPA_MAGIC = new Dictionary<string, double>()
        {
            {"RPA-2.0", 2},{"RPA-3.0", 3},{"RPA-3.2", 3.2},{"RPA-4.0", 4}
        };

        //Получает версию RPA из файла
        //Необходим Double а не INT32, так есть промежуточные версии RPA
        public static double GetVersion(string path)
        {
            if (path.EndsWith(".rpi")) return 1;
            string magic = File.ReadLines(path).First();
            var result = RPA_MAGIC.FirstOrDefault(x => magic.StartsWith(x.Key)).Value;
            Console.WriteLine("[MAGIC]: " + magic);

            return result;
        }

        public static void CreateRPA(string outputPath, string InputDirectory)
        {
            var fileName = outputPath.Split('\\')[outputPath.Split('\\').Length - 1];
            var l = ListFromDirectory(InputDirectory);
            //1. Получить файлы из директории
            var files = Directory.GetFiles(InputDirectory, "*", SearchOption.AllDirectories);

            //2 Изменяем саму строку заголовка Zlib
            var tZlib = Solicen.EX.Zlib.CreateZlibHeader("", l);
            //3. Изменить оффсет для будущего извлечения
            var tOffset = Solicen.EX.Zlib.ModifyZlibOffset(tZlib.Length, files, "");

            //4 Компресируем новую строку заголовка Zlib
            var header = Solicen.Compress.CompressString(tZlib);

            string newFile = "RPA-3.0 0x0000000 42424242";
            var index = 0; var lenghtFile = newFile.Length + 7;
            foreach (var file in files)
            {
                var renpySplitter = "\n" + "Made with Ren'Py.";
                var f1 = File.ReadAllText(file, Encoding.GetEncoding(1251));
                newFile += renpySplitter + f1;
                Console.WriteLine(" To archive : " + fileName + " : added file : " + l[index]);
                index++;
                lenghtFile += f1.Length + renpySplitter.Length;
            }

            //6 Добавляем измененную строку
            newFile = Solicen.EX.Zlib.ReplaceZlibHeader(newFile, "", header);

            //7 Меняем оффсет на длину всех строк файла
            tOffset = lenghtFile;

            //8 Изменить оффсет заголовка RPA
            newFile = ReplaceRPAHeader(newFile, tOffset);

            byte[] bytesFile = Encoding.Default.GetBytes(newFile);

            Console.WriteLine("Все файлы были успешно добавлены");
            File.WriteAllBytes(outputPath+".rpa", bytesFile);
        }
        
        // Осталось в коде, но является не рабочим и не используется.
        public static void AddFilesToRPA(string PATHtoFile, string InputDirectory)
        {
            var fileName = PATHtoFile.Split('\\')[PATHtoFile.Split('\\').Length - 1];
            var l = ListFromDirectory(InputDirectory);

            //0 Читаем файл
            var newFile = File.ReadAllText(PATHtoFile, Encoding.GetEncoding(1251));

            //1. Получить файлы из директории
            var files = Directory.GetFiles(InputDirectory, "*", SearchOption.AllDirectories);

            //2. Получить заголовок Zlib
            ExtractZlibHeader(PATHtoFile);

            //3. Изменить оффсет для будущего извлечения
            var tOffset = Solicen.EX.Zlib.ModifyZlibOffset(Offset, files, HeaderText);

            //4 Изменяем саму строку заголовка Zlib
            var tZlib = Solicen.EX.Zlib.CreateZlibHeader(HeaderText, l);

            //5 Компресируем новую строку заголовка Zlib
            var header = Solicen.Compress.CompressString(tZlib);

            //6 Удаляем строку прочь из файла
            newFile = Solicen.EX.Zlib.ReplaceZlibHeader(newFile, HeaderText, " ");

            var index = 0; var lenghtFile=newFile.Length+1;
            foreach (var file in files)
            {
                var renpySplitter = "\n" + "Made with Ren'Py.";
                var f1 = File.ReadAllText(file, Encoding.GetEncoding(1251));
                newFile += renpySplitter + f1;
                Console.WriteLine(" в архив : " + fileName + " : добавлен файл : " + l[index]);
                index++;
                lenghtFile += f1.Length + renpySplitter.Length;
            }
             
            //6 Добавляем измененную строку
            newFile = Solicen.EX.Zlib.ReplaceZlibHeader(newFile, "", header);

            //7 Меняем оффсет на длину всех строк файла
            tOffset = lenghtFile;

            //8 Изменить оффсет заголовка RPA
            newFile = ReplaceRPAHeader(newFile, tOffset);

            byte[] bytesFile = Encoding.Default.GetBytes(newFile);

            Console.WriteLine("Все файлы были успешно добавлены");
            File.WriteAllBytes(PATHtoFile, bytesFile);
        }


        static string ReplaceRPAHeader(string text, int newRPAOffset)
        {
            var t = text;

            var f1 = text.Split(' ')[0];
            var f2 = text.Split(' ')[1];
            Console.WriteLine(f1 + " " + f2);

            var zOffset = "000000000" + Convert.ToString(newRPAOffset, 16);

            Console.WriteLine(zOffset);

            Console.WriteLine("Old Offset : " + f2);
            Console.WriteLine("New Offset : " + zOffset);

            Console.WriteLine("Итоговая строка:\n" + f1 + " " + zOffset);
            t = t.Replace(f2, zOffset);
            return t;
        }

        //Извлекает все RPA файлы
        public static void ExtractAllRPA(string directory, string output)
        {     
            foreach (var rpa in RPAFilesInDirectory(directory))
            {
                ExtractArchive(rpa, output);
                GC.Collect();
            }
            Console.WriteLine("[INF] All process is finished!");
        }

        public static string[] ListFromDirectory(string directory) 
            => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Select(x => x.Replace($"{directory}\\", "")).ToArray();
        
        //Создает поток из строки
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writter = new StreamWriter(stream);
            writter.Write(s);
            writter.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string[] GetFilesList(string path)
        {
            if (!File.Exists(path)) return null;

            Encoding enCode = Encoding.GetEncoding(1252);
            string[] files = Regex.Split(File.ReadAllText(path, enCode), "Made with Ren'Py.", RegexOptions.Multiline);
            ExtractZlibHeader(path); //Получаем индексы и заголовок файла
            return files.Select(x => x.Split('/')[x.Split('/').Length - 1]).ToArray();
        }

        private static string ParseFormat(string line)
        {
            if (line.StartsWith("RENPY RPC2")) return ".rpyс"; // Заголовок RPYC файла
            if (line.StartsWith("‰PNG")) return ".png";        // Заголовок RNG файла
            if (line.StartsWith("Ogg")) return ".ogg";         // Заголовок OGG файла
            if (line.StartsWith("ID3")) return ".mp3";         // Заголовок MP3 файла
            if (line.Contains("#File ")) return ".txt";        // Заголовок TXT файла

            //Иначе считаем файлом PRY
            return ".rpy";
        }

        static int index = 0;
        public static void ExtractArchive(string path, string output)
        {
            Console.WriteLine($"[Parse process]");

            //Стандартная кодировка Windows 1251
            Encoding enCode = Encoding.GetEncoding(1252);
            string[] files = Regex.Split(File.ReadAllText(path, enCode), "Made with Ren'Py.", RegexOptions.Multiline);
            Console.WriteLine($"[INF] Parse file : {path}");
            Console.WriteLine($"[INF] Files found: {files.Length - 1}");
        
            ExtractZlibHeader(path); //Получаем индексы и заголовок файла

            var indexFile = 0; string fileName = "", directory = output;
            var directoryName = directory.Split('/')[directory.Split('/').Length - 1];
            var filePath = "\\m\\";

            //Console.WriteLine(directoryName);
            Console.WriteLine($"[Extraction process]");
            Directory.CreateDirectory(directory);

            foreach (var f in files)
            {
                //Если первая строка содержит RPA-X-X то пропустить данный 'файл'
                if (f.StartsWith("RPA")) continue;
                fileName = "";

                if (indexes.Length != 0)
                {
                    //Получаем имя файла через разделение строки
                    fileName = indexes[indexFile].Split('/')[indexes[indexFile].Split('/').Length - 1];
                    //Получаем путь до файла убирая имя файла
                    filePath = indexes[indexFile].Replace(fileName, "");
                }

                //Если имя файла не найдено.
                if (fileName == "")
                {
                    var fName = $"{index}{ParseFormat(f)}";
                    if (File.Exists(directory + fName)) continue;
                    Console.WriteLine($"[INF] Successfully extracted file: {fName}");
                    File.WriteAllText(directory + fName, f, enCode);                
                    index++;

                }
                //Если имя файла найдено
                else
                {
                    try
                    {
                        Console.WriteLine($"[INF] Successfully extracted ({Path.GetExtension(fileName)}): " + fileName);
                        Directory.CreateDirectory(directory + "\\" + filePath);
                        File.WriteAllText(directory + "\\" + filePath + "\\" + fileName, f, enCode);
                        indexFile++;
                    }
                    catch { }
                }
            }
            Console.WriteLine($"\n[INF] Extraction ({Path.GetFileName(path)}) is finished!\n");
        }

        //Прочитывает все необходимые байты из потока
        public static byte[] ReadAllBytesOfStream(Stream stream, int offset, int lenght)
        {
            BinaryReader br = new BinaryReader(stream);
            List<byte> bytesOfFile = new List<byte>();
            for (int i = offset; i < lenght; i++)
            {
                br.BaseStream.Position = i;
                bytesOfFile.Add(br.ReadByte());
            }
            return bytesOfFile.ToArray();
        }

        //Получить RPA файлы в папке
        static string[] RPAFilesInDirectory(string DirectoryPath) 
            => Directory.GetFiles(DirectoryPath).Where(x => x.EndsWith(".rpa")).ToArray();
        
        static void ExtractZlibHeader(string path)
        {
            var key = 0; version = GetVersion(path); indexes = null;
            var code = EncodingType.GetType(path);

            Console.WriteLine("\nFile         : " + Path.GetFileName(path));
            Console.WriteLine("Encoding     : " + code.HeaderName);

            FileStream fileStream = new FileStream(path, FileMode.Open);
            BinaryReader br = new BinaryReader(fileStream);
            StreamReader reader = new StreamReader(br.BaseStream, true);
            var fs = br.BaseStream; 

            fs.Seek(0, SeekOrigin.Current);
            if (version == 2 || version == 3 || version == 3.2 || version == 4)
            {          
                var metaData = reader.ReadLine();
                var vals = metaData.Split(' ');
                var offset = Convert.ToInt32(vals[1], 16);

                if (version == 3)
                {
                    key = Convert.ToInt32(vals[2], 16);
                }
                else if (version == 3.2) 
                {
                    key ^= Convert.ToInt32(vals[3], 16);
                }

                Console.WriteLine("RPA Offset   : " + offset);
                Console.WriteLine("RPA EndKey   : " + key);
                Console.WriteLine("RPA Length   : " + reader.BaseStream.Length);
                Console.WriteLine();

                byte[] bytesOfFile = ReadAllBytesOfStream(br.BaseStream, offset, (int)br.BaseStream.Length);

                List<string> tempIndexes = new List<string>();
                string final = Solicen.Compress.DeDecompressToString(bytesOfFile);
                // Console.WriteLine("Final : " + final + "\n");
                Regex regex = new Regex(@"(?<=\x00\x00\x00).*?(?=\])", RegexOptions.IgnoreCase);
                MatchCollection matches = regex.Matches(final);

                if (matches.Count > 0)
                {
                    Console.WriteLine($"[INF] Files in archive:");
                    foreach (Match match in matches)
                    { 
                        string s1 = match.ToString(); 
                        Regex r1 = new Regex(@"q$|q.$");
                        string s = r1.Replace(s1, "");

                        tempIndexes.Add(s);
 
                        Console.WriteLine($"- {s}");
                    }               
                }
                Console.WriteLine();
                tempIndexes.Sort(StringComparer.OrdinalIgnoreCase);
                indexes = tempIndexes.ToArray();
                fs.Close(); br.Close(); fileStream.Close();

                Offset = offset; HeaderText = final;
            }
        } 

        static string ConvertToHex(string input) 
            => BitConverter.ToString(Encoding.Default.GetBytes(input)).Replace("-","");
    }
}

namespace Solicen
{
    class Compress
    {
        public static string CompressString(string s)
        {
            byte[] buffer = Encoding.Default.GetBytes(s);
            MemoryStream memOutput = new MemoryStream();
            ZOutputStream zipOut = new ZOutputStream(memOutput, zlibConst.Z_DEFAULT_COMPRESSION);

            zipOut.Write(buffer, 0, buffer.Length);
            zipOut.finish();

            memOutput.Seek(0, SeekOrigin.Begin);
            byte[] result = memOutput.ToArray();

            return Encoding.Default.GetString(result);
        }

        //Декомпресирует строку Zlib
        public static string DeDecompressToString(byte[] buffer)
        {
            try
            {
                MemoryStream memOutput = new MemoryStream();
                ZOutputStream zipOut = new ZOutputStream(memOutput);
                zipOut.Write(buffer, 0, buffer.Length); zipOut.finish();

                memOutput.Seek(0, SeekOrigin.Begin);
                byte[] result = memOutput.ToArray();
                var str = Encoding.Default.GetString(result); return str;
            }
            catch
            {
                string s = Encoding.Default.GetString(buffer); return s;
            }
        }

    }
}

namespace Solicen.EX
{
    class Zlib
    {
        public static string CreateZlibHeader(string zlibHeader, string[] newFilesPATH)
        {
            var t = zlibHeader;
            foreach (var file in newFilesPATH)
            {
                Console.WriteLine("Add file :" + file + " to a header");
                t += "\x00\x00\x00" + file + "]";
            }
            return t;
        }

        public static int ModifyZlibOffset(int zlibOffset, string[] newFiles, string oldHeader)
        {
            var lenght = zlibOffset;
            foreach (var file in newFiles)
            {
                lenght += file.Length;
            }
            return lenght - oldHeader.Length;
        }

        public static string ReplaceZlibHeader(string text, string rawZlibHeader, string newZlibHeader)
        {
            if (rawZlibHeader != "")
            {
                return text.Replace(rawZlibHeader, newZlibHeader);
            }
            else
            {
                return text + "\n" + newZlibHeader;
            }
        }
    }
}


