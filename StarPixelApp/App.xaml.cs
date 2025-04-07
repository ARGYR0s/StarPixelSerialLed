
using SkiaSharp;
using System.Collections.ObjectModel;
using Microsoft.Maui.Dispatching;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

//Install - Package Plugin.BLE

//dotnet publish -f net9.0-android -c Release
//dotnet publish -f net9.0-ios -c Release

namespace StarPixelApp
{
    public partial class App : Application
    {
        private static ConnectionManager? _connectionManager;
        private static PageCacheManager _pageCacheManager;
        private static DynamicPageLoader _pageLoader;

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

            //StartLoop();
            _serialProcessing = Dispatcher.CreateTimer();
            _serialProcessing.Interval = TimeSpan.FromMilliseconds(5);//(16); // 60 FPS
            _serialProcessing.Tick += (s, e) => SerialProcessing();
            _serialProcessing.Start();

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

        private void StartLoop()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await _serialReceiver.ProcessDataExternally();
                    //await Task.Delay(1000); // Задержка 1 секунда, чтобы не перегружать процессор
                }
            });
        }

        private void SerialProcessing()
        {

            _serialReceiver.ProcessData();
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