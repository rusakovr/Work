using System.IO;
using System.Collections;
using  System.Text.RegularExpressions;
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace sig_crt
{
    // Структура неисправности
    public  struct modR
    {
        public bool up;        /// Бесконечный резистор или в ноль
        public bool type;        /// Резистор или конденсатор
        public int pos;        /// Номер строки нужного резистора
        public bool IsDev;       /// Есть ли отклонение по методу чувствительности                
        public modR(bool tup = false, int tpos = 0, bool ttype = false, bool tIsDev = false)
        {
            up = tup;
            pos = tpos;
            type = ttype;
            IsDev = tIsDev;
        }

    };

    // Структура данных для анализа
    public struct Readings
    {
        public string name;		// имя
        public double data;		// данные 
    };

    // Описание вариации
    public struct RandC
    {
        public int pos;            /// Позиция
        public string name;       /// Название компонента
        public double bot;
        public double top;
        public double mid;
        public RandC(int tpos=0,double tbot=0,double tmid=0,double ttop=0,string tname="")
        {
            pos=tpos;
            mid = tmid;
            bot = tbot;
            top = ttop;
            name=tname;
        }
  
    };

    // Показания измерителя
    public struct SgnParam
    {
        public SgnParam(string tname=" ",double ttop = 0, double tmid = 0, double tbot = 0)
        {
            top = ttop;
            mid = tmid;
            bot = tbot;
            name = tname;
        }
        public double top, mid, bot;
        public string name;
    };

    // Класс для анализа файла netlist и создания сигнатуры заданной неисправности
    public class model_block
    {
        public static int cnt;
        public float NDIV;
        string netlistName;              /// Файл с тестовым нетлист
        string test_netlistName;         /// Нетлист с неисправностью
        string resultName;               /// Файл с временными результатами моделирования
        public List<modR> faults;                  /// Неисправности
        public List<string> name_of_faults;    /// Имена
        public List<RandC> variations;         /// Вариации значений
        List<string> name_of_AM;        /// Наименования амперметров
        public List<string> name_of_VM;        /// Нименования вольтметров
        NumberFormatInfo nfi;
        int ac;                         /// Номер строки .ac
        int dc;                         /// Номер строки .dc
        int timer;                      /// Номер строки .tr
        string tempRes;                             /// 
        double start;
        double stop;
        double tol_mode;
        bool af_flag;
        double L_max;
        double freq_max;
        int time_points_number;
        int freq_points_number;
        List<double> timePoints;
        List<double> freqPoints;
        public bool modelFault;
        // Метод для получения значения приставки размерности 
        double mean_of_forward(char pre)
        {
            switch(pre)
            {
                // Положительные
                case 'h': return Math.Pow(10.0,2.0);
                case 'k': return Math.Pow(10.0,3.0);
                case 'M': return Math.Pow(10.0,6.0);
                case 'G': return Math.Pow(10.0,9.0);
                case 'Т': return Math.Pow(10.0,12.0);
                case 'П': return Math.Pow(10.0,15.0);
                case 'E': return Math.Pow(10.0,18.0);
                case 'З': return Math.Pow(10.0,21.0);
                case 'И': return Math.Pow(10.0,24.0);

                // Отрицательные
                case 'd': return Math.Pow(10.0,-1.0);
                case 'c': return Math.Pow(10.0,-2.0);
                case 'm': return Math.Pow(10.0,-3.0);
                case 'u': return Math.Pow(10.0,-6.0);
                case 'n': return Math.Pow(10.0,-9.0);
                case 'p': return Math.Pow(10.0,-12.0);
                case 'f': return Math.Pow(10.0,-15.0);
                case 'a': return Math.Pow(10.0,-18.0);
                case 'z': return Math.Pow(10.0,-21.0);
                case 'y': return Math.Pow(10.0,-24.0);
            }
            return 1.0;
        }

        // Конструктор - инициализирует имена обрабатываемых файлов
        public model_block(string tnetlist,             // Имя исходного файла netlist
                            string ttest_netlist="test_netlist.txt",       // Имя файла, в которм будет хранится обработанный nrtlist
                            string tresult="results.txt",             // Имя файла, в который будут писаться промежуточные результаты моделирования
                            string TFile="faults.txt",
                            float tNDIV=0)
        {
            cnt = 0;
            NDIV = tNDIV;
            netlistName=tnetlist;
            test_netlistName=ttest_netlist;
            resultName=tresult;
            tempRes = TFile;
            af_flag = false;
            name_of_faults = new List<string>();
            timePoints = new List<double>();
            freqPoints = new List<double>();
            faults = new List<modR>();
            variations=new List<RandC> ();         /// Вариации значений
            name_of_AM=new List<string>();        /// Наименования амперметров
            name_of_VM=new List<string> ();        /// Нименования вольтметров
            nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.CurrencyDecimalSeparator = ".";
            time_points_number = 0;
            freq_points_number = 0;
            L_max = 0;
            freq_max = 0;
            start = 0;
            stop = 0;
            modelFault = false;
            tol_mode = -1;
        }            
        
        // Имя файла, в котором будут хранится модифицированные нетлисты с неисправностями
        ~model_block()
        {
        }

        // Метод, возвращающий списрк  имён полученных неисправностей
        List<string> get_names_of_faults(){return name_of_faults;}

        
        // Метод для установки режима определения допусков( если <0, то допуски определяются 
        // на основе статистических методов, если положительное число, то отклонение на m процентов 
        public void SetTolMode(double m)
        {
            tol_mode = m;
        }

        // Анализ netlist и поиск возможных неисправностей
        public void analiz_file()                                          
        {
            string line;                                                         // Очередная строка
            List<string> strl=new List<string>();                                 // Для узлов, с которыми необходимо поработать
            string wtisit;                                                       // С чем работаем
            int n=0;                                                             // Номер для теста
            FileStream netlist = new FileStream(netlistName,FileMode.Open);
            FileStream test_netlist = new FileStream(test_netlistName,FileMode.Create);
            int num_of_line=0;                                                  // Номер текущей линии
            StreamReader NetRead=new StreamReader(netlist);
            StreamWriter NetWrite=new StreamWriter(test_netlist);
            while (NetRead.EndOfStream!=true)
            {
                line = NetRead.ReadLine();
                num_of_line++;
                strl.Clear();
                wtisit=line.Split(':')[0];
                //Console.WriteLine(wtisit);
                // Выделить и переименовать узлы
                if(wtisit=="BJT"||wtisit=="Sub"||wtisit=="OpAmp")
                {
                    strl.AddRange(line.Split(' '));
                    //string templine;              // Формируем линию с переименнованными узлами
                    System.Text.StringBuilder templine = new System.Text.StringBuilder();
                    for(int i=0;i<strl.Count;i++)
                    {
                        if(Regex.IsMatch(strl[i],"[:=\"]+"))
                        {
                            if(!strl[i].Contains("\n"))
                                templine.Append(strl[i]+' ');
                            else
                                templine.Append(strl[i]);
                            strl.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            if(i==3&&wtisit=="BJT")
                            {
                                templine.Append("test_net"+(n-2).ToString()+' ');
                                continue;
                            }
                            templine.Append("test_net"+n.ToString()+' ');
                            string newR="R:test_R"+n.ToString()+" test_net"+n.ToString()+' '+strl[i]+" R=\"0.001 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n";
                            modR tmp=new modR (true,num_of_line++);
                            //Console.WriteLine(tmp.pos + " ?");
                            faults.Add(tmp);
                            NetWrite.Write(newR);
                            n++;
                        }
                    }
                    NetWrite.WriteLine(templine.ToString());
                    if(wtisit=="Sub")
                    {
                        foreach(string str in strl)
                        {
                            name_of_faults.Add("Разрыв вывода "+str+" элемента"+line.Split(' ')[0].Split(':')[1]);
                        }
                    }
                    if(wtisit=="BJT")
                   {
                        name_of_faults.Add("Разрыв на базе транзистора " + line.Split(' ')[0].Split(':')[1]);
                        name_of_faults.Add("Разрыв на коллекторе транзистора " + line.Split(' ')[0].Split(':')[1]);
                        name_of_faults.Add("Разрыв на эмиттере транзистора " + line.Split(' ')[0].Split(':')[1]);

                        // И добавим специфику транзистора - замыкания
                        num_of_line++;
                        name_of_faults.Add("Замыкание между базой и коллектором транзистора " + line.Split(' ')[0].Split(':')[1]);
                        NetWrite.Write("R:test_R" + (n++).ToString() + " test_net" + (n - 4).ToString() + " test_net" + (n - 3).ToString() + " R=\"10e99 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n");
                        modR tmp = new modR(false, num_of_line++);
                        //Console.WriteLine("R:test_R" + (n).ToString() + " test_net" + (n - 3). ToString() + " test_net" + (n - 2).ToString() + " R=\"10e99 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n");
                        faults.Add(tmp);

                        name_of_faults.Add("Замыкание между коллектором и эмиттером транзистора " + line.Split(' ')[0].Split(':')[1]);
                        NetWrite.Write("R:test_R" + (n++).ToString() + " test_net" + (n - 4).ToString() + " test_net" + (n - 3).ToString() + " R=\"10e99 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n");
                        modR tmp2 = new modR(false, num_of_line++);
                        //Console.WriteLine("R:test_R" + (n).ToString() + " test_net" + (n - 3).ToString() + " test_net" + (n - 2).ToString() + " R=\"10e99 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n");
                        faults.Add(tmp2);

                        name_of_faults.Add("Замыкание между базой и эмиттером транзистора " + line.Split(' ')[0].Split(':')[1]);
                        NetWrite.Write("R:test_R" + (n++).ToString() + " test_net" + (n - 4).ToString() + " test_net" + (n - 6).ToString() + " R=\"10e99 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n");
                        modR tmp3 = new modR(false, num_of_line);

                        faults.Add(tmp3);
                        //Console.WriteLine("R:test_R" + (n).ToString() + " test_net" + (n - 5).ToString() + " test_net" + (n - 3).ToString() + " R=\"10e99 Ohm\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"\n");

                    }
                    if (wtisit == "OpAmp")
                    {
                        name_of_faults.Add("Разрыв на инвертирующем выводе ОУ " + line.Split(' ')[0].Split(':')[1]);
                        name_of_faults.Add("Разрыв на неинвертирующем выводе ОУ " + line.Split(' ')[0].Split(':')[1]);
                        name_of_faults.Add("Разрыв на выходе ОУ " + line.Split(' ')[0].Split(':')[1]);
                    }
                    continue;
                }
                else NetWrite.WriteLine(line);
                if (wtisit == "IProbe")
                {
                    name_of_AM.Add(line.Split(' ')[0].Split(':')[1]);
                    continue;
                }
                if (wtisit == "VProbe")
                {
                    name_of_VM.Add(line.Split(' ')[0].Split(':')[1]);
                    continue;
                }
                if (wtisit == "R")
                {
                    n += 2;
                    string temp_name = line.Split(' ')[0].Split(':')[1];
                    name_of_faults.Add("Короткое замыкание резистора " + temp_name);
                    name_of_faults.Add("Холостой ход резистора " + temp_name);
                    modR tmp4 = new modR(false, num_of_line);
                    faults.Add(tmp4);
                    //Console.WriteLine(tmp4.pos + line);
                    modR tmp5 = new modR(true, num_of_line);
                    //Console.WriteLine(tmp5.pos + line);
                    faults.Add(tmp5);
                    if (NDIV != 0)
                    {
                        name_of_faults.Add("Отклонение номинала резистора " + temp_name + " на +" + NDIV + " %");
                        name_of_faults.Add("Отклонение номинала резистора " + temp_name + " на -" + NDIV + " %");
                        modR dtmp3 = new modR(true, num_of_line, false, true);
                        modR dtmp4 = new modR(false, num_of_line, false, true);
                        faults.Add(dtmp3);
                        faults.Add(dtmp4);
                    }
                    int temp1 = line.IndexOf("R=\"");
                    int temp2 = line.IndexOf('\"', temp1 + 3);
                    double vr = Convert.ToDouble(line.Substring(temp1 + 3, temp2 - temp1 - 3).Split(' ')[0], nfi) * mean_of_forward(line[temp2 - 4]);
                    RandC tmp6 = new RandC(num_of_line, vr, vr, vr, temp_name);
                    //Console.WriteLine(tmp6.name + " " + num_of_line);
                    variations.Add(tmp6);
                    continue;
                }
                if (wtisit == "C")
                {
                    n += 2;
                    string temp_name = line.Split(' ')[0].Split(':')[1];
                    name_of_faults.Add("Пробой конденсатора " + temp_name);
                    name_of_faults.Add("Разрыв в конденсаторе  " + temp_name);

                    modR tmp4 = new modR(false, num_of_line, true);
                    //Console.WriteLine(tmp4.pos + line);
                    faults.Add(tmp4);
                    modR tmp5 = new modR(true, num_of_line, true);
                    //Console.WriteLine(tmp5.pos + line);
                    faults.Add(tmp5);
                    if (NDIV != 0)
                    {
                        name_of_faults.Add("Отклонение номинала конленсатора " + temp_name + " на +" + NDIV + " %");
                        name_of_faults.Add("Отклонение номинала конленсатора " + temp_name + " на -" + NDIV + " %");
                        modR dtmp1 = new modR(true, num_of_line, true, true);
                        modR dtmp2 = new modR(false, num_of_line, true, true);
                        faults.Add(dtmp1);
                        faults.Add(dtmp2);
                    }
                    int temp1 = line.IndexOf("C=\"");
                    int temp2 = line.IndexOf('\"', temp1 + 3);
                    Console.WriteLine(line.Substring(temp1 + 3, temp2 - temp1 - 3).Split(' ')[0], nfi);
                    double vr = Convert.ToDouble(line.Substring(temp1 + 3, temp2 - temp1 - 3).Split(' ')[0], nfi) * mean_of_forward(line[temp2 - 2]);
                    RandC tmp9 = new RandC(num_of_line, vr, vr, vr, temp_name);
                    //Console.WriteLine(tmp9.name + " "+ num_of_line);
                    variations.Add(tmp9);
                    continue;
                }
                if (wtisit == ".AC")
                {

                    af_flag = true;
                    ac = num_of_line;
                    string s = " ";
                    int n0, n1;
                    n0 = line.IndexOf("Points=\"");
                    n1 = line.IndexOf("\"", n0 + 8);
                    s = line.Substring(n0 + 8, n1 - n0 - 8);
                    freq_points_number = Convert.ToInt32(s);
                    continue;
                }
                if (wtisit == ".DC")
                {
                    dc = num_of_line;
                    continue;
                }
                if (wtisit == ".TR")
                {
                    string s2 = " ";
                    int n0 = 0, n1 = 0, n3 = 0;
                    char c = 'd';
                    n0 = line.IndexOf("Start=\"");
                    n1 = line.IndexOf("\"", n0 + 8);
                    s2 = line.Substring(n0 + 7, n1 - n0 - 7);
                    n3 = s2.IndexOf("s");
                    if (n3 != -1)
                    {
                        c = s2[n3 - 1];
                        if (c != ' ')
                        {
                            start = Convert.ToDouble(s2.Substring(0, n3 - 2), nfi) * mean_forward.mean_of_forward(c);
                        }
                        else
                        {
                            start = Convert.ToDouble(s2.Substring(0, s2.Length - 2), nfi) * mean_forward.mean_of_forward(c);
                        }
                    }
                    else
                        start = Convert.ToDouble(s2);
                    n0 = line.IndexOf("Stop=\"");
                    n1 = line.IndexOf("\"", n0 + 6);
                    s2 = line.Substring(n0 + 6, n1 - n0 - 6);
                    n3 = s2.IndexOf("s");
                    if (n3 != -1)
                    {
                        c = s2[n3 - 1];
                        if (c != ' ')
                        {
                            stop = Convert.ToDouble(s2.Substring(0, n3 - 2), nfi) * mean_forward.mean_of_forward(c);
                        }
                        else
                        {
                            stop = Convert.ToDouble(s2.Substring(0, s2.Length - 2), nfi);
                        }
                    }
                    else
                    {
                        stop = Convert.ToDouble(s2, nfi);
                    }
                    n0 = line.IndexOf("Points=\"");
                    n1 = line.IndexOf("\"", n0 + 8);
                    s2 = line.Substring(n0 + 8, n1 - n0 - 8);
                    time_points_number = Convert.ToInt32(s2);
                    timer = num_of_line;
                    continue;
                }
                if (wtisit == ".Def")
                {
                    while (!line.Contains(".Def:End"))
                    {
                        //Console.WriteLine(line);
                        line = NetRead.ReadLine();
                        NetWrite.WriteLine(line);
                        num_of_line++;
                    }
                }
            }
            NetRead.Close();
            NetWrite.Close();
            netlist.Close();
            test_netlist.Close();
        
        }

        // Моделирование заданной неисправности
        public bool model(int f=-1,int v=-1, bool up=false )                     // Моделировать (-1 - обычное)
        {
            FileStream test_netlist = new FileStream(test_netlistName,FileMode.Open);
            StreamReader NetRead=new StreamReader(test_netlist);
            FileStream tempFile = new FileStream(tempRes, FileMode.Create);
            StreamWriter temp=new StreamWriter(tempFile);
            string line;
            string wtisit;
            int n=0;
            while (!NetRead.EndOfStream)
            {
                line = NetRead.ReadLine();
                n++;
                wtisit=line.Split(':')[0];
                if(f!=-1&&n==faults[f].pos&&faults[f].IsDev==false)
                {
                    int temp1;
                    if(faults[f].type==false)
                        temp1=line.IndexOf("R=\"");
                    else
                        temp1 = line.IndexOf("C=\"");
                    int temp2=line.IndexOf('\"',temp1+4);
                    //Console.WriteLine("length :    " + line);
                    string elType = line.Substring(0, temp1);
                    string elNumb;
                    int temp99;
                    if (faults[f].type == true)
                    {  
                        elType=elType.Replace('C','R');
                        temp99=elType.IndexOf(' ');
                        elNumb = elType.Substring(temp99, elType.Length - temp99);
                        temp.Write("R:R121 ");
                        //Console.WriteLine(elNumb); 
                        temp.Write(elNumb);
                    }
                    else
                    temp.Write(elType);
                    if (faults[f].up)
                    {
                        temp.Write("R=\"10e99 Ohm");
                    }
                    else
                    {
                        temp.Write("R=\"0.001 Ohm");
                    }
                    if (faults[f].type == false)
                    {
                        //Console.WriteLine(temp2);
                        //Console.WriteLine(line);
                        //Console.WriteLine(line.Substring(line.Length - temp2));
                        temp.WriteLine(line.Substring(temp2));
                    }
                    else
                        temp.WriteLine("\" Temp=\"26.85\" Tc1=\"0.0\" Tc2=\"0.0\" Tnom=\"26.85\"");
                    continue;
                }



                
                    if(f!=-1&&n==faults[f].pos&& faults[f].IsDev==true) 
                {
                    int dev_n=0;
                    for (int i = 0; i < variations.Count; i++)
                    {
                        if (variations[i].pos == faults[f].pos)
                        {
                            dev_n = i;
                            break;
                        }
                    }                  
                    //Console.WriteLine(variations[v].pos);
                    int temp1;
                    if (wtisit == "R")
                        temp1 = line.IndexOf("R=\"");
                    else
                        temp1 = line.IndexOf("C=\"");
                    int temp2 = line.IndexOf('\"', temp1 + 3);
                    temp.Write(line.Substring(0, temp1));
                    if (wtisit == "R")
                        if (faults[f].up)
                            temp.Write("R=\"" + (variations[dev_n].mid+variations[dev_n].mid * NDIV / 100).ToString(nfi) + " Ohm");
                        else
                        {

                            temp.Write("R=\"" + (variations[dev_n].mid - variations[dev_n].mid * NDIV / 100).ToString(nfi) + " Ohm");
                        }
                    else
                        if (faults[f].up)
                            temp.Write("C=\"" + (variations[dev_n].mid + variations[dev_n].mid * NDIV / 100).ToString(nfi) + " F");
                        else
                            temp.Write("C=\"" + (variations[dev_n].mid - variations[dev_n].mid * NDIV / 100).ToString(nfi) + " F");
                    temp.WriteLine(line.Substring(temp2));

                    continue;
                }
  


                if(v!=-1&&n==variations[v].pos) 
                {
                    //Console.WriteLine(variations[v].pos);
                    int temp1;
                    if(wtisit=="R")
                    temp1=line.IndexOf("R=\"");
                    else
                    temp1=line.IndexOf("C=\"");
                    int temp2=line.IndexOf('\"',temp1+3);
                    temp.Write(line.Substring(0,temp1));
                    if(wtisit=="R")
                        if (up)
                            temp.Write("R=\"" + variations[v].top.ToString(nfi) + " Ohm");
                        else
                        {
                            
                            temp.Write("R=\"" + variations[v].bot.ToString(nfi) + " Ohm");
                        }
                    else
                        if (up)
                            temp.Write("C=\"" + variations[v].top.ToString(nfi) + " F");
                        else
                            temp.Write("C=\"" + variations[v].bot.ToString(nfi) + " F");
                    temp.WriteLine(line.Substring(temp2));
                    
                    continue;
                }
                temp.WriteLine(line);
            }
            test_netlist.Close();
            temp.Close();
            NetRead.Close();
            tempFile.Close();
            Process p;
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Console.WriteLine(tempFile.Name);
            p = Process.Start( "qucsator.exe ", "-i " + tempFile.Name + " -o " + resultName);
            //ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.FileName = "qucsator.exe";
            //String prm;
            //prm = ("-i " + tempFile.Name + " -o " + resultName);
            //Console.WriteLine(prm);
            //startInfo.Arguments = prm;
            ////startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //p=Process.Start(startInfo);
            modelFault=p.WaitForExit(8000);
            if (!modelFault)
            {
                p.CloseMainWindow();
                p.Close();
            }
            //System.IO.File.Delete(tempFile.Name);
            return modelFault;
        }
        
        // Метод для установки возможных вариаций конкретного элемента
        public void change_variation(double mean, int n, bool up)          
        {
            if (n > variations.Count - 1) System.Console.WriteLine( "Ошибка. Выход за пределы массива вариаций.");
            if (up)
            {
                RandC temp20 = new RandC(variations[n].pos, variations[n].bot, variations[n].mid, variations[n].top, variations[n].name);
                temp20.top = mean;
                variations[n] = temp20;
            }
            else
            {
                RandC temp20 = new RandC(variations[n].pos, variations[n].bot, variations[n].mid, variations[n].top, variations[n].name);
                temp20.bot = mean;
                variations[n] = temp20;
            }
        }


        // Метод чтения результатов моделирования
        public List<Readings> sign(string file)
        {
            //Console.WriteLine(af_flag);
            FileStream sn=new FileStream(file,FileMode.Open);
            StreamReader inn=new StreamReader(sn);  
            List<Readings> mas=new List<Readings>();
            string line;
            List<double> inF = new List<double>();
            List<double> outF = new List<double>();
            List<double> L = new List<double>();
            while(!inn.EndOfStream)
            {
                line=inn.ReadLine();
                if (line == "<indep time " + time_points_number + ">")
                {
                    line = inn.ReadLine();
                    while (line != "</indep>")
                    {
                        timePoints.Add(Convert.ToDouble(line, nfi));
                        line = inn.ReadLine();
                    }
                }
                if (line == "<indep acfrequency " + freq_points_number + ">")
                {
                    line = inn.ReadLine();
                    while (line != "</indep>")
                    {
                        freqPoints.Add(Convert.ToDouble(line.Substring(0, 21), nfi));
                        line = inn.ReadLine();
                    }
                }

            
               
                if (line == "<dep in.v acfrequency>")
                {
                    line = inn.ReadLine();

                    while (line != "</dep>")
                    {
                        double pp;
                        try
                        {
                            pp = Convert.ToDouble(line.Substring(0, 21), nfi);
                        }
                        catch (System.FormatException e)
                        {
                            //Console.WriteLine("0000");
                            pp = 0;
                        }
                        inF.Add(pp);
                       // inF_im.Add(Convert.ToDouble(line.Substring(22, 5), nfi));
                        line = inn.ReadLine();
                    }
                    
                    continue;
                }

              
                if (line == "<dep out.v acfrequency>")
                {
                    line = inn.ReadLine();
                    while (line != "</dep>")
                    {
                        double amp;
                        if(line.Length>24)
                        {
                            amp = 0;
                            try
                           {
                                
                                amp = Math.Sqrt((Math.Pow(Convert.ToDouble(line.Substring(0, 21), nfi), 2) + Math.Pow(Convert.ToDouble(Convert.ToDouble(line.Substring(23, 18), nfi)), 2)));
                           }
                           catch (System.FormatException e)
                              {
                                  amp = 0;
                                  ///Console.WriteLine("11111111");
                              }
                            }
                        else
                            amp =Convert.ToDouble(line.Substring(0, 21), nfi);
                        outF.Add(amp);
                        line = inn.ReadLine();
                    }
                    continue;
                }
                


                // Проверка на амперметр
                for(int i=0;i<name_of_AM.Count;i++)
                {

                    if(line=="<dep "+name_of_AM[i]+".It time>")
                    {
                        line=inn.ReadLine();
                        List<double> tmpI = new List<double>();
                        tmpI.Max();
                        while(line!="</dep>")
                        {
                            tmpI.Add(Convert.ToDouble(line,nfi));
                            line=inn.ReadLine();
                        }
                        double ptl = (stop - start) / time_points_number;
                        int len = tmpI.Count;
                        double max = Math.Abs(tmpI.Max());
                        double min = Math.Abs(tmpI.Min());
                        double zero = (max - min) / 2;
                        Readings t = new Readings();
                        t.name = name_of_AM[i] + ".It_time_Amp";
                        t.data = max - zero;
                        mas.Add(t);
                        List<int> pos = new List<int>();
                        List<double> periods = new List<double>();
                        for (int j = 1; j < len; j++)
                        {
                            if (((tmpI[j - 1] - zero) * (tmpI[j] - zero)) <= 0)
                            {
                                pos.Add(j);
                            }
                        }
                        for (int j = 1; j < pos.Count; j++)
                        {
                            periods.Add((pos[j] - pos[j - 1]) * ptl);
                        }
                        double period = periods.Sum() / periods.Count * 2;
                        Readings t1 = new Readings();
                        t1.name = name_of_AM[i] + ".It_time_Period";
                        t1.data = period;
                        mas.Add(t1);
                        continue;
                    }
            
                    if(line=="<indep "+name_of_AM[i]+".I 1>")
                    {
                        line=inn.ReadLine();
                        Readings t2 = new Readings();
                        t2.name = name_of_AM[i] + ".I_1_Value";
                        t2.data = Convert.ToDouble(line, nfi);
                        mas.Add(t2);
                
                    }
                }

                for (int i = 0; i < name_of_VM.Count; i++)
                {
          
                    if (line == "<dep " + name_of_VM[i] + ".Vt time>")
                    {
                
                        line = inn.ReadLine();
                        List<double> tmpI = new List<double>();
                        while (line != "</dep>")
                        {
                            try
                            {
                                tmpI.Add(Convert.ToDouble(line, nfi));
                            }
                            catch (System.FormatException e)
                            {
                                Console.WriteLine("\n" + line);
                            }
                            line = inn.ReadLine();
                        }
                        double ptl = (stop - start) / time_points_number;
                        double max = Math.Abs(tmpI.Max());
                        double min = Math.Abs(tmpI.Min());
                        double zero = min + (max - min) / 2;
                        Readings t = new Readings();
                        t.name = name_of_VM[i] + ".Vt_time_Amp";
                        //Console.WriteLine(name_of_VM[i] + " max= " + max + " zero=" + zero);
                        t.data = max - zero;
                        mas.Add(t);
                        List<int> pos = new List<int>();
                        List<double> periods = new List<double>();
                        for (int j = 1; j < time_points_number; j++)
                        {
                            //Console.WriteLine(j);
                            try
                            {
                                if (((tmpI[j - 1] - zero) * (tmpI[j] - zero)) <= 0)
                                {
                                    pos.Add(j);
                                }
                            }
                            catch (System.ArgumentOutOfRangeException e)
                            {
                                //double sf;
                                //Console.WriteLine(tmpI.Count + "j=" + j + " i=" + i + "tpn=" + time_points_number  + " cnt= " + cnt);
                                //Console.WriteLine("2222222");
                                modelFault = true;
                                break;
                                //sf = ((tmpI[j - 1] - zero) * (tmpI[j] - zero));
                            }
                        }
                        if (!modelFault)
                        {
                            for (int j = 1; j < pos.Count; j++)
                            {
                                periods.Add((timePoints[pos[j]] - timePoints[pos[j - 1]]));
                            }

                            double period = periods.Sum() / periods.Count * 2;
                            //Readings t1 = new Readings();
                            //t1.name = name_of_VM[i] + ".Vt_time_Period";
                            //t1.data = period;
                            //mas.Add(t1);
                            continue;
                        }
                    }

                    if (line == "<indep " + name_of_VM[i] + ".V 1>")
                    {
                        line = inn.ReadLine();
                        Readings t2 = new Readings();
                        t2.name = name_of_VM[i] + ".V_1_Value";
                        try
                        {
                            t2.data = Convert.ToDouble(line, nfi);
                        }
                        catch (System.FormatException e)
                        {
                            //Console.WriteLine("333333333   -   " + line);
                            t2.data = 0;
                        }
                        mas.Add(t2);

                    }
                }

            }
            if (af_flag == true)
            {
                for (int i = 0; i < freq_points_number; i++)
                {
                    if (outF.ElementAt(i) != 0)
                    {
                        L.Add(20 * Math.Log10(Math.Abs(outF.ElementAt(i)) / Math.Abs(inF.ElementAt(i))));
                    }
                    else
                    {
                        L.Add(0);
                    }
                }
                Readings t2 = new Readings();
                t2.name = "Max_L";
                t2.data = L.Max();
                if (L.Max() <-100)
                    t2.data = 0;
                mas.Add(t2);
                Readings t3 = new Readings();
                t3.name = "Max_L_Freq";
                if (L.Max() >-100 && L.Max()!=0)
                    t3.data = freqPoints.ElementAt(L.IndexOf(L.Max()));
                else
                    t3.data = 0;
                mas.Add(t3);
                //Console.WriteLine(L.Max());
                //Console.WriteLine(freqPoints.ElementAt(L.IndexOf(L.Max())));
            }
            inn.Close();
            sn.Close();

            return mas;
    }

        // Метод для установки вариацийй в процентах для всех элементов
        public void SetAllVariations(double pre)
    {
        for(int i=0;i<variations.Count;i++)
        {
            RandC r=new RandC();
            r=variations[i];
            double t=r.mid;
            change_variation(t-t * pre * 0.01, i, false);
            change_variation(t + t * pre * 0.01, i, true);
        }
    }

        // Метод для создания сигнатуры с возможными отклонениями заданной неисправности
        public List<SgnParam> get_one_means(int neis)
        {
            List<SgnParam> results = new List<SgnParam>();   //вектор результатов (интервалы)
            List<Readings> massiv = new List<Readings>();
            List<List<Readings>> resmod = new List<List<Readings>>(); //результаты моделирования
            int sizeRC = variations.Count;
            model(neis, -1, true);
            massiv = sign(resultName);
            resmod.Add(massiv);
            for (int k = 0; k < massiv.Count; k++)
            {
                SgnParam t = new SgnParam();
                t.mid = massiv[k].data;
                t.name = massiv[k].name; 
                if (tol_mode >=0)
                {
                    t.bot = t.mid - tol_mode / 100 * t.mid;
                    t.top= t.mid +tol_mode / 100 * t.mid;
                }
                results.Add(t);
            }
            if (tol_mode < 0)
            {
            for (int j = 0; j < sizeRC; j++)
            {
                model(neis, j, false);
                massiv = sign(resultName);
                resmod.Add(massiv);
                model(neis, j, true);
                massiv = sign(resultName);
                resmod.Add(massiv);
            }
            double max = 0,min = 0;
            int massize = massiv.Count,resmsize = resmod.Count;
            //Console.WriteLine("massize=" + massize + "  remsize=" + resmsize);
            
                for (int i = 0; i < massize; i++)
                {
                    min = 0;
                    max = 0;
                    for (int j = 1; j < resmsize; j += 2)
                    {
                        //Console.WriteLine(resmod[j][i].data);
                        min = min + ((resmod[0][i].data - resmod[j][i].data) * (resmod[0][i].data - resmod[j][i].data)) / 2;
                        //if(i>10)
                        //Console.WriteLine(min);
                    }
                    min = 0.5 * Math.Sqrt(min);
                    SgnParam t2 = new SgnParam();
                    t2 = results[i];
                    t2.bot = results[i].mid - min;
                    results[i] = t2;
                    for (int k = 2; k < resmsize; k += 2)
                    {
                        max = max + ((resmod[0][i].data - resmod[k][i].data) * (resmod[0][i].data - resmod[k][i].data)) / 2;
                    }
                    max = 0.5 * Math.Sqrt(max);
                    SgnParam t3 = new SgnParam();
                    t3 = results[i];
                    t3.top = results[i].mid + max;
                    results[i] = t3;
                }
            }

            return results;
        }

        // Метод для создания базы данных
        public void CreateDB(string DBFileName="sigs.txt")
        { 
            List<SgnParam> res = new List<SgnParam>();
            FileStream sigFile = new FileStream(DBFileName, FileMode.Create);
            StreamWriter sgStr = new StreamWriter(sigFile);
            sgStr.WriteLine("<?xml version = \"1.0\" ?>");
            sgStr.WriteLine("<signatures>");
            for (int i = -1; i < faults.Count; i++)
            {
                res = get_one_means(i);
                if (modelFault)
                {
                    if (i == -1)
                        sgStr.WriteLine("<fault name=\"Normal work\" type=\"1\">");
                    else
                        sgStr.WriteLine("<fault name=\"" + name_of_faults[i] + "\" type=\"" + Convert.ToInt16(faults[i].up) + "\">");
                    for (int j = 0; j < res.Count; j++)
                    {
                        sgStr.WriteLine("<" + res[j].name + " min_val=\"" + res[j].bot + "\" max_val=\"" + res[j].top + "\" val=\"" + res[j].mid + "\"/>");
                    }
                    sgStr.WriteLine("</fault>");
                }
                else
                {
                    Console.WriteLine("\"" + name_of_faults[i] + "\" не удалось промоделировать");
                    continue;
                }
                if(i!=-1)
                Console.WriteLine("\"" + name_of_faults[i] + "\" промоделирована");
                cnt = cnt + 1; 
            }
            sgStr.WriteLine("</signatures>");
            sgStr.Close();
        }
};

}



