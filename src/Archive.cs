using System;using System.Collections.Generic;using System.IO;using System.IO.Compression;
using System.Text;using System.Text.RegularExpressions;using System.Threading.Tasks;
using zlib; // zlib.net 

//REN'PY RPA Extractor by Denis Solicen & SAn4Es_TV
//Made for Solicen.TEAM and Ren'Py games Translation Community
namespace Solicen.RenPy
{
    class Archive
    {
        static string RPA2_MAGIC = "RPA-2.0"; static string RPA3_MAGIC = "RPA-3.0"; static string RPA3_2MAGIC = "RPA-3.2";
        static string[] files; static string[] indexes; static double version; public static int Offset; public static string HeaderText;

        //Получает версию RPA из файла
        static double GetVersion(string path)
        {
            string s = File.ReadAllLines(path)[0];
            string magic = s;

            Console.WriteLine("magic: " + s);
            return 3;

            if (magic.StartsWith(RPA3_2MAGIC))
                return 3.2;
            else if (magic.StartsWith(RPA3_MAGIC))
                return 3;
            else if (magic.StartsWith(RPA2_MAGIC))
                return 2;
            else if (path.EndsWith(".rpi")) 
                return 1;

            return 0;
        }

        //Находит все строки с .rpy 
        static string[] RegexMatchesOfRPY(string f)
        {
            var r = Regex.Matches(f, "[A-z.-]*[.rpy]");
            List<string> list = new List<string>();
            foreach (var match in r)
            {
                list.Add(match.ToString());
            }
            return list.ToArray();
        }

        //Технически нужно только для не зашифрованных файлов .rpy внутри RPA
        //У них можно определить их имя по внутрениму контексту
        static string GetStringContainsOfRPY(string[] list)
        {
            var temp = "";
            foreach (var l in list)
            {
                if (l.Contains(".rpy"))
                {                 
                    temp = l;
                    break;
                }
            }
            return temp;
        }


        public static void CreateRPA(string outputPath, string InputDirectory)
        {
            var fileName = outputPath.Split('\\')[outputPath.Split('\\').Length - 1];
            var l = ListFromDirectory(InputDirectory);
            //1. Получить файлы из директории
            var files = Directory.GetFiles(InputDirectory, "*", SearchOption.AllDirectories);

            //2 Изменяем саму строку заголовка Zlib
            var tZlib = CreateZlibHeader("", l);
            //3. Изменить оффсет для будущего извлечения
            var tOffset = ModifyZlibOffset(tZlib.Length, files, "");

            //4 Компресируем новую строку заголовка Zlib
            var header = CompressString(tZlib);

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
            newFile = ReplaceZlibHeader(newFile, "", header);

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
            var tOffset = ModifyZlibOffset(Offset, files, HeaderText);

            //4 Изменяем саму строку заголовка Zlib
            var tZlib = CreateZlibHeader(HeaderText, l);

            //5 Компресируем новую строку заголовка Zlib
            var header = CompressString(tZlib);

            //6 Удаляем строку прочь из файла
            newFile = ReplaceZlibHeader(newFile, HeaderText, " ");

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
            newFile = ReplaceZlibHeader(newFile, "", header);

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
        public static void ExtractAllRPA(string directory)
        {     
            var RPAFiles = RPAFilesInDirectory(directory);
            foreach (var rpa in RPAFiles)
            {
                ExtractArchive(rpa);
            }
        }

        public static string[] ListFromDirectory(string directory)
        {
            var list = new List<string>();
            var data = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            if (data != null)
            {
                foreach (var d in data)
                {
                    Console.WriteLine(d.Replace(directory + "\\", ""));
                    list.Add(d.Replace(directory + "\\", ""));
                }
            }
            return list.ToArray();

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

        static int index = 0;
        public static async void ExtractArchive(string path)
        {
            //Стандартная кодировка Windows 1251
            Encoding enCode = Encoding.GetEncoding(1252);
            string[] files = Regex.Split(File.ReadAllText(path, enCode), "Made with Ren'Py.", RegexOptions.Multiline);
            Console.WriteLine("Найдено файлов : " + (files.Length - 1));

            //Получаем индексы и заголовок файла
            ExtractZlibHeader(path);

            var indexFile = 0;
            var fileName = "";
            var directory = Environment.CurrentDirectory + "\\.rpy\\";
            var directoryName = directory.Split('/')[directory.Split('/').Length - 1];
            var filePath = "\\m\\";

            Console.WriteLine(directoryName);
            foreach (var f in files)
            {
                //Если первая строка содержит RPA-X-X то пропустить данный 'файл'
                if (f.StartsWith("RPA")) continue;

                await Task.Delay(1);
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

        static string[] GetAllFileName(string textfile)
        {
            Encoding enCode = Encoding.GetEncoding(1252);
            var files = Regex.Split(File.ReadAllText(textfile, enCode), "Made with Ren'Py.", RegexOptions.Multiline);
            List<string> fileNames = new List<string>();

            int indexOfLenght = 2;
            foreach (var file in files)
            {
                if (file.Length < 150) continue;
                var f = file.Substring(0, 150);

                //if (f.StartsWith("#") || f.StartsWith("RPA")) continue;
                for (int i = 1; i < indexOfLenght; i++)
                {
                    if (f.Split('=').Length > i)
                    {
                        builder.AppendLine(f + "\n");
                        Console.WriteLine("Имя файла : " + i + " : " + f.Split('=')[i]);
                        fileNames.Add(f.Split('=')[i]);
                    }
                    else
                    {
                        builder.AppendLine(f + "\n\n");
                        Console.WriteLine("Имя файла : 0 : " + f.Split('=')[0]);
                        fileNames.Add(f.Split('=')[0]);
                    }

                }

            }
            builder.AppendLine("================================================");
            builder.AppendLine("======" + " FilesCount : " + fileNames.Count + "======");
            builder.AppendLine("=================================================");
            Console.WriteLine("Найдено файлов : " + fileNames.Count);

            //CreateFilesLog();
            return fileNames.ToArray();
        }

        static void ExtractZlibHeader(string path)
        {
            var key = 0; version = 3;
            indexes = null;

            var code = EncodingType.GetType(path);
            Console.WriteLine("\nФайл         : " + path.Split('\\')[path.Split('\\').Length-1]);
            Console.WriteLine("Кодировка    : " + code.HeaderName);

            FileStream fileStream = new FileStream(path, FileMode.Open);
            BinaryReader br = new BinaryReader(fileStream);
            StreamReader reader = new StreamReader(br.BaseStream, true);
            var fs = br.BaseStream; 

            fs.Seek(0, SeekOrigin.Current);
            if (version == 2 || version == 3 || version == 3.2)
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
                string final = DeDecompressToString(bytesOfFile);
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

                        tempIndexes.Add(s);
                        Console.WriteLine(s);
                    }
                    
                }

                Console.WriteLine("Количество совпадений : " + tempIndexes.Count);
                tempIndexes.Sort(StringComparer.OrdinalIgnoreCase);
                indexes = tempIndexes.ToArray();
                fs.Close(); br.Close();
                fileStream.Close();
                Offset = offset;
                HeaderText = final;
            }
        } 


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

                zipOut.Write(buffer, 0, buffer.Length);
                zipOut.finish();

                memOutput.Seek(0, SeekOrigin.Begin);
                byte[] result = memOutput.ToArray();

                var str = Encoding.Default.GetString(result);
                return str;
            }
            catch
            {
                string s = Encoding.Default.GetString(buffer);
                return s;
            }
            return "";
        }
        
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
            return "";
        }

        public static int ModifyZlibOffset(int zlibOffset, string[] newFiles, string oldHeader)
        {
            var lenght = zlibOffset;
            foreach (var file in newFiles)
            {
                lenght += file.Length;
            }
            return lenght-oldHeader.Length;
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
