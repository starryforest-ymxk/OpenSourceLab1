//WanChen
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerForLinux
{
    internal class RecordMgr
    {

        public int maxNum = 5;
        public string[] nameA = new string[5];
        public string[] nameB = new string[5];
        public int[] time = new int[5];
        public string record;



        /// <summary>
        /// 开机读入已保存的数据
        /// </summary>
        public void ReadRecord()
        {
            StreamReader reader = new StreamReader("Record.txt");
            record = reader.ReadToEnd();

            Console.WriteLine($"Read game record:");

            string[] recordLine = record.Split(':');
            for (int i = 0; i < maxNum; i++)
            {
                string[] parts = recordLine[i].Split('/');
                nameA[i] = parts[0];
                nameB[i] = parts[1];
                time[i] = Convert.ToInt32(parts[2]);
                Console.WriteLine($"[{i+1}] Time: {time[i]}  PlayerA: {nameA[i]}  PlayerB: {nameB[i]}");
            }
            reader.Close();

        }


        /// <summary>
        /// 写入数据
        /// </summary>
        public void WriteRecord()
        {

            //文本文件写入流
            StreamWriter writer = new StreamWriter("Record.txt");
            //如果文件存在，那么文件会被覆盖,不存在就创建

            record = "";

            for (int i = 0; i < maxNum; i++)
            {
                record = record + nameA[i] + "/" + nameB[i] + "/" + time[i].ToString() + ":";
            }


            writer.Write(record);

            writer.Close();
        }



        /// <summary>
        /// 返回最新的纪录
        /// </summary>
        /// <returns></returns>
        public string GetLatestRecord()
        {
            return record;
        }




        /// <summary>
        /// 放入纪录
        /// </summary>
        /// <param name="newRecord"></param>
        public void PutInRecord(string newRecord)
        {

            record = "";

            string[] strings = newRecord.Split('/');
            nameA[4] = strings[0];
            nameB[4] = strings[1];
            time[4] = Convert.ToInt32(strings[2]);

            for (int i = 0; i < time.Length - 1; i++)
            {
                for (int j = 0; j < time.Length - 1 - i; j++)
                {
                    if (time[j] > time[j + 1])
                    {
                        int temp = time[j];
                        string tempa = nameA[j];
                        string tempb = nameB[j];

                        time[j] = time[j + 1];
                        nameA[j] = nameA[j + 1];
                        nameB[j] = nameB[j + 1];

                        time[j + 1] = temp;
                        nameA[j + 1] = tempa;
                        nameB[j + 1] = tempb;

                    }
                }
            }

            WriteRecord();

        }

    }

}
