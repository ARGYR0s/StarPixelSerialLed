The application is written in .NET MAUI.

It is required that the application can connect via a saved communication channel (Bluetooth, USB, virtual, etc.) to a saved device. If the connection fails or this is the first launch, the user must be provided with a choice of the required connection type and a display of available devices for that connection type. From the provided list of devices, connection should be possible.

For implementing possible connection types, the Hexagonal (Ports and Adapters) architectural pattern is used. Connections are created from a single interface.

The application operates based on the Event-Driven Architecture (EDA) principle combined with MVVM.

For flexibility, XAML is not used in Views; instead, pages are loaded from JSON. This JSON file also stores application parameters (connection type and device for connection). Pages and parameters are then stored in memory and are not reloaded from the file. This allows modifying the application's appearance without recompiling the application itself. The JSON is read during application startup or when initializing the parameter loading function from the file. Each UI element can have an ID. If data with a specified ID is received via the communication channel, such elements are updated with this data.

Приложение написано на .NET MAUI.

Необходимо чтоб приложение могло подключаться по сохраненному каналу связи (bluetooth, usb, virtual...) к сохраненному устройству. Если не получилось подключиться или это первый запуск - то необходимо предоставить выбор необходимого типа подключения и  отображение найденных устройств по данному типу соединения. По предоставленному списку устройств должно быть доступно подключение.

Для реализации возможных типов соединения используется архитектурный патерн Hexagonal (Ports and Adapters). Соединения созданы из одного интерфейса.

Для работы приложения используется архитектурный принцип Event-Driven (EDA) + MVVM

Для реализации гибкости не используется XAML во View, а используется загрузка страницы из JSON. В этом файле JSON так же хранятся параметры приложения (тип соединения и устройство для подключения). Страницы и параметры дальше хранятся в памяти и не перечитываются из файла. Это позволяет изменять вид приложения без перекомпиляции самого приложения. JSON считывается при загрузке приложения или при инициализации функции загрузки параметров из файла. Каждый элемент интерфейса может иметь ID. Если данные с указанным ID приходят по каналу связи - такие элементы обновляются этими данными. 