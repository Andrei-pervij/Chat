using System; // Пространство имен для базовых классов .NET Framework
using System.Net; // Пространство имен для работы с сетевыми адресами и протоколами
using System.Net.Sockets; // Пространство имен для работы с сокетами
using System.Text; // Пространство имен для работы с текстовыми данными и кодировками
using System.Threading.Tasks; // Пространство имен для работы с задачами асинхронного программирования
using System.Windows; // Пространство имен для WPF-приложений
using System.Windows.Input; // Пространство имен для работы с входными данными (клавиатура и мышь)

namespace UdpChatWpf // Объявление пространства имен для текущего проекта
{
    public partial class MainWindow : Window // Класс MainWindow, наследующийся от Window (основное окно приложения WPF)
    {
        private IPAddress localAddress = IPAddress.Parse("127.0.0.1"); // Локальный IP-адрес (localhost)
        private string username; // Имя пользователя
        private int localPort; // Порт для приема сообщений
        private int remotePort; // Порт для отправки сообщений
        private Socket senderSocket; // Сокет для отправки сообщений
        private Socket receiverSocket; // Сокет для приема сообщений

        public MainWindow()
        {
            InitializeComponent(); // Инициализация компонентов WPF
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Старт". Настраивает сокеты и запускает чат.
        /// </summary>
        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            username = textBoxUsername.Text.Trim(); // Получение и обработка введенного имени пользователя
            if (string.IsNullOrWhiteSpace(username)) // Проверка на пустое или пробельное имя пользователя
            {
                MessageBox.Show("Введите имя пользователя."); // Вывод сообщения об ошибке
                return; // Прерывание выполнения метода
            }

            // Проверка на корректный ввод портов для приема и отправки сообщений
            if (!int.TryParse(textBoxLocalPort.Text, out localPort) || !int.TryParse(textBoxRemotePort.Text, out remotePort))
            {
                MessageBox.Show("Введите корректные номера портов."); // Вывод сообщения об ошибке
                return; // Прерывание выполнения метода
            }

            try
            {
                senderSocket?.Close(); // Закрытие предыдущего сокета для отправки (если есть)
                receiverSocket?.Close(); // Закрытие предыдущего сокета для приема (если есть)

                // Инициализация нового сокета для отправки сообщений
                senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // Инициализация нового сокета для приема сообщений
                receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Привязка сокета для приема сообщений к указанному локальному IP-адресу и порту
                receiverSocket.Bind(new IPEndPoint(localAddress, localPort));

                Task.Run(ReceiveMessageAsync); // Запуск асинхронного приема сообщений в отдельном потоке
                MessageBox.Show("Чат запущен."); // Вывод сообщения об успешном запуске чата
            }
            catch (Exception ex) // Обработка исключений
            {
                MessageBox.Show($"Ошибка: {ex.Message}"); // Вывод сообщения об ошибке
            }
        }

        /// <summary>
        /// Отправка сообщения через UDP.
        /// </summary>
        private async void SendMessage()
        {
            string message = textBoxInput.Text.Trim(); // Получение и обработка введенного сообщения
            if (!string.IsNullOrWhiteSpace(message)) // Проверка на пустое или пробельное сообщение
            {
                message = $"{username}: {message}"; // Добавление имени пользователя к сообщению
                byte[] data = Encoding.UTF8.GetBytes(message); // Кодировка сообщения в массив байт

                // Асинхронная отправка сообщения на указанный удаленный IP-адрес и порт
                await senderSocket.SendToAsync(new ArraySegment<byte>(data), SocketFlags.None, new IPEndPoint(localAddress, remotePort));

                // Обновление UI-потока для добавления сообщения в список сообщений
                Dispatcher.Invoke(() =>
                {
                    if (!listBoxMessages.Items.Contains(message)) // Проверка на дубликат
                        listBoxMessages.Items.Add(message); // Добавление сообщения в список
                });

                textBoxInput.Clear(); // Очистка текстового поля ввода сообщения
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отправить".
        /// </summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(); // Вызов метода отправки сообщения
        }

        /// <summary>
        /// Обработчик нажатия Enter для отправки сообщения.
        /// </summary>
        private void TextBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // Проверка на нажатие клавиши Enter
            {
                SendMessage(); // Вызов метода отправки сообщения
            }
        }

        /// <summary>
        /// Прием сообщений через UDP.
        /// </summary>
        private async Task ReceiveMessageAsync()
        {
            byte[] data = new byte[65535]; // Буфер для получаемых данных
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0); // Конечная точка для приема данных (любой IP-адрес и любой порт)

            while (true) // Бесконечный цикл для непрерывного приема сообщений
            {
                try
                {
                    // Асинхронный прием данных
                    var result = await receiverSocket.ReceiveFromAsync(new ArraySegment<byte>(data), SocketFlags.None, remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data, 0, result.ReceivedBytes); // Декодирование полученного сообщения

                    // Исключаем свои сообщения
                    if (!message.StartsWith(username + ":"))
                    {
                        // Обновление UI-потока для добавления сообщения в список сообщений
                        Dispatcher.Invoke(() => listBoxMessages.Items.Add(message));
                    }
                }
                catch (Exception ex) // Обработка исключений
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Ошибка при получении данных: {ex.Message}")); // Вывод сообщения об ошибке
                    break; // Прерывание цикла в случае ошибки
                }
            }
        }

        /// <summary>
        /// Закрытие сокетов при выходе.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            senderSocket?.Close(); // Закрытие сокета для отправки сообщений (если есть)
            receiverSocket?.Close(); // Закрытие сокета для приема сообщений (если есть)
            base.OnClosing(e); // Вызов базового метода закрытия окна
        }
    }
}
