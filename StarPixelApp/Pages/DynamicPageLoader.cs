using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using SkiaSharp;
using System.Collections.Generic;
using System.Reflection;
using SkiaSharp.Views.Maui.Controls;
using StarPixelApp.ViewModels;
using System.Diagnostics;
//using StackLayout = Microsoft.Maui.Controls.Compatibility.StackLayout;
//using AbsoluteLayout = Microsoft.Maui.Controls.Compatibility.AbsoluteLayout;
//using Microsoft.UI.Xaml.Controls;

public class DynamicPageLoader
{
    private readonly PageCacheManager _pageCacheManager;
    private readonly Dictionary<string, StackLayout> _checkLists = new Dictionary<string, StackLayout>();

    private readonly Dictionary<string, Action<object>> _eventHandlers = new();

    // Этот флаг указывает, что страница была создана и добавлена через PushAsync
    public bool IsPagePushed { get; set; } // Change to 'public' setter

    public DynamicPageLoader(PageCacheManager pageCacheManager)
    {
        _pageCacheManager = pageCacheManager;
    }

    public ContentPage? GetPageView(string pageId)
    {
        var pageData = _pageCacheManager.GetPage(pageId);

        if (pageData == null)
        {
            var page404 = new ContentPage { Title = "404" };
            page404.Content = new Label { Text = $"{pageId}: Страница не найдена", TextColor = Colors.Red };
            return page404;
        }

        var page = new ContentPage { Title = pageData.Name };
        var absoluteLayout = new AbsoluteLayout();


        // Обработчик событий при открытии страницы
        page.Appearing += (s, e) =>
        {
            // Код, который выполняется при открытии страницы
            Debug.WriteLine($"Страница {pageId} открыта");

            if (!IsPagePushed)
            {
                Debug.WriteLine("вернулись через кнопку назад");

                StarPixelApp.App.NavigateToPage(pageId);
            }

            IsPagePushed = false;
            // Можно подписаться на события или выполнить другие действия
        };

        // Обработчик событий при закрытии страницы
        page.Disappearing += (s, e) =>
        {
            // Код, который выполняется при закрытии страницы
            //Debug.WriteLine($"Страница {pageId} закрыта");
            // Небольшая задержка перед проверкой стека

            // Можно отписаться от событий или выполнить другие действия
        };

        // Если вам нужно удостовериться, что страница была именно добавлена через PushAsync



        foreach (var element in pageData.Controls)
        {
            var view = CreateView(element);
            if (view != null)
                absoluteLayout.Children.Add(view);
        }

        page.Content = absoluteLayout;
        return page;
    }

    private static readonly Dictionary<string, LayoutOptions> LayoutOptionsMap = new Dictionary<string, LayoutOptions>(StringComparer.OrdinalIgnoreCase)
    {
        { "start", LayoutOptions.Start },
        { "center", LayoutOptions.Center },
        { "end", LayoutOptions.End },
        { "fill", LayoutOptions.Fill }
    };

    private static LayoutOptions ParseLayoutOptions(string? value, LayoutOptions defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        string key = value.Trim();
        return LayoutOptionsMap.TryGetValue(key, out LayoutOptions options) ? options : defaultValue;
    }

    private View? CreateView(ControlConfig control)
    {
        View? view = null;

        switch (control.Type)
        {
            case "Label":
                var label = new Label
                {
                    Text = control.Text,
                    AutomationId = control.Id,
                    //for finding - var element = page.FindByName<View>("myElementId");
                    FontSize = control.FontSize ?? 18, // Используем значение по умолчанию, если не указано
                    HorizontalOptions = ParseLayoutOptions(control.HorizontalAlignment, LayoutOptions.Fill),
                    VerticalOptions = ParseLayoutOptions(control.VerticalAlignment, LayoutOptions.Fill),
                    WidthRequest = control.Width ?? -1, // -1 означает, что размер будет определяться автоматически
                    HeightRequest = control.Height ?? -1
                };
                
                Action<object> labelHandler = data =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        //Debug.WriteLine($"Получены данные label: {data}");
                        label.Text = data?.ToString() ?? string.Empty;
                    });
                };
                AsyncEventBus.Subscribe(control.Id, labelHandler);
                view = label;

                break;
                

            case "Button":
                var button = new Button
                {
                    Text = control.Text,
                    AutomationId = control.Id,
                    FontSize = control.FontSize ?? 16, // Используем значение по умолчанию, если не указано
                    HorizontalOptions = ParseLayoutOptions(control.HorizontalAlignment, LayoutOptions.Fill),
                    VerticalOptions = ParseLayoutOptions(control.VerticalAlignment, LayoutOptions.Fill),
                    WidthRequest = control.Width ?? -1, // -1 означает, что размер будет определяться автоматически
                    HeightRequest = control.Height ?? -1
                };

                var actionMethod = FindMethod(control.Action);
                /*if (actionMethod != null)
                {
                    button.Clicked += (s, e) => actionMethod.Invoke(null, null);
                }*/
                if (actionMethod != null)
                {
                    button.Clicked += async (s, e) =>
                    {
                        try
                        {
                            Task task = (Task)actionMethod.Invoke(null, null);
                            if (task != null)
                                await task;
                        }
                        catch (Exception ex)
                        {
                            await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
                        }
                    };
                }
                //button.Clicked += (s, e) => EventBus.Publish("buttonClicked", control.Id);

                view = button;
                break;

            case "Image":
                var image = new Image
                {
                    Source = control.Source,
                    AutomationId = control.Id,
                    HorizontalOptions = ParseLayoutOptions(control.HorizontalAlignment, LayoutOptions.Fill),
                    VerticalOptions = ParseLayoutOptions(control.VerticalAlignment, LayoutOptions.Fill),
                    WidthRequest = control.Width ?? -1, // -1 означает, что размер будет определяться автоматически
                    HeightRequest = control.Height ?? -1,
                    //Aspect = control.Aspect ?? Aspect.AspectFit
                };
                view = image;
                break;

            case "Canvas":
                var canvasView = new SKCanvasView
                {
                    AutomationId = control.Id,
                    HorizontalOptions = ParseLayoutOptions(control.HorizontalAlignment, LayoutOptions.Fill),
                    VerticalOptions = ParseLayoutOptions(control.VerticalAlignment, LayoutOptions.Fill),
                    WidthRequest = control.Width ?? -1,
                    HeightRequest = control.Height ?? -1
                };

                var renderer = new PixelRenderer(canvasView, (int)canvasView.WidthRequest, (int)canvasView.HeightRequest);
                canvasView.PaintSurface += renderer.OnPaintSurface;


                AsyncEventBus.Subscribe<List<(int X, int Y, SKColor Color)>>(control.Id, async data =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Создаем объект Stopwatch для измерения времени рендера
                        //Stopwatch stopwatch = new Stopwatch();
                        //stopwatch.Start();

                        if (data is List<(int X, int Y, SKColor Color)> pixels)
                        {
                            var newBitmap = renderer.CreateBitmapFromPixels(pixels);
                            if (newBitmap != null)
                            {
                                renderer.SetBitmap(newBitmap);
                            }
                        }

                    });
                });
                //_eventHandlers[control.Id] = labelHandler; // Сохранение обработчика
                view = canvasView;
                break;

            case "CheckList":
                var stackLayout = new StackLayout
                {
                    AutomationId = control.Id,
                    Orientation = StackOrientation.Vertical,
                    HorizontalOptions = ParseLayoutOptions(control.HorizontalAlignment, LayoutOptions.Fill),
                    VerticalOptions = ParseLayoutOptions(control.VerticalAlignment, LayoutOptions.Fill),
                    WidthRequest = control.Width ?? -1, // -1 означает, что размер будет определяться автоматически
                    HeightRequest = control.Height ?? -1,
                };

                Debug.WriteLine($"CheckList with ID {control.Id} created");

                // Находим метод
                var actionMethodList = FindMethod(control.Action);

                // Вызываем метод сразу при создании чеклиста
                actionMethodList?.Invoke(null, new object[] { stackLayout, control.Action, null });

                // Добавляем элементы при создании (по желанию)
//                CreateCheckListItems(stackLayout, control.Id);
                //_checkLists[control.Id] = stackLayout;

                view = stackLayout;
                break;

            default:
                return null;
        }

        // Применение дополнительных свойств, таких как отступы и выравнивание
        if (view != null)
        {
            if (control.Margin != null)
            {
                view.Margin = new Thickness(control.Margin.Left ?? 0, control.Margin.Top ?? 0, control.Margin.Right ?? 0, control.Margin.Bottom ?? 0);
            }
            /*
             * //свойство недоступно
            if (control.Padding != null)
            {
                view.Padding = new Thickness(control.Padding.Left ?? 0, control.Padding.Top ?? 0, control.Padding.Right ?? 0, control.Padding.Bottom ?? 0);
            }
            */
            if (control.BackgroundColor != null)
            {
                view.BackgroundColor = Color.FromHex(control.BackgroundColor);
            }

            // Установка позиции (X, Y) для AbsoluteLayout
            if (control.X != null && control.Y != null)
            {
                double x = control.X.Value; // Ограничение X
                double y = control.Y.Value; // Ограничение Y
                double width = control.Width ?? view.WidthRequest;
                double height = control.Height ?? view.HeightRequest;

                AbsoluteLayout.SetLayoutBounds(view, new Rect(x, y, width, height));
                AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.None);
            }
        }
        return view;
    }

    public void AddCheckListItem(string checkListId, string item, string controlId)
    {
        Debug.WriteLine($"Add: {item}");

        // Ищем StackLayout по AutomationId
        var stackLayout = FindByAutomationId<StackLayout>(checkListId);
        if (stackLayout == null)
        {
            Debug.WriteLine($"CheckList with ID {checkListId} not found");
            return;
        }

        //if (_checkLists.TryGetValue(checkListId, out var stackLayout))
        {
            var checkBox = new CheckBox
            {
                IsChecked = false
            };

            var labelItem = new Label
            {
                Text = item,
                VerticalOptions = LayoutOptions.Center
            };

            var itemLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { checkBox, labelItem }
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                //AsyncEventBus.Publish("сheckListChanged", item);
                Debug.WriteLine("CheckList action");
            };
            itemLayout.GestureRecognizers.Add(tapGesture);

            checkBox.CheckedChanged += (s, e) =>
            {
                //AsyncEventBus.Publish(controlId, new { Item = item, IsChecked = e.Value });
                //AsyncEventBus.Publish("сheckListChanged", item);
                Debug.WriteLine("CheckList action");
            };

            stackLayout.Children.Add(itemLayout);
        }
        /*
        else
        {
            // Можно добавить логирование или выбросить исключение
            Debug.WriteLine($"CheckList with ID {checkListId} not found");
        }
        */
    }

    public static void CreateCheckListItems(StackLayout checkList, string action, IEnumerable<string> items)
    {
        //int itemNumber = 1;
        foreach (var itemText in items)
        {
            var checkBox = new CheckBox
            {
                IsChecked = false,
                VerticalOptions = LayoutOptions.Center
            };

            var labelItem = new Label
            {
                Text = itemText,
                VerticalOptions = LayoutOptions.Center
            };

            var itemLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { checkBox, labelItem }
            };

            var actionMethod = FindMethod(action);


            if (actionMethod != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    try
                    {
                        Task task = (Task)actionMethod.Invoke(null, new object[] { checkList, action, itemText });
                        if (task != null)
                            await task;
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                    //AsyncEventBus.Publish("сheckListChanged", item);
                    Debug.WriteLine("CheckList action");
                };
                itemLayout.GestureRecognizers.Add(tapGesture);

                checkBox.CheckedChanged += async (s, e) =>
                {
                    try
                    {
                        Task task = (Task)actionMethod.Invoke(null, new object[] { checkList, action, itemText });
                        if (task != null)
                            await task;
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
                    }

                    Debug.WriteLine("CheckList action");
                };
            }
            checkList.Children.Add(itemLayout);
        }
    }

    public void ClearCheckLists()
    {
        _checkLists.Clear();
    }

    private MenuFlyoutItem CreateMenuFlyoutItem(ControlConfig control)
    {
        var menuFlyoutItem = new MenuFlyoutItem
        {
            Text = control.Text
        };

        var actionMethod = FindMethod(control.Action);
        if (actionMethod != null)
        {
            menuFlyoutItem.Clicked += (s, e) => actionMethod.Invoke(null, null);
        }

        return menuFlyoutItem;
    }

    private static MethodInfo FindMethod(string methodName)
    {
        return typeof(ViewModel).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
    }

    private T FindByAutomationId<T>(string automationId) where T : VisualElement
    {
        return Application.Current?.MainPage?.FindByName<T>(automationId);
    }
}
