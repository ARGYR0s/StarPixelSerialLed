using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarPixelApp.ViewModels;
using StarPixelApp.Models;
using System.Diagnostics;
using Microsoft.Maui.Storage;
//using static CoreFoundation.DispatchSource;

//namespace StarPixelApp.ViewModels
//{
class ViewModel
    {
        
        public ViewModel()
        {

        AsyncEventBus.Subscribe <string> ("сheckListChanged", OnCheckListChanged);
        }


        private void OnButtonClicked(object buttonId)
        {
        /*
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
                case "OpenMedia":
                    OpenMedia();
                    break;
                case "BrowseFile":
                    BrowseFile();
                    break;
                case "ClearListFiles":
                    ClearListFiles();
                    break;
                case "GoBack":
                    GoBack();
                    break;
                default:
                    Debug.WriteLine($"Неизвестная кнопка: {buttonId}");
                    break;
            }
        */
        }

        private async void OnCheckListChanged(string item)
        {
        //Application.Current.MainPage.DisplayAlert("Оповещение", data.ToString(), "OK");
            (Application.Current as StarPixelApp.App).ConnectionTry(item);
        }

        //int dataUpdated;
        private async void SeriaReceived(object data)
        {

        if (data is byte[] bytes)
            {
                // Преобразование байтов в строку с использованием UTF-8
                string text = Encoding.UTF8.GetString(bytes);
                Debug.WriteLine($"labelSerial: {text}");
            }
        }

        public static async Task OpenWindow()
        {
            //test window
            //await Application.Current.MainPage.DisplayAlert("Оповещение", "Окно открыто!", "OK");
            //new MainViewModel();

        }

        public static async Task OpenSecondPage()
        {
            StarPixelApp.App.NavigateToPage("SecondPage");
        }

        public static async Task OpenSettings()
        {

            StarPixelApp.App.NavigateToPage("SerialConfig");
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

        public static async void OpenMedia()
        {
        //StarPixelApp.App.NavigateToPage("SecondPage");
            StarPixelApp.App.NavigateToPage("BrowseFiles");
            //StarPixelApp.App.NavigateToPage("MainPage");
        }

        public static async Task ActionFileList(StackLayout checkList, string action, string fileAddr)
        {
            Debug.WriteLine("ActionFileList");
            if (fileAddr == null) //инициализация
            {
                StarPixelApp.App app = (StarPixelApp.App)Application.Current;
                //await app.ShowDevicesAsync();
                DynamicPageLoader.CreateCheckListItems(checkList, action, await app.GetRecentFiles());
                Debug.WriteLine("ActionFileList Init list");
            }
            else //действия по нажатию
            {
                StarPixelApp.App app = (StarPixelApp.App)Application.Current;
                app.OpenTry(fileAddr);
                //StarPixelApp.App.NavigateToPage("BrowseFiles");
                StarPixelApp.App.NavigateToPage("MainPage");
                Debug.WriteLine("Action RecentFile" + fileAddr);
            }
        }

        public static async void BrowseFile()
        {
            //StarPixelApp.App.NavigateToPage("SecondPage");
            //StarPixelApp.App.NavigateToPage("BrowseFiles");
            var pxlFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { ".pxl" } },
                { DevicePlatform.iOS, new[] { "public.data" } }, // или другой UTI, если знаешь точный
                { DevicePlatform.MacCatalyst, new[] { ".pxl" } },
                { DevicePlatform.WinUI, new[] { ".pxl" } }
            });

            var fileResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите файл .pxl",
                FileTypes = pxlFileType
            });

            if (fileResult != null)
            {
                //_gifPath = fileResult.FullPath;
                StarPixelApp.App app = (StarPixelApp.App)Application.Current;
                app.OpenTry(fileResult.FullPath);
                Debug.WriteLine("Action OpenFile" + fileResult.FullPath);
                //StarPixelApp.App.NavigateToPage("BrowseFiles");
                StarPixelApp.App.NavigateToPage("MainPage");
            }
        }

        public static async void ClearListFiles()
        {
            StarPixelApp.App app = (StarPixelApp.App)Application.Current;
            app.ClearFilesConfig();
            StarPixelApp.App.NavigateToPage("BrowseFiles");
        }

        public static async void GoBack()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }


}
//}
