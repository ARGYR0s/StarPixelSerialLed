using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarPixelApp.ViewModels;
using StarPixelApp.Models;
using System.Diagnostics;
//using static CoreFoundation.DispatchSource;

//namespace StarPixelApp.ViewModels
//{
class ViewModel
    {
        //private static SerialReceiver _serialReceiver = new SerialReceiver();

        public ViewModel()
        {
        //EventBus.Subscribe("ButtonClicked", OnButtonClicked);
        AsyncEventBus.Subscribe <string> ("сheckListChanged", OnCheckListChanged);
//        AsyncEventBus.Subscribe<byte[]>("deviceDataReceived", SeriaReceived);

        //new GridCanvasView(16, 128);
        //new GridCanvasView();
        //_serialReceiver = new SerialReceiver();
            //_serialReceiver.ScreenUpdater();
            //Task.Run(() => _serialReceiver.ScreenUpdater());
    }


        private void OnButtonClicked(object buttonId)
        {
            switch (buttonId.ToString())
            {
                case "OpenWindow":
                    OpenWindow();
                    break;
                case "OpenSecondPage":
                    OpenSecondPage();
                    break;
                case "UpdateDevices":
                    UpdateDevices();
                    break;
                case "GoBack":
                    GoBack();
                    break;
                default:
                    Console.WriteLine($"Неизвестная кнопка: {buttonId}");
                    break;
            }
        }

        private async void OnCheckListChanged(string item)
        {
        //Application.Current.MainPage.DisplayAlert("Оповещение", data.ToString(), "OK");
            (Application.Current as StarPixelApp.App).ConnectionTry(item);
        }

        //int dataUpdated;
        private async void SeriaReceived(object data)
        {

            // Создаем экземпляр Stopwatch
            //Stopwatch stopwatch = new Stopwatch();

            //stopwatch.Reset(); // Сбрасываем счетчик
            //stopwatch.Start();

        //Application.Current.MainPage.DisplayAlert("Оповещение", data.ToString(), "OK");
        //++dataUpdated;
        //EventBus.Publish("labelMain", "Data Updated "+ dataUpdated.ToString());

        if (data is byte[] bytes)
            {
                // Преобразование байтов в строку с использованием UTF-8
                string text = Encoding.UTF8.GetString(bytes);

            // Увеличение счетчика обновлений
            //++dataUpdated;

            // Публикация обновленного текста для отображения в Label
            //EventBus.Publish("labelMain", $"{dataUpdated}: {text}");

            //AsyncEventBus.Publish("labelSerial", "labelSerial: " + text);
            Debug.WriteLine($"labelSerial: {text}");
        }
            else
            {
                //Console.WriteLine("Error: Expected byte[] data, but received: " + data.GetType().Name);
            }

            //stopwatch.Stop();

            //long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            //EventBus.Publish("labelTime", elapsedMilliseconds.ToString());
        }

        public static async Task OpenWindow()
        {
            //await Application.Current.MainPage.DisplayAlert("Оповещение", "Окно открыто!", "OK");
            //new MainViewModel();

        }

        public static async Task OpenSecondPage()
        {
        //var secondPage = new DynamicPage("SecondPage");
        //await Application.Current.MainPage.Navigation.PushAsync(secondPage);
            //await App.NavigateToPage("SecondPage");
            StarPixelApp.App.NavigateToPage("SecondPage");
        }

        public static async Task OpenSettings()
        {
            //var secondPage = new DynamicPage("SecondPage");
            //await Application.Current.MainPage.Navigation.PushAsync(secondPage);
            //await App.NavigateToPage("SecondPage");
            StarPixelApp.App.NavigateToPage("SerialConfig");
        /*
            StarPixelApp.App app = (StarPixelApp.App)Application.Current;
            await app.ShowDevicesAsync();
        */
        }

        public static async Task ActionSerialList(StackLayout checkList, string action, string serialAddr)
        {
            Debug.WriteLine("ActionSerialList");
            if (serialAddr == null) //инициализация
            {
                StarPixelApp.App app = (StarPixelApp.App)Application.Current;
                //await app.ShowDevicesAsync();
                DynamicPageLoader.CreateCheckListItems(checkList, action, await app.GetDevices());
                Debug.WriteLine("ActionSerialList Init list");
            }
            else //действия по нажатию
            {
                StarPixelApp.App app = (StarPixelApp.App)Application.Current;
                app.ConnectionTry(serialAddr);
                Debug.WriteLine("Action " + serialAddr);
            }
        }

        public static async Task UpdateCanvas()
        {
            //int dfsdfs = 0;
            //await _serialReceiver.ProcessDataExternally();
        }

        public static async void UpdateDevices()
        {
            StarPixelApp.App.NavigateToPage("SecondPage");
        }

        public static async void GoBack()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }


}
//}
