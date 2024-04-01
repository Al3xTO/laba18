using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Notepad
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_VSCROLL = 0x115;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;

        private Stack<TextMemento> mementoStack = new Stack<TextMemento>();
        private Stack<TextMemento> redoStack = new Stack<TextMemento>();
        private string currentFilePath = null;
        private string lastSavedText = string.Empty;

        public Form1()
        {
            InitializeComponent();
            this.Resize += Form1_Resize;

            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            returnToolStripMenuItem.Click += returnToolStripMenuItem_Click;
            backToolStripMenuItem.Click += backToolStripMenuItem_Click;
            textBox1.MouseWheel += TextBox_MouseWheel;
            textBox1.TextChanged += textBox1_TextChanged;
            formattingToolStripMenuItem.Click += formattingToolStripMenuItem_Click;

            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveAsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            returnToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            backToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
        }

        public class TextMemento
        {
            public string Text { get; }

            public TextMemento(string text)
            {
                Text = text;
            }
        }

        private void CreateTextMemento()
        {
            mementoStack.Push(new TextMemento(textBox1.Text));
        }

        private void RestoreTextFromMemento(TextMemento memento)
        {
            textBox1.Text = memento.Text;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Текстовий документ (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(openFileDialog.FileName))
                    {
                        textBox1.Text = sr.ReadToEnd();
                        currentFilePath = openFileDialog.FileName;
                        UpdateFormTitle();
                        mementoStack.Clear(); 
                        lastSavedText = textBox1.Text; 
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка при відкритті файлу: " + ex.Message);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentFilePath != null)
            {
                SaveToFile(currentFilePath);
                UpdateFormTitle();
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Текстовий документ (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = saveFileDialog.FileName;
                    SaveToFile(currentFilePath);
                    UpdateFormTitle();
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Текстовий документ (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = saveFileDialog.FileName;
                SaveToFile(currentFilePath);
                UpdateFormTitle();
            }
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.Write(textBox1.Text);
                    lastSavedText = textBox1.Text; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при збереженні файлу: " + ex.Message);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            textBox1.Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height);
        }

        private void TextBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                SendMessage(textBox1.Handle, WM_VSCROLL, SB_LINEUP, 0);
            }
            else
            {
                SendMessage(textBox1.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != lastSavedText)
            {
                CreateTextMemento();
                UpdateFormTitle();
            }
        }

        private void returnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                TextMemento memento = redoStack.Pop();
                mementoStack.Push(new TextMemento(textBox1.Text));
                RestoreTextFromMemento(memento);
                UpdateFormTitle();
            }
        }

        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mementoStack.Count > 0)
            {
                TextMemento memento = mementoStack.Pop();
                redoStack.Push(new TextMemento(textBox1.Text));
                RestoreTextFromMemento(memento);
                UpdateFormTitle();
            }
        }

        private void UpdateFormTitle()
        {
            if (currentFilePath != null)
            {
                string fileName = Path.GetFileName(currentFilePath);
                if (IsTextChanged())
                {
                    this.Text = $"{fileName} (не збережено)";
                }
                else
                {
                    this.Text = fileName;
                }
            }
            else
            {
                this.Text = "Без імені";
            }
        }

        private bool IsTextChanged()
        {
            if (lastSavedText == string.Empty)
                return false;

            return textBox1.Text != lastSavedText;
        }

        private void formattingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsTextChanged() || (currentFilePath == null && textBox1.Text != string.Empty))
            {
                DialogResult result = MessageBox.Show("Хочете зберегти зміни перед закриттям файлу?", "Збереження змін", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    saveToolStripMenuItem_Click(sender, e);
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            textBox1.Text = string.Empty;
            currentFilePath = null;
            lastSavedText = string.Empty;
            mementoStack.Clear();
            redoStack.Clear();
            UpdateFormTitle();
        }
    }
}
