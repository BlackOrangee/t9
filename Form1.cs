using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace t9
{
    public partial class Form1 : Form
    {
        //private string dictionaryPath = "../../dictionary.txt";
        private string dictionaryPath = "../../sortedDictionary.txt";


        private volatile bool _running = true;

        private bool isTextSelected = false;

        private bool isErased = false;

        private bool isPositionUpdated = false;

        private bool isOnInput = false;

        private string word = "";

        private int position = 0;

        private string foundWord = "";

        private string wordToInsert = "";

        private List<string> englishWords;

        private AlwaysFocusedTextBox textBox2;

        private Dictionary<Button, char[]> buttonChars;

        private Dictionary<Button, char[]> ENChars;

        private Dictionary<Button, char[]> UKChars;

        private Dictionary<Button, char> buttonNumbers;

        private Button lastButtonPressed;

        private int currentCharIndex = -1;

        private DateTime lastKeyPressTime;

        private System.Windows.Forms.Timer timer;

        private System.Windows.Forms.Timer longPressTimer;

        private bool isLongPress;

        public Form1()
        {
            InitializeComponent();

            textBox2 = new AlwaysFocusedTextBox
            {
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(300, 200),
                Multiline = true,
                Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))),

            };

            Controls.Add(textBox2);

            this.Shown += (sender, e) => textBox2.Focus();

            ENChars = new Dictionary<Button, char[]>
            {
                { button2, new char[] { 'a', 'b', 'c' } },
                { button3, new char[] { 'd', 'e', 'f' } },
                { button4, new char[] { 'g', 'h', 'i' } },
                { button5, new char[] { 'j', 'k', 'l' } },
                { button6, new char[] { 'm', 'n', 'o' } },
                { button7, new char[] { 'p', 'q', 'r', 's' } },
                { button8, new char[] { 't', 'u', 'v' } },
                { button9, new char[] { 'w', 'x', 'y', 'z' } },
                { button10, new char[] { '*' }},
                { button11, new char[] { '+' }},
                { button12, new char[] { ' ' }},
            };

            UKChars = new Dictionary<Button, char[]>
            {
                { button2, new char[] { 'а', 'б', 'в', 'г' } },
                { button3, new char[] { 'д', 'е', 'є', 'ж' } },
                { button4, new char[] { 'з', 'и', 'і', 'ї' } },
                { button5, new char[] { 'й', 'к', 'л', 'м' } },
                { button6, new char[] { 'н', 'о', 'п', 'р' } },
                { button7, new char[] { 'с', 'т', 'у', 'ф' } },
                { button8, new char[] { 'х', 'ц', 'ч', 'ш' } },
                { button9, new char[] { 'щ', 'ь', 'ю', 'я' } },
                { button10, new char[] { '*' }},
                { button11, new char[] { '+' }},
                { button12, new char[] { ' ' }},
            };

            //set default dictionary
            buttonChars = ENChars;

            buttonNumbers = new Dictionary<Button, char>
            {
                { button2, '2' },
                { button3, '3' },
                { button4, '4' },
                { button5, '5' },
                { button6, '6' },
                { button7, '7' },
                { button8, '8' },
                { button9, '9' },
                //{ button10, '' },
                { button11, '0' },
                { button12, '#' },
            };

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;

            longPressTimer = new System.Windows.Forms.Timer { Interval = 500 };
            longPressTimer.Tick += LongPressTimer_Tick;

            foreach (Button button in buttonChars.Keys)
            {
                button.MouseDown += InputButton_MouseDown;
                button.MouseUp += InputButton_MouseUp;
            }

            textBox2.KeyDown += textBox2_KeyDown;
            textBox2.Click += textBox2_Click;

            LoadDictionaryFromFile(dictionaryPath);
            DictionarySort();

            //RemoveDuplicates();
            //SortWordsByLength();
            //SaveSortedWordsToFile("sortedDictionary.txt");

            ButtonsHidrator();


            Thread t9 = new Thread(() =>
            {

                while (_running)
                {
                    TextSelectionChecker();
                    UpdatePosition();
                    UpdateSelectedWord(textBox2.Text);
                    FindWord();
                    WordInsert();

                    if (textBox2.InvokeRequired)
                    {
                        textBox2.Invoke(new Action(() =>
                        {
                            label2.Text = foundWord;
                            label1.Text = word;
                        }));
                    }

                    Thread.Sleep(100);
                }
            });

            t9.Start();
            //textBox2.Focus();
        }

        private void LoadDictionaryFromFile(string filePath)
        {
            englishWords = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        englishWords.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dictionary: {ex.Message}");
            }
        }

        private void DictionarySort()
        {
            englishWords.Sort((x, y) =>
            {
                int result = x.Length.CompareTo(y.Length);
                if (result == 0)
                {
                    result = x.CompareTo(y);
                }
                return result;
            });

            //englishWords.Sort((x, y) =>
            //{
            //    int result = x.CompareTo(y);
            //    if (result == 0)
            //    {
            //        result = x.Length.CompareTo(y.Length);
            //    }
            //    return result;
            //});
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _running = false;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void TextSelectionChecker()
        {
            if (textBox2.InvokeRequired)
            {
                textBox2.Invoke(new Action(() =>
                {
                    if (textBox2.SelectionLength == 0)
                    {
                        isTextSelected = false;

                    }
                }));
            }
        }

        private void UpdatePosition()
        {
            int newPosition = 0;

            if (textBox2.InvokeRequired)
            {
                textBox2.Invoke(new Action(() =>
                {
                    newPosition = textBox2.SelectionStart;
                }));
            }

            if (newPosition != position)
            {
                position = newPosition;
                isPositionUpdated = true;
            }
            else
            {
                isPositionUpdated = false;
            }
            //System.Console.WriteLine(position);
        }

        private void UpdateSelectedWord(string text)
        {
            if (!isPositionUpdated || isOnInput)
            {
                return;
            }


            Stack<char> selectedWord = new Stack<char>();

            for (int i = position - 1; i >= 0; i--)
            {
                if (text[i] == ' ')
                {
                    break;
                }

                selectedWord.Push(text[i]);
            }

            string selectedWordString = new string(selectedWord.ToArray());

            if (word != selectedWordString)
            {
                word = selectedWordString;
            }

            //System.Console.WriteLine(word);
        }

        private void FindWord()
        {
            if (word == "")
            {
                foundWord = "";
                return;
            }

            foundWord = englishWords.Find(w => w.StartsWith(word));

            //System.Console.WriteLine(foundWord);
        }

        private void WordInsert()
        {
            if (foundWord == null || foundWord == "" || word == "" || isTextSelected || isErased || isOnInput)
            {
                return;
            }

            wordToInsert = foundWord.Substring(word.Length);
            //System.Console.WriteLine(wordToInsert);

            if (wordToInsert != "")
            {
                if (textBox2.InvokeRequired)
                {
                    textBox2.Invoke(new Action(() =>
                    {
                        textBox2.Text = textBox2.Text.Insert(position, wordToInsert);
                        textBox2.SelectionStart = position;
                        textBox2.SelectionLength = wordToInsert.Length;
                    }));
                }

                isTextSelected = true;
                foundWord = "";
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                isErased = true;
            }
            else
            {
                isErased = false;
            }

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                isErased = true;
            }
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            System.Console.WriteLine(e.KeyCode);

        }

        private void textBox2_Click(object sender, EventArgs e)
        {
            isErased = true;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string word = textBox2.SelectedText;
            string foundWord = englishWords.Find(w => w == word);

            if (word == "")
            {
                MessageBox.Show("Word is empty");
                return;
            }

            if (foundWord == null || foundWord == "")
            {
                using (StreamWriter sw = new StreamWriter(dictionaryPath, true))
                {
                    sw.WriteLine(word);
                }

                englishWords.Add(word);

                DictionarySort();

                MessageBox.Show($"Word \"{word}\" is added to dictionary");
                return;
            }

            MessageBox.Show($"Word \"{word}\" is already exists in dictionary");
        }

        void ButtonsHidrator()
        {
            button1.Text = "1";
            button2.Text = "2\nАБВГ\nABC";
            button3.Text = "3\nДЕЄЖ\nDEF";
            button4.Text = "4\nЗИІЇ\nGHI";
            button5.Text = "5\nЙКЛМ\nJKL";
            button6.Text = "6\nНОПР\nMNO";
            button7.Text = "7\nСТУФ\nPQRS";
            button8.Text = "8\nХЦЧШ\nTUV";
            button9.Text = "9\nЩЬЮЯ\nWXYZ";
            button10.Text = "*";
            button11.Text = "0\n+";
            button12.Text = "#\n_";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            isOnInput = true;

            int selectionStart = textBox2.SelectionStart;
            textBox2.Text = textBox2.Text.Insert(selectionStart, "1");
            textBox2.SelectionStart = selectionStart + 1;

            isOnInput = false;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Length > 0)
            {
                isErased = true;
                int selectionStart = textBox2.SelectionStart;
                int selectionLength = textBox2.SelectionLength;

                if (selectionLength > 0)
                {
                    textBox2.Text = textBox2.Text.Remove(selectionStart, selectionLength);
                    textBox2.SelectionStart = selectionStart;
                }
                else if (selectionStart > 0)
                {
                    textBox2.Text = textBox2.Text.Remove(selectionStart - 1, 1);
                    textBox2.SelectionStart = selectionStart - 1;
                }
                else if (selectionStart == 0)
                {
                    textBox2.Text = textBox2.Text.Remove(0, 1);
                    textBox2.SelectionStart = 0;
                }
            }
        }

        private void InputButton_MouseDown(object sender, EventArgs e)
        {
            isOnInput = true;
            isErased = false;
            Button button = sender as Button;
            DateTime now = DateTime.Now;


            isLongPress = false;
            longPressTimer.Start();
            lastKeyPressTime = now;

            if (lastButtonPressed != button || (now - lastKeyPressTime).TotalMilliseconds > 1000)
            {
                CompleteLastInput();
                currentCharIndex = 0;
                lastButtonPressed = button;

            }
            else
            {
                currentCharIndex = (currentCharIndex + 1) % buttonChars[button].Length;
            }


            lastKeyPressTime = now;
            InsertCharacter(buttonChars[button][currentCharIndex]);

            if (!timer.Enabled)
            {
                timer.Start();
            }
        }

        private void InputButton_MouseUp(object sender, MouseEventArgs e)
        {
            longPressTimer.Stop();
            if (isLongPress && lastButtonPressed != button10)
            {
                InsertCharacter(buttonNumbers[lastButtonPressed]);
            }
        }

        private void CompleteLastInput()
        {
            if (lastButtonPressed != null && currentCharIndex >= 0)
            {
                int selectionStart = textBox2.SelectionStart;
                textBox2.SelectionStart = selectionStart + 1;
                textBox2.SelectionLength = 0;
                currentCharIndex = -1;

            }
        }

        private void InsertCharacter(char c)
        {
            int selectionStart = textBox2.SelectionStart;
            if (textBox2.SelectionLength > 0)
            {
                textBox2.Text = textBox2.Text.Remove(selectionStart, textBox2.SelectionLength);
            }

            textBox2.Text = textBox2.Text.Insert(selectionStart, c.ToString());
            textBox2.SelectionStart = selectionStart;
            textBox2.SelectionLength = 1;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - lastKeyPressTime).TotalMilliseconds > 1000)
            {
                CompleteLastInput();
                timer.Stop();
                isOnInput = false;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            isErased = true;
            if (textBox2.SelectionLength > 0)
            {
                textBox2.SelectionStart = textBox2.SelectionStart + textBox2.SelectionLength;
                textBox2.SelectionLength = 0;
            }
            int newPosition = Math.Min(textBox2.Text.Length, textBox2.SelectionStart + 1);
            textBox2.SelectionStart = newPosition;
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Length == 0)
            {
                return;
            }

            isErased = true;

            if (textBox2.SelectionLength > 0)
            {
                //textBox2.SelectionStart = textBox2.SelectionStart - textBox2.SelectionLength;
                textBox2.SelectionLength = 0;
            }
            int newPosition = Math.Min(textBox2.Text.Length, textBox2.SelectionStart - 1);
            if (newPosition < 0)
            {
                newPosition = 0;
            }
            textBox2.SelectionStart = newPosition;
        }

        private void LongPressTimer_Tick(object sender, EventArgs e)
        {
            longPressTimer.Stop();
            isLongPress = true;
            if (lastButtonPressed != button10)
                InsertCharacter(buttonNumbers[lastButtonPressed]);
        }


        // language changer
        private void button19_Click(object sender, EventArgs e)
        {
            if (buttonChars == ENChars)
            {
                buttonChars = UKChars;
                button19.Text = "English";
            }
            else
            {
                buttonChars = ENChars;
                button19.Text = "Українська";

            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            string text = textBox2.Text;

            if (text.Length == 0)
            {
                return;
            }

            int position = textBox2.SelectionStart;

            if (position < 0 || position > text.Length)
            {
                return;
            }

            int rightPosition = position;
            int leftPosition = position;

            while (rightPosition < text.Length && text[rightPosition] != ' ')
            {
                rightPosition++;
            }

            while (leftPosition > 0 && text[leftPosition - 1] != ' ')
            {
                leftPosition--;
            }

            if (position == text.Length)
            {
                rightPosition = text.Length;
            }

            int selectionLength = rightPosition - leftPosition;

            textBox2.SelectionStart = leftPosition;
            textBox2.SelectionLength = selectionLength;
        }


        private void SortWordsByLength()
        {
            //englishWords.Sort((x, y) => x.Length.CompareTo(y.Length));
            englishWords = englishWords.OrderBy(word => word.Length).ThenBy(word => word).ToList();
        }

        private void SaveSortedWordsToFile(string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    foreach (var word in englishWords)
                    {
                        sw.WriteLine(word);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void RemoveDuplicates()
        {
            HashSet<string> uniqueWords = new HashSet<string>(englishWords);
            englishWords = uniqueWords.ToList();
        }

        private void button16_Click(object sender, EventArgs e)
        {

        }
    }
}
