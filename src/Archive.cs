using System;using System.Collections.Generic;using System.IO;using System.IO.Compression;
using System.Linq;
using System.Text;using System.Text.RegularExpressions;using System.Threading.Tasks;
using zlib; // zlib.net 

//REN'PY RPA Extractor by Denis Solicen & SAn4Es_TV
//Made for Solicen.TEAM and Ren'Py games Translation Community
namespace Solicen.RenPy
{
    class Archive
    {
        static string[] files; static string[] indexes; static double version; public static int Offset; public static string HeaderText;
        static Dictionary<string, double> RPA_MAGIC = new Dictionary<string, double>()
        {
            {"RPA-2.0", 2},{"RPA-3.0", 3},{"RPA-3.2", 3.2},{"RPA-4.0", 4}

        };

        //Получает версию RPA из файла
        public static double GetVersion(string path)
        {
            string s = File.ReadAllLines(path)[0];
            string magic = s;

            Console.WriteLine("magic: " + s);
            var _magic = RPA_MAGIC.FirstOrDefault(x => magic.StartsWith(x.Key)).Value;

            if (path.EndsWith(".rpi")) return 1;
            else return _magic;
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


            Console.WriteLine("Итогая строка:\n" + f1 + " " + zOffset);
            t = t.Replace(f2, zOffset);
            return t;
        }

        //Извлекает все RPA файлы
        public static void ExtractAllRPA(string directory, string output)
        {     
            var RPAFiles = RPAFilesInDirectory(directory);
            foreach (var rpa in RPAFiles)
            {
                ExtractArchive(rpa, output);
            }

            Console.WriteLine("\nВсе процессы завершены!");
        }

        public static string[] ListFromDirectory(string directory)
        {
            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(x => x.Replace($"{directory}\\", "")).ToArray();
        }

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

        static int index = 0;
        public static void ExtractArchive(string path, string output)
        {
            //Стандартная кодировка Windows 1251
            Encoding enCode = Encoding.GetEncoding(1252);
            string[] files = Regex.Split(File.ReadAllText(path, enCode), "Made with Ren'Py.", RegexOptions.Multiline);
            Console.WriteLine("Найдено файлов : " + (files.Length - 1));
        
            ExtractZlibHeader(path); //Получаем индексы и заголовок файла

            var indexFile = 0; string fileName = "", directory = output;
            var directoryName = directory.Split('/')[directory.Split('/').Length - 1];
            var filePath = "\\m\\";

            Console.WriteLine(directoryName);
            foreach (var f in files)
            {
                //Если первая строка содержит RPA-X-X то пропустить данный 'файл'
                if (f.StartsWith("RPA")) continue;
                fileName = ""; 

                //Следующие два метода проверяют есть ли в файле RPA осмысленный текст от файл RPY
                //То есть запакован ли в RPA файл обычный RPY файл без обфускации
                //RegexMatchesOfRPY(f);
                //if (f.Contains(".rpy")) fileName = GetStringContainsOfRPY(RegexMatchesOfRPY(f));

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
                    //Заголовок RPYC файла
                    if (f.StartsWith("RENPY RPC2"))
                    {          
                        if (File.Exists(directory + index + ".rpyс")) continue;
                        Console.WriteLine("Извлечен файл : " + index + ".rpyс" + " : ");
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(directory + index + ".rpyс", f);
                        
                    }
                    //Заголовок RNG файла
                    else if (f.StartsWith("‰PNG"))
                    {
                        if (File.Exists(directory + index + ".png")) continue;
                        Console.WriteLine("Извлечен файл : " + index + ".png" + " : ");
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(directory + index + ".png", f, enCode);
                    }
                    //Заголовок OGG файла
                    else if (f.StartsWith("Ogg"))
                    {
                        if (File.Exists(directory + index + ".ogg")) continue;
                        Console.WriteLine("Извлечен файл : " + index + ".ogg" + " : ");
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(directory + index + ".ogg", f, enCode);
                    }
                    //Заголовок MP3 файла
                    else if (f.StartsWith("ID3"))
                    {
                        if (File.Exists(directory + index + ".mp3")) continue;
                        Console.WriteLine("Извлечен файл : " + index + ".mp3" + " : ");
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(directory + index + ".mp3", f, enCode);
                    }
                    //Заголовок RPY или TXT файла
                    else if (f.Contains("#File ") || f.Contains("  "))
                    {
                        if (File.Exists(directory + index + ".txt")) continue;
                        Console.WriteLine("Извлечен файл : " + index + ".txt" + " : ");
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(directory + index + ".txt", f);
                    }
                    //Если ни одного заголовка не найдено считаем файлом PRY
                    else
                    {
                        if (File.Exists(directory + index + ".rpy")) continue;
                        Console.WriteLine("Извлечен файл : " + index + ".rpy" + " : ");
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(directory + index + ".rpy", f);
                    }
                    index++;
                    
                }
                //Если имя файла найденно
                else
                {
                    try
                    {
                        Console.WriteLine("Извлечен файл : " + fileName + " : ");
                        Directory.CreateDirectory(directory + "\\" + filePath);
                        File.WriteAllText(directory + "\\" + filePath + "\\" + fileName, f, enCode);
                        indexFile++;
                    }
                    catch { }
                }            
            }

            Console.WriteLine("\nПроцесс завершен!");
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
        {
            var d = Directory.GetFiles(DirectoryPath);
            List<string> directories = new List<string>();
            foreach(var dir in d)
            {
                if (dir.EndsWith(".rpa")) directories.Add(dir);
            }
            return directories.ToArray();
        }

        static void ExtractZlibHeader(string path)
        {
            var key = 0; version = 3; indexes = null;

            var code = EncodingType.GetType(path);
            Console.WriteLine("\nФайл         : " + Path.GetFileName(path));
            Console.WriteLine("Кодировка    : " + code.HeaderName);

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
                    key = 0;
                    key = Convert.ToInt32(vals[2], 16);
                }
                else if (version == 3.2) 
                {
                    key = 0;
                    key ^= Convert.ToInt32(vals[3], 16);
                }

                Console.WriteLine("RPA Offset   : " + offset);
                Console.WriteLine("RPA EndKey   : " + key);
                Console.WriteLine("Длина потока : " + reader.BaseStream.Length);


                byte[] bytesOfFile = ReadAllBytesOfStream(br.BaseStream, offset, (int)br.BaseStream.Length);

                List<string> tempIndexes = new List<string>();
                string final = Solicen.Compress.DeDecompressToString(bytesOfFile);
                Console.WriteLine("Финал : " + final + "\n");
                Regex regex = new Regex(@"(?<=\x00\x00\x00).*?(?=\])", RegexOptions.IgnoreCase);
                MatchCollection matches = regex.Matches(final);

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    { 
                        string s1 = match.ToString(); 
                        Regex r1 = new Regex(@"q$|q.$");
                        string s = r1.Replace(s1, "");

                        tempIndexes.Add(s); Console.WriteLine(s);
                    }
                    
                }

                Console.WriteLine("Количество совпадений : " + tempIndexes.Count);
                tempIndexes.Sort(StringComparer.OrdinalIgnoreCase);
                indexes = tempIndexes.ToArray();
                fs.Close(); br.Close(); fileStream.Close();

                Offset = offset; HeaderText = final;
            }
        } 

        static string ConvertToHex(string input)
        {
            var dataByte = Encoding.Default.GetBytes(input);
            string HEX = BitConverter.ToString(dataByte);
            HEX = HEX.Replace("-", "");
            return HEX;
        }

        static StringBuilder builder = new StringBuilder();
        static void CreateFilesLog()
        {
            File.WriteAllText(Environment.CurrentDirectory + "\\extractor.log", builder.ToString(), Encoding.GetEncoding(1252));
            Environment.Exit(0);
        }
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
                Console.WriteLine("Добавляю файл :" + file + " в заголовок");
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

