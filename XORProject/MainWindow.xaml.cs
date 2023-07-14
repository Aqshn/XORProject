using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XORProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts;
        private bool isEncrypting;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void startbtn_Click(object sender, RoutedEventArgs e)
        {
            string filePath = filetxtbox.Text;
            string password = passwordtxtbox.Text;

            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("File or password are NULL", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not find", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                progress.Value = 0;
                isEncrypting = true;
                startbtn.IsEnabled = false;
                Cancelbtn.IsEnabled = true;

                cts = new CancellationTokenSource();

                await Task.Run(() => ProcessFile(filePath, password, cts.Token), cts.Token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Process canceled", "Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
                RestoreInitialState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RestoreInitialState();
            }
        }

        private void Cancelbtn_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null && !cts.Token.IsCancellationRequested)
                cts.Cancel();
        }

        private void ProcessFile(string filePath, string password, CancellationToken cancellationToken)
        {
            byte[] fileBytes;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);

                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                int totalBytes = fileBytes.Length;
                int processedBytes = 0;

                for (int i = 0; i < totalBytes; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    fileBytes[i] ^= passwordBytes[i % passwordBytes.Length];
                    processedBytes++;

                    if (processedBytes % 100 == 0)
                    {
                        int progressPercentage = (int)((processedBytes / (double)totalBytes) * 100);
                        UpdateProgress(progressPercentage);
                    }
                }

                fileStream.Position = 0;
                fileStream.Write(fileBytes, 0, fileBytes.Length);
                fileStream.SetLength(fileBytes.Length);
                fileStream.Flush(true);
            }

            isEncrypting = !isEncrypting;
            UpdateProgress(100);
            MessageBox.Show(isEncrypting ? "Encryption complete." : "Decryption complete.", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            RestoreInitialState();
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                progress.Value = value;
            });
        }

        private void FileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                filetxtbox.Text = openFileDialog.FileName;
        }

        private void RestoreInitialState()
        {
            Dispatcher.Invoke(() =>
            {
                progress.Value = 0;
                startbtn.IsEnabled = true;
                Cancelbtn.IsEnabled = false;
            });
        }
    }
}
