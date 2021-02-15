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

        static void Main(string[] args)
        {
            // command:
            // lzw -c C:\Users\Acer\Downloads\pic.jpg
            // lzw -d C:\Users\Acer\Downloads\pic.jpg.lzw

            List<string> compressInputPath = new List<string>();
            //compressInputPath.Add(@"C:\Users\Acer\Downloads\pic.jpg");
            compressInputPath.Add(@"C:\Users\Acer\Downloads\pic1.jpg");
            compressInputPath.Add(@"C:\Users\Acer\Downloads\pic2.jpg");
            //compressInputPath.Add(@"C:\Users\Acer\Downloads\pic3.jpg");

            List<string> compressOutputPath = new List<string>();
            compressOutputPath.Add(@"C:\Users\Acer\Downloads\result.lzw");
            //compressOutputPath.Add(@"C:\Users\Acer\Downloads\pic3.jpg.lzw");

            Compress(compressInputPath, compressOutputPath);
            //Decompress(compressOutputPath, compressInputPath);

            Console.Write("\n DONE");
            Console.ReadLine();

            /*
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
                                Compress(compressInputPath, compressOutputPath);
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
                                Decompress(decompressInputPath, decompressOutputPath);
                            }

                            else
                            {
                                Console.WriteLine("\n The file doesnt exits");
                            }
                        }

                        else if (text[5].ToString().Trim() == "h")
                        {

                        }

                        else
                        {
                            Console.WriteLine("\n Invalid flag");
                        }
                    }

                    else
                    {
                        Console.WriteLine("\n No flag input");
                    }
                }

                else
                {
                    Console.WriteLine("\n Invalid command");
                }

                Console.Write("\n Press any key to continue...");
                Console.ReadLine();
                Console.Clear();
            }
            while (text != "exit");*/
        }

        //used to blank  out bit buffer incase this class is called to comprss and decompress from the same instance
        private static void Initialize()
        {
            _iBitBuffer = 0;
            _iBitCounter = 0;
        }

        public static bool Compress(List<string> pInputFileName, List<string> pOutputFileName)
        {
            //List<Stream> reader = new List<Stream>();
            //List<Stream> writer = new List<Stream>();
            Stream reader = null;
            Stream writer = null;

            try
            {
                Initialize();

                for (int i = 0; i < pInputFileName.Count; i++)
                {
                    //reader.Add(new FileStream(pInputFileName[i], FileMode.Open));
                    reader = new FileStream(pInputFileName[i], FileMode.Open);
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    //writer.Add(new FileStream(pOutputFileName[i], FileMode.Create));
                    writer = new FileStream(pOutputFileName[i], FileMode.Create);
                }

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
                    iIndex = FindMatch(iString, iChar);

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
                        WriteCode(writer, iString);
                        iString = iChar;
                    }
                }

                //output last code
                WriteCode(writer, iString);

                //output end of buffer
                WriteCode(writer, MAX_VALUE);

                //flush
                WriteCode(writer, 0);
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

                if (writer != null)
                {
                    writer.Close();
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    File.Delete(pOutputFileName[i]);
                }

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

        private static int FindMatch(int pPrefix, int pChar)
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

        private static void WriteCode(Stream pWriter, int pCode)
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

        public static bool Decompress(List<string> pInputFileName, List<string> pOutputFileName)
        {
            //List<Stream> reader = new List<Stream>();
            //List<Stream> writer = new List<Stream>();
            Stream reader = null;
            Stream writer = null;

            try
            {
                Initialize();

                for (int i = 0; i < pInputFileName.Count; i++)
                {
                    //reader.Add(new FileStream(pInputFileName[i], FileMode.Open));
                    reader = new FileStream(pInputFileName[i], FileMode.Open);
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    //writer.Add(new FileStream(pOutputFileName[i], FileMode.Create));
                    writer = new FileStream(pOutputFileName[i], FileMode.Create);
                }

                int iNextCode = 256;
                int iNewCode;
                int iOldCode;
                byte bChar;
                int iCurrentCode;
                int iCounter;
                byte[] baDecodeStack = new byte[TABLE_SIZE];

                iOldCode = ReadCode(reader);
                bChar = (byte)iOldCode;

                //write first byte since it is plain ascii
                writer.WriteByte((byte)iOldCode);

                iNewCode = ReadCode(reader);

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
                    iNewCode = ReadCode(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                if (writer != null)
                {
                    writer.Close();
                }

                for (int i = 0; i < pOutputFileName.Count; i++)
                {
                    File.Delete(pOutputFileName[i]);
                }

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

        private static int ReadCode(Stream pReader)
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
