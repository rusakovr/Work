using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
namespace sig_crt
{

    // Класс для тестирования 
    class Program
    {
    static void Main(string[] args)
    {
        // Объект класса для создания базы сигнатур
        model_block mb = new model_block("C:\\Users\\User\\.qucs\\netlist.txt", "test_netlist.txt", "C:\\Users\\User\\.qucs\\Results.txt", "C:\\Users\\User\\.qucs\\FaultNetList.txt",70);

       
        // Задали вычисление 15-процентных допусков, вычисление по сложной формуле пока не актуально
        mb.SetTolMode(15);

        // Анализ netlist
        mb.analiz_file();


        //// Задание вариаций для всех эfлементов в процентах
        mb.SetAllVariations(5);



        for (int i = 0; i < mb.faults.Count; i++)
       {
            Console.WriteLine(i + " " + mb.name_of_faults[i]);
        //    //mb.model(i, 2, true);

        }
        //mb.model(1, 1, true);
        Console.WriteLine("---------------------------------");
        for (int i = 0; i < mb.variations.Count; i++)
        {
            Console.WriteLine(i + " " + mb.variations[i].name + " bot= " + mb.variations[i].bot + " mid= " + mb.variations[i].mid + " top= " +mb.variations[i].top);
            //mb.model(i, 2, true);

        }
        //mb.model(3, 1, true);
        //Console.WriteLine("---------------------------------");

        //mb.model(-1, -1, true);
        //List<Readings> tt = new List<Readings>();
        //tt = mb.sign("C:\\Users\\User\\Desktop\\Results.txt");
        //Console.WriteLine(tt[0].name + " " + tt[0].data);
        //mb.model(26, -1, true);
        //tt = mb.sign("C:\\Users\\User\\Desktop\\ResultsOfSimulations.txt");
        //Console.WriteLine(tt[0].name + " " + tt[0].data);
        //List<SgnParam> s = new List<SgnParam>();
        //s= mb.get_one_means(-1);
        //Console.WriteLine(s[0].bot);
        
        //for (int i = 0; i < mb.faults.Count; i++)
        //{
        //    //Console.WriteLine(mb.faults[i].pos + "     " + i);
        //    mb.model(i, 2, true);
        //    if(!mb.modelFault)
        //    Console.WriteLine(" Неисправность " + mb.name_of_faults[i] + " не удалось промоделировать");
        //}

        //mb.model(3, 0, true);
        //List<Readings> rd = new List<Readings>();
        //for (int i = 0; i < mb.variations.Count; i++)
        //{
        //    mb.model(3, i, false);
        //    rd = mb.sign("C:\\Users\\User\\Desktop\\Results.txt");
        //    Console.WriteLine(rd[0].data);
        //}
        //mb.model(2, 1, false);
        //List<SgnParam> sg = new List<SgnParam>();
        //sg = mb.get_one_means(3);
        //for (int i = 0; i < sg.Count; i++)
        //{
        //    Console.WriteLine(sg[i].name + " " + sg[i].bot + " " + sg[i].mid + " " + sg[i].top);
        //}
        //// Создание базы данных
        //mb.sign("C:\\Users\\User\\.qucs\\batt.dat");
        //mb.model(2, -1, false);
        //mb.CreateDB("SigDB.txt");
        }     
    }
}








