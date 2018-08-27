using System;
using System.Collections.Generic;
using System.Text;

namespace AirQuality
{
    public class SDS011
    {
        public void DoThings()
        {
            ////write message
            //        //set data reporting mode
            //        var writeBuf = new byte[]
            //        {
            //            0xAA, //0 Head
            //            0xB4, //1 Command ID
            //            0x2,  //2 Data byte 1 always 2
            //            0x00,  //3 Data byte 2 0 = query current mode, 1 = reporting mode
            //            0x00,  //4 Data byte 3 0 = report active mode, 1 = report query mode
            //            0x00,  //5 Data byte 4 reserved
            //            0x00,  //6 Data byte 5 reserved
            //            0x00,  //7 Data byte 6 reserved
            //            0x00,  //8 Data byte 7 reserved
            //            0x00,  //9 Data byte 8 reserved
            //            0x00,  //10 Data byte 9 reserved
            //            0x00,  //11 Data byte 10 reserved
            //            0x00,  //12 Data byte 11 reserved
            //            0x00,  //13 Data byte 12 reserved
            //            0x00,  //14 Data byte 13 reserved
            //            0xFF,  //15 Data byte 14 (ff all sensor, or device id byte 1)
            //            0xFF,  //16 Data byte 15 (ff all sensor, or device id byte 2)
            //            0x00,  //17 Checksum - Low 8bit of the sum result of Data Bytes（not including packet head, tail and Command ID）
            //            0xAB,  //18 Tail
            //        };
            //        port.Write(writeBuf,0,writeBuf.Length);
            //        Console.WriteLine($"Set data report mode");

            //        //discard until we get a head byte
            //        while (port.ReadByte() != 0xAA)
            //        {

            //        }

            //        //read response
            //        var dataBuf = new byte[19];
            //        dataBuf[0] = 0xAA;//we read that already

            //        port.Read(dataBuf, 1, dataBuf.Length -1);
            //        Console.WriteLine($"Read reporting mode response");
            //        for(var i = 0; i <= dataBuf.Length-1; i++)
            //        {
            //            Console.Write($"{dataBuf[i]:X2} ");
            //        }
            //        Console.WriteLine("");

            //        if (dataBuf[1] == 0xC5) //report response
            //        {
            //            PrintReportResponse(dataBuf);
            //        }
            //        if (dataBuf[1] == 0xC0) //query data response
            //        {
            //            PrintQueryDataResponse(dataBuf);
            //        }
        }
    }
}
