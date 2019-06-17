using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using System.Threading;
using System.IO;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        static Form1 instance = null; // For label down
        InfoDownload infoDown = new InfoDownload(); // Класс InfoDownload хранит информацию для скачивания
        SaveFile file = new SaveFile(); // Для сохранения в файл
        Download download = new Download(); // Для загрузки

        public Form1()
        {
            InitializeComponent();
            instance = this;
            checkSaveFile(); // Проверить остались ли файлы для скачивания
        }
        

        // Проверка на незавершенные сохранения и их добавления в таблицу
        public void checkSaveFile()
        {
            String[] saveDown = file.read();

            // Если файл невозможно прочитать 
            if (saveDown == null)
            {
                return;
            }

            // Заполнить таблицу нескаченными видео
            for (int i =0;i<saveDown.Length-1;i++)
            {
                String[] param = saveDown[i].Split('|');
                tableLayoutPanel1.Controls.Add(new Label() { Text = "none" });
                tableLayoutPanel1.Controls.Add(new TextBox() { Text = param[0], Size = new System.Drawing.Size(350, 20), ReadOnly = true });
                tableLayoutPanel1.Controls.Add(new TextBox() { Text = param[1], Size = new System.Drawing.Size(200, 20), ReadOnly = true });
            }
        }

        // Получение параметров для скачивания
        private async void button1_Click(object sender, EventArgs e)
        {
            string result = await Task.Factory.StartNew<string>(
                                             () => Getlink(textBox1.Text),
                                             TaskCreationOptions.LongRunning);
            // Очищаем comboBox от записей
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();

            if (result != "")
            {
                string[] line = result.Split('\n');

                for (int i = 0; i < line.Length; i++)
                {
                    // Только аудио
                    if (line[i].Contains("audio only"))
                    {
                        line[i] = line[i].Replace("audio only DASH audio ", "");
                        line[i] = line[i].Replace("    ", " ");
                        comboBox1.Items.Add(line[i]);
                    }
                    // Только видео
                    else if (line[i].Contains("video only"))
                    {
                        line[i] = line[i].Replace("    ", " ");
                        comboBox2.Items.Add(line[i]);
                    }
                }
                infoDown.Url = textBox1.Text;
            }
            else
            {
                MessageBox.Show("Ссылки не получены");
            } 
        }

        /*async void RunAsyncGetLink(string str)
        {
            Console.WriteLine(str);
            await Task.Run(() => Getlink(str));
        }*/

        string Getlink(string str)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "youtube-dl.exe",
                    Arguments = "-F " + str,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            string allLink = "";
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine(); // вывод из youtube-dl
                int temp;

                // Найдем строки необходимые для загрузки (Добавляем строки с первым числовым значением)
                if (Int32.TryParse(line.Split(' ')[0], out temp))
                {
                    allLink += line + "\n";
                }

            }
            MessageBox.Show("Готово");
            return allLink;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            for (int i = 1; ; i++)
            {
                // Если значение пустое то остановить
                if (tableLayoutPanel1.GetControlFromPosition(0, i) == null)
                {
                    return;
                }
                // Если файл скачен тогда продолить
                if (tableLayoutPanel1.GetControlFromPosition(0, i).Text == "Скачено")
                {
                    continue;
                }
                tableLayoutPanel1.GetControlFromPosition(0, i).Text = "В процессе";

                // Запустить закачку
                var stat = await Task.Run(() => download.Run(tableLayoutPanel1.GetControlFromPosition(1, i).Text + " -o \"" + tableLayoutPanel1.GetControlFromPosition(2, i).Text+ "\\\\%(title)s-%(id)s.%(ext)s\""));
                
                if (stat == "ERROR")
                {
                    tableLayoutPanel1.GetControlFromPosition(0, i).Text = "Ошибка";
                }
                else
                {
                    tableLayoutPanel1.GetControlFromPosition(0, i).Text = "Скачено";
                }
                // Удалить запись с файла
                file.delete(tableLayoutPanel1.GetControlFromPosition(1, i).Text + "|" + tableLayoutPanel1.GetControlFromPosition(2, i).Text+";");
            }
        }

        // Добавить в таблицу
        private void button3_Click(object sender, EventArgs e)
        {
            string kod_format = "";

            if (comboBox2.Text != "")
            {
                kod_format += comboBox2.Text.Split(' ')[0] + "+";
            }

            if (comboBox1.Text != "")
            {
                kod_format += comboBox1.Text.Split(' ')[0];
            }
            else
            {
                MessageBox.Show("Не выбрано аудио");
                return;
            }
            infoDown.FormatCode = kod_format;

            tableLayoutPanel1.Controls.Add(new Label() { Text = "none" });
            tableLayoutPanel1.Controls.Add(new TextBox() { Text = kod_format + " " + infoDown.Url, Size = new Size(350, 20), ReadOnly = true });
            tableLayoutPanel1.Controls.Add(new TextBox() { Text = infoDown.Path, Size = new Size(200, 20), ReadOnly = true });

            // Добавить в сохр. файл
            file.write(kod_format + " " + infoDown.Url + "|" + infoDown.Path);
        }

        // Показать статус загрузки
        public static void Status(string info)
        {
            instance.StatusWrite(info);
        }
        public void StatusWrite(string info)
        {
            labelStatus.Invoke(new Action(() => labelStatus.Text = info));
        }

        // Выбрать путь для сохранения
        private void button4_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath;
                textBox2.Text = folderName;
                infoDown.Path = folderName;
            }
        }
    }
    class Download
    {
        private bool status = false; // Статус загруки
        private Process proc = new Process();
        public String Run(string kod_url) // Отслеживание статуса загруки
        {
            if (status == true)
            {
                return ""; // Возвратить если закрука активна
            }

            status = true;
            String stat = startDownload(kod_url);
            status = false;
            return stat;
        }

        public String startDownload(string kod_url)
        {
            proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "youtube-dl.exe",
                    Arguments = "-f" + kod_url,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            string lineError = null;

            // вывод из youtube-dl
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine(); 
                Form1.Status(line);   
            }

            // ERROR от youtube-dl
            lineError = proc.StandardError.ReadLine();
            if (lineError != null)
            {
                if (lineError.Contains("ERROR"))
                {
                    MessageBox.Show(lineError);
                    return "ERROR";
                }
                lineError = null;
            }
            Form1.Status("Всё скачено");
            return "OK";
        }
    }

    // Работа с файлом сохранения
    class SaveFile
    {
        public String line;

        public String[] read()
        {
            try
            {
                using (StreamReader sr = new StreamReader("save.txt"))
                {
                    line = sr.ReadToEnd();
                    String[] urlSave = line.Split(';');
                    return urlSave;
                }
            }
            catch (IOException e)
            {
                MessageBox.Show("[ERROR] Файл с сохранениями не прочитан:");
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public void delete(String url)
        {
            line = line.Replace(url, "");
            System.IO.File.WriteAllText("save.txt", line);
        }

        public void write(string save)
        {
            line += save+";";
            System.IO.File.WriteAllText("save.txt", line);
        }
    }

    // Информация о скачивающимся видео 
    class InfoDownload
    {
        private string url = "";
        private string formatCode = "";
        private string path = "";

        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }

        public string Path
        {
            get
            {
                  return path;
            }
            set
            {
                path = value;
            }
        }

        public string FormatCode 
        {
            get
            {
                return formatCode;
            }
            set
            {
                formatCode = value;
            }
        }
    }
}
