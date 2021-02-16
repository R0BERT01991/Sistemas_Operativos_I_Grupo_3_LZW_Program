using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LZW_Program
{
    class Program
    {
        //maimxum bits allowed to read
        private const int MAX_BITS = 14;

        //hash bit to use with the hasing algorithm to find correct index
        private const int HASH_BIT = MAX_BITS - 8;

        //max value allowed based on max bits
        private const int MAX_VALUE = (1 << MAX_BITS) - 1;

        //max code possible
        private const int MAX_CODE = MAX_VALUE - 1;

        //must be bigger than the maximum allowed by maxbits and prime
        private const int TABLE_SIZE = 18041;

        //code table
        private static int[] _iaCodeTable = new int[TABLE_SIZE];

        //prefix table
        private static int[] _iaPrefixTable = new int[TABLE_SIZE];

        //character table
        private static int[] _iaCharTable = new int[TABLE_SIZE];

        //bit buffer to temporarily store bytes read from the files
        private static ulong _iBitBuffer;

        //counter for knowing how many bits are in the bit buffer
        private static int _iBitCounter;

        static void test()
        {
            // command:
            // lzw -c C:\Users\Acer\Downloads\pic.jpg
            // lzw -d C:\Users\Acer\Downloads\pic.jpg.lzw

            // TEST WITH ONE FILE
            /*
            string InputPath = @"C:\Users\Acer\Downloads\pic.jpg";
            string OutputPath = @"C:\Users\Acer\Downloads\pic.jpg.lzw";

            CompressOneFile(InputPath, OutputPath);
            Console.Write("\n CompressOneFile DONE");
            Console.ReadLine();

            DecompressOneFile(OutputPath, InputPath);
            Console.Write("\n DecompressOneFile DONE");
            Console.ReadLine();
            */

            /*
            // TEST WITH MULTIPLE FILES #1
            List<string> InputPath = new List<string>();
            InputPath.Add(@"C:\Users\Acer\Downloads\pic.jpg");

            List<string> OutputPath = new List<string>();
            OutputPath.Add(@"C:\Users\Acer\Downloads\pic.jpg.lzw");

            CompressMultipleFiles(InputPath, OutputPath);
            Console.Write("\n CompressMultipleFiles #1 DONE");
            Console.ReadLine();

            DecompressOneFile(OutputPath[0], InputPath[0]);
            Console.Write("\n DecompressMultipleFiles #1 DONE");
            Console.ReadLine();
            */

            /*
            // TEST WITH MULTIPLE FILES #2
            List<string> InputPath = new List<string>();
            InputPath.Add(@"C:\Users\Acer\Downloads\pic.jpg");
            InputPath.Add(@"C:\Users\Acer\Downloads\pic1.jpg");
            InputPath.Add(@"C:\Users\Acer\Downloads\pic2.jpg");
            InputPath.Add(@"C:\Users\Acer\Downloads\pic3.jpg");

            List<string> OutputPath = new List<string>();
            OutputPath.Add(@"C:\Users\Acer\Downloads\result.lzw");
            OutputPath.Add(@"C:\Users\Acer\Downloads\pic3.jpg.lzw");


            CompressMultipleFiles(InputPath, OutputPath);
            Console.Write("\n CompressMultipleFiles #2 DONE");
            Console.ReadLine();

            DecompressMultipleFiles(OutputPath, InputPath);
            Console.Write("\n DecompressMultipleFiles #2 DONE");
            Console.ReadLine();
            */
        }

        static void Main(string[] args)
        {
            string text;

            do
            {
                Console.Write("\n command: ");
                text = Console.ReadLine();

                string command = text.Substring(0, 3);

                if (command.Trim() == "lzw")
                {
                    if (text[4].ToString().Trim() == "-")
                    {
                        if (text[5].ToString().Trim() == "c")
                        {
                            string compressInputPath = text.Substring(7);

                            if (File.Exists(compressInputPath.Trim()) == true)
                            {
                                string compressOutputPath = compressInputPath + ".lzw";
                                CompressOneFile(compressInputPath, compressOutputPath);
                            }

                            else
                            {
                                Console.WriteLine("\n The file doesnt exits");
                            }
                        }

                        else if (text[5].ToString().Trim() == "d")
                        {
                            string decompressInputPath = text.Substring(7);

                            if (File.Exists(decompressInputPath.Trim()))
                            {
                                string decompressOutputDirectory = Path.GetDirectoryName(decompressInputPath);
                                string decompressOutputFile = Path.GetFileNameWithoutExtension(decompressInputPath);
                                string decompressOutputPath = decompressOutputDirectory + @"\" + decompressOutputFile;
                                DecompressOneFile(decompressInputPath, decompressOutputPath);
                            }

                            else
                            {
                                Console.WriteLine("\n The file doesnt exits");
                            }
                        }

                        else if (text[5].ToString().Trim() == "h")
                        {
                            Console.WriteLine("\n Help: ");
                            Console.WriteLine("\n Compress file: lzw -c <filepath> ");
                            Console.WriteLine("\n example : lzw -c " + @"C:\Users\Acer\Downloads\pic.jpg ");
                            Console.WriteLine();
                            Console.WriteLine("\n Decompress file: lzw -d <filepath> ");
                            Console.WriteLine("\n example " + @"lzw -d C:\Users\Acer\Downloads\pic.jpg.lzw");
                        }

                        else
                        {
                            Console.WriteLine("\n Invalid flag");
                            Console.WriteLine("\n Please try lzw -h for help");
                        }
                    }

                    else
                    {
                        Console.WriteLine("\n No flag input");
                        Console.WriteLine("\n Please try lzw -h for help");
                    }
                }

                else
                {
                    Console.WriteLine("\n Invalid command");
                    Console.WriteLine("\n Please try lzw -h for help");
                }

                Console.Write("\n Press any key to continue...");
                Console.ReadLine();
                Console.Clear();
            }
            while (text != "exit");
        }

        // ONE FILE

        //used to blank  out bit buffer incase this class is called to comprss and decompress from the same instance
        private static void InitializeOneFile()
        {
            _iBitBuffer = 0;
            _iBitCounter = 0;
        }

        public static bool CompressOneFile(string pInputFileName, string pOutputFileName)
        {
            Stream reader = null;
            Stream writer = null;

            try
            {
                InitializeOneFile();
                reader = new FileStream(pInputFileName, FileMode.Open);
                writer = new FileStream(pOutputFileName, FileMode.Create);

                int iNextCode = 256;
                int iChar = 0;
                int iString = 0;
                int iIndex = 0;

                //blank out table
                for (int i = 0; i < TABLE_SIZE; i++)
                {
                    _iaCodeTable[i] = -1;
                }

                //get first code, will be 0-255 ascii char
                iString = reader.ReadByte();

                //read until we reach end of file
                while ((iChar = reader.ReadByte()) != -1)
                {
                    //get correct index for prefix+char
                    iIndex = FindMatchOneFile(iString, iChar);

                    //set string if we have something at that index
                    if (_iaCodeTable[iIndex] != -1)
                    {
                        iString = _iaCodeTable[iIndex];
                    }

                    //insert new entry
                    else
                    {
                        //otherwise we insert into the tables
                        if (iNextCode <= MAX_CODE)
                        {
                            //insert and increment next code to use
                            _iaCodeTable[iIndex] = iNextCode++;
                            _iaPrefixTable[iIndex] = iString;
                            _iaCharTable[iIndex] = (byte)iChar;
                        }

                        //output the data in the string
                        WriteCodeOneFile(writer, iString);
                        iString = iChar;
                    }
                }

                //output last code
                WriteCodeOneFile(writer, iString);

                //output end of buffer
                WriteCodeOneFile(writer, MAX_VALUE);

                //flush
                WriteCodeOneFile(writer, 0);
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                if (writer != null)
                {
                    writer.Close();
                }

                File.Delete(pOutputFileName);
                return false;
            }

            finally
            {

                if (reader != null)
                {
                    reader.Close();
                }

                if (writer != null)
                {
                    writer.Close();
                }
            }

            return true;
        }

        private static int FindMatchOneFile(int pPrefix, int pChar)
        {
            int index = 0;
            int offset = 0;

            index = (pChar << HASH_BIT) ^ pPrefix;

            offset = (index == 0) ? 1 : TABLE_SIZE - index;

            while (true)
            {
                if (_iaCodeTable[index] == -1)
                {
                    return index;
                }

                if (_iaPrefixTable[index] == pPrefix && _iaCharTable[index] == pChar)
                {
                    return index;
                }

                index -= offset;

                if (index < 0)
                {
                    index += TABLE_SIZE;
                }
            }
        }

        private static void WriteCodeOneFile(Stream pWriter, int pCode)
        {
            //make space and insert new code in buffer
            _iBitBuffer |= (ulong)pCode << (32 - MAX_BITS - _iBitCounter);

            //increment bit counter
            _iBitCounter += MAX_BITS;

            //write all the bytes we can
            while (_iBitCounter >= 8)
            {
                int temp = (byte)((_iBitBuffer >> 24) & 255);

                //write byte from bit buffer
                pWriter.WriteByte((byte)((_iBitBuffer >> 24) & 255));

                //remove written byte from buffer
                _iBitBuffer <<= 8;

                //decrement counter
                _iBitCounter -= 8;
            }
        }

        public static bool DecompressOneFile(string pInputFileName, string pOutputFileName)
        {
            Stream reader = null;
            Stream writer = null;

            try
            {
                InitializeOneFile();

                reader = new FileStream(pInputFileName, FileMode.Open);
                writer = new FileStream(pOutputFileName, FileMode.Create);

                int iNextCode = 256;
                int iNewCode;
                int iOldCode;
                byte bChar;
                int iCurrentCode;
                int iCounter;
                byte[] baDecodeStack = new byte[TABLE_SIZE];

                iOldCode = ReadCodeOneFile(reader);
                bChar = (byte)iOldCode;

                //write first byte since it is plain ascii
                writer.WriteByte((byte)iOldCode);

                iNewCode = ReadCodeOneFile(reader);

                //read file all file
                while (iNewCode != MAX_VALUE)
                {
                    if (iNewCode >= iNextCode)
                    {
                        //fix for prefix+chr+prefix+char+prefx special case
                        baDecodeStack[0] = bChar;
                        iCounter = 1;
                        iCurrentCode = iOldCode;
                    }
                    else
                    {
                        iCounter = 0;
                        iCurrentCode = iNewCode;
                    }

                    //decode string by cycling back through the prefixes
                    while (iCurrentCode > 255)
                    {
                        //lstDecodeStack.Add((byte)_iaCharTable[iCurrentCode]);
                        //iCurrentCode = _iaPrefixTable[iCurrentCode];
                        baDecodeStack[iCounter] = (byte)_iaCharTable[iCurrentCode];
                        ++iCounter;

                        if (iCounter >= MAX_CODE)
                        {
                            throw new Exception("oh crap");
                        }

                        iCurrentCode = _iaPrefixTable[iCurrentCode];
                    }

                    baDecodeStack[iCounter] = (byte)iCurrentCode;

                    //set last char used
                    bChar = baDecodeStack[iCounter];

                    //write out decodestack
                    while (iCounter >= 0)
                    {
                        writer.WriteByte(baDecodeStack[iCounter]);
                        --iCounter;
                    }

                    //insert into tables
                    if (iNextCode <= MAX_CODE)
                    {
                        _iaPrefixTable[iNextCode] = iOldCode;
                        _iaCharTable[iNextCode] = bChar;
                        ++iNextCode;
                    }

                    iOldCode = iNewCode;

                    //if (reader.PeekChar() != 0)
                    iNewCode = ReadCodeOneFile(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                if (writer != null)
                {
                    writer.Close();
                }

                File.Delete(pOutputFileName);

                return false;
            }

            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }

                if (writer != null)
                {
                    writer.Close();
                }
            }

            return true;
        }

        private static int ReadCodeOneFile(Stream pReader)
        {
            uint iReturnVal;

            //fill up buffer
            while (_iBitCounter <= 24)
            {
                //insert byte into buffer
                _iBitBuffer |= (ulong)pReader.ReadByte() << (24 - _iBitCounter);

                //increment counter
                _iBitCounter += 8;
            }

            //get last byte from buffer so we can return it
            iReturnVal = (uint)_iBitBuffer >> (32 - MAX_BITS);

            //remove it from buffer
            _iBitBuffer <<= MAX_BITS;

            //decrement bit counter
            _iBitCounter -= MAX_BITS;

            int temp = (int)iReturnVal;
            return temp;
        }


        // MULTIPLE FILES
        private static void InitializeMultipleFiles()
        {
            _iBitBuffer = 0;
            _iBitCounter = 0;
        }

        public static bool CompressMultipleFiles(List<string> pInputFileName, List<string> pOutputFileName)
        {
            List<Stream> reader = new List<Stream>();
            List<Stream> writer = new List<Stream>();

            try
            {
                InitializeMultipleFiles();

                for (int i = 0; i < pInputFileName.Count; i++)
                {
                    reader.Add(new FileStream(pInputFileName[i], FileMode.Open));
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    writer.Add(new FileStream(pOutputFileName[i], FileMode.Create));
                }

                int iNextCode = 256;
                int iChar = 0;
                //List<int> iString = new List<int>();
                int iString = 0;
                int iIndex = 0;

                //blank out table
                for (int i = 0; i < TABLE_SIZE; i++)
                {
                    _iaCodeTable[i] = -1;
                }

                //get first code, will be 0-255 ascii char

                for (int i = 0; i < reader.Count; i++)
                {
                    //iString.Add(reader[i].ReadByte());
                    iString = reader[i].ReadByte();
                }

                //read until we reach end of file
                for (int i = 0; i < reader.Count; i++)
                {
                    while ((iChar = reader[i].ReadByte()) != -1)
                    {
                        //get correct index for prefix+char
                        //iIndex = FindMatchMultipleFiles(iString[i], iChar)
                        iIndex = FindMatchMultipleFiles(iString, iChar);

                        //set string if we have something at that index
                        if (_iaCodeTable[iIndex] != -1)
                        {
                            //iString[i] = _iaCodeTable[iIndex]
                            iString = _iaCodeTable[iIndex];
                        }

                        //insert new entry
                        else
                        {
                            //otherwise we insert into the tables
                            if (iNextCode <= MAX_CODE)
                            {
                                //insert and increment next code to use
                                _iaCodeTable[iIndex] = iNextCode++;
                                //_iaPrefixTable[iIndex] = iString[i];
                                _iaPrefixTable[iIndex] = iString;
                                _iaCharTable[iIndex] = (byte)iChar;
                            }

                            //output the data in the string
                            //WriteCodeMultipleFiles(writer[i], iString[i]);
                            WriteCodeMultipleFiles(writer[i], iString);

                            //iString.Add(iChar);
                            iString = iChar;
                        }
                    }
                }

                for (int i = 0; i < writer.Count; i++)
                {
                    //output last code
                    //WriteCodeMultipleFiles(writer[i], iString[i]);
                    WriteCodeMultipleFiles(writer[i], iString);

                    //output end of buffer
                    WriteCodeMultipleFiles(writer[i], MAX_VALUE);

                    //flush
                    WriteCodeMultipleFiles(writer[i], 0);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

                for (int i = 0; i < writer.Count; i++)
                {
                    if (writer != null)
                    {
                        writer[i].Close();
                    }
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    File.Delete(pOutputFileName[i]);
                }

                return false;
            }

            finally
            {
                for (int i = 0; i < reader.Count; i++)
                {
                    if (reader != null)
                    {
                        reader[i].Close();
                    }
                }

                for (int i = 0; i < writer.Count; i++)
                {
                    if (writer != null)
                    {
                        writer[i].Close();
                    }
                }
            }

            return true;
        }

        private static int FindMatchMultipleFiles(int pPrefix, int pChar)
        {
            int index = 0;
            int offset = 0;

            index = (pChar << HASH_BIT) ^ pPrefix;

            offset = (index == 0) ? 1 : TABLE_SIZE - index;

            while (true)
            {
                if (_iaCodeTable[index] == -1)
                {
                    return index;
                }

                if (_iaPrefixTable[index] == pPrefix && _iaCharTable[index] == pChar)
                {
                    return index;
                }

                index -= offset;

                if (index < 0)
                {
                    index += TABLE_SIZE;
                }
            }
        }

        private static void WriteCodeMultipleFiles(Stream pWriter, int pCode)
        {
            //make space and insert new code in buffer
            _iBitBuffer |= (ulong)pCode << (32 - MAX_BITS - _iBitCounter);

            //increment bit counter
            _iBitCounter += MAX_BITS;

            //write all the bytes we can
            while (_iBitCounter >= 8)
            {
                int temp = (byte)((_iBitBuffer >> 24) & 255);

                //write byte from bit buffer
                pWriter.WriteByte((byte)((_iBitBuffer >> 24) & 255));

                //remove written byte from buffer
                _iBitBuffer <<= 8;

                //decrement counter
                _iBitCounter -= 8;
            }
        }

        public static bool DecompressMultipleFiles(List<string> pInputFileName, List<string> pOutputFileName)
        {
            List<Stream> reader = new List<Stream>();
            List<Stream> writer = new List<Stream>();

            try
            {
                InitializeMultipleFiles();

                for (int i = 0; i < pInputFileName.Count; i++)
                {
                    reader.Add(new FileStream(pInputFileName[i], FileMode.Open));
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    writer.Add(new FileStream(pOutputFileName[i], FileMode.Create));
                }

                List<int> iNextCode = new List<int>();
                List<int> iNewCode = new List<int>();
                List<int> iOldCode = new List<int>();
                List<byte> bChar = new List<byte>();
                int iCurrentCode;
                int iCounter;
                byte[] baDecodeStack = new byte[TABLE_SIZE];

                for (int i = 0; i < reader.Count; i++)
                {
                    iNextCode.Add(256);
                }

                for (int i = 0; i < reader.Count; i++)
                {
                    iOldCode.Add(ReadCodeMultipleFiles(reader[i]));
                    bChar.Add((byte)iOldCode[i]);
                }

                //write first byte since it is plain ascii
                for (int i = 0; i < writer.Count; i++)
                {
                    writer[i].WriteByte((byte)iOldCode[i]);
                }

                for (int i = 0; i < reader.Count; i++)
                {
                    iNewCode.Add(ReadCodeMultipleFiles(reader[i]));
                }

                for (int i = 0; i < reader.Count; i++)
                {
                    //read file all file
                    while (iNewCode[i] != MAX_VALUE)
                    {
                        if (iNewCode[i] >= iNextCode[i])
                        {
                            //fix for prefix+chr+prefix+char+prefx special case
                            baDecodeStack[0] = bChar[i];
                            iCounter = 1;
                            iCurrentCode = iOldCode[i];
                        }
                        else
                        {
                            iCounter = 0;
                            iCurrentCode = iNewCode[i];
                        }

                        //decode string by cycling back through the prefixes
                        while (iCurrentCode > 255)
                        {
                            //lstDecodeStack.Add((byte)_iaCharTable[iCurrentCode]);
                            //iCurrentCode = _iaPrefixTable[iCurrentCode];
                            baDecodeStack[iCounter] = (byte)_iaCharTable[iCurrentCode];
                            ++iCounter;

                            if (iCounter >= MAX_CODE)
                            {
                                throw new Exception("oh crap");
                            }

                            iCurrentCode = _iaPrefixTable[iCurrentCode];
                        }

                        baDecodeStack[iCounter] = (byte)iCurrentCode;

                        //set last char used
                        bChar.Add(baDecodeStack[iCounter]);

                        //write out decodestack
                        while (iCounter >= 0)
                        {
                            writer[i].WriteByte(baDecodeStack[iCounter]);

                            --iCounter;
                        }

                        //insert into tables
                        if (iNextCode[i] <= MAX_CODE)
                        {
                            _iaPrefixTable[iNextCode[i]] = iOldCode[i];
                            _iaCharTable[iNextCode[i]] = bChar[i];
                            ++iNextCode[i];
                        }

                        iOldCode = iNewCode;

                        //if (reader.PeekChar() != 0)
                        iNewCode.Add(ReadCodeMultipleFiles(reader[i]));

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

                for (int i = 0; i < writer.Count; i++)
                {
                    if (writer[i] != null)
                    {
                        writer[i].Close();
                    }
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    File.Delete(pOutputFileName[i]);
                }

                return false;
            }

            finally
            {
                for (int i = 0; i < reader.Count; i++)
                {
                    if (reader[i] != null)
                    {
                        reader[i].Close();
                    }
                }

                for (int i = 0; i < writer.Count; i++)
                {

                    if (writer[i] != null)
                    {
                        writer[i].Close();
                    }
                }
            }

            return true;
        }

        private static int ReadCodeMultipleFiles(Stream pReader)
        {
            uint iReturnVal;

            //fill up buffer
            while (_iBitCounter <= 24)
            {
                //insert byte into buffer
                _iBitBuffer |= (ulong)pReader.ReadByte() << (24 - _iBitCounter);

                //increment counter
                _iBitCounter += 8;
            }

            //get last byte from buffer so we can return it
            iReturnVal = (uint)_iBitBuffer >> (32 - MAX_BITS);

            //remove it from buffer
            _iBitBuffer <<= MAX_BITS;

            //decrement bit counter
            _iBitCounter -= MAX_BITS;

            int temp = (int)iReturnVal;
            return temp;
        }
    }
}
