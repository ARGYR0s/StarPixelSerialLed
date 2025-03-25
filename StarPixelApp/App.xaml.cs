
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
            //_pageCacheManager.LoadPagesAsync();
            _pageLoader = new DynamicPageLoader(_pageCacheManager);

            //MainPage = new NavigationPage(new DynamicPage("MainPage"));
            //MainPage = new NavigationPage(_pageLoader.GetPageView("main"));
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
                //_pageLoader.AddCheckListItem("ComCheckList", device.Name, "control" + icontrol);
                //icontrol++;
            }

            if (isDeviceFound)
            {

                //connect serial
                //_connectionManager = new ConnectionManager(connectionType, deviceId);
                //_connectionManager.ConnectAsync().Wait();

                //_connectionManager.ConnectAsync();

                ConnectionTry(deviceId);
                /*
                // После загрузки данных обновляем MainPage
                if (_connectionManager.IsConnected)
                {
                    UpdateMainPage();
                }
                else
                {
                    _pageLoader.ClearCheckLists();
                    MainPage = new NavigationPage(_pageLoader.GetPageView("SerialConfig"));
                    //_pageLoader.AddCheckListItem("ComCheckList", "Новый элемент", "control1");

                    //string savedConnectionType = Preferences.Get(ConnectionTypeKey, "USB");

                    //            var connection = Connections.ConnectionFactory.CreateConnection("Virtual"); // Можно менять на USB
                    //var connection = Connections.ConnectionFactory.CreateConnection(connectionType);                                  //----
                    //var connection = Connections.ConnectionFactory.CreateConnection("Bluetooth"); // Можно менять на USB
                    //_deviceService = new Services.DeviceService(connectionType, deviceId);
                    //_discoveryService = new Services.DeviceDiscoveryService((Connections.IDeviceDiscovery)connection);

                    await ShowDevicesAsync();
                }*/
            }
            else
            {
                _pageLoader.ClearCheckLists();
                //MainPage = new NavigationPage(_pageLoader.GetPageView("SerialConfig"));
                NavigateToPage("SerialConfig");
                //await ShowDevicesAsync();
            }
            //MainPage = new NavigationPage(_pageLoader.GetPageView("main"));
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


            // Вызываем обновление элементов UI через DataBus
            //_serialReceiver.ProcessDataExternally();

            //stopwatch.Stop();
            //long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            //AsyncEventBus.Publish("labelTime", "labelTime: " + elapsedMilliseconds.ToString());
           //Debug.WriteLine($"labelTime: {elapsedMilliseconds.ToString()}");

            _serialReceiver.ScreenUpdater();
            //stopwatch.Reset(); // Сбрасываем счетчик
            //stopwatch.Start();
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
            //_connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged; // Подписка на события

            try
            {
                //Debug.WriteLine($"Перед подключением: IsConnected={_connectionManager?.IsConnected}");


                bool connected = await _connectionManager.ConnectAsync();

                await Task.Delay(500); // Даем время обновить состояние

                if (connected && _connectionManager.IsConnected)
                {
                    Debug.WriteLine("Подключение удалось!");
                    _pageCacheManager.UpdateSetting("deviceId", address);
                    //UpdateMainPage();
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

                /*
                if (_connectionManager.IsConnected)
                {
                    _pageCacheManager.UpdateSetting("deviceId", address);
                    UpdateMainPage();
                }
                else
                {
                    _pageLoader.ClearCheckLists();
                    MainPage = new NavigationPage(_pageLoader.GetPageView("SerialConfig"));

                    await ShowDevicesAsync();
                }
                */
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
                //_pageLoader.CreateCheckListItems("сomCheckList", device.Address, "control" + icontrol);
                icontrol++;
            }
            /*
            if (devices.Count < 1)
            {
                _pageLoader.AddCheckListItem("ComCheckList", "Нет устройств", "control1");
                // Формируем строку с устройствами для отображения
                //var deviceList = string.Join(Environment.NewLine, devices.Select(d => $"{d.Name}"));
                //await Application.Current.MainPage.DisplayAlert("USB Devices", deviceList, "OK");
            }
            */

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
                //_pageLoader.IsPagePushed = true;
                //MainPage = new NavigationPage(_pageLoader.GetPageView("SerialConfig"));
                NavigateToPage("SerialConfig");
                //await ShowDevicesAsync();
            }
            /*
            Debug.WriteLine($"Статус соединения изменился: {(isConnected ? "Подключено" : "Отключено")}");

            if (!isConnected)
            {
                Debug.WriteLine("Соединение потеряно, пробуем переподключиться...");
                Task.Run(async () => await ConnectionTry(lastUsedAddress)); // Повторное подключение
            }
            */
        }

        private void UpdateMainPage()
        {
            _pageLoader.IsPagePushed = true;
            // Устанавливаем основную страницу после загрузки данных
            MainPage = new NavigationPage(_pageLoader.GetPageView("MainPage"));

            /*
            // Создаем новый путь (линия)
            var path = new SKPath();
            path.MoveTo(50, 50);
            path.LineTo(150, 150);

            var command = new ViewModels.DrawCommand(path, SKColors.Red); // Рисуем красную линию

            EventBus.Publish("canvas1", command);
            */
            /*
            List<(int X, int Y, SKColor Color)> squarePixels = new();
            int startX = 10, startY = 10, size = 20;
            SKColor color = SKColors.Red;

            // Заполняем массив пикселями для квадрата
            for (int x = startX; x < startX + size; x++)
            {
                for (int y = startY; y < startY + size; y++)
                {
                    squarePixels.Add((x, y, color));
                }
            }

            // Отправляем пиксели на EventBus
            EventBus.Publish("canvas1", squarePixels);
            */
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
       
/*        
        public static void NavigateToPage(string pageId) // с очисткой от подписок только которых нет на следующей странице
        {
            _pageLoader.IsPagePushed = true;
            if (Current.MainPage is NavigationPage navPage && navPage.CurrentPage is ContentPage currentPage)
            {
                var oldAutomationIds = GetAllAutomationIds(currentPage);
                var newPage = _pageLoader.GetPageView(pageId);
                if (newPage != null)
                {
                    var newAutomationIds = GetAllAutomationIds(newPage);
                    //-----UnsubscribeFromObsoleteEvents(currentPage, newAutomationIds);
                    UnsubscribeNotUsed(oldAutomationIds, newAutomationIds);
                    (Current.MainPage as NavigationPage)?.PushAsync(newPage);
                }
            }
        }

        // Функция для отписки от устаревших подписок
        private static void UnsubscribeNotUsed(HashSet<string> oldIds, HashSet<string> newIds)
        {
            foreach (var id in oldIds)
            {
                if (!newIds.Contains(id)) // Если элемента нет на новой странице — отписываемся
                {
                    AsyncEventBus.UnsubscribeId(id);
                    Debug.WriteLine($"Отписались: {id}");
                }
            }
        }
*/
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
        /*
        private static void UnsubscribeFromObsoleteEvents(ContentPage page, HashSet<string> newAutomationIds)
        {
            void UnsubscribeRecursive(View view)
            {
                if (view == null) return;

                if (!string.IsNullOrEmpty(view.AutomationId) && !newAutomationIds.Contains(view.AutomationId))
                {
                    AsyncEventBus.UnsubscribeId(view.AutomationId);
                }

                if (view is Layout layout)
                {
                    foreach (var child in layout.Children)
                    {
                        UnsubscribeRecursive(child as View);
                    }
                }
            }

            if (page.Content is Layout rootLayout)
            {
                foreach (var child in rootLayout.Children)
                {
                    UnsubscribeRecursive(child as View);
                }
            }
        }
        */

    }
}