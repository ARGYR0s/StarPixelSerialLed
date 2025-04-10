
using SkiaSharp;
using System.Collections.ObjectModel;
using Microsoft.Maui.Dispatching;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.Maui.Controls;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

//Install - Package Plugin.BLE

//dotnet publish -f net9.0-android -c Release
//dotnet publish -f net9.0-ios -c Release

namespace StarPixelApp
{
    public partial class App : Application
    {
        private static ConnectionManager? _connectionManager;
        private static PageCacheManager? _pageCacheManager;
        private static DynamicPageLoader? _pageLoader;
        private static RawFileCacheReader? _reader;

        private string connectionType;
        private string deviceId;


        private Services.DeviceDiscoveryService _discoveryService;

        //private Services.DeviceService _deviceService;

        public ObservableCollection<Connections.ConnectionDeviceInfo> Devices { get; } = new();

        private static Models.SerialReceiver _serialReceiver = new Models.SerialReceiver();
        private readonly IDispatcherTimer _uiUpdater;
        private readonly IDispatcherTimer _serialProcessing;
        public App()
        {
            InitializeComponent();

            var viewModel = new ViewModel(); //обработчик событий интерфейса

            //string jsonPath = Path.Combine(FileSystem.AppDataDirectory, "Config\\ui_config.json");
            string jsonPath = "Config\\ui_config.json";
            _pageCacheManager = new PageCacheManager(jsonPath);

            _pageLoader = new DynamicPageLoader(_pageCacheManager);

            // Не устанавливаем MainPage сразу, ждем завершения загрузки данных
            MainPage = new NavigationPage(new ContentPage()); // Временная страница-заглушка


            _uiUpdater = Dispatcher.CreateTimer();
            _uiUpdater.Interval = TimeSpan.FromMilliseconds(50);//(16); // 60 FPS
            _uiUpdater.Tick += (s, e) => UpdateUI();
            _uiUpdater.Start();
/*            
            //StartLoop();
            _serialProcessing = Dispatcher.CreateTimer();
            _serialProcessing.Interval = TimeSpan.FromMilliseconds(5);//(16); // 60 FPS
            _serialProcessing.Tick += (s, e) => SerialProcessing();
            _serialProcessing.Start();
*/
            _reader = new RawFileCacheReader();
            StartSerialProcessing();
            //Debug.WriteLine("DEBUG START!");
        }

        bool isDeviceFound = false;
        protected override async void OnStart()
        {
            
            await _pageCacheManager.LoadPagesAsync();

            connectionType = _pageCacheManager.GetSetting("connectionType")??"Virtual";
            deviceId = _pageCacheManager.GetSetting("deviceId")??"0x01";
            
            // find all devices
            var connection = Connections.ConnectionFactory.CreateConnection(connectionType);
            //_deviceService = new Services.DeviceService(connectionType, deviceId);
            _discoveryService = new Services.DeviceDiscoveryService((Connections.IDeviceDiscovery)connection);

            Devices.Clear();
            isDeviceFound = false;
            var devices = await _discoveryService.DiscoverDevicesAsync();

            foreach (var device in devices)
            {
                Devices.Add(device);
                if (device.Address == deviceId) 
                {
                    isDeviceFound = true;
                }

            }

            if (isDeviceFound)
            {
                ConnectionTry(deviceId);

            }
            else
            {
                _pageLoader.ClearCheckLists();

                NavigateToPage("SerialConfig");

            }

        }



        Stopwatch stopwatch = new Stopwatch();

        private CancellationTokenSource _cts = new();
        private Task _processingTask;
        //private readonly AutoResetEvent _dataAvailable = new(false);
        //private readonly object bufferLock = new(); // если еще нет

        public void StartSerialProcessing()
        {
            _processingTask = Task.Run(() => ProcessSerialData(_cts.Token));
        }

        public void StopSerialProcessing()
        {
            _cts.Cancel();
            //_dataAvailable.Set();
        }


        // Создаем объект Stopwatch для измерения времени рендера
        Stopwatch stopwatcSerial = new Stopwatch();


        private async Task ProcessSerialData(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_serialReceiver.isRequestUpdated || _serialReceiver.isDataAdded)
                {

                    stopwatcSerial.Restart();
                    //stopwatcSerial.Start();
                    // Подождать сигнал или timeout (вдруг пришло чуть-чуть данных)
                    //_dataAvailable.WaitOne();
                    //_serialReceiver.isRequestUpdated = false;
                    await SerialProcessing(); //300-600us
                    stopwatcSerial.Stop();
                    long elapsedMilliseconds = stopwatcSerial.ElapsedMilliseconds;
                    //long microseconds = stopwatcSerial.ElapsedTicks * 1000000 / Stopwatch.Frequency;
                    _serialReceiver.isDataAdded = false;
                }

            }
        }

        private void StartLoop()
        {
            /*
            Task.Run(async () =>
            {
                while (true)
                {
                    await _serialReceiver.ProcessDataExternally();
                    //await Task.Delay(1000); // Задержка 1 секунда, чтобы не перегружать процессор
                }
            });
            */
            //if
        }

        private async Task SerialProcessing()
        {
            //пересмотреть вызов, очень долгий
            /*
            byte[] data = {0x00, 0x01, 0x00};
            _connectionManager.SendDataAsync(data);
            */
            /*
            if (_serialReceiver.isUpdated)
            {
                _connectionManager.SendDataAsync(_serialReceiver.raw);
                _serialReceiver.isUpdated = false;
            }
            */
            string command2 = "+PXLS=2,0,0\n\n"; // или просто \n, зависит от устройства
            byte[] data2 = Encoding.UTF8.GetBytes(command2);

            string command6 = "+PXLS=6,0,0\n\n"; // или просто \n, зависит от устройства
            byte[] data6 = Encoding.UTF8.GetBytes(command6);

            //_serialReceiver.ProcessData();
            _serialReceiver.ProcessRequests();




            if (_reader.isFileOpened && _serialReceiver.isRequestUpdated)
            {
                switch (_serialReceiver.requestCmd)
                {
                    case 1: //Открыть файл
                    {
                        _connectionManager.SendDataAsync(data2);
                        break;
                    }

                    case 3: //Прочитать данные
                    {
                        // Читаем данные с нужным смещением и длиной
                        int offset = _serialReceiver.requestOffset;
                        int length = _serialReceiver.requestLength;

                        byte[] chunk = await _reader.GetDataAsync(offset: offset, length: length); //30-50us

                            
                        //stopwatcSerial.Restart();

                        int actualLength = chunk.Length;
                        // Формируем строку команды с указанием offset и length
                        string command4 = $"+PXLS=4,{offset},{actualLength}\n";
                        byte[] data4 = Encoding.UTF8.GetBytes(command4);
                        string newLine = "\n";
                        byte[] data5 = Encoding.UTF8.GetBytes(newLine);




                        //stopwatcSerial.Start();

                        // Объединяем команду и chunk
                        byte[] fullMessage = new byte[data4.Length + chunk.Length + 1];
                        Buffer.BlockCopy(data4, 0, fullMessage, 0, data4.Length);
                        Buffer.BlockCopy(chunk, 0, fullMessage, data4.Length, chunk.Length);
                        Buffer.BlockCopy(data5, 0, fullMessage, data4.Length + chunk.Length, data5.Length);

                            //stopwatcSerial.Restart();


                            long lastTimeMicros = _connectionManager.LastRXTimeUs;
                            long nowMicros = Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency;

                            long delta = nowMicros - lastTimeMicros;

                            AsyncEventBus.Publish("serialDeltaTime", $"serialDeltaTime: {delta}");

                            await _connectionManager.SendDataAsync(fullMessage); //20ms



                            
                        //stopwatcSerial.Stop();
                        //long microseconds = stopwatcSerial.ElapsedTicks * 1000000 / Stopwatch.Frequency;
                        break;
                    }


                    case 5: //Закрыть файл
                    {

                        _reader.Dispose();
                        _connectionManager.SendDataAsync(data6);
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
                //_serialReceiver.isRequestUpdated = false;
            }
        }

        private void UpdateUI()
        {
            _serialReceiver.ScreenUpdater();

        }

        public async Task ConnectionTry(string address)
        {
            //Debug.WriteLine($"Порт={address}");
            if (_connectionManager?.IsConnected == true)
            {
                _connectionManager.Stop(); // Остановим текущее соединение


                if (_connectionManager != null)
                {
                    _connectionManager.Dispose();
                    _connectionManager = null;
                    await Task.Delay(1000); // Даем время на корректное закрытие соединения
                }
            }

            _connectionManager = new ConnectionManager(connectionType, address);

            try
            {
                bool connected = await _connectionManager.ConnectAsync();

                await Task.Delay(500); // Даем время обновить состояние

                if (connected && _connectionManager.IsConnected)
                {
                    Debug.WriteLine("Подключение удалось!");
                    _pageCacheManager.UpdateSetting("deviceId", address);

                    NavigateToPage("MainPage");
                }
                else
                {
                    Debug.WriteLine("Не подключено!!");
                    _pageLoader.ClearCheckLists();
                    //MainPage = new NavigationPage(_pageLoader.GetPageView("SerialConfig"));
                    NavigateToPage("SerialConfig");
                    //await ShowDevicesAsync();
                }

            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Доступ запрещен: {ex.Message}");
            }
            catch (COMException ex)
            {
                Debug.WriteLine($"Ошибка COM: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Неизвестная ошибка: {ex.Message}");
            }
        }

        public async Task ShowDevicesAsync()
        {
            //_connectionManager.Stop();

            int icontrol = 0;
            foreach (var device in Devices)
            {

                _pageLoader.AddCheckListItem("сomCheckList", device.Address, "control"+icontrol);

                icontrol++;
            }
        }

        public async Task <IEnumerable<string>> GetDevices()
        {
            // Получаем список адресов устройств
            var deviceAddresses = Devices.Select(device => device.Address).ToList();

            // Здесь можно добавить любую дополнительную асинхронную логику,
            // если она нужна, с использованием await

            return deviceAddresses; // Возвращаем IEnumerable<string>
        }

        private async void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            if (!isConnected)
            {
                _pageLoader.ClearCheckLists();
                NavigateToPage("SerialConfig");

            }
        }

        public async Task<IEnumerable<string>> GetRecentFiles()
        {
            // Получаем список недавно открытых файлов
            var recentFiles = new List<string>();

            for (int i = 1; i <= 5; i++) // допустим, у вас 5 недавних файлов
            {
                string? file = _pageCacheManager.GetSetting($"recentFile{i}");
                if (!string.IsNullOrEmpty(file))
                {
                    recentFiles.Add(file);
                }
            }

            return recentFiles;
        }

        public async Task OpenTry(string path) 
        {
            //_pageCacheManager.UpdateSetting("recentFile1", path);
            // Шаг 1: Получаем текущий список последних 10 файлов
            var recentFiles = new List<string>();

            _reader.SetFile(path);
            //byte[] chunk = await _reader.GetDataAsync(offset: 1000, length: 512);
            //_reader.Dispose();

            for (int i = 1; i <= 5; i++)
            {
                string? file = _pageCacheManager.GetSetting($"recentFile{i}");
                if (!string.IsNullOrEmpty(file))
                {
                    recentFiles.Add(file);
                }
            }

            // Шаг 2: Удаляем путь, если он уже есть в списке (чтобы избежать дубликатов)
            recentFiles.Remove(path);

            // Шаг 3: Вставляем путь в начало списка
            recentFiles.Insert(0, path);

            // Шаг 4: Ограничиваем список до 10 элементов
            if (recentFiles.Count > 5)
            {
                recentFiles = recentFiles.Take(5).ToList();
            }

            // Шаг 5: Сохраняем обновлённый список обратно в настройки
            for (int i = 0; i < recentFiles.Count; i++)
            {
                _pageCacheManager.UpdateSetting($"recentFile{i + 1}", recentFiles[i]);
            }
            /*
            // Шаг 6: Удаляем лишние ключи, если ранее было сохранено больше
            for (int i = recentFiles.Count + 1; i <= 10; i++)
            {
                _pageCacheManager.UpdateSetting($"recentFile{i}", null);
            }
            */
        }

        public async Task ClearFilesConfig()
        {
            for (int i = 0; i <= 5; i++)
            {
                _pageCacheManager.UpdateSetting($"recentFile{i}", null);
            }
        }

        private void UpdateMainPage()
        {
            _pageLoader.IsPagePushed = true;
            // Устанавливаем основную страницу после загрузки данных
            MainPage = new NavigationPage(_pageLoader.GetPageView("MainPage"));

         }
        
        public static void NavigateToPage(string pageId)
        {
            if (Current.MainPage is NavigationPage navPage && navPage.CurrentPage is ContentPage currentPage)
            {
                var oldAutomationIds = GetAllAutomationIds(currentPage);
                UnsubscribeFromAllEvents(currentPage);
            }

            _pageLoader.IsPagePushed = true;
            var page = _pageLoader.GetPageView(pageId);
            if (page != null)
                (Current.MainPage as NavigationPage)?.PushAsync(page);
        }


        private static void UnsubscribeFromAllEvents(ContentPage page)
        {
            void UnsubscribeRecursive(View view)
            {
                if (view == null) return;

                if (!string.IsNullOrEmpty(view.AutomationId))
                {
                    AsyncEventBus.UnsubscribeId(view.AutomationId);
                }

                if (view is Layout layout)
                {
                    foreach (var child in layout.Children)
                    {
                        UnsubscribeRecursive((View)child);
                    }
                }
            }

            if (page.Content is Layout rootLayout)
            {
                foreach (var child in rootLayout.Children)
                {
                    UnsubscribeRecursive((View)child);
                }
            }
        }
       
        private static HashSet<string> GetAllAutomationIds(ContentPage page)
        {
            var ids = new HashSet<string>();

            void CollectIds(View view)
            {
                if (view == null) return;
                if (!string.IsNullOrEmpty(view.AutomationId))
                {
                    ids.Add(view.AutomationId);
                }
                if (view is Layout layout)
                {
                    foreach (var child in layout.Children)
                    {
                        CollectIds(child as View);
                    }
                }
            }

            if (page.Content is Layout rootLayout)
            {
                foreach (var child in rootLayout.Children)
                {
                    CollectIds(child as View);
                }
            }

            return ids;
        }
 
    }
}