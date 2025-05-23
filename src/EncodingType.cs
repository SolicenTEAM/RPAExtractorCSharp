﻿using System;using System.IO;using System.Text;

class EncodingType
{
    public static Encoding GetType(string FILE_NAME)
    {
        FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read);
        Encoding r = GetType(fs);
        fs.Close();
        return r;
    }

    public static Encoding GetType(FileStream fs)
    {
        Encoding reVal = Encoding.Default; int i;
        BinaryReader r = new BinaryReader(fs, Encoding.Default);

        int.TryParse(fs.Length.ToString(), out i);
        byte[] ss = r.ReadBytes(i);
        if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
        {
            reVal = Encoding.UTF8;
        }
        else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
        {
            reVal = Encoding.BigEndianUnicode;
        }
        else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
        {
            reVal = Encoding.Unicode;
        }
        r.Close();
        return reVal;

    }

    private static bool IsUTF8Bytes(byte[] data)
    {
        int charByteCounter = 1;
        byte curByte;
        for (int i = 0; i < data.Length; i++)
        {
            curByte = data[i];
            if (charByteCounter == 1)
            {
                if (curByte >= 0x80)
                {
                    while (((curByte <<= 1) & 0x80) != 0)
                    {
                        charByteCounter++;
                    }

                    if (charByteCounter == 1 || charByteCounter > 6)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if ((curByte & 0xC0) != 0x80)
                {
                    return false;
                }
                charByteCounter--;
            }
        }
        if (charByteCounter > 1)
        {
            throw new Exception("Error byte format");
        }
        return true;
    }
}
